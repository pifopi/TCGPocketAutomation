using Newtonsoft.Json.Linq;
using System.IO;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceMuMu : ADBInstanceViaIP
    {
        private int _mumuId = 1;
        private static SemaphoreSlim emulatorSemaphore = new(0, 1);
        private bool hasTakenEmulatorSemaphore = false;

        public int MuMuId
        {
            get => _mumuId;
            set { _mumuId = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"{Name}\t{IP}:{Port}\t{MuMuId}";
        }

        public static void SetMaxParallelInstance(int maxParallelInstance)
        {
            emulatorSemaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task StartInstanceAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await base.StartInstanceAsync(token);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({emulatorSemaphore.CurrentCount} available)");
            await emulatorSemaphore.WaitAsync(token);
            hasTakenEmulatorSemaphore = true;
            token.ThrowIfCancellationRequested();
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({emulatorSemaphore.CurrentCount} available)");

            string executablePath = Path.Combine(SettingsManager.Settings.MuMuPath, "shell", "MuMuPlayer.exe");
            Utils.ExecuteCmd($"\"{executablePath}\" -v {MuMuId}");
            await Task.Delay(TimeSpan.FromSeconds(30), token);

            ReadAdbPorts();
        }

        protected override async Task StopInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!hasTakenEmulatorSemaphore)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({emulatorSemaphore.CurrentCount} available)");
                return;
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({emulatorSemaphore.CurrentCount} available)");
            hasTakenEmulatorSemaphore = false;

            Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"MuMuPlayer.exe\" /F");
            await Task.Delay(TimeSpan.FromSeconds(10));

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({emulatorSemaphore.CurrentCount} available)");
            emulatorSemaphore.Release();
            await base.StopInstanceAsync();
        }

        private void ReadAdbPorts()
        {
            string configFile = Path.Combine(SettingsManager.Settings.MuMuPath, "vms", $"MuMuPlayerGlobal-12.0-{MuMuId}", "configs", "vm_config.json");
            string jsonContent = File.ReadAllText(configFile);
            JObject jsonFile = JObject.Parse(jsonContent);
            string? portAsString = jsonFile.SelectToken("vm")?.SelectToken("nat")?.SelectToken("port_forward")?.SelectToken("adb")?.SelectToken("host_port")?.ToString();
            if (portAsString == null)
            {
                throw new Exception("The adb config cannot be found");
            }
            Port = Int32.Parse(portAsString);
        }
    }
}
