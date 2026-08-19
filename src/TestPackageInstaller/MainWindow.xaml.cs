using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using TestPackage.Core;

namespace TestPackageInstaller
{
    public partial class MainWindow : Window
    {
        private ConfigParser _config = null!;
        private readonly List<StackPanel> _pages = new();
        private int _currentPage;
        private InstallActions? _installer;

        private bool _previewMode;

        public MainWindow()
        {
            InitializeComponent();
            _previewMode = App.IsPreviewMode;
            LoadConfig();
            BuildPageList();
            ShowPage(0);

            if (_previewMode)
            {
                Title += " [PREVIEW]";
                HeaderSubtitle.Text = "Preview Mode — no changes will be made";
            }
        }

        private void LoadConfig()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_data", "config.ini");
            if (!File.Exists(configPath))
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            _config = ConfigParser.Load(configPath);

            // Apply UI customization
            var bannerColor = _config.Get("UI", "BannerColor", "#0078D4");
            try
            {
                HeaderBanner.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(bannerColor));
            }
            catch { /* use default */ }

            var appName = _config.Get("General", "AppName", "TestPackage");
            Title = $"{appName} Setup";
            HeaderTitle.Text = $"{appName} Setup";

            // EULA text
            EULAText.Text = _config.Get("EULA", "EULAText",
                "This is a test application. No warranty provided.");

            // Default install path
            var defaultPath = _config.Get("TargetDirectory", "DefaultPath",
                @"%ProgramFiles%\RWK Systems\TestPackage");
            InstallPath.Text = _config.ExpandVariables(defaultPath, "");

            // Default context
            var defaultContext = _config.Get("InstallContext", "DefaultContext", "Machine");
            ContextMachine.IsChecked = defaultContext.Equals("Machine", StringComparison.OrdinalIgnoreCase);
            ContextUser.IsChecked = !ContextMachine.IsChecked;

            // Build components list
            var components = _config.GetSection("Components");
            foreach (var comp in components)
            {
                var cb = new CheckBox
                {
                    Content = FormatComponentName(comp.Key),
                    Tag = comp.Key,
                    IsChecked = comp.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
                    FontSize = 13,
                    Margin = new Thickness(0, 6, 0, 6)
                };
                ComponentsList.Children.Add(cb);
            }

            // Options defaults
            OptDesktopShortcut.IsChecked = _config.GetBool("Shortcuts", "CreateDesktopShortcut", true);
            OptStartMenuPin.IsChecked = _config.GetBool("Shortcuts", "PinToStartMenu");
            OptActiveSetup.IsChecked = _config.GetBool("ActiveSetup", "Enabled");
            OptReboot.IsChecked = _config.GetBool("Reboot", "PromptForReboot");

