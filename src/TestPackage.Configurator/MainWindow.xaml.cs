using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
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
            // Seed a fresh, dated installer name each session (users typically
            // want the date in the filename so multiple test builds don't
            // collide). Overriding here rather than baking into ConfigModel so
            // the smoke test and other Core consumers keep their stable value.
            _model.InstallerExeName = ComputeDatedInstallerName();
            InitCompositeSchemas();
            PopulateFromModel(_model);
            StartReceiptTimer();
            // First selected-tile paint after layout so TranslatePoint works.
            Dispatcher.BeginInvoke(new Action(UpdateSelectedTileFromScroll), DispatcherPriority.Loaded);
        }

        private static string ComputeDatedInstallerName()
        {
            var stamp = DateTime.Now.ToString("ddMMMyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            return $"TestPackage_{stamp}.exe";
        }

        private void ClampToScreen()
        {
            var workArea = SystemParameters.WorkArea;
            if (Height > workArea.Height) Height = workArea.Height;
            if (Width > workArea.Width) Width = workArea.Width;
        }

        // ===== Populate / Collect =====

        private void PopulateFromModel(ConfigModel m)
        {
            TxtInstallerExeName.Text = m.InstallerExeName;
            TxtAppExeName.Text = m.AppExeName;

            _suppressPathSync = true;
            TxtAppName.Text = m.AppName;
            TxtAppVersion.Text = m.AppVersion;
            TxtAppPublisher.Text = m.AppPublisher;
            TxtAppURL.Text = m.AppURL;
            TxtAppGUID.Text = m.AppGUID;
            ChkRequireAdmin.IsChecked = m.RequireAdmin;
            CboDefaultContext.SelectedIndex = m.DefaultContext.Equals("User", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            TxtDefaultPath.Text = m.DefaultPath;
            _userEditedDefaultPath = !string.Equals(m.DefaultPath, DerivePath(m.AppPublisher, m.AppName), StringComparison.OrdinalIgnoreCase);
            _suppressPathSync = false;
            ChkAllowCustomPath.IsChecked = m.AllowCustomPath;
            ChkInstallerSize.IsChecked = m.InstallerSizeEnabled;
            SetInstallerSizeMB(m.InstallerSizeMB);

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
            ChkDefaultDesktopShortcut.IsChecked = m.CreateDesktopShortcut;
            ChkDefaultStartMenuPin.IsChecked = m.PinToStartMenu;
            ChkDefaultActiveSetup.IsChecked = m.ActiveSetupEnabled;
            ChkDefaultReboot.IsChecked = m.PromptForReboot;

            ComponentsList.Children.Clear();
            foreach (var c in m.Components)
                AddComponentRow(c.Name, c.DefaultSelected);

            ChkTestFilesEnabled.IsChecked  = m.TestFilesEnabled;
            ChkRegistryEnabled.IsChecked   = m.RegistryEnabled;
            ChkDesktopShortcut.IsChecked   = m.CreateDesktopShortcut;
            TxtDesktopShortcutName.Text    = m.DesktopShortcutName;
            ChkStartMenuEntry.IsChecked    = m.CreateStartMenuEntry;
            _suppressPathSync = true;
            TxtStartMenuFolder.Text        = m.StartMenuFolder;
            _userEditedStartMenuFolder     = !string.Equals(m.StartMenuFolder, DeriveStartMenu(m.AppPublisher, m.AppName), StringComparison.OrdinalIgnoreCase);
            _suppressPathSync = false;
            ChkPinToStartMenu.IsChecked    = m.PinToStartMenu;
            ChkFileAssociations.IsChecked  = m.FileAssociationsEnabled;
            ChkContextMenu.IsChecked       = m.ContextMenuEnabled;
            ChkEnvVars.IsChecked           = m.EnvironmentVariablesEnabled;
            ChkService.IsChecked           = m.ServicesEnabled;
            TxtServiceName.Text            = m.ServiceName;
            TxtServiceDisplayName.Text     = m.ServiceDisplayName;
            SelectComboItem(CboServiceStartType, m.ServiceStartType);
            ChkScheduledTask.IsChecked     = m.ScheduledTasksEnabled;
            TxtTaskName.Text               = m.TaskName;
            SelectComboItem(CboTaskSchedule, m.TaskSchedule);
            ChkFirewall.IsChecked          = m.FirewallRulesEnabled;
            ChkProtocolHandlers.IsChecked  = m.ProtocolHandlersEnabled;
            ChkActiveSetup.IsChecked       = m.ActiveSetupEnabled;
            ChkAppPaths.IsChecked          = m.AppPathsEnabled;
            ChkStartup.IsChecked           = m.StartupEnabled;
            SelectComboItem(CboStartupMethod, m.StartupMethod);

            ChkRegisterUninstaller.IsChecked = m.RegisterUninstaller;
            ChkCleanFiles.IsChecked          = m.CleanFiles;
            ChkCleanRegistry.IsChecked       = m.CleanRegistry;
            ChkCleanShortcuts.IsChecked      = m.CleanShortcuts;
            ChkLeaveFiles.IsChecked          = m.IntentionallyLeaveFiles;
            TxtLeftoverFiles.Text            = m.LeftoverFiles;
            ChkLeaveRegistry.IsChecked       = m.IntentionallyLeaveRegistry;
            TxtLeftoverRegistry.Text         = m.LeftoverRegistry;
            ChkPromptReboot.IsChecked        = m.PromptForReboot;
            ChkForceReboot.IsChecked         = m.ForceReboot;

            TxtBannerColor.Text = m.BannerColor;
            TxtAccentColor.Text = m.AccentColor;
            UpdateColorPreview(BannerColorPreview, m.BannerColor);
            UpdateColorPreview(AccentColorPreview, m.AccentColor);
            ChkShowProgressBar.IsChecked = m.ShowProgressBar;
            ChkSimulateDelay.IsChecked   = m.SimulateInstallDelay;
            TxtDelaySec.Text = (m.InstallDelayMs / 1000.0).ToString("0.###");

            SelectComboItem(CboCodeSigningMode, string.IsNullOrWhiteSpace(m.CodeSigningMode) ? "None" : m.CodeSigningMode);
            TxtCodeSigningPfxPath.Text     = m.CodeSigningPfxPath ?? "";
            PwbCodeSigningPassword.Password = m.CodeSigningPfxPassword ?? "";
            TxtCodeSigningTimestamp.Text   = string.IsNullOrWhiteSpace(m.CodeSigningTimestampUrl)
                ? "http://timestamp.digicert.com" : m.CodeSigningTimestampUrl;
            UpdateSigningPanelVisibility();

            LoadCompositesFromModel(m);
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
            m.InstallerSizeEnabled = ChkInstallerSize.IsChecked == true;
            m.InstallerSizeMB = ParseInstallerSizeMB();

            m.ShowWelcome         = ChkShowWelcome.IsChecked == true;
            m.ShowEULA            = ChkShowEULA.IsChecked == true;
            m.ShowInstallContext  = ChkShowInstallContext.IsChecked == true;
            m.ShowTargetDirectory = ChkShowTargetDirectory.IsChecked == true;
            m.ShowComponents      = ChkShowComponents.IsChecked == true;
            m.ShowDesktopShortcut = ChkShowDesktopShortcut.IsChecked == true;
            m.ShowStartMenuPin    = ChkShowStartMenuPin.IsChecked == true;
            m.ShowRebootOption    = ChkShowRebootOption.IsChecked == true;
            m.ShowActiveSetup     = ChkShowActiveSetup.IsChecked == true;
            m.EULAText            = TxtEULAText.Text;

            m.Components.Clear();
            foreach (var child in ComponentsList.Children)
            {
                if (child is StackPanel panel && panel.Children.Count >= 2
                    && panel.Children[0] is CheckBox cb && panel.Children[1] is TextBlock tb)
                    m.Components.Add(new ComponentEntry(tb.Text, cb.IsChecked == true));
            }

            m.TestFilesEnabled            = ChkTestFilesEnabled.IsChecked == true;
            m.RegistryEnabled             = ChkRegistryEnabled.IsChecked == true;
            m.CreateDesktopShortcut       = ChkDefaultDesktopShortcut.IsChecked == true;
            m.DesktopShortcutName         = TxtDesktopShortcutName.Text.Trim();
            m.CreateStartMenuEntry        = ChkStartMenuEntry.IsChecked == true;
            m.StartMenuFolder             = TxtStartMenuFolder.Text.Trim();
            m.PinToStartMenu              = ChkDefaultStartMenuPin.IsChecked == true;
            m.FileAssociationsEnabled     = ChkFileAssociations.IsChecked == true;
            m.ContextMenuEnabled          = ChkContextMenu.IsChecked == true;
            m.EnvironmentVariablesEnabled = ChkEnvVars.IsChecked == true;

            m.ServicesEnabled       = ChkService.IsChecked == true;
            m.ServiceName           = TxtServiceName.Text.Trim();
            m.ServiceDisplayName    = TxtServiceDisplayName.Text.Trim();
            m.ServiceStartType      = GetComboText(CboServiceStartType);
            m.ScheduledTasksEnabled = ChkScheduledTask.IsChecked == true;
            m.TaskName              = TxtTaskName.Text.Trim();
            m.TaskSchedule          = GetComboText(CboTaskSchedule);
            m.FirewallRulesEnabled  = ChkFirewall.IsChecked == true;
            m.ProtocolHandlersEnabled = ChkProtocolHandlers.IsChecked == true;
            m.ActiveSetupEnabled    = ChkDefaultActiveSetup.IsChecked == true;
            m.AppPathsEnabled       = ChkAppPaths.IsChecked == true;
            m.StartupEnabled        = ChkStartup.IsChecked == true;
            m.StartupMethod         = GetComboText(CboStartupMethod);

            m.RegisterUninstaller       = ChkRegisterUninstaller.IsChecked == true;
            m.CleanFiles                = ChkCleanFiles.IsChecked == true;
            m.CleanRegistry             = ChkCleanRegistry.IsChecked == true;
            m.CleanShortcuts            = ChkCleanShortcuts.IsChecked == true;
            m.IntentionallyLeaveFiles   = ChkLeaveFiles.IsChecked == true;
            m.LeftoverFiles             = TxtLeftoverFiles.Text;
            m.IntentionallyLeaveRegistry = ChkLeaveRegistry.IsChecked == true;
            m.LeftoverRegistry          = TxtLeftoverRegistry.Text;
            m.PromptForReboot           = ChkDefaultReboot.IsChecked == true;
            m.ForceReboot               = ChkForceReboot.IsChecked == true;

            m.BannerColor = TxtBannerColor.Text.Trim();
            m.AccentColor = TxtAccentColor.Text.Trim();
            m.ShowProgressBar     = ChkShowProgressBar.IsChecked == true;
            m.SimulateInstallDelay = ChkSimulateDelay.IsChecked == true;
            m.InstallDelayMs = double.TryParse(TxtDelaySec.Text, out var sec) ? (int)(sec * 1000) : 500;
            m.AppPathsExeName = m.AppExeName;

            m.CodeSigningMode         = GetComboText(CboCodeSigningMode);
            if (string.IsNullOrWhiteSpace(m.CodeSigningMode)) m.CodeSigningMode = "None";
            m.CodeSigningPfxPath      = TxtCodeSigningPfxPath.Text.Trim();
            m.CodeSigningPfxPassword  = PwbCodeSigningPassword.Password;
            m.CodeSigningTimestampUrl = TxtCodeSigningTimestamp.Text.Trim();

            SaveCompositesToModel(m);
            return m;
        }

        // ===== Preview =====

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            var model = CollectToModel();
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = Path.Combine(exeDir, "templates");
            if (!Directory.Exists(templatesDir)) templatesDir = exeDir;

            var installerTemplate = Path.Combine(templatesDir, "TestPackageInstaller.exe");
            if (!File.Exists(installerTemplate))
            {
                MessageBox.Show("Template not found. Cannot launch preview.\n\nEnsure the templates directory exists alongside the Configurator.",
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "TestPackage_Preview_" + Guid.NewGuid().ToString("N")[..8]);
                var tempDataDir = Path.Combine(tempDir, "_data");
                Directory.CreateDirectory(tempDataDir);
                File.Copy(installerTemplate, Path.Combine(tempDir, "TestPackageInstaller.exe"), true);
                File.WriteAllText(Path.Combine(tempDataDir, "config.ini"), ConfigWriter.Write(model));
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(tempDir, "TestPackageInstaller.exe"),
                    Arguments = "--preview",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch preview:\n{ex.Message}",
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== Generate / Load / Save =====

        private void GenerateInstaller_Click(object sender, RoutedEventArgs e)
        {
            var model = CollectToModel();
            var outputFolder = TxtOutputFolder.Text.Trim();
            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select an output folder.", "TestPackage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = Path.Combine(exeDir, "templates");
            if (!Directory.Exists(templatesDir)) templatesDir = exeDir;

            var installerTemplate = Path.Combine(templatesDir, "TestPackageInstaller.exe");
            var appTemplate = Path.Combine(templatesDir, "TestPackageApp.exe");

            if (!File.Exists(installerTemplate))
            {
                MessageBox.Show($"Template not found:\n{installerTemplate}", "TestPackage", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var subfolderName = Path.GetFileNameWithoutExtension(model.InstallerExeName);
                var packageFolder = Path.Combine(outputFolder, subfolderName);
                var dataFolder = Path.Combine(packageFolder, "_data");
                Directory.CreateDirectory(dataFolder);

                var installerExePath = Path.Combine(packageFolder, model.InstallerExeName);
                File.Copy(installerTemplate, installerExePath, true);

                var sizeNote = "";
                if (model.InstallerSizeEnabled && model.InstallerSizeMB > 0)
                {
                    long targetBytes = (long)model.InstallerSizeMB * 1024L * 1024L;
                    var info = new FileInfo(installerExePath);
                    if (targetBytes > info.Length)
                    {
                        using var fs = new FileStream(installerExePath, FileMode.Open, FileAccess.Write);
                        fs.SetLength(targetBytes);
                        sizeNote = $"\n\nInstaller size: {model.InstallerSizeMB} MB";
                    }
                    else
                    {
                        sizeNote = $"\n\nRequested size ({model.InstallerSizeMB} MB) is smaller than the " +
                                   $"base installer ({info.Length / (1024 * 1024)} MB); left unpadded.";
                    }
                }

                // Code signing (after padding so the signature covers the padded file).
                // On None, the template's original signature is already invalidated by
                // padding, so the generated installer ships unsigned.
                var signNote = "\n\nSigned as: unsigned (template signature invalidated by padding)";
                if (model.CodeSigningMode.Equals("PFX", StringComparison.OrdinalIgnoreCase))
                {
                    var (ok, msg) = SignInstaller(installerExePath, model);
                    signNote = ok
                        ? $"\n\nSigned with your PFX via {msg}"
                        : $"\n\nSigning failed — installer is unsigned:\n{msg}";
                }

                if (File.Exists(appTemplate))
                    File.Copy(appTemplate, Path.Combine(dataFolder, model.AppExeName), true);
                File.WriteAllText(Path.Combine(dataFolder, "config.ini"), ConfigWriter.Write(model));

                MessageBox.Show(
                    $"Installer generated!\n\n{packageFolder}\\{model.InstallerExeName}{sizeNote}{signNote}\n\n" +
                    "Run this EXE to test your packaging workflow.",
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo { FileName = packageFolder, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*", Title = "Load Configuration" };
            if (dialog.ShowDialog() == true)
            {
                try { _model = ConfigModel.FromParser(ConfigParser.Load(dialog.FileName)); PopulateFromModel(_model); }
                catch (Exception ex) { MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "INI files (*.ini)|*.ini", FileName = "config.ini", Title = "Save Configuration" };
            if (dialog.ShowDialog() == true)
            {
                try { File.WriteAllText(dialog.FileName, ConfigWriter.Write(CollectToModel())); }
                catch (Exception ex) { MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        // ===== UI Helpers =====

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Select output folder", ShowNewFolderButton = true };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtOutputFolder.Text = dialog.SelectedPath;
        }

        private void GenerateGUID_Click(object sender, RoutedEventArgs e) =>
            TxtAppGUID.Text = $"{{{Guid.NewGuid().ToString().ToUpper()}}}";

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
            panel.Children.Add(new TextBlock { Text = name, FontSize = 13, VerticalAlignment = VerticalAlignment.Center, Width = 200 });
            ComponentsList.Children.Add(panel);
        }

        private void BannerColorPicker_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        { var c = ShowColorPicker(TxtBannerColor.Text); if (c != null) { TxtBannerColor.Text = c; UpdateColorPreview(BannerColorPreview, c); } }

        private void AccentColorPicker_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        { var c = ShowColorPicker(TxtAccentColor.Text); if (c != null) { TxtAccentColor.Text = c; UpdateColorPreview(AccentColorPreview, c); } }

        private void BannerColor_TextChanged(object sender, TextChangedEventArgs e) => UpdateColorPreview(BannerColorPreview, TxtBannerColor.Text);
        private void AccentColor_TextChanged(object sender, TextChangedEventArgs e) => UpdateColorPreview(AccentColorPreview, TxtAccentColor.Text);

        private static void UpdateColorPreview(Border preview, string hex)
        { try { preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)); } catch { preview.Background = Brushes.Transparent; } }

        private static string? ShowColorPicker(string currentHex)
        {
            var dialog = new System.Windows.Forms.ColorDialog();
            try { var c = (Color)ColorConverter.ConvertFromString(currentHex); dialog.Color = System.Drawing.Color.FromArgb(c.R, c.G, c.B); } catch { }
            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}" : null;
        }

        private void Hyperlink_Navigate(object sender, RequestNavigateEventArgs e)
        { Process.Start(new ProcessStartInfo { FileName = e.Uri.AbsoluteUri, UseShellExecute = true }); e.Handled = true; }

        private static void SelectComboItem(ComboBox combo, string value)
        { foreach (ComboBoxItem item in combo.Items) if (item.Content?.ToString()?.Equals(value, StringComparison.OrdinalIgnoreCase) == true) { item.IsSelected = true; return; } }

        private static string GetComboText(ComboBox combo) => (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        // ===== Default install path & Start Menu folder auto-derived from publisher + app name =====

        private bool _suppressPathSync;
        private bool _userEditedDefaultPath;
        private bool _userEditedStartMenuFolder;

        private static string DerivePath(string publisher, string appName)
        {
            var p = (publisher ?? "").Trim();
            var a = (appName ?? "").Trim();
            if (p.Length == 0 && a.Length == 0) return @"%ProgramFiles%";
            if (p.Length == 0) return $@"%ProgramFiles%\{a}";
            if (a.Length == 0) return $@"%ProgramFiles%\{p}";
            return $@"%ProgramFiles%\{p}\{a}";
        }

        private static string DeriveStartMenu(string publisher, string appName)
        {
            var p = (publisher ?? "").Trim();
            var a = (appName ?? "").Trim();
            if (p.Length == 0 && a.Length == 0) return "";
            if (p.Length == 0) return a;
            if (a.Length == 0) return p;
            return $@"{p}\{a}";
        }

        private void DefaultPathSource_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressPathSync) return;
            _suppressPathSync = true;
            if (!_userEditedDefaultPath)
                TxtDefaultPath.Text = DerivePath(TxtAppPublisher.Text, TxtAppName.Text);
            if (!_userEditedStartMenuFolder)
                TxtStartMenuFolder.Text = DeriveStartMenu(TxtAppPublisher.Text, TxtAppName.Text);
            _suppressPathSync = false;
        }

        private void DefaultPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressPathSync) return;
            _userEditedDefaultPath = TxtDefaultPath.Text.Trim().Length > 0;
        }

        private void StartMenuFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressPathSync) return;
            _userEditedStartMenuFolder = TxtStartMenuFolder.Text.Trim().Length > 0;
        }

        // ===== Installer Size (non-linear slider <-> exact MB textbox) =====

        private bool _suppressSizeSync;

        private static int SliderPosToMB(double pos)
        {
            pos = Math.Clamp(pos, 0, 100);
            return (int)Math.Round(Math.Pow(pos / 100.0, 3) * ConfigModel.MaxInstallerSizeMB);
        }

        private static double MBToSliderPos(int mb)
        {
            mb = Math.Clamp(mb, 0, ConfigModel.MaxInstallerSizeMB);
            return 100.0 * Math.Pow(mb / (double)ConfigModel.MaxInstallerSizeMB, 1.0 / 3.0);
        }

        private int ParseInstallerSizeMB()
        {
            if (!int.TryParse(TxtInstallerSizeMB.Text.Trim(), out var mb) || mb < 0) mb = 0;
            return Math.Min(mb, ConfigModel.MaxInstallerSizeMB);
        }

        private void SetInstallerSizeMB(int mb)
        {
            mb = Math.Clamp(mb, 0, ConfigModel.MaxInstallerSizeMB);
            _suppressSizeSync = true;
            TxtInstallerSizeMB.Text = mb.ToString();
            SldInstallerSize.Value = MBToSliderPos(mb);
            UpdateInstallerSizeReadout(mb);
            _suppressSizeSync = false;
        }

        private void UpdateInstallerSizeReadout(int mb)
        {
            if (LblInstallerSizeReadout == null) return;
            LblInstallerSizeReadout.Text = mb >= 1024
                ? $"= {mb / 1024.0:0.##} GB"
                : (mb > 0 ? "" : "smallest possible");
        }

        private void InstallerSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSizeSync) return;
            var mb = SliderPosToMB(e.NewValue);
            _suppressSizeSync = true;
            TxtInstallerSizeMB.Text = mb.ToString();
            UpdateInstallerSizeReadout(mb);
            _suppressSizeSync = false;
        }

        private void InstallerSizeMB_Changed(object sender, TextChangedEventArgs e)
        {
            if (_suppressSizeSync) return;
            var mb = ParseInstallerSizeMB();
            _suppressSizeSync = true;
            SldInstallerSize.Value = MBToSliderPos(mb);
            UpdateInstallerSizeReadout(mb);
            _suppressSizeSync = false;
        }

        // ===== Live receipt rail + frame nav tiles =====

        private DispatcherTimer? _receiptTimer;

        private void StartReceiptTimer()
        {
            _receiptTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _receiptTimer.Tick += (_, _) => { try { UpdateReceipt(); } catch { } };
            _receiptTimer.Start();
        }

        private void UpdateReceipt()
        {
            var installerName = string.IsNullOrWhiteSpace(TxtInstallerExeName.Text)
                ? "YourSimulatedSetup.exe" : TxtInstallerExeName.Text.Trim();
            var displayName = string.IsNullOrWhiteSpace(TxtAppName.Text)
                ? "your installer" : TxtAppName.Text.Trim();
            var publisher = string.IsNullOrWhiteSpace(TxtAppPublisher.Text) ? "—" : TxtAppPublisher.Text.Trim();
            if (RunInstallerExeName != null) RunInstallerExeName.Text = installerName;
            if (RunPersonaAppName   != null) RunPersonaAppName.Text   = displayName;

            if (LblTileIdentity != null) LblTileIdentity.Text = $"{displayName} · {publisher}";
            if (LblTileWizard != null)
            {
                var pages = 0;
                if (ChkShowWelcome.IsChecked == true) pages++;
                if (ChkShowEULA.IsChecked == true) pages++;
                if (ChkShowInstallContext.IsChecked == true) pages++;
                if (ChkShowTargetDirectory.IsChecked == true) pages++;
                if (ChkShowComponents.IsChecked == true) pages++;
                if (ChkShowDesktopShortcut.IsChecked == true) pages++;
                if (ChkShowStartMenuPin.IsChecked == true) pages++;
                if (ChkShowRebootOption.IsChecked == true) pages++;
                if (ChkShowActiveSetup.IsChecked == true) pages++;
                LblTileWizard.Text = pages == 1 ? "1 page" : $"{pages} pages";
            }

            var behaviors = new List<string>();
            if (ChkTestFilesEnabled.IsChecked == true) behaviors.Add("Files");
            if (ChkRegistryEnabled.IsChecked == true)  behaviors.Add("Registry");
            if (ChkDesktopShortcut.IsChecked == true || ChkStartMenuEntry.IsChecked == true) behaviors.Add("Shortcuts");
            if (ChkFileAssociations.IsChecked == true) behaviors.Add("File associations");
            if (ChkContextMenu.IsChecked == true)      behaviors.Add("Context menu");
            if (ChkEnvVars.IsChecked == true)          behaviors.Add("Env vars");
            if (ChkService.IsChecked == true)          behaviors.Add("Windows service");
            if (ChkScheduledTask.IsChecked == true)    behaviors.Add("Scheduled task");
            if (ChkFirewall.IsChecked == true)         behaviors.Add("Firewall");
            if (ChkProtocolHandlers.IsChecked == true) behaviors.Add("Protocol handlers");
            if (ChkActiveSetup.IsChecked == true)      behaviors.Add("Active Setup");
            if (ChkAppPaths.IsChecked == true)         behaviors.Add("App Paths");
            if (ChkStartup.IsChecked == true)          behaviors.Add("Startup");
            if (LblReceiptBehaviors != null)
                LblReceiptBehaviors.Text = behaviors.Count == 0 ? "none" : string.Join("\n", behaviors);

            if (LblTileInstallActions != null)
                LblTileInstallActions.Text = behaviors.Count == 0 ? "none on" : $"{behaviors.Count} on";

            if (LblTileUninstall != null)
            {
                var leftovers = (ChkLeaveFiles.IsChecked == true) || (ChkLeaveRegistry.IsChecked == true);
                var clean = ChkCleanFiles.IsChecked == true && ChkCleanRegistry.IsChecked == true;
                LblTileUninstall.Text = leftovers ? "leftovers on"
                    : (clean ? "clean sweep" : "partial cleanup");
            }

            if (LblTileAppearance != null)
                LblTileAppearance.Text = string.IsNullOrWhiteSpace(TxtBannerColor.Text) ? "default" : TxtBannerColor.Text.Trim();

            if (LblTilePackage != null)
                LblTilePackage.Text = installerName;

            if (LblReceiptSize != null)
            {
                if (ChkInstallerSize.IsChecked == true)
                {
                    var mb = ParseInstallerSizeMB();
                    LblReceiptSize.Text = mb >= 1024 ? $"{mb} MB  ({mb / 1024.0:0.##} GB)" : $"{mb} MB";
                }
                else
                {
                    LblReceiptSize.Text = "smallest possible";
                }
            }

            if (LblReceiptElevation != null)
            {
                var ctx = CboDefaultContext.SelectedIndex == 1 ? "Per-user" : "Per-machine";
                var admin = ChkRequireAdmin.IsChecked == true ? "  ·  UAC" : "";
                LblReceiptElevation.Text = ctx + admin;
            }

            if (LblReceiptInstallDir != null)
                LblReceiptInstallDir.Text = TxtDefaultPath.Text;

            if (LblReceiptSummary != null)
                LblReceiptSummary.Text = $"Generate builds {installerName}. Running that installer registers {displayName} in Add/Remove Programs, performs the behaviors above, and drops the audit viewer.";

            UpdateCompositeSummaries();
        }

        private void FrameTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.Tag is not string key) return;
            ScrollToFrame(FrameByKey(key));
            UpdateSelectedTile(key);
        }

        private FrameworkElement? FrameByKey(string key) => key switch
        {
            "Identity"       => FrameIdentity,
            "Wizard"         => FrameWizard,
            "InstallActions" => FrameInstallActions,
            "Uninstall"      => FrameUninstall,
            "Appearance"     => FrameAppearance,
            "Package"        => FramePackage,
            _ => null
        };

        // Scroll the frames column so the target frame's top sits near the
        // top of the viewport. BringIntoView() only guarantees visibility,
        // which lands mid-frame if you were already scrolled down in the
        // previous frame — this puts the frame heading at the top.
        private void ScrollToFrame(FrameworkElement? target)
        {
            if (target == null || FramesScroll == null) return;
            var content = FramesScroll.Content as UIElement;
            if (content == null) return;
            try
            {
                var y = target.TranslatePoint(new System.Windows.Point(0, 0), content).Y;
                FramesScroll.ScrollToVerticalOffset(Math.Max(0, y - 12));
            }
            catch { /* pre-layout call; a later ScrollChanged will resync */ }
        }

        // As the user scrolls, the tile matching whichever frame is currently
        // at the top of the viewport lights up.
        private void FramesScroll_ScrollChanged(object sender, System.Windows.Controls.ScrollChangedEventArgs e)
            => UpdateSelectedTileFromScroll();

        private void UpdateSelectedTileFromScroll()
        {
            if (FramesScroll == null || FramesScroll.Content is not UIElement content) return;
            var scrollY = FramesScroll.VerticalOffset;
            var threshold = scrollY + 40;   // a tile "activates" once its top has scrolled past 40px into the viewport
            var frames = new (string key, FrameworkElement? el)[]
            {
                ("Identity",       FrameIdentity),
                ("Wizard",         FrameWizard),
                ("InstallActions", FrameInstallActions),
                ("Uninstall",      FrameUninstall),
                ("Appearance",     FrameAppearance),
                ("Package",        FramePackage),
            };
            string current = "Identity";
            foreach (var (key, el) in frames)
            {
                if (el == null) continue;
                try
                {
                    var y = el.TranslatePoint(new System.Windows.Point(0, 0), content).Y;
                    if (y <= threshold) current = key;
                }
                catch { }
            }
            UpdateSelectedTile(current);
        }

        private void UpdateSelectedTile(string current)
        {
            if (FrameTilesPanel == null) return;
            var accent    = (Brush)FindResource("AccentBrush");
            var accentTint = (Brush)FindResource("AccentTintBrush");
            var ink       = (Brush)FindResource("InkBrush");
            foreach (var child in FrameTilesPanel.Children)
            {
                if (child is Button btn && btn.Tag is string tag)
                {
                    var selected = tag == current;
                    btn.BorderBrush = selected ? accent : ink;
                    btn.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
                    btn.Background = selected ? accentTint : Brushes.White;
                }
            }
        }

        // ===== Code signing =====

        private void CodeSigningMode_Changed(object sender, SelectionChangedEventArgs e)
            => UpdateSigningPanelVisibility();

        private void UpdateSigningPanelVisibility()
        {
            if (PnlSigningPfxDetails == null) return;
            var mode = GetComboText(CboCodeSigningMode);
            PnlSigningPfxDetails.Visibility = mode.Equals("PFX", StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BrowsePfx_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PFX certificate (*.pfx)|*.pfx|All files (*.*)|*.*",
                Title  = "Select .pfx certificate"
            };
            if (!string.IsNullOrEmpty(TxtCodeSigningPfxPath.Text)
                && File.Exists(TxtCodeSigningPfxPath.Text))
                dialog.InitialDirectory = Path.GetDirectoryName(TxtCodeSigningPfxPath.Text);
            if (dialog.ShowDialog() == true)
                TxtCodeSigningPfxPath.Text = dialog.FileName;
        }

        // Attempts signtool.exe first (better output), falls back to PowerShell
        // Set-AuthenticodeSignature. Returns (success, message describing method
        // or the error output).
        private static (bool ok, string message) SignInstaller(string exePath, ConfigModel m)
        {
            if (string.IsNullOrWhiteSpace(m.CodeSigningPfxPath))
                return (false, "no PFX path set");
            if (!File.Exists(m.CodeSigningPfxPath))
                return (false, $"PFX not found: {m.CodeSigningPfxPath}");

            var signtool = FindSigntool();
            if (signtool != null)
            {
                var args = $"sign /fd SHA256 /td SHA256 /tr \"{m.CodeSigningTimestampUrl}\" " +
                           $"/f \"{m.CodeSigningPfxPath}\" /p \"{m.CodeSigningPfxPassword}\" " +
                           $"\"{exePath}\"";
                var (exit, so, se) = RunCapture(signtool, args, null);
                if (exit == 0) return (true, "signtool");
                var detail = string.IsNullOrWhiteSpace(se) ? so : se;
                return (false, $"signtool failed (exit {exit}):\n{detail.Trim()}");
            }

            // PowerShell fallback. Pass password via env var to avoid quoting
            // pitfalls, and 'exit $LASTEXITCODE'-style propagate signing errors.
            var script =
                "$ErrorActionPreference='Stop';" +
                "$pw = ConvertTo-SecureString -String $env:_TP_PFXPW -AsPlainText -Force;" +
                $"$cert = Get-PfxCertificate -FilePath '{m.CodeSigningPfxPath.Replace("'", "''")}' -Password $pw;" +
                $"$r = Set-AuthenticodeSignature -FilePath '{exePath.Replace("'", "''")}' -Certificate $cert " +
                $" -TimestampServer '{m.CodeSigningTimestampUrl.Replace("'", "''")}' -HashAlgorithm SHA256;" +
                "if ($r.Status -ne 'Valid') { Write-Error $r.StatusMessage; exit 1 }";
            var env = new Dictionary<string, string?> { ["_TP_PFXPW"] = m.CodeSigningPfxPassword };
            var (pxExit, pxOut, pxErr) = RunCapture("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                env);
            if (pxExit == 0) return (true, "PowerShell Set-AuthenticodeSignature");
            var pxDetail = string.IsNullOrWhiteSpace(pxErr) ? pxOut : pxErr;
            return (false, $"PowerShell signing failed (exit {pxExit}):\n{pxDetail.Trim()}");
        }

        private static string? FindSigntool()
        {
            // Try `where signtool` first (respects PATH)
            try
            {
                var (exit, so, _) = RunCapture("where.exe", "signtool.exe", null);
                if (exit == 0)
                {
                    var first = so.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(first) && File.Exists(first)) return first;
                }
            }
            catch { }

            // Fall back to common Windows Kits locations
            var kits = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(kits))
            {
                var binDir = Path.Combine(kits, "Windows Kits", "10", "bin");
                if (Directory.Exists(binDir))
                {
                    foreach (var arch in new[] { "x64", "x86" })
                    {
                        foreach (var version in Directory.EnumerateDirectories(binDir)
                                                        .Select(Path.GetFileName)
                                                        .OrderByDescending(n => n ?? ""))
                        {
                            if (version == null) continue;
                            var candidate = Path.Combine(binDir, version, arch, "signtool.exe");
                            if (File.Exists(candidate)) return candidate;
                        }
                    }
                }
            }
            return null;
        }

        private static (int exit, string stdOut, string stdErr) RunCapture(string file, string args,
            IDictionary<string, string?>? extraEnv)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            if (extraEnv != null)
                foreach (var kv in extraEnv) psi.EnvironmentVariables[kv.Key] = kv.Value;
            using var p = Process.Start(psi);
            if (p == null) return (-1, "", $"failed to start {file}");
            var so = p.StandardOutput.ReadToEnd();
            var se = p.StandardError.ReadToEnd();
            p.WaitForExit(120_000);
            return (p.ExitCode, so, se);
        }

        // ===== Presets =====

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string presetName)
                ApplyPreset(presetName);
        }

        private void ApplyPreset(string presetName)
        {
            string iniText;
            try { iniText = ReadEmbeddedPreset(presetName); }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load preset '{presetName}':\n{ex.Message}",
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var current = CollectToModel();
            var next = CloneModel(current);
            ResetInstallActions(next);
            ApplyPresetIniToModel(next, iniText, current);

            _model = next;
            PopulateFromModel(_model);
            UpdateReceipt();
        }

        private static string ReadEmbeddedPreset(string presetName)
        {
            var asm = Assembly.GetExecutingAssembly();
            var resource = $"TestPackage.Configurator.Presets.{presetName}.ini";
            using var stream = asm.GetManifestResourceStream(resource)
                ?? throw new FileNotFoundException($"Embedded preset not found: {resource}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static ConfigModel CloneModel(ConfigModel s)
        {
            var text = ConfigWriter.Write(s);
            var tmp = Path.Combine(Path.GetTempPath(), "TestPackage_Clone_" + Guid.NewGuid().ToString("N")[..8] + ".ini");
            try
            {
                File.WriteAllText(tmp, text);
                return ConfigModel.FromParser(ConfigParser.Load(tmp));
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }

        private static void ResetInstallActions(ConfigModel m)
        {
            m.TestFilesEnabled = false;                m.TestFiles = "";
            m.RegistryEnabled = false;                 m.RegistryEntries = "";
            m.CreateDesktopShortcut = false;
            m.CreateStartMenuEntry = false;
            m.PinToStartMenu = false;
            m.FileAssociationsEnabled = false;         m.FileAssociations = "";
            m.ContextMenuEnabled = false;              m.ContextMenuEntries = "";
            m.EnvironmentVariablesEnabled = false;     m.EnvironmentVariables = "";
            m.ServicesEnabled = false;
            m.ScheduledTasksEnabled = false;
            m.FirewallRulesEnabled = false;            m.FirewallRules = "";
            m.ProtocolHandlersEnabled = false;
            m.ActiveSetupEnabled = false;
            m.AppPathsEnabled = false;
            m.StartupEnabled = false;
            m.FontsEnabled = false;
            m.COMRegistrationEnabled = false;
        }

        private static void ApplyPresetIniToModel(ConfigModel m, string iniText, ConfigModel identity)
        {
            var sections = ParseIndicativeIni(iniText);
            string Sub(string s) => (s ?? "")
                .Replace("<Publisher>", identity.AppPublisher ?? "")
                .Replace("<AppName>", identity.AppName ?? "")
                .Replace("<AppExe>", identity.AppExeName ?? "")
                .Replace("<Version>", identity.AppVersion ?? "");

            bool GetEnabled(string sect)
            {
                if (!sections.TryGetValue(sect, out var s)) return false;
                if (!s.TryGetValue("Enabled", out var v)) return false;
                return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            List<string> Collect(string sect, string prefix)
            {
                var list = new List<string>();
                if (!sections.TryGetValue(sect, out var s)) return list;
                for (int i = 1; ; i++)
                {
                    if (!s.TryGetValue(prefix + i, out var v)) break;
                    list.Add(Sub(v));
                }
                return list;
            }

            m.TestFilesEnabled = GetEnabled("Files");
            var files = Collect("Files", "File");
            if (files.Count > 0) m.TestFiles = string.Join(",", files);

            m.RegistryEnabled = GetEnabled("Registry");
            var regs = Collect("Registry", "Entry");
            if (regs.Count > 0) m.RegistryEntries = string.Join(",", regs);

            if (GetEnabled("Shortcuts"))
            {
                var sc = sections["Shortcuts"];
                if (sc.TryGetValue("Desktop", out var d) && d == "1")     m.CreateDesktopShortcut = true;
                if (sc.TryGetValue("StartMenu", out var sm) && sm == "1") m.CreateStartMenuEntry = true;
                if (sc.TryGetValue("Pin", out var pin) && pin == "1")     m.PinToStartMenu = true;
                if (sc.TryGetValue("StartMenuFolder", out var smf) && !string.IsNullOrWhiteSpace(smf))
                    m.StartMenuFolder = Sub(smf);
            }

            m.FileAssociationsEnabled = GetEnabled("FileAssociations");
            var assocs = Collect("FileAssociations", "Assoc");
            if (assocs.Count > 0) m.FileAssociations = string.Join(",", assocs);

            m.ContextMenuEnabled = GetEnabled("ContextMenu");

            m.EnvironmentVariablesEnabled = GetEnabled("EnvVars");
            var vars = Collect("EnvVars", "Var");
            if (vars.Count > 0)
            {
                var expanded = vars.Select(v =>
                {
                    var parts = v.Split('|');
                    return parts.Length >= 3 ? v : "User|" + v;
                });
                m.EnvironmentVariables = string.Join(",", expanded);
            }

            m.ServicesEnabled = GetEnabled("Service");
            if (sections.TryGetValue("Service", out var svc))
            {
                if (svc.TryGetValue("ServiceName", out var sn)) m.ServiceName = Sub(sn);
                if (svc.TryGetValue("DisplayName", out var dn)) m.ServiceDisplayName = Sub(dn);
                if (svc.TryGetValue("StartType", out var st))   m.ServiceStartType = st;
            }

            m.ScheduledTasksEnabled = GetEnabled("ScheduledTask");
            if (sections.TryGetValue("ScheduledTask", out var tsk))
            {
                if (tsk.TryGetValue("TaskName", out var tn)) m.TaskName = Sub(tn);
                if (tsk.TryGetValue("Schedule", out var sc)) m.TaskSchedule = sc;
            }

            m.FirewallRulesEnabled = GetEnabled("Firewall");
            var rules = Collect("Firewall", "Rule");
            if (rules.Count > 0) m.FirewallRules = string.Join(",", rules);

            m.ProtocolHandlersEnabled = GetEnabled("Protocols");
            m.ActiveSetupEnabled      = GetEnabled("ActiveSetup");
            m.AppPathsEnabled         = GetEnabled("AppPaths");
            m.StartupEnabled          = GetEnabled("Startup");
            m.FontsEnabled            = GetEnabled("Fonts");
            m.COMRegistrationEnabled  = GetEnabled("COM");

            if (sections.TryGetValue("Install", out var inst)
                && inst.TryGetValue("DefaultContext", out var ctx))
            {
                m.DefaultContext = ctx.Equals("machine", StringComparison.OrdinalIgnoreCase)
                    ? "Machine" : "User";
            }
        }

        private static Dictionary<string, Dictionary<string, string>> ParseIndicativeIni(string text)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            string current = "";
            foreach (var raw in text.Split('\n'))
            {
                var line = raw.Trim().TrimEnd('\r');
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;

                while (line.StartsWith("["))
                {
                    var end = line.IndexOf(']');
                    if (end < 0) break;
                    current = line[1..end].Trim();
                    if (!result.ContainsKey(current))
                        result[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    line = line[(end + 1)..].Trim();
                    if (line.Length == 0) break;
                }
                if (line.Length == 0) continue;

                var eq = line.IndexOf('=');
                if (eq > 0 && !string.IsNullOrEmpty(current))
                {
                    var key = line[..eq].Trim();
                    var val = line[(eq + 1)..].Trim();
                    if (!result.ContainsKey(current))
                        result[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    result[current][key] = val;
                }
            }
            return result;
        }

        // ===================================================================
        //   Composite-field editor overlay
        //
        // Each composite field (registry entries, env vars, firewall rules,
        // etc.) is stored in the model as a pipe-separated string. In the UI
        // we deserialize it into a List<string[]> for the duration of a
        // Configurator session, edit through the overlay, and re-serialize
        // on Collect. The seven schemas below describe the field layout of
        // each composite.
        // ===================================================================

        private sealed class FieldDef
        {
            public string Label;
            public string Placeholder;
            public bool Mono;
            public string[]? Options;      // ComboBox when non-null
            public string Default;
            public FieldDef(string label, string placeholder, bool mono = false, string[]? options = null, string @default = "")
            {
                Label = label; Placeholder = placeholder; Mono = mono; Options = options; Default = @default;
            }
        }

        private sealed class CompositeSchema
        {
            public string Key;               // "RegistryEntries"
            public string Title;             // "Registry entries"
            public string Subject;           // "entry" (used in "+ Add entry")
            public string Description;       // one-line explainer under the overlay title
            public FieldDef[] Fields;
            public Func<ConfigModel, string> Read;
            public Action<ConfigModel, string> Write;
            public TextBlock? SummaryLabel;
            public CheckBox? EnabledCheckbox;
            public CompositeSchema(string key, string title, string subject, string description,
                FieldDef[] fields, Func<ConfigModel, string> read, Action<ConfigModel, string> write)
            {
                Key = key; Title = title; Subject = subject; Description = description;
                Fields = fields; Read = read; Write = write;
            }
        }

        private readonly Dictionary<string, CompositeSchema> _schemas = new();
        private readonly Dictionary<string, List<string[]>> _composites = new();
        private string? _openComposite;
        private int _selectedEntryIdx;

        private void InitCompositeSchemas()
        {
            _schemas["TestFiles"] = new CompositeSchema(
                "TestFiles", "Test files", "file",
                "Marker files the installer writes so packaging tools can verify file-system capture. Path supports %InstallDir%, %ProgramData%, %AppData% and friends.",
                new[] {
                    new FieldDef("PATH",    @"e.g. %InstallDir%\marker.txt", mono: true),
                    new FieldDef("CONTENT", "Optional marker text"),
                },
                m => m.TestFiles, (m, s) => m.TestFiles = s);

            _schemas["RegistryEntries"] = new CompositeSchema(
                "RegistryEntries", "Registry entries", "entry",
                "Values the installer writes to HKCU or HKLM. Type-strict; installers on non-admin contexts will silently skip HKLM entries.",
                new[] {
                    new FieldDef("KEY PATH",   @"HKCU\Software\Publisher\App", mono: true),
                    new FieldDef("VALUE NAME", "e.g. InstallDate"),
                    new FieldDef("TYPE",       "", options: new[] { "REG_SZ", "REG_DWORD", "REG_EXPAND_SZ", "REG_MULTI_SZ" }, @default: "REG_SZ"),
                    new FieldDef("DATA",       "e.g. %DATE% or 1"),
                },
                m => m.RegistryEntries, (m, s) => m.RegistryEntries = s);

            _schemas["FileAssociations"] = new CompositeSchema(
                "FileAssociations", "File associations", "association",
                "Extensions the installer registers as owned by this app. Icon may be a path with optional \",N\" index.",
                new[] {
                    new FieldDef("EXTENSION",   ".tpkg"),
                    new FieldDef("PROGID",      "MyApp.Document"),
                    new FieldDef("DESCRIPTION", "MyApp Document"),
                    new FieldDef("ICON PATH",   @"%InstallDir%\app.exe,0", mono: true),
                },
                m => m.FileAssociations, (m, s) => m.FileAssociations = s);

            _schemas["ContextMenuEntries"] = new CompositeSchema(
                "ContextMenuEntries", "Context menu entries", "entry",
                "Right-click actions registered under a file type or the Directory target. Command receives \"%1\".",
                new[] {
                    new FieldDef("TARGET",    "* or .ext or Directory"),
                    new FieldDef("MENU TEXT", "Open with MyApp"),
                    new FieldDef("COMMAND",   @"""%InstallDir%\app.exe"" ""%1""", mono: true),
                },
                m => m.ContextMenuEntries, (m, s) => m.ContextMenuEntries = s);

            _schemas["EnvironmentVariables"] = new CompositeSchema(
                "EnvironmentVariables", "Environment variables", "variable",
                "User or System environment variables the installer sets.",
                new[] {
                    new FieldDef("SCOPE", "", options: new[] { "User", "System" }, @default: "User"),
                    new FieldDef("NAME",  "MY_APP_HOME"),
                    new FieldDef("VALUE", "%InstallDir%"),
                },
                m => m.EnvironmentVariables, (m, s) => m.EnvironmentVariables = s);

            _schemas["FirewallRules"] = new CompositeSchema(
                "FirewallRules", "Firewall rules", "rule",
                "Windows Firewall rules the installer adds. Requires elevation at install-time.",
                new[] {
                    new FieldDef("RULE NAME", "My app inbound"),
                    new FieldDef("DIRECTION", "", options: new[] { "In", "Out" }, @default: "In"),
                    new FieldDef("ACTION",    "", options: new[] { "Allow", "Block" }, @default: "Allow"),
                    new FieldDef("PROTOCOL",  "", options: new[] { "TCP", "UDP" }, @default: "TCP"),
                    new FieldDef("PORT",      "19876"),
                },
                m => m.FirewallRules, (m, s) => m.FirewallRules = s);

            _schemas["ProtocolHandlers"] = new CompositeSchema(
                "ProtocolHandlers", "URI protocol handlers", "protocol",
                "Custom URI schemes (e.g. myapp://) the installer registers.",
                new[] {
                    new FieldDef("PROTOCOL",    "myapp"),
                    new FieldDef("DESCRIPTION", "MyApp Protocol Handler"),
                },
                m => m.ProtocolHandlers, (m, s) => m.ProtocolHandlers = s);

            // Wire the summary labels + enabled checkboxes now that InitializeComponent has run.
            _schemas["TestFiles"].SummaryLabel            = LblTestFilesSummary;
            _schemas["TestFiles"].EnabledCheckbox         = ChkTestFilesEnabled;
            _schemas["RegistryEntries"].SummaryLabel      = LblRegistrySummary;
            _schemas["RegistryEntries"].EnabledCheckbox   = ChkRegistryEnabled;
            _schemas["FileAssociations"].SummaryLabel     = LblFileAssociationsSummary;
            _schemas["FileAssociations"].EnabledCheckbox  = ChkFileAssociations;
            _schemas["ContextMenuEntries"].SummaryLabel   = LblContextMenuSummary;
            _schemas["ContextMenuEntries"].EnabledCheckbox = ChkContextMenu;
            _schemas["EnvironmentVariables"].SummaryLabel = LblEnvVarsSummary;
            _schemas["EnvironmentVariables"].EnabledCheckbox = ChkEnvVars;
            _schemas["FirewallRules"].SummaryLabel        = LblFirewallSummary;
            _schemas["FirewallRules"].EnabledCheckbox     = ChkFirewall;
            _schemas["ProtocolHandlers"].SummaryLabel     = LblProtocolHandlersSummary;
            _schemas["ProtocolHandlers"].EnabledCheckbox  = ChkProtocolHandlers;
        }

        private void LoadCompositesFromModel(ConfigModel m)
        {
            foreach (var (key, schema) in _schemas)
                _composites[key] = ParseComposite(schema.Read(m), schema.Fields.Length);
        }

        private void SaveCompositesToModel(ConfigModel m)
        {
            foreach (var (key, schema) in _schemas)
            {
                if (!_composites.TryGetValue(key, out var entries)) continue;
                schema.Write(m, SerializeComposite(entries));
            }
        }

        // Parse a pipe-separated composite string into entries of exactly
        // fieldsPerEntry fields. Handles values containing commas (icon
        // "app.exe,0") by rejoining at entry boundaries.
        private static List<string[]> ParseComposite(string data, int fieldsPerEntry)
        {
            var result = new List<string[]>();
            if (string.IsNullOrWhiteSpace(data)) return result;
            var allParts = data.Split('|').Select(s => s.Trim()).ToArray();
            var current = new List<string>();
            foreach (var part in allParts)
            {
                if (current.Count == fieldsPerEntry - 1)
                {
                    var commaIdx = part.LastIndexOf(',');
                    if (commaIdx > 0)
                    {
                        current.Add(part[..commaIdx].Trim());
                        result.Add(current.ToArray());
                        current = new List<string> { part[(commaIdx + 1)..].Trim() };
                    }
                    else
                    {
                        current.Add(part);
                        result.Add(current.ToArray());
                        current = new List<string>();
                    }
                }
                else
                {
                    current.Add(part);
                }
            }
            if (current.Count > 0) result.Add(current.ToArray());
            return result.Where(e => e.Any(f => !string.IsNullOrEmpty(f))).ToList();
        }

        private static string SerializeComposite(List<string[]> entries)
        {
            var items = new List<string>();
            foreach (var entry in entries)
            {
                var parts = entry.ToList();
                while (parts.Count > 0 && string.IsNullOrEmpty(parts[^1])) parts.RemoveAt(parts.Count - 1);
                if (parts.Any(p => !string.IsNullOrEmpty(p)))
                    items.Add(string.Join("|", parts));
            }
            return string.Join(",", items);
        }

        private void UpdateCompositeSummaries()
        {
            foreach (var schema in _schemas.Values)
            {
                if (schema.SummaryLabel == null) continue;
                var enabled = schema.EnabledCheckbox?.IsChecked == true;
                var count = _composites.TryGetValue(schema.Key, out var list) ? list.Count : 0;
                if (!enabled)
                {
                    schema.SummaryLabel.Text = count == 0 ? "off · no entries" : $"off · {count} entries";
                }
                else
                {
                    if (count == 0) schema.SummaryLabel.Text = "no entries yet";
                    else
                    {
                        // First-entry preview: the first field trimmed to something
                        // that scans in a single-line receipt.
                        var first = list![0].ElementAtOrDefault(0) ?? "";
                        var preview = first.Length > 48 ? first[..45] + "…" : first;
                        schema.SummaryLabel.Text = count == 1
                            ? preview
                            : $"{count} entries · {preview}";
                    }
                }
            }
        }

        // ----- Overlay open / close / add / remove -----

        private void OpenComposite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.Tag is not string key) return;
            if (!_schemas.TryGetValue(key, out var schema)) return;

            _openComposite = key;
            LblOverlayTitle.Text = schema.Title;
            LblOverlaySubtitle.Text = schema.Description;
            _selectedEntryIdx = _composites[key].Count > 0 ? 0 : -1;
            RebuildMasterList();
            BuildDetailForm();
            CompositeOverlay.Visibility = Visibility.Visible;
        }

        private void CompositeOverlay_Close(object sender, RoutedEventArgs e) => CloseOverlay();
        private void CompositeOverlay_ScrimClick(object sender, MouseButtonEventArgs e) => CloseOverlay();

        private void CloseOverlay()
        {
            _openComposite = null;
            CompositeOverlay.Visibility = Visibility.Collapsed;
            OverlayMasterList.Children.Clear();
            OverlayDetailForm.Children.Clear();
        }

        private void CompositeOverlay_Add(object sender, RoutedEventArgs e)
        {
            if (_openComposite is null) return;
            var schema = _schemas[_openComposite];
            var blank = schema.Fields.Select(f => f.Default).ToArray();
            _composites[_openComposite].Add(blank);
            _selectedEntryIdx = _composites[_openComposite].Count - 1;
            RebuildMasterList();
            BuildDetailForm();
            // Focus the first field
            if (OverlayDetailForm.Children.Count > 0
                && FindFirstEditable(OverlayDetailForm.Children[0]) is Control ctrl)
                ctrl.Focus();
        }

        private static UIElement? FindFirstEditable(UIElement element)
        {
            // UIElementCollection is non-generic (yields object); type the loop
            // variable so the return value is the required UIElement.
            if (element is StackPanel sp)
            {
                foreach (UIElement c in sp.Children)
                    if (c is TextBox || c is ComboBox) return c;
            }
            return null;
        }

        private void CompositeOverlay_Remove(object sender, RoutedEventArgs e)
        {
            if (_openComposite is null || _selectedEntryIdx < 0) return;
            var entries = _composites[_openComposite];
            if (_selectedEntryIdx >= entries.Count) return;
            entries.RemoveAt(_selectedEntryIdx);
            if (_selectedEntryIdx >= entries.Count) _selectedEntryIdx = entries.Count - 1;
            RebuildMasterList();
            BuildDetailForm();
        }

        private void RebuildMasterList()
        {
            OverlayMasterList.Children.Clear();
            if (_openComposite is null) return;
            var schema = _schemas[_openComposite];
            var entries = _composites[_openComposite];

            for (int i = 0; i < entries.Count; i++)
            {
                var idx = i;   // capture
                var entry = entries[i];
                var isSelected = i == _selectedEntryIdx;

                var btn = new Button();
                btn.SetResourceReference(StyleProperty, "MasterListRow");
                btn.BorderBrush = isSelected ? (Brush)FindResource("AccentBrush") : Brushes.Transparent;
                btn.Background = isSelected ? (Brush)FindResource("AccentTintBrush") : Brushes.Transparent;

                var stack = new StackPanel();

                var first = string.IsNullOrWhiteSpace(entry.ElementAtOrDefault(0))
                    ? "(new " + schema.Subject + ")"
                    : entry[0];
                stack.Children.Add(new TextBlock
                {
                    Text = first,
                    FontFamily = (FontFamily)FindResource("MonoFont"),
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("InkBrush"),
                    TextTrimming = TextTrimming.CharacterEllipsis
                });

                var rest = string.Join(" · ",
                    entry.Skip(1).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x));
                if (!string.IsNullOrEmpty(rest))
                {
                    stack.Children.Add(new TextBlock
                    {
                        Text = rest,
                        FontFamily = (FontFamily)FindResource("BodyFont"),
                        FontSize = 11,
                        Foreground = (Brush)FindResource("InkMutedBrush"),
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
                btn.Content = stack;
                btn.Click += (_, _) => { _selectedEntryIdx = idx; RebuildMasterList(); BuildDetailForm(); };
                OverlayMasterList.Children.Add(btn);
            }

            if (entries.Count == 0)
            {
                OverlayMasterList.Children.Add(new TextBlock
                {
                    Text = "No " + schema.Subject + " entries yet.\nUse + Add entry below.",
                    FontFamily = (FontFamily)FindResource("BodyFont"),
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkFaintBrush"),
                    Margin = new Thickness(16, 24, 16, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        private void BuildDetailForm()
        {
            OverlayDetailForm.Children.Clear();
            if (_openComposite is null)
            {
                BtnOverlayRemove.IsEnabled = false;
                return;
            }
            var schema = _schemas[_openComposite];
            var entries = _composites[_openComposite];
            if (_selectedEntryIdx < 0 || _selectedEntryIdx >= entries.Count)
            {
                BtnOverlayRemove.IsEnabled = false;
                OverlayDetailForm.Children.Add(new TextBlock
                {
                    Text = "Select an entry from the list to edit, or add a new one.",
                    FontFamily = (FontFamily)FindResource("BodyFont"),
                    FontSize = 12,
                    Foreground = (Brush)FindResource("InkFaintBrush"),
                    Margin = new Thickness(0, 12, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
                return;
            }

            BtnOverlayRemove.IsEnabled = true;
            var entry = entries[_selectedEntryIdx];

            for (int i = 0; i < schema.Fields.Length; i++)
            {
                var fieldDef = schema.Fields[i];
                var fieldIdx = i;

                OverlayDetailForm.Children.Add(new TextBlock
                {
                    Text = fieldDef.Label,
                    Style = (Style)FindResource("FieldLabel")
                });

                var value = entry.ElementAtOrDefault(i) ?? "";

                if (fieldDef.Options is { } opts)
                {
                    var cb = new ComboBox();
                    cb.SetResourceReference(StyleProperty, "ModernComboBox");
                    cb.Width = 220;
                    cb.HorizontalAlignment = HorizontalAlignment.Left;
                    foreach (var o in opts) cb.Items.Add(new ComboBoxItem { Content = o });
                    var initial = string.IsNullOrEmpty(value) ? fieldDef.Default : value;
                    foreach (ComboBoxItem it in cb.Items)
                        if (it.Content?.ToString() == initial) { it.IsSelected = true; break; }
                    cb.SelectionChanged += (_, _) =>
                    {
                        var arr = entries[_selectedEntryIdx];
                        EnsureLength(ref arr, schema.Fields.Length);
                        arr[fieldIdx] = (cb.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                        entries[_selectedEntryIdx] = arr;
                        RefreshSelectedMasterRow();
                    };
                    OverlayDetailForm.Children.Add(cb);
                }
                else
                {
                    var tb = new TextBox { Text = value };
                    tb.SetResourceReference(StyleProperty, fieldDef.Mono ? "MonoTextBox" : "FieldTextBox");
                    tb.LostFocus += (_, _) =>
                    {
                        var arr = entries[_selectedEntryIdx];
                        EnsureLength(ref arr, schema.Fields.Length);
                        arr[fieldIdx] = tb.Text.Trim();
                        entries[_selectedEntryIdx] = arr;
                        RefreshSelectedMasterRow();
                    };
                    tb.KeyDown += (_, ev) =>
                    {
                        if (ev.Key == Key.Enter)
                        {
                            var arr = entries[_selectedEntryIdx];
                            EnsureLength(ref arr, schema.Fields.Length);
                            arr[fieldIdx] = tb.Text.Trim();
                            entries[_selectedEntryIdx] = arr;
                            RefreshSelectedMasterRow();
                            ev.Handled = true;
                        }
                    };
                    OverlayDetailForm.Children.Add(tb);
                }

                if (!string.IsNullOrEmpty(fieldDef.Placeholder))
                {
                    OverlayDetailForm.Children.Add(new TextBlock
                    {
                        Text = fieldDef.Placeholder,
                        Style = (Style)FindResource("MutedText"),
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
            }
        }

        private static void EnsureLength(ref string[] arr, int len)
        {
            if (arr.Length >= len) return;
            var padded = new string[len];
            Array.Copy(arr, padded, arr.Length);
            for (int i = arr.Length; i < len; i++) padded[i] = "";
            arr = padded;
        }

        private void RefreshSelectedMasterRow()
        {
            // Cheap way to keep the master list in sync: rebuild it.
            RebuildMasterList();
        }
    }
}
