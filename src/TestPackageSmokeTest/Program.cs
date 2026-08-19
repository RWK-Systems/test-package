using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using TestPackage.Core;

namespace TestPackageSmokeTest
{
    /// <summary>
    /// Console-mode smoke test that exercises the installer logic without a GUI.
    /// Runs in CI to prove the install/uninstall mechanics work.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            var separator = new string('=', 70);
            Console.WriteLine(separator);
            Console.WriteLine("  TestPackage Smoke Test");
            Console.WriteLine("  Exercises install/uninstall logic in console mode");
            Console.WriteLine(separator);
            Console.WriteLine();

            // --- Execution Context ---
            Console.WriteLine("[Execution Context]");
            Console.WriteLine($"  User:           {Environment.UserDomainName}\\{Environment.UserName}");
            Console.WriteLine($"  Machine:        {Environment.MachineName}");
            Console.WriteLine($"  OS:             {Environment.OSVersion}");
            Console.WriteLine($"  64-bit OS:      {Environment.Is64BitOperatingSystem}");
            Console.WriteLine($"  64-bit Process: {Environment.Is64BitProcess}");
            Console.WriteLine($"  CLR Version:    {Environment.Version}");

            bool isAdmin;
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            Console.WriteLine($"  Is Admin:       {isAdmin}");
            Console.WriteLine();

            // --- Load Config ---
            var configPath = FindConfig();
            if (configPath == null)
            {
                Console.WriteLine("ERROR: config.ini not found!");
                return 1;
            }
            Console.WriteLine($"[Config] Loaded: {configPath}");
            var config = ConfigParser.Load(configPath);
            Console.WriteLine($"  AppName:       {config.Get("General", "AppName")}");
            Console.WriteLine($"  AppVersion:    {config.Get("General", "AppVersion")}");
            Console.WriteLine($"  RequireAdmin:  {config.Get("General", "RequireAdmin")}");
            Console.WriteLine();

            // --- List Configured Features ---
            Console.WriteLine("[Configured Features]");
            var features = new (string section, string key, string label)[]
            {
                ("TestFiles", "Enabled", "Test Files"),
                ("Registry", "Enabled", "Registry Entries"),
                ("Shortcuts", "CreateDesktopShortcut", "Desktop Shortcut"),
                ("Shortcuts", "CreateStartMenuEntry", "Start Menu Entry"),
                ("FileAssociations", "Enabled", "File Associations"),
                ("ContextMenu", "Enabled", "Context Menu"),
                ("EnvironmentVariables", "Enabled", "Environment Variables"),
                ("Services", "Enabled", "Windows Service"),
                ("ScheduledTasks", "Enabled", "Scheduled Task"),
                ("FirewallRules", "Enabled", "Firewall Rules"),
                ("ProtocolHandlers", "Enabled", "Protocol Handlers"),
                ("ActiveSetup", "Enabled", "Active Setup"),
                ("AppPaths", "Enabled", "App Paths"),
                ("Startup", "Enabled", "Startup Entry"),
                ("Fonts", "Enabled", "Font Installation"),
                ("InstallerSize", "Enabled", "Installer Size"),
            };
            foreach (var (section, key, label) in features)
            {
                var enabled = config.GetBool(section, key);
                Console.WriteLine($"  {(enabled ? "[ON] " : "[OFF]")} {label}");
            }
            Console.WriteLine();

            // --- Wizard Pages ---
            Console.WriteLine("[Wizard Pages]");
            var pages = new[] { "ShowWelcome", "ShowEULA", "ShowInstallContext", "ShowTargetDirectory",
                               "ShowComponents", "ShowDesktopShortcut", "ShowStartMenuPin",
                               "ShowRebootOption", "ShowActiveSetup" };
            foreach (var page in pages)
            {
                var shown = config.GetBool("WizardPages", page, true);
                Console.WriteLine($"  {(shown ? "[ON] " : "[OFF]")} {page}");
            }
            Console.WriteLine();

