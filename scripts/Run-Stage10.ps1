# Requires .NET SDK 10.0 and a restored external/CryptoKit submodule.
# Non-admin tests: core and CryptoKit can run on any supported .NET platform;
# Windows Named Pipe + multiprocess tests require Windows, but not elevation.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    dotnet test .\ApiKit.Tests\ApiKit.Tests.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "ApiKit.Tests failed." }
    dotnet test .\ApiKit.Management.CryptoKit.Tests\ApiKit.Management.CryptoKit.Tests.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) { throw "CryptoKit integration tests failed." }
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        dotnet test .\ApiKit.Management.Windows.Tests\ApiKit.Management.Windows.Tests.csproj --configuration Release
        if ($LASTEXITCODE -ne 0) { throw "Windows integration tests failed." }
    }
} finally { Pop-Location }
