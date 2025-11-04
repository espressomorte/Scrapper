using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Scrapper.Domain;

namespace AnalyticsService
{
    public record NetworkUsageSummary(
        double TotalBytes, 
        double AverageBytes, 
        double MinBytes, 
        double MaxBytes
    );

    public class AnalyticsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _scrapperBaseUrl = "http://localhost:5000"; // Приклад базової URL Scrapper API

        public AnalyticsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        private async Task<IReadOnlyList<Metric>> FetchRawMetricsAsync(int limit)
        {
            // call Scrapper API: GET /metrics/data?limit={limit}
            var response = await _httpClient.GetAsync($"{_scrapperBaseUrl}/metrics/data?limit={limit}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var metrics = JsonSerializer.Deserialize<List<Metric>>(json, options);
            
            return metrics ?? new List<Metric>();
        }

        /// <summary>
        /// Agregates metrics.
        /// </summary>
        /// <param name="limit">metrics limit for processing.</param>
        /// <param name="metricNameFilter">filter by metric name (наприклад, "node_network_receive_bytes_total").</param>
        /// <returns>Object NetworkUsageSummary with aggregated data.</returns>
        public async Task<NetworkUsageSummary> GetAggregatedNetworkUsageAsync(int limit, string metricNameFilter)
        {
            var rawMetrics = await FetchRawMetricsAsync(limit);

            var filteredMetrics = rawMetrics
                .Where(m => m.MetricName == metricNameFilter)
                .ToList();

            if (!filteredMetrics.Any())
            {
                return new NetworkUsageSummary(0, 0, 0, 0);
            }

            var values = filteredMetrics.Select(m => m.Value).ToList();
            
            var total = values.Sum();
            var average = values.Average();
            var min = values.Min();
            var max = values.Max();

            return new NetworkUsageSummary(
                TotalBytes: total, 
                AverageBytes: average, 
                MinBytes: min, 
                MaxBytes: max
            );
        }
    }
}
