# ApiKit.Management.CryptoKit

[English](README.md) | Русский

Необязательный адаптер .NET 10 между безопасностью ApiKit Management и независимым субмодулем CryptoKit. CryptoKit управляет RSA-материалом, хранилищем и жизненным циклом собственных провайдеров; адаптер реализует RSA-PSS/SHA-256 для Management, сохраняемое состояние credentials и явное доверие к публичным ключам.

[Обзор репозитория](../README.ru.md) | Библиотеки: [ApiKit](../ApiKit/README.ru.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.ru.md) · **ApiKit.Management.CryptoKit**

Тестовые проекты: [ApiKit.Tests](../ApiKit.Tests/README.ru.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.ru.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.ru.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.ru.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.ru.md) | [CryptoKit](../external/CryptoKit/README.ru.md)

## Содержание

- [Основные возможности](#основные-возможности)
- [Архитектура](#архитектура)
- [Публичный API](#публичный-api)
- [Жизненный цикл и параллелизм](#жизненный-цикл-и-параллелизм)
- [Тестирование](#тестирование)
- [Важные ограничения](#важные-ограничения)
- [Точки изменения и расширения](#точки-изменения-и-расширения)
- [Зависимости](#зависимости)

## Основные возможности

- Подписывать и проверять привязанные к контексту Management challenge без передачи закрытых ключей другим сторонам.
- Управлять отдельными локальными credentials каждой identity и хранилищем явно доверенных удалённых публичных ключей.
- Подготавливать, активировать и выводить из эксплуатации credentials с периодом одновременного доверия при ротации.
- Создавать изолированное хранилище ключей сервиса при установке в Windows и восстанавливать доступ при следующих запусках.

## Архитектура

[`CryptoKitManagementServiceCollectionExtensions`](DependencyInjection/CryptoKitManagementServiceCollectionExtensions.cs) объединяет RSA и storage-провайдеры CryptoKit с контрактами ApiKit [`IManagementCredentialProvider`](../ApiKit/Management/Abstractions/IManagementCredentialProvider.cs) и [`IManagementPeerVerifier`](../ApiKit/Management/Abstractions/IManagementPeerVerifier.cs). [Реестр credentials](Internal/CryptoKitManagementCredentialRegistry.cs) отвечает за сохраняемые активацию и retirement локальных ключей; [trust store](Internal/CryptoKitManagementTrustStore.cs) хранит удалённые публичные credentials; [реализация proof](Internal/CryptoKitManagementRsaProof.cs) использует Management-специфичные RSA-PSS/SHA-256.

```text
ApiKit Management challenge/peer contracts
           │
ApiKit.Management.CryptoKit
   ├─ RSA-PSS proof + peer verifier
   ├─ durable local credential rotation
   ├─ explicitly trusted remote public keys
   └─ managed-service credential provisioner
           │
external/CryptoKit → IRsaKeyProvider + IKeyStorage
```

<details>
<summary>Разделение локальной подписи и состояния доверия</summary>

Только владелец локального credential получает закрытый RSA-материал. Другие стороны получают соответствующий публичный SPKI через отдельно доверенный канал распространения и явно добавляют его через [`ICryptoKitManagementTrustStore`](Security/ICryptoKitManagementTrustStore.cs). `Available`, `Active` и `Retired` описывают локальные credentials подписи; `Trusted` и `Retiring` — условия принятия удалённых публичных ключей. Bootstrap-параметр `IsActive` не перезаписывает уже сохранённое решение о ротации. Идентификаторы credentials и RSA-ключей не могут повторно использоваться между несвязанными identities.

</details>

## Публичный API

Сначала зарегистрируйте CryptoKit storage и RSA, затем адаптер для identity текущего процесса:

```csharp
using ApiKit.Management.CryptoKit.DependencyInjection;
using ApiKit.Management.Security;
using CryptoKit.Rsa;
using CryptoKit.Storage;

services.AddCryptoKitFileStorage(options => options.DirectoryPath = keyDirectory);
services.AddCryptoKitRsa();
services.AddApiKitManagementCryptoKit(options => options.AddCredential(
    peerId: "management-host",
    kind: ManagementPeerKind.ManagementHost,
    keyId: "management/host/2026-01",
    credentialId: "host-2026-01"));
```

При использовании [`ApiKitManagementBuilder`](../ApiKit/Management/Builders/ApiKitManagementBuilder.cs) вместо прямого вызова `AddApiKitManagementCryptoKit` доступен `AddCryptoKitSecurity(options => ...)`. Настроенный credential должен существовать заранее, если явно не указан `createIfMissing: true`; в рабочих сервисах обычно используется provisioning установщика. Сама регистрация **не устанавливает** доверие ни к одному удалённому участнику.

<details>
<summary>Ротация и provisioning установщика</summary>

Для подготовки и активации нового локального ключа подписи используйте [`ICryptoKitManagementCredentialRotationManager`](Security/ICryptoKitManagementCredentialRotationManager.cs). Получите публичный credential через [`ICryptoKitManagementPublicCredentialProvider`](Security/ICryptoKitManagementPublicCredentialProvider.cs), передайте его по доверенному каналу и вызовите [`ICryptoKitManagementTrustStore.TrustAsync`](Security/ICryptoKitManagementTrustStore.cs) на каждой проверяющей стороне **до** активации. Во время overlap `MarkRetiringAsync` устанавливает ограниченный срок приёма предыдущего публичного ключа; затем необходимо отозвать старое доверие и вывести неактивный локальный credential из эксплуатации. Отзыв не удаляет автоматически RSA-материал.

Для provisioning сервисов со стороны Windows Host зарегистрируйте `AddApiKitManagementCryptoKitProvisioning()` вместе с настроенным адаптером. Установленный сервис может загрузить подготовленное состояние через `AddApiKitProvisionedManagementCryptoKit()`; нестандартные каталоги обслуживает [`CryptoKitManagementProvisioningPaths`](Provisioning/CryptoKitManagementProvisioningPaths.cs).

</details>

## Жизненный цикл и параллелизм

Локальные данные ротации и доверия сохраняются через CryptoKit `IKeyStorage` отдельно от RSA-ключей и восстанавливаются после перезапуска. Не используйте одно хранилище credentials для независимых одновременно записывающих процессов без дополнительной межпроцессной синхронизации. Закрытые ключи должны принадлежать только своему сервису; распространяйте исключительно публичный SPKI. Provisioning и deprovisioning credentials включены в жизненный цикл Windows-установщика; каждому сервису требуется отдельное хранилище.

## Тестирование

[Тесты адаптера](../ApiKit.Management.CryptoKit.Tests/README.ru.md) проверяют настоящие RSA-proof с ключами CryptoKit, отрицательные сценарии verification, изменения доверия, сохранение ротации, изоляцию credentials и provisioning сервисов. Запуск из корня репозитория:

```powershell
dotnet test ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj -c Release
```

Эти тесты не заменяют проверку настоящих Windows Named Pipes и привилегированные тесты SCM/ACL.

## Важные ограничения

Адаптер подписывает Management challenge и не реализует универсальное RSA-шифрование и не заменяет собственные провайдеры CryptoKit. Проверка RSA-proof зависит от корректного provisioning доверенных публичных ключей. Локальные credentials со статусом `Retired` нельзя активировать повторно; удалённый публичный ключ со статусом `Retiring` можно вновь сделать доверенным при контролируемом rollback. Management-идентичность привязана к credential сервиса и не является независимой аппаратной аттестацией конкретного процесса.

## Точки изменения и расширения

| Изменение | С чего начать |
|---|---|
| Граница алгоритма подписи | [CryptoKitManagementRsaProof](Internal/CryptoKitManagementRsaProof.cs), [тесты proof](../ApiKit.Management.CryptoKit.Tests/Security/CryptoProofTests.cs) |
| Доверие и поведение ротации | [Trust store](Internal/CryptoKitManagementTrustStore.cs), [реестр credentials](Internal/CryptoKitManagementCredentialRegistry.cs), [тесты ротации](../ApiKit.Management.CryptoKit.Tests/Security/CredentialRotationTests.cs) |
| Provisioning ключей и структура каталогов | [Provisioner](Internal/CryptoKitManagedServiceCredentialProvisioner.cs), [пути](Provisioning/CryptoKitManagementProvisioningPaths.cs), [тесты provisioning](../ApiKit.Management.CryptoKit.Tests/Security/ProvisioningTests.cs) |

## Зависимости

Зависит от [ApiKit](../ApiKit/README.ru.md), [CryptoKit](../external/CryptoKit/README.ru.md) через Git-субмодуль `external/CryptoKit` и Microsoft DI. Проект не ссылается на `ApiKit.Management.Windows`; сборка и пакеты указаны в [файле проекта](ApiKit.Management.CryptoKit.csproj). Для исходников субмодуля действует его [лицензия MIT](../external/CryptoKit/LICENSE.txt).
