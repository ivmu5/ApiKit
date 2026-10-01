# Verify the build in dependency order without installing services or changing ACLs.
# Run with Windows PowerShell or PowerShell 7 on Windows with the .NET 10 SDK.
param(
    [switch]$RunTests
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'The .NET SDK is not available on PATH. Install the .NET 10 SDK.'
    }

    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdkVersion -notmatch '^10\.') {
        throw "The .NET 10 SDK is required (detected: $sdkVersion)."
    }

    $requiredPaths = @(
        'ApiKit/Management/Builders/ApiKitManagementBuilder.cs',
        'ApiKit/Management/Security/ManagementPeerIdentity.cs',
        'ApiKit/Authentication/DependencyInjection/JwtServiceCollectionExtensions.cs',
        'external/CryptoKit/CryptoKit/CryptoKit.csproj'
    )
    foreach ($path in $requiredPaths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required source file is missing: $path. Re-export the repository and initialize its submodules."
        }
    }

    function Invoke-DotNet {
        param([Parameter(Mandatory = $true)][string[]]$Arguments)
        Write-Host "`n> dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "The preceding dotnet command failed with exit code $LASTEXITCODE. Inspect its first error."
        }
    }

    Invoke-DotNet -Arguments @('restore', 'ApiKit.slnx')
    Invoke-DotNet -Arguments @('build', 'ApiKit/ApiKit.csproj', '--configuration', 'Debug', '--no-restore')
    Invoke-DotNet -Arguments @('build', 'ApiKit.Management.CryptoKit/ApiKit.Management.CryptoKit.csproj', '--configuration', 'Debug', '--no-restore')
    Invoke-DotNet -Arguments @('build', 'ApiKit.Management.Windows/ApiKit.Management.Windows.csproj', '--configuration', 'Debug', '--no-restore')
    Invoke-DotNet -Arguments @('build', 'ApiKit.slnx', '--configuration', 'Debug', '--no-restore')

    if ($RunTests) {
        Invoke-DotNet -Arguments @('test', 'ApiKit.Tests/ApiKit.Tests.csproj', '--configuration', 'Debug', '--no-build')
        Invoke-DotNet -Arguments @('test', 'ApiKit.Management.CryptoKit.Tests/ApiKit.Management.CryptoKit.Tests.csproj', '--configuration', 'Debug', '--no-build')
        if ($env:OS -eq "Windows_NT") {
            Invoke-DotNet -Arguments @('test', 'ApiKit.Management.Windows.Tests/ApiKit.Management.Windows.Tests.csproj', '--configuration', 'Debug', '--no-build')
        }
    }

    Write-Host "`nBuild verification completed." -ForegroundColor Green
} finally {
    Pop-Location
}
