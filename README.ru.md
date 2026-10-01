# ApiKit

[English](README.md) | Русский

ApiKit — набор компонентов .NET 10 для ASP.NET Core API и управления локальными микросервисами Windows. Он объединяет JWT-аутентификацию, CRUD на EF Core с permissions и независимые от платформы Management-контракты с дополнительным Windows-транспортом и отдельным адаптером управления ключами CryptoKit.

## Содержание

- [Основные возможности](#основные-возможности)
- [Проекты](#проекты)
- [Архитектура](#архитектура)
- [Начало работы](#начало-работы)
- [Безопасность и жизненный цикл](#безопасность-и-жизненный-цикл)
- [Тестирование](#тестирование)
- [Точки изменения и расширения](#точки-изменения-и-расширения)
- [Зависимости и лицензии сторонних компонентов](#зависимости-и-лицензии-сторонних-компонентов)

## Основные возможности

ApiKit позволяет выпускать и проверять JWT; применять ресурсные или буквальные permissions ASP.NET Core; создавать CRUD на EF Core с DTO, пагинацией и JSON Patch; публиковать Management discovery и собственные операции; а при подключении необязательных проектов — аутентифицировать локальные Windows-микросервисы и управлять их установкой с отдельными credentials и изоляцией средствами ОС.

## Проекты

Три библиотеки содержат код для использования в приложениях. Ещё пять собственных проектов предназначены для тестирования или обеспечивают работу тестового процесса. У каждого проекта есть самостоятельная документация на двух языках.

| Проект | Назначение |
|---|---|
| [ApiKit](ApiKit/README.ru.md) | Независимые от платформы компоненты ASP.NET Core и контракты Management. |
| [ApiKit.Management.Windows](ApiKit.Management.Windows/README.ru.md) | Windows-транспорт, Host/Client, установщик служб и изоляция. |
| [ApiKit.Management.CryptoKit](ApiKit.Management.CryptoKit/README.ru.md) | RSA-PSS для Management, credentials, trust и provisioning через CryptoKit. |
| [ApiKit.Tests](ApiKit.Tests/README.ru.md) | Тесты основной авторизации, challenge и канонической нагрузки. |
| [ApiKit.Management.CryptoKit.Tests](ApiKit.Management.CryptoKit.Tests/README.ru.md) | Тесты RSA, доверия, ротации и provisioning. |
| [ApiKit.Management.Windows.Tests](ApiKit.Management.Windows.Tests/README.ru.md) | Тесты Named Pipes, авторизации, пакетов и нескольких процессов. |
| [ApiKit.Management.Windows.TestWorker](ApiKit.Management.Windows.TestWorker/README.ru.md) | Отдельный процесс Host, Service, Admin или SCM для интеграционных тестов. |
| [ApiKit.Management.Windows.PrivilegedTests](ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | Отдельно разрешаемые тесты Windows SCM и ACL. |

[CryptoKit](external/CryptoKit/README.ru.md) — самостоятельный субмодуль с отдельной [английской документацией](external/CryptoKit/README.md) и [лицензией](external/CryptoKit/LICENSE.txt). Его исходники не входят в область изменения ApiKit.

## Архитектура

ApiKit не зависит ни от Windows, ни от CryptoKit. Windows-библиотека зависит от ApiKit; адаптер зависит от ApiKit и независимого субмодуля CryptoKit. Приложение объединяет компоненты через внедрение зависимостей.

```text
ASP.NET Core application ──► ApiKit (JWT / policies / EF Core CRUD)
          │                         │
          └─ management contracts ◄─┘
                    │
         ┌──────────┴───────────┐
         ▼                      ▼
ApiKit.Management.Windows  ApiKit.Management.CryptoKit
  pipes / SCM / ACL          management RSA proofs / trust
                                  │
                             external/CryptoKit
                             RSA key storage/lifecycle
```

```text
ApiKit                    platform-independent ASP.NET Core and management contracts
ApiKit.Management.Windows Windows Named Pipes and Windows service lifecycle
ApiKit.Management.CryptoKit  bridge to external/CryptoKit
ApiKit.*Tests             core, adapter, Windows and privileged test suites
ApiKit.Management.Windows.TestWorker  isolated multi-process test executable
external/CryptoKit        independent Git submodule (own documentation)
scripts                   opt-in test runners and comment-language check
```

<details>
<summary>Путь Management-запроса и границы безопасности</summary>

Административный клиент проверяет Management Host и подтверждает собственную идентичность. Host проверяет отправителя до применения политик Management из ASP.NET Core. При перенаправлении команды Host и целевой сервис выполняют отдельное аутентифицированное соединение Named Pipe; сервис проверяет делегированную идентичность администратора и выполняет собственную авторизацию. Регистрация сервисов и heartbeat используют отдельный канал. Константа версии протокола — **6**; ACL операционной системы ограничивают доступ, но не заменяют криптографическую проверку.

</details>

## Начало работы

Установите **.NET 10 SDK** и инициализируйте субмодуль CryptoKit, если используете адаптер или собираете всё решение:

```bash
git submodule update --init --recursive
```

Для обычного API подключите необходимые проекты или пакеты и зарегистрируйте только используемые компоненты:

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

`AppDbContext` — ваш собственный контекст EF Core. Настройте `ConnectionStrings:DefaultConnection`, `Jwt:Generation`, `Jwt:Validation` и `Jwt:Hmac`; не сохраняйте секреты подписи в репозитории. При асимметричной подписи приложение может реализовать [`IJwtSigningCredentialsProvider`](ApiKit/Authentication/Abstractions/IJwtSigningCredentialsProvider.cs) и [`IJwtValidationKeysProvider`](ApiKit/Authentication/Abstractions/IJwtValidationKeysProvider.cs) вместо HMAC-провайдера. Windows Management дополнительно требует настройки криптографических identities и доверия; см. документацию [Windows](ApiKit.Management.Windows/README.ru.md) и [адаптера](ApiKit.Management.CryptoKit/README.ru.md).

<details>
<summary>Регистрация Management в Windows-сервисе</summary>

```csharp
using ApiKit.Management;
using ApiKit.Management.Windows;

// In a managed service; security credentials and trust must be configured separately.
builder.Services.AddApiKitManagement(options =>
{
    options.ServiceName = "messages";
    options.DisplayName = "Messages API";
}).AddWindowsNamedPipeTransport();
```

Для Host зарегистрируйте `AddApiKitWindowsManagementHost`, для административного клиента — `AddApiKitWindowsManagementClient`. Сами по себе эти вызовы не устанавливают доверенный обмен ключами.

</details>

## Безопасность и жизненный цикл

[ManagementChallengeProofPayload](ApiKit/Management/Security/ManagementChallengeProofPayload.cs) формирует канонические данные challenge с привязкой к идентичности и контексту. [WindowsNamedPipeChallengeResponseProtocol](ApiKit.Management.Windows/Internal/WindowsNamedPipeChallengeResponseProtocol.cs) потребляет одноразовые challenge в рамках одного соединения. [ICryptoKitManagementCredentialRotationManager](ApiKit.Management.CryptoKit/Security/ICryptoKitManagementCredentialRotationManager.cs) управляет сохраняемыми состояниями активации и retirement локальных credentials отдельно от доверенных публичных ключей удалённых сторон. Установщик Management может выпускать отдельный ключ каждому сервису и настраивать Windows service SID и ACL каталогов.

Успешная проверка challenge не обеспечивает идемпотентность бизнес-операций. При прерывании обновления Windows SCM также **не гарантируется** полное восстановление прежних настроек SCM. Границы развёртывания и восстановления подробно описаны в [SECURITY_AUDIT.md](SECURITY_AUDIT.md).

## Тестирование

В репозитории используется xUnit. Обычные наборы запускаются через PowerShell-скрипт (специфичные для Windows тесты выполняются только в Windows):

```powershell
pwsh -File ./scripts/Run-Stage10.ps1
```

Для проверки установки, SCM и ACL необходима **изолированная тестовая Windows VM с повышенными правами** и явное разрешение:

```powershell
pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges
```

Вторая команда действительно изменяет состояние операционной системы. Назначение тестов, зависимости и правила пропуска см. в [TESTING.md](TESTING.md) и README соответствующих тестовых проектов. Само наличие тестов не означает, что все они успешно выполнены.

## Точки изменения и расширения

| Задача | С чего начать |
|---|---|
| Изменить проверку JWT или провайдеры подписи | [JWT DI](ApiKit/Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs), [контракты провайдеров](ApiKit/Authentication/Abstractions/IJwtValidationKeysProvider.cs) |
| Изменить имена permissions или внедрение политик CRUD | [PermissionNameResolver](ApiKit/Authorization/Permissions/PermissionNameResolver.cs), [CrudPermissionApplicationModelConvention](ApiKit/Crud/Authorization/CrudPermissionApplicationModelConvention.cs), [тесты ядра](ApiKit.Tests/README.ru.md) |
| Добавить Management-операцию | [ApiKitManagementBuilder](ApiKit/Management/Builders/ApiKitManagementBuilder.cs), [IManagementOperation](ApiKit/Management/Abstractions/IManagementOperation.cs) |
| Изменить протокол pipes или установку сервиса | [протокол](ApiKit.Management.Windows/Internal/WindowsNamedPipeChallengeResponseProtocol.cs), [установщик](ApiKit.Management.Windows/Internal/WindowsManagedServiceInstaller.cs), [Windows-тесты](ApiKit.Management.Windows.Tests/README.ru.md) |
| Расширить жизненный цикл credentials | [контракт ротации](ApiKit.Management.CryptoKit/Security/ICryptoKitManagementCredentialRotationManager.cs), [тесты адаптера](ApiKit.Management.CryptoKit.Tests/README.ru.md) |

## Зависимости и лицензии сторонних компонентов

Основные зависимости перечислены в соответствующих `.csproj`; их версии здесь не дублируются. Для существенных внешних компонентов установлены следующие лицензии:

| Зависимость | Назначение | Лицензия исходного проекта |
|---|---|---|
| [CryptoKit](external/CryptoKit/LICENSE.txt) | Внешний субмодуль управления RSA-ключами | MIT; при распространении копий необходимо сохранять указанные в лицензии copyright и текст лицензии. |
| [Npgsql EF Core provider](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3) | Интеграция PostgreSQL | PostgreSQL License. |
| [Microsoft ASP.NET Core JWT bearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.12) | JWT bearer-аутентификация | MIT. |
| [Microsoft ASP.NET Core JSON Patch](https://www.nuget.org/packages/Microsoft.AspNetCore.JsonPatch.SystemTextJson/10.0.12) | JSON Patch | MIT. |
| [xUnit](https://www.nuget.org/packages/xunit/2.9.3) и [его runner](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.4) | Тестирование | Apache-2.0. |
| [coverlet.collector](https://www.nuget.org/packages/coverlet.collector/6.0.4) | Сбор покрытия тестов | MIT. |

При распространении программ со сторонними бинарными компонентами проверьте лицензии точного набора разрешённых NuGet-зависимостей, включая транзитивные, и включите в поставку обязательные тексты лицензий, copyright и NOTICE. Лицензия самостоятельного субмодуля CryptoKit не определяет автоматически лицензию остальной части ApiKit; этот README не назначает лицензию ApiKit.
