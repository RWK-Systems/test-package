using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TestPackage.Core
{
    /// <summary>
    /// Records all actions taken during install so they can be reversed on uninstall.
    /// </summary>
    public class InstallManifest
    {
        public string AppName { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public string AppGUID { get; set; } = "";
        public string InstallDir { get; set; } = "";
        public string InstallContext { get; set; } = "";
        public string InstalledBy { get; set; } = "";
        public DateTime InstallDate { get; set; }
        public List<string> CreatedFiles { get; set; } = new();
        public List<string> CreatedDirectories { get; set; } = new();
        public List<string> RegistryEntries { get; set; } = new();
        public List<string> Shortcuts { get; set; } = new();
        public List<string> Components { get; set; } = new();
        public bool DesktopShortcut { get; set; }
        public bool StartMenuEntry { get; set; }
        public bool StartMenuPinned { get; set; }
        public bool ActiveSetup { get; set; }
        public bool AppPathsRegistered { get; set; }
        public bool FileAssociationsRegistered { get; set; }
        public bool ContextMenuRegistered { get; set; }
        public bool EnvironmentVariablesSet { get; set; }
        public bool ServiceInstalled { get; set; }
        public string ServiceName { get; set; } = "";
        public bool ScheduledTaskCreated { get; set; }
        public string ScheduledTaskName { get; set; } = "";
        public bool FirewallRulesCreated { get; set; }
        public List<string> FirewallRuleNames { get; set; } = new();
        public bool ProtocolHandlerRegistered { get; set; }
        public bool StartupEntryCreated { get; set; }
        public bool FontInstalled { get; set; }
        public bool IntentionallyLeaveFiles { get; set; }
        public bool IntentionallyLeaveRegistry { get; set; }
        public List<string> LeftoverFiles { get; set; } = new();
        public List<string> LeftoverRegistry { get; set; } = new();

        public void Save(string installDir)
        {
            var path = Path.Combine(installDir, "install-manifest.json");
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public static InstallManifest? Load(string installDir)
        {
            var path = Path.Combine(installDir, "install-manifest.json");
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<InstallManifest>(json);
        }
    }
}
