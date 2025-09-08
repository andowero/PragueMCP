using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Services;

public interface IBicycleCounterService
{
    Task<ToolResponse<CleanBicycleCounterFeatureCollection>> GetBicycleCountersAsync(
        string? latlng = null,
        string? range = null,
        string? limit = null,
        string? offset = null);
}

public class BicycleCounterService : IBicycleCounterService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly Serilog.ILogger _logger;
    private const string ApiUrl = "https://api.golemio.cz/v2/bicyclecounters";

    public BicycleCounterService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = Log.ForContext<BicycleCounterService>();
    }

    public async Task<ToolResponse<CleanBicycleCounterFeatureCollection>> GetBicycleCountersAsync(
        string? latlng = null,
        string? range = null,
        string? limit = null,
        string? offset = null)
    {
        try
        {
            // Build query parameters
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(latlng))
                queryParams.Add($"latlng={Uri.EscapeDataString(latlng)}");

            if (!string.IsNullOrEmpty(range))
            {
                if (double.TryParse(range, out var rangeValue))
                    queryParams.Add($"range={rangeValue}");
                else
                    return new ToolResponse<CleanBicycleCounterFeatureCollection>
                    {
                        Success = false,
                        ErrorMessage = $"Invalid range value: {range}. Must be a valid number."
                    };
            }

            if (!string.IsNullOrEmpty(limit))
            {
                if (int.TryParse(limit, out var limitValue) && limitValue > 0)
                    queryParams.Add($"limit={limitValue}");
                else
                    return new ToolResponse<CleanBicycleCounterFeatureCollection>
                    {
                        Success = false,
                        ErrorMessage = $"Invalid limit value: {limit}. Must be a positive integer."
                    };
            }

            if (!string.IsNullOrEmpty(offset))
            {
                if (int.TryParse(offset, out var offsetValue) && offsetValue >= 0)
                    queryParams.Add($"offset={offsetValue}");
                else
                    return new ToolResponse<CleanBicycleCounterFeatureCollection>
                    {
                        Success = false,
                        ErrorMessage = $"Invalid offset value: {offset}. Must be a non-negative integer."
                    };
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var requestUrl = ApiUrl + queryString;

            _logger.Debug("Starting bicycle counters API request to {RequestUrl}", requestUrl);

            var apiToken = _configuration["GOLEMIO_API_TOKEN"] ?? _configuration["GolemioApiToken"];
            if (string.IsNullOrEmpty(apiToken))
            {
                _logger.Error("GOLEMIO_API_TOKEN environment variable or GolemioApiToken is not configured");
                return new ToolResponse<CleanBicycleCounterFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = "GOLEMIO_API_TOKEN environment variable is not configured"
                };
            }

            // Configure request headers
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("accept", "application/json; charset=utf-8");
            request.Headers.Add("x-access-token", apiToken);

            _logger.Debug("Sending HTTP request with headers configured");

            // Send request
            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("API request failed with status code {StatusCode}: {ReasonPhrase}",
                    response.StatusCode, response.ReasonPhrase);
                return new ToolResponse<CleanBicycleCounterFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = $"API request failed: {response.StatusCode} - {response.ReasonPhrase}"
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            _logger.Debug("Received response with {ContentLength} characters", jsonContent.Length);

            // Deserialize the response
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var featureCollection = JsonSerializer.Deserialize<BicycleCounterFeatureCollection>(jsonContent, options);

            if (featureCollection == null)
            {
                _logger.Warning("Failed to deserialize API response");
                return new ToolResponse<CleanBicycleCounterFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize API response"
                };
            }

            _logger.Information("Successfully retrieved {FeatureCount} bicycle counter features",
                featureCollection.Features.Count);

            // Convert to clean model without "type" properties
            var cleanFeatureCollection = new CleanBicycleCounterFeatureCollection
            {
                Features = featureCollection.Features.Select(f => new CleanBicycleCounterFeature
                {
                    Geometry = new CleanBicycleCounterGeometry
                    {
                        Coordinates = f.Geometry.Coordinates
                    },
                    Properties = f.Properties
                }).ToList()
            };

            _logger.Debug("Converted to clean model structure without type properties");
            return new ToolResponse<CleanBicycleCounterFeatureCollection>
            {
                Success = true,
                Data = cleanFeatureCollection
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP request failed while fetching bicycle counters");
            return new ToolResponse<CleanBicycleCounterFeatureCollection>
            {
                Success = false,
                ErrorMessage = $"HTTP request failed while fetching bicycle counters: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize bicycle counters response");
            return new ToolResponse<CleanBicycleCounterFeatureCollection>
            {
                Success = false,
                ErrorMessage = $"Failed to parse API response: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error occurred while fetching bicycle counters");
            return new ToolResponse<CleanBicycleCounterFeatureCollection>
            {
                Success = false,
                ErrorMessage = $"Unexpected error occurred while fetching bicycle counters: {ex.Message}"
            };
        }
    }
}
