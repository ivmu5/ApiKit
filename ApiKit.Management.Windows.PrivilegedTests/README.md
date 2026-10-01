# ApiKit.Management.Windows.PrivilegedTests

English | [Русский](README.ru.md)

Explicitly enabled Windows administrator integration tests that create and manage real Windows SCM services and verify service SID/ACL behavior. This suite is intentionally separate from all routine unit and integration test runs.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · [ApiKit.Management.Windows.TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) · **ApiKit.Management.Windows.PrivilegedTests** | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Scope](#scope)
- [Safety gate](#safety-gate)
- [Run](#run)
- [Limitations and extension points](#limitations-and-extension-points)
- [Dependencies](#dependencies)

## Scope

[`WindowsScmInstallerTests`](WindowsScmInstallerTests.cs) verifies refusal to operate on an unmanaged service and the privileged installation, start, stop, update, uninstall and SID isolation path. The suite uses a randomly named `ApiKitTest*` service and an isolated temporary install directory; the operations still affect the real OS. Filesystem tests in the ordinary Windows test assembly are not a replacement for these SCM/ACL checks.

## Safety gate

[`WindowsAdminFactAttribute`](WindowsAdminFactAttribute.cs) skips every privileged case unless all three conditions hold: the operating system is Windows, the environment variable `APIKIT_RUN_WINDOWS_ADMIN_TESTS` equals `I_ACCEPT_SCM_AND_ACL_CHANGES`, and the current process has an elevated Windows administrator token. The [runner](../scripts/Run-Stage10-WindowsAdmin.ps1) also requires an explicit switch and clears the variable on exit.

## Run

**Only** in an elevated, disposable Windows test VM, from the repository root:

```powershell
pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges
```

The runner first builds [TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) and then invokes the privileged xUnit project. Do not manually set `APIKIT_RUN_WINDOWS_ADMIN_TESTS` or include this project in unreviewed CI. Inspect the resulting SCM entries and filesystem ACLs after failures; best-effort test cleanup does not make OS changes risk-free.

## Limitations and extension points

This suite is not exhaustive SCM fault injection and does not prove complete transactional rollback after every partial SCM reconfiguration. Add new privileged cases to [`WindowsScmInstallerTests`](WindowsScmInstallerTests.cs), keep the mandatory opt-in enforced by [`WindowsAdminFactAttribute`](WindowsAdminFactAttribute.cs), and update [the Windows installer](../ApiKit.Management.Windows/Internal/WindowsManagedServiceInstaller.cs) only after confirming an implementation defect. Re-run ordinary [Windows tests](../ApiKit.Management.Windows.Tests/README.md) first.

## Dependencies

Targets `net10.0-windows`; references [ApiKit](../ApiKit/README.md), [Windows implementation](../ApiKit.Management.Windows/README.md), [TestWorker](../ApiKit.Management.Windows.TestWorker/README.md) and xUnit/Microsoft.NET.Test.Sdk, as listed in [the project file](ApiKit.Management.Windows.PrivilegedTests.csproj).
