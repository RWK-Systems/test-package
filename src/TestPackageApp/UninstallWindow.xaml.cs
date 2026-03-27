using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using TestPackage.Core;

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

            InstallManifest? manifest = null;
            try
            {
                manifest = InstallManifest.Load(exeDir);
            }
            catch (Exception ex)
            {
                Log($"Warning: Could not load manifest: {ex.Message}");
            }

            if (manifest == null)
            {
                Log("No installation manifest found. Performing basic cleanup only.");
                manifest = new InstallManifest { InstallDir = exeDir };
            }

            var uninstaller = new UninstallActions(Log);

            var steps = new (string description, Action action)[]
            {
                ("Removing shortcuts...", () => uninstaller.RemoveShortcuts(manifest)),
                ("Removing registry entries...", () => uninstaller.RemoveRegistry(manifest)),
                ("Removing services...", () => uninstaller.RemoveService(manifest)),
                ("Removing scheduled tasks...", () => uninstaller.RemoveScheduledTasks(manifest)),
                ("Removing firewall rules...", () => uninstaller.RemoveFirewallRules(manifest)),
                ("Removing environment variables...", () => uninstaller.RemoveEnvironmentVariables(manifest)),
                ("Removing startup entries...", () => uninstaller.RemoveStartupEntries(manifest)),
                ("Removing uninstaller registration...", () => uninstaller.RemoveUninstaller(manifest)),
                ("Removing test files...", () => uninstaller.RemoveFiles(manifest)),
                ("Cleaning up directories...", () => uninstaller.RemoveDirectories(manifest)),
            };

            await Task.Run(async () =>
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    var step = steps[i];
                    var progress = (int)((double)(i + 1) / steps.Length * 100);

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
            uninstaller.ScheduleSelfDelete(manifest.InstallDir);

            if (_quiet)
            {
                await Task.Delay(1000);
                Dispatcher.Invoke(Close);
            }
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
