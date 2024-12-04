using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation
{
    public abstract class ADBInstance : INotifyPropertyChanged
    {
        public enum StatusEnum
        {
            NotRunning,
            CheckWonderPickPeriodically,
            CheckWonderPickOnce
        }

        private string _name = "New instance";
        private StatusEnum _status = StatusEnum.NotRunning;

        protected AdbClient adbClient = new AdbClient();
        protected DeviceData deviceData = new DeviceData();

        protected static Logger logger = new Logger();

        protected CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public StatusEnum Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsNotRunning));
            }
        }

        public bool IsRunning
        {
            get => _status != StatusEnum.NotRunning;
        }

        public bool IsNotRunning
        {
            get => _status == StatusEnum.NotRunning;
        }

        protected abstract string LogHeader { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task StartProgram(StatusEnum status, string programName)
        {
            Status = status;
            await logger.Log($"{LogHeader} - StartProgram {programName}");
        }

        public async Task StopProgram()
        {
            await logger.Log($"{LogHeader} - StopProgram Begin");
            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            await logger.Log($"{LogHeader} - StopProgram End");
            Status = StatusEnum.NotRunning;
        }

        protected abstract Task ConnectToADBInstance();

        protected abstract Task DisconnectFromADBInstance();

        private async Task<(bool, double, Point)> WaitFor(Func<Framebuffer, (bool, double, Point)> check, Action action, uint retry, int millisecondsDelay)
        {
            bool found = false;
            double alpha = 0;
            Point location = new Point();
            for (uint i = 0; i < retry; ++i)
            {
                Framebuffer framebuffer = await adbClient.GetFrameBufferAsync(deviceData, cancellationTokenSource.Token);
                (found, alpha, location) = check(framebuffer);
                if (found)
                {
                    break;
                }
                action();
                await Task.Delay(millisecondsDelay, cancellationTokenSource.Token);
            }
            return (found, alpha, location);
        }

        protected async Task GoPastTileScreen()
        {
            await logger.Log($"{LogHeader} - GoPastTileScreen");
            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchTitleScreen, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenGame - Could not find the title screen template ({alpha})");
            }

            await logger.Log($"{LogHeader} - OpenGame - Found in location:{location} ({alpha})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenGame - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task OpenWonderPickMenu()
        {
            await logger.Log($"{LogHeader} - OpenWonderPickMenu");
            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchWonderPick, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - OpenWonderPickMenu - Could not find the wonder pick template ({alpha})");
            }

            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Found in location:{location} ({alpha})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - OpenWonderPickMenu - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickOK()
        {
            await logger.Log($"{LogHeader} - ClickOK");
            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchOK, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickOK - Could not find the OK template ({alpha})");
            }

            await logger.Log($"{LogHeader} - ClickOK - Found in location:{location} ({alpha})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickOK - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ClickTopRightCard()
        {
            await logger.Log($"{LogHeader} - ClickTopRightCard");
            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchCard, () => { }, 60, 1_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ClickTopRightCard - Could not find the card template ({alpha})");
            }

            await logger.Log($"{LogHeader} - ClickTopRightCard - Found in location:{location} ({alpha})");
            await Task.Delay(10_000, cancellationTokenSource.Token);
            await logger.Log($"{LogHeader} - ClickTopRightCard - Clicking on location:{location}");
            await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
        }

        private async Task ReturnToMainMenu()
        {
            await logger.Log($"{LogHeader} - ReturnToMainMenu");
            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchWonderPick, async () => { await adbClient.ClickBackButtonAsync(deviceData, cancellationTokenSource.Token); }, 60, 10_000);
            if (!found)
            {
                throw new Exception($"{LogHeader} - ReturnToMainMenu - Could not return to main menu ({alpha})");
            }

            await logger.Log($"{LogHeader} - ReturnToMainMenu - Found in location:{location} ({alpha})");
        }

        private async Task CheckWonderPickOnceAsync()
        {
            await logger.Log($"{LogHeader} - CheckWonderPickOnceAsync");
            await OpenWonderPickMenu();

            (bool found, double alpha, Point location) = await WaitFor(ImageProcessing.SearchBonusWonderPick, () => { }, 60, 1_000);
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
        }

        public async Task CheckWonderPickPeriodically()
        {
            await StartProgram(StatusEnum.CheckWonderPickPeriodically, "CheckWonderPickPeriodically");
            while (true)
            {
                try
                {
                    await ConnectToADBInstance();
                    await CheckWonderPickOnceAsync();
                    await DisconnectFromADBInstance();
                    await Task.Delay(10 * 60 * 1_000, cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    if (exception is not TaskCanceledException && exception is not OperationCanceledException)
                    {
                        await logger.Log($"<@282197676982927375> An exception has been raised:{exception}");
                    }
                }
            }
        }

        public async Task CheckWonderPickOnce()
        {
            await StartProgram(StatusEnum.CheckWonderPickOnce, "CheckWonderPickOnce");
            await ConnectToADBInstance();
            await CheckWonderPickOnceAsync();
            await DisconnectFromADBInstance();
            await StopProgram();
        }
    }
}
