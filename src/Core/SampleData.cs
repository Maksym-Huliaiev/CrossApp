using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<Product> Products() => new()
    {
        Product.Create("p-001", "SKU-001", "Цемент М400 25кг", "мішок", 120),
        Product.Create("p-002", "SKU-002", "Пісок будівельний", "т", 18),
        Product.Create("p-003", "SKU-003", "Арматура 12мм", "м", 250),
        Product.Create("p-004", "SKU-004", "Цегла силікатна", "шт", 1500),
        Product.Create("p-005", "SKU-005", "Гіпсокартон 12.5мм", "лист", 50),
        Product.Create("p-006", "SKU-006", "Шпаклівка фінішна", "мішок", 45),
        Product.Create("p-007", "SKU-007", "Грунтовка глибока", "шт", 8),
        Product.Create("p-008", "SKU-008", "Профіль CD-60", "м", 180),
        Product.Create("p-009", "SKU-009", "Саморізи 35мм", "шт", 2500),
        Product.Create("p-010", "SKU-010", "Щебінь фр. 5-20", "т", 4),
        Product.Create("p-011", "SKU-011", "Клей для плитки", "мішок", 85),
        Product.Create("p-012", "SKU-012", "Мінеральна вата", "лист", 35),
        Product.Create("p-013", "SKU-013", "Фарба фасадна", "шт", 12),
        Product.Create("p-014", "SKU-014", "Труба профільна", "м", 95),
        Product.Create("p-015", "SKU-015", "Клей монтажний", "шт", 5),
        Product.Create("p-016", "SKU-016", "Вапно гашене", "мішок", 22)
    };
}