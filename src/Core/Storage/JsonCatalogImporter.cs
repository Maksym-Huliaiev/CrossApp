using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    public async Task<IReadOnlyList<Product>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                result.Add(Product.FromDto(dtos[i]));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }
        }
        return result;
    }

    // Додаткове завдання 1 та 3
    public async IAsyncEnumerable<Product> StreamAsync(
        string path, 
        IProgress<int>? progress = null, 
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл не знайдено", path);

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(stream, cancellationToken: ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");

        for (var i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            Product product;
            try
            {
                product = Product.FromDto(dtos[i]);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {i + 1}: {ex.Message}", ex);
            }

            // Додаткове 3: повідомляємо поточний прогрес у відсотках
            var percent = (int)(((i + 1) / (double)dtos.Count) * 100);
            progress?.Report(percent);

            yield return product;
        }
    }
}