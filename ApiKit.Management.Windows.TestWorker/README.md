# ApiKit.Management.Windows.TestWorker

English | [Русский](README.ru.md)

Windows test-only executable used by the multi-process Named Pipe and privileged Windows SCM integration suites. It intentionally implements small host, service, admin and SCM roles rather than serving as a production deployment template.

[Repository overview](../README.md) | Libraries: [ApiKit](../ApiKit/README.md) · [ApiKit.Management.Windows](../ApiKit.Management.Windows/README.md) · [ApiKit.Management.CryptoKit](../ApiKit.Management.CryptoKit/README.md)

Test projects: [ApiKit.Tests](../ApiKit.Tests/README.md) · [ApiKit.Management.CryptoKit.Tests](../ApiKit.Management.CryptoKit.Tests/README.md) · [ApiKit.Management.Windows.Tests](../ApiKit.Management.Windows.Tests/README.md) · **ApiKit.Management.Windows.TestWorker** · [ApiKit.Management.Windows.PrivilegedTests](../ApiKit.Management.Windows.PrivilegedTests/README.md) | [CryptoKit](../external/CryptoKit/README.md)

## Contents

- [Process roles](#process-roles)
- [Execution path](#execution-path)
- [Invocation](#invocation)
- [Testing and changes](#testing-and-changes)
- [Dependencies](#dependencies)

## Process roles

| Role | Behavior |
|---|---|
| `host <settings.json>` | Starts a Management Host with isolated test crypto storage and waits for console control. |
| `service <settings.json>` | Starts a service using test credentials, announces itself and waits for console control. |
| `admin <settings.json>` | Connects to the Host, enumerates visible services and prints a `RESULT:` JSON line. |
| `scm <service-name>` | Runs a minimal .NET Windows Service host for real SCM lifecycle tests. |

## Execution path

[`Program.Main`](Program.cs) selects a role by command-line arguments. The `host`, `service` and `admin` roles deserialize [`WorkerSettings`](Program.cs), configure real CryptoKit providers against role-specific storage, and register the matching ApiKit Windows transport. Multi-process fixtures create and distribute test keys/trust **before** launch; the worker's own startup deliberately sets `createIfMissing: false`. The host and service write `READY`, then read console input until `STOP`. The `admin` role is short-lived.

## Invocation

The integration suites usually launch the executable themselves. To debug a prepared fixture manually:

```powershell
# Run from the repository root after building and provisioning test identities.
dotnet run --project ApiKit.Management.Windows.TestWorker -- host path/to/settings.json
dotnet run --project ApiKit.Management.Windows.TestWorker -- service path/to/settings.json
dotnet run --project ApiKit.Management.Windows.TestWorker -- admin path/to/settings.json
```

`settings.json` must supply fields defined by `WorkerSettings` with matching JSON property names; for example:

```json
{
  "StorageDirectory": "C:\\test\\host-keys",
  "PeerId": "management-host",
  "HostPeerId": "management-host",
  "RegistrationPipeName": "apikit-test.registration",
  "AdminPipeName": "apikit-test.admin",
  "ServicePipePrefix": "apikit-test.service",
  "InstallRoot": "C:\\test\\installed-services",
  "InstanceId": null
}
```

The example JSON is a **structure example**, not an initialized trust configuration. Do not use hard-coded test paths or credentials for production deployments. The `scm` role should be started only by the explicitly enabled privileged SCM runner.

## Testing and changes

This executable is consumed by [multi-process Windows tests](../ApiKit.Management.Windows.Tests/README.md) and [privileged SCM tests](../ApiKit.Management.Windows.PrivilegedTests/README.md). Build it explicitly when diagnosing process startup:

```powershell
dotnet build ApiKit.Management.Windows.TestWorker/ApiKit.Management.Windows.TestWorker.csproj -c Release
```

When changing output markers, arguments or `WorkerSettings`, update the corresponding process-launch tests. Keep role-specific security downgrades in this test executable only.

## Dependencies

Targets `net10.0-windows`, references the three production-facing ApiKit projects, CryptoKit transitively through the adapter and Microsoft Windows Service hosting. See [the project file](ApiKit.Management.Windows.TestWorker.csproj).
