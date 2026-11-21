# Splitwise Clone API

REST API для обліку спільних витрат у групах: учасники записують витрати, система рахує баланси й підказує, хто кому скільки винен.

**Живий Swagger:** [splitwise-clone-ari1.onrender.com/swagger](https://splitwise-clone-ari1.onrender.com/swagger/index.html)

> Сервіс працює на безкоштовному тарифі Render і «засинає» без запитів, тому перший запит може тривати до хвилини.

## Можливості

- реєстрація й вхід (ASP.NET Core Identity + JWT);
- групи з ролями `Admin` та `Member`, додавання й видалення учасників;
- витрати з розподілом `Equal`, `Exact`, `Percentage`, `Shares` (до копійки, без втрат);
- баланси групи та пропозиції розрахунків («хто кому скільки винен»);
- запис розрахунків (погашення боргу);
- перевірка доступу: користувач бачить лише дані своїх груп.

## Технології

.NET 10 (ASP.NET Core Web API), Entity Framework Core, PostgreSQL (Npgsql), ASP.NET Core Identity, JWT Bearer, FluentValidation, Swagger (Swashbuckle), xUnit + Moq, Docker, GitHub Actions, Render.

## Архітектура

```
Controllers  →  Services (бізнес-логіка)  →  SplitwiseDbContext (EF Core)  →  PostgreSQL
     ↑
 FluentValidation            GlobalExceptionHandler: AppException → ProblemDetails (400 / 401 / 403 / 404 / 409 / 500)
```

| Папка | Призначення |
|---|---|
| `Controllers` | тонкий HTTP-шар: читає `UserId` з токена й викликає сервіс |
| `Services` | уся бізнес-логіка (`Auth`, `Group`, `Expense`, `Balance`, `Settlement`, `User`) |
| `Validators` | правила FluentValidation для вхідних DTO |
| `Exceptions`, `Middleware` | власні винятки й глобальний обробник помилок |
| `Domain`, `Data`, `Migrations` | сутності, `DbContext`, міграції (застосовуються автоматично при старті) |
| `task2.Tests` | unit-тести сервісів і валідаторів |

## Ендпоінти

Усі, окрім `/api/auth/*`, потребують заголовка `Authorization: Bearer <token>`.

| Метод | URL | Опис |
|---|---|---|
| POST | `/api/auth/register` | реєстрація, повертає JWT |
| POST | `/api/auth/login` | вхід за логіном або email, повертає JWT |
| GET | `/api/users` | список користувачів |
| GET | `/api/users/{id}` | користувач за Id |
| PUT | `/api/users/{id}` | редагувати власний профіль |
| DELETE | `/api/users/{id}` | видалити власний акаунт |
| POST | `/api/group` | створити групу (автор стає `Admin`) |
| GET | `/api/group` | мої групи |
| POST | `/api/group/{id}/members` | додати учасника (лише `Admin`) |
| DELETE | `/api/group/{id}/members/{userId}` | видалити учасника (лише `Admin`) |
| GET | `/api/expenses` | мої витрати |
| GET | `/api/expenses/{id}` | витрата за Id |
| GET | `/api/expenses/group/{groupId}` | витрати групи |
| POST | `/api/expenses` | створити витрату з розподілом |
| DELETE | `/api/expenses/{id}` | видалити витрату |
| GET | `/api/balance/group/{groupId}` | баланси учасників групи |
| GET | `/api/balance/group/{groupId}/settlements` | хто кому скільки винен |
| POST | `/api/settlements` | записати розрахунок |

## Формат помилок

Усі помилки повертаються як `ProblemDetails`:

```json
{
  "status": 404,
  "title": "Витрату не знайдено.",
  "instance": "/api/expenses/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

| Код | Коли |
|---|---|
| 400 | некоректні дані, не сходиться розподіл, платник чи учасник не в групі |
| 401 | немає токена, токен недійсний, неправильний пароль |
| 403 | користувач не учасник групи або не має прав на дію |
| 404 | ресурс не знайдено |
| 409 | конфлікт (наприклад, учасник уже в групі) |

## Запуск локально

Потрібні [.NET 10 SDK](https://dotnet.microsoft.com/download) і PostgreSQL. Базу найпростіше підняти в Docker:

```bash
docker run --name splitwise-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=splitwise \
  -p 5432:5432 -d postgres:16
```

Рядок підключення за замовчуванням (`appsettings.json`) уже відповідає цій базі. Секретний ключ JWT у репозиторії не зберігається, задайте його через user-secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "довільний-секрет-щонайменше-32-символи"
dotnet run
```

Міграції застосовуються автоматично при старті. Порт береться зі змінної `PORT` (за замовчуванням 8080), Swagger відкриється на `http://localhost:8080/swagger`.

### Запуск у Docker

```bash
docker build -t splitwise-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=splitwise;Username=postgres;Password=postgres" \
  -e Jwt__Key="довільний-секрет-щонайменше-32-символи" \
  splitwise-api
```

## Конфігурація

| Змінна середовища | Призначення |
|---|---|
| `ConnectionStrings__DefaultConnection` | рядок підключення до PostgreSQL |
| `Jwt__Key` | секрет підпису токенів (щонайменше 32 символи), **обов'язковий** |
| `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiresMinutes` | параметри токена (є значення за замовчуванням) |
| `Cors__AllowedOrigins__0` | дозволений origin фронтенду (так само `__1`, `__2` ...) |
| `PORT` | порт, на якому слухає застосунок (за замовчуванням 8080) |

Секрети (ключ JWT, паролі бази) не потрапляють у git: локально це `dotnet user-secrets`, на сервері змінні середовища.

## Тести

```bash
dotnet test task2.sln
```

Unit-тести покривають усі сервіси та валідатори: розподіл витрат без втрати копійок, перевірки доступу до груп, баланси (сума балансів у групі дорівнює нулю), обробку помилок.

## CI/CD

Pipeline `.github/workflows/dotnet-ci.yml` на кожен push і pull request у `main` виконує restore, build, тести та `docker build`. Після push у `main` і зелених тестів запускається деплой на Render через deploy hook (секрет `DEPLOY_HOOK_URL`).

## UML-діаграми

Лежать у папці [`UML diagrams`](UML%20diagrams).

**Use case**

![Use case](UML%20diagrams/Use%20case.png)

**Реєстрація та вхід**

![Реєстрація та вхід](UML%20diagrams/SequenceDiagram_Auth.png)

**Додавання витрати**

![Додавання витрати](UML%20diagrams/SequenceDiagram_Expense.png)

**Баланси та розрахунки**

![Баланси та розрахунки](UML%20diagrams/SequenceDiagram_Balance.png)
