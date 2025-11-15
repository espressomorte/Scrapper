
namespace Scrapper.Domain
{
    public record Metric
    {
        public int Id { get; set; }

        public long Timestamp { get; set; }

        public string MetricName { get; init; } = string.Empty;

        public string Device { get; init; } = string.Empty;

        public double Value { get; init; }

        public int NodeExporterSettingId { get; set; }

        public NodeExporterSetting? NodeExporterSetting { get; set; }

        public DateTime TimestampDateTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime;
            set => Timestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
        }
    }
}
