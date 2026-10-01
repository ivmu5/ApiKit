# ApiKit

[English](README.md) | Русский

Независимая от платформы библиотека .NET 10 для JWT-аутентификации, permissions ASP.NET Core, CRUD на EF Core и Management-контрактов. Основная сборка использует ASP.NET Core, но не ссылается на Windows API или адаптер CryptoKit.

[Обзор репозитория](../README.ru.md) | Библиотеки: **ApiKit** · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Основные возможности](#основные-возможности)
- [Архитектура](#архитектура)
- [Публичный API](#публичный-api)
- [Жизненный цикл и выполнение](#жизненный-цикл-и-выполнение)
- [Тестирование](#тестирование)
- [Важные особенности](#важные-особенности)
- [Точки изменения и расширения](#точки-изменения-и-расширения)
- [Зависимости](#зависимости)

## Основные возможности

- Настраивать выпуск JWT и проверку bearer-токенов с HMAC-ключами или заменяемыми провайдерами подписи и проверки.
- Применять буквальные и привязанные к ресурсу permissions через стандартные политики ASP.NET Core.
- Создавать CRUD-контроллеры EF Core с DTO, пагинацией и JSON Patch на System.Text.Json.
- Регистрировать PostgreSQL `DbContext` через EF Core/Npgsql.
- Объявлять сервисы, обнаруживать HTTP endpoints и CRUD-ресурсы, регистрировать собственные Management-операции без зависимости от транспорта.

## Архитектура

Публичная часть разделена по ответственности: [`Authentication`](Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs) регистрирует JWT; [`Authorization`](Authorization/DependencyInjection/AuthorizationServiceCollectionExtensions.cs) использует стандартную авторизацию ASP.NET Core; [`CrudController`](Crud/Controllers/CrudController.cs) предоставляет MVC actions; [`Data`](Data/DependencyInjection/PostgreSqlServiceCollectionExtensions.cs) настраивает Npgsql; [`Management`](Management/DependencyInjection/ManagementServiceCollectionExtensions.cs) содержит независимые от транспорта контракты discovery, dispatch, challenge и установщика.

```text
HTTP request → ASP.NET Core JWT middleware → authorization policy
  → concrete CrudController action → EF Core DbContext → DTO/HTTP response

Management resource call → ManagementResourceDispatcher
  → ASP.NET Core MVC action invoker → ordinary authorization and CRUD pipeline
```

<details>
<summary>Обнаружение Management и мост к MVC</summary>

[`DefaultManagementServiceDescriptorProvider`](Management/Internal/DefaultManagementServiceDescriptorProvider.cs) объединяет данные об HTTP endpoints, адресах сервера, метаданных CRUD и зарегистрированных операциях. [`ManagementRegistrationHostedService`](Management/Internal/ManagementRegistrationHostedService.cs) публикует descriptors и heartbeat через зарегистрированные publishers. [`ManagementResourceDispatcher`](Management/Internal/ManagementResourceDispatcher.cs) вызывает реальные MVC actions, а не дублирует бизнес-логику. Он требует аутентифицированного административного principal перед добавлением строго ограниченного синтетического разрешения CRUD; стандартная MVC-авторизация продолжает действовать. [`InMemoryManagementChallengeProvider`](Management/Internal/InMemoryManagementChallengeProvider.cs) выдаёт и потребляет одноразовые challenge, а конкретную криптографическую подпись предоставляет адаптер.

</details>

## Публичный API

В точке запуска ASP.NET Core зарегистрируйте только необходимые сервисы:

```csharp
using ApiKit.Authentication;
using ApiKit.Authorization;
using ApiKit.Crud.Authorization;
using ApiKit.Data;

builder.Services.AddApiKitJwtHmacKeyProvider(builder.Configuration);
builder.Services.AddApiKitJwtTokenGeneration(builder.Configuration);
builder.Services.AddApiKitJwtBearerAuthentication(builder.Configuration);
builder.Services.AddApiKitAuthorization();
builder.Services.AddApiKitPostgreSqlDbContext<AppDbContext>(builder.Configuration);
builder.Services.AddControllers().AddApiKitCrudPermissions();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

`AppDbContext` — тип приложения, а не часть ApiKit. Для этого примера задайте `ConnectionStrings:DefaultConnection`, `Jwt:Generation`, `Jwt:Validation` и `Jwt:Hmac`. Контракты [`IJwtTokenGenerator`](Authentication/Abstractions/IJwtTokenGenerator.cs), [`IJwtSigningCredentialsProvider`](Authentication/Abstractions/IJwtSigningCredentialsProvider.cs) и [`IJwtValidationKeysProvider`](Authentication/Abstractions/IJwtValidationKeysProvider.cs) позволяют независимо реализовать выпуск и проверку ключей.

<details>
<summary>Generic CRUD и регистрация Management</summary>

```csharp
using ApiKit.Crud.Controllers;

public sealed class ItemsController
    : CrudController<AppDbContext, Item, Guid, ItemReadDto, ItemCreateDto, ItemUpdateDto>
{
    public ItemsController(AppDbContext dbContext) : base(dbContext) { }

    // Override GetReadProjection/MapReadModelAsync and the create/update mapping hooks.
}
```

Реализуйте методы преобразования моделей, необходимые унаследованным actions. Для автоматического применения permissions к CRUD используйте `AddApiKitCrudPermissions()` совместно с `AddApiKitAuthorization()`. Явные атрибуты авторизации action имеют приоритет перед автоматическими соглашениями CRUD.

```csharp
using ApiKit.Management;
using ApiKit.Management.Builders;

var management = services.AddApiKitManagement(options =>
{
    options.ServiceName = "messages";
    options.DiscoverCrudResources = true;
});
management.AddCapability("messages");
```

Builder Management также поддерживает `AddOperation<TOperation, TRequest, TResult>` и `AddPublisher<TPublisher>`. Для межпроцессного взаимодействия отдельно настройте транспорт и криптографические credentials.

</details>

## Жизненный цикл и выполнение

Параметры JWT проверяются при запуске приложения. Контекст EF Core по умолчанию регистрируется как scoped; генератор токенов — transient. Идентичность Management-сервиса и его descriptor формируются зарегистрированными службами; фоновый сервис регистрации управляет heartbeat и отзывом регистрации. In-memory challenge provider является singleton и хранит ограниченное количество challenge с истечением срока. Большинство CRUD-операций и dispatcher асинхронны и принимают `CancellationToken`; приложение самостоятельно обеспечивает корректную конкурентную работу `DbContext` и границы сохранения.

## Тестирование

Сопутствующий проект [ApiKit.Tests](../ApiKit.Tests/README.ru.md) проверяет авторизацию, границы permissions моста MVC, одноразовость и истечение challenge, привязку identities и каноническое кодирование. Запуск из корня репозитория:

```powershell
dotnet test ApiKit.Tests/ApiKit.Tests.csproj -c Release
```

Эти тесты не проверяют Windows-транспорт или внешнее хранилище RSA; для таких сценариев предназначены тестовые проекты адаптера и Windows.

## Важные особенности

Стандартные маршруты и mapping `CrudController` рассчитаны на один первичный ключ EF Core; для других схем переопределяйте нужные hooks или actions. JSON Patch применяется к update-моделям и должен ограничиваться валидацией приложения. Встроенная HMAC-конфигурация требует не менее 32 байт UTF-8; не сохраняйте рабочие секреты в репозитории. Контракты Management сами по себе не обеспечивают безопасность транспорта, а Management CRUD продолжает использовать стандартную авторизацию ASP.NET MVC.

## Точки изменения и расширения

| Изменение | С чего начать |
|---|---|
| Выпуск и проверка JWT | [JwtTokenGenerator](Authentication/Services/JwtTokenGenerator.cs), [JWT DI](Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs) |
| Permissions и типы claims | [Authorization DI](Authorization/DependencyInjection/AuthorizationServiceCollectionExtensions.cs), [PermissionNameResolver](Authorization/Permissions/PermissionNameResolver.cs) |
| CRUD mapping/query/patch | [CrudController.Mapping](Crud/Controllers/CrudController.Mapping.cs), [CrudController.Query](Crud/Controllers/CrudController.Query.cs), [CrudController.Patch](Crud/Controllers/CrudController.Patch.cs) |
| Обнаружение и вызов ресурсов | [CrudResourceDescriptorContributor](Management/Internal/CrudResourceDescriptorContributor.cs), [ManagementResourceDispatcher](Management/Internal/ManagementResourceDispatcher.cs) |
| Собственные операции и контракты транспорта | [ApiKitManagementBuilder](Management/Builders/ApiKitManagementBuilder.cs), [IManagementPeerAuthenticator](Management/Abstractions/IManagementPeerAuthenticator.cs) |

## Зависимости

Проект напрямую использует ASP.NET Core, EF Core, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.JsonPatch.SystemTextJson` и `Microsoft.AspNetCore.Authentication.JwtBearer`, `System.IdentityModel.Tokens.Jwt`; точные версии указаны в [ApiKit.csproj](ApiKit.csproj). См. [общие сведения о зависимостях и лицензиях](../README.ru.md#зависимости-и-лицензии-сторонних-компонентов).