            // Hide options not configured
            if (!_config.GetBool("WizardPages", "ShowDesktopShortcut", true))
                OptDesktopShortcut.Visibility = Visibility.Collapsed;
            if (!_config.GetBool("WizardPages", "ShowStartMenuPin", true))
                OptStartMenuPin.Visibility = Visibility.Collapsed;
            if (!_config.GetBool("WizardPages", "ShowActiveSetup", true))
                OptActiveSetup.Visibility = Visibility.Collapsed;
            if (!_config.GetBool("WizardPages", "ShowRebootOption", true))
                OptReboot.Visibility = Visibility.Collapsed;
        }

        private void BuildPageList()
        {
            _pages.Clear();

            if (_config.GetBool("WizardPages", "ShowWelcome", true))
                _pages.Add(WelcomePage);
            if (_config.GetBool("WizardPages", "ShowEULA", true))
                _pages.Add(EULAPage);
            if (_config.GetBool("WizardPages", "ShowInstallContext", true))
                _pages.Add(ContextPage);
            if (_config.GetBool("WizardPages", "ShowTargetDirectory", true))
                _pages.Add(DirectoryPage);
            if (_config.GetBool("WizardPages", "ShowComponents", true))
                _pages.Add(ComponentsPage);

            // Always show options page (it aggregates shortcut/reboot/active setup options)
            _pages.Add(OptionsPage);

            // Installing and Complete pages are always last
            _pages.Add(InstallingPage);
            _pages.Add(CompletePage);
        }

        private void ShowPage(int index)
        {
            // Hide all pages
            foreach (var page in new StackPanel[] {
                WelcomePage, EULAPage, ContextPage, DirectoryPage,
                ComponentsPage, OptionsPage, InstallingPage, CompletePage })
            {
                page.Visibility = Visibility.Collapsed;
            }

            _currentPage = index;
            var currentPanel = _pages[index];
            currentPanel.Visibility = Visibility.Visible;

            // Update navigation buttons
            BtnBack.Visibility = index > 0 && currentPanel != InstallingPage && currentPanel != CompletePage
                ? Visibility.Visible : Visibility.Collapsed;
            BtnNext.Visibility = currentPanel != OptionsPage && currentPanel != InstallingPage && currentPanel != CompletePage
                ? Visibility.Visible : Visibility.Collapsed;
            BtnInstall.Visibility = currentPanel == OptionsPage ? Visibility.Visible : Visibility.Collapsed;
            BtnFinish.Visibility = currentPanel == CompletePage ? Visibility.Visible : Visibility.Collapsed;
            BtnCancel.Visibility = currentPanel != CompletePage && currentPanel != InstallingPage
                ? Visibility.Visible : Visibility.Collapsed;

            // Update header subtitle
            HeaderSubtitle.Text = currentPanel.Name switch
            {
                "WelcomePage" => "Welcome",
                "EULAPage" => "License Agreement",
                "ContextPage" => "Installation Context",
                "DirectoryPage" => "Choose Location",
                "ComponentsPage" => "Select Components",
                "OptionsPage" => "Additional Options",
                "InstallingPage" => "Installing...",
                "CompletePage" => "Complete",
                _ => "Installation Wizard"
            };
        }

        private bool ValidateCurrentPage()
        {
            var current = _pages[_currentPage];

            if (current == EULAPage)
            {
                if (AcceptEULA.IsChecked != true)
                {
                    MessageBox.Show("You must accept the license agreement to continue.",
                        "TestPackage Setup", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            else if (current == DirectoryPage)
            {
                var path = InstallPath.Text.Trim();
                if (string.IsNullOrEmpty(path))
                {
                    MessageBox.Show("Please enter an installation directory.",
                        "TestPackage Setup", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                try
                {
                    Path.GetFullPath(path);
                }
                catch
                {
                    MessageBox.Show("The specified path is not valid.",
                        "TestPackage Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            return true;
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentPage()) return;
            if (_currentPage < _pages.Count - 1)
                ShowPage(_currentPage + 1);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 0)
                ShowPage(_currentPage - 1);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel the installation?",
                "TestPackage Setup", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Close();
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (_previewMode)
            {
                MessageBox.Show("This is a preview. No installation will be performed.\n\nIn the real installer, this would begin the installation process.",
                    "Preview Mode", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var installDir = InstallPath.Text.Trim();
            var context = ContextMachine.IsChecked == true ? "Machine" : "User";

            // Elevation pre-check: writing to Program Files / Windows / ProgramData
            // needs admin. If we're not elevated, offer to re-launch as admin
            // before wasting the user's time walking to a mid-install failure.
            if (!IsRunningAsAdmin() && PathRequiresAdmin(installDir))
            {
                var choice = MessageBox.Show(
                    "The chosen install directory needs administrator privileges:\n\n" +
                    installDir + "\n\n" +
                    "Restart the installer as administrator?\n\n" +
                    "Yes: relaunch with a UAC prompt.\n" +
                    "No:  cancel this install so you can pick a per-user location.",
                    "TestPackage Setup", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (choice == MessageBoxResult.Yes)
                {
                    TryRelaunchElevated();
                }
                return;
            }

            // Move to Installing page
            var installPageIndex = _pages.IndexOf(InstallingPage);
            ShowPage(installPageIndex);

            var selectedComponents = new List<string>();
            foreach (CheckBox cb in ComponentsList.Children)
            {
                if (cb.IsChecked == true && cb.Tag is string tag)
                    selectedComponents.Add(tag);
            }

            var desktopShortcut = OptDesktopShortcut.IsChecked == true;
            var startMenuPin = OptStartMenuPin.IsChecked == true;
            var activeSetup = OptActiveSetup.IsChecked == true;

            _installer = new InstallActions(_config, LogMessage);

            var delay = _config.GetInt("UI", "InstallDelayMs", 500);
            var simulate = _config.GetBool("UI", "SimulateInstallDelay", true);

            var installSucceeded = false;
            string? installError = null;

            await Task.Run(async () =>
            {
                try
                {
                    var steps = new (string name, Action action)[]
                    {
                        ("Preparing installation...", () => { }),
                        ("Creating directories...", () => { }),
                        ("Copying application files...", () => { }),
                        ("Writing test files...", () => { }),
                        ("Configuring registry...", () => { }),
                        ("Installing components...", () => { }),
                        ("Creating shortcuts...", () => { }),
                        ("Registering application...", () => { }),
                        ("Finalizing installation...", () =>
                            _installer.Execute(installDir, context, selectedComponents,
                                desktopShortcut, startMenuPin, activeSetup))
                    };

                    for (int i = 0; i < steps.Length; i++)
                    {
                        var progress = (int)((double)(i + 1) / steps.Length * 100);
                        var step = steps[i];

                        Dispatcher.Invoke(() =>
                        {
                            InstallStatus.Text = step.name;
                            InstallProgress.Value = progress;
                        });

                        if (simulate && i < steps.Length - 1)
                            await Task.Delay(delay);

                        step.action();
                    }

                    installSucceeded = true;
                    Dispatcher.Invoke(() =>
                    {
                        InstallProgress.Value = 100;
                        InstallStatus.Text = "Installation complete!";
                    });
                }
                catch (Exception ex)
                {
                    installError = ex.Message;
                    Dispatcher.Invoke(() =>
                    {
                        LogMessage($"ERROR: {ex.Message}");
                        InstallStatus.Text = "Installation failed!";
                    });
                }
            });

            if (!installSucceeded)
            {
                // Stay on the Installing page so the log is visible. Show the
                // error, and offer to re-launch elevated when the failure looks
                // like an access-denied on an admin-only path.
                var msg = "Installation failed:\n" + (installError ?? "unknown error");
                var offersElevation = !IsRunningAsAdmin()
                    && (installError?.Contains("Access to the path", StringComparison.OrdinalIgnoreCase) == true
                        || installError?.Contains("Access is denied", StringComparison.OrdinalIgnoreCase) == true
                        || PathRequiresAdmin(installDir));
                if (offersElevation)
                {
                    var choice = MessageBox.Show(
                        msg + "\n\nThis usually means administrator privileges are needed.\n\nRelaunch the installer as administrator?",
                        "TestPackage Setup", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (choice == MessageBoxResult.Yes) TryRelaunchElevated();
                }
                else
                {
                    MessageBox.Show(msg, "TestPackage Setup", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                // Enable Cancel again so the user can close cleanly, and offer
                // to jump back to the earlier pages.
                BtnCancel.Visibility = Visibility.Visible;
                BtnBack.Visibility = _pages.IndexOf(OptionsPage) >= 0 ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            // Build summary — only on genuine success.
            var manifest = _installer!.Manifest;
            SummaryText.Text = $"Installed to: {manifest.InstallDir}\n" +
                              $"Context: {manifest.InstallContext}\n" +
                              $"Components: {string.Join(", ", manifest.Components)}\n" +
                              $"Files created: {manifest.CreatedFiles.Count}\n" +
                              $"Registry entries: {manifest.RegistryEntries.Count}\n" +
                              $"Shortcuts: {manifest.Shortcuts.Count}";

            ShowPage(_pages.IndexOf(CompletePage));
        }

        // ----- Elevation helpers -----

        private static bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        private static bool PathRequiresAdmin(string path)
        {
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(path ?? "");
                var full = System.IO.Path.GetFullPath(expanded);
                string[] adminRoots =
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                };
                foreach (var root in adminRoots)
                {
                    if (!string.IsNullOrEmpty(root)
                        && full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        private void TryRelaunchElevated()
        {
            try
            {
                var exe = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exe)) return;
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(psi);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not relaunch as administrator:\n" + ex.Message,
                    "TestPackage Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (LaunchApp.IsChecked == true && _installer?.Manifest != null)
            {
                var appExe = Path.Combine(_installer.Manifest.InstallDir, _installer.AppExeName);
                if (File.Exists(appExe))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = appExe,
                            UseShellExecute = true
                        });
                    }
                    catch { /* app will be launched manually */ }
                }
            }

            // Handle reboot
            if (OptReboot.IsChecked == true && _config.GetBool("Reboot", "ForceReboot"))
            {
                var result = MessageBox.Show(
                    "A reboot is required to complete the installation.\n\nReboot now?",
                    "TestPackage Setup", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("shutdown", "/r /t 10 /c \"TestPackage installation requires a reboot.\"");
                }
            }

            Close();
        }

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select installation folder",
                ShowNewFolderButton = true,
                SelectedPath = InstallPath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                InstallPath.Text = dialog.SelectedPath;
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

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                InstallLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                InstallLog.ScrollToEnd();
            });
        }

        private static string FormatComponentName(string key)
        {
            // Convert PascalCase to spaced: "CoreFiles" -> "Core Files"
            var result = new System.Text.StringBuilder();
            foreach (var c in key)
            {
                if (char.IsUpper(c) && result.Length > 0)
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }
    }
}
