namespace Scrapper.Services
{
    public interface IMetricsProcessor
    {
        Task<int> ProcessAndSaveMetricsAsync(string metrics, DateTime timestamp, int nodeExporterSettingId);
    }
}