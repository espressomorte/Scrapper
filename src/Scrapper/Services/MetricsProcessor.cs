using System.Diagnostics.Metrics;
using Serilog;
using static MetricsCollectorService;

namespace Scrapper.Services
{
    public class MetricsProcessor : IMetricsProcessor
{
    private readonly IMetricsRepository _metricRepository;
    private readonly string[] _prefixes = { "node_network_", "node_netstat_", "node_sockstat_" };

    public MetricsProcessor(IMetricsRepository metricRepository)
    {
        _metricRepository = metricRepository;
    }

    public async Task<int> ProcessAndSaveMetricsAsync(string metrics)
    {
        var filtered = metrics
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => _prefixes.Any(p => line.StartsWith(p)))
            .Where(line => !line.StartsWith("#")) // skip HELP/TYPE
            .ToList();
        var parsedMetrics = new List<Metric>();
        var lineCounter = 1;

        Log.Information("Processing {Count} raw metric lines.", filtered.Count);

        foreach (var line in filtered)
        {
            try
            {
                var metric = ParseMetricLine(line);
                parsedMetrics.Add(metric);
            }
            catch (MetricParseException)
            {
                Log.Warning("Cannot parse metric line at line {line}", lineCounter++);
            }
        }
        return await _metricRepository.SaveMetricsAsync(parsedMetrics);
    }
    private Metric ParseMetricLine(string line)
    {
        var parts = line.Split(' ');
        if (parts.Length != 2)
        {
            throw new MetricParseException("Line doen't have two parts when split by space");
        }

        var nameAndLabels = parts[0];
    double value;
    if (double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
    {
        Console.WriteLine("Parsed with Any + InvariantCulture");
    }
    else if (double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
    {
        Console.WriteLine("Parsed with Float + InvariantCulture");
    }
    else if (double.TryParse(parts[1], out value))
    {
        Console.WriteLine("Parsed with default settings");
    }
    else
    {
        throw new MetricParseException($"Cannot parse value: {parts[1]}");
    }

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

}
