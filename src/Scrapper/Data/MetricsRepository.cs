using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MetricsRepository : IMetricsRepository
{
    private readonly IServiceScopeFactory _scopeFactory;


    public MetricsRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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
