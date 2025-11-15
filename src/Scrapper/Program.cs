using Microsoft.EntityFrameworkCore;
using Serilog;
using Scrapper.Services;
using Scrapper.Data;
using Microsoft.OpenApi.Models;

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
builder.Services.AddScoped<IDiscoveryService, DiscoveryService>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer(); // Необхідно для Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Scrapper API", Version = "v1" });
});
builder.Services.AddHttpClient();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Scrapper API V1");
        c.RoutePrefix = string.Empty; // Дозволяє доступ до UI за адресою http://localhost:PORT/
    });
}

//app.UseHttpsRedirection();
app.UseCors("AllowLocalhostUI");
app.MapControllers();

app.Run();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();