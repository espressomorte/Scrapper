
namespace Scrapper.Domain
{
    public record Metric
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; init; }
        public string MetricName { get; init; } = string.Empty;
        public string Device { get; init; } = string.Empty;
        public double Value { get; init; }
    }
}
