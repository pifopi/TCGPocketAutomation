using System.IO;
using System.Text.Json;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class Settings
    {
        public ulong DiscordUserId { get; set; }
        public ulong DiscordChannelId { get; set; }
        public string MuMuPath { get; set; } = "";
        public int MuMuMaxParallelInstance { get; set; }
        public List<MuMuSettings> MuMuInstances { get; set; } = [];
        public string BlueStacksPath { get; set; } = "";
        public int BlueStacksMaxParallelInstance { get; set; }
        public List<BlueStacksSettings> BlueStacksInstances { get; set; } = [];
        public string LDPlayerPath { get; set; } = "";
        public int LDPlayerMaxParallelInstance { get; set; }
        public List<LDPlayerSettings> LDPlayerInstances { get; set; } = [];
        public List<RealPhoneSettings> RealPhoneInstances { get; set; } = [];
    }

    public class MuMuSettings
    {
        public required string Name { get; set; }
        public int MuMuId { get; set; }
    }

    public class BlueStacksSettings
    {
        public required string Name { get; set; }
        public required string IP { get; set; }
        public int Port { get; set; }
        public required string BlueStacksName { get; set; }
    }

    public class LDPlayerSettings
    {
        public required string Name { get; set; }
        public required string SerialName { get; set; }
    }

    public class RealPhoneSettings
    {
        public required string Name { get; set; }
        public required string IP { get; set; }
        public int Port { get; set; }
    }

    public static class SettingsManager
    {
        public static Settings Settings { get; private set; } = new();

        public static void LoadSettings(string settingsFile)
        {
            if (!File.Exists(settingsFile))
            {
                throw new FileNotFoundException(settingsFile);
            }
            string jsonString = File.ReadAllText(settingsFile);
            Settings settings = JsonSerializer.Deserialize<Settings>(jsonString) ?? throw new Exception($"{settingsFile} cannot be read properly. Verify you fill it properly");
            Settings = settings;
        }
    }
}
