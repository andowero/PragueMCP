using System.ComponentModel;
using ModelContextProtocol.Server;
using PragueMCP.Services;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Tools;

[McpServerToolType]
public class AirQualityStationTool
{
    private readonly IAirQualityService _airQualityService;
    private readonly Serilog.ILogger _logger;

    public AirQualityStationTool(IAirQualityService airQualityService)
    {
        _airQualityService = airQualityService;
        _logger = Log.ForContext<AirQualityStationTool>();
    }

    [McpServerTool]
    [Description("Retrieves current air quality station data from Prague (Golemio API). Returns enriched data with station locations, current measurements, air quality indices with descriptions, and component information including pollutant types, units, and descriptions in both Czech and English.")]
    public async Task<ToolResponse<CleanAirQualityStationFeatureCollection>> GetAirQualityStations(
        [Description("Sorting by location (Latitude and Longitude separated by comma, latitude first, e.g., '50.124935,14.457204'). Results will be sorted by distance from this point.")]
        string? latlng = null,

        [Description("Filter by distance from latlng in meters (range query). Depends on the latlng parameter. For example, 5000 for 5km radius.")]
        double? range = null,

        [Description("Filter by Prague city districts (slug) separated by comma. Examples: 'praha-1', 'praha-4,praha-6'.")]
        string? districts = null,

        [Description("Limits number of retrieved items. The maximum is 10000 (default value is 10).")]
        int limit = 10,

        [Description("Number of the first items that are skipped (for pagination).")]
        int offset = 0,

        [Description("Filters all results with older updated_at than this parameter. Expected format is ISO 8601 (e.g., '2019-05-18T07:38:37.000Z'). UTC timezone.")]
        string? updatedSince = null)
    {
        _logger.Debug("AirQualityStationTool.GetAirQualityStations method called");
        _logger.Information("Fetching Prague air quality station data with parameters: latlng={Latlng}, range={Range}, districts={Districts}, limit={Limit}, offset={Offset}, updatedSince={UpdatedSince}",
            latlng, range, districts, limit, offset, updatedSince);

        string[]? districtArray = null;
        if (!string.IsNullOrEmpty(districts))
        {
            districtArray = districts.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim()).ToArray();
        }

        DateTime? parsedUpdatedSince = null;
        if (!string.IsNullOrEmpty(updatedSince))
        {
            if (!DateTime.TryParse(updatedSince, out var parsedDate))
            {
                _logger.Error("Invalid updatedSince parameter format: {UpdatedSince}. Expected ISO 8601 format.", updatedSince);
                return new ToolResponse<CleanAirQualityStationFeatureCollection>
                {
                    Success = false,
                    ErrorMessage = $"Invalid updatedSince parameter format: '{updatedSince}'. Expected ISO 8601 format (e.g., '2019-05-18T07:38:37.000Z')."
                };
            }
            parsedUpdatedSince = parsedDate;
        }

        var result = await _airQualityService.GetAirQualityStationsAsync(
            latlng, range, districtArray, limit, offset, parsedUpdatedSince);

        if (result.Success)
        {
            _logger.Information("Successfully retrieved air quality station data with {StationCount} stations",
                result.Data!.Features.Count);
        }
        else
        {
            _logger.Warning("Air quality service returned error: {ErrorMessage}", result.ErrorMessage);
        }

        return result;
    }


}
