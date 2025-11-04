using Scrapper.Domain;

namespace Scrapper.Data
{
    public interface IMetricsRepository
    {
        Task<IEnumerable<Metric>> GetMetricsAsync(int limit, string? device = null);
        Task<int> SaveMetricsAsync(List<Metric> metrics);
    }
}