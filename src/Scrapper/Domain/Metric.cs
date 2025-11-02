// TODO [low priority]: Convert to `record`?
namespace Scrapper.Domain
{
    public class Metric
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public double Value { get; set; }
    }
}
