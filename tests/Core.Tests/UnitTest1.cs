using Xunit;
using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public static class TestData
{
    public static List<Product> Items() => new()
    {
        Product.Create("1", "SKU-BIG", "Саморізи 35мм", "шт", 2500),
        Product.Create("2", "SKU-MID", "Цегла силікатна", "шт", 1500),
        Product.Create("3", "SKU-LOW", "Клей монтажний", "шт", 5)
    };
}

public class CatalogServiceTests
{
    [Fact]
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var p = service.Add("SKU-1", "Цемент М400 25кг", "мішок", 10);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive(p.Id, -5));
        Assert.Equal(10, store.GetById(p.Id)!.Quantity);
    }

    [Fact]
    public void Add_RaisesChangedExactlyOnce()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-2", "Гвинт", "шт", 7);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expected)
    {
        var result = ReportService.LowStock(TestData.Items(), threshold);
        Assert.Equal(expected, result.Count);
    }

    [Fact]
    public void TopByQuantity_IsDeterministic()
    {
        var top = ReportService.TopByQuantity(TestData.Items(), 2).Select(p => p.Sku);
        Assert.Equal(new[] { "SKU-BIG", "SKU-MID" }, top);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact]
    public async Task ImportAsync_CorruptedData_ThrowsDomainException()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "[{\"Id\": \"99\", \"Sku\": \"\", \"Name\": \"Тест\", \"Unit\": \"шт\", \"Quantity\": -5}]");
            var service = new CatalogService(new InMemoryCatalogStore());

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.ImportAsync(new JsonCatalogImporter(), tempFile));

            Assert.Contains("Запис 1", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}