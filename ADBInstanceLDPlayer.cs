using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        private static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        private Timer timer;

        public string ADBName
        {
            get => _adbName;
            set { _adbName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{ADBName}";
        }

        private string LDPlayerName
        {
            get => $"\"{Name}\"";
        }

        public static void SetMaxParallelInstance(int maxParallelInstance)
        {
            semaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task ConnectToADBInstanceAsync()
        {
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({semaphore.CurrentCount} available)");
            await semaphore.WaitAsync(cancellationTokenSource.Token);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({semaphore.CurrentCount} available)");

            timer = new Timer(async state => { await DisconnectFromADBInstanceAsync(); }, null, (int)TimeSpan.FromMinutes(5).TotalMilliseconds, Timeout.Infinite);

            try
            {
                using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
                {
                    Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
                    await Task.Delay(60_000, cancellationTokenSource.Token);
                    deviceData = Utils.GetDeviceDataFrom(adbClient, $"{ADBName}");
                    await GoPastTileScreenAsync();
                }
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@282197676982927375> An exception has been raised:{exception}");
                await DisconnectFromADBInstanceAsync();
                throw;
            }
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            if (timer == null)
            {
                return Task.CompletedTask;
            }

            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                timer.Dispose();
                deviceData = new DeviceData();
                Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
            semaphore.Release();

            return Task.CompletedTask;
        }
    }
}
