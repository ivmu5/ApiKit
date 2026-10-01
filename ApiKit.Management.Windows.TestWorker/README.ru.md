# ApiKit.Management.Windows.TestWorker

[English](README.md) | Русский

Только тестовое исполняемое приложение для Windows, используемое проверками нескольких процессов через Named Pipes и привилегированными интеграционными тестами SCM. Содержит небольшие роли Host, Service, Admin и SCM и не является образцом рабочего развёртывания.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · **ApiKit.Management.Windows.TestWorker** · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Роли процессов](#роли-процессов)
- [Путь выполнения](#путь-выполнения)
- [Вызов](#вызов)
- [Тестирование и изменения](#тестирование-и-изменения)
- [Зависимости](#зависимости)

## Роли процессов

| Роль | Поведение |
|---|---|
| `host <settings.json>` | Запускает Management Host с изолированным тестовым crypto storage и ожидает команды в консоли. |
| `service <settings.json>` | Запускает сервис с тестовыми credentials, регистрируется и ожидает команды в консоли. |
| `admin <settings.json>` | Подключается к Host, запрашивает видимые сервисы и выводит JSON в строке `RESULT:`. |
| `scm <service-name>` | Запускает минимальный хост .NET Windows Service для настоящих тестов SCM. |

## Путь выполнения

[`Program.Main`](Program.cs) выбирает роль по аргументам командной строки. Роли `host`, `service` и `admin` загружают [`WorkerSettings`](Program.cs), регистрируют настоящие CryptoKit-провайдеры с отдельным storage для каждой роли и соответствующий Windows-транспорт ApiKit. Фикстуры нескольких процессов создают и распространяют тестовые ключи/доверие **до** запуска; worker намеренно использует `createIfMissing: false`. Host и Service выводят `READY`, затем ожидают ввод консоли до команды `STOP`. Роль `admin` завершается после запроса.

## Вызов

Обычно интеграционные наборы запускают этот executable самостоятельно. Для ручной отладки подготовленной фикстуры:

```powershell
# Run from the repository root after building and provisioning test identities.
dotnet run --project ApiKit.Management.Windows.TestWorker -- host path/to/settings.json
dotnet run --project ApiKit.Management.Windows.TestWorker -- service path/to/settings.json
dotnet run --project ApiKit.Management.Windows.TestWorker -- admin path/to/settings.json
```

В `settings.json` необходимо передать поля `WorkerSettings` с совпадающими именами JSON-свойств, например:

```json
{
  "StorageDirectory": "C:\\test\\host-keys",
  "PeerId": "management-host",
  "HostPeerId": "management-host",
  "RegistrationPipeName": "apikit-test.registration",
  "AdminPipeName": "apikit-test.admin",
  "ServicePipePrefix": "apikit-test.service",
  "InstallRoot": "C:\\test\\installed-services",
  "InstanceId": null
}
```

Этот JSON показывает **структуру**, но не содержит подготовленного доверия. Не используйте тестовые пути или credentials в рабочем развёртывании. Роль `scm` должна запускаться только отдельным, явно разрешённым набором привилегированных тестов.

## Тестирование и изменения

Этот executable используют [тесты Windows с несколькими процессами](../ApiKit.Management.Windows.Tests/README.ru.md) и [привилегированные SCM-тесты](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md). Для диагностики запуска отдельно соберите проект:

```powershell
dotnet build ApiKit.Management.Windows.TestWorker/ApiKit.Management.Windows.TestWorker.csproj -c Release
```

При изменении маркеров вывода, аргументов или `WorkerSettings` обновляйте соответствующие тесты запуска процессов. Ослабленные для тестов настройки безопасности должны оставаться только внутри этого тестового приложения.

## Зависимости

Ориентирован на `net10.0-windows`, ссылается на три прикладных проекта ApiKit, транзитивно использует CryptoKit через адаптер и Microsoft Windows Service hosting. См. [файл проекта](ApiKit.Management.Windows.TestWorker.csproj).
