using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore(ICatalogStore innerStore) : ICatalogStore
{
    private readonly ICatalogStore _innerStore = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _fullyLoaded;

    public IReadOnlyList<Product> List()
    {
        if (!_fullyLoaded)
        {
            var items = _innerStore.List();
            _cache.Clear();
            foreach (var item in items)
            {
                _cache[item.Id] = item;
            }
            _fullyLoaded = true;
        }

        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        if (_cache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        var item = _innerStore.GetById(id);
        if (item is not null)
        {
            _cache[id] = item;
        }

        return item;
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _innerStore.Add(item);
        _cache[item.Id] = item;
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _innerStore.Update(item);
        _cache[item.Id] = item;
    }

    public bool Remove(string id)
    {
        var removed = _innerStore.Remove(id);
        if (removed)
        {
            _cache.Remove(id);
        }

        return removed;
    }

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _fullyLoaded 
            ? _cache.Values.Where(predicate).ToList() 
            : _innerStore.Find(predicate);
    }
}