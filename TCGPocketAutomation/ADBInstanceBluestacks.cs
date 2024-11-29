using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceBlueStacks : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private string _blueStacksName = "Pie64";

        private static SemaphoreSlim blueStacksSemaphore = new(0, 1);

        private bool blueStacksSemaphoreToRelease = false;

        public string IP
        {
            get => _ip;
            set { _ip = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

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
            blueStacksSemaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            await base.ConnectToADBInstanceAsync(token);
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({blueStacksSemaphore.CurrentCount} available)");
            await blueStacksSemaphore.WaitAsync(token);
            blueStacksSemaphoreToRelease = true;
            token.ThrowIfCancellationRequested();
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({blueStacksSemaphore.CurrentCount} available)");

            Utils.ExecuteCmd($"HD-Player.exe --instance {BlueStacksName} --cmd launchAppWithBsx --package jp.pokemon.pokemontcgp");
            await Task.Delay(TimeSpan.FromMinutes(1), token);
            string resultConnect = await adbClient.ConnectAsync(IP, Port, token);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            deviceData = await Utils.GetDeviceDataFromAsync(adbClient, $"{IP}:{Port}", TimeSpan.FromMinutes(1), token);
            await Task.Delay(TimeSpan.FromSeconds(30), token);
            await WaitForTitleScreenAsync(TimeSpan.FromMinutes(2), token);
            await GoPastTitleScreenAsync(TimeSpan.FromSeconds(30), token);
            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(30), token);
        }

        protected override async Task DisconnectFromADBInstanceAsync()
        {

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!blueStacksSemaphoreToRelease)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({blueStacksSemaphore.CurrentCount} available)");
                return;
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({blueStacksSemaphore.CurrentCount} available)");
            blueStacksSemaphoreToRelease = false;

            try
            {
                await adbClient.DisconnectAsync(IP, Port);
            }
            catch (AdbException exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
            }
            finally
            {
                deviceData = new DeviceData();
                Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"HD-Player.exe\" /F");
                await Task.Delay(TimeSpan.FromSeconds(10));

                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({blueStacksSemaphore.CurrentCount} available)");
                blueStacksSemaphore.Release();
                await base.DisconnectFromADBInstanceAsync();
            }
        }
    }
}
