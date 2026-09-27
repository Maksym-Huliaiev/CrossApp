using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

MixedImportResult result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new NotSupportedException("Підтримуються лише файли .csv та .json")
};

// Статистика (Додаткове завдання 3)
int total = result.Products.Count + result.Warehouses.Count + result.Errors.Count;
int accepted = result.Products.Count + result.Warehouses.Count;
int skipped = result.Errors.Count;
double errorRate = total == 0 ? 0 : (double)skipped / total * 100;

Console.WriteLine($"[Статистика] Усього: {total} | Прийнято: {accepted} | Пропущено: {skipped} | Помилок: {errorRate:F1}%");
Console.WriteLine(new string('-', 70));

Console.WriteLine($"Завантажено товарів: {result.Products.Count}");
foreach (ProductDto p in result.Products)
    Console.WriteLine($" [ТОВАР] {p.Id,-6} {p.Sku,-10} {p.Name,-20} {p.Quantity, 5} {p.Unit}");

Console.WriteLine($"\nЗавантажено складів: {result.Warehouses.Count}");
foreach (WarehouseDto w in result.Warehouses)
    Console.WriteLine($" [СКЛАД] {w.Id,-6} {w.Name,-18} {w.Location}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"\nПропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($" ! {e}");
}

return 0;