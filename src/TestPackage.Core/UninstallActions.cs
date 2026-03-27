using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TestPackage.Core
{
    public class UninstallActions
    {
        private readonly Action<string> _log;

        public UninstallActions(Action<string> log)
        {
            _log = log;
        }

        public void Execute(InstallManifest manifest)
        {
            RemoveShortcuts(manifest);
            RemoveRegistry(manifest);
            RemoveService(manifest);
            RemoveScheduledTasks(manifest);
            RemoveFirewallRules(manifest);
            RemoveEnvironmentVariables(manifest);
            RemoveStartupEntries(manifest);
            RemoveUninstaller(manifest);
            RemoveFiles(manifest);
            RemoveDirectories(manifest);
        }

        public void RemoveShortcuts(InstallManifest manifest)
        {
            foreach (var shortcut in manifest.Shortcuts)
            {
                try
                {
                    if (File.Exists(shortcut))
                    {
                        File.Delete(shortcut);
                        _log($"  Removed: {shortcut}");
                    }
                }
                catch (Exception ex)
                {
                    _log($"  Warning: Could not remove {shortcut}: {ex.Message}");
                }
            }
        }

        public void RemoveRegistry(InstallManifest manifest)
        {
            foreach (var entry in manifest.RegistryEntries)
            {
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
                        continue;

                    var keyPath = entry.Split('|')[0];
                    DeleteRegistryKey(keyPath);
                    _log($"  Removed: {keyPath}");
                }
                catch (Exception ex)
                {
                    _log($"  Warning: Could not remove registry entry: {ex.Message}");
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

        public void RemoveService(InstallManifest manifest)
        {
            if (!manifest.ServiceInstalled) return;
            try
            {
                RunProcess("sc.exe", "stop TestPackageSvc");
                RunProcess("sc.exe", "delete TestPackageSvc");
                _log("  Removed service: TestPackageSvc");
            }
            catch (Exception ex)
            {
                _log($"  Warning: {ex.Message}");
            }
        }

        public void RemoveScheduledTasks(InstallManifest manifest)
        {
            if (!manifest.ScheduledTaskCreated) return;
            try
            {
                RunProcess("schtasks.exe", "/Delete /TN \"TestPackage Maintenance\" /F");
                _log("  Removed scheduled task");
            }
            catch (Exception ex)
            {
                _log($"  Warning: {ex.Message}");
            }
        }

        public void RemoveFirewallRules(InstallManifest manifest)
        {
            if (!manifest.FirewallRulesCreated) return;
            try
            {
                RunProcess("netsh.exe", "advfirewall firewall delete rule name=\"TestPackage Inbound\"");
                RunProcess("netsh.exe", "advfirewall firewall delete rule name=\"TestPackage Outbound\"");
                _log("  Removed firewall rules");
            }
            catch (Exception ex)
            {
                _log($"  Warning: {ex.Message}");
            }
        }

        public void RemoveEnvironmentVariables(InstallManifest manifest)
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
                    _log($"  Removed env var: {name}");
                }
                catch { }
            }
        }

        public void RemoveStartupEntries(InstallManifest manifest)
        {
            if (!manifest.StartupEntryCreated) return;
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                key?.DeleteValue("TestPackage", false);
                _log("  Removed startup entry");
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

        public void RemoveUninstaller(InstallManifest manifest)
        {
            var guid = manifest.AppGUID;
            if (string.IsNullOrEmpty(guid)) return;

            var keyPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{guid}";
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree(keyPath, false);
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
                _log("  Removed uninstaller registration");
            }
            catch { }
        }

        public void RemoveFiles(InstallManifest manifest)
        {
            foreach (var file in manifest.CreatedFiles)
            {
                if (manifest.IntentionallyLeaveFiles && manifest.LeftoverFiles.Contains(file))
                    continue;

                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        _log($"  Deleted: {file}");
                    }
                }
                catch (Exception ex)
                {
                    _log($"  Warning: Could not delete {file}: {ex.Message}");
                }
            }
        }

        public void RemoveDirectories(InstallManifest manifest)
        {
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
                            _log($"  Removed directory: {dir}");
                        }
                        else
                        {
                            _log($"  Skipped non-empty directory: {dir} ({remaining.Length} items)");
                        }
                    }
                }
                catch { }
            }
        }

        public void ScheduleSelfDelete(string installDir)
        {
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
                _log("  Scheduled directory cleanup on exit.");
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
    }
}
