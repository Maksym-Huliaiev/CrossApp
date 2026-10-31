using Xunit;
using Core.Domain;
using Core.Events;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class UnitTest1
{
    [Fact]
    public void Receive_WithNegativeQuantity_Throws_AndKeepsState()
    {
        var store = new InMemoryCatalogStore();
        var product = Product.Create("p1", "SKU-001", "Цемент М500", "мішок", 10);
        store.Add(product);
        var service = new CatalogService(store);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.Receive("p1", -5));
        Assert.Equal(10, service.Find("p1")?.Quantity);
    }

    [Fact]
    public void Add_RaisesChangedExactlyOnce()
    {
        var store = new InMemoryCatalogStore();
        var service = new CatalogService(store);
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.Add("SKU-002", "Пісок річковий", "т", 5);

        Assert.Single(events);
        Assert.Equal(CatalogChangeKind.Added, events[0].Kind);
    }

    [Theory]
    [InlineData(10, 1)]
    [InlineData(1, 0)]
    public void LowStock_CountsOnlyBelowThreshold(int threshold, int expectedCount)
    {
        var items = new List<Product>
        {
            Product.Create("p1", "SKU-001", "Цегла", "шт", 5),
            Product.Create("p2", "SKU-002", "Брус", "м", 20)
        };

        var result = ReportService.LowStock(items, threshold);

        Assert.Equal(expectedCount, result.Count);
    }

    [Fact]
    public void TopByQuantity_IsDeterministic()
    {
        var items = new List<Product>
        {
            Product.Create("p1", "SKU-MID", "Шпаклівка", "мішок", 20),
            Product.Create("p2", "SKU-BIG", "Гіпсокартон", "лист", 50),
            Product.Create("p3", "SKU-LOW", "Ґрунтовка", "л", 5)
        };

        var result = ReportService.TopByQuantity(items, 2);

        Assert.Equal(2, result.Count);
        Assert.Equal("SKU-BIG", result[0].Sku);
        Assert.Equal("SKU-MID", result[1].Sku);
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "[]");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ImportAsync(new JsonCatalogImporter(), tempFile, cts.Token));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImportAsync_CorruptedData_ThrowsDomainException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "[{\"Id\": \"bad-1\", \"Sku\": \"\", \"Name\": \"Брак\", \"Unit\": \"шт\", \"Quantity\": 1}]");

            var ex = await Assert.ThrowsAsync<DomainException>(() =>
                service.ImportAsync(new JsonCatalogImporter(), tempFile));

            Assert.Contains("Запис 1", ex.Message);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ImportAsync_LinkedTokens_ThrowsOnTimeout()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "[{\"Id\": \"t1\", \"Sku\": \"SKU-T\", \"Name\": \"Тест\", \"Unit\": \"шт\", \"Quantity\": 10}]");
            var service = new CatalogService(new InMemoryCatalogStore());

            await Task.Delay(100);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                service.ImportAsync(new JsonCatalogImporter(), tempFile, linkedCts.Token));

            Assert.True(timeoutCts.IsCancellationRequested);
            Assert.False(userCts.IsCancellationRequested);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}