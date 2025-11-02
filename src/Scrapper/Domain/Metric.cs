// TODO [low priority]: Convert to `record`?
public class Metric
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public double Value { get; set; }
}
