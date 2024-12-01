using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using Auto_LDPlayer;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation
{
    public class Instance : INotifyPropertyChanged
    {
        public enum StatusEnum
        {
            Available,
            CheckWonderPickPeriodically,
            CheckWonderPickOnce
        }

        private string _name = "New instance";
        private bool _useLDPlayer = true;
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private StatusEnum _status = StatusEnum.Available;

        private AdbClient adbClient = new AdbClient();
        private DeviceData deviceData;

        private static Logger logger = new Logger();

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool UseLDPlayer
        {
            get => _useLDPlayer;
            set { _useLDPlayer = value; OnPropertyChanged(); }
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

        public StatusEnum Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsBusy));
                OnPropertyChanged(nameof(IsAvailable));
            }
        }

        public bool IsBusy
        {
            get => _status != StatusEnum.Available;
        }

        public bool IsAvailable
        {
            get => _status == StatusEnum.Available;
        }

        private string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{IP}:{Port}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async void Stop()
        {
            await logger.Log($"{LogHeader} - Stop");
            cancellationTokenSource.Cancel();
            Status = StatusEnum.Available;
        }

        private async Task<(bool, double, Point)> WaitFor(Func<Framebuffer, (bool, double, Point)> check, Action action, int retry, int delay)
        {
            bool found = false;
            double value = 0;
            Point location = new Point();
            for (int i = 0; i < retry; ++i)
            {
                Framebuffer framebuffer = await adbClient.GetFrameBufferAsync(deviceData, cancellationTokenSource.Token);
                (found, value, location) = check(framebuffer);
                if (found)
                {
                    break;
                }
                action();
                await Task.Delay(delay, cancellationTokenSource.Token);
            }
            return (found, value, location);
        }

        private DeviceData GetDeviceData(string key)
        {
            foreach (DeviceData device in adbClient.GetDevices())
            {
                if (device.Serial == key)
                {
                    return deviceData = device;
                }
            }
            throw new Exception($"Could not find {key} in list of adb devices");
        }

        private string GetLDPlayerName(string name)
        {
            return $"\"{Name}\"";
        }

        private void ConnectViaIP()
        {
            string resultConnect = adbClient.Connect(IP, Port);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }

            deviceData = GetDeviceData($"{IP}:{Port}");
        }

        private void ConnectViaLDPlayer()
        {
            deviceData = GetDeviceData($"emulator-{Port}");
        }

        private async Task GoPastTileScreen()
        {
            await logger.Log($"{LogHeader} - GoPastTileScreen");
            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchTitleScreen, () => { }, 60, 10_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenGame - Could not find the title screen template (${value})");
            }

            await logger.Log($"{LogHeader} - OpenGame - Found in location:{location} (${value})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenGame - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task OpenWonderPickMenu()
        {
            await logger.Log($"{LogHeader} - OpenWonderPickMenu");
            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchWonderPick, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenWonderPickMenu - Could not find the wonder pick template (${value})");
            }

            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Found in location:{location} (${value})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickOK()
        {
            await logger.Log($"{LogHeader} - ClickOK");
            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchOK, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickOK - Could not find the OK template (${value})");
            }

            await logger.Log($"{LogHeader} - ClickOK - Found in location:{location} (${value})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickOK - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickTopRightCard()
        {
            await logger.Log($"{LogHeader} - ClickTopRightCard");
            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchCard, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickTopRightCard - Could not find the card template (${value})");
            }

            await logger.Log($"{LogHeader} - ClickTopRightCard - Found in location:{location} (${value})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickTopRightCard - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ReturnToMainMenu()
        {
            await logger.Log($"{LogHeader} - ReturnToMainMenu");
            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchWonderPick, async () => { await adbClient.ClickBackButtonAsync(deviceData, cancellationTokenSource.Token); }, 60, 10_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ReturnToMainMenu - Could not return to main menu (${value})");
            }

            await logger.Log($"{LogHeader} - ReturnToMainMenu - Found in location:{location} (${value})");
        }

        private async Task CheckWonderPickOnceAsync()
        {
            await logger.Log($"{LogHeader} - CheckWonderPickOnceAsync");
            if (UseLDPlayer)
            {
                LDPlayer.OpenApp(Auto_LDPlayer.Enums.LDType.Name, GetLDPlayerName(Name), "jp.pokemon.pokemontcgp");
                await Task.Delay(60_000, cancellationTokenSource.Token);
                ConnectViaLDPlayer();
                await GoPastTileScreen();
            }
            else
            {
                ConnectViaIP();
            }

            await OpenWonderPickMenu();
            await Task.Delay(30_000, cancellationTokenSource.Token);

            (bool found, double value, Point location) = await WaitFor(ImageProcessing.SearchBonusWonderPick, () => { }, 60, 1_000);
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

            if (UseLDPlayer)
            {
                LDPlayer.Close(Auto_LDPlayer.Enums.LDType.Name, GetLDPlayerName(Name));
            }
        }

        public async void CheckWonderPickPeriodically()
        {
            await logger.Log($"{LogHeader} - CheckWonderPickPeriodically");
            try
            {
                Status = StatusEnum.CheckWonderPickPeriodically;
                while (true)
                {
                    await CheckWonderPickOnceAsync();
                    await Task.Delay(10 * 60 * 1_000, cancellationTokenSource.Token);
                }
            }
            catch (Exception exception)
            {
                await logger.Log($"<@282197676982927375> An exception has been raised:{exception}");
                if (UseLDPlayer)
                {
                    LDPlayer.Close(Auto_LDPlayer.Enums.LDType.Name, GetLDPlayerName(Name));
                }
                CheckWonderPickPeriodically();
            }
            finally
            {
                Status = StatusEnum.Available;
            }
        }

        public async void CheckWonderPickOnce()
        {
            await logger.Log($"{LogHeader} - CheckWonderPickOnce");
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
                Status = StatusEnum.Available;
            }
        }
    }
}
