# ApiKit.Management.Windows.PrivilegedTests

[English](README.md) | Русский

Отдельные интеграционные тесты с явным разрешением администратора Windows: создают и изменяют реальные службы SCM и проверяют service SID/ACL. Этот набор намеренно исключён из всех обычных модульных и интеграционных прогонов.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.ru.md)

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · **ApiKit.Management.Windows.PrivilegedTests** | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Область проверки](#область-проверки)
- [Защитный механизм](#защитный-механизм)
- [Запуск](#запуск)
- [Ограничения и точки расширения](#ограничения-и-точки-расширения)
- [Зависимости](#зависимости)

## Область проверки

[`WindowsScmInstallerTests`](WindowsScmInstallerTests.cs) проверяет запрет действий с посторонней службой, а также привилегированный сценарий установки, запуска, остановки, обновления, удаления и SID-изоляции. Набор использует случайное имя службы `ApiKitTest*` и изолированный временный каталог установки; операции при этом реально изменяют ОС. Файловые тесты обычного Windows-проекта не заменяют проверку SCM/ACL.

## Защитный механизм

[`WindowsAdminFactAttribute`](WindowsAdminFactAttribute.cs) пропускает каждый привилегированный тест, пока одновременно не выполнены три условия: ОС — Windows, переменная `APIKIT_RUN_WINDOWS_ADMIN_TESTS` равна `I_ACCEPT_SCM_AND_ACL_CHANGES`, процесс запущен с повышенным административным токеном Windows. [Скрипт запуска](../scripts/Run-Stage10-WindowsAdmin.ps1) дополнительно требует явный параметр и удаляет переменную среды при завершении.

## Запуск

**Только** в одноразовой тестовой Windows VM с повышенными правами, из корня репозитория:

```powershell
pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges
```

Скрипт сначала собирает [TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md), затем запускает привилегированный xUnit-проект. Не устанавливайте вручную `APIKIT_RUN_WINDOWS_ADMIN_TESTS` и не включайте проект в CI без отдельной проверки. После сбоев проверяйте оставшиеся SCM-записи и ACL каталогов: best-effort cleanup не делает изменения ОС безопасными при любых условиях.

## Ограничения и точки расширения

Набор не моделирует все возможные сбои SCM и не подтверждает транзакционный rollback после каждого частичного изменения конфигурации SCM. Добавляйте привилегированные сценарии в [`WindowsScmInstallerTests`](WindowsScmInstallerTests.cs), сохраняйте обязательный opt-in через [`WindowsAdminFactAttribute`](WindowsAdminFactAttribute.cs) и изменяйте [Windows-установщик](../ApiKit.Management.Windows/Internal/WindowsManagedServiceInstaller.cs) только после подтверждения дефекта. Сначала выполняйте обычные [Windows-тесты](../ApiKit.Management.Windows.Tests/README.ru.md).

## Зависимости

Ориентирован на `net10.0-windows`; зависит от [ApiKit](../ApiKit/README.ru.md), [Windows-реализации](../ApiKit.Management.Windows/README.ru.md), [TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md), xUnit/Microsoft.NET.Test.Sdk из [файла проекта](ApiKit.Management.Windows.PrivilegedTests.csproj).
