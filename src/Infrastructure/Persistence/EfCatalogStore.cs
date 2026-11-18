using Core.Abstractions;
using Core.Domain;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure;

public sealed class EfCatalogStore(AppDbContext db) : ICatalogStore
{
    private static Product ToDomain(ProductRow row) =>
        Product.Create(row.Id, row.Sku, row.Name, row.Unit, row.Quantity);

    private static ProductRow ToRow(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        Unit = p.Unit,
        Quantity = p.Quantity
    };

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken ct = default)
    {
        var rows = await db.Products
            .AsNoTracking()
            .OrderBy(x => x.Sku)
            .ToListAsync(ct);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Product>> FindAsync(Func<Product, bool> predicate, CancellationToken ct = default)
    {
        var items = await ListAsync(ct);
        return items.Where(predicate).ToList();
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var row = await db.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, ct);

        return row is not null ? ToDomain(row) : null;
    }

    public async Task AddAsync(Product item, CancellationToken ct = default)
    {
        try
        {
            await db.Products.AddAsync(ToRow(item), ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new DomainException($"Товар з артикулом '{item.Sku}' вже існує.");
        }
    }

    public async Task UpdateAsync(Product item, CancellationToken ct = default)
    {
        try
        {
            var row = await db.Products.SingleAsync(x => x.Id == item.Id, ct);
            row.Sku = item.Sku;
            row.Name = item.Name;
            row.Unit = item.Unit;
            row.Quantity = item.Quantity;
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new DomainException($"Товар з артикулом '{item.Sku}' вже існує.");
        }
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var row = await db.Products.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return false;

        db.Products.Remove(row);
        await db.SaveChangesAsync(ct);
        return true;
    }
}