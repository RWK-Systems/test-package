using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Win32;

namespace TestPackage.Core
{
    public class InstallActions
    {
        private readonly ConfigParser _config;
        private readonly InstallManifest _manifest;
        private readonly Action<string> _log;

        public InstallActions(ConfigParser config, Action<string> log)
        {
            _config = config;
            _manifest = new InstallManifest();
            _log = log;
        }

        public InstallManifest Manifest => _manifest;

        /// <summary>
        /// The EXE name of the simulated app, read from config or defaulting to TestPackageApp.exe.
        /// </summary>
        public string AppExeName => _config.Get("General", "AppExeName", "TestPackageApp.exe");

        public void Execute(string installDir, string context, List<string> selectedComponents,
            bool desktopShortcut, bool startMenuPin, bool activeSetup)
        {
            _manifest.InstallDir = installDir;
            _manifest.InstallContext = context;
            _manifest.InstalledBy = $@"{Environment.UserDomainName}\{Environment.UserName}";
            _manifest.InstallDate = DateTime.Now;
            _manifest.AppName = _config.Get("General", "AppName", "TestPackage");
            _manifest.AppVersion = _config.Get("General", "AppVersion", "2.0.0");
            _manifest.AppGUID = _config.Get("General", "AppGUID");
            _manifest.Components = selectedComponents;
            _manifest.DesktopShortcut = desktopShortcut;
            _manifest.StartMenuPinned = startMenuPin;
            _manifest.ActiveSetup = activeSetup;

            // Uninstall settings
            _manifest.IntentionallyLeaveFiles = _config.GetBool("Uninstall", "IntentionallyLeaveFiles");
            _manifest.IntentionallyLeaveRegistry = _config.GetBool("Uninstall", "IntentionallyLeaveRegistry");
            if (_manifest.IntentionallyLeaveFiles)
            {
                _manifest.LeftoverFiles = _config.GetList("Uninstall", "LeftoverFiles")
                    .Select(f => _config.ExpandVariables(f, installDir)).ToList();
            }
            if (_manifest.IntentionallyLeaveRegistry)
            {
                _manifest.LeftoverRegistry = _config.GetList("Uninstall", "LeftoverRegistry");
            }

            CreateDirectories(installDir);
            CopyApplicationFiles(installDir);
            CreateTestFiles(installDir);
            WriteRegistryEntries(installDir);
            CreateComponentFiles(installDir, selectedComponents);

            if (desktopShortcut)
                CreateDesktopShortcut(installDir);
            if (_config.GetBool("Shortcuts", "CreateStartMenuEntry"))
                CreateStartMenuEntry(installDir);
            if (startMenuPin)
                PinToStartMenu(installDir);
            if (activeSetup && _config.GetBool("ActiveSetup", "Enabled"))
                RegisterActiveSetup(installDir);
            if (_config.GetBool("AppPaths", "Enabled"))
                RegisterAppPaths(installDir);
            if (_config.GetBool("FileAssociations", "Enabled"))
                RegisterFileAssociations(installDir);
            if (_config.GetBool("ContextMenu", "Enabled"))
                RegisterContextMenu(installDir);
            if (_config.GetBool("EnvironmentVariables", "Enabled"))
                SetEnvironmentVariables(installDir);
            if (_config.GetBool("Services", "Enabled"))
                InstallService(installDir);
            if (_config.GetBool("ScheduledTasks", "Enabled"))
                CreateScheduledTask(installDir);
            if (_config.GetBool("FirewallRules", "Enabled"))
                CreateFirewallRules();
            if (_config.GetBool("ProtocolHandlers", "Enabled"))
                RegisterProtocolHandlers(installDir);
            if (_config.GetBool("Startup", "Enabled"))
                CreateStartupEntry(installDir);
            if (_config.GetBool("Fonts", "Enabled"))
                InstallFont(installDir);
            if (_config.GetBool("Uninstall", "RegisterUninstaller"))
                RegisterUninstaller(installDir);

            // Create intentional leftover files if configured
            if (_manifest.IntentionallyLeaveFiles)
            {
                foreach (var file in _manifest.LeftoverFiles)
                {
                    var dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    File.WriteAllText(file, $"Intentional leftover file created by TestPackage at {DateTime.Now}");
                    _log($"Created intentional leftover: {file}");
                }
            }

            // Write description.txt - a human-readable summary of what this installer does
            WriteDescription(installDir);

            // Save the manifest last
            _manifest.Save(installDir);
            _log("Installation manifest saved.");
        }

