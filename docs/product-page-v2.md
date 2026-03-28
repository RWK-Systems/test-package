# TestPackage Product Page Content (WordPress)
# Copy the content below into your WordPress page editor.
# The image banner goes at the top (upload separately).

---

## Above the fold (after banner image)

**TestPackage** is a configurable Windows installation simulator. It generates realistic setup programs that exercise the exact installer behaviors your packaging tools need to handle — so you can test your workflow, not someone else's app.

---

## The Problem

Packaging engineers need test installers. Most reach for whatever's handy — 7-Zip, Notepad++, VLC — but production apps make poor test subjects. They exercise a narrow, fixed set of installer behaviors. They can't be reconfigured. And when something breaks, it's hard to tell whether the failure is in your tooling or in the app itself.

## The Solution

TestPackage Configurator lets you design a simulated installer from scratch. Toggle on exactly the Windows installer behaviors you need to test, click **Generate Installer**, and get a standalone setup EXE ready for your packaging tool to capture.

Every generated installer behaves like real software: it runs a multi-page wizard, creates files, writes registry entries, registers shortcuts — whatever you configured. When it finishes, it installs a lightweight viewer app that shows exactly what happened and provides a clean uninstall.

One tool. Any combination of behaviors. Repeatable.

---

## How It Works

### 1. Configure

Open TestPackage Configurator and design your test scenario. Every option has a clear description, and changes take effect immediately — no code, no command line, no rebuilding.

Choose from 15+ independently toggleable installer behaviors:

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

After installation, the context viewer displays every action the installer took: files created, registry entries written, shortcuts placed, services installed. A machine-readable `install-manifest.json` and a human-readable `description.txt` provide full audit trails.

### 4. Iterate

Change the configuration and generate again. Need to add a service? Toggle it on. Need to test a reboot prompt? Enable it. Each new scenario is one click away.

---

## Download

TestPackage Configurator is open source and free.

**[Download the latest release](https://github.com/RWK-Systems/test-package/releases/latest)**

Or install via winget:

```
winget install RWKSystems.TestPackage
```

**[View source on GitHub](https://github.com/RWK-Systems/test-package)**

---

## Technical Details

- .NET 8 / C# / WPF — self-contained, no runtime required on the target machine
- Zero external dependencies
- INI-based configuration with GUI editor
- JSON install manifest for machine-readable audit trails
- NSIS-based tool installer
- Signed with Azure Trusted Signing

---
