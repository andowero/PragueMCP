using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Services;

public interface IBicycleCounterDetectionService
{
    Task<ToolResponse<List<BicycleCounterDetection>>> GetBicycleCounterDetectionsAsync(
        string directionId,
        string? from = null,
        string? to = null);
}

public class BicycleCounterDetectionService : IBicycleCounterDetectionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly Serilog.ILogger _logger;
    private const string ApiUrl = "https://api.golemio.cz/v2/bicyclecounters/detections";

    public BicycleCounterDetectionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = Log.ForContext<BicycleCounterDetectionService>();
    }

    public async Task<ToolResponse<List<BicycleCounterDetection>>> GetBicycleCounterDetectionsAsync(
        string directionId,
        string? from = null,
        string? to = null)
    {
        try
        {
            // Validate required parameter
            if (string.IsNullOrWhiteSpace(directionId))
            {
                return new ToolResponse<List<BicycleCounterDetection>>
                {
                    Success = false,
                    ErrorMessage = "Direction ID is required and cannot be null or empty."
                };
            }

            // Parse date parameters with validation
            DateTime fromTime;
            DateTime toTime;

            if (!string.IsNullOrWhiteSpace(from))
            {
                if (!DateTime.TryParse(from, null, System.Globalization.DateTimeStyles.RoundtripKind, out fromTime))
                {
                    return new ToolResponse<List<BicycleCounterDetection>>
                    {
                        Success = false,
                        ErrorMessage = $"Invalid 'from' date format: '{from}'. Expected ISO 8601 format (e.g., '2024-01-15T10:30:00Z' or '2024-01-15')."
                    };
                }
            }
            else
            {
                fromTime = DateTime.UtcNow.AddDays(-1);
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                if (!DateTime.TryParse(to, null, System.Globalization.DateTimeStyles.RoundtripKind, out toTime))
                {
                    return new ToolResponse<List<BicycleCounterDetection>>
                    {
                        Success = false,
                        ErrorMessage = $"Invalid 'to' date format: '{to}'. Expected ISO 8601 format (e.g., '2024-01-15T10:30:00Z' or '2024-01-15')."
                    };
                }
            }
            else
            {
                toTime = DateTime.UtcNow;
            }
            
            var queryParams = new List<string>
            {
                "limit=10",
                "offset=0",
                "aggregate=true",
                $"from={fromTime:yyyy-MM-ddTHH:mm:ss.fffZ}",
                $"to={toTime:yyyy-MM-ddTHH:mm:ss.fffZ}",
                $"id={directionId}"
            };

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var requestUrl = ApiUrl + queryString;

            _logger.Debug("Starting bicycle counter detections API request to {RequestUrl}", requestUrl);

            var apiToken = _configuration["GolemioApiToken"];
            if (string.IsNullOrEmpty(apiToken))
            {
                _logger.Error("GolemioApiToken is not configured in appsettings.json");
                return new ToolResponse<List<BicycleCounterDetection>>
                {
                    Success = false,
                    ErrorMessage = "GolemioApiToken is not configured"
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
                return new ToolResponse<List<BicycleCounterDetection>>
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

            var detections = JsonSerializer.Deserialize<List<BicycleCounterDetection>>(jsonContent, options);

            if (detections == null)
            {
                _logger.Warning("Failed to deserialize API response");
                return new ToolResponse<List<BicycleCounterDetection>>
                {
                    Success = false,
                    ErrorMessage = "Failed to deserialize API response"
                };
            }

            _logger.Information("Successfully retrieved {DetectionCount} bicycle counter detections",
                detections.Count);

            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = true,
                Data = detections
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.Error(ex, "HTTP request failed while fetching bicycle counter detections");
            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = false,
                ErrorMessage = $"HTTP request failed while fetching bicycle counter detections: {ex.Message}"
            };
        }
        catch (JsonException ex)
        {
            _logger.Error(ex, "Failed to deserialize bicycle counter detections response");
            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = false,
                ErrorMessage = $"Failed to parse API response: {ex.Message}"
            };
        }
        catch (ArgumentException ex)
        {
            _logger.Error(ex, "Invalid parameter provided for bicycle counter detections request");
            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = false,
                ErrorMessage = $"Invalid parameter provided: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected error occurred while fetching bicycle counter detections");
            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = false,
                ErrorMessage = $"Unexpected error occurred while fetching bicycle counter detections: {ex.Message}"
            };
        }
    }
}
