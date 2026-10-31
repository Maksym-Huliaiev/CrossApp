using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class CatalogService
{
    private readonly ICatalogStore _store;
    private readonly INotificationSink? _sink;

    public CatalogService(ICatalogStore store, INotificationSink? sink = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _sink = sink;
    }

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product p, CatalogChangeKind kind, int delta)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, p.Quantity);
        Changed?.Invoke(this, args);
        _sink?.Notify(args);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public bool Remove(string id)
    {
        var product = _store.GetById(id);
        if (product is null) return false;

        var removed = _store.Remove(id);
        if (removed)
        {
            OnChanged(product, CatalogChangeKind.Removed, -product.Quantity);
        }
        return removed;
    }

    public int ImportProducts(IEnumerable<string> rawLines, Action<string>? onLineSkipped = null)
    {
        int importedCount = 0;

        foreach (var line in rawLines)
        {
            var parts = line.Split(';', StringSplitOptions.TrimEntries);

            if (parts.Length < 4)
            {
                onLineSkipped?.Invoke($"Пропущено рядок (недостатньо даних): \"{line}\"");
                continue;
            }

            if (!int.TryParse(parts[3], out int quantity) || quantity < 0)
            {
                onLineSkipped?.Invoke($"Пропущено рядок (некоректна кількість): \"{line}\"");
                continue;
            }

            Add(parts[0], parts[1], parts[2], quantity);
            importedCount++;
        }

        return importedCount;
    }

    public async Task<int> ImportAsync(ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        var items = await importer.LoadAsync(path, ct);
        var added = 0;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            if (_store.GetById(item.Id) is not null) 
                continue;

            _store.Add(item);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            added++;
        }

        return added;
    }

    // Додаткове завдання 1 та 3
    public async Task<int> ImportStreamAsync(
        ICatalogImporter importer, 
        string path, 
        IProgress<int>? progress = null, 
        CancellationToken ct = default)
    {
        var added = 0;

        await foreach (var item in importer.StreamAsync(path, progress, ct))
        {
            if (_store.GetById(item.Id) is not null) 
                continue;

            _store.Add(item);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            added++;
        }

        return added;
    }

    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> Search(Func<Product, bool> predicate) =>
        _store.List().Where(predicate).ToList();

    public IReadOnlyList<Product> All() => _store.List();
}