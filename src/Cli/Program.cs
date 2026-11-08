using System.Reflection;
using Cli;
using Core;
using Core.Configuration;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 1. Визначення назви середовища (за замовчуванням Development)[cite: 4]
var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

// 2. Побудова ланцюжка конфігурації за пріоритетами[cite: 2, 4]
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{envName}.json", optional: true)
    .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables(prefix: "CROSSAPP_")
    .AddCommandLine(args)
    .Build();

// 3. Біндинг об'єкта AppOptions[cite: 3, 4]
var options = config.Get<AppOptions>() ?? new AppOptions();

// 4. Підтримка простої змінної CROSSAPP_DATA_PATH (вимога п. 4 методички)[cite: 3, 4]
var legacyDataPath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
var storage = new StorageOptions
{
    DataPath = !string.IsNullOrWhiteSpace(legacyDataPath) ? legacyDataPath : options.Storage.DataPath,
    FileName = options.Storage.FileName
};

// 5. Отримання кросплатформного абсолютного шляху відносно каталогу програми[cite: 1, 4]
var fullDataPath = storage.ResolveFullPath(AppContext.BaseDirectory);
var apiKey = config["Integration:ApiKey"];

// 6. Обов'язковий вивід згідно з очікуваним виводом методички[cite: 4, 7]
Console.WriteLine($"Файл даних: {fullDataPath}");
Console.WriteLine(apiKey is null ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {apiKey.Length}");

// 7. Сховище та сервіс
var store = new FileCatalogStore(fullDataPath);
var service = new CatalogService(store);

// Команда за замовчуванням — "export"
var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "export";
var items = service.All();

switch (command)
{
    case "export":
        // Якщо каталог порожній, додамо зразок запису з методички для наповнення items.json[cite: 7]
        if (store.List().Count == 0)
        {
            store.Add(Product.Create("a1b2c3d4", "A-100", "Цемент М400", "мішок", 40));
        }

        var exported = store.List();
        Console.WriteLine($"Експортовано {exported.Count} записи.");
        break;

    case "import":
        var fileArgIndex = Array.IndexOf(args, "--file");
        var dataPath = fileArgIndex >= 0 && fileArgIndex + 1 < args.Length
            ? args[fileArgIndex + 1]
            : (args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "data/import.json");

        using (var cts = new CancellationTokenSource())
        {
            ConsoleCancelEventHandler cancelHandler = (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                var added = await service.ImportAsync(new JsonCatalogImporter(), dataPath, cts.Token);
                Console.WriteLine($"Імпортовано записів: {added}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Імпорт скасовано.");
            }
            catch (DomainException ex)
            {
                Console.WriteLine($"Дані відхилено: {ex.Message}");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine($"Файл не знайдено: {dataPath}");
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
        break;

    case "extra-stream":
        var streamPath = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "data/import.json";
        using (var streamCts = new CancellationTokenSource())
        {
            var progress = new Progress<int>(percent =>
            {
                Console.Write($"\r[Прогрес імпорту]: {percent}%");
            });

            try
            {
                var count = await service.ImportStreamAsync(new JsonCatalogImporter(), streamPath, progress, streamCts.Token);
                Console.WriteLine($"\nУспішно імпортовано через IAsyncEnumerable: {count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nПомилка: {ex.Message}");
            }
        }
        break;

    case "report-top":
        Print(ReportService.TopByQuantity(items, 5));
        break;

    case "report-low":
        Print(ReportService.LowStock(items, 10));
        break;

    case "report-groups":
        foreach (var g in ReportService.ByUnit(items))
        {
            Console.WriteLine($"  {g.Key,-8} позицій: {g.Count,3}  сумарно: {g.TotalQuantity,6}");
        }
        break;

    case "report-sum":
        var t = ReportService.Totals(items);
        Console.WriteLine($"  позицій: {t.Items}, всього: {t.Total}, середнє: {t.Average:F2}, макс: {t.Max}");
        break;

    case "demo-deferred":
        var source = service.All().ToList();
        var query = source.Where(p => p.Quantity < 10);
        Console.WriteLine($"Дефіцитних до змін: {query.Count()}");

        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба монтажна", "шт", 3));
        Console.WriteLine($"Дефіцитних після змін: {query.Count()}");

        var snapshot = query.ToList();
        source.Clear();
        Console.WriteLine($"У знімку лишилось: {snapshot.Count}");
        break;

    case "extra-join":
        var movements = new List<StockMovement>
        {
            new("p-001", 50, "Постачання від заводу"),
            new("p-001", -20, "Відвантаження бригаді"),
            new("p-004", 500, "Нова партія"),
            new("p-009", -100, "Списання браку")
        };
        var joinResult = ReportService.GetMovementsReport(items, movements);
        foreach (var m in joinResult)
        {
            Console.WriteLine($"  {m.Sku,-10} {m.Name,-24} {m.Delta,6}  {m.Reason}");
        }
        break;

    case "extra-top-ext":
        Print(ReportService.TopByQuantityCustom(items, 3));
        break;

    case "extra-query-syntax":
        foreach (var g in ReportService.ByUnitQuerySyntax(items))
        {
            Console.WriteLine($"  {g.Key,-8} позицій: {g.Count,3}  сумарно: {g.TotalQuantity,6}");
        }
        break;

    case "extra-benchmark":
        var largeDataset = Enumerable.Range(1, 100_000)
            .Select(i => Product.Create($"id-{i}", $"SKU-{i}", $"Товар {i}", "шт", i % 100))
            .ToList();

        var sw = System.Diagnostics.Stopwatch.StartNew();

        sw.Restart();
        var streamingResult = largeDataset
            .Where(p => p.Quantity > 50)
            .Select(p => p.Quantity)
            .Take(10)
            .ToList();
        sw.Stop();
        var timeStreaming = sw.ElapsedMilliseconds;

        sw.Restart();
        var bufferedResult = largeDataset
            .Where(p => p.Quantity > 50)
            .ToList()
            .Select(p => p.Quantity)
            .Take(10)
            .ToList();
        sw.Stop();
        var timeBuffered = sw.ElapsedMilliseconds;

        Console.WriteLine($"Без ToList() у середині: {timeStreaming} мс");
        Console.WriteLine($"З ToList() у середині:   {timeBuffered} мс");
        break;

    default:
        Console.WriteLine("export | import | extra-stream | report-top | report-low | report-groups | report-sum | demo-deferred | extra-join | extra-top-ext | extra-query-syntax | extra-benchmark");
        break;
}

static void Print(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
    {
        Console.WriteLine($"  {l.Sku,-10} {l.Name,-24} {l.Quantity,6} {l.Unit}");
    }
}