# CrossApp

## Лабораторна робота 11 — EF Core + SQLite
API (`src/Api`) переведено на базу даних **SQLite** через Entity Framework Core.
- Робота з БД винесена в окремий проєкт `src/Infrastructure` (`AppDbContext`, `EfCatalogStore`, моделі `ProductRow` і `BatchRow`, зв'язок 1:N з каскадним видаленням).
- Файл бази зберігається локально в `src/Api/data/app.db` (додано в `.gitignore`), рядок підключення — у `src/Api/appsettings.json` (`ConnectionStrings:AppDb`).
- Дублікат артикулу (`Sku`) повертає `409 Conflict`.
- Додаткове завдання: весь стек доступу до даних, сервіси, ендпоінти, `Cli` і тести переведено на `async/await`.

## Лабораторна робота 12 — Blazor-клієнт (`src/Web`)
Додано веб-інтерфейс на Blazor, який працює з `src/Api` по HTTP і не має прямого доступу до БД (`src/Infrastructure`).
- Режим: **Blazor Interactive Server** (`@rendermode InteractiveServer`) — C#-код виконується на сервері `Web`, оновлення в браузер ідуть через SignalR.
- Конфігурація:
  - `src/Web/appsettings.json` → `Api:BaseUrl` (`http://localhost:5080`)
  - `src/Api/appsettings.json` → `Cors:WebOrigin` (`http://localhost:5090`)
- Реалізовано список товарів (з підсвіткою залишку < 150), створення, редагування, обробку помилок (`400`, `404`, `409`, недоступний API).
- Додаткові завдання: видалення з підтвердженням без перезавантаження сторінки та скасування запитів через `CancellationTokenSource`.

---

## Команди

### Лаба 11
Міграції та перевірка БД:
```bash
dotnet ef migrations list --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

Запуск API:
```bash
dotnet run --project src/Api --urls http://localhost:5080
```

Збірка і тести:
```bash
dotnet build
dotnet test
```

### Лаба 12 (потрібно два термінали)
Термінал 1 — API (порт 5080):
```bash
dotnet run --project src/Api --urls http://localhost:5080
```

Термінал 2 — Blazor Web (порт 5090):
```bash
dotnet run --project src/Web --urls http://localhost:5090
```

Сторінка в браузері: `http://localhost:5090/products`