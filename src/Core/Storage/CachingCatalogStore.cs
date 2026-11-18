using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore(ICatalogStore innerStore) : ICatalogStore
{
    private readonly ICatalogStore _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _fullyLoaded;

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default)
    {
        if (!_fullyLoaded)
        {
            var items = await _innerStore.ListAsync(ct);
            _cache.Clear();
            foreach (var item in items)
            {
                _cache[item.Id] = item;
            }
            _fullyLoaded = true;
        }

        return _cache.Values.ToList();
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var item = await _innerStore.GetByIdAsync(id, ct);
        if (item is not null)
        {
            _cache[id] = item;
        }

        return item;
    }

    public async Task AddAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _innerStore.AddAsync(item, ct);
        _cache[item.Id] = item;
    }

    public async Task UpdateAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _innerStore.UpdateAsync(item, ct);
        _cache[item.Id] = item;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var removed = await _innerStore.RemoveAsync(id, ct);
        if (removed)
        {
            _cache.Remove(id);
        }

        return removed;
    }

    public async Task<IReadOnlyList<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _fullyLoaded
            ? _cache.Values.Where(predicate).ToList()
            : await _innerStore.FindAsync(predicate, ct);
    }
}