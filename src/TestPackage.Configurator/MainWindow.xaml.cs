using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using TestPackage.Core;

namespace TestPackage.Configurator
{
    public partial class MainWindow : Window
    {
        private ConfigModel _model = new();

        public MainWindow()
        {
            InitializeComponent();
            ClampToScreen();
            TxtOutputFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            PopulateFromModel(_model);
        }

        private void ClampToScreen()
        {
            var workArea = SystemParameters.WorkArea;
            if (Height > workArea.Height)
                Height = workArea.Height;
            if (Width > workArea.Width)
                Width = workArea.Width;
        }

        private void PopulateFromModel(ConfigModel m)
        {
            // Output names
            TxtInstallerExeName.Text = m.InstallerExeName;
            TxtAppExeName.Text = m.AppExeName;

            // General
            TxtAppName.Text = m.AppName;
            TxtAppVersion.Text = m.AppVersion;
            TxtAppPublisher.Text = m.AppPublisher;
            TxtAppURL.Text = m.AppURL;
            TxtAppGUID.Text = m.AppGUID;
            ChkRequireAdmin.IsChecked = m.RequireAdmin;
            CboDefaultContext.SelectedIndex = m.DefaultContext.Equals("User", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            TxtDefaultPath.Text = m.DefaultPath;
            ChkAllowCustomPath.IsChecked = m.AllowCustomPath;

            // Wizard Pages
            ChkShowWelcome.IsChecked = m.ShowWelcome;
            ChkShowEULA.IsChecked = m.ShowEULA;
            ChkShowInstallContext.IsChecked = m.ShowInstallContext;
            ChkShowTargetDirectory.IsChecked = m.ShowTargetDirectory;
            ChkShowComponents.IsChecked = m.ShowComponents;
            ChkShowDesktopShortcut.IsChecked = m.ShowDesktopShortcut;
            ChkShowStartMenuPin.IsChecked = m.ShowStartMenuPin;
            ChkShowRebootOption.IsChecked = m.ShowRebootOption;
            ChkShowActiveSetup.IsChecked = m.ShowActiveSetup;
            TxtEULAText.Text = m.EULAText;

            // Wizard page defaults
            ChkDefaultDesktopShortcut.IsChecked = m.CreateDesktopShortcut;
            ChkDefaultStartMenuPin.IsChecked = m.PinToStartMenu;
            ChkDefaultActiveSetup.IsChecked = m.ActiveSetupEnabled;
            ChkDefaultReboot.IsChecked = m.PromptForReboot;

            // Components
            ComponentsList.Children.Clear();
            foreach (var c in m.Components)
            {
                AddComponentRow(c.Name, c.DefaultSelected);
            }

            // System Actions
            ChkTestFilesEnabled.IsChecked = m.TestFilesEnabled;
            TxtTestFiles.Text = m.TestFiles;
            ChkRegistryEnabled.IsChecked = m.RegistryEnabled;
            TxtRegistryEntries.Text = m.RegistryEntries;
            ChkDesktopShortcut.IsChecked = m.CreateDesktopShortcut;
            TxtDesktopShortcutName.Text = m.DesktopShortcutName;
            ChkStartMenuEntry.IsChecked = m.CreateStartMenuEntry;
            TxtStartMenuFolder.Text = m.StartMenuFolder;
            ChkPinToStartMenu.IsChecked = m.PinToStartMenu;
            ChkFileAssociations.IsChecked = m.FileAssociationsEnabled;
            TxtFileAssociations.Text = m.FileAssociations;
            ChkContextMenu.IsChecked = m.ContextMenuEnabled;
            TxtContextMenuEntries.Text = m.ContextMenuEntries;
            ChkEnvVars.IsChecked = m.EnvironmentVariablesEnabled;
            TxtEnvVars.Text = m.EnvironmentVariables;

            // Advanced
            ChkService.IsChecked = m.ServicesEnabled;
            TxtServiceName.Text = m.ServiceName;
            TxtServiceDisplayName.Text = m.ServiceDisplayName;
            SelectComboItem(CboServiceStartType, m.ServiceStartType);
            ChkScheduledTask.IsChecked = m.ScheduledTasksEnabled;
            TxtTaskName.Text = m.TaskName;
            SelectComboItem(CboTaskSchedule, m.TaskSchedule);
            ChkFirewall.IsChecked = m.FirewallRulesEnabled;
            TxtFirewallRules.Text = m.FirewallRules;
            ChkProtocolHandlers.IsChecked = m.ProtocolHandlersEnabled;
            TxtProtocolHandlers.Text = m.ProtocolHandlers;
            ChkActiveSetup.IsChecked = m.ActiveSetupEnabled;
            ChkAppPaths.IsChecked = m.AppPathsEnabled;
            ChkStartup.IsChecked = m.StartupEnabled;
            SelectComboItem(CboStartupMethod, m.StartupMethod);

            // Uninstall & Reboot
            ChkRegisterUninstaller.IsChecked = m.RegisterUninstaller;
            ChkCleanFiles.IsChecked = m.CleanFiles;
            ChkCleanRegistry.IsChecked = m.CleanRegistry;
            ChkCleanShortcuts.IsChecked = m.CleanShortcuts;
            ChkLeaveFiles.IsChecked = m.IntentionallyLeaveFiles;
            TxtLeftoverFiles.Text = m.LeftoverFiles;
            ChkLeaveRegistry.IsChecked = m.IntentionallyLeaveRegistry;
            TxtLeftoverRegistry.Text = m.LeftoverRegistry;
            ChkPromptReboot.IsChecked = m.PromptForReboot;
            ChkForceReboot.IsChecked = m.ForceReboot;

            // UI
            TxtBannerColor.Text = m.BannerColor;
            TxtAccentColor.Text = m.AccentColor;
            UpdateColorPreview(BannerColorPreview, m.BannerColor);
            UpdateColorPreview(AccentColorPreview, m.AccentColor);
            ChkShowProgressBar.IsChecked = m.ShowProgressBar;
            ChkSimulateDelay.IsChecked = m.SimulateInstallDelay;
            TxtDelaySec.Text = (m.InstallDelayMs / 1000.0).ToString("0.###");
        }

        private ConfigModel CollectToModel()
        {
            var m = new ConfigModel();

            m.InstallerExeName = TxtInstallerExeName.Text.Trim();
            m.AppExeName = TxtAppExeName.Text.Trim();

            m.AppName = TxtAppName.Text.Trim();
            m.AppVersion = TxtAppVersion.Text.Trim();
            m.AppPublisher = TxtAppPublisher.Text.Trim();
            m.AppURL = TxtAppURL.Text.Trim();
            m.AppGUID = TxtAppGUID.Text.Trim();
            m.RequireAdmin = ChkRequireAdmin.IsChecked == true;
            m.DefaultContext = CboDefaultContext.SelectedIndex == 1 ? "User" : "Machine";
            m.DefaultPath = TxtDefaultPath.Text.Trim();
            m.AllowCustomPath = ChkAllowCustomPath.IsChecked == true;

            m.ShowWelcome = ChkShowWelcome.IsChecked == true;
            m.ShowEULA = ChkShowEULA.IsChecked == true;
            m.ShowInstallContext = ChkShowInstallContext.IsChecked == true;
            m.ShowTargetDirectory = ChkShowTargetDirectory.IsChecked == true;
            m.ShowComponents = ChkShowComponents.IsChecked == true;
            m.ShowDesktopShortcut = ChkShowDesktopShortcut.IsChecked == true;
            m.ShowStartMenuPin = ChkShowStartMenuPin.IsChecked == true;
            m.ShowRebootOption = ChkShowRebootOption.IsChecked == true;
            m.ShowActiveSetup = ChkShowActiveSetup.IsChecked == true;
            m.EULAText = TxtEULAText.Text;

            m.Components.Clear();
            foreach (var child in ComponentsList.Children)
            {
                if (child is StackPanel panel && panel.Children.Count >= 2
                    && panel.Children[0] is CheckBox cb && panel.Children[1] is TextBlock tb)
                {
                    m.Components.Add(new ComponentEntry(tb.Text, cb.IsChecked == true));
                }
            }

            m.TestFilesEnabled = ChkTestFilesEnabled.IsChecked == true;
            m.TestFiles = TxtTestFiles.Text;
            m.RegistryEnabled = ChkRegistryEnabled.IsChecked == true;
            m.RegistryEntries = TxtRegistryEntries.Text;
            m.CreateDesktopShortcut = ChkDefaultDesktopShortcut.IsChecked == true;
            m.DesktopShortcutName = TxtDesktopShortcutName.Text.Trim();
            m.CreateStartMenuEntry = ChkStartMenuEntry.IsChecked == true;
            m.StartMenuFolder = TxtStartMenuFolder.Text.Trim();
            m.PinToStartMenu = ChkDefaultStartMenuPin.IsChecked == true;
            m.FileAssociationsEnabled = ChkFileAssociations.IsChecked == true;
            m.FileAssociations = TxtFileAssociations.Text;
            m.ContextMenuEnabled = ChkContextMenu.IsChecked == true;
            m.ContextMenuEntries = TxtContextMenuEntries.Text;
            m.EnvironmentVariablesEnabled = ChkEnvVars.IsChecked == true;
            m.EnvironmentVariables = TxtEnvVars.Text;

            m.ServicesEnabled = ChkService.IsChecked == true;
            m.ServiceName = TxtServiceName.Text.Trim();
            m.ServiceDisplayName = TxtServiceDisplayName.Text.Trim();
            m.ServiceStartType = GetComboText(CboServiceStartType);
            m.ScheduledTasksEnabled = ChkScheduledTask.IsChecked == true;
            m.TaskName = TxtTaskName.Text.Trim();
            m.TaskSchedule = GetComboText(CboTaskSchedule);
            m.FirewallRulesEnabled = ChkFirewall.IsChecked == true;
            m.FirewallRules = TxtFirewallRules.Text;
            m.ProtocolHandlersEnabled = ChkProtocolHandlers.IsChecked == true;
            m.ProtocolHandlers = TxtProtocolHandlers.Text;
            m.ActiveSetupEnabled = ChkDefaultActiveSetup.IsChecked == true;
            m.AppPathsEnabled = ChkAppPaths.IsChecked == true;
            m.StartupEnabled = ChkStartup.IsChecked == true;
            m.StartupMethod = GetComboText(CboStartupMethod);

            m.RegisterUninstaller = ChkRegisterUninstaller.IsChecked == true;
            m.CleanFiles = ChkCleanFiles.IsChecked == true;
            m.CleanRegistry = ChkCleanRegistry.IsChecked == true;
            m.CleanShortcuts = ChkCleanShortcuts.IsChecked == true;
            m.IntentionallyLeaveFiles = ChkLeaveFiles.IsChecked == true;
            m.LeftoverFiles = TxtLeftoverFiles.Text;
            m.IntentionallyLeaveRegistry = ChkLeaveRegistry.IsChecked == true;
            m.LeftoverRegistry = TxtLeftoverRegistry.Text;
            m.PromptForReboot = ChkDefaultReboot.IsChecked == true;
            m.ForceReboot = ChkForceReboot.IsChecked == true;

            m.BannerColor = TxtBannerColor.Text.Trim();
            m.AccentColor = TxtAccentColor.Text.Trim();
            m.ShowProgressBar = ChkShowProgressBar.IsChecked == true;
            m.SimulateInstallDelay = ChkSimulateDelay.IsChecked == true;
            m.InstallDelayMs = double.TryParse(TxtDelaySec.Text, out var sec) ? (int)(sec * 1000) : 500;

            // Sync derived values
            m.AppPathsExeName = m.AppExeName;

            return m;
        }

        private void GenerateInstaller_Click(object sender, RoutedEventArgs e)
        {
            var model = CollectToModel();
            var outputFolder = TxtOutputFolder.Text.Trim();

            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select an output folder.", "TestPackage Configurator",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Find template files
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = Path.Combine(exeDir, "templates");
            if (!Directory.Exists(templatesDir))
                templatesDir = exeDir;

            var installerTemplate = Path.Combine(templatesDir, "TestPackageInstaller.exe");
            var appTemplate = Path.Combine(templatesDir, "TestPackageApp.exe");

            if (!File.Exists(installerTemplate))
            {
                MessageBox.Show($"Template file not found:\n{installerTemplate}\n\nEnsure the templates directory exists alongside the Configurator.",
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputFolder);

                var destInstaller = Path.Combine(outputFolder, model.InstallerExeName);
                File.Copy(installerTemplate, destInstaller, true);

                var destApp = Path.Combine(outputFolder, model.AppExeName);
                if (File.Exists(appTemplate))
                    File.Copy(appTemplate, destApp, true);

                var configContent = ConfigWriter.Write(model);
                File.WriteAllText(Path.Combine(outputFolder, "config.ini"), configContent);

                MessageBox.Show(
                    $"Installer generated successfully!\n\nFiles created in:\n{outputFolder}\n\n" +
                    $"  {model.InstallerExeName}\n  {model.AppExeName}\n  config.ini\n\n" +
                    "Point your packaging/automation tool at the installer EXE to test.",
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Information);

                Process.Start(new ProcessStartInfo
                {
                    FileName = outputFolder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate installer:\n{ex.Message}",
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*",
                Title = "Load Configuration"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var parser = ConfigParser.Load(dialog.FileName);
                    _model = ConfigModel.FromParser(parser);
                    PopulateFromModel(_model);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load configuration:\n{ex.Message}",
                        "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "INI files (*.ini)|*.ini",
                FileName = "config.ini",
                Title = "Save Configuration"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var model = CollectToModel();
                    var content = ConfigWriter.Write(model);
                    File.WriteAllText(dialog.FileName, content);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save configuration:\n{ex.Message}",
                        "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select output folder for generated installer",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtOutputFolder.Text = dialog.SelectedPath;
        }

        private void GenerateGUID_Click(object sender, RoutedEventArgs e)
        {
            TxtAppGUID.Text = $"{{{Guid.NewGuid().ToString().ToUpper()}}}";
        }

        private void AddComponent_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtNewComponent.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;
            AddComponentRow(name, true);
            TxtNewComponent.Text = "";
        }

        private void AddComponentRow(string name, bool selected)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            panel.Children.Add(new CheckBox { IsChecked = selected, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Width = 200 });
            var removeBtn = new Button { Content = "Remove", Padding = new Thickness(6, 1, 6, 1), FontSize = 11 };
            removeBtn.Click += (_, _) => ComponentsList.Children.Remove(panel);
            panel.Children.Add(removeBtn);
            ComponentsList.Children.Add(panel);
        }

        // Color picker support
        private void BannerColorPicker_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = ShowColorPicker(TxtBannerColor.Text);
            if (color != null)
            {
                TxtBannerColor.Text = color;
                UpdateColorPreview(BannerColorPreview, color);
            }
        }

        private void AccentColorPicker_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var color = ShowColorPicker(TxtAccentColor.Text);
            if (color != null)
            {
                TxtAccentColor.Text = color;
                UpdateColorPreview(AccentColorPreview, color);
            }
        }

        private void BannerColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreview(BannerColorPreview, TxtBannerColor.Text);
        }

        private void AccentColor_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateColorPreview(AccentColorPreview, TxtAccentColor.Text);
        }

        private static void UpdateColorPreview(Border preview, string hex)
        {
            try
            {
                preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                preview.Background = Brushes.Transparent;
            }
        }

        private static string? ShowColorPicker(string currentHex)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(currentHex);
                dialog.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B);
            }
            catch { }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var c = dialog.Color;
                return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            }
            return null;
        }

        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true });
            e.Handled = true;
        }

        private static void SelectComboItem(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString()?.Equals(value, StringComparison.OrdinalIgnoreCase) == true)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }

        private static string GetComboText(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        }
    }
}
