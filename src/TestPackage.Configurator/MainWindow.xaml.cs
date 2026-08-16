using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
            PopulateFromModel(_model);
            StartReceiptTimer();
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

        private TextBox MakeTextBox(string text, string placeholder)
        {
            var tb = new TextBox { Text = text };
            tb.SetResourceReference(StyleProperty, "FieldTextBox");
            tb.FontSize = 12;
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

        private void PopulatePipeSeparatedList(StackPanel list, string data, int fieldsPerEntry, Action<string[]> addRow)
        {
            list.Children.Clear();
            if (string.IsNullOrWhiteSpace(data)) return;

            // Split all fields by pipe, then group into entries of the expected size.
            // This handles cases where field values contain commas (e.g. icon indices "app.exe,0")
            // by first splitting the whole string on pipes and regrouping.
            var allParts = data.Split('|').Select(s => s.Trim()).ToArray();

            // Each entry has fieldsPerEntry pipe-separated fields.
            // The comma separating entries appears at the boundary between the last field
            // of one entry and the first field of the next.
            // Reassemble then split properly: find commas that are between entries.
            var entries = new List<string[]>();
            var currentFields = new List<string>();

            foreach (var part in allParts)
            {
                if (currentFields.Count == fieldsPerEntry - 1)
                {
                    // This is the last field of the current entry.
                    // It may contain a comma followed by the first field of the next entry.
                    var commaIdx = part.LastIndexOf(',');
                    if (commaIdx > 0 && currentFields.Count == fieldsPerEntry - 1)
                    {
                        currentFields.Add(part[..commaIdx].Trim());
                        entries.Add(currentFields.ToArray());
                        currentFields = new List<string> { part[(commaIdx + 1)..].Trim() };
                    }
                    else
                    {
                        currentFields.Add(part);
                        entries.Add(currentFields.ToArray());
                        currentFields = new List<string>();
                    }
                }
                else
                {
                    currentFields.Add(part);
                }
            }
            if (currentFields.Count > 0)
                entries.Add(currentFields.ToArray());

            foreach (var entry in entries)
            {
                if (entry.Any(f => !string.IsNullOrEmpty(f)))
                    addRow(entry);
            }
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

            ChkTestFilesEnabled.IsChecked = m.TestFilesEnabled;
            PopulatePipeSeparatedList(TestFilesList, m.TestFiles, 2, p => AddTestFileRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? ""));

            ChkRegistryEnabled.IsChecked = m.RegistryEnabled;
            PopulatePipeSeparatedList(RegistryEntriesList, m.RegistryEntries, 4, p => AddRegistryEntryRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? "REG_SZ", p.ElementAtOrDefault(3) ?? ""));

            ChkDesktopShortcut.IsChecked = m.CreateDesktopShortcut;
            TxtDesktopShortcutName.Text = m.DesktopShortcutName;
            ChkStartMenuEntry.IsChecked = m.CreateStartMenuEntry;
            _suppressPathSync = true;
            TxtStartMenuFolder.Text = m.StartMenuFolder;
            _userEditedStartMenuFolder = !string.Equals(m.StartMenuFolder, DeriveStartMenu(m.AppPublisher, m.AppName), StringComparison.OrdinalIgnoreCase);
            _suppressPathSync = false;
            ChkPinToStartMenu.IsChecked = m.PinToStartMenu;

            ChkFileAssociations.IsChecked = m.FileAssociationsEnabled;
            PopulatePipeSeparatedList(FileAssociationsList, m.FileAssociations, 4, p => AddFileAssociationRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? "", p.ElementAtOrDefault(3) ?? ""));

            ChkContextMenu.IsChecked = m.ContextMenuEnabled;
            PopulatePipeSeparatedList(ContextMenuList, m.ContextMenuEntries, 3, p => AddContextMenuRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? ""));

            ChkEnvVars.IsChecked = m.EnvironmentVariablesEnabled;
            PopulatePipeSeparatedList(EnvVarsList, m.EnvironmentVariables, 3, p => AddEnvVarRow(p.ElementAtOrDefault(0) ?? "User", p.ElementAtOrDefault(1) ?? "", p.ElementAtOrDefault(2) ?? ""));

            ChkService.IsChecked = m.ServicesEnabled;
            TxtServiceName.Text = m.ServiceName;
            TxtServiceDisplayName.Text = m.ServiceDisplayName;
            SelectComboItem(CboServiceStartType, m.ServiceStartType);
            ChkScheduledTask.IsChecked = m.ScheduledTasksEnabled;
            TxtTaskName.Text = m.TaskName;
            SelectComboItem(CboTaskSchedule, m.TaskSchedule);

            ChkFirewall.IsChecked = m.FirewallRulesEnabled;
            PopulatePipeSeparatedList(FirewallRulesList, m.FirewallRules, 5, p => AddFirewallRuleRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? "In", p.ElementAtOrDefault(2) ?? "Allow", p.ElementAtOrDefault(3) ?? "TCP", p.ElementAtOrDefault(4) ?? ""));

            ChkProtocolHandlers.IsChecked = m.ProtocolHandlersEnabled;
            PopulatePipeSeparatedList(ProtocolHandlersList, m.ProtocolHandlers, 2, p => AddProtocolHandlerRow(p.ElementAtOrDefault(0) ?? "", p.ElementAtOrDefault(1) ?? ""));

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
            m.InstallerSizeEnabled = ChkInstallerSize.IsChecked == true;
            m.InstallerSizeMB = ParseInstallerSizeMB();

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
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generate to a temp folder
                var tempDir = Path.Combine(Path.GetTempPath(), "TestPackage_Preview_" + Guid.NewGuid().ToString("N")[..8]);
                var tempDataDir = Path.Combine(tempDir, "_data");
                Directory.CreateDirectory(tempDataDir);

                File.Copy(installerTemplate, Path.Combine(tempDir, "TestPackageInstaller.exe"), true);
                File.WriteAllText(Path.Combine(tempDataDir, "config.ini"), ConfigWriter.Write(model));

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
                // Create a subfolder using the installer name (without extension)
                var subfolderName = Path.GetFileNameWithoutExtension(model.InstallerExeName);
                var packageFolder = Path.Combine(outputFolder, subfolderName);
                var dataFolder = Path.Combine(packageFolder, "_data");
                Directory.CreateDirectory(dataFolder);

                // Installer EXE goes in the root of the package folder
                var installerExePath = Path.Combine(packageFolder, model.InstallerExeName);
                File.Copy(installerTemplate, installerExePath, true);

                // Pad the generated setup EXE to the requested size (appends trailing
                // bytes after the PE image, which the loader ignores).
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

                // Companion files go in _data (installer reads from here at runtime)
                if (File.Exists(appTemplate))
                    File.Copy(appTemplate, Path.Combine(dataFolder, model.AppExeName), true);
                File.WriteAllText(Path.Combine(dataFolder, "config.ini"), ConfigWriter.Write(model));

                MessageBox.Show(
                    $"Installer generated!\n\n{packageFolder}\\{model.InstallerExeName}{sizeNote}\n\n" +
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
            // Any user-typed change opts out of auto-derivation. Clearing the field
            // re-enables it so they can let it follow publisher/app name again.
            _userEditedDefaultPath = TxtDefaultPath.Text.Trim().Length > 0;
        }

        private void StartMenuFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressPathSync) return;
            _userEditedStartMenuFolder = TxtStartMenuFolder.Text.Trim().Length > 0;
        }

        // ===== Installer Size (non-linear slider <-> exact MB textbox) =====

        private bool _suppressSizeSync;

        // Slider position 0..100 maps to MB via a cubic curve, so small drags on
        // the left give fine megabyte control and the right end reaches 100 GB.
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

        // ===== Live receipt rail =====
        //
        // A 400 ms DispatcherTimer recomputes the receipt from the current form
        // state. Cheap, keeps things reactive without wiring TextChanged/Checked
        // handlers to every control on the page. The proper reactive plumbing
        // arrives with the composite-editor overlay in alpha.2.

        private DispatcherTimer? _receiptTimer;

        private void StartReceiptTimer()
        {
            _receiptTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _receiptTimer.Tick += (_, _) =>
            {
                try { UpdateReceipt(); } catch { /* ignore; timer keeps ticking */ }
            };
            _receiptTimer.Start();
        }

        private void UpdateReceipt()
        {
            // Identity strip runs — always keep the persona sentence current.
            var installerName = string.IsNullOrWhiteSpace(TxtInstallerExeName.Text)
                ? "YourSimulatedSetup.exe" : TxtInstallerExeName.Text.Trim();
            var appName = string.IsNullOrWhiteSpace(TxtAppName.Text)
                ? "your app" : TxtAppName.Text.Trim();
            if (RunInstallerExeName != null) RunInstallerExeName.Text = installerName;
            if (RunPersonaAppName != null)   RunPersonaAppName.Text   = appName;

            // Behaviors enabled — one line each so the rail stays scannable.
            var behaviors = new List<string>();
            if (ChkTestFilesEnabled.IsChecked == true) behaviors.Add("Files");
            if (ChkRegistryEnabled.IsChecked == true)  behaviors.Add("Registry");
            if (ChkDesktopShortcut.IsChecked == true || ChkStartMenuEntry.IsChecked == true)
                behaviors.Add("Shortcuts");
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

            // Installer size
            if (LblReceiptSize != null)
            {
                if (ChkInstallerSize.IsChecked == true)
                {
                    var mb = ParseInstallerSizeMB();
                    LblReceiptSize.Text = mb >= 1024
                        ? $"{mb} MB  ({mb / 1024.0:0.##} GB)"
                        : $"{mb} MB";
                }
                else
                {
                    LblReceiptSize.Text = "smallest possible";
                }
            }

            // Elevation
            if (LblReceiptElevation != null)
            {
                var ctx = CboDefaultContext.SelectedIndex == 1 ? "Per-user" : "Per-machine";
                var admin = ChkRequireAdmin.IsChecked == true ? "  ·  UAC" : "";
                LblReceiptElevation.Text = ctx + admin;
            }

            // Install directory
            if (LblReceiptInstallDir != null)
                LblReceiptInstallDir.Text = TxtDefaultPath.Text;

            // Bottom-of-rail summary
            if (LblReceiptSummary != null)
                LblReceiptSummary.Text = $"Generate produces {installerName}. Run it and it installs {appName}, performs the behaviors above, and drops the audit viewer.";
        }

        // ===== Presets =====
        //
        // Presets replace the install-action behaviors and their sample data.
        // Identity, wizard pages, and uninstall settings are preserved (per
        // docs/ux/README.md). Placeholders <Publisher>/<AppName>/<AppExe>/
        // <Version> resolve against the current Identity at apply time.

        private void ApplyPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string presetName)
                ApplyPreset(presetName);
        }

        private void ApplyPreset(string presetName)
        {
            string iniText;
            try
            {
                iniText = ReadEmbeddedPreset(presetName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not load preset '{presetName}':\n{ex.Message}",
                    "TestPackage", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Snapshot everything currently on-screen so we can carry identity /
            // wizard / uninstall / cosmetics across untouched.
            var current = CollectToModel();

            // Start from the current model; reset only the install-action fields.
            var next = CloneModel(current);
            ResetInstallActions(next);

            // Layer the preset's install-action data on top.
            ApplyPresetIniToModel(next, iniText, current);

            _model = next;
            PopulateFromModel(_model);
            UpdateReceipt();
        }

        private static string ReadEmbeddedPreset(string presetName)
        {
            var asm = Assembly.GetExecutingAssembly();
            // Manifest resource name is "<default-namespace>.Presets.<name>.ini"
            var resource = $"TestPackage.Configurator.Presets.{presetName}.ini";
            using var stream = asm.GetManifestResourceStream(resource)
                ?? throw new FileNotFoundException($"Embedded preset not found: {resource}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static ConfigModel CloneModel(ConfigModel s)
        {
            // Round-trip via the config writer/parser — this is the same
            // serialization the app already uses and it guarantees fidelity.
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

            // Files
            m.TestFilesEnabled = GetEnabled("Files");
            var files = Collect("Files", "File");
            if (files.Count > 0) m.TestFiles = string.Join(",", files);

            // Registry
            m.RegistryEnabled = GetEnabled("Registry");
            var regs = Collect("Registry", "Entry");
            if (regs.Count > 0) m.RegistryEntries = string.Join(",", regs);

            // Shortcuts (multi-key section)
            if (GetEnabled("Shortcuts"))
            {
                var sc = sections["Shortcuts"];
                if (sc.TryGetValue("Desktop", out var d) && d == "1")     m.CreateDesktopShortcut = true;
                if (sc.TryGetValue("StartMenu", out var sm) && sm == "1") m.CreateStartMenuEntry = true;
                if (sc.TryGetValue("Pin", out var pin) && pin == "1")     m.PinToStartMenu = true;
                if (sc.TryGetValue("StartMenuFolder", out var smf) && !string.IsNullOrWhiteSpace(smf))
                    m.StartMenuFolder = Sub(smf);
            }

            // File associations
            m.FileAssociationsEnabled = GetEnabled("FileAssociations");
            var assocs = Collect("FileAssociations", "Assoc");
            if (assocs.Count > 0) m.FileAssociations = string.Join(",", assocs);

            // Context menu
            m.ContextMenuEnabled = GetEnabled("ContextMenu");

            // Env vars — presets carry "NAME|VALUE" (2-field), real format is
            // "Scope|Name|Value" (3-field). Default scope to User when absent.
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

            // Service
            m.ServicesEnabled = GetEnabled("Service");
            if (sections.TryGetValue("Service", out var svc))
            {
                if (svc.TryGetValue("ServiceName", out var sn)) m.ServiceName = Sub(sn);
                if (svc.TryGetValue("DisplayName", out var dn)) m.ServiceDisplayName = Sub(dn);
                if (svc.TryGetValue("StartType", out var st))   m.ServiceStartType = st;
            }

            // Scheduled task
            m.ScheduledTasksEnabled = GetEnabled("ScheduledTask");
            if (sections.TryGetValue("ScheduledTask", out var tsk))
            {
                if (tsk.TryGetValue("TaskName", out var tn)) m.TaskName = Sub(tn);
                if (tsk.TryGetValue("Schedule", out var sc)) m.TaskSchedule = sc;
            }

            // Firewall
            m.FirewallRulesEnabled = GetEnabled("Firewall");
            var rules = Collect("Firewall", "Rule");
            if (rules.Count > 0) m.FirewallRules = string.Join(",", rules);

            // Simple booleans
            m.ProtocolHandlersEnabled = GetEnabled("Protocols");
            m.ActiveSetupEnabled      = GetEnabled("ActiveSetup");
            m.AppPathsEnabled         = GetEnabled("AppPaths");
            m.StartupEnabled          = GetEnabled("Startup");
            m.FontsEnabled            = GetEnabled("Fonts");
            m.COMRegistrationEnabled  = GetEnabled("COM");

            // Suggested install context
            if (sections.TryGetValue("Install", out var inst)
                && inst.TryGetValue("DefaultContext", out var ctx))
            {
                m.DefaultContext = ctx.Equals("machine", StringComparison.OrdinalIgnoreCase)
                    ? "Machine" : "User";
            }
        }

        // Permissive INI parser for the indicative preset files. Handles the
        // "[Section] Key=Value" on-one-line style the presets use, and the
        // standard multi-line style.
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
    }
}
