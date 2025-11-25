using Microsoft.EntityFrameworkCore;
using Scrapper.Data;
using Scrapper.Domain;
using System.Text.RegularExpressions;

namespace Scrapper.Services
{
    /// <summary>
    /// Service containing the business logic for discovering and adding Node Exporters.
    /// This service encapsulates the shared logic for discovery and database operations.
    /// </summary>
    public class DiscoveryService : IDiscoveryService
    {
        private readonly MetricsDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DiscoveryService> _logger;

        private static readonly Regex UnameRegex =
        new Regex("nodename=\"([^\"]+)\"", RegexOptions.Compiled);

        public DiscoveryService(MetricsDbContext context, IHttpClientFactory httpClientFactory, ILogger<DiscoveryService> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(DiscoveryResultStatus Status, NodeExporterSetting? Setting)> AddSingleExporterAsync(string ipAddress)
        {
            var url = $"http://{ipAddress}:9100/metrics";

            // 1. Check for existing entry
            if (await _context.NodeExporterSettings.AnyAsync(n => n.Url == url))
            {
                return (DiscoveryResultStatus.AlreadyExists, null);
            }

            // 2. Perform Discovery
            var newSetting = await DiscoverExporter(url);

            if (newSetting != null)
            {
                _context.NodeExporterSettings.Add(newSetting);
                await _context.SaveChangesAsync();

                return (DiscoveryResultStatus.Added, newSetting);
            }

            // If discovery failed
            return (DiscoveryResultStatus.Unreachable, null);
        }


        /// <summary>
        /// Executes a full network scan for Node Exporters in the specified subnet.
        /// </summary>
        /// <param name="ipPrefix">The first three octets of the subnet (e.g., "192.168.1").</param>
        /// <returns>A list of newly found exporters.</returns>
        public async Task<List<NodeExporterSetting>> ScanNetworkAsync(string ipPrefix)
        {
            const int maxParallelScans = 10;
            using var semaphore = new SemaphoreSlim(maxParallelScans);

            // Fetch existing URLs to avoid scanning and adding duplicates
            var existingUrls = await _context.NodeExporterSettings.Select(n => n.Url).ToListAsync();
            var scanTasks = new List<Task<NodeExporterSetting?>>();

            // Scan IPs from 1 to 254
            for (int i = 1; i <= 254; i++)
            {
                var ip = $"{ipPrefix}.{i}";
                var url = $"http://{ip}:9100/metrics";

                // Skip if already in the database
                if (existingUrls.Contains(url)) continue;

                scanTasks.Add(Task.Run(async () =>
                {
                    // Limit concurrency
                    await semaphore.WaitAsync();
                    try
                    {
                        return await DiscoverExporter(url);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            // Wait for all scan tasks to complete
            var results = await Task.WhenAll(scanTasks);

            var foundExporters = results
                .Where(e => e != null)
                .ToList();

            if (foundExporters.Any())
            {
                _context.NodeExporterSettings.AddRange(foundExporters!);
                await _context.SaveChangesAsync();
            }

            return foundExporters!;
        }

        /// <summary>
        /// The core method to attempt connection to a Node Exporter, read metrics, 
        /// and extract its hostname/nodename.
        /// This method is reused by both AddSingleExporterAsync and ScanNetworkAsync.
        /// </summary>
        /// <param name="url">The full URL of the metrics endpoint.</param>
        /// <returns>A new NodeExporterSetting if successful, otherwise null.</returns>
        private async Task<NodeExporterSetting?> DiscoverExporter(string url)
        {
            var httpClient = _httpClientFactory.CreateClient();
            // Set a short timeout for discovery to avoid blocking the scan for too long
            httpClient.Timeout = TimeSpan.FromSeconds(2);

            try
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var match = UnameRegex.Match(content);
                    var ip = new Uri(url).Host;

                    if (match.Success && match.Groups.Count > 1)
                    {
                        var nodename = match.Groups[1].Value;

                        return new NodeExporterSetting
                        {
                            Name = nodename,
                            Url = url,
                            IsEnabled = true
                        };
                    }

                    // If Nodename is not found, use the IP as a fallback name
                    return new NodeExporterSetting
                    {
                        Name = $"exporter-{ip}",
                        Url = url,
                        IsEnabled = true
                    };
                }
            }
            catch (Exception ex)
            {
                // Log only debug level since many IPs will fail during a scan
                _logger.LogDebug(ex, "Failed to discover exporter at {Url}", url);
            }

            return null;
        }
    }
}