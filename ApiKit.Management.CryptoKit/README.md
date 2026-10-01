# ApiKit.Management.CryptoKit

English | [Русский](README.ru.md)

Optional .NET 10 adapter connecting ApiKit management security to the independent CryptoKit submodule. CryptoKit owns RSA key material, storage and its provider lifecycle; this adapter supplies RSA-PSS/SHA-256 management proofs, durable credential state and explicit public-key trust.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · **ApiKit.Management.CryptoKit**

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Capabilities](#capabilities)
- [Architecture](#architecture)
- [Public API](#public-api)
- [Lifecycle and concurrency](#lifecycle-and-concurrency)
- [Testing](#testing)
- [Important limitations](#important-limitations)
- [Modification and extension points](#modification-and-extension-points)
- [Dependencies](#dependencies)

## Capabilities

- Sign and verify context-bound management challenge proofs without disclosing private keys to peers.
- Maintain per-identity local credentials and a separate store of explicitly trusted remote public keys.
- Prepare, activate and retire credentials with a staged overlap for key rotation.
- Provision isolated service key storage during Windows service installation and restore it on subsequent launches.

## Architecture

[`CryptoKitManagementServiceCollectionExtensions`](DependencyInjection/CryptoKitManagementServiceCollectionExtensions.cs) composes CryptoKit's RSA and storage providers with ApiKit's [`IManagementCredentialProvider`](../ApiKit/Management/Abstractions/IManagementCredentialProvider.cs) and [`IManagementPeerVerifier`](../ApiKit/Management/Abstractions/IManagementPeerVerifier.cs). The [credential registry](Internal/CryptoKitManagementCredentialRegistry.cs) owns durable local activation/retirement; the [trust store](Internal/CryptoKitManagementTrustStore.cs) keeps remote public credentials; the [proof implementation](Internal/CryptoKitManagementRsaProof.cs) applies management-specific RSA-PSS/SHA-256.

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
<summary>Separate signer and trust state</summary>

Only the local credential holder gets its private RSA material. Other parties receive the corresponding public SPKI by a separately trusted distribution path and explicitly add it through [`ICryptoKitManagementTrustStore`](Security/ICryptoKitManagementTrustStore.cs). `Available`, `Active` and `Retired` describe local signer credentials; `Trusted` and `Retiring` describe remote public-key acceptance. Bootstrap `IsActive` does not overwrite an existing durable rotation decision. Credential IDs and RSA key IDs cannot be reused across unrelated identities.

</details>

## Public API

Register CryptoKit storage and RSA first, then register the adapter for the current process identity:

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

To attach the adapter to [`ApiKitManagementBuilder`](../ApiKit/Management/Builders/ApiKitManagementBuilder.cs), use `AddCryptoKitSecurity(options => ...)` instead of calling `AddApiKitManagementCryptoKit` directly. The configured credential must already exist unless `createIfMissing: true` is explicitly requested; production services should normally use installer provisioning. A registration alone does **not** trust any remote peer.

<details>
<summary>Rotation and installer provisioning</summary>

Use [`ICryptoKitManagementCredentialRotationManager`](Security/ICryptoKitManagementCredentialRotationManager.cs) to prepare and activate a new local signer. Export the public credential via [`ICryptoKitManagementPublicCredentialProvider`](Security/ICryptoKitManagementPublicCredentialProvider.cs), distribute it over a trusted channel and call [`ICryptoKitManagementTrustStore.TrustAsync`](Security/ICryptoKitManagementTrustStore.cs) on each verifier **before** activation. During the overlap, `MarkRetiringAsync` places the previous public credential into a time-limited state; finally revoke the old trust and retire the inactive local signer. Revocation does not automatically erase RSA key material.

To provision services from the Windows Host, register `AddApiKitManagementCryptoKitProvisioning()` alongside a configured adapter. An installed service can consume the generated state with `AddApiKitProvisionedManagementCryptoKit()`; custom layouts use [`CryptoKitManagementProvisioningPaths`](Provisioning/CryptoKitManagementProvisioningPaths.cs).

</details>

## Lifecycle and concurrency

Local rotation/trust metadata is persisted through the registered CryptoKit `IKeyStorage` separately from RSA keys and is restored on restart. Do not share the same credential store among independent concurrent writers without additional cross-process coordination. Treat private service keys as scoped to the owning service, and distribute only public SPKI. Managed-service credential provisioning and deprovisioning are part of the Windows installer lifecycle; maintain isolated storage per service.

## Testing

[Adapter tests](../ApiKit.Management.CryptoKit.Tests/README.md) cover actual CryptoKit-generated RSA proofs, negative verification, trust changes, rotation persistence, credential isolation and service provisioning. Run from the repository root:

```powershell
dotnet test ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj -c Release
```

These tests do not replace real Windows Named Pipe or privileged SCM/ACL tests.

## Important limitations

The adapter signs management challenge payloads; it does not implement general-purpose RSA encryption or redefine CryptoKit's own providers. Valid RSA proofs depend on correctly provisioned public-key trust. Local retired credentials cannot be reactivated, while a retiring remote public credential can be re-trusted during a controlled rollback. Management identity is bound to its service credential; it is not independent hardware-backed per-process attestation.

## Modification and extension points

| Change | Start with |
|---|---|
| Proof algorithm boundary | [CryptoKitManagementRsaProof](Internal/CryptoKitManagementRsaProof.cs), [proof tests](../ApiKit.Management.CryptoKit.Tests/Security/CryptoProofTests.cs) |
| Trust and rotation behavior | [Trust store](Internal/CryptoKitManagementTrustStore.cs), [credential registry](Internal/CryptoKitManagementCredentialRegistry.cs), [rotation tests](../ApiKit.Management.CryptoKit.Tests/Security/CredentialRotationTests.cs) |
| Installer key provisioning/layout | [Provisioner](Internal/CryptoKitManagedServiceCredentialProvisioner.cs), [paths](Provisioning/CryptoKitManagementProvisioningPaths.cs), [provisioning tests](../ApiKit.Management.CryptoKit.Tests/Security/ProvisioningTests.cs) |

## Dependencies

References [ApiKit](../ApiKit/README.md), [CryptoKit](../external/CryptoKit/README.md) via the `external/CryptoKit` Git submodule, and Microsoft DI. The project does not reference `ApiKit.Management.Windows`; its assembly and package references are in [the project file](ApiKit.Management.CryptoKit.csproj). The submodule's [MIT license](../external/CryptoKit/LICENSE.txt) applies to its sources.
