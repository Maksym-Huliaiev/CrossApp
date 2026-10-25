namespace Core.Reports;

public sealed record StockLine(string Sku, string Name, int Quantity, string Unit);
public sealed record StockMovement(string ProductId, int Delta, string Reason);
public sealed record MovementReportDto(string Sku, string Name, int Delta, string Reason);