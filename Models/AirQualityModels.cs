using System.Text.Json.Serialization;

namespace PragueMCP.Models;

/// <summary>
/// Generic response wrapper for all tool operations
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class ToolResponse<T>
{
    /// <summary>
    /// Indicates if the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Contains the actual data when successful, null when failed
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Contains error details when Success is false, null when successful
    /// </summary>
    public string? ErrorMessage { get; set; }
}

// Component Types (static lookup data)
public class AirQualityComponentType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("component_code")]
    public string ComponentCode { get; set; } = string.Empty;

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("description_cs")]
    public string DescriptionCs { get; set; } = string.Empty;

    [JsonPropertyName("description_en")]
    public string DescriptionEn { get; set; } = string.Empty;
}

// Index Types (static lookup data)
public class AirQualityIndexType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("index_code")]
    public string IndexCode { get; set; } = string.Empty;

    [JsonPropertyName("limit_gte")]
    public double? LimitGte { get; set; }

    [JsonPropertyName("limit_lt")]
    public double? LimitLt { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("color_text")]
    public string ColorText { get; set; } = string.Empty;

    [JsonPropertyName("description_cs")]
    public string DescriptionCs { get; set; } = string.Empty;

    [JsonPropertyName("description_en")]
    public string DescriptionEn { get; set; } = string.Empty;
}

// Air Quality Station Feature Collection
public class AirQualityStationFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("features")]
    public List<AirQualityStationFeature> Features { get; set; } = new();
}

public class AirQualityStationFeature
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("geometry")]
    public AirQualityStationGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public AirQualityStationProperties Properties { get; set; } = new();
}

public class AirQualityStationGeometry
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; } = new();
}

public class AirQualityStationProperties
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("district")]
    public string District { get; set; } = string.Empty;

    [JsonPropertyName("measurement")]
    public AirQualityMeasurement Measurement { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class AirQualityMeasurement
{
    [JsonPropertyName("AQ_hourly_index")]
    public string AqHourlyIndex { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<AirQualityComponent> Components { get; set; } = new();
}

public class AirQualityComponent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("averaged_time")]
    public AirQualityAveragedTime AveragedTime { get; set; } = new();
}

public class AirQualityAveragedTime
{
    [JsonPropertyName("averaged_hours")]
    public string AveragedHours { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

// Air Quality Station History
public class AirQualityStationHistory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("measurement")]
    public AirQualityMeasurement Measurement { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

// Clean models for output (without "type" properties)
public class CleanAirQualityStationFeatureCollection
{
    [JsonPropertyName("features")]
    public List<CleanAirQualityStationFeature> Features { get; set; } = new();
}

public class CleanAirQualityStationFeature
{
    [JsonPropertyName("geometry")]
    public CleanAirQualityStationGeometry Geometry { get; set; } = new();

    [JsonPropertyName("properties")]
    public EnrichedAirQualityStationProperties Properties { get; set; } = new();
}

public class CleanAirQualityStationGeometry
{
    [JsonPropertyName("coordinates")]
    public List<double> Coordinates { get; set; } = new();
}

// Enriched properties with lookup data
public class EnrichedAirQualityStationProperties
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("district")]
    public string District { get; set; } = string.Empty;

    [JsonPropertyName("measurement")]
    public EnrichedAirQualityMeasurement Measurement { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public class EnrichedAirQualityMeasurement
{
    [JsonPropertyName("AQ_hourly_index")]
    public string AqHourlyIndex { get; set; } = string.Empty;

    [JsonPropertyName("index_info")]
    public AirQualityIndexType? IndexInfo { get; set; }

    [JsonPropertyName("components")]
    public List<EnrichedAirQualityComponent> Components { get; set; } = new();
}

public class EnrichedAirQualityComponent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("component_info")]
    public AirQualityComponentType? ComponentInfo { get; set; }

    [JsonPropertyName("averaged_time")]
    public AirQualityAveragedTime AveragedTime { get; set; } = new();
}

// Enriched history model
public class EnrichedAirQualityStationHistory
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("measurement")]
    public EnrichedAirQualityMeasurement Measurement { get; set; } = new();

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
