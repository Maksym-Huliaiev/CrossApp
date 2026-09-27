using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static MixedImportResult Load(string path)
    {
        var errors = new List<string>();
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
            
            // Повертаємо товари, порожній список складів і список помилок
            return new MixedImportResult(items, new List<WarehouseDto>(), errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Помилка читання JSON: {ex.Message}");
            return new MixedImportResult(new List<ProductDto>(), new List<WarehouseDto>(), errors);
        }
    }
}