using System.Text.Json.Serialization;

namespace PragueMCP.Models;

// Raw API response models
public class CityDistrictFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<CityDistrictFeature> Features { get; set; } = new();
}

public class CityDistrictFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public CityDistrictGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public CityDistrictProperties Properties { get; set; } = new();
}

public class CityDistrictGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public List<List<List<double>>> Coordinates { get; set; } = new();
}

public class CityDistrictProperties
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

// Processed response models (without type fields and with center coordinates)
public class CleanCityDistrictFeatureCollection
{
    public List<CleanCityDistrictFeature> Features { get; set; } = new();
}

public class CleanCityDistrictFeature
{
    public CleanCityDistrictGeometry Geometry { get; set; } = new();
    public CityDistrictProperties Properties { get; set; } = new();
}

public class CleanCityDistrictGeometry
{
    /// <summary>
    /// Center point coordinates calculated from the polygon coordinates [longitude, latitude]
    /// </summary>
    public double[] Coordinates { get; set; } = new double[2];
}
