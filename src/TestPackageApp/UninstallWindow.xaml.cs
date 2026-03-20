using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace TestPackageApp
{
    public partial class UninstallWindow : Window
    {
        private readonly bool _quiet;

        public UninstallWindow(bool quiet)
        {
            InitializeComponent();
            _quiet = quiet;
            Loaded += async (_, _) => await PerformUninstall();
        }

        private async Task PerformUninstall()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var manifestPath = Path.Combine(exeDir, "install-manifest.json");

            InstallManifest? manifest = null;
            if (File.Exists(manifestPath))
            {
                try
                {
                    var json = File.ReadAllText(manifestPath);
                    manifest = JsonSerializer.Deserialize<InstallManifest>(json);
                }
                catch (Exception ex)
                {
                    Log($"Warning: Could not load manifest: {ex.Message}");
                }
            }

            if (manifest == null)
            {
                Log("No installation manifest found. Performing basic cleanup only.");
                manifest = new InstallManifest { InstallDir = exeDir };
            }

            var steps = new List<(string description, Action action)>
            {
                ("Removing shortcuts...", () => RemoveShortcuts(manifest)),
                ("Removing registry entries...", () => RemoveRegistry(manifest)),
                ("Removing services...", () => RemoveService(manifest)),
                ("Removing scheduled tasks...", () => RemoveScheduledTasks(manifest)),
                ("Removing firewall rules...", () => RemoveFirewallRules(manifest)),
                ("Removing environment variables...", () => RemoveEnvironmentVariables(manifest)),
                ("Removing startup entries...", () => RemoveStartupEntries(manifest)),
                ("Removing uninstaller registration...", () => RemoveUninstaller(manifest)),
                ("Removing test files...", () => RemoveFiles(manifest)),
                ("Cleaning up directories...", () => RemoveDirectories(manifest)),
            };

            await Task.Run(async () =>
            {
                for (int i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    var progress = (int)((double)(i + 1) / steps.Count * 100);

                    Dispatcher.Invoke(() =>
                    {
                        UninstallProgress.Value = progress;
                    });

                    Log(step.description);
                    try
                    {
                        step.action();
                    }
                    catch (Exception ex)
                    {
                        Log($"  Warning: {ex.Message}");
                    }

                    await Task.Delay(300);
                }
            });

            Log("");
            Log("=== Uninstall Complete ===");

            if (manifest.IntentionallyLeaveFiles && manifest.LeftoverFiles.Count > 0)
            {
                Log("");
                Log("NOTE: The following files were intentionally left behind:");
                foreach (var f in manifest.LeftoverFiles)
                    Log($"  {f}");
            }
            if (manifest.IntentionallyLeaveRegistry && manifest.LeftoverRegistry.Count > 0)
            {
                Log("");
                Log("NOTE: The following registry entries were intentionally left behind:");
                foreach (var r in manifest.LeftoverRegistry)
                    Log($"  {r}");
            }

            Dispatcher.Invoke(() =>
            {
                UninstallProgress.Value = 100;
                BtnClose.IsEnabled = true;
            });

            // Schedule self-deletion
            ScheduleSelfDelete(manifest.InstallDir);

            if (_quiet)
            {
                await Task.Delay(1000);
                Dispatcher.Invoke(Close);
            }
        }

        private void RemoveShortcuts(InstallManifest manifest)
        {
            foreach (var shortcut in manifest.Shortcuts)
            {
                try
                {
                    if (File.Exists(shortcut))
                    {
                        File.Delete(shortcut);
                        Log($"  Removed: {shortcut}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"  Warning: Could not remove {shortcut}: {ex.Message}");
                }
            }
        }

        private void RemoveRegistry(InstallManifest manifest)
        {
            foreach (var entry in manifest.RegistryEntries)
            {
                // Skip entries that should be left behind
                if (manifest.IntentionallyLeaveRegistry)
                {
                    bool skip = false;
                    foreach (var leftover in manifest.LeftoverRegistry)
                    {
                        if (leftover.StartsWith(entry.Split('|')[0], StringComparison.OrdinalIgnoreCase))
                        {
                            skip = true;
                            break;
                        }
                    }
                    if (skip) continue;
                }

                try
                {
                    if (entry.StartsWith("EnvVar|"))
                    {
                        continue; // Handled separately
                    }

                    var keyPath = entry.Split('|')[0];
                    DeleteRegistryKey(keyPath);
                    Log($"  Removed: {keyPath}");
                }
                catch (Exception ex)
                {
                    Log($"  Warning: Could not remove registry entry: {ex.Message}");
                }
            }
        }

        private void DeleteRegistryKey(string keyPath)
        {
            RegistryKey? root;
            string subKey;

            if (keyPath.StartsWith("HKLM\\"))
            {
                root = Registry.LocalMachine;
                subKey = keyPath[5..];
            }
            else if (keyPath.StartsWith("HKCU\\"))
            {
                root = Registry.CurrentUser;
                subKey = keyPath[5..];
            }
            else if (keyPath.StartsWith("HKCR\\"))
            {
                root = Registry.ClassesRoot;
                subKey = keyPath[5..];
            }
            else return;

            try { root.DeleteSubKeyTree(subKey, false); } catch { }
        }

        private void RemoveService(InstallManifest manifest)
        {
            if (!manifest.ServiceInstalled) return;
            try
            {
                RunProcess("sc.exe", "stop TestPackageSvc");
                RunProcess("sc.exe", "delete TestPackageSvc");
                Log("  Removed service: TestPackageSvc");
            }
            catch (Exception ex)
            {
                Log($"  Warning: {ex.Message}");
            }
        }

        private void RemoveScheduledTasks(InstallManifest manifest)
        {
            if (!manifest.ScheduledTaskCreated) return;
            try
            {
                RunProcess("schtasks.exe", "/Delete /TN \"TestPackage Maintenance\" /F");
                Log("  Removed scheduled task");
            }
            catch (Exception ex)
            {
                Log($"  Warning: {ex.Message}");
            }
        }

        private void RemoveFirewallRules(InstallManifest manifest)
        {
            if (!manifest.FirewallRulesCreated) return;
            try
            {
                RunProcess("netsh.exe", "advfirewall firewall delete rule name=\"TestPackage Inbound\"");
                RunProcess("netsh.exe", "advfirewall firewall delete rule name=\"TestPackage Outbound\"");
                Log("  Removed firewall rules");
            }
            catch (Exception ex)
            {
                Log($"  Warning: {ex.Message}");
            }
        }

        private void RemoveEnvironmentVariables(InstallManifest manifest)
        {
            if (!manifest.EnvironmentVariablesSet) return;
            foreach (var entry in manifest.RegistryEntries)
            {
                if (!entry.StartsWith("EnvVar|")) continue;
                var parts = entry.Split('|');
                if (parts.Length < 3) continue;

                var scope = parts[1];
                var name = parts[2];
                try
                {
                    var target = scope.Equals("System", StringComparison.OrdinalIgnoreCase)
                        ? EnvironmentVariableTarget.Machine
                        : EnvironmentVariableTarget.User;
                    Environment.SetEnvironmentVariable(name, null, target);
                    Log($"  Removed env var: {name}");
                }
                catch { }
            }
        }

        private void RemoveStartupEntries(InstallManifest manifest)
        {
            if (!manifest.StartupEntryCreated) return;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue("TestPackage", false);
                Log("  Removed startup entry");
            }
            catch { }
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue("TestPackage", false);
            }
            catch { }
        }

        private void RemoveUninstaller(InstallManifest manifest)
        {
            var guid = manifest.AppGUID;
            if (string.IsNullOrEmpty(guid)) return;

            var keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}";
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
                Log("  Removed uninstaller registration");
            }
            catch { }
        }

        private void RemoveFiles(InstallManifest manifest)
        {
            foreach (var file in manifest.CreatedFiles)
            {
                // Skip intentional leftovers
                if (manifest.IntentionallyLeaveFiles && manifest.LeftoverFiles.Contains(file))
                    continue;

                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        Log($"  Deleted: {file}");
                    }
                }
                catch (Exception ex)
                {
                    Log($"  Warning: Could not delete {file}: {ex.Message}");
                }
            }
        }

        private void RemoveDirectories(InstallManifest manifest)
        {
            // Remove directories in reverse order (deepest first)
            var dirs = new List<string>(manifest.CreatedDirectories);
            dirs.Sort((a, b) => b.Length.CompareTo(a.Length));

            foreach (var dir in dirs)
            {
                try
                {
                    if (Directory.Exists(dir))
                    {
                        var remaining = Directory.GetFileSystemEntries(dir);
                        if (remaining.Length == 0)
                        {
                            Directory.Delete(dir);
                            Log($"  Removed directory: {dir}");
                        }
                        else
                        {
                            Log($"  Skipped non-empty directory: {dir} ({remaining.Length} items)");
                        }
                    }
                }
                catch { }
            }
        }

        private void ScheduleSelfDelete(string installDir)
        {
            // Schedule batch file to delete the install directory after the app exits
            try
            {
                var batchPath = Path.Combine(Path.GetTempPath(), "testpackage_cleanup.cmd");
                var script = $"""
                    @echo off
                    timeout /t 3 /nobreak >nul
                    rd /s /q "{installDir}" 2>nul
                    del "%~f0"
                    """;
                File.WriteAllText(batchPath, script);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                Log("  Scheduled directory cleanup on exit.");
            }
            catch { }
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
            proc?.WaitForExit(15000);
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                UninstallLog.AppendText($"{message}\n");
                UninstallLog.ScrollToEnd();
            });
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
