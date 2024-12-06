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
            get => $"[{DateTime.Now}]\t{Name}\t{IP}:{Port}\t{BluestacksName}";
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
                    Utils.ExecuteCmd($"HD-Player.exe --instance {BluestacksName} --cmd launchAppWithBsx --package jp.pokemon.pokemontcgp --source desktop_shortcut");
                    await Task.Delay(60_000, cancellationTokenSource.Token);
                    string resultConnect = adbClient.Connect(IP, Port);
                    if (resultConnect != $"connected to {IP}:{Port}" &&
                        resultConnect != $"already connected to {IP}:{Port}")
                    {
                        throw new Exception(resultConnect);
                    }
                    deviceData = Utils.GetDeviceDataFrom(adbClient, $"{IP}:{Port}");
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
                adbClient.Disconnect(IP, Port);
                Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"HD-Player.exe\" /F");
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({semaphore.CurrentCount} available)");
            semaphore.Release();

            return Task.CompletedTask;
        }
    }
}
