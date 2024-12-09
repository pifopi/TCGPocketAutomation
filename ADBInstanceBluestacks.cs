using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceBluestacks : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private string _bluestacksName = "Pie64";

        private static SemaphoreSlim semaphore = new SemaphoreSlim(0, 1);

        private Timer timer;
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

        protected override async Task ConnectToADBInstanceAsync()
        {
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({semaphore.CurrentCount} available)");
            await semaphore.WaitAsync(program.Token);
            semaphoreToRelease = true;
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({semaphore.CurrentCount} available)");

            timer = new Timer(state =>
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, "Cancelling everything because 5 minutes has passed without releasing the held semaphore");
                program.Cancel();
                program = new CancellationTokenSource();
            }, null, (int)TimeSpan.FromMinutes(5).TotalMilliseconds, Timeout.Infinite);

            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                Utils.ExecuteCmd($"HD-Player.exe --instance {BluestacksName} --cmd launchAppWithBsx --package jp.pokemon.pokemontcgp");
                await Task.Delay(TimeSpan.FromMinutes(1), program.Token);
                string resultConnect = adbClient.Connect(IP, Port);
                if (resultConnect != $"connected to {IP}:{Port}" &&
                    resultConnect != $"already connected to {IP}:{Port}")
                {
                    throw new Exception(resultConnect);
                }
                DeviceData? device = await Utils.GetDeviceDataFromAsync(adbClient, $"{IP}:{Port}");
                deviceData = device.Value;
                await GoPastTileScreenAsync();
                await ReturnToMainMenuAsync();
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
                adbClient.Disconnect(IP, Port);
                Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"HD-Player.exe\" /F");
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
            semaphore.Release();

            return Task.CompletedTask;
        }
    }
}
