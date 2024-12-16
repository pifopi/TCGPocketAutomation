using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceBluestacks : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private string _bluestacksName = "Pie64";

        private static SemaphoreSlim semaphore = new(0, 1);

        private bool semaphoreToRelease = false;

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

        public string BluestacksName
        {
            get => _bluestacksName;
            set { _bluestacksName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"{Name}\t{IP}:{Port}\t{BluestacksName}";
        }

        public static void SetMaxParallelInstance(int maxParallelInstance)
        {
            semaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({semaphore.CurrentCount} available)");
            await semaphore.WaitAsync(token);
            semaphoreToRelease = true;
            token.ThrowIfCancellationRequested();
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({semaphore.CurrentCount} available)");

            Utils.ExecuteCmd($"HD-Player.exe --instance {BluestacksName} --cmd launchAppWithBsx --package jp.pokemon.pokemontcgp");
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
            if (!semaphoreToRelease)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({semaphore.CurrentCount} available)");
                return;
            }
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({semaphore.CurrentCount} available)");
            semaphoreToRelease = false;

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

                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
                semaphore.Release();
            }
        }
    }
}
