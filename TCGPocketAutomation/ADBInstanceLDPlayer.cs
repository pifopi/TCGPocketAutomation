using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        private static SemaphoreSlim emulatorSemaphore = new(0, 1);

        private bool hasTakenEmulatorSemaphore = false;

        public string ADBName
        {
            get => _adbName;
            set { _adbName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"{Name}\t{ADBName}";
        }

        private string LDPlayerName
        {
            get => $"\"{Name}\"";
        }

        public static void SetMaxParallelInstance(int maxParallelInstance)
        {
            emulatorSemaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            await base.ConnectToADBInstanceAsync(token);
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({emulatorSemaphore.CurrentCount} available)");
            await emulatorSemaphore.WaitAsync(token);
            hasTakenEmulatorSemaphore = true;
            token.ThrowIfCancellationRequested();
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({emulatorSemaphore.CurrentCount} available)");

            Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
            deviceData = await Utils.GetDeviceDataFromAsync(adbClient, ADBName, TimeSpan.FromMinutes(1), token);
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            await WaitForTitleScreenAsync(TimeSpan.FromMinutes(2), token);
            await GoPastTitleScreenAsync(TimeSpan.FromSeconds(30), token);
            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(30), token);
        }

        protected override async Task DisconnectFromADBInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!hasTakenEmulatorSemaphore)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({emulatorSemaphore.CurrentCount} available)");
                return;
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({emulatorSemaphore.CurrentCount} available)");
            hasTakenEmulatorSemaphore = false;

            deviceData = new DeviceData();
            Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            await Task.Delay(TimeSpan.FromSeconds(10));

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({emulatorSemaphore.CurrentCount} available)");
            emulatorSemaphore.Release();
            await base.DisconnectFromADBInstanceAsync();
        }
    }
}
