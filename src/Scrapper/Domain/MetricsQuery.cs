namespace Scrapper.Domain;

public record MetricQuery
{
    public int Limit { get; set; } = 50;
    public string Device { get; set; } = "";
}