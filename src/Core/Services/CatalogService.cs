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

    public async Task<Product> AddAsync(string sku, string name, string unit, int quantity, CancellationToken ct = default)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        await _store.AddAsync(product, ct);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

    public async Task ReceiveAsync(string id, int quantity, CancellationToken ct = default)
    {
        var product = await _store.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        product.RegisterArrival(quantity);
        await _store.UpdateAsync(product, ct);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public async Task IssueAsync(string id, int quantity, CancellationToken ct = default)
    {
        var product = await _store.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Немає запису з id={id}.");

        product.Issue(quantity);
        await _store.UpdateAsync(product, ct);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var product = await _store.GetByIdAsync(id, ct);
        if (product is null) return false;

        var removed = await _store.RemoveAsync(id, ct);
        if (removed)
        {
            OnChanged(product, CatalogChangeKind.Removed, -product.Quantity);
        }
        return removed;
    }

    public async Task<int> ImportProductsAsync(IEnumerable<string> rawLines, Action<string>? onLineSkipped = null, CancellationToken ct = default)
    {
        int importedCount = 0;

        foreach (var line in rawLines)
        {
            ct.ThrowIfCancellationRequested();
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

            await AddAsync(parts[0], parts[1], parts[2], quantity, ct);
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
            if (await _store.GetByIdAsync(item.Id, ct) is not null)
                continue;

            await _store.AddAsync(item, ct);
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
            if (await _store.GetByIdAsync(item.Id, ct) is not null)
                continue;

            await _store.AddAsync(item, ct);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            added++;
        }

        return added;
    }

    public Task<Product?> FindAsync(string id, CancellationToken ct = default) =>
        _store.GetByIdAsync(id, ct);

    public async Task<IReadOnlyList<Product>> SearchAsync(Func<Product, bool> predicate, CancellationToken ct = default)
    {
        var items = await _store.ListAsync(ct);
        return items.Where(predicate).ToList();
    }

    public Task<IReadOnlyList<Product>> AllAsync(CancellationToken ct = default) =>
        _store.ListAsync(ct);
}