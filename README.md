# TestPackage

**A configurable Windows installation simulator for testing repackaging, application virtualization, and deployment solutions.**

By [RWK Systems](https://rwksystems.com)

---

## Why TestPackage?

When testing repackaging tools (like AdminStudio, PACE Suite, Advanced Installer), application virtualization solutions (like App-V, ThinApp, Turbo.net), or deployment platforms (like SCCM, Intune, PDQ), engineers typically grab a generic app like 7-Zip or Notepad++ and hope it exercises enough installer behaviors. This is hit-or-miss.

**TestPackage** is purpose-built for this scenario. It's a fully configurable installer that lets you selectively enable or disable specific system changes — registry writes, file associations, services, shortcuts, environment variables, and more — so you can create targeted test cases for your tooling.

## Features

All features are controlled via `config.ini` and can be toggled independently:

| Feature | Description |
|---------|-------------|
| **Wizard Pages** | Welcome, EULA, install context, directory, components, options |
| **Install Context** | Per-user or per-machine installation |
| **Test Files** | Write marker files to configurable locations |
| **Registry Entries** | Write to HKCU/HKLM with configurable keys and values |
| **Desktop Shortcuts** | Create `.lnk` shortcuts on the desktop |
| **Start Menu** | Create Start Menu folder and entries |
| **File Associations** | Register custom file type associations (.tpkg, .tpkx) |
| **Context Menu** | Add right-click context menu entries |
| **Environment Variables** | Set user or system environment variables |
| **Windows Services** | Install a test Windows service |
| **Scheduled Tasks** | Create scheduled tasks |
| **Firewall Rules** | Add inbound/outbound firewall rules |
| **Protocol Handlers** | Register custom URI protocols (testpkg://) |
| **Active Setup** | Register per-user Active Setup components |
| **App Paths** | Register in App Paths for Run dialog |
| **Startup Entries** | Add to Windows startup (registry or Startup folder) |
| **Font Installation** | Install test fonts |
| **UAC Testing** | Optionally require admin elevation |
| **Intentional Leftovers** | Optionally leave files/registry behind on uninstall |

## The Installed Application

After installation, TestPackage provides a context viewer that shows:

- **Execution context** — user, domain, admin status, integrity level, session ID
- **Installation details** — location, context, components, install date
- **Test file status** — checkmarks showing which files exist
- **Registry status** — verification of all registry entries
- **Feature status** — which installer features were enabled
- **Uninstall button** — clean removal with detailed logging

## Installation

### Option 1: winget (Recommended)

```
winget install RWKSystems.TestPackage
```

### Option 2: Download Installer

Download **TestPackageSetup.exe** from the [latest GitHub release](https://github.com/rwk-systems/test-package/releases/latest) and run it. Installs to `%ProgramFiles%\RWK Systems\Test Package` by default.

### Option 3: Build from Source

#### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building)
- Windows 10/11 (for running)

#### Build

```powershell
# PowerShell
.\build.ps1

# Or use the batch file
build.cmd

# Self-contained (no .NET runtime needed on target):
.\build.ps1 -SelfContained
```

### Configure

Edit `config.ini` to enable/disable features. The file is heavily commented — each section controls a specific installer behavior.

### Run

1. Place `config.ini` alongside `test-package.exe`
2. Run `test-package.exe`
3. Follow the wizard

### Uninstall

- Use the **Uninstall** button in the TestPackage application
- Or use **Add/Remove Programs** (if `RegisterUninstaller=true`)
- Or run: `TestPackageApp.exe --uninstall`

## Building on macOS/Linux

This is a Windows application (WPF), so it must be built and run on Windows. Options for Mac users:

### Option 1: GitHub Actions (Recommended — Free)

Push to this repo and the included GitHub Actions workflow will build automatically. Download the artifact from the Actions tab.

### Option 2: Windows VM

- Use [UTM](https://mac.getutm.app/) (free) or Parallels to run a Windows VM
- Install .NET 8 SDK in the VM
- Clone and build

### Option 3: Windows Sandbox

If you have Windows in a VM, use Windows Sandbox for isolated testing.

## Testing Scenarios

### Repackaging Validation
1. Set `config.ini` with desired features enabled
2. Run the installer while your repackaging tool captures
3. Build the repackaged output
4. Deploy and verify with the context viewer

### Application Virtualization
1. Capture the install with your virtualization tool
2. Run the virtual package
3. Verify file/registry isolation in the context viewer

### Incomplete Uninstall Detection
1. Set `IntentionallyLeaveFiles=true` and/or `IntentionallyLeaveRegistry=true`
2. Install and uninstall
3. Verify your tool detects the orphaned artifacts

## Project Structure

```
test-package/
├── config.ini                          # Installation behavior configuration
├── build.ps1                           # PowerShell build script
├── build.cmd                           # Batch build script
├── TestPackage.sln                     # Visual Studio solution
├── src/
│   ├── TestPackageInstaller/           # Wizard-style installer (WPF) → builds as test-package.exe
│   │   ├── ConfigParser.cs             # INI file parser
│   │   ├── InstallActions.cs           # All installation logic
│   │   ├── MainWindow.xaml/xaml.cs     # Wizard UI
│   │   └── App.xaml/xaml.cs            # Entry point, UAC handling
│   └── TestPackageApp/                 # Installed application (WPF)
│       ├── MainWindow.xaml/xaml.cs     # Context viewer UI
│       ├── UninstallWindow.xaml/xaml.cs # Uninstall UI with logging
│       └── App.xaml/xaml.cs            # Entry point, CLI args
└── .github/
    └── workflows/
        └── build.yml                   # GitHub Actions CI
```

## Product Page

For more information, visit the [Test Package product page](https://www.rwksystems.com/test-package/).

## License

This project is provided for testing purposes by [RWK Systems](https://rwksystems.com).

---

*TestPackage — because your repackaging tests deserve better than 7-Zip.*
