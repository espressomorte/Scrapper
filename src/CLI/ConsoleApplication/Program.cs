using Spectre.Console;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

// --- Data Records (Моделі даних) ---
// Модель конфігурації джерела метрик (відповідає Scrapper API)
public record ExporterConfig(
    int Id,
    string Name,
    string Url,
    // Використовуємо атрибут, оскільки в API назва поля "isEnabled"
    [property: JsonPropertyName("isEnabled")] bool IsActive
);

public record Metric(int Id, DateTime Timestamp, string MetricName, string Device, double Value);
public record NetworkUsageSummary(
    double TotalBytes,
    double AverageBytes,
    double MinBytes,
    double MaxBytes
);

public class Program
{
    private const string ScrapperBaseUrl = "http://localhost:5252/";
    private const string AnalyticsServiceUrl = "http://localhost:5232/analytics/";
    private static readonly HttpClient _httpClient = new HttpClient();

    public static async Task Main(string[] args)
    {
        AnsiConsole.MarkupLine("[bold blue]*** Network Metrics Analysis CLI ***[/]");
        AnsiConsole.WriteLine();

        // Якщо аргументів немає, показуємо допомогу
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            switch (command)
            {
                case "show":
                    await HandleShowCommand(args.Skip(1).ToArray());
                    break;
                case "config":
                    await HandleConfigCommand(args.Skip(1).ToArray());
                    break;
                case "help":
                case "-h":
                case "--help":
                    ShowHelp();
                    break;
                default:
                    AnsiConsole.MarkupLine($"[bold red]Error:[/][red] Unknown command: {command}.[/]");
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            // Обробка непередбачених помилок
            AnsiConsole.MarkupLine($"[bold red]FATAL ERROR:[/]\n[red]{ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]Operation complete.[/]");
    }

    // --- Обробка команди 'CONFIG' ---

    private static async Task HandleConfigCommand(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/][red] 'config' command requires a subcommand (show or add).[/]");
            ShowHelp();
            return;
        }

        var subcommand = args[0].ToLowerInvariant();

