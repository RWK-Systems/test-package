@echo off
REM TestPackage Build Script (Batch wrapper)
REM Requires: .NET 8 SDK

echo === TestPackage Build ===
echo.

if not exist dist mkdir dist

echo Building TestPackageApp...
dotnet publish src\TestPackageApp\TestPackageApp.csproj -c Release -r win-x64 --self-contained false -o dist\publish
if errorlevel 1 goto :error

echo Building TestPackageInstaller (single-file)...
dotnet publish src\TestPackageInstaller\TestPackageInstaller.csproj -c Release -r win-x64 --self-contained false -o dist\publish
if errorlevel 1 goto :error

copy /y config.ini dist\publish\config.ini

echo.
echo === Build Complete ===
echo Output: dist\publish
echo Run dist\publish\TestPackageInstaller.exe to test
goto :end

:error
echo.
echo BUILD FAILED
exit /b 1

:end

