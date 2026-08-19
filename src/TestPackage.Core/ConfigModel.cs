using System;
using System.Collections.Generic;

namespace TestPackage.Core
{
    /// <summary>
    /// Strongly-typed model of all config.ini settings.
    /// Used by the Configurator GUI for two-way binding and by ConfigWriter for serialization.
    /// </summary>
    public class ConfigModel
    {
        // General
        public string AppName { get; set; } = "My Test Package";
        public string AppVersion { get; set; } = "3.0.0";
        public string AppPublisher { get; set; } = "Your Mom";
        public string AppURL { get; set; } = "https://stupidapps.com";
        public string AppGUID { get; set; } = "{E8A3B025-7F4D-4B1A-9C6E-2D8F5A1B3C4D}";
        public bool RequireAdmin { get; set; } = true;
        public string AppExeName { get; set; } = "TestSetupAuditViewer.exe";
        public string InstallerExeName { get; set; } = "YourSimulatedSetup.exe";

        // Wizard Pages
        public bool ShowWelcome { get; set; } = true;
        public bool ShowEULA { get; set; } = true;
        public bool ShowInstallContext { get; set; } = true;
        public bool ShowTargetDirectory { get; set; } = true;
        public bool ShowComponents { get; set; } = true;
        public bool ShowDesktopShortcut { get; set; } = true;
        public bool ShowStartMenuPin { get; set; } = true;
        public bool ShowRebootOption { get; set; } = true;
        public bool ShowActiveSetup { get; set; } = true;

        // EULA
        public string EULAText { get; set; } = "This is a test application provided by RWK Systems for the purpose of testing software repackaging, application virtualization, and deployment solutions. This software is provided \"as is\" without warranty of any kind. By proceeding, you agree to use this software solely for testing purposes.";

        // Install Context
        public string DefaultContext { get; set; } = "Machine";

        // Target Directory
        public string DefaultPath { get; set; } = @"%ProgramFiles%\Your Mom\My Test Package";
        public bool AllowCustomPath { get; set; } = true;

        // Components
        public List<ComponentEntry> Components { get; set; } = new()
        {
            new("CoreFiles", true),
            new("SampleDocuments", true),
            new("CommandLineTools", false),
            new("PluginFramework", false),
            new("LocalizationPack", false),
        };

        // Test Files
        public bool TestFilesEnabled { get; set; } = true;
        public string TestFiles { get; set; } = @"%InstallDir%\testfile.txt|TestPackage marker file,%InstallDir%\data\config.dat|Configuration data,%ProgramData%\RWK Systems\TestPackage\shared.dat|Shared data file";

        // Registry
        public bool RegistryEnabled { get; set; } = true;
        public string RegistryEntries { get; set; } = @"HKCU\Software\RWK Systems\TestPackage|InstallDate|REG_SZ|%DATE%,HKCU\Software\RWK Systems\TestPackage|Version|REG_SZ|3.0.0,HKLM\Software\RWK Systems\TestPackage|InstallPath|REG_SZ|%InstallDir%";

        // Shortcuts
        public bool CreateDesktopShortcut { get; set; } = true;
        public string DesktopShortcutName { get; set; } = "TestPackage";
        public bool CreateStartMenuEntry { get; set; } = true;
        public string StartMenuFolder { get; set; } = @"Your Mom\My Test Package";
        public bool PinToStartMenu { get; set; }

        // File Associations
        public bool FileAssociationsEnabled { get; set; }
        public string FileAssociations { get; set; } = @".tpkg|TestPackage.Document|TestPackage Document|%InstallDir%\TestPackageApp.exe,0,.tpkx|TestPackage.Archive|TestPackage Archive|%InstallDir%\TestPackageApp.exe,1";

        // Context Menu
        public bool ContextMenuEnabled { get; set; }
        public string ContextMenuEntries { get; set; } = @"*|Open with TestPackage|""%InstallDir%\TestPackageApp.exe"" ""%1"",Directory|Scan with TestPackage|""%InstallDir%\TestPackageApp.exe"" --scan ""%1""";

        // Environment Variables
        public bool EnvironmentVariablesEnabled { get; set; }
        public string EnvironmentVariables { get; set; } = @"User|TESTPACKAGE_HOME|%InstallDir%,User|TESTPACKAGE_VERSION|3.0.0";

