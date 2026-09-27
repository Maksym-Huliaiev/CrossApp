using Core.Dto;

namespace Core.Import;

public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<WarehouseDto> Warehouses,
    IReadOnlyList<string> Errors
);

public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith("type", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseProductOk p:
                    products.Add(p.Value);
                    break;
                case ParseWarehouseOk w:
                    warehouses.Add(w.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }
        return new MixedImportResult(products, warehouses, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);
        
        // ОДИН SWITCH - ДВА РІЗНІ ТИПИ РЕЗУЛЬТАТУ
        return parts switch
        {
            // Патерн для товару (префікс P)
            ["P", var id, var sku, var name, var unit, var qty] when int.TryParse(qty, out int q) && q >= 0
                => new ParseProductOk(new ProductDto(id, sku, name, unit, q)),
            ["P", ..] => new ParseFailed($"Неправильний формат товару (отримано {parts.Length} колонок)"),
            
            // Патерн для складу (префікс W)
            ["W", var id, var name, var loc] 
                => new ParseWarehouseOk(new WarehouseDto(id, name, loc)),
            ["W", ..] => new ParseFailed($"Неправильний формат складу (отримано {parts.Length} колонок)"),
            
            _ => new ParseFailed($"Невідомий тип рядка: {parts[0]}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProductOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseWarehouseOk(WarehouseDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}