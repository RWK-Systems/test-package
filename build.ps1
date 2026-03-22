# TestPackage Build Script
# Builds the installer and app, then packages them together.
# Requires: .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\dist",
    [switch]$SelfContained,
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

Write-Host "=== TestPackage Build ===" -ForegroundColor Cyan
Write-Host ""

# Clean output
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir | Out-Null

$publishArgs = @(
    "--configuration", $Configuration,
    "--runtime", $Runtime,
    "--output", "$OutputDir\publish"
)

if ($SelfContained) {
    $publishArgs += "--self-contained", "true"
    Write-Host "Building self-contained (no .NET runtime required on target)" -ForegroundColor Yellow
} else {
    $publishArgs += "--self-contained", "false"
    Write-Host "Building framework-dependent (.NET 8 runtime required on target)" -ForegroundColor Yellow
}

# Build TestPackageApp first (installer copies it)
Write-Host ""
Write-Host "Building TestPackageApp..." -ForegroundColor Green
dotnet publish src\TestPackageApp\TestPackageApp.csproj @publishArgs

# Build TestPackageInstaller
Write-Host ""
Write-Host "Building TestPackageInstaller (single-file)..." -ForegroundColor Green
dotnet publish src\TestPackageInstaller\TestPackageInstaller.csproj @publishArgs

# Copy config.ini alongside the installer
Copy-Item "config.ini" "$OutputDir\publish\config.ini" -Force

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Output: $OutputDir\publish" -ForegroundColor White
Write-Host ""
Write-Host "Contents:" -ForegroundColor White
Get-ChildItem "$OutputDir\publish" | Format-Table Name, Length -AutoSize
Write-Host ""
Write-Host "To test: Run $OutputDir\publish\test-package.exe" -ForegroundColor Yellow
Write-Host "(config.ini must be in the same directory as the executable)" -ForegroundColor Yellow

