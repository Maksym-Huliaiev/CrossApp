using Core.Domain;

namespace Core.Abstractions;

public interface ICatalogImporter
{
    Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default);
    
    // Додаткове завдання 1 і 3
    IAsyncEnumerable<Product> StreamAsync(string path, IProgress<int>? progress = null, CancellationToken ct = default);
}