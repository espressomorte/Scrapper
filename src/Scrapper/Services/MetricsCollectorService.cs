using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scrapper.Data;
using Serilog;

namespace Scrapper.Services
{
    public partial class MetricsCollectorService : BackgroundService
    {

        private readonly HttpClient _httpClient = new();
        private readonly IMetricsProcessor _metricProcessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public MetricsCollectorService(
            IMetricsProcessor metricProcessor,
            IServiceScopeFactory scopeFactory)
        {
            _metricProcessor = metricProcessor;
            _scopeFactory = scopeFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CollectAndSaveMetrics();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task CollectAndSaveMetrics()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

                var activeNodes = await db.NodeExporterSettings
                .Where(n => n.IsEnabled)
                .ToListAsync();

                if (!activeNodes.Any())
                {
                    Log.Information("No active NodeExporterSettings found. Skipping metrics collection.");
                    return;
                }

                foreach (var node in activeNodes)
                {
                    try
                    {
                        var response = await _httpClient.GetAsync(node.Url);
                        response.EnsureSuccessStatusCode();

                        var metrics = await response.Content.ReadAsStringAsync();
                        var timestamp = response.Headers.Date?.UtcDateTime ?? DateTime.UtcNow;
                        var savedCount = await _metricProcessor.ProcessAndSaveMetricsAsync(metrics, timestamp, node.Id);

                        Log.Information("Saved {Count} metrics for Node {NodeName} ({Url}).", savedCount, node.Name, node.Url);
                    }
                    catch (Exception exNode)
                    {
                        Log.Error(exNode, "Error collecting metrics from Node {NodeName} ({Url})", node.Name, node.Url);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while collecting or saving metrics.");
            }
        }
        // try
        // {
        //     var response = await _httpClient.GetAsync("http://localhost:9100/metrics");
        //     response.EnsureSuccessStatusCode();

        //     var metrics = await response.Content.ReadAsStringAsync();
        //     var timestamp = response.Headers.Date?.UtcDateTime ?? DateTime.UtcNow;
        //     var savedCount = await _metricProcessor.ProcessAndSaveMetricsAsync(metrics, timestamp);

        //     Log.Information("Successfully saved {Count} metrics.", savedCount);
        // }
        // catch (Exception ex)
        // {
        //     Log.Error(ex, "Error occurred while collecting or saving metrics.");
        // }
    }
}


