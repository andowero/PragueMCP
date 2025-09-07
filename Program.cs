using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Serilog;
using PragueMCP.Services;
using PragueMCP.Tools;

var builder = WebApplication.CreateBuilder(args);

// Ensure configuration files are loaded from the application's base directory
// This fixes issues when running from different working directories
builder.Configuration.SetBasePath(AppContext.BaseDirectory);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Clear all existing logging providers to ensure no console output
builder.Logging.ClearProviders();

// Configure Serilog for file-only logging with all log levels
// Get the application's base directory (where the .csproj file is located)
var baseDirectory = AppContext.BaseDirectory;
var logsDirectory = Path.Combine(baseDirectory, "logs");
var logFilePath = Path.Combine(logsDirectory, "application-.log");

// Ensure the logs directory exists
Directory.CreateDirectory(logsDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  // Capture all log levels from Debug and above
    .WriteTo.File(
        path: logFilePath,  // Absolute log file path with date rolling
        rollingInterval: RollingInterval.Day,  // Create new file daily
        retainedFileCountLimit: 30,  // Keep 30 days of logs
        fileSizeLimitBytes: 10_000_000,  // 10MB file size limit
        rollOnFileSizeLimit: true,  // Roll to new file when size limit reached
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Add Serilog to the host builder
builder.Services.AddSerilog();

// Test all log levels to verify file logging is working
Log.Information("Logging configured. Log files will be written to: {LogsDirectory}", logsDirectory);
Log.Debug("Application starting - Debug level log");
Log.Information("Application starting - Information level log");
Log.Warning("Application starting - Warning level log");
Log.Error("Application starting - Error level log");
Log.Fatal("Application starting - Critical/Fatal level log");

// Register HTTP client factory
builder.Services.AddHttpClient<IBicycleCounterService, BicycleCounterService>();
builder.Services.AddHttpClient<IBicycleCounterDetectionService, BicycleCounterDetectionService>();
builder.Services.AddHttpClient<IAirQualityService, AirQualityService>();
builder.Services.AddHttpClient<ICityDistrictsService, CityDistrictsService>();

// Add memory cache for air quality lookup data
builder.Services.AddMemoryCache();

// Register services
builder.Services.AddScoped<IBicycleCounterService, BicycleCounterService>();
builder.Services.AddScoped<IBicycleCounterDetectionService, BicycleCounterDetectionService>();
builder.Services.AddScoped<IAirQualityService, AirQualityService>();
builder.Services.AddScoped<ICityDistrictsService, CityDistrictsService>();
builder.Services.AddScoped<BicycleCounterTool>();
builder.Services.AddScoped<BicycleCounterDetectionTool>();
builder.Services.AddScoped<AirQualityStationTool>();
builder.Services.AddScoped<AirQualityHistoryTool>();
builder.Services.AddScoped<CityDistrictsTool>();

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

try
{
    var app = builder.Build();

    // Configure CORS first
    app.UseCors();

    // Map MCP endpoint with specific route
    app.MapMcp("/api/mcp");

    await app.RunAsync();
}
finally
{
    // Ensure all logs are flushed before application exits
    Log.CloseAndFlush();
}