        switch (subcommand)
        {
            case "show":
                await ShowConfigSources();
                break;
            case "add":
                if (args.Length < 2)
                {
                    AnsiConsole.MarkupLine("[bold red]Error:[/][red] 'config add' requires an IP address or hostname.[/]");
                    ShowHelp();
                    return;
                }
                await AddConfigSource(args[1]);
                break;
            default:
                AnsiConsole.MarkupLine($"[bold red]Error:[/][red] Unknown subcommand for 'config': {subcommand}.[/]");
                ShowHelp();
                break;
        }
    }

    // Відображення списку джерел метрик
    private static async Task ShowConfigSources()
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching metric sources configuration...", async ctx =>
            {
                var url = $"{ScrapperBaseUrl}api/Config";
                try
                {
                    var exporters = await FetchDataAsync<IReadOnlyList<ExporterConfig>>(url);

                    ctx.Status("Rendering configuration table...");
                    RenderConfigTable(exporters);
                }
                catch (HttpRequestException ex)
                {
                    HandleConnectionError(url, ex, ScrapperBaseUrl);
                }
            });
    }

    // Додавання нового джерела метрик
    private static async Task AddConfigSource(string ipOrHost)
    {
        await AnsiConsole.Status()
            .StartAsync($"Requesting Scrapper to add new source: {ipOrHost}...", async ctx =>
            {
                // Використовуємо ендпоїнт Discovery API
                var url = $"{ScrapperBaseUrl}Discovery/AddSingle?ipOrHost={ipOrHost}";
                try
                {
                    // Discovery/AddSingle використовує POST без тіла запиту
                    var response = await _httpClient.PostAsync(url, content: null);
                    response.EnsureSuccessStatusCode();

                    ctx.Status("Source added successfully. Refreshing list...");
                    Thread.Sleep(500); // Даємо час на запис у БД

                    AnsiConsole.MarkupLine($"[green]Successfully added source: [bold]{ipOrHost}[/].[/]");
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("400"))
                {
                    // Обробка, якщо джерело вже існує або IP недійсний
                    AnsiConsole.MarkupLine($"[bold red]ERROR:[/][red] Source [bold]{ipOrHost}[/] is probably already configured or request is invalid (Status 400).[/]");
                }
                catch (HttpRequestException ex)
                {
                    HandleConnectionError(url, ex, ScrapperBaseUrl);
                }
            });
    }

    private static void RenderConfigTable(IReadOnlyList<ExporterConfig> exporters)
    {
        var table = new Table()
            .Title("[bold white on darkmagenta]Configured Metric Sources[/]")
            .Border(TableBorder.Heavy)
            .BorderStyle(Style.Parse("darkmagenta"))
            .AddColumn(new TableColumn("[bold yellow]ID[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Name[/]"))
            .AddColumn(new TableColumn("[bold yellow]URL[/]"))
            .AddColumn(new TableColumn("[bold yellow]Active[/]").Centered());

        if (!exporters.Any())
        {
            table.AddRow("[grey]N/A[/]", "[grey]No sources configured.[/]", "[grey]N/A[/]", "[grey]N/A[/]");
        }

        foreach (var e in exporters.OrderBy(e => e.Id))
        {
            var status = e.IsActive ? "[green]YES[/]" : "[red]NO[/]";
            table.AddRow(
                e.Id.ToString(),
                e.Name,
                e.Url,
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey italic]Configuration API:[/][link]{ScrapperBaseUrl}admin/config/exporters[/]");
    }

    // --- Обробка команди 'SHOW' ---

    private static async Task HandleShowCommand(string[] args)
    {
        if (args.Length == 0)
        {
            AnsiConsole.MarkupLine("[bold red]Error:[/][red] 'show' command requires a flag (-s or -l).[/]");
            ShowHelp();
            return;
        }

        var flag = args[0].ToLowerInvariant();

        switch (flag)
        {
            case "-s":
            case "--summary":
                await ShowAggregateSummary();
                break;
            case "-l":
            case "--live":
                await ShowLiveTicker();
                break;
            default:
                AnsiConsole.MarkupLine($"[bold red]Error:[/][red] Unknown flag: {flag} for 'show' command.[/]");
                ShowHelp();
                break;
        }
    }

    private static async Task ShowAggregateSummary()
    {
        await AnsiConsole.Status()
            .StartAsync("Fetching and analyzing data from Analytics Service...", async ctx =>
            {
                var url = $"{AnalyticsServiceUrl}network-summary";
                try
                {
                    var summary = await FetchDataAsync<NetworkUsageSummary>(url);
                    ctx.Status("Rendering analysis table...");
                    Thread.Sleep(500);
                    RenderAnalysisTable(summary);
                }
                catch (HttpRequestException ex)
                {
                    HandleConnectionError(url, ex, AnalyticsServiceUrl);
                }
            });
    }

    private static async Task ShowLiveTicker()
    {
        var url = $"{AnalyticsServiceUrl}live-metrics";

        AnsiConsole.MarkupLine("\n[yellow]--- Real-Time Metrics Ticker ---[/]");
        AnsiConsole.MarkupLine("[grey]Metrics update every 2 seconds. Press any key to stop.[/]");

        await AnsiConsole.Live(new Panel(new Markup("... Starting live stream ...")))
            .StartAsync(async ctx =>
            {
                // Запускаємо читання клавіш у фоновому потоці
                var keyTask = Task.Run(() => Console.ReadKey(true));

                try
                {
                    while (!keyTask.IsCompleted)
                    {
                        var metrics = await FetchDataAsync<IReadOnlyList<Metric>>(url);
                        var tickerPanel = RenderMetricsTicker(metrics);
                        ctx.UpdateTarget(tickerPanel);

                        // Додаємо невелику затримку та перевіряємо, чи не натиснута клавіша
                        var delayTask = Task.Delay(2000);
                        await Task.WhenAny(delayTask, keyTask);
                    }
                }
                catch (HttpRequestException ex)
                {
                    HandleConnectionError(url, ex, AnalyticsServiceUrl);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[bold red]Error in Live Ticker:[/]\n[red]{ex.Message}[/]");
                }
            });
        AnsiConsole.MarkupLine("\n[bold yellow]Live Ticker stopped.[/]");
    }

    private static void RenderAnalysisTable(NetworkUsageSummary summary)
    {
        var table = new Table()
            .Title("[bold white on navy]Network Usage Aggregation Summary[/]")
            .Border(TableBorder.Heavy)
            .BorderStyle(Style.Parse("blue"))
            .AddColumn(new TableColumn("[bold yellow]Metric[/]").Centered())
            .AddColumn(new TableColumn("[bold yellow]Value (Bytes)[/]").Centered());

        string formatValue(double value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

        table.AddRow("[cyan]Total Received Bytes[/]", $"[green]{formatValue(summary.TotalBytes)}[/]");
        table.AddRow("[cyan]Average Received Bytes[/]", $"[orange3]{formatValue(summary.AverageBytes)}[/]");
        table.AddRow("[cyan]Minimum Received Bytes[/]", $"[lightgreen]{formatValue(summary.MinBytes)}[/]");
        table.AddRow("[cyan]Maximum Received Bytes[/]", $"[red]{formatValue(summary.MaxBytes)}[/]");

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey italic]Data source: [/][link]{AnalyticsServiceUrl}network-summary[/]");
    }

    private static Panel RenderMetricsTicker(IReadOnlyList<Metric> metrics)
    {
        var tickerBuilder = new StringBuilder();

        foreach (var metric in metrics.OrderBy(m => m.MetricName).Take(20)) // Обмежимо для читабельності
        {
            var formattedValue = metric.Value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

            tickerBuilder.Append($"[bold purple]{metric.MetricName}[/]: [green]{formattedValue}[/] ([yellow]{metric.Device}[/]) [grey]|[/] ");
        }

        var tickerContent = tickerBuilder.ToString().TrimEnd(' ', '|', ' ');

        var panel = new Panel(new Markup(tickerContent))
            .Header($"[bold white on darkred] Live Metrics Ticker ({DateTime.Now:HH:mm:ss}) [/]")
            .Expand()
            .BorderStyle(Style.Parse("darkred"));

        return panel;
    }

    // --- Допомога ---

    private static void ShowHelp()
    {
        var table = new Table()
            .Title("[bold white on blue]Available Commands[/]")
            .Border(TableBorder.Rounded)
            .AddColumn("[bold yellow]Command[/]")
            .AddColumn("[bold yellow]Description[/]");

        table.AddRow("[cyan]show -s, --summary[/]", "Fetches and displays the network usage aggregation summary (snapshot).");
        table.AddRow("[cyan]show -l, --live[/]", "Starts the real-time metrics ticker (live stream). Press any key to stop.");
        table.AddRow("[magenta]config show[/]", "Displays the list of all configured metric sources (Exporters).");
        table.AddRow("[magenta]config add [[IP/Host]][/]", "Adds a new Prometheus Exporter source (e.g., config add 192.168.1.166).");
        table.AddRow("help", "Displays this help message.");

        AnsiConsole.Write(table);
    }

    // --- Узагальнений допоміжний метод для Fetching ---

    private static async Task<T> FetchDataAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<T>();

        if (data == null)
        {
            throw new Exception($"Failed to deserialize data from {url}. Response was empty or invalid.");
        }
        return data;
    }

    private static void HandleConnectionError(string url, HttpRequestException ex, string baseUrl)
    {
        AnsiConsole.MarkupLine($"[bold red]Error connecting to Service ([/]{url}[bold red]):[/]");
        AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
        AnsiConsole.WriteLine($"\nPlease ensure the required service is running at [yellow]{baseUrl}[/].");
    }
}