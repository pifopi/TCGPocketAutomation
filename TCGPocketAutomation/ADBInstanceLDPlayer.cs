﻿using AdvancedSharpAdbClient.Models;
using System.Windows.Input;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        private static SemaphoreSlim semaphore = new(0, 1);

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
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({semaphore.CurrentCount} available)");
            await semaphore.WaitAsync(program.Token);
            semaphoreToRelease = true;
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({semaphore.CurrentCount} available)");

            Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
            while (!program.IsCancellationRequested)
            {
                DeviceData? device = await Utils.GetDeviceDataFromAsync(adbClient, ADBName);
                if (device.HasValue)
                {
                    deviceData = device.Value;
                    break;
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(20), program.Token);
            await WaitForTileScreenAsync();
            await GoPastTileScreenAsync();
            await ReturnToMainMenuAsync();
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!semaphoreToRelease)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({semaphore.CurrentCount} available)");
                return Task.CompletedTask;
            }
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({semaphore.CurrentCount} available)");
            semaphoreToRelease = false;

            deviceData = new DeviceData();
            Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
            semaphore.Release();

            return Task.CompletedTask;
        }
    }
}
