## Лабораторна робота 5: Сервісний шар, сховища та ручний DI

### Основна частина
* **Інтерфейс сховища**: `ICatalogStore` (Core/Abstractions)
* **Реалізації сховища**:
  * `InMemoryCatalogStore` — збереження у словнику в оперативній пам'яті на базі `SampleData`
  * `FileCatalogStore` — персистентне файлове сховище з кешем і збереженням у `data/catalog.json` через DTO
* **Сервісний шар**: `CatalogService` (Core/Services) — інкапсулює бізнес-операції `Add`, `Receive`, `Issue`, `Find`, `Search`
* **Composition Root**: вибір реалізації сховища та створення залежностей без DI-контейнера

### Додаткові завдання
1. **Декоратор кешування**: реалізовано клас `CachingCatalogStore` (патерн Decorator), який реалізує `ICatalogStore`, приймає внутрішнє сховище та оптимізує повторні читання.
2. **Параметризований пошук**: метод `Find(Func<Product, bool> predicate)` в `ICatalogStore` та `Search` у `CatalogService` для гнучкої фільтрації записів.
3. **Фабрика сховищ**: клас `StoreFactory.Create(string[] args)` у CLI, що ізолює логіку аналізу ключів командного рядка (`--file`, `--cache`) від `Program.cs`.

### Команди запуску
```bash
# 1. Режим InMemory
dotnet run --project src/Cli

# 2. Режим InMemory + Caching Decorator
dotnet run --project src/Cli -- --cache

# 3. Режим FileStore
dotnet run --project src/Cli -- --file

# 4. Режим FileStore + Caching Decorator
dotnet run --project src/Cli -- --file --cache