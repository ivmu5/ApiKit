# ApiKit

English | [Русский](README.ru.md)

ApiKit is a collection of .NET 10 components for ASP.NET Core APIs and management of local Windows microservices. It combines JWT authentication, permission-aware EF Core CRUD and platform-neutral management contracts with optional Windows transport and a separate CryptoKit key-management adapter.

## Contents

- [Core capabilities](#core-capabilities)
- [Projects](#projects)
- [Architecture](#architecture)
- [Getting started](#getting-started)
- [Security and lifecycle](#security-and-lifecycle)
- [Testing](#testing)
- [Modification and extension points](#modification-and-extension-points)
- [Dependencies and third-party licenses](#dependencies-and-third-party-licenses)

## Core capabilities

ApiKit can issue and validate JWTs; enforce resource-scoped or literal ASP.NET Core permissions; expose DTO-based EF Core CRUD with pagination and JSON Patch; publish management discovery and custom operations; and, when composed with the optional projects, authenticate local Windows microservices and manage service installation with separate credentials and OS-level isolation.

## Projects

Three libraries supply production-facing code. The other five first-party projects are tests or their dedicated worker. Each project has independent English and Russian documentation.

| Project | Responsibility |
|---|---|
| [ApiKit](ApiKit/README.md) | Platform-independent ASP.NET Core components and management contracts. |
| [ApiKit.Management.Windows](ApiKit.Management.Windows/README.md) | Windows transport, management host/client, service installer and isolation. |
| [ApiKit.Management.CryptoKit](ApiKit.Management.CryptoKit/README.md) | RSA-PSS management proofs, credentials, trust and provisioning over CryptoKit. |
| [ApiKit.Tests](ApiKit.Tests/README.md) | Core authorization, challenge and canonical-payload tests. |
| [ApiKit.Management.CryptoKit.Tests](ApiKit.Management.CryptoKit.Tests/README.md) | Real RSA, trust, rotation and provisioning tests. |
| [ApiKit.Management.Windows.Tests](ApiKit.Management.Windows.Tests/README.md) | Named Pipe, authorization, package and multi-process tests. |
| [ApiKit.Management.Windows.TestWorker](ApiKit.Management.Windows.TestWorker/README.md) | Separate host, service, admin or SCM process for integration tests. |
| [ApiKit.Management.Windows.PrivilegedTests](ApiKit.Management.Windows.PrivilegedTests/README.md) | Explicit-only Windows SCM and ACL tests. |

[CryptoKit](external/CryptoKit/README.md) is an independent submodule with its own [Russian documentation](external/CryptoKit/README.ru.md) and [license](external/CryptoKit/LICENSE.txt). Its sources are not part of ApiKit's documentation or modification scope.

## Architecture

ApiKit depends on neither Windows nor CryptoKit. The Windows library depends on ApiKit; the adapter depends on ApiKit and the independent CryptoKit submodule. The application composes these pieces through dependency injection.

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
<summary>Management request path and security boundaries</summary>

An administrative client verifies the Management Host and proves its own identity. The Host verifies the caller before applying ASP.NET Core management policies. When forwarding a command, the Host and target service perform a separate authenticated Named Pipe handshake; the service checks the forwarded administrator identity and runs its own authorization. Service registration and heartbeats take a separate pipe path. The protocol's version constant is **6**; OS ACLs narrow access but do not replace cryptographic verification.

</details>

## Getting started

Install the **.NET 10 SDK** and initialize the CryptoKit submodule if you use the adapter or the complete solution:

```bash
git submodule update --init --recursive
```

For a conventional API, add the appropriate project/package references and register only the components you use:

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

`AppDbContext` is your own EF Core context. Configure `ConnectionStrings:DefaultConnection`, `Jwt:Generation`, `Jwt:Validation` and `Jwt:Hmac`; keep signing secrets out of source control. An application using asymmetric signing can implement [`IJwtSigningCredentialsProvider`](ApiKit/Authentication/Abstractions/IJwtSigningCredentialsProvider.cs) and [`IJwtValidationKeysProvider`](ApiKit/Authentication/Abstractions/IJwtValidationKeysProvider.cs) instead of the HMAC provider. Windows management additionally needs explicit cryptographic identity/trust setup; consult the [Windows](ApiKit.Management.Windows/README.md) and [adapter](ApiKit.Management.CryptoKit/README.md) documentation.

<details>
<summary>Service-side Windows management registration</summary>

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

For a host register `AddApiKitWindowsManagementHost`; for an administrative client register `AddApiKitWindowsManagementClient`. These calls do not themselves establish a trusted key exchange.

</details>

## Security and lifecycle

[ManagementChallengeProofPayload](ApiKit/Management/Security/ManagementChallengeProofPayload.cs) encodes identity- and context-bound canonical challenge data. [WindowsNamedPipeChallengeResponseProtocol](ApiKit.Management.Windows/Internal/WindowsNamedPipeChallengeResponseProtocol.cs) consumes one-time challenges on a single connection. [ICryptoKitManagementCredentialRotationManager](ApiKit.Management.CryptoKit/Security/ICryptoKitManagementCredentialRotationManager.cs) manages durable local credential activation and retirement separately from trusted remote public credentials. Management installations can provision an independent key for each service and configure Windows service SIDs and directory ACLs.

An authenticated challenge proof is not a business-operation idempotency mechanism. An interrupted Windows SCM update is also **not** guaranteed to roll back every prior SCM configuration setting. Deployment and recovery boundaries are detailed in [SECURITY_AUDIT.md](SECURITY_AUDIT.md).

## Testing

The repository uses xUnit. Run ordinary suites with the supplied PowerShell runner (Windows-specific cases run only on Windows):

```powershell
pwsh -File ./scripts/Run-Stage10.ps1
```

To test privileged installation/SCM/ACL behavior, use an **elevated disposable Windows VM** and opt in explicitly:

```powershell
pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges
```

The second command performs actual operating-system modifications. Refer to [TESTING.md](TESTING.md) and each test project's README for scope, prerequisites and skip behavior. This repository does not claim that all suites have passed merely because they exist.

## Modification and extension points

| Task | Start with |
|---|---|
| Change token validation or signing providers | [JWT DI](ApiKit/Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs), [provider contracts](ApiKit/Authentication/Abstractions/IJwtValidationKeysProvider.cs) |
| Adjust permission naming or CRUD policy injection | [PermissionNameResolver](ApiKit/Authorization/Permissions/PermissionNameResolver.cs), [CrudPermissionApplicationModelConvention](ApiKit/Crud/Authorization/CrudPermissionApplicationModelConvention.cs), [core tests](ApiKit.Tests/README.md) |
| Add a management operation | [ApiKitManagementBuilder](ApiKit/Management/Builders/ApiKitManagementBuilder.cs), [IManagementOperation](ApiKit/Management/Abstractions/IManagementOperation.cs) |
| Modify pipe protocol or service installation | [protocol](ApiKit.Management.Windows/Internal/WindowsNamedPipeChallengeResponseProtocol.cs), [installer](ApiKit.Management.Windows/Internal/WindowsManagedServiceInstaller.cs), [Windows tests](ApiKit.Management.Windows.Tests/README.md) |
| Extend credential lifecycle | [rotation contract](ApiKit.Management.CryptoKit/Security/ICryptoKitManagementCredentialRotationManager.cs), [adapter tests](ApiKit.Management.CryptoKit.Tests/README.md) |

## Dependencies and third-party licenses

Main dependencies are specified by the respective `.csproj` files; versions are not duplicated here. Project-relevant upstream licensing includes:

| Dependency | Role | Upstream license |
|---|---|---|
| [CryptoKit](external/CryptoKit/LICENSE.txt) | External RSA key management submodule | MIT; retain its included copyright and license notice with redistributed copies. |
| [Npgsql EF Core provider](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3) | PostgreSQL integration | PostgreSQL License. |
| [Microsoft ASP.NET Core JWT bearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.12) | JWT bearer authentication | MIT. |
| [Microsoft ASP.NET Core JSON Patch](https://www.nuget.org/packages/Microsoft.AspNetCore.JsonPatch.SystemTextJson/10.0.12) | JSON Patch | MIT. |
| [xUnit](https://www.nuget.org/packages/xunit/2.9.3) and [its runner](https://www.nuget.org/packages/xunit.runner.visualstudio/3.1.4) | Tests | Apache-2.0. |
| [coverlet.collector](https://www.nuget.org/packages/coverlet.collector/6.0.4) | Test coverage collection | MIT. |

When distributing products containing third-party binaries, inspect the exact resolved package/transitive dependency licenses and carry required copyright, license and NOTICE materials into the distribution. The standalone CryptoKit license applies to that submodule, not automatically to the rest of ApiKit; this README does not assign a license to ApiKit.
