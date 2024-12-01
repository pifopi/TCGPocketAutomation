using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation
{
    public class Instance : INotifyPropertyChanged
    {
        public enum StatusEnum
        {
            Disconnected,
            Connected,
            CheckWonderPickPeriodically,
            CheckWonderPickOnce
        }

        private string _name = "New instance";
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private StatusEnum _status = StatusEnum.Disconnected;

        private AdbClient adbClient = new AdbClient();
        private DeviceData deviceData;

        private static Logger logger = new Logger();

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

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

        private string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{IP}:{Port}";
        }

        public StatusEnum Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(IsDisconnected));
            }
        }

        public bool IsConnected
        {
            get => _status == StatusEnum.Connected;
        }

        public bool IsBusy
        {
            get => !IsConnected && !IsDisconnected;
        }

        public bool IsDisconnected
        {
            get => _status == StatusEnum.Disconnected;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Connect()
        {
            if (!AdbServer.Instance.GetStatus().IsRunning)
            {
                AdbServer server = new AdbServer();
                StartServerResult resultStartServer = server.StartServer("adb", false);
                if (resultStartServer != StartServerResult.Started)
                {
                    throw new Exception("Can't start adb server, make sure you add adb.exe to your PATH");
                }
            }

            string resultConnect = adbClient.Connect(IP, Port);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }

            foreach (DeviceData device in adbClient.GetDevices())
            {
                if (device.Serial == $"{IP}:{Port}")
                {
                    deviceData = device;
                    break;
                }
            }

            Status = StatusEnum.Connected;
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            Status = StatusEnum.Connected;
        }

        private async Task<(bool, Point)> WaitFor(Func<Framebuffer, (bool, Point)> check, Action action, int retry, int delay)
        {
            bool found = false;
            Point location = new Point();
            for (int i = 0; i < retry; ++i)
            {
                Framebuffer framebuffer = await adbClient.GetFrameBufferAsync(deviceData, cancellationTokenSource.Token);
                (found, location) = check(framebuffer);
                if (found)
                {
                    break;
                }
                action();
                await Task.Delay(delay, cancellationTokenSource.Token);
            }
            return (found, location);
        }

        private async Task OpenGame()
        {
            var receiver = new ConsoleOutputReceiver();
            await adbClient.ExecuteRemoteCommandAsync("dumpsys activity", deviceData, receiver);
            string output = receiver.ToString();
            if (output.Contains("jp.pokemon.pokemontcgp"))
            {
                await adbClient.StopAppAsync(deviceData, "jp.pokemon.pokemontcgp", cancellationTokenSource.Token);
            }

            await adbClient.StartAppAsync(deviceData, "jp.pokemon.pokemontcgp", cancellationTokenSource.Token);
            (bool found, Point location) = await WaitFor(ImageProcessing.FindTitleScreen, () => { }, 60, 10_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenGame - Can't find the title screen template");
            }

            await logger.Log($"{LogHeader} - OpenGame - Found in location:{location}");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenGame - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task OpenWonderPickMenu()
        {
            (bool found, Point location) = await WaitFor(ImageProcessing.FindWonderPick, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenWonderPickMenu - Can't find the wonder pick template");
            }

            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Found in location:{location}");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickOK()
        {
            (bool found, Point location) = await WaitFor(ImageProcessing.FindOK, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickOK - Can't find the OK template");
            }

            await logger.Log($"{LogHeader} - ClickOK - Found in location:{location}");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickOK - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickTopRightCard()
        {
            (bool found, Point location) = await WaitFor(ImageProcessing.FindCard, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickTopRightCard - Can't find the card template");
            }

            await logger.Log($"{LogHeader} - ClickTopRightCard - Found in location:{location}");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickTopRightCard - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ReturnToMainMenu()
        {
            (bool found, Point location) = await WaitFor(ImageProcessing.FindWonderPick, async () => { await adbClient.ClickBackButtonAsync(deviceData, cancellationTokenSource.Token); }, 60, 10_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ReturnToMainMenu - Can't return to main menu");
            }

            await logger.Log($"{LogHeader} - ReturnToMainMenu - Found in location:{location}");
        }

        private async Task CheckWonderPickOnceAsync()
        {
            //await OpenGame();
            //await Task.Delay(30_000, cancellationTokenSource.Token);

            await OpenWonderPickMenu();
            await Task.Delay(30_000, cancellationTokenSource.Token);

            (bool found, Point location) = await WaitFor(ImageProcessing.FindBonusWonderPick, () => { }, 60, 1_000);
            if (found)
            {
                await Task.Delay(10_000, cancellationTokenSource.Token);
                await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
                await Task.Delay(30_000, cancellationTokenSource.Token);

                await ClickOK();
                await Task.Delay(30_000, cancellationTokenSource.Token);

                await ClickTopRightCard();
                await Task.Delay(30_000, cancellationTokenSource.Token);
            }
            await ReturnToMainMenu();
            await Task.Delay(30_000, cancellationTokenSource.Token);
        }

        public async void CheckWonderPickPeriodically()
        {
            try
            {
                Status = StatusEnum.CheckWonderPickPeriodically;
                while (true)
                {
                    await CheckWonderPickOnceAsync();
                    await Task.Delay(15 * 60 * 1_000, cancellationTokenSource.Token);
                }
            }
            catch (Exception exception)
            {
                await logger.Log($"<@282197676982927375> An exception has been raised:{exception}");
                CheckWonderPickPeriodically();
            }
            finally
            {
                Status = StatusEnum.Connected;
            }
        }

        public async void CheckWonderPickOnce()
        {
            try
            {
                Status = StatusEnum.CheckWonderPickOnce;
                await CheckWonderPickOnceAsync();
            }
            catch (Exception exception)
            {
                await logger.Log($"<@282197676982927375> An exception has been raised:{exception}");
            }
            finally
            {
                Status = StatusEnum.Connected;
            }
        }
    }
}
