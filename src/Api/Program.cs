using Api.Endpoints;
using Api.Infrastructure;
using Core.Abstractions;
using Infrastructure;
using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування OpenAPI та формату помилок
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(); 

// 2. Підключаємо наш кастомний обробник помилок (Додаткове завдання 3)
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// --- ДОДАЄМО НАЛАШТУВАННЯ БАЗИ ДАНИХ (Лабораторна 11) ---
var raw = builder.Configuration.GetConnectionString("AppDb") ?? "Data Source=data/app.db";
var csb = new SqliteConnectionStringBuilder(raw);
if (!Path.IsPathRooted(csb.DataSource))
{ 
    var full = Path.Combine(builder.Environment.ContentRootPath, csb.DataSource);
    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
    csb.DataSource = full; 
}

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(csb.ConnectionString));

// 3. Реєструємо сховище як SCOPED (замість Singleton)
builder.Services.AddScoped<ICatalogStore, EfCatalogStore>(); 
// --------------------------------------------------------

var app = builder.Build();

// --- АВТОМАТИЧНЕ СТВОРЕННЯ БАЗИ ПРИ ЗАПУСКУ ---
using (var scope = app.Services.CreateScope())
{
    if (app.Environment.IsDevelopment())
    {
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
// ----------------------------------------------

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