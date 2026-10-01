# ApiKit.Management.Windows.Tests

English | [Русский](README.ru.md)

Windows-focused xUnit integration tests for real Named Pipe authentication, authorization delegation, Windows isolation metadata, package recovery and multi-process service discovery. No SCM or administrator rights are required by the ordinary suite.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · **ApiKit.Management.Windows.Tests** · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Coverage](#coverage)
- [Test architecture](#test-architecture)
- [Run](#run)
- [Scope and limits](#scope-and-limits)
- [Extension points](#extension-points)
- [Dependencies](#dependencies)

## Coverage

| Suite | Verified behavior |
|---|---|
| [NamedPipeHandshakeTests](Security/NamedPipeHandshakeTests.cs) | Mutual proof, forged identities, new-connection replay, invalid wire version, stalled handshakes and framing. |
| [ManagementAuthorizationTests](Security/ManagementAuthorizationTests.cs) and [DelegationSecurityTests](Security/DelegationSecurityTests.cs) | Policy boundaries and rejection of absent/non-administrative delegation. |
| [MultiProcessManagementTests](Security/MultiProcessManagementTests.cs) | Host, admin, two services, forged admin, untrusted service and withdrawal in real processes. |
| [PackageStorageTests](Installer/PackageStorageTests.cs) | Package validation, staging, symlink rejection, filesystem rollback and version isolation. |
| [IsolationRegistryTests](Installer/IsolationRegistryTests.cs) | Persisted SID records and invalid/mismatched isolation metadata. |

```text
xUnit test → isolated test fixture → real pipe endpoints or package staging
                 │
                 └─ multi-process test → TestWorker host + services + admin
```

## Test architecture

[`WindowsPeerFixture`](Support/WindowsPeerFixture.cs) prepares trusted test identities using isolated key storage. `[WindowsFact]` in [`WindowsFactAttribute`](Support/WindowsFactAttribute.cs) skips real OS integration on non-Windows environments. The multi-process suite launches [TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) and uses distinct temporary state; it does not use the privileged Windows SCM suite.

## Run

On Windows, run from the repository root after restoring CryptoKit:

```powershell
dotnet test ApiKit.Management.Windows.Tests/ApiKit.Management.Windows.Tests.csproj -c Release
```

Alternatively use the root `Run-Stage10.ps1` runner. Builds use `net10.0-windows`; real `[WindowsFact]` cases skip rather than count as successful runs on unsupported systems. The worker executable is a project reference, but it can also be built explicitly if diagnosing process-launch failures.

## Scope and limits

These tests exercise actual Named Pipes and an isolated package filesystem. They do **not** inject failures after every SCM reconfiguration step and do not certify a complete SCM rollback. Real service creation, start/stop, ACL changes and SID enforcement are in the [separate privileged suite](../ApiKit.Management.Windows.PrivilegedTests/README.md), which must be started explicitly.

## Extension points

Protocol changes require corresponding handshake and framing tests; authorization/delegation changes belong in their respective `Security/` suites. Update `PackageStorageTests` for new package/rollback invariants, `IsolationRegistryTests` for SID persistence, and `MultiProcessManagementTests` for cross-process assumptions. Test-only worker behavior lives in [TestWorker](../ApiKit.Management.Windows.TestWorker/README.md), not in the production host.

## Dependencies

References the [core](../ApiKit/README.md), [Windows implementation](../ApiKit.Management.Windows/README.md), [CryptoKit adapter](../ApiKit.Management.CryptoKit/README.md) and [TestWorker](../ApiKit.Management.Windows.TestWorker/README.md), plus xUnit and coverlet listed in [the project file](ApiKit.Management.Windows.Tests.csproj).
