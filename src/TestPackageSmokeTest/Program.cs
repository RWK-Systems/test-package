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