        // Services
        public bool ServicesEnabled { get; set; }
        public string ServiceName { get; set; } = "TestPackageSvc";
        public string ServiceDisplayName { get; set; } = "TestPackage Background Service";
        public string ServiceDescription { get; set; } = "A test service for validating service capture in repackaging tools";
        public string ServiceStartType { get; set; } = "Manual";

        // Scheduled Tasks
        public bool ScheduledTasksEnabled { get; set; }
        public string TaskName { get; set; } = "TestPackage Maintenance";
        public string TaskDescription { get; set; } = "A test scheduled task for validating task capture";
        public string TaskSchedule { get; set; } = "Daily";
        public string TaskTime { get; set; } = "12:00";

        // Firewall Rules
        public bool FirewallRulesEnabled { get; set; }
        public string FirewallRules { get; set; } = @"TestPackage Inbound|In|Allow|TCP|19876,TestPackage Outbound|Out|Allow|TCP|19877";

        // Protocol Handlers
        public bool ProtocolHandlersEnabled { get; set; }
        public string ProtocolHandlers { get; set; } = @"testpkg|TestPackage Protocol Handler";

        // Active Setup
        public bool ActiveSetupEnabled { get; set; }
        public string ActiveSetupStubPath { get; set; } = @"%InstallDir%\TestPackageApp.exe --activesetup";
        public string ActiveSetupVersion { get; set; } = "1,0,0,0";

        // App Paths
        public bool AppPathsEnabled { get; set; } = true;
        public string AppPathsExeName { get; set; } = "TestPackageApp.exe";

        // Startup
        public bool StartupEnabled { get; set; }
        public string StartupMethod { get; set; } = "Registry";
        public string StartupScope { get; set; } = "User";

        // Fonts
        public bool FontsEnabled { get; set; }
        public string FontFile { get; set; } = @"assets\TestPackageFont.ttf";
        public string FontName { get; set; } = "TestPackage Font";

        // COM Registration
        public bool COMRegistrationEnabled { get; set; }
        public string COMCLSID { get; set; } = "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}";
        public string COMProgID { get; set; } = "TestPackage.TestObject";
        public string COMDescription { get; set; } = "TestPackage COM Test Object";

        // Uninstall
        public bool RegisterUninstaller { get; set; } = true;
        public bool CleanFiles { get; set; } = true;
        public bool CleanRegistry { get; set; } = true;
        public bool CleanShortcuts { get; set; } = true;
        public bool IntentionallyLeaveFiles { get; set; }
        public string LeftoverFiles { get; set; } = @"%InstallDir%\leftover.dat,%ProgramData%\RWK Systems\leftover.log";
        public bool IntentionallyLeaveRegistry { get; set; }
        public string LeftoverRegistry { get; set; } = @"HKCU\Software\RWK Systems\TestPackage\Leftover|OrphanedKey|REG_SZ|This was intentionally left behind";

        // Reboot
        public bool PromptForReboot { get; set; }
        public bool ForceReboot { get; set; }

        // Installer Size
        // When enabled, the Configurator pads the generated setup EXE to
        // InstallerSizeMB (up to 100 GB). The installer also verifies free space
        // on the target drive before installing.
        public bool InstallerSizeEnabled { get; set; }
        public int InstallerSizeMB { get; set; }

        // Code Signing
        // Configurator-time signing of the generated installer .exe. Mode
        // is "None" (unsigned, default) or "PFX" (sign with a .pfx via
        // signtool or PowerShell Set-AuthenticodeSignature). Signing runs
        // after size padding so the signature covers the padded file.
        public string CodeSigningMode { get; set; } = "None";
        public string CodeSigningPfxPath { get; set; } = "";
        public string CodeSigningPfxPassword { get; set; } = "";
        public string CodeSigningTimestampUrl { get; set; } = "http://timestamp.digicert.com";

        /// <summary>Largest installer padding size that can be requested (100 GB, in MB).</summary>
        public const int MaxInstallerSizeMB = 102400;

        // UI
        public string BannerColor { get; set; } = "#0078D4";
        public string AccentColor { get; set; } = "#106EBE";
        public bool ShowProgressBar { get; set; } = true;
        public bool SimulateInstallDelay { get; set; } = true;
        public int InstallDelayMs { get; set; } = 500;

