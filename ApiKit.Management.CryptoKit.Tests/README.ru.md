# ApiKit.Management.CryptoKit.Tests

[English](README.md) | Русский

Кроссплатформенные интеграционные xUnit-тесты адаптера ApiKit/CryptoKit с настоящей генерацией RSA-ключей и криптографическими операциями в изолированном in-memory key-storage окружении.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · **ApiKit.Management.CryptoKit.Tests** · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Покрытие](#покрытие)
- [Архитектура тестов](#архитектура-тестов)
- [Запуск](#запуск)
- [Точки расширения](#точки-расширения)
- [Зависимости](#зависимости)

## Покрытие

| Набор | Проверяемое поведение |
|---|---|
| [CryptoProofTests](Security/CryptoProofTests.cs) | Проверка настоящих RSA-PSS proof, изменение подписанного контекста, неизвестные credentials, сроки/skew и несовпадение identity. |
| [CredentialRotationTests](Security/CredentialRotationTests.cs) | Сохранение активного credential, overlap/истечение, отзыв/повторное доверие, изоляция key ID и неверные доверенные ключи. |
| [ProvisioningTests](Security/ProvisioningTests.cs) | Отдельные ключи сервисов, идемпотентный provisioning, отзыв доверия/deprovisioning и запрет небезопасных путей. |

## Архитектура тестов

[`CryptoFixture`](Support/CryptoFixture.cs) регистрирует настоящие RSA-провайдеры CryptoKit поверх тестовой in-memory реализации [`IKeyStorage`](Support/InMemoryKeyStorage.cs). Это позволяет проверять генерацию ключей и подпись без записи рабочих credentials и без Windows-служб. Тесты сохранения состояния проверяют восстановление после повторного создания компонентов на том же тестовом storage, а не восстановление после физических сбоев диска.

## Запуск

Инициализируйте субмодуль CryptoKit и запустите тесты из корня:

```powershell
git submodule update --init --recursive
dotnet test ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj -c Release
```

SCM и повышенные права не требуются. Успешное прохождение тестов не подтверждает безопасность конкурентных записей в общее рабочее хранилище credentials.

## Точки расширения

Новые поля proof следует тестировать в `CryptoProofTests`; переходы локального состояния и trust — в `CredentialRotationTests`; изменения identities или каталогов installer provisioning — в `ProvisioningTests`. Не изменяйте реализацию внешней [CryptoKit](../external/CryptoKit/README.ru.md) без отдельного согласования её интерфейса.

## Зависимости

Зависит от [ApiKit](../ApiKit/README.ru.md) и [адаптера CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md); xUnit, Microsoft.NET.Test.Sdk и coverlet указаны в [файле проекта](ApiKit.Management.CryptoKit.Tests.csproj).
