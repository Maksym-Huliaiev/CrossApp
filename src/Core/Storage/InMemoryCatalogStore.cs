using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryCatalogStore(IEnumerable<Product>? seed = null) : ICatalogStore
{
    private readonly Dictionary<string, Product> _items =
        (seed ?? []).ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Product>>(_items.Values.ToList());

    public Task<Product?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    public Task UpdateAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_items.Remove(id));

    public Task AddAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

        _items.Add(item.Id, item);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Task.FromResult<IReadOnlyList<Product>>(_items.Values.Where(predicate).ToList());
    }
}