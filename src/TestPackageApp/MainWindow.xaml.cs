using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;
using TestPackage.Core;

namespace TestPackageApp
{
    public partial class MainWindow : Window
    {
        private InstallManifest? _manifest;

        public MainWindow()
        {
            InitializeComponent();
            ClampToScreen();
            LoadManifest();
            PopulateContext();
            PopulateInstallDetails();
            PopulateTestFiles();
            PopulateRegistryStatus();
            PopulateFeatures();
        }

        private void ClampToScreen()
        {
            var workArea = SystemParameters.WorkArea;
            MaxHeight = workArea.Height;
            if (Width > workArea.Width)
                Width = workArea.Width;
        }

        private void LoadManifest()
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                _manifest = InstallManifest.Load(exeDir);
            }
            catch { }
        }

        private void PopulateContext()
        {
            RunningAs.Text = $@"{Environment.UserDomainName}\{Environment.UserName}";
            UserDomain.Text = Environment.UserDomainName;
            MachineName.Text = Environment.MachineName;
            OSVersion.Text = $"{Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")})";
            SessionId.Text = Process.GetCurrentProcess().SessionId.ToString();

            bool isAdmin;
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            IsElevated.Text = isAdmin ? "Yes (Elevated)" : "No (Standard User)";
            IsElevated.Foreground = new SolidColorBrush(isAdmin ? Color.FromRgb(0x2E, 0x7D, 0x32) : Color.FromRgb(0xC6, 0x28, 0x28));

            // Integrity level
            IntegrityLevel.Text = isAdmin ? "High" : "Medium";

            // Show version from manifest, or fall back to assembly version
            if (_manifest != null)
                VersionText.Text = $"v{_manifest.AppVersion}";
            else
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (asm != null)
                    VersionText.Text = $"v{asm.Major}.{asm.Minor}.{asm.Build}";
            }
        }

        private void PopulateInstallDetails()
        {
            if (_manifest == null)
            {
                InstallLocation.Text = AppDomain.CurrentDomain.BaseDirectory;
                InstallContext.Text = "Unknown (no manifest)";
                InstalledBy.Text = "Unknown";
                InstallDate.Text = "Unknown";
                InstalledComponents.Text = "Unknown";
                return;
            }

            InstallLocation.Text = _manifest.InstallDir;
            InstallContext.Text = _manifest.InstallContext;
            InstalledBy.Text = _manifest.InstalledBy;
            InstallDate.Text = _manifest.InstallDate.ToString("yyyy-MM-dd HH:mm:ss");
            InstalledComponents.Text = _manifest.Components.Count > 0
                ? string.Join(", ", _manifest.Components)
                : "None";
        }

        private void PopulateTestFiles()
        {
            TestFilesPanel.Children.Clear();

            if (_manifest == null || _manifest.CreatedFiles.Count == 0)
            {
                TestFilesPanel.Children.Add(new TextBlock
                {
                    Text = "No test files recorded in manifest.",
                    FontSize = 12,
                    Foreground = Brushes.Gray
                });
                return;
            }

            foreach (var file in _manifest.CreatedFiles)
            {
                var exists = File.Exists(file);
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

                panel.Children.Add(new TextBlock
                {
                    Text = exists ? "\u2713" : "\u2717",
                    Foreground = new SolidColorBrush(exists ? Color.FromRgb(0x2E, 0x7D, 0x32) : Color.FromRgb(0xC6, 0x28, 0x28)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Width = 20
                });
                panel.Children.Add(new TextBlock
                {
                    Text = file,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(exists ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xC6, 0x28, 0x28)),
                    VerticalAlignment = VerticalAlignment.Center
                });

                TestFilesPanel.Children.Add(panel);
            }
        }

        private void PopulateRegistryStatus()
        {
            RegistryPanel.Children.Clear();

            if (_manifest == null || _manifest.RegistryEntries.Count == 0)
            {
                RegistryPanel.Children.Add(new TextBlock
                {
                    Text = "No registry entries recorded in manifest.",
                    FontSize = 12,
                    Foreground = Brushes.Gray
                });
                return;
            }

            foreach (var entry in _manifest.RegistryEntries)
            {
                bool exists = CheckRegistryEntry(entry);
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

                panel.Children.Add(new TextBlock
                {
                    Text = exists ? "\u2713" : "\u2717",
                    Foreground = new SolidColorBrush(exists ? Color.FromRgb(0x2E, 0x7D, 0x32) : Color.FromRgb(0xC6, 0x28, 0x28)),
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Width = 20
                });
                panel.Children.Add(new TextBlock
                {
                    Text = entry.Length > 80 ? entry[..77] + "..." : entry,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(exists ? Color.FromRgb(0x33, 0x33, 0x33) : Color.FromRgb(0xC6, 0x28, 0x28)),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = entry
                });

                RegistryPanel.Children.Add(panel);
            }
        }

        private bool CheckRegistryEntry(string entry)
        {
            try
            {
                // Handle env var entries
                if (entry.StartsWith("EnvVar|")) return true;

                var parts = entry.Split('|');
                var keyPath = parts[0];

                RegistryKey? root = null;
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
                else return false;

                using var key = root.OpenSubKey(subKey);
                return key != null;
            }
            catch { return false; }
        }

        private void PopulateFeatures()
        {
            FeaturesPanel.Children.Clear();

            if (_manifest == null) return;

            AddFeatureStatus("Desktop Shortcut", _manifest.DesktopShortcut);
            AddFeatureStatus("Start Menu Entry", _manifest.StartMenuEntry);
            AddFeatureStatus("Start Menu Pinned", _manifest.StartMenuPinned);
            AddFeatureStatus("Active Setup", _manifest.ActiveSetup);
            AddFeatureStatus("App Paths", _manifest.AppPathsRegistered);
            AddFeatureStatus("File Associations", _manifest.FileAssociationsRegistered);
            AddFeatureStatus("Context Menu", _manifest.ContextMenuRegistered);
            AddFeatureStatus("Environment Variables", _manifest.EnvironmentVariablesSet);
            AddFeatureStatus("Windows Service", _manifest.ServiceInstalled);
            AddFeatureStatus("Scheduled Task", _manifest.ScheduledTaskCreated);
            AddFeatureStatus("Firewall Rules", _manifest.FirewallRulesCreated);
            AddFeatureStatus("Protocol Handler", _manifest.ProtocolHandlerRegistered);
            AddFeatureStatus("Startup Entry", _manifest.StartupEntryCreated);
            AddFeatureStatus("Font Installed", _manifest.FontInstalled);
            AddFeatureStatus("Intentional Leftover Files", _manifest.IntentionallyLeaveFiles);
            AddFeatureStatus("Intentional Leftover Registry", _manifest.IntentionallyLeaveRegistry);
        }

        private void AddFeatureStatus(string name, bool enabled)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            panel.Children.Add(new TextBlock
            {
                Text = enabled ? "\u2713 Enabled" : "\u2013 Not configured",
                Foreground = new SolidColorBrush(enabled ? Color.FromRgb(0x2E, 0x7D, 0x32) : Color.FromRgb(0x99, 0x99, 0x99)),
                FontSize = 12,
                Width = 120,
                FontWeight = enabled ? FontWeights.Medium : FontWeights.Normal
            });
            panel.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                VerticalAlignment = VerticalAlignment.Center
            });
            FeaturesPanel.Children.Add(panel);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadManifest();
            PopulateContext();
            PopulateInstallDetails();
            PopulateTestFiles();
            PopulateRegistryStatus();
            PopulateFeatures();
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to uninstall TestPackage?\n\nThis will remove installed files, registry entries, and shortcuts.",
                "TestPackage - Uninstall",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var uninstallWindow = new UninstallWindow(false);
                uninstallWindow.Show();
                Close();
            }
        }

        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
    }
}
