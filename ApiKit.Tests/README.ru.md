# ApiKit.Tests

[English](README.md) | Русский

Кроссплатформенные xUnit-тесты независимых от платформы компонентов ApiKit: Management security, канонической нагрузки challenge, permissions и границы авторизации при переходе из Management в MVC.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: **ApiKit.Tests** · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Покрытие](#покрытие)
- [Архитектура тестов](#архитектура-тестов)
- [Запуск](#запуск)
- [Точки расширения](#точки-расширения)
- [Зависимости](#зависимости)

## Покрытие

| Набор | Проверяемое поведение |
|---|---|
| [ChallengeProviderTests](Security/ChallengeProviderTests.cs) | Одноразовое и конкурентное потребление challenge, истечение, защитные копии, идентификаторы и отмена. |
| [CanonicalProofPayloadTests](Security/CanonicalProofPayloadTests.cs) | Стабильное кодирование полей, подписанный контекст, сравнение identities, отклонение неоднозначных или неверных значений. |
| [AuthorizationPolicyTests](Security/AuthorizationPolicyTests.cs) | Точные permissions, аутентифицированные principals и разделение буквальных permissions и обычных политик ASP.NET. |
| [ManagementBridgePrincipalTests](Security/ManagementBridgePrincipalTests.cs) | Запрет синтетических разрешений CRUD для неадминистраторов и анонимных вызывающих сторон. |

## Архитектура тестов

Тесты используют DI внутри процесса и прямые вызовы компонентов ядра без Windows SCM и внешней CryptoKit. [`ManualTimeProvider`](Support/ManualTimeProvider.cs) детерминированно перемещает время для проверки истечения challenge и граничных условий. Тесты MVC-моста проверяют реальное решение dispatcher/authorization, а не заведомо разрешающий mock policy provider.

## Запуск

Из корня репозитория при наличии .NET 10:

```powershell
dotnet test ApiKit.Tests/ApiKit.Tests.csproj -c Release
```

Проект не требует повышенных прав. Ориентируйтесь на итоговый отчёт xUnit: наличие исходников или успешная компиляция не подтверждают прохождение assertions.

## Точки расширения

Добавляйте проверки рядом с соответствующей границей: соглашения авторизации — в `AuthorizationPolicyTests`, переходы состояния challenge — в `ChallengeProviderTests`, поля подписанной нагрузки — в `CanonicalProofPayloadTests`, principal и делегирование Management — в `ManagementBridgePrincipalTests`. Изменяйте [реализацию ApiKit](../ApiKit/README.ru.md) только при выявлении ошибки продукта.

## Зависимости

Зависит от [ApiKit](../ApiKit/README.ru.md), xUnit, Microsoft.NET.Test.Sdk и coverlet; см. [ApiKit.Tests.csproj](ApiKit.Tests.csproj) и [общие сведения о лицензиях](../README.ru.md#зависимости-и-лицензии-сторонних-компонентов).
