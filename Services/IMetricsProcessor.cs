public interface IMetricsProcessor
{
    Task<int> ProcessAndSaveMetricsAsync(string metrics);
}