        private void WriteDescription(string installDir)
        {
            var sb = new System.Text.StringBuilder();
            var appName = _config.Get("General", "AppName", "TestPackage");
            var appVersion = _config.Get("General", "AppVersion", "2.0.0");

            sb.AppendLine($"{appName} v{appVersion}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();
            sb.AppendLine("This is a simulated installation created by TestPackage Configurator.");
            sb.AppendLine("It was designed to test software packaging, deployment, and");
            sb.AppendLine("virtualization tools by exercising real Windows installer behaviors.");
            sb.AppendLine();
            sb.AppendLine($"Installed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Location:  {installDir}");
            sb.AppendLine($"Context:   {_manifest.InstallContext}");
            sb.AppendLine($"User:      {_manifest.InstalledBy}");
            sb.AppendLine();
            sb.AppendLine("Configured behaviors:");
            sb.AppendLine(new string('-', 40));

            if (_manifest.CreatedFiles.Count > 0)
                sb.AppendLine($"  Test files:           {_manifest.CreatedFiles.Count} files created");
            if (_manifest.RegistryEntries.Count > 0)
                sb.AppendLine($"  Registry entries:     {_manifest.RegistryEntries.Count} entries written");
            if (_manifest.Components.Count > 0)
                sb.AppendLine($"  Components:           {string.Join(", ", _manifest.Components)}");
            if (_manifest.DesktopShortcut)
                sb.AppendLine("  Desktop shortcut:     Yes");
            if (_manifest.StartMenuEntry)
                sb.AppendLine("  Start Menu entry:     Yes");
            if (_manifest.StartMenuPinned)
                sb.AppendLine("  Start Menu pinned:    Yes");
            if (_manifest.ActiveSetup)
                sb.AppendLine("  Active Setup:         Registered");
            if (_manifest.AppPathsRegistered)
                sb.AppendLine("  App Paths:            Registered");
            if (_manifest.FileAssociationsRegistered)
                sb.AppendLine("  File associations:    Registered");
            if (_manifest.ContextMenuRegistered)
                sb.AppendLine("  Context menu:         Registered");
            if (_manifest.EnvironmentVariablesSet)
                sb.AppendLine("  Environment vars:     Set");
            if (_manifest.ServiceInstalled)
                sb.AppendLine("  Windows service:      Installed");
            if (_manifest.ScheduledTaskCreated)
                sb.AppendLine("  Scheduled task:       Created");
            if (_manifest.FirewallRulesCreated)
                sb.AppendLine("  Firewall rules:       Created");
            if (_manifest.ProtocolHandlerRegistered)
                sb.AppendLine("  Protocol handler:     Registered");
            if (_manifest.StartupEntryCreated)
                sb.AppendLine("  Startup entry:        Created");
            if (_manifest.FontInstalled)
                sb.AppendLine("  Font:                 Installed");
            if (_manifest.IntentionallyLeaveFiles)
                sb.AppendLine("  Leftover files:       Will remain after uninstall");
            if (_manifest.IntentionallyLeaveRegistry)
                sb.AppendLine("  Leftover registry:    Will remain after uninstall");

            sb.AppendLine();
            sb.AppendLine("This file was generated by TestPackage (https://rwksystems.com/test-package).");

            var path = Path.Combine(installDir, "description.txt");
            File.WriteAllText(path, sb.ToString());
            _manifest.CreatedFiles.Add(path);
            _log("Created description.txt");
        }

        private void CreateDirectories(string installDir)
        {
            _log($"Creating install directory: {installDir}");
            Directory.CreateDirectory(installDir);
            _manifest.CreatedDirectories.Add(installDir);
        }

        private void CopyApplicationFiles(string installDir)
        {
            var sourceDir = AppDomain.CurrentDomain.BaseDirectory;
            var appExeName = AppExeName;
            var appBaseName = Path.GetFileNameWithoutExtension(appExeName);

            // Copy the simulated app executable and its sidecar files
            foreach (var file in Directory.GetFiles(sourceDir, $"{appBaseName}*"))
            {
                var destFile = Path.Combine(installDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
                _manifest.CreatedFiles.Add(destFile);
                _log($"Copied: {Path.GetFileName(file)}");
            }

            // Copy runtime deps if present
            foreach (var pattern in new[] { "*.dll", "*.runtimeconfig.json", "*.deps.json" })
            {
                foreach (var file in Directory.GetFiles(sourceDir, pattern))
                {
                    var destFile = Path.Combine(installDir, Path.GetFileName(file));
                    if (!File.Exists(destFile))
                    {
                        File.Copy(file, destFile, true);
                        _manifest.CreatedFiles.Add(destFile);
                    }
                }
            }

            // Copy config.ini
            var configSrc = Path.Combine(sourceDir, "config.ini");
            if (File.Exists(configSrc))
            {
                var configDest = Path.Combine(installDir, "config.ini");
                File.Copy(configSrc, configDest, true);
                _manifest.CreatedFiles.Add(configDest);
                _log("Copied: config.ini");
            }

            _log("Application files installed.");
        }

        private void CreateTestFiles(string installDir)
        {
            if (!_config.GetBool("TestFiles", "Enabled")) return;

            var files = _config.GetList("TestFiles", "Files");
            foreach (var entry in files)
            {
                var parts = entry.Split('|');
                var path = _config.ExpandVariables(parts[0], installDir);
                var content = parts.Length > 1 ? parts[1] : $"TestPackage marker file - created {DateTime.Now}";

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _manifest.CreatedDirectories.Add(dir);
                }

                File.WriteAllText(path, content);
                _manifest.CreatedFiles.Add(path);
                _log($"Created test file: {path}");
            }
        }

        private void WriteRegistryEntries(string installDir)
        {
            if (!_config.GetBool("Registry", "Enabled")) return;

            var entries = _config.GetList("Registry", "Entries");
            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                if (parts.Length < 4) continue;

                var keyPath = parts[0].Trim();
                var valueName = parts[1].Trim();
                var valueType = parts[2].Trim();
                var valueData = _config.ExpandVariables(parts[3].Trim(), installDir);

                try
                {
                    WriteRegistryValue(keyPath, valueName, valueType, valueData);
                    _manifest.RegistryEntries.Add(entry);
                    _log($"Registry: {keyPath}\\{valueName} = {valueData}");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not write registry {keyPath}: {ex.Message}");
                }
            }
        }

