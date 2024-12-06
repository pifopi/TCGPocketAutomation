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

        private void StartProgram(StatusEnum status, string programName)
        {
            Status = status;
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"StartProgram {programName}");
        }

        public void StopProgram()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader, ""))
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
            }
            Status = StatusEnum.NotRunning;
        }

        protected abstract Task ConnectToADBInstanceAsync();

        protected abstract Task DisconnectFromADBInstanceAsync();

        private async Task<(bool, double, Point)> WaitForAsync(Func<Framebuffer, (bool, double, Point)> check, Action action, uint retry, int millisecondsDelay)
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

        protected async Task GoPastTileScreenAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                {
                    (bool found, double alpha, Point location) = await WaitForAsync(ImageProcessing.SearchTitleScreen, () => { }, 60, 1_000);
                    if (!found)
                    {
                        throw new Exception($"Could not find the title screen template ({alpha})");
                    }

                    Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} ({alpha})");
                    await Task.Delay(10_000, cancellationTokenSource.Token);
                    Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location}");
                    await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);

                    await ReturnToMainMenuAsync();
                }
            }
        }

        private async Task OpenWonderPickMenuAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                (bool found, double alpha, Point location) = await WaitForAsync(ImageProcessing.SearchWonderPick, () => { }, 60, 1_000);
                if (!found)
                {
                    throw new Exception($"Could not find the wonder pick template ({alpha})");
                }

                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} ({alpha})");
                await Task.Delay(10_000, cancellationTokenSource.Token);
                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location}");
                await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
            }
        }

        private async Task ClickOKAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                (bool found, double alpha, Point location) = await WaitForAsync(ImageProcessing.SearchOK, () => { }, 60, 1_000);
                if (!found)
                {
                    throw new Exception($"Could not find the OK template ({alpha})");
                }

                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} ({alpha})");
                await Task.Delay(10_000, cancellationTokenSource.Token);
                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location}");
                await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
            }
        }

        private async Task ClickTopRightCardAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                (bool found, double alpha, Point location) = await WaitForAsync(ImageProcessing.SearchCard, () => { }, 60, 1_000);
                if (!found)
                {
                    throw new Exception($"Could not find the card template ({alpha})");
                }

                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} ({alpha})");
                await Task.Delay(10_000, cancellationTokenSource.Token);
                Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location}");
                await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
            }
        }

        private async Task ReturnToMainMenuAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                (bool found, double alpha, Point _) = await WaitForAsync(ImageProcessing.SearchWonderPick, async () =>
                    {
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, "Going back");
                        await adbClient.ClickBackButtonAsync(deviceData, cancellationTokenSource.Token);
                    }, 10, 10_000);
                if (!found)
                {
                    throw new Exception($"Could not return to main menu ({alpha})");
                }
            }
        }

        private async Task CheckWonderPickOnceAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                await OpenWonderPickMenuAsync();

                (bool found, double alpha, Point location) = await WaitForAsync(ImageProcessing.SearchBonusWonderPick, () => { }, 60, 1_000);
                if (found)
                {
                    await Task.Delay(10_000, cancellationTokenSource.Token);
                    await adbClient.ClickAsync(deviceData, location, cancellationTokenSource.Token);
                    await Task.Delay(30_000, cancellationTokenSource.Token);

                    await ClickOKAsync();
                    await Task.Delay(30_000, cancellationTokenSource.Token);

                    await ClickTopRightCardAsync();
                    await Task.Delay(30_000, cancellationTokenSource.Token);
                }
                await ReturnToMainMenuAsync();
            }
        }

        public async Task StartCheckWonderPickPeriodicallyAsync()
        {
            StartProgram(StatusEnum.CheckWonderPickPeriodically, "CheckWonderPickPeriodically");
            while (true)
            {
                try
                {
                    await ConnectToADBInstanceAsync();
                    await CheckWonderPickOnceAsync();
                    await DisconnectFromADBInstanceAsync();
                    await Task.Delay(10 * 60 * 1_000, cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    if (exception is not TaskCanceledException && exception is not OperationCanceledException)
                    {
                        Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@282197676982927375> An exception has been raised:{exception}");
                    }
                    await DisconnectFromADBInstanceAsync();
                }
            }
        }

        public async Task StartCheckWonderPickOnceAsync()
        {
            StartProgram(StatusEnum.CheckWonderPickOnce, "CheckWonderPickOnce");
            try
            {
                await ConnectToADBInstanceAsync();
                await CheckWonderPickOnceAsync();
                await DisconnectFromADBInstanceAsync();
            }
            catch (Exception exception)
            {
                if (exception is not TaskCanceledException && exception is not OperationCanceledException)
                {
                    Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@282197676982927375> An exception has been raised:{exception}");
                }
                await DisconnectFromADBInstanceAsync();
            }
            finally
            {
                StopProgram();
            }
        }
    }
}
