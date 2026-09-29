# ApiKit.Management.CryptoKit

Опциональный адаптер management security между ApiKit и отдельной библиотекой CryptoKit.

По умолчанию проект ожидает CryptoKit по пути:

```text
../CryptoKit/CryptoKit.csproj
```

Путь можно переопределить MSBuild-свойством `CryptoKitProjectPath`.

Пример регистрации:

```csharp
services.AddCryptoKitFileStorage(options =>
{
    options.DirectoryPath = "...";
});
services.AddCryptoKitRsa();

services
    .AddApiKitManagement()
    .AddCryptoKitSecurity(options =>
    {
        options.AddCredential(
            peerId: "messages",
            kind: ManagementPeerKind.ManagedService,
            keyId: "management/messages/current",
            credentialId: "messages-2026-01");
    });
```

Runtime-адаптер по умолчанию не создаёт отсутствующий RSA-ключ. Для bootstrap можно явно передать `createIfMissing: true`, однако production provisioning рекомендуется выполнять отдельным установщиком/control plane.

Доверенные peer public keys хранятся отдельно через `ICryptoKitManagementTrustStore`. Проверяющая сторона не должна хранить private keys других сервисов.

## Регистрация сервиса через Windows Named Pipes

Management protocol v4 ввёл challenge-response для регистрации сервиса. Начиная с protocol v5 Host и Service используют взаимную аутентификацию до передачи `Register`, `Withdraw`, management operations или CRUD payload.
Управляемый сервис должен зарегистрировать `IManagementCredentialProvider`, а Management Host — `IManagementPeerVerifier` и доверенный public key сервиса.

```csharp
services.AddCryptoKitFileStorage(options =>
{
    options.DirectoryPath = "...";
});
services.AddCryptoKitRsa();

var management = services
    .AddApiKitManagement(options => options.ServiceName = "messages")
    .AddCryptoKitSecurity(options =>
    {
        options.AddCredential(
            peerId: "messages",
            kind: ManagementPeerKind.ManagedService,
            keyId: "management/messages/current",
            credentialId: "messages-2026-01");
    })
    .AddWindowsNamedPipeTransport();
```

На стороне Management Host public credential сервиса должен быть предварительно добавлен в `ICryptoKitManagementTrustStore`.
После успешного proof host дополнительно проверяет, что подтверждённые `PeerId` и `InstanceId` совпадают с `ServiceName` и `InstanceId` присланного descriptor.

## Взаимная аутентификация Host и Service

Начиная с этапа 5 (management protocol v5) соединения между Management Host и управляемыми сервисами используют взаимный challenge-response.

На registration pipe порядок намеренно такой:

```text
Management Host -> Service proof
Service verifies trusted host credential
Service -> Management Host proof
Host verifies trusted service credential
Register / Heartbeat / Withdraw
```

Сервис не подписывает challenge до проверки Management Host. Это закрывает сценарий поддельного registration host, который мог бы получить proof от сервиса до подтверждения собственной identity.

На management pipe перед передачей operations, CRUD payload и delegated claims обе стороны также подтверждают identity. Host проверяет именно `ServiceName + InstanceId`, опубликованные ранее в registry.

Поэтому trust store должен быть настроен в обе стороны:

- Management Host хранит trusted public credentials управляемых сервисов;
- каждый сервис хранит trusted public credential Management Host;
- Host имеет собственный локальный RSA credential с `ManagementPeerKind.ManagementHost`;
- сервис имеет собственный локальный RSA credential с `ManagementPeerKind.ManagedService`.

Например credential Host можно зарегистрировать так:

```csharp
services.AddApiKitManagementCryptoKit(options =>
{
    options.AddCredential(
        peerId: "management-host",
        kind: ManagementPeerKind.ManagementHost,
        keyId: "management/host/current",
        credentialId: "management-host-2026-01");
});
```

А управляемый сервис должен использовать тот же `ManagementHostPeerId`, который настроен на Host.

## Административный клиент и Management Host

Начиная с этапа 6 (management protocol v6) административный pipe также использует взаимный challenge-response.
Management Host сначала доказывает `ManagementPeerKind.ManagementHost`, после чего админ-панель доказывает
`ManagementPeerKind.AdminClient`. Административный request передаётся только после обеих проверок.

Для админ-панели нужен отдельный credential, например:

```csharp
services.AddApiKitManagementCryptoKit(options =>
{
    options.AddCredential(
        peerId: "admin-client",
        kind: ManagementPeerKind.AdminClient,
        keyId: "management/admin/current",
        credentialId: "admin-client-2026-01");
});
```

Trust store должен быть настроен в обе стороны:

- Management Host доверяет public credential административного клиента;
- административный клиент доверяет public credential Management Host;
- private keys не передаются между процессами.

Идентификаторы `AdminClientPeerId` и `ManagementHostPeerId` в `WindowsManagementClientOptions`
должны соответствовать зарегистрированным CryptoKit credentials.
