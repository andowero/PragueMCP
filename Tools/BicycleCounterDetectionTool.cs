using System.ComponentModel;
using ModelContextProtocol.Server;
using PragueMCP.Services;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Tools;

[McpServerToolType]
public class BicycleCounterDetectionTool
{
    private readonly IBicycleCounterDetectionService _bicycleCounterDetectionService;
    private readonly Serilog.ILogger _logger;

    public BicycleCounterDetectionTool(IBicycleCounterDetectionService bicycleCounterDetectionService)
    {
        _bicycleCounterDetectionService = bicycleCounterDetectionService;
        _logger = Log.ForContext<BicycleCounterDetectionTool>();
    }

    [McpServerTool]
    [Description("Retrieves bicycle counter detection measurements from Prague (Golemio API). Returns aggregated detection data including bicycle counts, pedestrian counts (if available), measurement periods, and location information. If no time range is specified, defaults to the last 24 hours.")]
    public async Task<ToolResponse<List<BicycleCounterDetection>>> GetBicycleCounterDetections(
        [Description("Single bicycle counter direction ID to filter results (e.g., 'camea-BC_ZA-BO'). Note: This parameter accepts only one direction ID, not multiple IDs. It must be the direction ID, not the counter ID. To get available direction IDs, use the get_bicycle_counters tool. To get measurements from the whole counter, you must run the tool separately for each direction. This parameter is mandatory.")]
        string directionId,

        [Description("Date in ISO8601 format, limits data measured from this datetime (e.g., '2020-03-13T10:54:00.000Z' or '2024-01-15'). If not provided, defaults to 24 hours ago.")]
        string? from = null,

        [Description("Date in ISO8601 format, limits data measured up until this datetime (e.g., '2020-03-15T13:05:00.000Z' or '2024-01-15'). If not provided, defaults to current time.")]
        string? to = null)
    {
        _logger.Debug("BicycleCounterDetectionTool.GetBicycleCounterDetections method called");

        // Validate required parameter
        if (string.IsNullOrWhiteSpace(directionId))
        {
            return new ToolResponse<List<BicycleCounterDetection>>
            {
                Success = false,
                ErrorMessage = "Direction ID is required and cannot be null or empty."
            };
        }

        _logger.Information("Fetching Prague bicycle counter detection data with parameters: directionId={DirectionId}, from={From}, to={To} (fixed: limit=10, offset=0, aggregate=true)",
            directionId, from, to);

        var result = await _bicycleCounterDetectionService.GetBicycleCounterDetectionsAsync(
            directionId, from, to);

        if (result.Success)
        {
            _logger.Information("Successfully retrieved bicycle counter detection data with {DetectionCount} measurements",
                result.Data!.Count);
        }
        else
        {
            _logger.Warning("Bicycle counter detection service returned error: {ErrorMessage}", result.ErrorMessage);
        }

        return result;
    }
}
