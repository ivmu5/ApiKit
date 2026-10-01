# ApiKit.Management.Windows

English | [Русский](README.ru.md)

Windows-only .NET 10 management transport and service lifecycle implementation. It supplies Named Pipe hosts and clients, protected service registration, SCM installation and directory/pipe access controls while consuming ApiKit's transport-neutral contracts.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · **ApiKit.Management.Windows** · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Capabilities](#capabilities)
- [Architecture](#architecture)
- [Public API](#public-api)
- [State and concurrency](#state-and-concurrency)
- [Testing](#testing)
- [Important limitations](#important-limitations)
- [Modification and extension points](#modification-and-extension-points)
- [Dependencies](#dependencies)

## Capabilities

- Run an authenticated management host and connect administrative clients and managed services over Named Pipes.
- Register services with heartbeats and forward authorized operations/resources.
- Install, update, start, stop and uninstall Windows services with package and identity checks.
- Provision distinct service SIDs and configure filesystem/Named Pipe ACL isolation.

## Architecture

[`WindowsManagementServiceCollectionExtensions`](DependencyInjection/WindowsManagementServiceCollectionExtensions.cs) registers three separate process roles: managed service, management host and admin client. The transport implementation uses [`WindowsNamedPipeChallengeResponseProtocol`](Internal/WindowsNamedPipeChallengeResponseProtocol.cs) and the platform-neutral peer/authenticator contracts. The installer uses [`WindowsManagedServiceInstaller`](Internal/WindowsManagedServiceInstaller.cs), [`WindowsServicePackageStorage`](Internal/WindowsServicePackageStorage.cs) and [`WindowsManagedServiceIsolation`](Internal/WindowsManagedServiceIsolation.cs).

```text
Admin Client ──mutual RSA auth──► Management Host
                                    │ verified admin + ASP.NET policies
                                    ├─ registration / heartbeat registry
                                    ├─ package verification → provisioning → SCM
                                    └─mutual RSA auth──► Managed Service
                                                        verified delegation → MVC
```

<details>
<summary>Authentication and authorization path</summary>

The Admin Client verifies the Host credential before submitting its own proof. The Host verifies the administrator and applies ASP.NET authorization. For service commands, the Host establishes a separate authenticated connection to the specific registered service/instance and forwards verified administrator delegation. Registration, withdrawal and heartbeat use a distinct pipe. The wire format is version 6; file and pipe ACLs are additional OS boundaries rather than substitutes for mutual authentication.

</details>

## Public API

Compose a service's management host in the application that runs it:

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

For other roles:

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

Use [`WindowsManagementServiceOptions`](Options/WindowsManagementServiceOptions.cs), [`WindowsManagementHostOptions`](Options/WindowsManagementHostOptions.cs), [`WindowsManagementClientOptions`](Options/WindowsManagementClientOptions.cs) and [`WindowsServiceManagementOptions`](Options/WindowsServiceManagementOptions.cs) to align peer IDs, pipe names, installation directories and isolation settings. Configure local credentials, trusted peer public keys and any required package verifier/provisioner **before** accepting management traffic.

<details>
<summary>Installer and administration contracts</summary>

The Host registers implementations of [`IManagedServiceInstaller`](../ApiKit/Management/Abstractions/IManagedServiceInstaller.cs) and [`IManagedServiceLifecycleController`](../ApiKit/Management/Abstractions/IManagedServiceLifecycleController.cs); the admin client exposes the same contracts as remote calls. The Host's `RequirePackageVerifier` and `RequireCredentialProvisioner` options default to `true`. Supply trusted package verification and credential provisioning rather than disabling these gates.

</details>

## State and concurrency

The Host owns an in-memory registry, the installer manages versioned packages plus durable `.management` security state, and the isolation registry records provisioned service SID data. Named Pipe servers bound active connection handlers and enforce handshake/read deadlines; authorization of a legitimate long-running operation is not terminated by the initial request-read timeout. Credential trust and key lifecycle come from the separately configured security adapter. Perform installation and updates with deliberate single-writer operational control.

## Testing

[Windows integration tests](../ApiKit.Management.Windows.Tests/README.md) exercise actual Named Pipes, authentication failures, service isolation data, package staging and separate worker processes. [Privileged tests](../ApiKit.Management.Windows.PrivilegedTests/README.md) cover actual SCM and ACL operations and are deliberately excluded from the standard run.

```powershell
pwsh -File ./scripts/Run-Stage10.ps1
```

Run this from the repository root on Windows for the full non-admin integration suite. For controlled elevated testing see [TESTING.md](../TESTING.md).

## Important limitations

This assembly targets `net10.0-windows`. The installer has filesystem rollback mechanisms, but **does not guarantee transactional restoration of Windows SCM configuration** after every partial failure. With SID isolation disabled, existing-service ownership checks are weaker; do not disable isolation as a routine deployment mode. Re-authentication prevents reusing a prior handshake proof, not repeating two separately authorized commands. See [security audit](../SECURITY_AUDIT.md) before deployment.

## Modification and extension points

| Change | Start with |
|---|---|
| Pipe framing, authentication or identity binding | [Protocol](Internal/WindowsNamedPipeProtocol.cs), [challenge-response](Internal/WindowsNamedPipeChallengeResponseProtocol.cs), [tests](../ApiKit.Management.Windows.Tests/Security/NamedPipeHandshakeTests.cs) |
| Host/command delegation | [Host server](Internal/WindowsNamedPipeManagementHostServer.cs), [service server](Internal/WindowsNamedPipeServiceServer.cs), [delegation tests](../ApiKit.Management.Windows.Tests/Security/DelegationSecurityTests.cs) |
| Installation/update/recovery | [Installer](Internal/WindowsManagedServiceInstaller.cs), [package storage](Internal/WindowsServicePackageStorage.cs), [tests](../ApiKit.Management.Windows.Tests/Installer/PackageStorageTests.cs) |
| ACL and SID layout | [Isolation](Internal/WindowsManagedServiceIsolation.cs), [isolation registry](Internal/WindowsManagedServiceIsolationRegistry.cs), [privileged suite](../ApiKit.Management.Windows.PrivilegedTests/README.md) |

## Dependencies

References [ApiKit](../ApiKit/README.md), the ASP.NET Core shared framework, Windows Service hosting and platform security packages listed in [the project file](ApiKit.Management.Windows.csproj). It does not reference CryptoKit directly; consuming applications add the [adapter](../ApiKit.Management.CryptoKit/README.md) where needed. See [third-party licensing](../README.md#dependencies-and-third-party-licenses).
