using Scrapper.Domain;

namespace Scrapper.Data
{
    public interface IMetricsRepository
    {
        Task<int> SaveMetricsAsync(List<Metric> metrics);
    }
}