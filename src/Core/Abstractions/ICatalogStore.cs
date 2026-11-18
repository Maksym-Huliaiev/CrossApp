using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogStore
{
    Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(Product item, CancellationToken ct = default);
    Task UpdateAsync(Product item, CancellationToken ct = default);
    Task<bool> RemoveAsync(string id, CancellationToken ct = default);

    // Додаткове завдання 2: фільтрація за довільним предикатом
    Task<IReadOnlyList<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken ct = default);
}