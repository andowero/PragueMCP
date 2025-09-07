using System.Text.Json.Serialization;

namespace PragueMCP.Models;

public class BicycleCounterFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<BicycleCounterFeature> Features { get; set; } = new();
}

public class BicycleCounterFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public BicycleCounterGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public BicycleCounterProperties Properties { get; set; } = new();
}

public class BicycleCounterGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; } = new();
}

public class BicycleCounterProperties
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("route")]
    public string Route { get; set; } = string.Empty;

    [JsonPropertyName("directions")]
    public List<BicycleCounterDirection> Directions { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class BicycleCounterDirection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("last_count")]
    public int? LastCount { get; set; }

    [JsonPropertyName("last_count_at")]
    public DateTime? LastCountAt { get; set; }
}

// Response models without "type" properties for clean output
public class CleanBicycleCounterFeatureCollection
{
    [JsonPropertyName("features")]
    public List<CleanBicycleCounterFeature> Features { get; set; } = new();
}

public class CleanBicycleCounterFeature
{
    [JsonPropertyName("geometry")]
    public CleanBicycleCounterGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public BicycleCounterProperties Properties { get; set; } = new();
}

public class CleanBicycleCounterGeometry
{
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; } = new();
}

// Models for bicycle counter detections
public class BicycleCounterDetection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public int Value { get; set; }

    [JsonPropertyName("value_pedestrians")]
    public int? ValuePedestrians { get; set; }

    [JsonPropertyName("locations_id")]
    public string LocationsId { get; set; } = string.Empty;

    [JsonPropertyName("measured_from")]
    public DateTime MeasuredFrom { get; set; }

    [JsonPropertyName("measured_to")]
    public DateTime MeasuredTo { get; set; }

    [JsonPropertyName("measurement_count")]
    public string MeasurementCount { get; set; } = string.Empty;
}
