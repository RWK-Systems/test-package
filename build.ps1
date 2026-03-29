# TestPackage v2.0 Build Script
# Builds the Configurator, SimulatedInstaller, SimulatedApp, and packages them.
# Requires: .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ".\dist",
    [switch]$SelfContained,
    [string]$Runtime = "win-x64",
    [switch]$Sign
)

$ErrorActionPreference = "Stop"

Write-Host "=== TestPackage v2.0 Build ===" -ForegroundColor Cyan
Write-Host ""

# Clean output
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}

$scFlag = if ($SelfContained) { "true" } else { "false" }
$outSubdir = if ($SelfContained) { "self-contained" } else { "publish" }

if ($SelfContained) {
    Write-Host "Building self-contained (no .NET runtime required)" -ForegroundColor Yellow
} else {
    Write-Host "Building framework-dependent (.NET 8 required)" -ForegroundColor Yellow
}

# 1. Build template files (SimulatedApp + SimulatedInstaller)
Write-Host ""
Write-Host "Building SimulatedApp (template)..." -ForegroundColor Green
dotnet publish src\TestPackageApp\TestPackageApp.csproj -c $Configuration -r $Runtime --self-contained $scFlag -o "$OutputDir\$outSubdir\templates"

Write-Host ""
Write-Host "Building SimulatedInstaller (template)..." -ForegroundColor Green
dotnet publish src\TestPackageInstaller\TestPackageInstaller.csproj -c $Configuration -r $Runtime --self-contained $scFlag -o "$OutputDir\$outSubdir\templates"

# Copy default config.ini to templates
Copy-Item "config.ini" "$OutputDir\$outSubdir\templates\config.ini" -Force

# 2. Build Configurator
Write-Host ""
Write-Host "Building TestPackage Configurator..." -ForegroundColor Green
dotnet publish src\TestPackage.Configurator\TestPackage.Configurator.csproj -c $Configuration -r $Runtime --self-contained $scFlag -o "$OutputDir\$outSubdir\configurator"

# Copy templates into configurator directory
New-Item -ItemType Directory -Path "$OutputDir\$outSubdir\configurator\templates" -Force | Out-Null
Copy-Item "$OutputDir\$outSubdir\templates\TestPackageInstaller.exe" "$OutputDir\$outSubdir\configurator\templates\" -Force
Copy-Item "$OutputDir\$outSubdir\templates\TestPackageApp.exe" "$OutputDir\$outSubdir\configurator\templates\" -Force
Copy-Item "$OutputDir\$outSubdir\templates\config.ini" "$OutputDir\$outSubdir\configurator\templates\" -Force

# Code signing
if ($Sign) {
    Write-Host ""
    Write-Host "Signing executables..." -ForegroundColor Green
    Get-ChildItem "$OutputDir\$outSubdir" -Recurse -Filter "*.exe" | ForEach-Object {
        Write-Host "  Signing $($_.Name)..." -ForegroundColor White
        signtool sign /v /fd SHA256 /tr http://timestamp.acs.microsoft.com /td SHA256 /dlib "Azure.CodeSigning.Dlib.dll" /dmdf sign-metadata.json $_.FullName
        if ($LASTEXITCODE -ne 0) { throw "Signing failed for $($_.Name)" }
    }
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Configurator: $OutputDir\$outSubdir\configurator\" -ForegroundColor White
Write-Host "Templates:    $OutputDir\$outSubdir\templates\" -ForegroundColor White
Write-Host ""
Write-Host "To test: Run $OutputDir\$outSubdir\configurator\TestPackageConfigurator.exe" -ForegroundColor Yellow
