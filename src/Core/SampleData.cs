using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IReadOnlyList<Product> Products() =>
    [
        Product.Create("p-001", "SKU-001", "Цемент М400 25кг", "шт", 120),
        Product.Create("p-002", "SKU-002", "Пісок будівельний", "т", 18),
        Product.Create("p-003", "SKU-003", "Арматура 12мм", "м", 250),
        Product.Create("p-004", "SKU-004", "Цегла силікатна", "шт", 1500)
    ];
}