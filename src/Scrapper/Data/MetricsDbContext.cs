using System.Data;
using Microsoft.EntityFrameworkCore;

public class MetricsDbContext : DbContext
{
    public DbSet<Metric> NetworkMetrics => Set<Metric>();

    public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options)
    {
    }
}
