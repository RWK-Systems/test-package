# TestPackage Generator

## Short description

A configurable Windows installation simulator for testing repackaging, virtualization, and deployment solutions.

## Description

TestPackage helps packaging engineers build repeatable test scenarios without relying on production applications. Use the visual Configurator to choose the installer behaviors you want to exercise, then generate a ready-to-run simulated installer.

Configure files and registry entries, shortcuts, services, scheduled tasks, firewall rules, file associations, environment variables, Active Setup, COM registration, fonts, uninstall behavior, and more. Each generated scenario includes an audit trail so you can verify exactly what your packaging or deployment workflow captured.

TestPackage is designed for testing repackaging tools, application virtualization, Intune, Configuration Manager, silent deployment, and uninstall validation.

Key features:

- Visual configuration for 15+ Windows installer behaviors
- One-click generation of self-contained test installers
- Per-user and per-machine installation scenarios
- Silent-install and command-line override testing
- Machine-readable and human-readable installation audit trails
- Intentional uninstall leftovers for cleanup validation

TestPackage creates simulated installers for lab and validation use. Review each scenario before running it, especially when enabling machine-wide system changes.

## Search terms

packaging, repackaging, installer, deployment, virtualization, Intune, SCCM

## Certification notes

TestPackage is a developer and IT administration testing utility. The installed Configurator generates simulated installers that can intentionally create files, registry entries, services, scheduled tasks, firewall rules, shell integrations, and optional uninstall leftovers according to the user's explicit configuration.

To exercise the primary workflow:

1. Launch TestPackage.
2. Leave the default configuration selected.
3. Choose an output folder writable by the current user.
4. Select **Generate Installer**.
5. Run the generated installer only if the test environment permits the selected changes.

The Store installer supports unattended installation with `TestPackageSetup.exe /S` and returns exit code 0 on success. The application does not require an account or collect credentials.
