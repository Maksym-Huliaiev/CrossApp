using Core.Domain;
using Core.Dto;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

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

// Статистика імпорту з Лабораторної №3
int total = result.Products.Count + result.Warehouses.Count + result.Errors.Count;
int accepted = result.Products.Count + result.Warehouses.Count;
int skipped = result.Errors.Count;
double errorRate = total == 0 ? 0 : (double)skipped / total * 100;

Console.WriteLine($"[Статистика імпорту] Усього: {total} | Прийнято: {accepted} | Пропущено: {skipped} | Помилок: {errorRate:F1}%");
Console.WriteLine(new string('-', 70));

// Додаткове завдання 1 (Лабораторна №4): перетворення DTO -> Доменні сутності з перевіркою інваріантів
List<Product> domainProducts = [];
List<Warehouse> domainWarehouses = [];
List<string> domainErrors = [.. result.Errors];

foreach (ProductDto dto in result.Products)
{
    try
    {
        domainProducts.Add(Product.FromDto(dto));
    }
    catch (Exception ex)
    {
        domainErrors.Add($"Інваріант товару ({dto.Id}): {ex.Message}");
    }
}

foreach (WarehouseDto dto in result.Warehouses)
{
    try
    {
        domainWarehouses.Add(Warehouse.FromDto(dto));
    }
    catch (Exception ex)
    {
        domainErrors.Add($"Інваріант складу ({dto.Id}): {ex.Message}");
    }
}

Console.WriteLine($"Створено доменних сутностей товарів: {domainProducts.Count}");
foreach (Product p in domainProducts)
    Console.WriteLine($" [ТОВАР] {p}");

Console.WriteLine($"\nСтворено доменних сутностей складів: {domainWarehouses.Count}");
foreach (Warehouse w in domainWarehouses)
    Console.WriteLine($" [СКЛАД] {w}");

if (domainErrors.Count > 0)
{
    Console.WriteLine($"\nПропущено записів (парсинг + інваріанти): {domainErrors.Count}");
    foreach (string e in domainErrors)
        Console.WriteLine($" ! {e}");
}

Console.WriteLine(new string('=', 70));

// Основне завдання Лабораторної №4: демонстрація доменної моделі та інваріантів
Console.WriteLine("\n--- Сценарій 1: успіх ---");
Warehouse warehouse = Warehouse.Create("W-001", "Головний склад", "Львів, вул. Зелена");
Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
Console.WriteLine(product);

product.RegisterArrival(50);
product.Issue(30);
warehouse.AddProduct(product);

Console.WriteLine(product);
Console.WriteLine(warehouse);

Console.WriteLine("\n--- Сценарій 2: порушення інваріантів ---");
TryDo("видача більша за залишок", () => product.Issue(1000));
TryDo("порожній SKU", () => Product.Create("P-002", "", "Пісок", "т", 10));
TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
TryDo("нульовий прихід", () => product.RegisterArrival(0));
TryDo("дублікат товару на складі", () => warehouse.AddProduct(Product.Create("P-099", "SKU-001", "Цемент копія", "шт", 10)));

Console.WriteLine($"\nСпостережуваний факт — після всіх відмов стан об'єкта не змінився:\n{product}");

return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($" {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" {title}: {ex.GetType().Name} — {ex.Message}");
    }
}