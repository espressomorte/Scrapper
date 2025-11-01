public interface IMetricsRepository
{
    Task<int> SaveMetricsAsync(List<Metric> metrics);
}