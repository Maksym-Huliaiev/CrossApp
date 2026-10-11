using Cli;
using Core.Domain;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"=== Лабораторна робота 5 ===");
Console.WriteLine($"Сховище: {store.GetType().Name}\n");

Console.WriteLine("--- 1. Початковий список товарів ---");
PrintCatalog(service.All());

Console.WriteLine("\n--- 2. Виконання операцій (Add, Receive, Issue) ---");
var added = service.Add("SKU-005", "Гіпсокартон 12.5мм", "лист", 40);
Console.WriteLine($"[Додано] ID: {added.Id} | {added.Sku} | {added.Name} | {added.Quantity} {added.Unit}");

Console.WriteLine($"Прихід товару (+20 {added.Unit})...");
service.Receive(added.Id, 20);

Console.WriteLine($"Видача товару (-15 {added.Unit})...");
service.Issue(added.Id, 15);

var updated = service.Find(added.Id);
Console.WriteLine($"Поточний залишок: {updated?.Quantity} {updated?.Unit}\n");

Console.WriteLine("--- 3. Пошук з предикатом Func<Product, bool> ---");
Console.WriteLine("Товари з залишком > 100:");
PrintCatalog(service.Search(p => p.Quantity > 100));

Console.WriteLine("\nТовари з назвою, що містить 'Цемент':");
PrintCatalog(service.Search(p => p.Name.Contains("Цемент", StringComparison.OrdinalIgnoreCase)));

Console.WriteLine("\n--- 4. Стан каталогу після операцій ---");
PrintCatalog(service.All());

Console.WriteLine("\n--- 5. Сценарій відмови (try/catch) ---");
try
{
    Console.WriteLine("Спроба виконати операцію для неіснуючого id='fake-999':");
    service.Receive("fake-999", 10);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"[Очікувана помилка]: {ex.Message}");
}

static void PrintCatalog(IEnumerable<Product> products)
{
    Console.WriteLine($"{"ID",-10} {"SKU",-10} {"Назва",-22} {"К-сть",6} {"Од.",-6}");
    Console.WriteLine(new string('-', 58));
    foreach (var p in products)
    {
        Console.WriteLine($"{p.Id,-10} {p.Sku,-10} {p.Name,-22} {p.Quantity,6} {p.Unit,-6}");
    }
}