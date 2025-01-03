using AdvancedSharpAdbClient.Models;
using System.IO;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceBlueStacks : ADBInstanceViaIP
    {
        private string _blueStacksName = "Pie64";
        private static SemaphoreSlim emulatorSemaphore = new(0, 1);
        private bool hasTakenEmulatorSemaphore = false;

        public string BlueStacksName
        {
            get => _blueStacksName;
            set { _blueStacksName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"{Name}\t{IP}:{Port}\t{BlueStacksName}";
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

            string executablePath = Path.Combine(SettingsManager.Settings.BlueStacksPath, "HD-Player.exe");
            Utils.ExecuteCmd($"\"{executablePath}\" --instance {BlueStacksName}");
            await Task.Delay(TimeSpan.FromSeconds(30), token);
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

            Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"HD-Player.exe\" /F");
            await Task.Delay(TimeSpan.FromSeconds(10));

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({emulatorSemaphore.CurrentCount} available)");
            emulatorSemaphore.Release();
            await base.StopInstanceAsync();
        }
    }
}
