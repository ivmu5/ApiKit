# ApiKit.Management.Windows

[English](README.md) | Русский

Windows-компоненты Management для .NET 10: Named Pipes, защищённая регистрация сервисов, управление установкой в SCM и контроль доступа к каталогам и каналам. Реализация использует независимые от транспорта контракты ApiKit.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · **ApiKit.Management.Windows** · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Основные возможности](#основные-возможности)
- [Архитектура](#архитектура)
- [Публичный API](#публичный-api)
- [Состояние и параллелизм](#состояние-и-параллелизм)
- [Тестирование](#тестирование)
- [Важные ограничения](#важные-ограничения)
- [Точки изменения и расширения](#точки-изменения-и-расширения)
- [Зависимости](#зависимости)

## Основные возможности

- Запускать аутентифицированный Management Host и подключать административные клиенты и управляемые сервисы через Named Pipes.
- Регистрировать сервисы с heartbeat и перенаправлять авторизованные операции и ресурсы.
- Устанавливать, обновлять, запускать, останавливать и удалять Windows-службы с проверкой пакета и идентичности.
- Создавать отдельные service SID и настраивать изоляцию через ACL каталогов и Named Pipes.

## Архитектура

[`WindowsManagementServiceCollectionExtensions`](DependencyInjection/WindowsManagementServiceCollectionExtensions.cs) регистрирует три роли процессов: управляемый сервис, Management Host и Admin Client. Реализация использует [`WindowsNamedPipeChallengeResponseProtocol`](Internal/WindowsNamedPipeChallengeResponseProtocol.cs) и независимые от платформы контракты проверки сторон. Установщик использует [`WindowsManagedServiceInstaller`](Internal/WindowsManagedServiceInstaller.cs), [`WindowsServicePackageStorage`](Internal/WindowsServicePackageStorage.cs) и [`WindowsManagedServiceIsolation`](Internal/WindowsManagedServiceIsolation.cs).

```text
Admin Client ──mutual RSA auth──► Management Host
                                    │ verified admin + ASP.NET policies
                                    ├─ registration / heartbeat registry
                                    ├─ package verification → provisioning → SCM
                                    └─mutual RSA auth──► Managed Service
                                                        verified delegation → MVC
```

<details>
<summary>Путь аутентификации и авторизации</summary>

Admin Client проверяет credential Host до отправки собственного proof. Host проверяет администратора и применяет авторизацию ASP.NET. Для команд сервиса Host устанавливает отдельное аутентифицированное соединение с конкретным зарегистрированным сервисом/экземпляром и передаёт проверенную делегацию администратора. Регистрация, отзыв регистрации и heartbeat используют отдельный канал. Версия wire-протокола — 6; ACL файлов и каналов дополняют взаимную криптографическую аутентификацию, а не заменяют её.

</details>

## Публичный API

В приложении управляемого сервиса подключите Management-транспорт:

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

Для остальных ролей:

```csharp
using ApiKit.Management.Windows;

// Configure IManagementCredentialProvider and IManagementPeerVerifier separately.
services.AddApiKitWindowsManagementHost();
```

```csharp
using ApiKit.Management.Windows;

// Configure local credentials and trust for the management host separately.
services.AddApiKitWindowsManagementClient();
```

Используйте [`WindowsManagementServiceOptions`](Options/WindowsManagementServiceOptions.cs), [`WindowsManagementHostOptions`](Options/WindowsManagementHostOptions.cs), [`WindowsManagementClientOptions`](Options/WindowsManagementClientOptions.cs) и [`WindowsServiceManagementOptions`](Options/WindowsServiceManagementOptions.cs), чтобы согласовать peer IDs, имена каналов, каталоги установки и изоляцию. Настройте локальные credentials, доверенные публичные ключи других сторон и необходимые package verifier/provisioner **до** приёма Management-запросов.

<details>
<summary>Контракты установщика и администрирования</summary>

Host регистрирует реализации [`IManagedServiceInstaller`](../ApiKit/Management/Abstractions/IManagedServiceInstaller.cs) и [`IManagedServiceLifecycleController`](../ApiKit/Management/Abstractions/IManagedServiceLifecycleController.cs); административный клиент предоставляет эти же контракты как удалённые вызовы. Параметры Host `RequirePackageVerifier` и `RequireCredentialProvisioner` по умолчанию равны `true`. Подключайте доверенную проверку пакетов и provisioning, а не отключайте эти ограничения.

</details>

## Состояние и параллелизм

Host владеет in-memory registry; установщик управляет пакетами по версиям и постоянным состоянием безопасности `.management`; реестр изоляции хранит данные выделенных service SID. Серверы Named Pipe ограничивают активные обработчики соединений и время handshake/чтения первого запроса; срок первичного чтения не прерывает выполнение легитимной длительной операции. Доверие к credentials и жизненный цикл ключей обеспечивает отдельно настроенный адаптер безопасности. Установку и обновления выполняйте с явным контролем единственного изменяющего процесса.

## Тестирование

[Windows-интеграционные тесты](../ApiKit.Management.Windows.Tests/README.ru.md) проверяют настоящие Named Pipes, отказ при ошибках аутентификации, данные изоляции сервисов, подготовку пакетов и отдельные рабочие процессы. [Привилегированные тесты](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) выполняют реальные операции SCM и ACL и намеренно исключены из обычного запуска.

```powershell
pwsh -File ./scripts/Run-Stage10.ps1
```

Эту команду необходимо выполнять из корня репозитория в Windows, чтобы запустить полный набор без повышенных прав. Запуск с повышенными правами описан в [TESTING.md](../TESTING.md).

## Важные ограничения

Сборка ориентирована на `net10.0-windows`. Установщик умеет откатывать файловые изменения, но **не гарантирует транзакционное восстановление конфигурации Windows SCM** после каждого частичного сбоя. При отключённой SID-изоляции проверка принадлежности существующей службы слабее; не отключайте изоляцию в обычных развёртываниях. Повторная аутентификация запрещает использование старого handshake proof, но не предотвращает два отдельно авторизованных одинаковых вызова. Перед развёртыванием изучите [аудит безопасности](../SECURITY_AUDIT.md).

## Точки изменения и расширения

| Изменение | С чего начать |
|---|---|
| Формат pipe, аутентификация и привязка identity | [Протокол](Internal/WindowsNamedPipeProtocol.cs), [challenge-response](Internal/WindowsNamedPipeChallengeResponseProtocol.cs), [тесты](../ApiKit.Management.Windows.Tests/Security/NamedPipeHandshakeTests.cs) |
| Host и делегирование команд | [Сервер Host](Internal/WindowsNamedPipeManagementHostServer.cs), [сервер сервиса](Internal/WindowsNamedPipeServiceServer.cs), [тесты делегации](../ApiKit.Management.Windows.Tests/Security/DelegationSecurityTests.cs) |
| Установка, обновление, восстановление | [Установщик](Internal/WindowsManagedServiceInstaller.cs), [хранилище пакетов](Internal/WindowsServicePackageStorage.cs), [тесты](../ApiKit.Management.Windows.Tests/Installer/PackageStorageTests.cs) |
| ACL и структура SID | [Изоляция](Internal/WindowsManagedServiceIsolation.cs), [реестр изоляции](Internal/WindowsManagedServiceIsolationRegistry.cs), [привилегированные тесты](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) |

## Зависимости

Зависит от [ApiKit](../ApiKit/README.ru.md), общей платформы ASP.NET Core, Windows Service hosting и пакетов безопасности платформы из [файла проекта](ApiKit.Management.Windows.csproj). Прямой зависимости от CryptoKit нет; приложения при необходимости подключают [адаптер](../ApiKit.Management.CryptoKit/README.ru.md). См. [лицензии сторонних компонентов](../README.ru.md#зависимости-и-лицензии-сторонних-компонентов).
