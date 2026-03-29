# TestPackage

<p align="center">
  <img src="assets/icon.png" alt="TestPackage" width="128" />
</p>

**A configurable Windows installation simulator for testing repackaging, application virtualization, and deployment solutions.**

---

## Why TestPackage?

Packaging engineers need test installers. Most reach for whatever's handy — 7-Zip, Notepad++, VLC — but production apps exercise a narrow, fixed set of installer behaviors. They can't be reconfigured, and when something breaks, it's hard to tell whether the failure is in your tooling or in the app.

**TestPackage** is purpose-built for this. It generates realistic simulated installers that exercise the exact Windows installer behaviors your packaging tools need to handle — so you can test your workflow, not someone else's app.

## How It Works

### 1. Configure

Open **TestPackage Configurator** and design your test scenario. Choose from 15+ independently toggleable installer behaviors:

| Category | Features |
|----------|----------|
| **Files & Registry** | Test files in configurable paths, registry entries (HKCU/HKLM/HKCR), environment variables |
| **Shortcuts & Shell** | Desktop shortcuts, Start Menu entries, file associations, context menus, App Paths |
| **Services & Tasks** | Windows services, scheduled tasks, startup entries |
| **Network & Security** | Firewall rules, custom URI protocol handlers |
| **System Integration** | Active Setup, font installation, COM registration |
| **Uninstall Behavior** | Clean removal, or intentional leftovers for testing incomplete-uninstall detection |
| **Wizard & UI** | Show/hide wizard pages, custom EULA text, banner colors, simulated install delay |

Name your output files whatever you want — the defaults are `YourSimulatedSetup.exe` and `YourSimulatedApp.exe`.

### 2. Generate

Click **Generate Installer**. TestPackage produces a ready-to-use installer in your chosen output folder. Point your packaging tool at it — MSIX, App-V, Intune, SCCM, or anything else that captures or wraps Windows installers.

### 3. Test

Run the generated installer through your workflow:

- **Repackaging** — Capture the install, build your package, deploy, and verify with the built-in context viewer
- **Application virtualization** — Capture with your virtualization tool, run the virtual package, verify file and registry isolation
- **Silent deployment** — Test silent install switches, per-user vs. per-machine contexts, and UAC elevation handling
- **Uninstall validation** — Verify clean removal, or enable intentional leftovers to test your tool's orphan detection

After installation, the context viewer displays every action the installer took. A machine-readable `install-manifest.json` and a human-readable `description.txt` provide full audit trails.

### 4. Iterate

Change the configuration and generate again. Each new scenario is one click away.

## Installation

### Option 1: winget

```
winget install RWKSystems.TestPackage
```

### Option 2: Download

Download **TestPackageConfiguratorSetup.exe** from the [latest GitHub release](https://github.com/RWK-Systems/test-package/releases/latest).

### Option 3: Build from Source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows 10/11.

```powershell
git clone https://github.com/RWK-Systems/test-package.git
cd test-package
.\build.ps1 -SelfContained
```

The Configurator will be in `dist\self-contained\configurator\`.

## Project Structure

```
test-package/
├── config.ini                          # Default configuration template
├── build.ps1                           # PowerShell build script
├── TestPackage.sln                     # Visual Studio solution
├── src/
│   ├── TestPackage.Core/               # Shared library
│   │   ├── ConfigParser.cs             # INI file parser
│   │   ├── ConfigModel.cs             # Strongly-typed config model
│   │   ├── ConfigWriter.cs            # INI file writer
│   │   ├── InstallActions.cs           # Installation logic
│   │   ├── InstallManifest.cs          # JSON manifest model
│   │   └── UninstallActions.cs         # Uninstall logic
│   ├── TestPackage.Configurator/       # GUI config editor (WPF)
│   ├── TestPackageInstaller/           # Simulated installer wizard (WPF)
│   ├── TestPackageApp/                 # Installed context viewer (WPF)
│   └── TestPackageSmokeTest/           # CI smoke test
├── installer/
│   └── test-package-setup.nsi          # NSIS installer for the Configurator
└── .github/workflows/
    ├── build.yml                       # CI build
    ├── release.yml                     # GitHub Releases
    └── submit-winget.yml               # Manual winget submission
```

## Product Page

For more information, visit the [TestPackage product page](https://www.rwksystems.com/test-package/).

## License

[MIT License](LICENSE)

## Roadmap

Future enhancements under consideration:

- **MSI output format** — Generate Windows Installer (.msi) packages using WiX Toolset for testing MSI-specific packaging workflows
- **MSIX output format** — Generate MSIX packages for testing modern Windows app deployment and Microsoft Store scenarios
- **Single-file installer** — Bundle config.ini inside the generated EXE so the output is a single file instead of three
- **Configuration presets** — Built-in preset configurations for common test scenarios (e.g. "Service + Firewall", "Full Enterprise", "Minimal")

---

*TestPackage — because your repackaging tests deserve better than 7-Zip.*
