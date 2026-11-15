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

[Додатково 1] Пагінація та пошук (GET -> 200 OK):
Отримання списку товарів з фільтрацією за пошуковим словом та поділом на сторінки. API повертає об'єкт формату { items, total }

[Додатково 3] Перехоплення бізнес-помилок (DomainException -> 409 Conflict):
Глобальний обробник DomainExceptionHandler інтегровано в пайплайн застосунку. Якщо під час POST/PUT запитів порушується логіка доменного шару (викидається DomainException), сервер не падає з критичною помилкою 500 Internal Server Error, а безпечно перехоплює її та повертає статус 409 Conflict із детальним описом проблеми у стандарті ProblemDetails.