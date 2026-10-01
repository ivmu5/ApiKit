# ApiKit.Tests

English | [Русский](README.ru.md)

Cross-platform xUnit tests for ApiKit's platform-independent management security, canonical challenge payloads, permission policy provider and management-to-MVC authorization boundary.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: **ApiKit.Tests** · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Coverage](#coverage)
- [Test architecture](#test-architecture)
- [Run](#run)
- [Extension points](#extension-points)
- [Dependencies](#dependencies)

## Coverage

| Suite | Verified behavior |
|---|---|
| [ChallengeProviderTests](Security/ChallengeProviderTests.cs) | Single-use and concurrent challenge consumption, expiry, defensive copies, identifiers and cancellation. |
| [CanonicalProofPayloadTests](Security/CanonicalProofPayloadTests.cs) | Stable field encoding, signed context, identity equality and rejection of ambiguous/invalid values. |
| [AuthorizationPolicyTests](Security/AuthorizationPolicyTests.cs) | Exact permissions, authenticated principals and separation of literal permissions from normal ASP.NET policies. |
| [ManagementBridgePrincipalTests](Security/ManagementBridgePrincipalTests.cs) | No synthetic CRUD permissions for non-administrators or anonymous callers. |

## Test architecture

Tests use in-process DI and direct core component calls, avoiding Windows SCM and external CryptoKit. [`ManualTimeProvider`](Support/ManualTimeProvider.cs) advances time deterministically for challenge expiry and boundary checks. The bridge tests exercise the real dispatcher/authorization decisions rather than substituting a permissive mock policy provider.

## Run

From the repository root with .NET 10:

```powershell
dotnet test ApiKit.Tests/ApiKit.Tests.csproj -c Release
```

This project has no elevated-privilege test gate. Inspect the xUnit summary: source existence or compilation alone does not establish passing assertions.

## Extension points

Add tests near the affected boundary: authorization conventions to `AuthorizationPolicyTests`, challenge state transitions to `ChallengeProviderTests`, payload fields to `CanonicalProofPayloadTests`, and management principal/delegation changes to `ManagementBridgePrincipalTests`. Update the matching [ApiKit implementation](../ApiKit/README.md) only when tests reveal a product defect.

## Dependencies

References [ApiKit](../ApiKit/README.md), xUnit, Microsoft.NET.Test.Sdk and coverlet; see [ApiKit.Tests.csproj](ApiKit.Tests.csproj) and [repository license notes](../README.md#dependencies-and-third-party-licenses).
