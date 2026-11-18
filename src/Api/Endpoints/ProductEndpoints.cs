using System.Security.Cryptography;
using Api.Contracts;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProducts(this WebApplication app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // 1. GET /api/products (з пагінацією та пошуком)
        group.MapGet("/", async (ICatalogStore store, CancellationToken ct, string? search, int page = 1, int pageSize = 20) =>
        {
            var allItems = await store.ListAsync(ct);
            var query = allItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    p.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var total = query.Count();
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => p.ToDto())
                .ToList();

            return TypedResults.Ok(new { items, total });
        });

        // 2. GET /api/products/low-stock?threshold=5 (Специфіка домену "Склад")
        group.MapGet("/low-stock", async (ICatalogStore store, CancellationToken ct, int threshold = 5) =>
        {
            var allItems = await store.ListAsync(ct);
            var lowStockItems = allItems
                .Where(p => p.Quantity < threshold)
                .Select(p => p.ToDto())
                .ToList();

            return TypedResults.Ok(lowStockItems);
        });

        // 3. GET /api/products/{id}
        group.MapGet("/{id}", async Task<Results<Ok<ProductDto>, NotFound>>
            (string id, ICatalogStore store, CancellationToken ct) =>
            await store.GetByIdAsync(id, ct) is { } found
                ? TypedResults.Ok(found.ToDto())
                : TypedResults.NotFound());

        // 4. POST /api/products
        group.MapPost("/", async Task<Results<Created<ProductDto>, ValidationProblem>>
            (SaveProductRequest request, ICatalogStore store, CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var created = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);

            await store.AddAsync(created, ct);
            return TypedResults.Created($"/api/products/{created.Id}", created.ToDto());
        });

        // 5. PUT /api/products/{id}
        group.MapPut("/{id}", async Task<Results<Ok<ProductDto>, NotFound, ValidationProblem>>
            (string id, SaveProductRequest request, ICatalogStore store, CancellationToken ct) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            if (await store.GetByIdAsync(id, ct) is null)
                return TypedResults.NotFound();

            var replacement = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            await store.UpdateAsync(replacement, ct);

            return TypedResults.Ok(replacement.ToDto());
        });

        // 6. DELETE /api/products/{id}
        group.MapDelete("/{id}", async Task<Results<NoContent, NotFound>>
            (string id, ICatalogStore store, CancellationToken ct) =>
            await store.RemoveAsync(id, ct)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());
    }
}