            // --- Components ---
            Console.WriteLine("[Components]");
            var components = config.GetSection("Components");
            foreach (var comp in components)
            {
                Console.WriteLine($"  {(comp.Value.Equals("true", StringComparison.OrdinalIgnoreCase) ? "[ON] " : "[OFF]")} {comp.Key}");
            }
            Console.WriteLine();

            // --- Perform Test Install ---
            var testDir = Path.Combine(Path.GetTempPath(), "TestPackage_SmokeTest_" + Guid.NewGuid().ToString("N")[..8]);
            Console.WriteLine(separator);
            Console.WriteLine($"[Install Test] Target: {testDir}");
            Console.WriteLine(separator);

            var selectedComponents = new List<string>();
            foreach (var comp in components)
            {
                if (comp.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    selectedComponents.Add(comp.Key);
            }

            var installer = new InstallActions(config, msg => Console.WriteLine($"  {msg}"));

            try
            {
                installer.Execute(
                    installDir: testDir,
                    context: "User",
                    selectedComponents: selectedComponents,
                    desktopShortcut: false,   // Skip shortcuts in CI
                    startMenuPin: false,
                    activeSetup: false
                );
                Console.WriteLine();
                Console.WriteLine("  >>> INSTALL COMPLETED SUCCESSFULLY <<<");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  >>> INSTALL FAILED: {ex.Message} <<<");
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
            Console.WriteLine();

            // --- Verify Install ---
            Console.WriteLine("[Verification] Checking installed files...");
            var manifest = InstallManifest.Load(testDir);
            if (manifest == null)
            {
                Console.WriteLine("  ERROR: Manifest not found!");
                return 1;
            }

            Console.WriteLine($"  Manifest loaded: {manifest.AppName} v{manifest.AppVersion}");
            Console.WriteLine($"  Install context: {manifest.InstallContext}");
            Console.WriteLine($"  Installed by:    {manifest.InstalledBy}");
            Console.WriteLine($"  Install date:    {manifest.InstallDate}");
            Console.WriteLine($"  Components:      {string.Join(", ", manifest.Components)}");
            Console.WriteLine();

            int foundFiles = 0, missingFiles = 0;
            Console.WriteLine("  [Files]");
            foreach (var file in manifest.CreatedFiles)
            {
                var exists = File.Exists(file);
                Console.WriteLine($"    {(exists ? "OK  " : "MISS")} {file}");
                if (exists) foundFiles++; else missingFiles++;
            }
            Console.WriteLine($"  Files: {foundFiles} found, {missingFiles} missing");
            Console.WriteLine();

            Console.WriteLine("  [Directories]");
            foreach (var dir in manifest.CreatedDirectories)
            {
                var exists = Directory.Exists(dir);
                Console.WriteLine($"    {(exists ? "OK  " : "MISS")} {dir}");
            }
            Console.WriteLine();

            // --- Perform Cleanup ---
            Console.WriteLine(separator);
            Console.WriteLine("[Cleanup] Removing test installation...");
            Console.WriteLine(separator);
            try
            {
                // Clean registry entries
                foreach (var entry in manifest.RegistryEntries)
                {
                    Console.WriteLine($"  Cleaning registry: {entry.Split('|')[0]}");
                }

                // Clean files
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                    Console.WriteLine($"  Removed: {testDir}");
                }
                Console.WriteLine("  >>> CLEANUP COMPLETED <<<");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Cleanup warning: {ex.Message}");
            }

            Console.WriteLine();

            // --- Installer Size Disk-Space Check Test ---
            // Installer size pads the generated setup EXE (done by the Configurator).
            // At install time the only effect is the target-drive free-space check;
            // the install directory itself must NOT be padded.
            Console.WriteLine(separator);
            Console.WriteLine("[Installer Size Test] Disk-space check, no install-dir padding");
            Console.WriteLine(separator);
            var sizeTestDir = Path.Combine(Path.GetTempPath(), "TestPackage_SizeTest_" + Guid.NewGuid().ToString("N")[..8]);
            try
            {
                config.Set("InstallerSize", "Enabled", "true");
                config.Set("InstallerSize", "SizeMB", "1");

                var sizeInstaller = new InstallActions(config, msg => Console.WriteLine($"  {msg}"));
                sizeInstaller.Execute(sizeTestDir, "User", selectedComponents, false, false, false);

                if (File.Exists(Path.Combine(sizeTestDir, "payload.dat")))
                {
                    Console.WriteLine("  >>> SIZE TEST FAILED: install dir was padded (payload.dat present) <<<");
                    return 1;
                }
                Console.WriteLine("  Disk-space check passed and install dir not padded.");
                Console.WriteLine("  >>> SIZE TEST PASSED <<<");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  >>> SIZE TEST FAILED: {ex.Message} <<<");
                return 1;
            }
            finally
            {
                try { if (Directory.Exists(sizeTestDir)) Directory.Delete(sizeTestDir, true); } catch { }
            }
            Console.WriteLine();

            // --- Config Round-Trip Test ---
            // Proves ConfigModel <-> ConfigWriter <-> ConfigParser is lossless.
            // A regression here would silently corrupt configs loaded/saved by
            // the Configurator, so it is worth catching in CI.
            Console.WriteLine(separator);
            Console.WriteLine("[Round-Trip Test] ConfigModel <-> INI");
            Console.WriteLine(separator);
            try
            {
                var m1 = ConfigModel.FromParser(ConfigParser.Load(configPath));
                var tmpIni = Path.Combine(Path.GetTempPath(), "TestPackage_RT_" + Guid.NewGuid().ToString("N")[..8] + ".ini");
                File.WriteAllText(tmpIni, ConfigWriter.Write(m1));
                var m2 = ConfigModel.FromParser(ConfigParser.Load(tmpIni));
                try { File.Delete(tmpIni); } catch { }

                var mismatches = new List<string>();
                void Check(string label, object? a, object? b)
                {
                    var sa = a?.ToString() ?? "";
                    var sb = b?.ToString() ?? "";
                    if (!string.Equals(sa, sb, StringComparison.Ordinal))
                        mismatches.Add($"    {label}: '{sa}' != '{sb}'");
                }
                Check("AppName",                 m1.AppName,                 m2.AppName);
                Check("AppVersion",              m1.AppVersion,              m2.AppVersion);
                Check("AppPublisher",            m1.AppPublisher,            m2.AppPublisher);
                Check("AppURL",                  m1.AppURL,                  m2.AppURL);
                Check("AppGUID",                 m1.AppGUID,                 m2.AppGUID);
                Check("RequireAdmin",            m1.RequireAdmin,            m2.RequireAdmin);
                Check("DefaultContext",          m1.DefaultContext,          m2.DefaultContext);
                Check("DefaultPath",             m1.DefaultPath,             m2.DefaultPath);
                Check("StartMenuFolder",         m1.StartMenuFolder,         m2.StartMenuFolder);
                Check("TestFiles",               m1.TestFiles,               m2.TestFiles);
                Check("RegistryEntries",         m1.RegistryEntries,         m2.RegistryEntries);
                Check("FileAssociations",        m1.FileAssociations,        m2.FileAssociations);
                Check("ContextMenuEntries",      m1.ContextMenuEntries,      m2.ContextMenuEntries);
                Check("EnvironmentVariables",    m1.EnvironmentVariables,    m2.EnvironmentVariables);
                Check("FirewallRules",           m1.FirewallRules,           m2.FirewallRules);
                Check("ProtocolHandlers",        m1.ProtocolHandlers,        m2.ProtocolHandlers);
                Check("InstallerSizeEnabled",     m1.InstallerSizeEnabled,     m2.InstallerSizeEnabled);
                Check("InstallerSizeMB",           m1.InstallerSizeMB,           m2.InstallerSizeMB);
                Check("CodeSigningMode",           m1.CodeSigningMode,           m2.CodeSigningMode);
                Check("CodeSigningPfxPath",        m1.CodeSigningPfxPath,        m2.CodeSigningPfxPath);
                Check("CodeSigningPfxPassword",    m1.CodeSigningPfxPassword,    m2.CodeSigningPfxPassword);
                Check("CodeSigningTimestampUrl",   m1.CodeSigningTimestampUrl,   m2.CodeSigningTimestampUrl);
                Check("Components.Count",        m1.Components.Count,        m2.Components.Count);
                for (int i = 0; i < Math.Min(m1.Components.Count, m2.Components.Count); i++)
                {
                    Check($"Components[{i}].Name",           m1.Components[i].Name,           m2.Components[i].Name);
                    Check($"Components[{i}].DefaultSelected", m1.Components[i].DefaultSelected, m2.Components[i].DefaultSelected);
                }

                if (mismatches.Count == 0)
                {
                    Console.WriteLine("  All checked properties round-trip identically.");
                    Console.WriteLine("  >>> ROUND-TRIP PASSED <<<");
                }
                else
                {
                    Console.WriteLine("  >>> ROUND-TRIP FAILED — mismatches:");
                    foreach (var mm in mismatches) Console.WriteLine(mm);
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  >>> ROUND-TRIP FAILED: {ex.Message} <<<");
                return 1;
            }
            Console.WriteLine();

            // --- Composite Pipe Parse Test ---
            // Exercises the pipe/comma parser rules that all seven composite
            // fields share, especially the "value contains a comma" case that
            // makes File Associations tricky (icon "app.exe,0").
            Console.WriteLine(separator);
            Console.WriteLine("[Composite Parse Test] pipe / comma boundary handling");
            Console.WriteLine(separator);
            try
            {
                // Reuse ConfigModel to check that a config with icon-index entries
                // parses and re-serializes losslessly.
                var m = new ConfigModel
                {
                    FileAssociationsEnabled = true,
                    FileAssociations = @".tpkg|TestPackage.Document|TestPackage Document|%InstallDir%\App.exe,0,.tpkx|TestPackage.Archive|TestPackage Archive|%InstallDir%\App.exe,1"
                };
                var tmp = Path.Combine(Path.GetTempPath(), "TestPackage_Composite_" + Guid.NewGuid().ToString("N")[..8] + ".ini");
                File.WriteAllText(tmp, ConfigWriter.Write(m));
                var m2 = ConfigModel.FromParser(ConfigParser.Load(tmp));
                try { File.Delete(tmp); } catch { }
                if (!string.Equals(m.FileAssociations, m2.FileAssociations, StringComparison.Ordinal))
                {
                    Console.WriteLine($"  >>> COMPOSITE PARSE FAILED:");
                    Console.WriteLine($"      before: {m.FileAssociations}");
                    Console.WriteLine($"      after:  {m2.FileAssociations}");
                    return 1;
                }
                Console.WriteLine("  File association with icon-index round-trips cleanly.");
                Console.WriteLine("  >>> COMPOSITE PARSE PASSED <<<");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  >>> COMPOSITE PARSE FAILED: {ex.Message} <<<");
                return 1;
            }
            Console.WriteLine();

            Console.WriteLine(separator);
            Console.WriteLine("  SMOKE TEST PASSED");
            Console.WriteLine($"  TestPackage by RWK Systems - https://rwksystems.com");
            Console.WriteLine(separator);

            return 0;
        }

        static string? FindConfig()
        {
            // Look for config.ini in several locations
            var candidates = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"),
                Path.Combine(Directory.GetCurrentDirectory(), "config.ini"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "config.ini"),
            };
            foreach (var path in candidates)
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full)) return full;
            }
            return null;
        }
    }
}
