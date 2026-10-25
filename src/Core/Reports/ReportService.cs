using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity)
              .ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.OrderByDescending(p => p.Quantity)
              .ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Take(n)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    public static IReadOnlyList<StockLine> TopByQuantityCustom(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity)
              .ToList();

    public static IReadOnlyList<GroupSummary> ByUnitQuerySyntax(IEnumerable<Product> source)
    {
        var query = from p in source
                    group p by p.Unit into g
                    orderby g.Sum(x => x.Quantity) descending
                    select new GroupSummary(g.Key, g.Count(), g.Sum(x => x.Quantity));

        return query.ToList();
    }

    public static IReadOnlyList<MovementReportDto> GetMovementsReport(
        IEnumerable<Product> products,
        IEnumerable<StockMovement> movements) =>
        products.Join(
            movements,
            p => p.Id,
            m => m.ProductId,
            (p, m) => new MovementReportDto(p.Sku, p.Name, m.Delta, m.Reason))
        .ToList();

    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity), list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }
}