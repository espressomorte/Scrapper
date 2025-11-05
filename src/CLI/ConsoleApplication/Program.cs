using Spectre.Console;
using System.Net.Http.Json;

// Визначення запису для відповідності структурі AnalyticsService
public record NetworkUsageSummary(
    double TotalBytes,
    double AverageBytes,
    double MinBytes,
    double MaxBytes
);

public class Program
{
    // URL вашого AnalyticsService
    private const string AnalyticsServiceUrl = "http://localhost:5232/analytics/network-summary";
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task Main(string[] args)
    {
        // Заголовок
        AnsiConsole.MarkupLine("[bold blue]*** Network Metrics Analysis Console ***[/]");
        AnsiConsole.WriteLine();
        
        // Використання Status для індикатора завантаження
        await AnsiConsole.Status()
            .StartAsync("Fetching and analyzing data from Analytics Service...", async ctx =>
            {
                try
                {
                    var summary = await FetchNetworkSummaryAsync();
                    
                    ctx.Status("Rendering analysis table...");
                    Thread.Sleep(500); 
                  RenderAnalysisTable(summary);
                }
                catch (HttpRequestException ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Error connecting to Analytics Service ([/]{AnalyticsServiceUrl}[bold red]):[/]");
                    AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    AnsiConsole.WriteLine("\nPlease ensure both Scrapper (port 5000) and AnalyticsService (port 5232) are running.");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]An unexpected error occurred:[/]\n[red]{ex.Message}[/]");
                }
            });

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Press any key to exit.[/]");
        Console.ReadKey(true);
    }

    private static async Task<NetworkUsageSummary> FetchNetworkSummaryAsync()
    {

        var response = await _httpClient.GetAsync(AnalyticsServiceUrl);
        response.EnsureSuccessStatusCode();

        var summary = await response.Content.ReadFromJsonAsync<NetworkUsageSummary>();
        
        return summary ?? new NetworkUsageSummary(0, 0, 0, 0);
    }

    private static void RenderAnalysisTable(NetworkUsageSummary summary)
    {
        // Створення та налаштування таблиці
        var table = new Table()
            .Title("[bold white on navy]Network Usage Aggregation Summary[/]")
            .Border(TableBorder.Heavy)
            .BorderStyle(Style.Parse("blue"))
            .AddColumn(new TableColumn("[bold yellow]Metric[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Value (Bytes)[/]").Centered());

        // Функція-допомога для форматування чисел (розділювач тисяч)
        string formatValue(double value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        // Додавання рядків до таблиці
        table.AddRow("[cyan]Total Received Bytes[/]", $"[green]{formatValue(summary.TotalBytes)}[/]");
        table.AddRow("[cyan]Average Received Bytes[/]", $"[orange3]{formatValue(summary.AverageBytes)}[/]");
        table.AddRow("[cyan]Minimum Received Bytes[/]", $"[lightgreen]{formatValue(summary.MinBytes)}[/]");
        table.AddRow("[cyan]Maximum Received Bytes[/]", $"[red]{formatValue(summary.MaxBytes)}[/]");

        AnsiConsole.Write(table);

        // Інформація про джерело
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey italic]Data source:[/][link]{AnalyticsServiceUrl}[/]");
    }
}
