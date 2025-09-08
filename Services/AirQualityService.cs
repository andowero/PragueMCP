using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Services;

public interface IAirQualityService
{
    Task<ToolResponse<CleanAirQualityStationFeatureCollection>> GetAirQualityStationsAsync(
        string? latlng = null,
        double? range = null,
        string[]? districts = null,
        int limit = 10,
        int offset = 0,
        DateTime? updatedSince = null);

    Task<ToolResponse<List<EnrichedAirQualityStationHistory>>> GetAirQualityStationsHistoryAsync(
        int limit = 10,
        int offset = 0,
        DateTime? from = null,
        DateTime? to = null,
        string? sensorId = null);

    Task<ToolResponse<List<AirQualityComponentType>>> GetComponentTypesAsync();
    Task<ToolResponse<List<AirQualityIndexType>>> GetIndexTypesAsync();
}

public class AirQualityService : IAirQualityService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly Serilog.ILogger _logger;
    
    private const string BaseApiUrl = "https://api.golemio.cz/v2/airqualitystations";
    private const string ComponentTypesCacheKey = "air_quality_component_types";
    private const string IndexTypesCacheKey = "air_quality_index_types";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24); // Cache lookup data for 24 hours

    public AirQualityService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = Log.ForContext<AirQualityService>();
    }

    public async Task<ToolResponse<CleanAirQualityStationFeatureCollection>> GetAirQualityStationsAsync(
        string? latlng = null,
        double? range = null,
        string[]? districts = null,
        int limit = 10,
        int offset = 0,
        DateTime? updatedSince = null)
    {
        try
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(latlng))
                queryParams.Add($"latlng={latlng}");

            if (range.HasValue)
                queryParams.Add($"range={range.Value}");

            if (districts != null && districts.Length > 0)
                queryParams.Add($"districts={string.Join(",", districts)}");

            queryParams.Add($"limit={limit}");
            queryParams.Add($"offset={offset}");

            if (updatedSince.HasValue)
                queryParams.Add($"updatedSince={updatedSince.Value:yyyy-MM-ddTHH:mm:ss.fffZ}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var requestUrl = BaseApiUrl + queryString;

            _logger.Debug("Starting air quality stations API request to {RequestUrl}", requestUrl);

            var response = await SendApiRequestAsync(requestUrl);
            if (response.Success == false)
            {
                return new ToolResponse<CleanAirQualityStationFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var stationCollection = JsonSerializer.Deserialize<AirQualityStationFeatureCollection>(response.Data!, options);

            if (stationCollection == null)
            {
                _logger.Warning("Failed to deserialize air quality stations response");
                return new ToolResponse<CleanAirQualityStationFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize air quality stations response"
                };
            }

            // Enrich the data with lookup information
            var enrichedCollectionResult = await EnrichStationDataAsync(stationCollection);
            if (enrichedCollectionResult.Success == false)
            {
                return new ToolResponse<CleanAirQualityStationFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = enrichedCollectionResult.ErrorMessage
                };
            }

            _logger.Information("Successfully retrieved {StationCount} air quality stations",
                enrichedCollectionResult.Data!.Features.Count);

            return new ToolResponse<CleanAirQualityStationFeatureCollection>
            {
                Success = true,
                Data = enrichedCollectionResult.Data
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while fetching air quality stations");
            return new ToolResponse<CleanAirQualityStationFeatureCollection>
            {
                Success = false,
                ErrorMessage = $"Error occurred while fetching air quality stations: {ex.Message}"
            };
        }
    }

    public async Task<ToolResponse<List<EnrichedAirQualityStationHistory>>> GetAirQualityStationsHistoryAsync(
        int limit = 10,
        int offset = 0,
        DateTime? from = null,
        DateTime? to = null,
        string? sensorId = null)
    {
        try
        {
            var queryParams = new List<string>();

            queryParams.Add($"limit={limit}");
            queryParams.Add($"offset={offset}");

            // If no time range is provided, default to the last 24 hours
            var fromTime = from ?? DateTime.UtcNow.AddDays(-1);
            var toTime = to ?? DateTime.UtcNow;

            queryParams.Add($"from={fromTime:yyyy-MM-ddTHH:mm:ss.fffZ}");
            queryParams.Add($"to={toTime:yyyy-MM-ddTHH:mm:ss.fffZ}");

            if (!string.IsNullOrEmpty(sensorId))
                queryParams.Add($"sensorId={sensorId}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var requestUrl = BaseApiUrl + "/history" + queryString;

            _logger.Debug("Starting air quality stations history API request to {RequestUrl}", requestUrl);

            var response = await SendApiRequestAsync(requestUrl);
            if (response.Success == false)
            {
                return new ToolResponse<List<EnrichedAirQualityStationHistory>>
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var historyData = JsonSerializer.Deserialize<List<AirQualityStationHistory>>(response.Data!, options);

            if (historyData == null)
            {
                _logger.Warning("Failed to deserialize air quality stations history response");
                return new ToolResponse<List<EnrichedAirQualityStationHistory>>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize air quality stations history response"
                };
            }

            // Enrich the data with lookup information
            var enrichedHistoryResult = await EnrichHistoryDataAsync(historyData);
            if (enrichedHistoryResult.Success == false)
            {
                return new ToolResponse<List<EnrichedAirQualityStationHistory>>
                {
                    Success = false,
                    ErrorMessage = enrichedHistoryResult.ErrorMessage
                };
            }

            _logger.Information("Successfully retrieved {HistoryCount} air quality station history records",
                enrichedHistoryResult.Data!.Count);

            return new ToolResponse<List<EnrichedAirQualityStationHistory>>
            {
                Success = true,
                Data = enrichedHistoryResult.Data
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while fetching air quality stations history");
            return new ToolResponse<List<EnrichedAirQualityStationHistory>>
            {
                Success = false,
                ErrorMessage = $"Error occurred while fetching air quality stations history: {ex.Message}"
            };
        }
    }

    public async Task<ToolResponse<List<AirQualityComponentType>>> GetComponentTypesAsync()
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(ComponentTypesCacheKey, out List<AirQualityComponentType>? cachedTypes))
            {
                _logger.Debug("Retrieved component types from cache");
                return new ToolResponse<List<AirQualityComponentType>>
                {
                    Success = true,
                    Data = cachedTypes
                };
            }

            var requestUrl = BaseApiUrl + "/componenttypes";
            _logger.Debug("Starting component types API request to {RequestUrl}", requestUrl);

            var response = await SendApiRequestAsync(requestUrl);
            if (response.Success == false)
            {
                return new ToolResponse<List<AirQualityComponentType>>
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var componentTypes = JsonSerializer.Deserialize<List<AirQualityComponentType>>(response.Data!, options);

            if (componentTypes == null)
            {
                return new ToolResponse<List<AirQualityComponentType>>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize component types response"
                };
            }

            // Cache the result
            _cache.Set(ComponentTypesCacheKey, componentTypes, CacheExpiration);
            _logger.Information("Successfully retrieved and cached {ComponentTypeCount} component types",
                componentTypes.Count);

            return new ToolResponse<List<AirQualityComponentType>>
            {
                Success = true,
                Data = componentTypes
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while fetching component types");
            return new ToolResponse<List<AirQualityComponentType>>
            {
                Success = false,
                ErrorMessage = $"Error occurred while fetching component types: {ex.Message}"
            };
        }
    }

    public async Task<ToolResponse<List<AirQualityIndexType>>> GetIndexTypesAsync()
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(IndexTypesCacheKey, out List<AirQualityIndexType>? cachedTypes))
            {
                _logger.Debug("Retrieved index types from cache");
                return new ToolResponse<List<AirQualityIndexType>>
                {
                    Success = true,
                    Data = cachedTypes
                };
            }

            var requestUrl = BaseApiUrl + "/indextypes";
            _logger.Debug("Starting index types API request to {RequestUrl}", requestUrl);

            var response = await SendApiRequestAsync(requestUrl);
            if (response.Success == false)
            {
                return new ToolResponse<List<AirQualityIndexType>>
                {
                    Success = false,
                    ErrorMessage = response.ErrorMessage
                };
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var indexTypes = JsonSerializer.Deserialize<List<AirQualityIndexType>>(response.Data!, options);

            if (indexTypes == null)
            {
                return new ToolResponse<List<AirQualityIndexType>>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize index types response"
                };
            }

            // Cache the result
            _cache.Set(IndexTypesCacheKey, indexTypes, CacheExpiration);
            _logger.Information("Successfully retrieved and cached {IndexTypeCount} index types",
                indexTypes.Count);

            return new ToolResponse<List<AirQualityIndexType>>
            {
                Success = true,
                Data = indexTypes
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error occurred while fetching index types");
            return new ToolResponse<List<AirQualityIndexType>>
            {
                Success = false,
                ErrorMessage = $"Error occurred while fetching index types: {ex.Message}"
            };
        }
    }

    private async Task<ToolResponse<string>> SendApiRequestAsync(string requestUrl)
    {
        try
        {
            var apiToken = _configuration["GOLEMIO_API_TOKEN"] ?? _configuration["GolemioApiToken"];
            if (string.IsNullOrEmpty(apiToken))
            {
                _logger.Error("GOLEMIO_API_TOKEN environment variable or GolemioApiToken is not configured");
                return new ToolResponse<string>
                {
                    Success = false,
                    ErrorMessage = "GOLEMIO_API_TOKEN environment variable is not configured"
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

    private async Task<ToolResponse<CleanAirQualityStationFeatureCollection>> EnrichStationDataAsync(
        AirQualityStationFeatureCollection stationCollection)
    {
        var componentTypesResult = await GetComponentTypesAsync();
        if (componentTypesResult.Success == false)
        {
            return new ToolResponse<CleanAirQualityStationFeatureCollection>
            {
                Success = false,
                ErrorMessage = componentTypesResult.ErrorMessage
            };
        }

        var indexTypesResult = await GetIndexTypesAsync();
        if (indexTypesResult.Success == false)
        {
            return new ToolResponse<CleanAirQualityStationFeatureCollection>
            {
                Success = false,
                ErrorMessage = indexTypesResult.ErrorMessage
            };
        }

        var componentTypesDict = componentTypesResult.Data?.ToDictionary(ct => ct.ComponentCode, ct => ct) ?? new Dictionary<string, AirQualityComponentType>();
        var indexTypesDict = indexTypesResult.Data?.ToDictionary(it => it.IndexCode, it => it) ?? new Dictionary<string, AirQualityIndexType>();

        var enrichedCollection = new CleanAirQualityStationFeatureCollection
        {
            Features = stationCollection.Features.Select(feature => new CleanAirQualityStationFeature
            {
                Geometry = new CleanAirQualityStationGeometry
                {
                    Coordinates = feature.Geometry.Coordinates
                },
                Properties = new EnrichedAirQualityStationProperties
                {
                    Id = feature.Properties.Id,
                    Name = feature.Properties.Name,
                    District = feature.Properties.District,
                    UpdatedAt = feature.Properties.UpdatedAt,
                    Measurement = new EnrichedAirQualityMeasurement
                    {
                        AqHourlyIndex = feature.Properties.Measurement.AqHourlyIndex,
                        IndexInfo = indexTypesDict.GetValueOrDefault(feature.Properties.Measurement.AqHourlyIndex),
                        Components = feature.Properties.Measurement.Components.Select(component => new EnrichedAirQualityComponent
                        {
                            Type = component.Type,
                            ComponentInfo = componentTypesDict.GetValueOrDefault(component.Type),
                            AveragedTime = component.AveragedTime
                        }).ToList()
                    }
                }
            }).ToList()
        };

        return new ToolResponse<CleanAirQualityStationFeatureCollection>
        {
            Success = true,
            Data = enrichedCollection
        };
    }

    private async Task<ToolResponse<List<EnrichedAirQualityStationHistory>>> EnrichHistoryDataAsync(
        List<AirQualityStationHistory> historyData)
    {
        var componentTypesResult = await GetComponentTypesAsync();
        if (componentTypesResult.Success == false)
        {
            return new ToolResponse<List<EnrichedAirQualityStationHistory>>
            {
                Success = false,
                ErrorMessage = componentTypesResult.ErrorMessage
            };
        }

        var indexTypesResult = await GetIndexTypesAsync();
        if (indexTypesResult.Success == false)
        {
            return new ToolResponse<List<EnrichedAirQualityStationHistory>>
            {
                Success = false,
                ErrorMessage = indexTypesResult.ErrorMessage
            };
        }

        var componentTypesDict = componentTypesResult.Data?.ToDictionary(ct => ct.ComponentCode, ct => ct) ?? new Dictionary<string, AirQualityComponentType>();
        var indexTypesDict = indexTypesResult.Data?.ToDictionary(it => it.IndexCode, it => it) ?? new Dictionary<string, AirQualityIndexType>();

        var enrichedHistory = historyData.Select(history => new EnrichedAirQualityStationHistory
        {
            Id = history.Id,
            UpdatedAt = history.UpdatedAt,
            Measurement = new EnrichedAirQualityMeasurement
            {
                AqHourlyIndex = history.Measurement.AqHourlyIndex,
                IndexInfo = indexTypesDict.GetValueOrDefault(history.Measurement.AqHourlyIndex),
                Components = history.Measurement.Components.Select(component => new EnrichedAirQualityComponent
                {
                    Type = component.Type,
                    ComponentInfo = componentTypesDict.GetValueOrDefault(component.Type),
                    AveragedTime = component.AveragedTime
                }).ToList()
            }
        }).ToList();

        return new ToolResponse<List<EnrichedAirQualityStationHistory>>
        {
            Success = true,
            Data = enrichedHistory
        };
    }
}