        public static ConfigModel FromParser(ConfigParser config)
        {
            var m = new ConfigModel();
            m.AppName = config.Get("General", "AppName", m.AppName);
            m.AppVersion = config.Get("General", "AppVersion", m.AppVersion);
            m.AppPublisher = config.Get("General", "AppPublisher", m.AppPublisher);
            m.AppURL = config.Get("General", "AppURL", m.AppURL);
            m.AppGUID = config.Get("General", "AppGUID", m.AppGUID);
            m.RequireAdmin = config.GetBool("General", "RequireAdmin");
            m.AppExeName = config.Get("General", "AppExeName", m.AppExeName);
            m.InstallerExeName = config.Get("General", "InstallerExeName", m.InstallerExeName);

            m.ShowWelcome = config.GetBool("WizardPages", "ShowWelcome", true);
            m.ShowEULA = config.GetBool("WizardPages", "ShowEULA", true);
            m.ShowInstallContext = config.GetBool("WizardPages", "ShowInstallContext", true);
            m.ShowTargetDirectory = config.GetBool("WizardPages", "ShowTargetDirectory", true);
            m.ShowComponents = config.GetBool("WizardPages", "ShowComponents", true);
            m.ShowDesktopShortcut = config.GetBool("WizardPages", "ShowDesktopShortcut", true);
            m.ShowStartMenuPin = config.GetBool("WizardPages", "ShowStartMenuPin", true);
            m.ShowRebootOption = config.GetBool("WizardPages", "ShowRebootOption", true);
            m.ShowActiveSetup = config.GetBool("WizardPages", "ShowActiveSetup", true);

            m.EULAText = config.Get("EULA", "EULAText", m.EULAText);
            m.DefaultContext = config.Get("InstallContext", "DefaultContext", m.DefaultContext);
            m.DefaultPath = config.Get("TargetDirectory", "DefaultPath", m.DefaultPath);
            m.AllowCustomPath = config.GetBool("TargetDirectory", "AllowCustomPath", true);

            var comps = config.GetSection("Components");
            if (comps.Count > 0)
            {
                m.Components.Clear();
                foreach (var c in comps)
                    m.Components.Add(new ComponentEntry(c.Key, c.Value.Equals("true", StringComparison.OrdinalIgnoreCase)));
            }

            m.TestFilesEnabled = config.GetBool("TestFiles", "Enabled", m.TestFilesEnabled);
            m.TestFiles = config.Get("TestFiles", "Files", m.TestFiles);

            m.RegistryEnabled = config.GetBool("Registry", "Enabled", m.RegistryEnabled);
            m.RegistryEntries = config.Get("Registry", "Entries", m.RegistryEntries);

            m.CreateDesktopShortcut = config.GetBool("Shortcuts", "CreateDesktopShortcut", m.CreateDesktopShortcut);
            m.DesktopShortcutName = config.Get("Shortcuts", "DesktopShortcutName", m.DesktopShortcutName);
            m.CreateStartMenuEntry = config.GetBool("Shortcuts", "CreateStartMenuEntry", m.CreateStartMenuEntry);
            m.StartMenuFolder = config.Get("Shortcuts", "StartMenuFolder", m.StartMenuFolder);
            m.PinToStartMenu = config.GetBool("Shortcuts", "PinToStartMenu");

            m.FileAssociationsEnabled = config.GetBool("FileAssociations", "Enabled");
            m.FileAssociations = config.Get("FileAssociations", "Associations", m.FileAssociations);

            m.ContextMenuEnabled = config.GetBool("ContextMenu", "Enabled");
            m.ContextMenuEntries = config.Get("ContextMenu", "Entries", m.ContextMenuEntries);

            m.EnvironmentVariablesEnabled = config.GetBool("EnvironmentVariables", "Enabled");
            m.EnvironmentVariables = config.Get("EnvironmentVariables", "Variables", m.EnvironmentVariables);

            m.ServicesEnabled = config.GetBool("Services", "Enabled");
            m.ServiceName = config.Get("Services", "ServiceName", m.ServiceName);
            m.ServiceDisplayName = config.Get("Services", "ServiceDisplayName", m.ServiceDisplayName);
            m.ServiceDescription = config.Get("Services", "ServiceDescription", m.ServiceDescription);
            m.ServiceStartType = config.Get("Services", "ServiceStartType", m.ServiceStartType);

            m.ScheduledTasksEnabled = config.GetBool("ScheduledTasks", "Enabled");
            m.TaskName = config.Get("ScheduledTasks", "TaskName", m.TaskName);
            m.TaskDescription = config.Get("ScheduledTasks", "TaskDescription", m.TaskDescription);
            m.TaskSchedule = config.Get("ScheduledTasks", "TaskSchedule", m.TaskSchedule);
            m.TaskTime = config.Get("ScheduledTasks", "TaskTime", m.TaskTime);

            m.FirewallRulesEnabled = config.GetBool("FirewallRules", "Enabled");
            m.FirewallRules = config.Get("FirewallRules", "Rules", m.FirewallRules);

            m.ProtocolHandlersEnabled = config.GetBool("ProtocolHandlers", "Enabled");
            m.ProtocolHandlers = config.Get("ProtocolHandlers", "Protocols", m.ProtocolHandlers);

            m.ActiveSetupEnabled = config.GetBool("ActiveSetup", "Enabled");
            m.ActiveSetupStubPath = config.Get("ActiveSetup", "StubPath", m.ActiveSetupStubPath);
            m.ActiveSetupVersion = config.Get("ActiveSetup", "Version", m.ActiveSetupVersion);

            m.AppPathsEnabled = config.GetBool("AppPaths", "Enabled", m.AppPathsEnabled);
            m.AppPathsExeName = config.Get("AppPaths", "ExeName", m.AppPathsExeName);

            m.StartupEnabled = config.GetBool("Startup", "Enabled");
            m.StartupMethod = config.Get("Startup", "Method", m.StartupMethod);
            m.StartupScope = config.Get("Startup", "Scope", m.StartupScope);

            m.FontsEnabled = config.GetBool("Fonts", "Enabled");
            m.FontFile = config.Get("Fonts", "FontFile", m.FontFile);
            m.FontName = config.Get("Fonts", "FontName", m.FontName);

            m.COMRegistrationEnabled = config.GetBool("COMRegistration", "Enabled");
            m.COMCLSID = config.Get("COMRegistration", "CLSID", m.COMCLSID);
            m.COMProgID = config.Get("COMRegistration", "ProgID", m.COMProgID);
            m.COMDescription = config.Get("COMRegistration", "Description", m.COMDescription);

            m.RegisterUninstaller = config.GetBool("Uninstall", "RegisterUninstaller", m.RegisterUninstaller);
            m.CleanFiles = config.GetBool("Uninstall", "CleanFiles", m.CleanFiles);
            m.CleanRegistry = config.GetBool("Uninstall", "CleanRegistry", m.CleanRegistry);
            m.CleanShortcuts = config.GetBool("Uninstall", "CleanShortcuts", m.CleanShortcuts);
            m.IntentionallyLeaveFiles = config.GetBool("Uninstall", "IntentionallyLeaveFiles");
            m.LeftoverFiles = config.Get("Uninstall", "LeftoverFiles", m.LeftoverFiles);
            m.IntentionallyLeaveRegistry = config.GetBool("Uninstall", "IntentionallyLeaveRegistry");
            m.LeftoverRegistry = config.Get("Uninstall", "LeftoverRegistry", m.LeftoverRegistry);

            m.PromptForReboot = config.GetBool("Reboot", "PromptForReboot");
            m.ForceReboot = config.GetBool("Reboot", "ForceReboot");

            m.InstallerSizeEnabled = config.GetBool("InstallerSize", "Enabled");
            m.InstallerSizeMB = config.GetInt("InstallerSize", "SizeMB", 0);

            m.CodeSigningMode         = config.Get("CodeSigning", "Mode",         m.CodeSigningMode);
            m.CodeSigningPfxPath      = config.Get("CodeSigning", "PfxPath",      m.CodeSigningPfxPath);
            m.CodeSigningPfxPassword  = config.Get("CodeSigning", "PfxPassword",  m.CodeSigningPfxPassword);
            m.CodeSigningTimestampUrl = config.Get("CodeSigning", "TimestampUrl", m.CodeSigningTimestampUrl);

            m.BannerColor = config.Get("UI", "BannerColor", m.BannerColor);
            m.AccentColor = config.Get("UI", "AccentColor", m.AccentColor);
            m.ShowProgressBar = config.GetBool("UI", "ShowProgressBar", m.ShowProgressBar);
            m.SimulateInstallDelay = config.GetBool("UI", "SimulateInstallDelay", m.SimulateInstallDelay);
            m.InstallDelayMs = config.GetInt("UI", "InstallDelayMs", m.InstallDelayMs);

            return m;
        }
    }

    public class ComponentEntry
    {
        public string Name { get; set; }
        public bool DefaultSelected { get; set; }

        public ComponentEntry(string name, bool defaultSelected)
        {
            Name = name;
            DefaultSelected = defaultSelected;
        }
    }
}
