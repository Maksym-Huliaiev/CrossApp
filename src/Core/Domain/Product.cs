using Core.Dto;

namespace Core.Domain;

// Додаткове завдання 3
public enum ProductStatus
{
    Draft,      
    Confirmed,  
    Cancelled   
}

public sealed class Product
{
    private int _quantity;

    public string Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public string Unit { get; }
    public string? Note { get; private set; }
    public int Quantity => _quantity;
    public ProductStatus Status { get; private set; } = ProductStatus.Draft;

    private Product(string id, string sku, string name, string unit, int quantity, string? note)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Unit = unit;
        _quantity = quantity;
        Note = note;
    }

    public static Product Create(string id, string sku, string name, string unit, int quantity, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Ідентифікатор обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU не може бути порожнім", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва не може бути порожньою", nameof(name));
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Одиниця виміру не може бути порожньою", nameof(unit));
        if (quantity < 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Початковий залишок не може бути від'ємним");

        string? normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        return new Product(
            id.Trim(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            unit.Trim(),
            quantity,
            normalizedNote);
    }

    // Додаткове завдання 3
    public void ChangeStatus(ProductStatus targetStatus)
    {
        bool canTransition = (Status, targetStatus) switch
        {
            (ProductStatus.Draft, ProductStatus.Confirmed) => true,
            (ProductStatus.Draft, ProductStatus.Cancelled) => true,
            (ProductStatus.Confirmed, ProductStatus.Cancelled) when _quantity == 0 => true,
            _ => false
        };

        if (!canTransition)
        {
            throw new InvalidOperationException(
                $"Неприпустимий перехід стану для {Sku}: {Status} -> {targetStatus} (поточний залишок: {_quantity})");
        }

        Status = targetStatus;
    }

    public void RegisterArrival(int amount)
    {
        if (Status == ProductStatus.Cancelled)
            throw new InvalidOperationException($"Товар {Sku} скасовано (Cancelled), прихід неможливий");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість приходу має бути більшою за нуль");

        _quantity += amount;
    }

    public void Issue(int amount)
    {
        if (Status == ProductStatus.Cancelled)
            throw new InvalidOperationException($"Товар {Sku} скасовано (Cancelled), видача неможлива");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Кількість видачі має бути більшою за нуль");

        if (amount > _quantity)
            throw new InvalidOperationException($"Не можна видати {amount}: залишок {Sku} = {_quantity}");

        _quantity -= amount;
    }

    public ProductDto ToDto() => new(Id, Sku, Name, Unit, Quantity, Note);

    public static Product FromDto(ProductDto dto) =>
        Create(dto.Id, dto.Sku, dto.Name, dto.Unit, dto.Quantity, dto.Note);

    public override string ToString() =>
        $"{Id} [{Sku}] {Name} - {Quantity} {Unit} (Стан: {Status})";
}