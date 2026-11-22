# Лабораторна робота 11: EF Core + SQLite

Переведення HTTP API (`src/Api`) на постійне сховище даних **SQLite** за допомогою Entity Framework Core без зміни наявних маршрутів і контрактів.

## Що реалізовано
- Створено окремий шар `src/Infrastructure` (`AppDbContext`, `EfCatalogStore`, persistence-моделі `ProductRow` і `BatchRow`) без додавання залежностей БД у `Core`.
- Реалізовано зв'язок **1:N** між таблицями `Products` та `Batches` (`ON DELETE CASCADE`) і унікальний індекс на `Products.Sku`.
- Замінено `InMemoryCatalogStore` (Singleton) на `EfCatalogStore` (**Scoped**).
- Додано перехоплення помилки дубліката SKU (`SqliteErrorCode: 19`) з поверненням `409 Conflict` через `DomainExceptionHandler`.
- Налаштовано автоматичне застосування міграцій при старті в середовищі `Development`.
- **Додаткове завдання:** весь стек доступу до даних (`ICatalogStore`, `EfCatalogStore`, `InMemoryCatalogStore`, `FileCatalogStore`, `CachingCatalogStore`, `CatalogService`, `ProductEndpoints`, `Cli` та тести `Core.Tests`) переведено на асинхронні операції (`async/await` + `CancellationToken`).

## База даних
- СУБД: **SQLite**
- Шлях до файлу БД: `src/Api/data/app.db` (додано до `.gitignore` разом із `*.db-shm` та `*.db-wal`)
- Рядок підключення: `src/Api/appsettings.json` → `ConnectionStrings:AppDb`

### Міграції
Команди виконуються з кореня репозиторію:

- Створення нової міграції:
  ```bash
  dotnet ef migrations add <НазваМіграції> --project src/Infrastructure --startup-project src/Api

  запуск серва
dotnet run --project src/Api

додаткове 11
dotnet build
dotnet test

список міграцій та бд

dotnet ef migrations list --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api