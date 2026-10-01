# ApiKit.Management.CryptoKit.Tests

English | [Русский](README.ru.md)

Cross-platform xUnit integration tests for the ApiKit/CryptoKit management adapter using genuine RSA key generation and cryptographic operations with an isolated in-memory key-storage test fixture.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · **ApiKit.Management.CryptoKit.Tests** · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Coverage](#coverage)
- [Test architecture](#test-architecture)
- [Run](#run)
- [Extension points](#extension-points)
- [Dependencies](#dependencies)

## Coverage

| Suite | Verified behavior |
|---|---|
| [CryptoProofTests](Security/CryptoProofTests.cs) | Real RSA-PSS proof verification, signed-context tampering, unknown credentials, deadline/skew and identity mismatch. |
| [CredentialRotationTests](Security/CredentialRotationTests.cs) | Persisted active credential state, overlap/expiration, revocation/retrust, key-ID isolation and invalid trusted keys. |
| [ProvisioningTests](Security/ProvisioningTests.cs) | Per-service key creation, idempotent provisioning, trust revocation/deprovisioning and unsafe path rejection. |

## Test architecture

[`CryptoFixture`](Support/CryptoFixture.cs) wires real CryptoKit RSA providers against the in-memory [`IKeyStorage` test double](Support/InMemoryKeyStorage.cs). This exercises key generation and signatures without writing production credentials or requiring a Windows service. Durable-state assertions verify persistence across component reconstruction against the same test storage, not physical disk-failure recovery.

## Run

Initialize the CryptoKit submodule and run from the repository root:

```powershell
git submodule update --init --recursive
dotnet test ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj -c Release
```

No SCM/elevation is required. A passing suite does not validate concurrent writers on a shared production credential store.

## Extension points

New proof fields belong in `CryptoProofTests`; new local/trusted state transitions in `CredentialRotationTests`; changes to installer-created identities or storage layout in `ProvisioningTests`. Keep the external [CryptoKit](../external/CryptoKit/README.md) implementation unchanged unless its own interface change is approved.

## Dependencies

References [ApiKit](../ApiKit/README.md) and the [CryptoKit adapter](../ApiKit.Management.CryptoKit/README.md); the test project references xUnit, Microsoft.NET.Test.Sdk and coverlet in [its project file](ApiKit.Management.CryptoKit.Tests.csproj).
