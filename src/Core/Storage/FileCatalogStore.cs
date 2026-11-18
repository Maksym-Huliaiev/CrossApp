using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private async Task EnsureLoadedAsync(CancellationToken ct)
    {
        if (_loaded) return;
        if (File.Exists(_path))
        {
            var json = await File.ReadAllTextAsync(_path, ct);
            var dtos = JsonSerializer.Deserialize<List<ProductDto>>(json, Options) ?? [];
            foreach (var dto in dtos)
            {
                var p = Product.FromDto(dto);
                _cache[p.Id] = p;
            }
        }
        _loaded = true;
    }

    private async Task FlushAsync(CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dtos = _cache.Values.Select(p => p.ToDto()).ToList();
        var tmp = _path + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(dtos, Options), ct);
        File.Move(tmp, _path, overwrite: true);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _cache.Values.ToList();
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        return _cache.GetValueOrDefault(id);
    }

    public async Task AddAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureLoadedAsync(ct);
        if (_cache.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        await FlushAsync(ct);
    }

    public async Task UpdateAsync(Product item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await EnsureLoadedAsync(ct);
        _cache[item.Id] = item;
        await FlushAsync(ct);
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        await EnsureLoadedAsync(ct);
        if (!_cache.Remove(id)) return false;

        await FlushAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        await EnsureLoadedAsync(ct);
        return _cache.Values.Where(predicate).ToList();
    }
}