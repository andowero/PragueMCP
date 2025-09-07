using System.ComponentModel;
using ModelContextProtocol.Server;
using PragueMCP.Services;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Tools;

[McpServerToolType]
public class AirQualityHistoryTool
{
    private readonly IAirQualityService _airQualityService;
    private readonly Serilog.ILogger _logger;

    public AirQualityHistoryTool(IAirQualityService airQualityService)
    {
        _airQualityService = airQualityService;
        _logger = Log.ForContext<AirQualityHistoryTool>();
    }

    [McpServerTool]
    [Description("Retrieves historical air quality station measurements from Prague (Golemio API). Returns enriched historical data with air quality measurements, indices with descriptions, and component information including pollutant types, units, and descriptions. If no time range is specified, defaults to the last 24 hours.")]
    public async Task<ToolResponse<List<EnrichedAirQualityStationHistory>>> GetAirQualityStationsHistory(
        [Description("Limits number of retrieved items. The maximum is 10000 (default value is 10).")]
        int limit = 10,

        [Description("Number of the first items that are skipped (for pagination).")]
        int offset = 0,

        [Description("Limits data measured from this datetime. Expected format is ISO 8601 (e.g., '2019-05-16T04:27:58.000Z'). UTC timezone. If not provided, defaults to 24 hours ago.")]
        string? from = null,

        [Description("Limits data measured up until this datetime. Expected format is ISO 8601 (e.g., '2019-05-18T04:27:58.000Z'). UTC timezone. If not provided, defaults to current time.")]
        string? to = null,

        [Description("Limits data measured by sensor with this ID (e.g., 'ACHOA'). Use this to get historical data for a specific air quality station.")]
        string? sensorId = null)
    {
        _logger.Debug("AirQualityHistoryTool.GetAirQualityStationsHistory method called");
        _logger.Information("Fetching Prague air quality station history data with parameters: limit={Limit}, offset={Offset}, from={From}, to={To}, sensorId={SensorId}",
            limit, offset, from, to, sensorId);

        DateTime? parsedFrom = null;
        if (!string.IsNullOrEmpty(from))
        {
            if (!DateTime.TryParse(from, out var parsedFromDate))
            {
                _logger.Error("Invalid from parameter format: {From}. Expected ISO 8601 format.", from);
                return new ToolResponse<List<EnrichedAirQualityStationHistory>>
                {
                    Success = false,
                    ErrorMessage = $"Invalid from parameter format: '{from}'. Expected ISO 8601 format (e.g., '2019-05-16T04:27:58.000Z')."
                };
            }
            parsedFrom = parsedFromDate;
        }

        DateTime? parsedTo = null;
        if (!string.IsNullOrEmpty(to))
        {
            if (!DateTime.TryParse(to, out var parsedToDate))
            {
                _logger.Error("Invalid to parameter format: {To}. Expected ISO 8601 format.", to);
                return new ToolResponse<List<EnrichedAirQualityStationHistory>>
                {
                    Success = false,
                    ErrorMessage = $"Invalid to parameter format: '{to}'. Expected ISO 8601 format (e.g., '2019-05-18T04:27:58.000Z')."
                };
            }
            parsedTo = parsedToDate;
        }

        var result = await _airQualityService.GetAirQualityStationsHistoryAsync(
            limit, offset, parsedFrom, parsedTo, sensorId);

        if (result.Success)
        {
            _logger.Information("Successfully retrieved air quality station history data with {HistoryCount} records",
                result.Data!.Count);
        }
        else
        {
            _logger.Warning("Air quality service returned error for history data: {ErrorMessage}", result.ErrorMessage);
        }

        return result;
    }
}
