using System.ComponentModel;
using ModelContextProtocol.Server;
using PragueMCP.Services;
using PragueMCP.Models;
using Serilog;

namespace PragueMCP.Tools;

[McpServerToolType]
public class CityDistrictsTool
{
    private readonly ICityDistrictsService _cityDistrictsService;
    private readonly Serilog.ILogger _logger;

    public CityDistrictsTool(ICityDistrictsService cityDistrictsService)
    {
        _cityDistrictsService = cityDistrictsService;
        _logger = Log.ForContext<CityDistrictsTool>();
    }

    [McpServerTool]
    [Description("Retrieves all Prague city districts data from the Golemio API. Returns processed data with polygon center coordinates instead of full polygon data, and all 'type' fields removed from the response structure. The response is cached to minimize API calls.")]
    public async Task<ToolResponse<CleanCityDistrictFeatureCollection>> GetCityDistricts()
    {
        _logger.Debug("CityDistrictsTool.GetCityDistricts method called");
        _logger.Information("Fetching all Prague city districts data");

        // Always use limit=1000 and offset=0 as per requirements, no filtering
        var result = await _cityDistrictsService.GetCityDistrictsAsync(
            districts: null, limit: 1000, offset: 0);

        if (result.Success)
        {
            _logger.Information("Successfully retrieved city districts data with {DistrictCount} districts",
                result.Data!.Features.Count);
        }
        else
        {
            _logger.Warning("City districts service returned error: {ErrorMessage}", result.ErrorMessage);
        }

        return result;
    }
}
