using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Services;

public interface ICityDistrictsService
{
    Task<ToolResponse<CleanCityDistrictFeatureCollection>> GetCityDistrictsAsync(
        string[]? districts = null,
        int limit = 1000,
        int offset = 0);
}

public class CityDistrictsService : ICityDistrictsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly Serilog.ILogger _logger;
    
    private const string BaseApiUrl = "https://api.golemio.cz/v2/citydistricts";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24); // Cache for 24 hours since city districts don't change often

    public CityDistrictsService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = Log.ForContext<CityDistrictsService>();
    }

    public async Task<ToolResponse<CleanCityDistrictFeatureCollection>> GetCityDistrictsAsync(
        string[]? districts = null,
        int limit = 1000,
        int offset = 0)
    {
        try
        {
            // Create cache key based on parameters
            var cacheKey = $"citydistricts_{string.Join(",", districts ?? Array.Empty<string>())}_{limit}_{offset}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out CleanCityDistrictFeatureCollection? cachedResult))
            {
                _logger.Debug("Retrieved city districts from cache with key: {CacheKey}", cacheKey);
                return new ToolResponse<CleanCityDistrictFeatureCollection>
                {
                    Success = true,
                    Data = cachedResult
                };
            }

            var queryParams = new List<string>();

            if (districts != null && districts.Length > 0)
                queryParams.Add($"districts={string.Join(",", districts)}");

            // Always set limit to 1000 and offset to 0 as per requirements
            queryParams.Add($"limit={limit}");
            queryParams.Add($"offset={offset}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var requestUrl = BaseApiUrl + queryString;

            _logger.Debug("Starting city districts API request to {RequestUrl}", requestUrl);

            var response = await SendApiRequestAsync(requestUrl);
            if (response.Success == false)
            {
                return new ToolResponse<CleanCityDistrictFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var districtCollection = JsonSerializer.Deserialize<CityDistrictFeatureCollection>(response.Data!, options);

            if (districtCollection == null)
            {
                _logger.Warning("Failed to deserialize city districts response");
                return new ToolResponse<CleanCityDistrictFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize city districts response"
                };
            }

            // Process the data: remove type fields and calculate center coordinates
            var cleanCollection = ProcessCityDistrictsData(districtCollection);

            // Cache the result
            _cache.Set(cacheKey, cleanCollection, CacheExpiration);
            _logger.Information("Successfully retrieved and cached {DistrictCount} city districts",
                cleanCollection.Features.Count);

            return new ToolResponse<CleanCityDistrictFeatureCollection>
            {
                Success = true,
                Data = cleanCollection
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while fetching city districts");
            return new ToolResponse<CleanCityDistrictFeatureCollection>
            {
                Success = false,
                ErrorMessage = $"Error occurred while fetching city districts: {ex.Message}"
            };
        }
    }

    private async Task<ToolResponse<string>> SendApiRequestAsync(string requestUrl)
    {
        try
        {
            var apiToken = _configuration["GolemioApiToken"];
            if (string.IsNullOrEmpty(apiToken))
            {
                _logger.Error("GolemioApiToken is not configured in appsettings.json");
                return new ToolResponse<string>
                {
                    Success = false,
                    ErrorMessage = "GolemioApiToken is not configured"
                };
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("accept", "application/json; charset=utf-8");
            request.Headers.Add("x-access-token", apiToken);

            _logger.Debug("Sending HTTP request with headers configured");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("API request failed with status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return new ToolResponse<string>
                {
                    Success = false,
                    ErrorMessage = $"API request failed: {response.StatusCode} - {response.ReasonPhrase}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.Debug("Received response with {ContentLength} characters", jsonContent.Length);

            return new ToolResponse<string>
            {
                Success = true,
                Data = jsonContent
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred during API request");
            return new ToolResponse<string>
            {
                Success = false,
                ErrorMessage = $"Error occurred during API request: {ex.Message}"
            };
        }
    }

    private CleanCityDistrictFeatureCollection ProcessCityDistrictsData(CityDistrictFeatureCollection rawCollection)
    {
        var cleanCollection = new CleanCityDistrictFeatureCollection
        {
            Features = rawCollection.Features.Select(feature => new CleanCityDistrictFeature
            {
                Geometry = new CleanCityDistrictGeometry
                {
                    Coordinates = CalculatePolygonCenter(feature.Geometry.Coordinates)
                },
                Properties = feature.Properties // Properties don't contain type fields, so we can use them directly
            }).ToList()
        };

        return cleanCollection;
    }

    private double[] CalculatePolygonCenter(List<List<List<double>>> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0 || coordinates[0].Count == 0)
        {
            _logger.Warning("Invalid coordinates provided for center calculation");
            return new double[] { 0, 0 };
        }

        // Get the first polygon (outer ring)
        var polygon = coordinates[0];
        
        double sumLon = 0;
        double sumLat = 0;
        int pointCount = 0;

        foreach (var point in polygon)
        {
            if (point.Count >= 2)
            {
                sumLon += point[0]; // longitude
                sumLat += point[1]; // latitude
                pointCount++;
            }
        }

        if (pointCount == 0)
        {
            _logger.Warning("No valid coordinate points found for center calculation");
            return new double[] { 0, 0 };
        }

        var centerLon = sumLon / pointCount;
        var centerLat = sumLat / pointCount;

        _logger.Debug("Calculated center coordinates: [{CenterLon}, {CenterLat}] from {PointCount} points", 
            centerLon, centerLat, pointCount);

        return new double[] { centerLon, centerLat };
    }
}
