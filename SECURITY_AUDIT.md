# ApiKit — Post-Stage-10 Security Review

This document summarizes source-level security review changes made against the supplied Stage 10 archive. **It is not a claim of comprehensive security certification.** There was no .NET SDK or usable package restore network in the review environment, so compilation, xUnit execution, Windows runtime verification, dependency advisory retrieval, and privileged SCM/ACL tests have not been performed as part of this review.

## Confirmed weaknesses addressed in the source

| Area | Observation and change | Regression evidence |
| --- | --- | --- |
| Management CRUD | Synthetic CRUD permission previously depended on an arbitrary caller principal. The dispatcher now requires an authenticated caller and only adds the requested synthetic CRUD permission when an administrator management claim is present. MVC authorization policies continue to run. | `ApiKit.Tests/Security/ManagementBridgePrincipalTests.cs` (added; not executed here) |
| Service command delegation | Missing forwarded claims previously fell back to the authenticated Management Host's principal. The service now refuses missing or non-administrative delegation, rejects malformed delegated claims, and never silently substitutes the transport identity as the application caller. | `ApiKit.Management.Windows.Tests/Security/DelegationSecurityTests.cs` (added; not executed here) |
| Named Pipe connection resource use | Open connections could wait for a handshake or request indefinitely and fan out into unbounded tasks. The handshake and initial authenticated request read now have separate 30-second deadlines; the Host and service bound concurrent connection handlers to 64 slots per relevant endpoint. A legitimate long-running authorized operation is not subject to the request-read deadline. | Added stalled-peer handshake test; timeout and load behavior must be checked on Windows |
| Windows installer ownership | `UpdateAsync` and `UninstallAsync` could target an arbitrary existing service name without verifying that it belonged to ApiKit. They now require an owned installation directory and, under the default enabled SID-isolation mode, a matching persisted isolation record. | Explicit-only privileged unmanaged-service refusal test (not executed here) |
| Untrusted package metadata | Null collections, malformed dependencies, device names, and a package directory enclosing its own staging destination were incompletely rejected. Manifest validation and staging/commit checks were strengthened. | Added package validation/staging tests (not executed here) |
| SCM cancellation and package recovery | Cancellation now terminates an in-flight `sc.exe` process tree. If update failure occurs after SCM reconfiguration is attempted, the new files and rollback snapshot are retained instead of blindly reverting package files while SCM may refer to them. | Static review only; recovery fault-injection not executed |

The wire protocol remains version 6. No public application-facing API change was intended. `external/CryptoKit` is unchanged.

## Residual security risks / deployment decisions

1. **SCM rollback is not transactional.** A partially applied Windows SCM configuration still requires inspection and manual recovery. A fully durable update would need a previous-SCM-state snapshot, explicit state restoration, and fault-injection tests. The installer intentionally retains both filesystem states if SCM changes may already have occurred.
2. **Service ownership when isolation is disabled.** With `EnableServiceSidIsolation = false`, directory presence is the ownership check; a stronger independent installation marker or SCM binary-path verification is desirable before allowing unmanaged/legacy configurations.
3. **Multi-process credential state.** Trust and credential workflows have not been proven safe under multiple independent writers sharing the same key storage. Until tested, use a single writer for each credential store and provision each service with its own isolated state.
4. **Scope of a service private key.** A managed service's signing credential authenticates the service identity. Instance IDs are asserted within signed challenge context, but the same service private key is not an independent per-process hardware/OS identity. Protect the private key and use process/OS isolation where needed.
5. **Authentication replay vs operation replay.** A consumed challenge prevents replaying that authentication proof, but separately authenticated duplicate business operations can run twice. Non-repeatable operations need their own idempotency/replay policy.
6. **Configuration and filesystem integrity.** Relaxing managed-directory ACLs, service SID isolation, package verification or credential provisioning increases deployment risk. Package tree validation cannot eliminate every concurrent filesystem mutation by another principal already granted write permissions.
7. **Default administrative scope.** The built-in administrative principal receives broad management permissions. Deployments needing separation of duties should define and test their own authorization scope rather than assuming per-command least privilege.
8. **Dependency and runtime coverage unverified.** No online advisory scan, fuzzing, stress test, cross-Windows-version compatibility matrix, or privileged SCM failure injection was possible in this environment. Run these before production use. Consider the policy for custom JWT signing providers and deeply nested JSON Patch models in your threat model.

## Documentation and validation

- All comments and XML documentation in the first-party C# code and test projects were converted to English. Some original application errors and log **string literals** remain in Russian intentionally; they are not comments, and changing them may affect callers and operational dashboards.
- The supplied `external/CryptoKit` submodule files are byte-identical to the supplied Stage 10 archive.
- `python ./scripts/check_english_comments.py` checks first-party C# comments for Cyrillic. It does not enforce terminology, correctness, or complete XML documentation coverage.
- XML documentation groups and project references receive additional static checks during archive preparation. None of these checks replaces `dotnet build` and `dotnet test`.

## Required pre-release validation

1. Restore the full CryptoKit submodule and compile all projects using the .NET 10 SDK; treat warnings and analyzer findings as review inputs.
2. Run normal automated tests using `pwsh -File ./scripts/Run-Stage10.ps1` and investigate failures; additionally test cancellation, flooding, stalled requests, and replay on actual Windows Named Pipes.
3. Run `pwsh -File ./scripts/Run-Stage10-WindowsAdmin.ps1 -IUnderstandScmAndAclChanges` **only** inside an elevated, disposable Windows test VM. Verify isolation DACLs, authentic SCM ownership checks, provisioning and uninstall state.
4. Inject failures during each SCM update step; verify the retained package and explicit recovery procedure. Do not describe filesystem snapshot tests as end-to-end SCM rollback verification.
5. Run NuGet dependency advisory analysis (including transitive dependencies), protocol fuzzing and operational threat-model review before production deployment.