        private void WriteRegistryValue(string keyPath, string valueName, string valueType, string valueData)
        {
            RegistryKey? root = null;
            string subKey;

            if (keyPath.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase))
            {
                root = Registry.LocalMachine;
                subKey = keyPath[5..];
            }
            else if (keyPath.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase))
            {
                root = Registry.CurrentUser;
                subKey = keyPath[5..];
            }
            else return;

            using var key = root.CreateSubKey(subKey, true);
            if (key == null) return;

            switch (valueType.ToUpper())
            {
                case "REG_SZ":
                    key.SetValue(valueName, valueData, RegistryValueKind.String);
                    break;
                case "REG_DWORD":
                    key.SetValue(valueName, int.Parse(valueData), RegistryValueKind.DWord);
                    break;
                case "REG_EXPAND_SZ":
                    key.SetValue(valueName, valueData, RegistryValueKind.ExpandString);
                    break;
                case "REG_MULTI_SZ":
                    key.SetValue(valueName, valueData.Split(';'), RegistryValueKind.MultiString);
                    break;
            }
        }

        private void CreateComponentFiles(string installDir, List<string> components)
        {
            foreach (var component in components)
            {
                var compDir = Path.Combine(installDir, "Components", component.Replace(" ", ""));
                Directory.CreateDirectory(compDir);
                _manifest.CreatedDirectories.Add(compDir);

                var markerFile = Path.Combine(compDir, $"{component.Replace(" ", "")}.marker");
                File.WriteAllText(markerFile, $"Component: {component}\nInstalled: {DateTime.Now}");
                _manifest.CreatedFiles.Add(markerFile);
                _log($"Installed component: {component}");
            }
        }

        private void CreateDesktopShortcut(string installDir)
        {
            var name = _config.Get("Shortcuts", "DesktopShortcutName", "TestPackage");
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktop, $"{name}.lnk");

            CreateShortcut(shortcutPath, Path.Combine(installDir, AppExeName), installDir);
            _manifest.Shortcuts.Add(shortcutPath);
            _manifest.DesktopShortcut = true;
            _log($"Created desktop shortcut: {shortcutPath}");
        }

        private void CreateStartMenuEntry(string installDir)
        {
            var appName = _config.Get("General", "AppName", "TestPackage");
            var folder = _config.Get("Shortcuts", "StartMenuFolder", @"RWK Systems\TestPackage");
            var startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs", folder);
            Directory.CreateDirectory(startMenu);
            _manifest.CreatedDirectories.Add(startMenu);

            var shortcutPath = Path.Combine(startMenu, $"{appName}.lnk");
            CreateShortcut(shortcutPath, Path.Combine(installDir, AppExeName), installDir);
            _manifest.Shortcuts.Add(shortcutPath);
            _manifest.StartMenuEntry = true;
            _log($"Created Start Menu entry: {shortcutPath}");

            // Uninstall shortcut
            var uninstallPath = Path.Combine(startMenu, $"Uninstall {appName}.lnk");
            CreateShortcut(uninstallPath, Path.Combine(installDir, AppExeName), installDir, "--uninstall");
            _manifest.Shortcuts.Add(uninstallPath);
        }

        private void PinToStartMenu(string installDir)
        {
            _manifest.StartMenuPinned = true;
            _log("Start Menu pin requested (application registered).");
        }

        private void RegisterActiveSetup(string installDir)
        {
            var guid = _config.Get("General", "AppGUID");
            var stubPath = _config.ExpandVariables(
                _config.Get("ActiveSetup", "StubPath", $"{installDir}\\{AppExeName} --activesetup"),
                installDir);
            var version = _config.Get("ActiveSetup", "Version", "1,0,0,0");

            var keyPath = $@"SOFTWARE\Microsoft\Active Setup\Installed Components\{guid}";
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(keyPath, true);
                if (key != null)
                {
                    key.SetValue("", _config.Get("General", "AppName", "TestPackage"));
                    key.SetValue("StubPath", stubPath);
                    key.SetValue("Version", version);
                    _manifest.RegistryEntries.Add($"HKLM\\{keyPath}");
                    _manifest.ActiveSetup = true;
                    _log($"Registered Active Setup: {guid}");
                }
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not register Active Setup (requires admin): {ex.Message}");
            }
        }

        private void RegisterAppPaths(string installDir)
        {
            var exeName = _config.Get("AppPaths", "ExeName", AppExeName);
            var keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}";
            var exePath = Path.Combine(installDir, exeName);

            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(keyPath, true);
                if (key != null)
                {
                    key.SetValue("", exePath);
                    key.SetValue("Path", installDir);
                    _manifest.RegistryEntries.Add($"HKLM\\{keyPath}");
                    _manifest.AppPathsRegistered = true;
                    _log($"Registered App Path: {exeName}");
                }
            }
            catch
            {
                try
                {
                    using var key = Registry.CurrentUser.CreateSubKey(keyPath, true);
                    if (key != null)
                    {
                        key.SetValue("", exePath);
                        key.SetValue("Path", installDir);
                        _manifest.RegistryEntries.Add($"HKCU\\{keyPath}");
                        _manifest.AppPathsRegistered = true;
                        _log($"Registered App Path (user): {exeName}");
                    }
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not register App Path: {ex.Message}");
                }
            }
        }

        private void RegisterFileAssociations(string installDir)
        {
            var associations = _config.GetList("FileAssociations", "Associations");
            foreach (var assoc in associations)
            {
                var parts = assoc.Split('|');
                if (parts.Length < 4) continue;

                var ext = parts[0].Trim();
                var progId = parts[1].Trim();
                var desc = parts[2].Trim();
                var icon = _config.ExpandVariables(parts[3].Trim(), installDir);

                try
                {
                    using (var progKey = Registry.ClassesRoot.CreateSubKey(progId, true))
                    {
                        progKey?.SetValue("", desc);
                        using var iconKey = progKey?.CreateSubKey("DefaultIcon", true);
                        iconKey?.SetValue("", icon);
                        using var cmdKey = progKey?.CreateSubKey(@"shell\open\command", true);
                        cmdKey?.SetValue("", $"\"{Path.Combine(installDir, AppExeName)}\" \"%1\"");
                    }

                    using (var extKey = Registry.ClassesRoot.CreateSubKey(ext, true))
                    {
                        extKey?.SetValue("", progId);
                    }

                    _manifest.RegistryEntries.Add($"HKCR\\{progId}");
                    _manifest.RegistryEntries.Add($"HKCR\\{ext}");
                    _log($"Registered file association: {ext} -> {progId}");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not register file association {ext}: {ex.Message}");
                }
            }
            _manifest.FileAssociationsRegistered = true;
        }

        private void RegisterContextMenu(string installDir)
        {
            var entries = _config.GetList("ContextMenu", "Entries");
            foreach (var entry in entries)
            {
                var parts = entry.Split('|');
                if (parts.Length < 3) continue;

                var ext = parts[0].Trim();
                var menuText = parts[1].Trim();
                var command = _config.ExpandVariables(parts[2].Trim(), installDir);
                var menuId = "TestPackage_" + menuText.Replace(" ", "");

                try
                {
                    var keyPath = $@"{ext}\shell\{menuId}";
                    using (var key = Registry.ClassesRoot.CreateSubKey(keyPath, true))
                    {
                        key?.SetValue("", menuText);
                        using var cmdKey = key?.CreateSubKey("command", true);
                        cmdKey?.SetValue("", command);
                    }
                    _manifest.RegistryEntries.Add($"HKCR\\{keyPath}");
                    _log($"Registered context menu: {menuText} for {ext}");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not register context menu {menuText}: {ex.Message}");
                }
            }
            _manifest.ContextMenuRegistered = true;
        }

        private void SetEnvironmentVariables(string installDir)
        {
            var variables = _config.GetList("EnvironmentVariables", "Variables");
            foreach (var variable in variables)
            {
                var parts = variable.Split('|');
                if (parts.Length < 3) continue;

                var scope = parts[0].Trim();
                var name = parts[1].Trim();
                var value = _config.ExpandVariables(parts[2].Trim(), installDir);

                try
                {
                    var target = scope.Equals("System", StringComparison.OrdinalIgnoreCase)
                        ? EnvironmentVariableTarget.Machine
                        : EnvironmentVariableTarget.User;
                    Environment.SetEnvironmentVariable(name, value, target);
                    _manifest.RegistryEntries.Add($"EnvVar|{scope}|{name}");
                    _log($"Set environment variable: {name}={value} ({scope})");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not set env var {name}: {ex.Message}");
                }
            }
            _manifest.EnvironmentVariablesSet = true;
        }

        private void InstallService(string installDir)
        {
            var serviceName = _config.Get("Services", "ServiceName", "TestPackageSvc");
            var displayName = _config.Get("Services", "ServiceDisplayName", "TestPackage Service");
            var description = _config.Get("Services", "ServiceDescription", "TestPackage test service");
            var startType = _config.Get("Services", "ServiceStartType", "Manual");

            try
            {
                var exePath = Path.Combine(installDir, AppExeName);
                var startFlag = startType.Equals("Automatic", StringComparison.OrdinalIgnoreCase) ? "auto" :
                                startType.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ? "disabled" : "demand";

                RunProcess("sc.exe", $"create \"{serviceName}\" binpath= \"{exePath} --service\" displayname= \"{displayName}\" start= {startFlag}");
                RunProcess("sc.exe", $"description \"{serviceName}\" \"{description}\"");

                _manifest.ServiceInstalled = true;
                _manifest.ServiceName = serviceName;
                _log($"Installed service: {serviceName}");
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not install service (requires admin): {ex.Message}");
            }
        }

        private void CreateScheduledTask(string installDir)
        {
            var taskName = _config.Get("ScheduledTasks", "TaskName", "TestPackage Task");
            var description = _config.Get("ScheduledTasks", "TaskDescription", "TestPackage scheduled task");
            var schedule = _config.Get("ScheduledTasks", "TaskSchedule", "Daily");
            var time = _config.Get("ScheduledTasks", "TaskTime", "12:00");

            try
            {
                var exePath = Path.Combine(installDir, AppExeName);
                var schedFlag = schedule.ToLower() switch
                {
                    "weekly" => "WEEKLY",
                    "atlogon" => "ONLOGON",
                    "atstartup" => "ONSTART",
                    _ => "DAILY"
                };

                var args = $"/Create /TN \"{taskName}\" /TR \"\\\"{exePath}\\\" --scheduled\" /SC {schedFlag} /F";
                if (schedFlag == "DAILY" || schedFlag == "WEEKLY")
                    args += $" /ST {time}";

                RunProcess("schtasks.exe", args);
                _manifest.ScheduledTaskCreated = true;
                _manifest.ScheduledTaskName = taskName;
                _log($"Created scheduled task: {taskName}");
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not create scheduled task: {ex.Message}");
            }
        }

        private void CreateFirewallRules()
        {
            var rules = _config.GetList("FirewallRules", "Rules");
            foreach (var rule in rules)
            {
                var parts = rule.Split('|');
                if (parts.Length < 5) continue;

                var name = parts[0].Trim();
                var direction = parts[1].Trim().ToLower() == "in" ? "in" : "out";
                var action = parts[2].Trim().ToLower() == "block" ? "block" : "allow";
                var protocol = parts[3].Trim();
                var port = parts[4].Trim();

                try
                {
                    RunProcess("netsh.exe",
                        $"advfirewall firewall add rule name=\"{name}\" dir={direction} action={action} protocol={protocol} localport={port}");
                    _manifest.FirewallRuleNames.Add(name);
                    _log($"Created firewall rule: {name}");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not create firewall rule {name}: {ex.Message}");
                }
            }
            _manifest.FirewallRulesCreated = true;
        }

        private void RegisterProtocolHandlers(string installDir)
        {
            var protocols = _config.GetList("ProtocolHandlers", "Protocols");
            foreach (var proto in protocols)
            {
                var parts = proto.Split('|');
                if (parts.Length < 2) continue;

                var protocol = parts[0].Trim();
                var desc = parts[1].Trim();

                try
                {
                    using var key = Registry.ClassesRoot.CreateSubKey(protocol, true);
                    if (key != null)
                    {
                        key.SetValue("", $"URL:{desc}");
                        key.SetValue("URL Protocol", "");
                        using var cmdKey = key.CreateSubKey(@"shell\open\command", true);
                        cmdKey?.SetValue("", $"\"{Path.Combine(installDir, AppExeName)}\" \"%1\"");
                    }
                    _manifest.RegistryEntries.Add($"HKCR\\{protocol}");
                    _log($"Registered protocol handler: {protocol}://");
                }
                catch (Exception ex)
                {
                    _log($"Warning: Could not register protocol {protocol}: {ex.Message}");
                }
            }
            _manifest.ProtocolHandlerRegistered = true;
        }

        private void CreateStartupEntry(string installDir)
        {
            var method = _config.Get("Startup", "Method", "Registry");
            var scope = _config.Get("Startup", "Scope", "User");
            var appName = _config.Get("General", "AppName", "TestPackage");
            var exePath = Path.Combine(installDir, AppExeName);

            try
            {
                if (method.Equals("Registry", StringComparison.OrdinalIgnoreCase))
                {
                    var root = scope.Equals("Machine", StringComparison.OrdinalIgnoreCase)
                        ? Registry.LocalMachine : Registry.CurrentUser;
                    using var key = root.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    key?.SetValue(appName, $"\"{exePath}\"");
                    _manifest.RegistryEntries.Add(
                        $"{(scope.Equals("Machine", StringComparison.OrdinalIgnoreCase) ? "HKLM" : "HKCU")}\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run|{appName}");
                }
                else
                {
                    var startupFolder = Environment.GetFolderPath(
                        scope.Equals("Machine", StringComparison.OrdinalIgnoreCase)
                            ? Environment.SpecialFolder.CommonStartup
                            : Environment.SpecialFolder.Startup);
                    var shortcutPath = Path.Combine(startupFolder, $"{appName}.lnk");
                    CreateShortcut(shortcutPath, exePath, installDir);
                    _manifest.Shortcuts.Add(shortcutPath);
                }
                _manifest.StartupEntryCreated = true;
                _log($"Created startup entry ({method}, {scope})");
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not create startup entry: {ex.Message}");
            }
        }

        private void InstallFont(string installDir)
        {
            var fontFile = _config.Get("Fonts", "FontFile", "");
            var fontName = _config.Get("Fonts", "FontName", "TestPackage Font");

            if (string.IsNullOrEmpty(fontFile)) return;

            try
            {
                var fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
                var srcFont = Path.Combine(installDir, fontFile);
                var destFont = Path.Combine(fontsDir, Path.GetFileName(fontFile));

                if (File.Exists(srcFont))
                {
                    File.Copy(srcFont, destFont, true);
                    using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts", true);
                    key?.SetValue($"{fontName} (TrueType)", Path.GetFileName(fontFile));
                    _manifest.FontInstalled = true;
                    _log($"Installed font: {fontName}");
                }
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not install font: {ex.Message}");
            }
        }

        private void RegisterUninstaller(string installDir)
        {
            var appName = _config.Get("General", "AppName", "TestPackage");
            var appVersion = _config.Get("General", "AppVersion", "2.0.0");
            var publisher = _config.Get("General", "AppPublisher", "RWK Systems");
            var guid = _config.Get("General", "AppGUID");
            var exePath = Path.Combine(installDir, AppExeName);

            var keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}";
            try
            {
                var root = _manifest.InstallContext.Equals("Machine", StringComparison.OrdinalIgnoreCase)
                    ? Registry.LocalMachine : Registry.CurrentUser;

                using var key = root.CreateSubKey(keyPath, true);
                if (key != null)
                {
                    key.SetValue("DisplayName", appName);
                    key.SetValue("DisplayVersion", appVersion);
                    key.SetValue("Publisher", publisher);
                    key.SetValue("UninstallString", $"\"{exePath}\" --uninstall");
                    key.SetValue("QuietUninstallString", $"\"{exePath}\" --uninstall --quiet");
                    key.SetValue("InstallLocation", installDir);
                    key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
                    key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                    key.SetValue("NoRepair", 1, RegistryValueKind.DWord);

                    var size = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories)
                        .Sum(f => new FileInfo(f).Length) / 1024;
                    key.SetValue("EstimatedSize", (int)size, RegistryValueKind.DWord);
                }

                var rootName = _manifest.InstallContext.Equals("Machine", StringComparison.OrdinalIgnoreCase) ? "HKLM" : "HKCU";
                _manifest.RegistryEntries.Add($"{rootName}\\{keyPath}");
                _log($"Registered uninstaller in Add/Remove Programs");
            }
            catch (Exception ex)
            {
                _log($"Warning: Could not register uninstaller: {ex.Message}");
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string arguments = "")
        {
            var ps = $@"$ws = New-Object -ComObject WScript.Shell; $s = $ws.CreateShortcut('{shortcutPath}'); $s.TargetPath = '{targetPath}'; $s.WorkingDirectory = '{workingDir}'; $s.Arguments = '{arguments}'; $s.Save()";
            RunProcess("powershell.exe", $"-NoProfile -Command \"{ps}\"");
        }

        private void RunProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(30000);
        }
    }
}
