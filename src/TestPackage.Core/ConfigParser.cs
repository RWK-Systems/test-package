using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestPackage.Core
{
    public class ConfigParser
    {
        private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

        public static ConfigParser Load(string path)
        {
            var parser = new ConfigParser();
            if (!File.Exists(path))
                throw new FileNotFoundException($"Configuration file not found: {path}");

            string currentSection = "";
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line[1..^1].Trim();
                    if (!parser._sections.ContainsKey(currentSection))
                        parser._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }

                var eqIndex = line.IndexOf('=');
                if (eqIndex > 0)
                {
                    var key = line[..eqIndex].Trim();
                    var value = line[(eqIndex + 1)..].Trim();
                    if (!parser._sections.ContainsKey(currentSection))
                        parser._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    parser._sections[currentSection][key] = value;
                }
            }
            return parser;
        }

        public string Get(string section, string key, string defaultValue = "")
        {
            if (_sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v))
                return v;
            return defaultValue;
        }

        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            var v = Get(section, key, defaultValue.ToString());
            return v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var v = Get(section, key, defaultValue.ToString());
            return int.TryParse(v, out var result) ? result : defaultValue;
        }

        public List<string> GetList(string section, string key, char separator = ',')
        {
            var v = Get(section, key);
            if (string.IsNullOrWhiteSpace(v)) return new List<string>();
            return v.Split(separator).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        public Dictionary<string, string> GetSection(string section)
        {
            return _sections.TryGetValue(section, out var s) ? new Dictionary<string, string>(s) : new Dictionary<string, string>();
        }

        public IEnumerable<string> GetSectionNames() => _sections.Keys;

        public void Set(string section, string key, string value)
        {
            if (!_sections.ContainsKey(section))
                _sections[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section][key] = value;
        }

        public string ExpandVariables(string input, string installDir)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = input;
            result = result.Replace("%InstallDir%", installDir, StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%ProgramFiles%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%LocalAppData%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%ProgramData%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%CommonAppData%", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%Desktop%", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%CommonDesktop%", Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), StringComparison.OrdinalIgnoreCase);
            result = result.Replace("%DATE%", DateTime.Now.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }
}
