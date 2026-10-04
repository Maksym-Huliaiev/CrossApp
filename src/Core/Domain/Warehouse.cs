using Core.Dto;

namespace Core.Domain;

public sealed class Warehouse
{
    private readonly List<Product> _products = [];

    public string Id { get; }
    public string Name { get; }
    public string Location { get; }
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    private Warehouse(string id, string name, string location)
    {
        Id = id;
        Name = name;
        Location = location;
    }

    public static Warehouse Create(string id, string name, string location)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор складу обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва складу не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Локація складу не може бути порожньою", nameof(location));

        return new Warehouse(id.Trim(), name.Trim(), location.Trim());
    }

    public void AddProduct(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (_products.Any(p => p.Sku == product.Sku))
            throw new InvalidOperationException($"Товар зі SKU {product.Sku} вже є на складі {Id}");

        _products.Add(product);
    }

    public WarehouseDto ToDto() => new(Id, Name, Location);

    public static Warehouse FromDto(WarehouseDto dto) =>
        Create(dto.Id, dto.Name, dto.Location);

    public override string ToString() => $"{Id} [{Name}] ({Location}) — товарів: {_products.Count}";
}