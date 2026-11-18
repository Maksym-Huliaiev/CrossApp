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

    public IReadOnlyList<Product> List() => 
        db.Products.AsNoTracking().OrderBy(x => x.Sku)
          .AsEnumerable().Select(ToDomain).ToList();

    public IReadOnlyList<Product> Find(Func<Product, bool> predicate) =>
        List().Where(predicate).ToList();

    public Product? GetById(string id) =>
        db.Products.AsNoTracking().SingleOrDefault(x => x.Id == id) is { } row
            ? ToDomain(row) : null;

    public void Add(Product item) 
    { 
        try
        {
            db.Products.Add(ToRow(item)); 
            db.SaveChanges(); 
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            throw new DomainException($"Товар з артикулом '{item.Sku}' вже існує.");
        }
    }

    public void Update(Product item)
    {
        var row = db.Products.Single(x => x.Id == item.Id);
        row.Sku = item.Sku;
        row.Name = item.Name;
        row.Unit = item.Unit;
        row.Quantity = item.Quantity;
        db.SaveChanges();
    }

    public bool Remove(string id)
    {
        var row = db.Products.SingleOrDefault(x => x.Id == id);
        if (row is null) return false;
        
        db.Products.Remove(row);
        db.SaveChanges();
        return true;
    }
}