using Cli;
using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var store = StoreFactory.Create(args);
var service = new CatalogService(store);

var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";
var items = service.All();

switch (command)
{
    case "import":
        var fileArgIndex = Array.IndexOf(args, "--file");
        var dataPath = fileArgIndex >= 0 && fileArgIndex + 1 < args.Length
            ? args[fileArgIndex + 1]
            : (args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "data/catalog.json");

        using (var cts = new CancellationTokenSource())
        {
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

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
        Console.WriteLine("import | report-top | report-low | report-groups | report-sum | demo-deferred | extra-join | extra-top-ext | extra-query-syntax | extra-benchmark");
        break;
}

static void Print(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
    {
        Console.WriteLine($"  {l.Sku,-10} {l.Name,-24} {l.Quantity,6} {l.Unit}");
    }
}