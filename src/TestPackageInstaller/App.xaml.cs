using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using TestPackage.Core;

namespace TestPackageInstaller
{
    public partial class App : Application
    {
        public static bool IsPreviewMode { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            IsPreviewMode = e.Args.Contains("--preview", StringComparer.OrdinalIgnoreCase);

            // Find config.ini in _data subfolder first, then next to the executable
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(exeDir, "_data", "config.ini");
            if (!File.Exists(configPath))
                configPath = Path.Combine(exeDir, "config.ini");

            if (!File.Exists(configPath))
            {
                MessageBox.Show(
                    "config.ini not found.\n\nThe configuration file must be placed alongside the installer executable.",
                    "TestPackage Installer - Configuration Missing",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            try
            {
                var config = ConfigParser.Load(configPath);

                // Skip UAC in preview mode
                if (!IsPreviewMode && config.GetBool("General", "RequireAdmin"))
                {
                    if (!IsRunningAsAdmin())
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "",
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        try
                        {
                            Process.Start(psi);
                        }
                        catch (Exception)
                        {
                            MessageBox.Show(
                                "This installer requires administrator privileges.\nPlease run as administrator.",
                                "TestPackage Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        Shutdown();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading configuration:\n{ex.Message}",
                    "TestPackage Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
