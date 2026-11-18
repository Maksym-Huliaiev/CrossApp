Лабораторна робота 10: Minimal API

Створення HTTP API для доменного шару з використанням ASP.NET Core Minimal API, DTO, валідації та OpenAPI (Scalar UI).
Додаткові завдання: реалізовано параметри пагінації/пошуку (page, pageSize, search) та власний обробник DomainExceptionHandler для перехоплення помилок бізнес-логіки (повертає 409 Conflict замість 500).

Команди та запити (ЛР №10)

Запуск API сервера:
dotnet run --project src/Api --urls http://localhost:5080
(Документація Scalar UI буде доступна за адресою: http://localhost:5080/scalar/v1)

Успішне створення товару (POST -> 201 Created):
curl -i -X POST http://localhost:5080/api/products -H "Content-Type: application/json" -d '{"sku":"A-100","name":"Дошки","unit":"шт","quantity":10}'

Перевірка валідації (POST -> 400 Bad Request):
curl -i -X POST http://localhost:5080/api/products -H "Content-Type: application/json" -d '{"sku":"","name":"","unit":"","quantity":-1}'

## База даних
Проєкт використовує SQLite. Файл бази даних зберігається за шляхом `src/Api/data/app.db`.
Рядок підключення знаходиться в `appsettings.json` у секції `ConnectionStrings:AppDb`.

### Міграції
Для застосування змін схеми бази даних використовуйте інструмент `dotnet-ef`:

- Створення нової міграції: `dotnet ef migrations add <НазваМіграції> --project src/Infrastructure --startup-project src/Api`
- Оновлення бази даних (застосування міграцій): `dotnet ef database update --project src/Infrastructure --startup-project src/Api`

### Скидання бази даних
Якщо потрібно повністю очистити базу даних, зупиніть сервер та видаліть файл `src/Api/data/app.db` разом з `app.db-shm` та `app.db-wal`. При наступному запуску застосунку база даних буде автоматично перестворена порожньою на основі міграцій.