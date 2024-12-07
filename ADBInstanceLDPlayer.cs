﻿using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        private static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        private Timer timer;
        private bool semaphoreToRelease = false;

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
            semaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task ConnectToADBInstanceAsync()
        {
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({semaphore.CurrentCount} available)");
            await semaphore.WaitAsync(cancellationTokenSource.Token);
            semaphoreToRelease = true;
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({semaphore.CurrentCount} available)");

            timer = new Timer(state =>
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, "Cancelling everything because 5 minutes has passed without releasing the holded semaphore");
                cancellationTokenSource.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
            }, null, (int)TimeSpan.FromMinutes(5).TotalMilliseconds, Timeout.Infinite);

            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
                await Task.Delay(60_000, cancellationTokenSource.Token);
                deviceData = Utils.GetDeviceDataFrom(adbClient, $"{ADBName}");
                await GoPastTileScreenAsync();
            }
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            if (!semaphoreToRelease)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({semaphore.CurrentCount} available)");
                return Task.CompletedTask;
            }
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({semaphore.CurrentCount} available)");
            semaphoreToRelease = false;

            timer.Dispose();

            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                deviceData = new DeviceData();
                Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
            semaphore.Release();

            return Task.CompletedTask;
        }
    }
}
