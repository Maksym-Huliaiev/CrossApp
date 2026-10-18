using Cli;
using Core.Domain;
using Core.Events;
using Core.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"=== Лабораторна робота 6 ===");
Console.WriteLine($"Сховище: {store.GetType().Name}\n");

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

if (File.Exists(logPath))
{
    File.Delete(logPath);
}

void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
{
    var line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} -> залишок {e.QuantityAfter}";
    Console.WriteLine(line);
    File.AppendAllText(logPath, line + Environment.NewLine);
}

service.Changed += OnCatalogChanged;

int changes = 0;
service.Changed += (_, _) => changes++;

Console.WriteLine("--- 1. Початковий список товарів ---");
PrintCatalog(service.All());

Console.WriteLine("\n--- 2. Виконання операцій (активна підписка) ---");
var added = service.Add("SKU-005", "Гіпсокартон 12.5мм", "лист", 40);
service.Receive(added.Id, 20);
service.Issue(added.Id, 15);

Console.WriteLine($"\n[Лічильник] Подій отримано обома підписниками: {changes}");

Console.WriteLine("\n--- 3. Демонстрація відписки (-= OnCatalogChanged) ---");
service.Changed -= OnCatalogChanged;
Console.WriteLine("Локальний метод відписано. Наступна операція тиха для логу/консолі:");

service.Receive(added.Id, 5);

Console.WriteLine($"[Лічильник] Подій після тихої операції: {changes}");

Console.WriteLine("\n--- 4. Пошук з предикатом Func<Product, bool> ---");
Console.WriteLine("Товари з залишком > 30:");
PrintCatalog(service.Search(p => p.Quantity > 30));

Console.WriteLine("\n--- 5. Стан каталогу після всіх операцій ---");
PrintCatalog(service.All());

Console.WriteLine("\n--- 6. Сценарій відмови (інваріант: помилка є, події немає) ---");
try
{
    Console.WriteLine("Спроба виконати прихід від'ємної кількості (-10):");
    service.Receive(added.Id, -10);
}
catch (Exception ex)
{
    Console.WriteLine($"[Очікувана помилка]: {ex.Message}");
}

try
{
    Console.WriteLine("Спроба операції для неіснуючого id='fake-999':");
    service.Receive("fake-999", 10);
}
catch (Exception ex)
{
    Console.WriteLine($"[Очікувана помилка]: {ex.Message}");
}

Console.WriteLine($"[Лічильник] Подій після помилок (має лишитися 4): {changes}");

static void PrintCatalog(IEnumerable<Product> products)
{
    Console.WriteLine($"{"ID",-10} {"SKU",-10} {"Назва",-22} {"К-сть",6} {"Од.",-6}");
    Console.WriteLine(new string('-', 58));
    foreach (var p in products)
    {
        Console.WriteLine($"{p.Id,-10} {p.Sku,-10} {p.Name,-22} {p.Quantity,6} {p.Unit,-6}");
    }
}

Console.WriteLine("\n=== Додаткове завдання 1: Ручний DI з INotificationSink ===");
var sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sink.log");
var compositeSink = new CompositeNotificationSink(
    new ConsoleSink(),
    new FileSink(sinkLogPath)
);

// Передаємо сховище і сінк через конструктор (ручний DI)
var diService = new CatalogService(store, compositeSink);
var diProduct = diService.Add("SKU-999", "Цемент М500", "мішок", 10);
diService.Receive(diProduct.Id, 5);

Console.WriteLine("\n=== Додаткове завдання 2: Імпорт з Action<string> ===");

var linesToImport = new[]
{
    "SKU-201;Плитка керамічна;м2;100",
    "пошкоджений_рядок_без_колонок",
    "SKU-202;Грунтовка універсальна;л;-15",
    "SKU-203;Клей монтажний;шт;40"
};

int imported = service.ImportProducts(linesToImport, reason =>
{
    Console.WriteLine($"[Пропущено]: {reason}");
});

Console.WriteLine($"Успішно імпортовано позицій: {imported}");