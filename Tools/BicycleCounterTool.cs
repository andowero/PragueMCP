using System.ComponentModel;
using ModelContextProtocol.Server;
using PragueMCP.Services;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Tools;

[McpServerToolType]
public class BicycleCounterTool
{
    private readonly IBicycleCounterService _bicycleCounterService;
    private readonly Serilog.ILogger _logger;

    public BicycleCounterTool(IBicycleCounterService bicycleCounterService)
    {
        _bicycleCounterService = bicycleCounterService;
        _logger = Log.ForContext<BicycleCounterTool>();
    }

    [McpServerTool]
    [Description("Retrieves current bicycle counter locations and data from Prague (Golemio API). Returns GeoJSON-like structure with counter positions, names, routes, directions, and latest count data for all bicycle counting stations in Prague.")]
    public async Task<ToolResponse<CleanBicycleCounterFeatureCollection>> GetBicycleCounters(
        [Description("Geographic coordinates for location-based sorting and filtering (latitude,longitude separated by comma, latitude first). Example: '50.124935,14.457204'. Keep empty for all counters.")]
        string? latlng = null,

        [Description("Distance range in meters for filtering results around the latlng coordinates. Requires latlng parameter. Example: '5000' for 5km radius. Keep empty for all counters.")]
        string? range = null,

        [Description("Maximum number of results to return. Must be a positive integer. Maximum is 10000. Example: '10'. Keep empty for all counters.")]
        string? limit = null,

        [Description("Number of results to skip for pagination. Must be a non-negative integer. Example: '0'")]
        string? offset = null)
    {
        _logger.Debug("BicycleCounterTool.GetBicycleCounters method called with parameters: latlng={Latlng}, range={Range}, limit={Limit}, offset={Offset}",
            latlng, range, limit, offset);
        _logger.Information("Fetching Prague bicycle counter data");

        var result = await _bicycleCounterService.GetBicycleCountersAsync(latlng, range, limit, offset);

        if (result.Success)
        {
            _logger.Information("Successfully retrieved bicycle counter data with {FeatureCount} locations",
                result.Data!.Features.Count);
        }
        else
        {
            _logger.Warning("Bicycle counter service returned error: {ErrorMessage}", result.ErrorMessage);
        }

        return result;
    }
}
