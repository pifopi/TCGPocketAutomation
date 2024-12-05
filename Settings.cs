using System.IO;
using System.Text.Json;

namespace TCGPocketAutomation
{
    public class ADBSettings
    {
        public int BluestacksMaxParallelInstance { get; set; } = new();
        public List<BluestacksSettings> BluestacksInstances { get; set; } = new();
        public int LDPlayerMaxParallelInstance { get; set; } = new();
        public List<LDPlayerSettings> LDPlayerInstances { get; set; } = new();
        public List<RealPhoneSettings> RealPhoneInstances { get; set; } = new();
    }

    public class BluestacksSettings
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public string BluestacksName { get; set; }
    }

    public class LDPlayerSettings
    {
        public string Name { get; set; }
        public string ADBName { get; set; }
    }

    public class RealPhoneSettings
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
    }

    public static class SettingsManager
    {
        private const string SettingsFile = "settings.json";

        public static ADBSettings LoadSettings()
        {
            if (File.Exists(SettingsFile))
            {
                string jsonString = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<ADBSettings>(jsonString) ?? new ADBSettings();
            }
            return new ADBSettings();
        }
    }
}
