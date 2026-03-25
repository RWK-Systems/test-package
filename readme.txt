TestPackage
A configurable Windows installation simulator by RWK Systems
https://rwksystems.com

================================================================================

WHAT IS TEST PACKAGE?

Test Package is a purpose-built Windows application that simulates real-world
software installation behaviors. It is designed for testing repackaging tools,
application virtualization solutions, and deployment platforms.

Unlike repurposing production apps like 7-Zip or Notepad++ for testing, Test
Package was designed from the ground up to exercise the specific installer
behaviors that packaging tools need to handle -- registry writes, file
associations, services, scheduled tasks, environment variables, and more.

Every feature is independently toggleable through config.ini.

================================================================================

FILES

  test-package.exe    The main application. Run this to launch the installation
                      wizard that simulates a software install.

  config.ini          Controls all installer behavior. Edit this to enable or
                      disable specific features for your test scenario.

  readme.txt          This file.

  TestPackageApp.exe  The application that gets installed by the wizard to the
                      target directory. Provides a context viewer for verifying
                      the installation and an uninstall button.

================================================================================

QUICK START

  1. Edit config.ini to enable the features you want to test.
  2. Run test-package.exe.
  3. Follow the installation wizard.
  4. The installed application will show a context viewer where you can verify
     what was installed and uninstall when done.

================================================================================

CONFIGURABLE FEATURES

All features are controlled via config.ini and can be toggled independently:

  - Test Files             Create marker files in configurable locations
  - Registry Entries       Write to HKCU, HKLM, or HKCR
  - Desktop Shortcuts      Create .lnk shortcuts on the desktop
  - Start Menu Entries     Create Start Menu folder and entries
  - File Associations      Register custom file types (.tpkg, .tpkx)
  - Context Menus          Add right-click context menu entries
  - Environment Variables  Set user or system environment variables
  - Windows Services       Install a test Windows service
  - Scheduled Tasks        Create scheduled tasks
  - Firewall Rules         Add inbound/outbound firewall rules
  - URI Protocol Handlers  Register custom protocols (testpkg://)
  - Active Setup           Per-user setup triggered on logon
  - App Paths              Register for Windows Run dialog
  - Startup Entries        Add to Windows startup
  - Font Installation      Install test fonts

================================================================================

TESTING SCENARIOS

Repackaging Validation:
  1. Set config.ini with desired features enabled
  2. Run test-package.exe while your repackaging tool captures
  3. Build the repackaged output
  4. Deploy and verify with the context viewer

Application Virtualization:
  1. Capture the install with your virtualization tool
  2. Run the virtual package
  3. Verify file/registry isolation in the context viewer

Incomplete Uninstall Detection:
  1. Set IntentionallyLeaveFiles=true and/or IntentionallyLeaveRegistry=true
  2. Install and uninstall
  3. Verify your tool detects the orphaned artifacts

================================================================================

BUILDING FROM SOURCE

Prerequisites:
  - .NET 8 SDK (https://dotnet.microsoft.com/download/dotnet/8.0)
  - Windows 10/11

Build:
  .\build.ps1                    # Framework-dependent
  .\build.ps1 -SelfContained    # Self-contained (no .NET runtime needed)

Or use the batch file:
  build.cmd

================================================================================

SOURCE CODE & CONTRIBUTING

Test Package is open source. Visit the GitHub repository to report issues,
request features, or contribute:

  https://github.com/rwk-systems/test-package

================================================================================

LICENSE

This project is provided for testing purposes by RWK Systems.
https://rwksystems.com

Test Package -- because your repackaging tests deserve better than 7-Zip.

