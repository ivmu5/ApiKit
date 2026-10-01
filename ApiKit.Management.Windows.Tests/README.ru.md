# ApiKit.Management.Windows.Tests

[English](README.md) | Русский

Интеграционные xUnit-тесты для Windows: настоящая аутентификация Named Pipes, делегирование полномочий, метаданные изоляции, восстановление пакетов и обнаружение сервисов между несколькими процессами. Обычный набор не требует доступа к SCM или прав администратора.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · **ApiKit.Management.Windows.Tests** · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Покрытие](#покрытие)
- [Архитектура тестов](#архитектура-тестов)
- [Запуск](#запуск)
- [Границы проверки](#границы-проверки)
- [Точки расширения](#точки-расширения)
- [Зависимости](#зависимости)

## Покрытие

| Набор | Проверяемое поведение |
|---|---|
| [NamedPipeHandshakeTests](Security/NamedPipeHandshakeTests.cs) | Взаимный proof, поддельные identities, replay при новом соединении, ошибочная версия wire-протокола, зависший handshake и framing. |
| [ManagementAuthorizationTests](Security/ManagementAuthorizationTests.cs) и [DelegationSecurityTests](Security/DelegationSecurityTests.cs) | Границы политик и отказ при отсутствующей либо неадминистративной делегации. |
| [MultiProcessManagementTests](Security/MultiProcessManagementTests.cs) | Host, Admin, два сервиса, поддельный Admin, недоверенный сервис и отзыв регистрации в реальных процессах. |
| [PackageStorageTests](Installer/PackageStorageTests.cs) | Проверка пакетов, staging, запрет symlink, файловый rollback и изоляция версий. |
| [IsolationRegistryTests](Installer/IsolationRegistryTests.cs) | Сохранённые SID-записи и неверные/несовпадающие метаданные изоляции. |

```text
xUnit test → isolated test fixture → real pipe endpoints or package staging
                 │
                 └─ multi-process test → TestWorker host + services + admin
```

## Архитектура тестов

[`WindowsPeerFixture`](Support/WindowsPeerFixture.cs) подготавливает доверенные тестовые identities в изолированном хранилище ключей. Атрибут `[WindowsFact]` из [`WindowsFactAttribute`](Support/WindowsFactAttribute.cs) пропускает реальные OS-интеграционные сценарии вне Windows. Набор с несколькими процессами запускает [TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) и использует отдельное временное состояние; он не запускает привилегированные тесты Windows SCM.

## Запуск

В Windows после восстановления CryptoKit выполните команду из корня:

```powershell
dotnet test ApiKit.Management.Windows.Tests/ApiKit.Management.Windows.Tests.csproj -c Release
```

Также можно использовать корневой скрипт `Run-Stage10.ps1`. Проект ориентирован на `net10.0-windows`; реальные сценарии `[WindowsFact]` вне поддерживаемой платформы пропускаются, а не считаются успешными. Исполняемый TestWorker подключён ссылкой на проект; для диагностики ошибок запуска его можно собрать отдельно.

## Границы проверки

Эти тесты работают с настоящими Named Pipes и изолированной файловой структурой пакетов. Они **не** моделируют сбой после каждого шага перенастройки SCM и не подтверждают полный rollback SCM. Создание реальных служб, запуск/остановка, изменения ACL и проверка SID вынесены в [отдельный привилегированный набор](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md), который требует явного запуска.

## Точки расширения

Изменения протокола требуют тестов handshake и framing; изменения авторизации/делегации относятся к соответствующим наборам `Security/`. Новые инварианты пакетов/rollback проверяйте в `PackageStorageTests`, сохранение SID — в `IsolationRegistryTests`, межпроцессные предположения — в `MultiProcessManagementTests`. Логика тестового worker находится в [TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md), а не в рабочем Host.

## Зависимости

Зависит от [ядра](../ApiKit/README.ru.md), [Windows-реализации](../ApiKit.Management.Windows/README.ru.md), [адаптера CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md) и [TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md), а также xUnit и coverlet из [файла проекта](ApiKit.Management.Windows.Tests.csproj).
