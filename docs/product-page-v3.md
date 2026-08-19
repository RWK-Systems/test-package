# TestPackage Product Page Content — v3 (WordPress)
# Copy the content below into your WordPress page editor.
# Screenshots referenced by relative path — upload separately or point at
# github raw URLs. Suggested filenames per section below.

---

## Above the fold (after banner image)

**TestPackage v3** is a configurable Windows installation simulator. It
generates realistic setup programs that exercise the exact installer
behaviors your packaging tools need to handle — so you can test your
workflow, not someone else's app.

**Now available on the Microsoft Store**, via winget, or as a signed
direct download from GitHub.

---

## Get TestPackage

<!-- Layout these as three side-by-side buttons or cards. -->

### Microsoft Store

- **[Get it on the Microsoft Store](https://apps.microsoft.com/store/detail/XPDNGCK5QPG7VK)**
- Windows 10 / 11 deep link (opens the Store app directly):
  `ms-windows-store://pdp/?productid=XPDNGCK5QPG7VK`

### winget (command line)

```
winget install RWKSystems.TestPackage
```

### Direct download

**[TestPackageSetup.exe (v3.0.0)](https://github.com/RWK-Systems/test-package/releases/latest)**
— a standalone Windows installer. The .NET 8 runtime is bundled inside
the .exe, so nothing else needs to be installed first. Signed with
Azure Trusted Signing.

**[View source on GitHub](https://github.com/RWK-Systems/test-package)**

---

## The Problem

Packaging engineers need test installers. Most reach for whatever's
handy — 7-Zip, Notepad++, VLC — but production apps make poor test
subjects. They exercise a narrow, fixed set of installer behaviors.
They can't be reconfigured. And when something breaks, it's hard to
tell whether the failure is in your tooling or in the app itself.

## The Solution

TestPackage Configurator lets you design a simulated installer from
scratch. Toggle on exactly the Windows installer behaviors you need
to test, click **Generate Installer**, and get a standalone setup EXE
ready for your packaging tool to capture.

Every generated installer behaves like real software: it runs a
multi-page wizard, creates files, writes registry entries, registers
shortcuts, installs services — whatever you configured. When it
finishes, it drops a lightweight **audit viewer** that shows every
action the installer took and provides a clean uninstall.

One tool. Any combination of behaviors. Repeatable.

---

## What's new in v3

TestPackage v3 is a full redesign of the Configurator based on months
of feedback from packaging engineers using v2 in the field.

### Single-screen Configurator

The 7-tab layout is gone. Identity, wizard, install actions, uninstall,
appearance and package settings all live on one scrollable canvas, with
a live "receipt" rail on the right that updates as you edit — behaviors
enabled, installer size, elevation, install directory, and a one-line
description of what your generated installer will do.

[SCREENSHOT: full Configurator window — recommended file name
`v3-configurator-full.png`. The design source lives at
`docs/ux/screenshots/03-install-actions.png`.]

### Presets

Start from a blank slate or from one of four ready-made scenarios —
**Blank**, **Typical desktop app**, **Service + firewall**, or
**Enterprise**. A preset replaces the install-action behaviors and
their sample data while leaving your app identity, wizard pages and
uninstall settings alone, so it's safe to try one mid-session.

### Composite editors

Registry entries, environment variables, firewall rules, file
associations, context menu entries, protocol handlers and test files
each get a proper master-list-plus-detail-form overlay. Add, edit,
remove, and see the sample data update in the receipt rail live —
no more editing pipe-separated fields by hand.

[SCREENSHOT: composite editor overlay — `v3-composite-editor.png`.
Design source at `docs/ux/screenshots/04-data-editor-registry.png`.]

### Configurable installer size

Pad the generated setup EXE up to 100 GB with a non-linear slider plus
exact MB entry, to test how repackaging and virtualization tools handle
large installers. At install time the wizard verifies the target drive
has enough free space and refuses to proceed if not.

### Code signing option

Sign the generated installer with **your own** certificate. Choose the
"PFX" mode, point at a .pfx file, and TestPackage runs `signtool.exe`
(or PowerShell `Set-AuthenticodeSignature` as a fallback) after
generating the installer. Leave the mode as "None" to ship it unsigned
— useful for reproducibility tests where a signature would change per
build.

### Elevation by default

New installers request administrator privileges by default, matching
the reality of Program Files and HKLM writes. Uncheck "Require
Administrator" for user-scope test installers.

### Auto-derived defaults

Default install path and Start Menu folder follow
`<Publisher>\<AppName>` live as you type. Type your own to opt out;
clear the field to re-derive.

### Everything from v2 still works

Silent install with command-line overrides (`/S`, `/D=`, `/context=`,
`/components=`), configurable app metadata, custom EULA text, banner
and accent colors, and a full JSON install manifest for machine-
readable audit trails.

**Existing v2 configuration files load unchanged** in v3 — nothing in
the config schema was removed.

---

## How It Works

### 1. Configure

Open TestPackage Configurator and design your test scenario. Every
option has a clear description, and changes take effect immediately —
no code, no command line, no rebuilding.

Choose from 16+ independently toggleable installer behaviors:

| Category | Features |
|----------|----------|
| **Files & Registry** | Test files in configurable paths, registry entries (HKCU/HKLM/HKCR), environment variables |
| **Shortcuts & Shell** | Desktop shortcuts, Start Menu entries, file associations, context menus, App Paths |
| **Services & Tasks** | Windows services, scheduled tasks, startup entries |
| **Network & Security** | Firewall rules, custom URI protocol handlers |
| **System Integration** | Active Setup, font installation, COM registration |
| **Scale & Prerequisites** | Configurable installer size up to 100 GB with a target-drive free-space check |
| **Uninstall Behavior** | Clean removal, or intentional leftovers for testing incomplete-uninstall detection |
| **Wizard & UI** | Show/hide wizard pages, custom EULA text, banner colors, simulated install delay |
| **Distribution** | Optional code-signing of the generated installer with your own PFX |

Name your output files whatever you want. Defaults are a dated
`TestPackage_DDMMMYY.exe` and `TestSetupAuditViewer.exe`.

### 2. Generate

Click **Generate Installer**. TestPackage produces a ready-to-use
installer in your chosen output folder, signs it if you asked, and
opens the folder for you. Point your packaging tool at it — MSIX,
App-V, Intune, SCCM, or anything else that captures or wraps Windows
installers.

### 3. Test

Run the generated installer through your workflow:

- **Repackaging** — Capture the install, build your package, deploy,
  and verify with the built-in audit viewer.
- **Application virtualization** — Capture with your virtualization
  tool, run the virtual package, verify file and registry isolation.
- **Silent deployment** — Test silent install switches, per-user vs.
  per-machine contexts, and UAC elevation handling.
- **Uninstall validation** — Verify clean removal, or enable intentional
  leftovers to test your tool's orphan detection.
- **Large-payload handling** — Set an installer size in the hundreds of
  MB or GB range and stress-test how your pipeline moves it around.

After installation, the audit viewer displays every action the
installer took: files created, registry entries written, shortcuts
placed, services installed. A machine-readable `install-manifest.json`
and a human-readable `description.txt` provide full audit trails.

### 4. Iterate

Change the configuration and generate again. Need to add a service?
Toggle it on. Need to test a reboot prompt? Enable it. Each new
scenario is one click away.

---

## Screenshots

<!-- Recommended layout: 2- or 3-column gallery. Design mockups are in
     docs/ux/screenshots/*.png; production screenshots of v3 are the
     ideal replacement when you have them. -->

- **The workspace** — the single-canvas Configurator with the receipt
  rail on the right (`docs/ux/screenshots/01-identity.png` or a real
  screenshot of `v3-configurator-full.png`).
- **Install actions frame** — the sample-data preview + Edit-data
  buttons for each composite behavior
  (`docs/ux/screenshots/03-install-actions.png`).
- **Composite editor overlay** — master list plus detail form for
  registry entries (`docs/ux/screenshots/04-data-editor-registry.png`).
- **The generated wizard on first run** — an example of what your
  test users will see (`docs/ux/screenshots/06-first-run.png`).
- **Uninstall behavior** — intentional-leftovers config in action
  (`docs/ux/screenshots/07-uninstall.png`).
- **Ready-to-generate confirmation** — the "here's what will land on
  disk" dialog (`docs/ux/screenshots/08-generate-dialog.png`).

---

## Technical Details

- .NET 8 / C# / WPF — the Configurator is a standalone Windows app;
  the .NET 8 runtime is bundled inside the installer so end users
  install nothing else first.
- Zero external dependencies at runtime.
- INI-based configuration; v2 configs load unchanged.
- JSON install manifest for machine-readable audit trails.
- NSIS-based tool installer.
- Signed with Azure Trusted Signing.
- MIT-friendly proprietary license (see
  [LICENSE](https://github.com/RWK-Systems/test-package/blob/main/LICENSE)).

---

## Release notes

Full release notes for v3.0.0 (and every previous release) live on the
GitHub Releases page:
<https://github.com/RWK-Systems/test-package/releases>
