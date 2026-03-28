using System;
using System.Collections.Generic;
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

        // ===== Structured Row Builders =====

        private void AddTestFileRow(string path = "", string content = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var pathBox = MakeTextBox(path, "Path (e.g. %InstallDir%\\test.txt)");
            Grid.SetColumn(pathBox, 0);
            pathBox.Margin = new Thickness(0, 0, 4, 0);

            var contentBox = MakeTextBox(content, "Content (optional)");
            Grid.SetColumn(contentBox, 1);
            contentBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(TestFilesList, grid);
            Grid.SetColumn(removeBtn, 2);

            grid.Children.Add(pathBox);
            grid.Children.Add(contentBox);
            grid.Children.Add(removeBtn);
            TestFilesList.Children.Add(grid);
        }

        private void AddRegistryEntryRow(string keyPath = "", string valueName = "", string valueType = "REG_SZ", string valueData = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var keyBox = MakeTextBox(keyPath, "HKCU\\Software\\...");
            Grid.SetColumn(keyBox, 0);
            keyBox.Margin = new Thickness(0, 0, 4, 0);

            var nameBox = MakeTextBox(valueName, "Value Name");
            Grid.SetColumn(nameBox, 1);
            nameBox.Margin = new Thickness(0, 0, 4, 0);

            var typeCombo = new ComboBox { FontSize = 11, Margin = new Thickness(0, 0, 4, 0), VerticalContentAlignment = VerticalAlignment.Center };
            foreach (var t in new[] { "REG_SZ", "REG_DWORD", "REG_EXPAND_SZ", "REG_MULTI_SZ" })
                typeCombo.Items.Add(t);
            typeCombo.SelectedItem = valueType;
            Grid.SetColumn(typeCombo, 2);

            var dataBox = MakeTextBox(valueData, "Data");
            Grid.SetColumn(dataBox, 3);
            dataBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(RegistryEntriesList, grid);
            Grid.SetColumn(removeBtn, 4);

            grid.Children.Add(keyBox);
            grid.Children.Add(nameBox);
            grid.Children.Add(typeCombo);
            grid.Children.Add(dataBox);
            grid.Children.Add(removeBtn);
            RegistryEntriesList.Children.Add(grid);
        }

        private void AddFileAssociationRow(string ext = "", string progId = "", string desc = "", string icon = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var extBox = MakeTextBox(ext, ".ext");
            Grid.SetColumn(extBox, 0);
            extBox.Margin = new Thickness(0, 0, 4, 0);

            var progBox = MakeTextBox(progId, "ProgID");
            Grid.SetColumn(progBox, 1);
            progBox.Margin = new Thickness(0, 0, 4, 0);

            var descBox = MakeTextBox(desc, "Description");
            Grid.SetColumn(descBox, 2);
            descBox.Margin = new Thickness(0, 0, 4, 0);

            var iconBox = MakeTextBox(icon, "Icon path");
            Grid.SetColumn(iconBox, 3);
            iconBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(FileAssociationsList, grid);
            Grid.SetColumn(removeBtn, 4);

            grid.Children.Add(extBox);
            grid.Children.Add(progBox);
            grid.Children.Add(descBox);
            grid.Children.Add(iconBox);
            grid.Children.Add(removeBtn);
            FileAssociationsList.Children.Add(grid);
        }

        private void AddContextMenuRow(string ext = "", string menuText = "", string command = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var extBox = MakeTextBox(ext, "* or .ext");
            Grid.SetColumn(extBox, 0);
            extBox.Margin = new Thickness(0, 0, 4, 0);

            var textBox = MakeTextBox(menuText, "Menu Text");
            Grid.SetColumn(textBox, 1);
            textBox.Margin = new Thickness(0, 0, 4, 0);

            var cmdBox = MakeTextBox(command, "Command");
            Grid.SetColumn(cmdBox, 2);
            cmdBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(ContextMenuList, grid);
            Grid.SetColumn(removeBtn, 3);

            grid.Children.Add(extBox);
            grid.Children.Add(textBox);
            grid.Children.Add(cmdBox);
            grid.Children.Add(removeBtn);
            ContextMenuList.Children.Add(grid);
        }

        private void AddEnvVarRow(string scope = "User", string name = "", string value = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var scopeCombo = new ComboBox { FontSize = 11, Margin = new Thickness(0, 0, 4, 0), VerticalContentAlignment = VerticalAlignment.Center };
            scopeCombo.Items.Add("User");
            scopeCombo.Items.Add("System");
            scopeCombo.SelectedItem = scope;
            Grid.SetColumn(scopeCombo, 0);

            var nameBox = MakeTextBox(name, "Variable Name");
            Grid.SetColumn(nameBox, 1);
            nameBox.Margin = new Thickness(0, 0, 4, 0);

            var valBox = MakeTextBox(value, "Value");
            Grid.SetColumn(valBox, 2);
            valBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(EnvVarsList, grid);
            Grid.SetColumn(removeBtn, 3);

            grid.Children.Add(scopeCombo);
            grid.Children.Add(nameBox);
            grid.Children.Add(valBox);
            grid.Children.Add(removeBtn);
            EnvVarsList.Children.Add(grid);
        }

        private void AddFirewallRuleRow(string name = "", string dir = "In", string action = "Allow", string protocol = "TCP", string port = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(55) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var nameBox = MakeTextBox(name, "Rule Name");
            Grid.SetColumn(nameBox, 0); nameBox.Margin = new Thickness(0, 0, 4, 0);

            var dirCombo = new ComboBox { FontSize = 11, Margin = new Thickness(0, 0, 4, 0) };
            dirCombo.Items.Add("In"); dirCombo.Items.Add("Out"); dirCombo.SelectedItem = dir;
            Grid.SetColumn(dirCombo, 1);

            var actCombo = new ComboBox { FontSize = 11, Margin = new Thickness(0, 0, 4, 0) };
            actCombo.Items.Add("Allow"); actCombo.Items.Add("Block"); actCombo.SelectedItem = action;
            Grid.SetColumn(actCombo, 2);

            var protoCombo = new ComboBox { FontSize = 11, Margin = new Thickness(0, 0, 4, 0) };
            protoCombo.Items.Add("TCP"); protoCombo.Items.Add("UDP"); protoCombo.SelectedItem = protocol;
            Grid.SetColumn(protoCombo, 3);

            var portBox = MakeTextBox(port, "Port");
            Grid.SetColumn(portBox, 4); portBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(FirewallRulesList, grid);
            Grid.SetColumn(removeBtn, 5);

            grid.Children.Add(nameBox); grid.Children.Add(dirCombo); grid.Children.Add(actCombo);
            grid.Children.Add(protoCombo); grid.Children.Add(portBox); grid.Children.Add(removeBtn);
            FirewallRulesList.Children.Add(grid);
        }

        private void AddProtocolHandlerRow(string protocol = "", string desc = "")
        {
            var grid = new Grid { Margin = new Thickness(0, 2, 0, 2) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var protoBox = MakeTextBox(protocol, "Protocol");
            Grid.SetColumn(protoBox, 0); protoBox.Margin = new Thickness(0, 0, 4, 0);

            var descBox = MakeTextBox(desc, "Description");
            Grid.SetColumn(descBox, 1); descBox.Margin = new Thickness(0, 0, 4, 0);

            var removeBtn = MakeRemoveButton(ProtocolHandlersList, grid);
            Grid.SetColumn(removeBtn, 2);

            grid.Children.Add(protoBox); grid.Children.Add(descBox); grid.Children.Add(removeBtn);
            ProtocolHandlersList.Children.Add(grid);
        }

        private static TextBox MakeTextBox(string text, string placeholder)
        {
            var tb = new TextBox { Text = text, FontSize = 11, Padding = new Thickness(4, 3, 4, 3),
                                   BorderBrush = new SolidColorBrush(Color.FromRgb(0xD0, 0xD0, 0xD0)), BorderThickness = new Thickness(1) };
            if (string.IsNullOrEmpty(text))
            {
                tb.Foreground = Brushes.Gray;
                tb.Text = placeholder;
                tb.Tag = placeholder;
                tb.GotFocus += (s, _) => { if (tb.Text == (string)tb.Tag) { tb.Text = ""; tb.Foreground = Brushes.Black; } };
                tb.LostFocus += (s, _) => { if (string.IsNullOrEmpty(tb.Text)) { tb.Text = (string)tb.Tag; tb.Foreground = Brushes.Gray; } };
            }
            return tb;
        }

        private static string GetTextBoxValue(TextBox tb)
        {
            if (tb.Tag != null && tb.Text == (string)tb.Tag) return "";
            return tb.Text.Trim();
        }

        private static Button MakeRemoveButton(StackPanel parent, UIElement row)
        {
            var btn = new Button();
            btn.SetResourceReference(StyleProperty, "RemoveButton");
            btn.Click += (_, _) => parent.Children.Remove(row);
            return btn;
        }

        // ===== Add Button Handlers =====

        private void AddTestFile_Click(object sender, RoutedEventArgs e) => AddTestFileRow();
        private void AddRegistryEntry_Click(object sender, RoutedEventArgs e) => AddRegistryEntryRow();
        private void AddFileAssociation_Click(object sender, RoutedEventArgs e) => AddFileAssociationRow();
        private void AddContextMenu_Click(object sender, RoutedEventArgs e) => AddContextMenuRow();
        private void AddEnvVar_Click(object sender, RoutedEventArgs e) => AddEnvVarRow();
        private void AddFirewallRule_Click(object sender, RoutedEventArgs e) => AddFirewallRuleRow();
        private void AddProtocolHandler_Click(object sender, RoutedEventArgs e) => AddProtocolHandlerRow();

        // ===== Serialization Helpers =====

        private string CollectPipeSeparatedList(StackPanel list, string separator = ",")
        {
            var items = new List<string>();
            foreach (var child in list.Children)
            {
                if (child is Grid grid)
                {
                    var parts = new List<string>();
                    foreach (var cell in grid.Children)
                    {
                        if (cell is TextBox tb) parts.Add(GetTextBoxValue(tb));
                        else if (cell is ComboBox cb) parts.Add(cb.SelectedItem?.ToString() ?? "");
                    }
                    // Remove trailing empty parts
                    while (parts.Count > 0 && string.IsNullOrEmpty(parts[^1])) parts.RemoveAt(parts.Count - 1);
                    if (parts.Any(p => !string.IsNullOrEmpty(p)))
                        items.Add(string.Join("|", parts));
                }
            }
            return string.Join(separator, items);
        }

        private void PopulatePipeSeparatedList(StackPanel list, string data, Action<string[]> addRow)
        {
            list.Children.Clear();
            if (string.IsNullOrWhiteSpace(data)) return;
            foreach (var item in data.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)))
            {
                var parts = item.Split('|');
                addRow(parts);
            }
        }

        // ===== Populate / Collect =====

        private void PopulateFromModel(ConfigModel m)
        {
            TxtInstallerExeName.Text = m.InstallerExeName;
            TxtAppExeName.Text = m.AppExeName;

            TxtAppName.Text = m.AppName;
            TxtAppVersion.Text = m.AppVersion;
            TxtAppPublisher.Text = m.AppPublisher;
            TxtAppURL.Text = m.AppURL;
            TxtAppGUID.Text = m.AppGUID;
            ChkRequireAdmin.IsChecked = m.RequireAdmin;
            CboDefaultContext.SelectedIndex = m.DefaultContext.Equals("User", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            TxtDefaultPath.Text = m.DefaultPath;
            ChkAllowCustomPath.IsChecked = m.AllowCustomPath;

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

            ChkTestFilesEnabled.IsChecked = m.TestFilesEnabled;
            PopulatePipeSeparatedList(TestFilesList, m.TestFiles, p => AddTestFileRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? ""));

            ChkRegistryEnabled.IsChecked = m.RegistryEnabled;
            PopulatePipeSeparatedList(RegistryEntriesList, m.RegistryEntries, p => AddRegistryEntryRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? "REG_SZ", p.ElementAtOrDefault(3) ?? ""));

            ChkDesktopShortcut.IsChecked = m.CreateDesktopShortcut;
            TxtDesktopShortcutName.Text = m.DesktopShortcutName;
            ChkStartMenuEntry.IsChecked = m.CreateStartMenuEntry;
            TxtStartMenuFolder.Text = m.StartMenuFolder;
            ChkPinToStartMenu.IsChecked = m.PinToStartMenu;

            ChkFileAssociations.IsChecked = m.FileAssociationsEnabled;
            PopulatePipeSeparatedList(FileAssociationsList, m.FileAssociations, p => AddFileAssociationRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? "", p.ElementAtOrDefault(3) ?? ""));

            ChkContextMenu.IsChecked = m.ContextMenuEnabled;
            PopulatePipeSeparatedList(ContextMenuList, m.ContextMenuEntries, p => AddContextMenuRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? ""));

            ChkEnvVars.IsChecked = m.EnvironmentVariablesEnabled;
            PopulatePipeSeparatedList(EnvVarsList, m.EnvironmentVariables, p => AddEnvVarRow(p.ElementAtOrDefault(0) ?? "User", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? ""));

            ChkService.IsChecked = m.ServicesEnabled;
            TxtServiceName.Text = m.ServiceName;
            TxtServiceDisplayName.Text = m.ServiceDisplayName;
            SelectComboItem(CboServiceStartType, m.ServiceStartType);
            ChkScheduledTask.IsChecked = m.ScheduledTasksEnabled;
            TxtTaskName.Text = m.TaskName;
            SelectComboItem(CboTaskSchedule, m.TaskSchedule);

            ChkFirewall.IsChecked = m.FirewallRulesEnabled;
            PopulatePipeSeparatedList(FirewallRulesList, m.FirewallRules, p => AddFirewallRuleRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "In", p.ElementAtOrDefault(2) ?? "Allow", p.ElementAtOrDefault(3) ?? "TCP", p.ElementAtOrDefault(4) ?? ""));

            ChkProtocolHandlers.IsChecked = m.ProtocolHandlersEnabled;
            PopulatePipeSeparatedList(ProtocolHandlersList, m.ProtocolHandlers, p => AddProtocolHandlerRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? ""));

            ChkActiveSetup.IsChecked = m.ActiveSetupEnabled;
            ChkAppPaths.IsChecked = m.AppPathsEnabled;
            ChkStartup.IsChecked = m.StartupEnabled;
            SelectComboItem(CboStartupMethod, m.StartupMethod);

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
                    m.Components.Add(new ComponentEntry(tb.Text, cb.IsChecked == true));
            }

            m.TestFilesEnabled = ChkTestFilesEnabled.IsChecked == true;
            m.TestFiles = CollectPipeSeparatedList(TestFilesList);
            m.RegistryEnabled = ChkRegistryEnabled.IsChecked == true;
            m.RegistryEntries = CollectPipeSeparatedList(RegistryEntriesList);
            m.CreateDesktopShortcut = ChkDefaultDesktopShortcut.IsChecked == true;
            m.DesktopShortcutName = TxtDesktopShortcutName.Text.Trim();
            m.CreateStartMenuEntry = ChkStartMenuEntry.IsChecked == true;
            m.StartMenuFolder = TxtStartMenuFolder.Text.Trim();
            m.PinToStartMenu = ChkDefaultStartMenuPin.IsChecked == true;
            m.FileAssociationsEnabled = ChkFileAssociations.IsChecked == true;
            m.FileAssociations = CollectPipeSeparatedList(FileAssociationsList);
            m.ContextMenuEnabled = ChkContextMenu.IsChecked == true;
            m.ContextMenuEntries = CollectPipeSeparatedList(ContextMenuList);
            m.EnvironmentVariablesEnabled = ChkEnvVars.IsChecked == true;
            m.EnvironmentVariables = CollectPipeSeparatedList(EnvVarsList);

            m.ServicesEnabled = ChkService.IsChecked == true;
            m.ServiceName = TxtServiceName.Text.Trim();
            m.ServiceDisplayName = TxtServiceDisplayName.Text.Trim();
            m.ServiceStartType = GetComboText(CboServiceStartType);
            m.ScheduledTasksEnabled = ChkScheduledTask.IsChecked == true;
            m.TaskName = TxtTaskName.Text.Trim();
            m.TaskSchedule = GetComboText(CboTaskSchedule);
            m.FirewallRulesEnabled = ChkFirewall.IsChecked == true;
            m.FirewallRules = CollectPipeSeparatedList(FirewallRulesList);
            m.ProtocolHandlersEnabled = ChkProtocolHandlers.IsChecked == true;
            m.ProtocolHandlers = CollectPipeSeparatedList(ProtocolHandlersList);
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
            m.AppPathsExeName = m.AppExeName;
            return m;
        }

        // ===== Preview =====

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            var model = CollectToModel();

            // Find the installer template
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = Path.Combine(exeDir, "templates");
            if (!Directory.Exists(templatesDir)) templatesDir = exeDir;

            var installerTemplate = Path.Combine(templatesDir, "TestPackageInstaller.exe");
            if (!File.Exists(installerTemplate))
            {
                MessageBox.Show("Template not found. Cannot launch preview.\n\nEnsure the templates directory exists alongside the Configurator.",
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generate to a temp folder
                var tempDir = Path.Combine(Path.GetTempPath(), "TestPackage_Preview_" + Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(tempDir);

                File.Copy(installerTemplate, Path.Combine(tempDir, "TestPackageInstaller.exe"), true);
                File.WriteAllText(Path.Combine(tempDir, "config.ini"), ConfigWriter.Write(model));

                // Launch with --preview flag
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
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== Generate / Load / Save =====

        private void GenerateInstaller_Click(object sender, RoutedEventArgs e)
        {
            var model = CollectToModel();
            var outputFolder = TxtOutputFolder.Text.Trim();
            if (string.IsNullOrEmpty(outputFolder))
            {
                MessageBox.Show("Please select an output folder.", "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var templatesDir = Path.Combine(exeDir, "templates");
            if (!Directory.Exists(templatesDir)) templatesDir = exeDir;

            var installerTemplate = Path.Combine(templatesDir, "TestPackageInstaller.exe");
            var appTemplate = Path.Combine(templatesDir, "TestPackageApp.exe");

            if (!File.Exists(installerTemplate))
            {
                MessageBox.Show($"Template not found:\n{installerTemplate}", "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Directory.CreateDirectory(outputFolder);
                File.Copy(installerTemplate, Path.Combine(outputFolder, model.InstallerExeName), true);
                if (File.Exists(appTemplate))
                    File.Copy(appTemplate, Path.Combine(outputFolder, model.AppExeName), true);
                File.WriteAllText(Path.Combine(outputFolder, "config.ini"), ConfigWriter.Write(model));

                MessageBox.Show($"Installer generated!\n\n{outputFolder}\n\n  {model.InstallerExeName}\n  {model.AppExeName}\n  config.ini",
                    "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo { FileName = outputFolder, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*", Title = "Load Configuration" };
            if (dialog.ShowDialog() == true)
            {
                try { _model = ConfigModel.FromParser(ConfigParser.Load(dialog.FileName)); PopulateFromModel(_model); }
                catch (Exception ex) { MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "INI files (*.ini)|*.ini", FileName = "config.ini", Title = "Save Configuration" };
            if (dialog.ShowDialog() == true)
            {
                try { File.WriteAllText(dialog.FileName, ConfigWriter.Write(CollectToModel())); }
                catch (Exception ex) { MessageBox.Show($"Failed:\n{ex.Message}", "TestPackage Configurator", MessageBoxButton.OK, MessageBoxImage.Error); }
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
            panel.Children.Add(new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Width = 200 });
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
    }
}
