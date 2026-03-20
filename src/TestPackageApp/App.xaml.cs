using System;
using System.Linq;
using System.Windows;

namespace TestPackageApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var args = e.Args;

            if (args.Contains("--uninstall", StringComparer.OrdinalIgnoreCase))
            {
                bool quiet = args.Contains("--quiet", StringComparer.OrdinalIgnoreCase);
                var uninstaller = new UninstallWindow(quiet);
                uninstaller.Show();
                return;
            }

            if (args.Contains("--activesetup", StringComparer.OrdinalIgnoreCase))
            {
                // Active Setup stub - just touch a marker file
                try
                {
                    var markerDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RWK Systems", "TestPackage");
                    System.IO.Directory.CreateDirectory(markerDir);
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(markerDir, "activesetup.marker"),
                        $"Active Setup executed at {DateTime.Now} by {Environment.UserName}");
                }
                catch { }
                Shutdown();
                return;
            }

            // Normal launch - show the context viewer
        }
    }
}
