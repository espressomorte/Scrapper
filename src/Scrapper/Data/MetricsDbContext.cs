using System.Data;
using Microsoft.EntityFrameworkCore;
using Scrapper.Domain;
namespace Scrapper.Data
{
    public class MetricsDbContext : DbContext
    {
        public DbSet<Metric> NetworkMetrics => Set<Metric>();
        public DbSet<NodeExporterSetting> NodeExporterSettings { get; set; } = null!;

        public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options)
        {
        }    
    }
}