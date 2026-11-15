using Api.Endpoints;
using Api.Infrastructure;
using Core.Abstractions;
using Core.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування OpenAPI та формату помилок
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(); 

// 2. Підключаємо наш кастомний обробник помилок (Додаткове завдання 3)
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// 3. Реєструємо сховище як Singleton
builder.Services.AddSingleton<ICatalogStore, InMemoryCatalogStore>(); 
var app = builder.Build();

// 4. Вмикаємо перехоплення помилок, щоб вони йшли через наш DomainExceptionHandler
app.UseExceptionHandler();
app.UseStatusCodePages();

// 5. Вмикаємо інтерфейс документації Scalar тільки для режиму розробки
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 6. Підключаємо наші маршрути з ProductEndpoints.cs
app.MapProducts();

app.Run();