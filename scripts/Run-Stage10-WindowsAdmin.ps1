# Explicitly privileged runner. Execute only in a disposable Windows test environment
# with an elevated administrator shell. Creates, starts and deletes a real SCM service,
# changes ACLs in an isolated temporary root, and uses a unique service name.
param([switch]$IUnderstandScmAndAclChanges)
$ErrorActionPreference = "Stop"
if (-not $IUnderstandScmAndAclChanges) {
    throw "Pass -IUnderstandScmAndAclChanges explicitly to enable Windows SCM / ACL tests."
}
if (-not ($IsWindows -or $env:OS -eq "Windows_NT")) { throw "This test suite requires Windows." }
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run from an elevated administrator PowerShell."
}
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet build .\ApiKit.Management.Windows.TestWorker\ApiKit.Management.Windows.TestWorker.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "SCM fixture build failed." }
    $env:APIKIT_RUN_WINDOWS_ADMIN_TESTS = "I_ACCEPT_SCM_AND_ACL_CHANGES"
    dotnet test .\ApiKit.Management.Windows.PrivilegedTests\ApiKit.Management.Windows.PrivilegedTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "Privileged Windows tests failed." }
} finally {
    Remove-Item Env:\APIKIT_RUN_WINDOWS_ADMIN_TESTS -ErrorAction SilentlyContinue
    Pop-Location
}
