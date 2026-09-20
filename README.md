## Лабораторна робота 2: Бібліотека Core та публікація

**Структура рішення:**
- `Cli` — консольний застосунок, який є точкою входу і лише виводить дані.
- `Core` — бібліотека класів (classlib), яка містить усю логіку збору інформації про середовище.

**Команди для роботи з проєктом:**
Збірка: `dotnet build`
Запуск: `dotnet run --project src/Cli`
Публікація (self-contained): `dotnet publish src/Cli -c Release -r win-x64 --self-contained true`
Публікація (framework-dependent): `dotnet publish src/Cli -c Release -r win-x64 --self-contained false`
Запуск скомпільованого файлу (Windows): `./src/Cli/bin/Release/net10.0/win-x64/publish/Cli.exe`

### Порівняння режимів публікації (RID: win-x64)

| Режим | Розмір publish | Потрібен runtime |
| :--- | :--- | :--- |
| self-contained | ~70 МБ | ні |
| framework-dependent | ~0.2 МБ | так (.NET 10) |


