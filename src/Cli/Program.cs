using Core.Domain;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

MixedImportResult rawImport = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => throw new NotSupportedException("Підтримуються лише файли .csv та .json")
};

// === Додаткове завдання 1: метод DomainImportMapper.ToDomain(rawImport) ===
DomainValidationResult domainResult = DomainImportMapper.ToDomain(rawImport);

Console.WriteLine("=== Додаткове завдання 1: Імпорт DTO -> Доменні сутності ===");
Console.WriteLine($"Успішно створено товарів: {domainResult.Products.Count}, складів: {domainResult.Warehouses.Count}");
foreach (Product p in domainResult.Products)
    Console.WriteLine($" [ТОВАР] {p}");
foreach (Warehouse w in domainResult.Warehouses)
    Console.WriteLine($" [СКЛАД] {w}");

if (domainResult.Errors.Count > 0)
{
    Console.WriteLine($"Відхилено записів (парсинг + інваріанти): {domainResult.Errors.Count}");
    foreach (string err in domainResult.Errors)
        Console.WriteLine($" ! {err}");
}

Console.WriteLine(new string('=', 70));

// === Основне завдання + Додаткові завдання 2 і 3 ===
Console.WriteLine("\n--- Сценарій 1: успіх (включно зі зміною станів enum) ---");
Warehouse warehouse = Warehouse.Create("W-001", "Головний склад", "Львів, вул. Зелена");
Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
Console.WriteLine(product);

// Додаткове завдання 3: перехід Draft -> Confirmed
product.ChangeStatus(ProductStatus.Confirmed);
product.RegisterArrival(50);
product.Issue(30);
warehouse.AddProduct(product);

Console.WriteLine(product);
Console.WriteLine(warehouse);

Console.WriteLine("\n--- Сценарій 2: порушення інваріантів ---");
TryDo("видача більша за залишок", () => product.Issue(1000));
TryDo("порожній SKU", () => Product.Create("P-002", "", "Пісок", "т", 10));
TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));
TryDo("дублікат товару на складі", () => warehouse.AddProduct(Product.Create("P-099", "SKU-001", "Цемент копія", "шт", 10)));

// Демонстрація Додаткового завдання 3 (неприпустимі переходи enum через switch expression)
TryDo("скасування товару з ненульовим залишком (Confirmed -> Cancelled)",
    () => product.ChangeStatus(ProductStatus.Cancelled));

Product cancelledProduct = Product.Create("P-004", "SKU-004", "Вапно", "шт", 0);
cancelledProduct.ChangeStatus(ProductStatus.Cancelled);
TryDo("повернення зі скасованого стану (Cancelled -> Confirmed)",
    () => cancelledProduct.ChangeStatus(ProductStatus.Confirmed));

// Демонстрація Додаткового завдання 2 (інваріант на 2 сутності: ліміт 5 товарів на складі)
for (int i = 2; i <= 5; i++)
{
    warehouse.AddProduct(Product.Create($"P-00{i}", $"SKU-00{i}", $"Товар {i}", "шт", 10));
}
Console.WriteLine($" Заповнений склад: {warehouse}");
TryDo("перевищення ліміту 5 товарів на складі",
    () => warehouse.AddProduct(Product.Create("P-006", "SKU-006", "Товар 6", "шт", 10)));

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