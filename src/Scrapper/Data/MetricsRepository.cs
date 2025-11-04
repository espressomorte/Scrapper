using Microsoft.EntityFrameworkCore;
using Scrapper.Domain;
using Serilog;

namespace Scrapper.Data
{
    public class MetricsRepository : IMetricsRepository
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public MetricsRepository(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        public async Task<IEnumerable<Metric>> GetMetricsAsync(int limit, string? device = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

            Log.Information("Fetching up to {Limit} metrics. Device filter: {Device}", limit, device ?? "None");

            IQueryable<Metric> query = db.NetworkMetrics;

            if (!string.IsNullOrEmpty(device))
            {
                query = query.Where(m => m.Device == device);
            }
            query = query.OrderByDescending(m => m.Timestamp);

            var metrics = await query
                .Take(limit)
                .AsNoTracking() 
                .ToListAsync();

            Log.Information("Fetched {Count} metrics from the database.", metrics.Count);

            return metrics;
        }
        public async Task<int> SaveMetricsAsync(List<Metric> metrics)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();

            Log.Information("Attempting to save {Count} metrics to database.", metrics.Count);

            db.NetworkMetrics.AddRange(metrics);

            var savedCount = await db.SaveChangesAsync();
            Log.Information("Saved {savedCount} metrics successfully.", savedCount);

            return savedCount;
        }
    }
}