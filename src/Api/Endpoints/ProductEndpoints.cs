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
        group.MapGet("/", (ICatalogStore store, string? search, int page = 1, int pageSize = 20) =>
        {
            var query = store.List().AsEnumerable();

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
        group.MapGet("/low-stock", (ICatalogStore store, int threshold = 5) =>
        {
            var lowStockItems = store.List()
                .Where(p => p.Quantity < threshold)
                .Select(p => p.ToDto())
                .ToList();
            
            return TypedResults.Ok(lowStockItems);
        });

        // 3. GET /api/products/{id}
        group.MapGet("/{id}", Results<Ok<ProductDto>, NotFound>
            (string id, ICatalogStore store) =>
            store.GetById(id) is { } found
                ? TypedResults.Ok(found.ToDto())
                : TypedResults.NotFound());

        // 4. POST /api/products
        group.MapPost("/", Results<Created<ProductDto>, ValidationProblem>
            (SaveProductRequest request, ICatalogStore store) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            var id = Convert.ToHexString(RandomNumberGenerator.GetBytes(4)).ToLowerInvariant();
            var created = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            
            store.Add(created);
            return TypedResults.Created($"/api/products/{created.Id}", created.ToDto());
        });

        // 5. PUT /api/products/{id}
        group.MapPut("/{id}", Results<Ok<ProductDto>, NotFound, ValidationProblem>
            (string id, SaveProductRequest request, ICatalogStore store) =>
        {
            if (!request.TryValidate(out var errors))
                return TypedResults.ValidationProblem(errors);

            if (store.GetById(id) is null)
                return TypedResults.NotFound();

            var replacement = Product.Create(id, request.Sku, request.Name, request.Unit, request.Quantity);
            store.Update(replacement);
            
            return TypedResults.Ok(replacement.ToDto());
        });

        // 6. DELETE /api/products/{id}
        group.MapDelete("/{id}", Results<NoContent, NotFound>
            (string id, ICatalogStore store) =>
            store.Remove(id)
                ? TypedResults.NoContent()
                : TypedResults.NotFound());
    }
}