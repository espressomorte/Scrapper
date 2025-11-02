using Microsoft.Extensions.Options;
using Serilog;

namespace Scrapper.Services
{
    public partial class MetricsCollectorService : BackgroundService
{
    private readonly HttpClient _httpClient = new();
    IMetricsProcessor _metricProcessor;

    public MetricsCollectorService(IMetricsProcessor metricProcessor) 
    {
        _metricProcessor = metricProcessor;

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
            var response = await _httpClient.GetAsync("http://localhost:9100/metrics");
            response.EnsureSuccessStatusCode();

            var metrics = await response.Content.ReadAsStringAsync();
            var timestamp = response.Headers.Date?.UtcDateTime ?? DateTime.UtcNow;
            var savedCount = await _metricProcessor.ProcessAndSaveMetricsAsync(metrics, timestamp);

            Log.Information("Successfully saved {Count} metrics.", savedCount);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while collecting or saving metrics.");
        }
    }
}

}
