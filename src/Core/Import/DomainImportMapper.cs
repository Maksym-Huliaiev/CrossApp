using Core.Domain;
using Core.Dto;

namespace Core.Import;

public record DomainValidationResult(
    IReadOnlyList<Product> Products,
    IReadOnlyList<Warehouse> Warehouses,
    IReadOnlyList<string> Errors);

public static class DomainImportMapper
{
    // Додаткове завдання 1 бере MixedImportResult (тиждень 3) і повертає сутності + помилки інваріантів
    public static DomainValidationResult ToDomain(MixedImportResult importResult)
    {
        List<Product> products = [];
        List<Warehouse> warehouses = [];
        List<string> errors = [.. importResult.Errors];

        foreach (ProductDto dto in importResult.Products)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"[Інваріант Product {dto.Id}] {ex.GetType().Name}: {ex.Message}");
            }
        }

        foreach (WarehouseDto dto in importResult.Warehouses)
        {
            try
            {
                warehouses.Add(Warehouse.FromDto(dto));
            }
            catch (Exception ex)
            {
                errors.Add($"[Інваріант Warehouse {dto.Id}] {ex.GetType().Name}: {ex.Message}");
            }
        }

        return new DomainValidationResult(products, warehouses, errors);
    }
}