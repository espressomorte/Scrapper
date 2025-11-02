using Microsoft.EntityFrameworkCore;
using Serilog;
using Scrapper.Services;
using Scrapper.Data;


Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

Log.Information("Hello, world! I'm Scrapper.");

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "metrics.db");
builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddHostedService<MetricsCollectorService>();
builder.Services.AddSingleton<IMetricsRepository, MetricsRepository>();
builder.Services.AddSingleton<IMetricsProcessor, MetricsProcessor>();
builder.Services.AddHostedService<MetricsCollectorService>();
var app = builder.Build();
app.Run();

internal interface IMetricProcessor
{
}

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();