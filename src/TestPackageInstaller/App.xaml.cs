using System;
using System.Collections.Generic;
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
        public static bool IsSilentMode { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var args = e.Args;
            IsPreviewMode = args.Contains("--preview", StringComparer.OrdinalIgnoreCase);
            IsSilentMode = args.Any(a => a.Equals("/S", StringComparison.OrdinalIgnoreCase)
                                      || a.Equals("/silent", StringComparison.OrdinalIgnoreCase)
                                      || a.Equals("--silent", StringComparison.OrdinalIgnoreCase));

            // Find config.ini in _data subfolder first, then next to the executable
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(exeDir, "_data", "config.ini");
            if (!File.Exists(configPath))
                configPath = Path.Combine(exeDir, "config.ini");

            if (!File.Exists(configPath))
            {
                if (!IsSilentMode)
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
                            Arguments = string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
                            Verb = "runas",
                            UseShellExecute = true
                        };
                        try
                        {
                            Process.Start(psi);
                        }
                        catch (Exception)
                        {
                            if (!IsSilentMode)
                                MessageBox.Show(
                                    "This installer requires administrator privileges.\nPlease run as administrator.",
                                    "TestPackage Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        Shutdown();
                        return;
                    }
                }

                // Silent mode: skip the wizard, install directly
                if (IsSilentMode)
                {
                    RunSilentInstall(config, args);
                    return;
                }
            }
            catch (Exception ex)
            {
                if (!IsSilentMode)
                    MessageBox.Show(
                        $"Error loading configuration:\n{ex.Message}",
                        "TestPackage Installer", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void RunSilentInstall(ConfigParser config, string[] args)
        {
            try
            {
                // Parse command-line overrides
                var installDir = GetArgValue(args, "/D") ?? GetArgValue(args, "/dir")
                    ?? config.ExpandVariables(
                        config.Get("TargetDirectory", "DefaultPath", @"%ProgramFiles%\RWK Systems\TestPackage"), "");

                var context = GetArgValue(args, "/context")
                    ?? config.Get("InstallContext", "DefaultContext", "Machine");

                // Component selection
                var componentArg = GetArgValue(args, "/components");
                List<string> selectedComponents;
                if (componentArg != null)
                {
                    selectedComponents = componentArg.Split(',').Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s)).ToList();
                }
                else
                {
                    var comps = config.GetSection("Components");
                    selectedComponents = comps.Where(c => c.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.Key).ToList();
                }

                // Option overrides — command line wins, then config defaults
                bool desktopShortcut = HasFlag(args, "/shortcut") ? true
                    : HasFlag(args, "/noshortcut") ? false
                    : config.GetBool("Shortcuts", "CreateDesktopShortcut", true);

                bool startMenuPin = HasFlag(args, "/startmenupin") ? true
                    : HasFlag(args, "/nostartmenupin") ? false
                    : config.GetBool("Shortcuts", "PinToStartMenu");

                bool activeSetup = HasFlag(args, "/activesetup") ? true
                    : HasFlag(args, "/noactivesetup") ? false
                    : config.GetBool("ActiveSetup", "Enabled");

                bool reboot = HasFlag(args, "/reboot") ? true
                    : HasFlag(args, "/noreboot") ? false
                    : config.GetBool("Reboot", "PromptForReboot");

                // Run the install
                var installer = new InstallActions(config, msg => { /* silent — no log output */ });
                installer.Execute(installDir, context, selectedComponents, desktopShortcut, startMenuPin, activeSetup);

                // Handle reboot
                if (reboot && config.GetBool("Reboot", "ForceReboot"))
                {
                    Process.Start("shutdown", "/r /t 10 /c \"TestPackage installation requires a reboot.\"");
                }

                Shutdown(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Silent install failed: {ex.Message}");
                Shutdown(1);
            }
        }

        private static string? GetArgValue(string[] args, string key)
        {
            // Supports: /D=C:\path  or  /D C:\path  or  /context=user  or  /context user
            foreach (var arg in args)
            {
                if (arg.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    return arg[(key.Length + 1)..];
                if (arg.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
                    return arg[(key.Length + 1)..];
            }

            // Two-part form: /D C:\path
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        private static bool HasFlag(string[] args, string flag)
        {
            return args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
