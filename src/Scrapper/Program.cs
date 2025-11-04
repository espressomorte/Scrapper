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
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "metrics.db");
builder.Services.AddDbContext<MetricsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddHostedService<MetricsCollectorService>();
builder.Services.AddSingleton<IMetricsRepository, MetricsRepository>();
builder.Services.AddSingleton<IMetricsProcessor, MetricsProcessor>();
builder.Services.AddControllers(); 

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowLocalhostUI",
                      policy =>
                      {
                        policy.AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                      });
});
var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowLocalhostUI"); 
app.MapControllers();

app.Run();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();