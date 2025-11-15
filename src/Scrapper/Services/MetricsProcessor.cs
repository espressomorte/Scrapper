using System.Diagnostics.Metrics;
using Scrapper.Data;
using Scrapper.Domain;
using Serilog;
using static Scrapper.Domain.MetricsCollectorService;

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

        public async Task<int> ProcessAndSaveMetricsAsync(string metrics, DateTime timestamp, int nodeExporterSettingId)
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
                    var metric = ParseMetricLine(line, timestamp, nodeExporterSettingId);
                    parsedMetrics.Add(metric);
                }
                catch (MetricParseException)
                {
                    Log.Warning("Cannot parse metric line at line {line}", lineCounter++);
                }
            }
            return await _metricRepository.SaveMetricsAsync(parsedMetrics);
        }
        private Metric ParseMetricLine(string line, DateTime timestamp, int nodeExporterSettingId)
        {
            var parts = line.Split(' ');
            if (parts.Length != 2)
            {
                throw new MetricParseException("Line doen't have two parts when split by space");
            }

            var nameAndLabels = parts[0];
            double value;

            if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                throw new MetricParseException($"Cannot parse value: {parts[1]}");
            }

            var name = nameAndLabels.Split('{')[0];

            var device = "unknown";
            var labelStart = nameAndLabels.IndexOf('{');
            if (labelStart > 0)
            {
                var labelContentLength = nameAndLabels.Length - labelStart - 2;

                if (labelContentLength <= 0)
                {
                    throw new MetricParseException("Metric contain empty or broken label.");
                }

                var labels = nameAndLabels.Substring(labelStart + 1, labelContentLength);
                var devPair = labels.Split(',')
                                    .FirstOrDefault(l => l.Trim().StartsWith("device="));

                if (devPair != null)
                {
                    var devParts = devPair.Split('"');
                    if (devParts.Length >= 2)
                    {
                        device = devParts[1];
                    }
                }
            }
            return new Metric
            {
                NodeExporterSettingId = nodeExporterSettingId,
                TimestampDateTime = timestamp,
                MetricName = name,
                Device = device,
                Value = value
            };
        }

    }
}
