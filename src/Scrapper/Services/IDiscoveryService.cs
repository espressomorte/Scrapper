using Scrapper.Domain;

namespace Scrapper.Services
{
    /// <summary>
    /// Contract for the service managing Node Exporter discovery processes.
    /// </summary>
    public interface IDiscoveryService
    {
        /// <summary>
        /// Attempts to discover a single exporter at the given IP and adds it to the database.
        /// </summary>
        /// <param name="ipAddress">The IP address for discovery.</param>
        /// <returns>The discovered NodeExporterSetting or null if discovery failed or the exporter already exists.</returns>
        //Task<NodeExporterSetting?> AddSingleExporterAsync(string ipAddress);
        Task<(DiscoveryResultStatus Status, NodeExporterSetting? Setting)> AddSingleExporterAsync(string ipAddress);

        /// <summary>
        /// Executes a full subnet scan and adds new exporters found.
        /// </summary>
        /// <param name="ipPrefix">The network prefix to scan (e.g., "192.168.1").</param>
        /// <returns>A list of successfully discovered and added NodeExporterSettings.</returns>
        Task<List<NodeExporterSetting>> ScanNetworkAsync(string ipPrefix);
    }
}