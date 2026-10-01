# ApiKit — Integration & Security Tests

This document describes the stage 10 test suites. **The presence of a test does not imply it has passed.** Tests must be compiled and executed on the target platforms before relying on their results.

## Requirements

- .NET SDK 10 and NuGet restore access (or a pre-populated package cache).
- Complete `external/CryptoKit` submodule source (`git submodule update --init --recursive` after cloning).
- Windows for real Named Pipe and multi-process integration tests. These tests use unique temporary pipes and per-process credential storage; they do **not** require elevation.
- A disposable Windows VM with an elevated administrator account for explicit SCM and ACL tests. **Do not run the privileged suite on a development or production host that contains important services.**

## Test matrix

| Suite | Target | Coverage | Privileges |
| --- | --- | --- | --- |
| `ApiKit.Tests` | `net10.0` | One-time challenges and concurrent consumption; expiry/capacity; nonce copy protection; canonical security payload; identity binding; actual ApiKit permission provider and ordinary ASP.NET policies | None |
| `ApiKit.Management.CryptoKit.Tests` | `net10.0` | Real CryptoKit RSA-PSS signatures; public-key trust/forgery negatives; protocol context/clock binding; durable credential activation/retirement/revocation; distinct provisioned service storage | None |
| `ApiKit.Management.Windows.Tests` | `net10.0-windows` | Real mutual Named Pipe handshake, replayed proof over a fresh connection, untrusted Host, forged Admin, invalid protocol and frames; real management authorization; multi-process Host + Admin + two services; package snapshot and filesystem rollback; persisted isolation state | Windows user, no elevation |
| `ApiKit.Management.Windows.PrivilegedTests` | `net10.0-windows` | Explicit, uniquely named **real** Windows service installation, DACL/service SID checks, start/stop, update, rejection of invalid package and uninstall | **Explicit opt-in + elevated Windows administrator** |

`ApiKit.Management.Windows.TestWorker` is the dedicated test executable; it is not a production management tool. Tests use locally generated 2048-bit RSA keys and separate stores for each simulated peer.

## Normal execution

Run from the repository root:

```powershell
pwsh -File ./scripts/Run-Stage10.ps1
```

The script runs `ApiKit.Tests` and `ApiKit.Management.CryptoKit.Tests` on supported platforms; it additionally runs the normal Windows test project on Windows. The privileged project is intentionally **not** included in `ApiKit.slnx` and never runs from the normal script. Windows tests depend on a built `ApiKit.Management.Windows.TestWorker` executable through a project reference.

Equivalent project-level commands:

```powershell
dotnet test ./ApiKit.Tests/ApiKit.Tests.csproj -c Release
dotnet test ./ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj -c Release
# Only on Windows:
dotnet test ./ApiKit.Management.Windows.Tests/ApiKit.Management.Windows.Tests.csproj -c Release
```

## Explicit Windows administrator execution

**Only in an isolated/disposable Windows test VM**, open an elevated PowerShell 7 terminal and run:

```powershell
pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges
```

Both the runner and each privileged test require explicit opt-in; the test assembly also checks the elevation token. It creates a random `ApiKitTest*` SCM service and a random temporary install directory. Service-name uniqueness is not a substitute for isolation: service configuration and filesystem ACL modifications are actual operating system operations.

Do not manually set `APIKIT_RUN_WINDOWS_ADMIN_TESTS` outside the privileged runner. The runner clears its opt-in marker on exit.

## What the suites do *not* guarantee

- **No production certification:** threat modeling, fuzzing of protocol v6, Windows matrices, stress tests and operational recovery exercises remain necessary.
- **Authentication replay vs business idempotency:** the tested one-time challenge prevents reusing a cryptographic authentication proof. The current request wire format has no general-purpose idempotency key; two separately authorized business requests may both execute. Callers should provide operation-level idempotency for non-repeatable actions.
- **SCM update recovery:** same-version package replacement has an isolated filesystem rollback snapshot. If Windows SCM configuration has already changed and a later step fails, full automatic restoration of previous service arguments/account/dependencies/description is not guaranteed. An operator must inspect SCM configuration and restore it where necessary. Tests must not be interpreted as covering a durable SCM configuration rollback.
- **Privileged negatives:** the administrator smoke test exercises an invalid entrypoint before SCM reconfiguration; it does not inject faults into every post-reconfiguration step.
- Skipped Windows tests on non-Windows platforms and skipped privileged tests do not count as successful execution.

`external/CryptoKit` is used through project references and **is not modified by the stage 10 implementation**.

## Post-Stage-10 source review

Run the first-party comment-language check from the repository root:

```powershell
python ./scripts/check_english_comments.py
```

Additional regression cases cover administrative delegation and synthetic CRUD permission boundaries, stalled handshake deadlines, safe package staging, and refusal to operate on unmanaged Windows services. The latter requires the explicitly enabled privileged suite. See [SECURITY_AUDIT.md](SECURITY_AUDIT.md) for the changes and the residual security risks. Static checks do not establish successful compilation or runtime test results.
