using Serilog;

public partial class MetricsCollectorService : BackgroundService
{
    private readonly HttpClient _httpClient = new();

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string[] _prefixes = { "node_network_", "node_netstat_", "node_sockstat_" };

    public MetricsCollectorService(IServiceScopeFactory scopeFactory)
    {
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

    private async Task NotCollectAndSaveMetrics()
    {
        var response = await _httpClient.GetAsync("http://localhost:9100/metrics");
        var timestamp = response.Headers.Date;
        var metrics = await response.Content.ReadAsStreamAsync();

        //metrics.
    }

    private async Task CollectAndSaveMetrics()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

        var response = await _httpClient.GetAsync("http://localhost:9100/metrics");
        var timestamp = response.Headers.Date;
        var metrics = await response.Content.ReadAsStringAsync();

        var filtered = metrics
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => _prefixes.Any(p => line.StartsWith(p)))
            .Where(line => !line.StartsWith("#")) // skip HELP/TYPE
            .ToList();

        
        Log.Information("Saving {Count} metrics", filtered.Count);

        var lineCounter = 1;
        foreach (var line in filtered)
        {
            try
            {
                var metric = ParseMetricLine(line);
                db.NetworkMetrics.Add(metric);
            }
            catch (MetricParseException)
            {
                Log.Warning("Cannot parse metric line at {line}", lineCounter++);
            }
        }

        var savedCount = await db.SaveChangesAsync();
        Log.Information("Saved {savedCount} metrics", savedCount);
    }

    private Metric ParseMetricLine(string line)
    {
        var parts = line.Split(' ');
        if (parts.Length != 2)
        {
            throw new MetricParseException("Line doen't have two parts when split by space");
        }

        var nameAndLabels = parts[0];
        var value = double.Parse(parts[1]);

        var name = nameAndLabels.Split('{')[0];

        var device = "unknown";
        var labelStart = nameAndLabels.IndexOf('{');
        if (labelStart > 0)
        {
            var labels = nameAndLabels.Substring(labelStart + 1, nameAndLabels.Length - labelStart - 2);
            var devPair = labels.Split(',').FirstOrDefault(l => l.StartsWith("device="));
            if (devPair != null)
                device = devPair.Split('"')[1];
        }
        else
        {
            throw new MetricParseException();
        }

        return new Metric
        {
            Timestamp = DateTime.UtcNow,
            MetricName = name,
            Device = device,
            Value = value
        };
    }
}
