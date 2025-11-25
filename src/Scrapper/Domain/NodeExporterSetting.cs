using Scrapper.Domain;

public class NodeExporterSetting
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

}