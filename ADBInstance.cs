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

        protected CancellationTokenSource program = new CancellationTokenSource();

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
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                program.Cancel();
                program = new CancellationTokenSource();
            }
            Status = StatusEnum.NotRunning;
        }

        protected abstract Task ConnectToADBInstanceAsync();

        protected abstract Task DisconnectFromADBInstanceAsync();

        protected async Task GoPastTileScreenAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                while (!program.IsCancellationRequested)
                {
                    OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                    var searchPastTitleScreenResult = ImageProcessing.SearchWhiteScreen(image);
                    if (searchPastTitleScreenResult.HasValue)
                    {
                        (double alpha, Point location) = searchPastTitleScreenResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                        return;
                    }

                    var searchTitleScreenResult = ImageProcessing.SearchTitleScreen(image);
                    if (searchTitleScreenResult.HasValue)
                    {
                        (double alpha, Point location) = searchTitleScreenResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                        await adbClient.ClickAsync(deviceData, location);
                        await Task.Delay(TimeSpan.FromSeconds(1), program.Token);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100), program.Token);
                }
            }
        }

        private async Task OpenWonderPickMenuAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                while (!program.IsCancellationRequested)
                {
                    OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                    var searchWonderPickMenuResult = ImageProcessing.SearchWonderPickMenu(image);
                    if (searchWonderPickMenuResult.HasValue)
                    {
                        (double alpha, Point location) = searchWonderPickMenuResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                        return;
                    }

                    var searchWonderPickButtonResult = ImageProcessing.SearchWonderPickButton(image);
                    if (searchWonderPickButtonResult.HasValue)
                    {
                        (double alpha, Point location) = searchWonderPickButtonResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                        await adbClient.ClickAsync(deviceData, location);
                        await Task.Delay(TimeSpan.FromSeconds(1), program.Token);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100), program.Token);
                }
            }
        }

        private async Task ClickOKAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                while (!program.IsCancellationRequested)
                {
                    OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                    var searchPastOkResult = ImageProcessing.SearchWhiteScreen(image);
                    if (searchPastOkResult.HasValue)
                    {
                        (double alpha, Point location) = searchPastOkResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                        return;
                    }

                    var searchOkResult = ImageProcessing.SearchOK(image);
                    if (searchOkResult.HasValue)
                    {
                        (double alpha, Point location) = searchOkResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                        await adbClient.ClickAsync(deviceData, location);
                        await Task.Delay(TimeSpan.FromSeconds(1), program.Token);
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100), program.Token);
                }
            }
        }

        private async Task ClickTopRightCardAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchCardResult = ImageProcessing.SearchCard(image);
                if (searchCardResult.HasValue)
                {
                    (double alpha, Point location) = searchCardResult.Value;
                    Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location);
                    await Task.Delay(TimeSpan.FromSeconds(1), program.Token);
                }
            }
        }

        private async Task ReturnToMainMenuAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                while (true)
                {
                    OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                    var mainMenuResult = ImageProcessing.SearchWonderPickButton(image);
                    if (mainMenuResult.HasValue)
                    {
                        (double alpha, Point location) = mainMenuResult.Value;
                        Logger.Log(Logger.LogLevel.Debug, LogHeader, $"Found wonder pick in location:{location} ({alpha})");
                        await Task.Delay(TimeSpan.FromSeconds(10), program.Token);
                        break;
                    }

                    Logger.Log(Logger.LogLevel.Debug, LogHeader, "Going back");
                    await adbClient.ClickBackButtonAsync(deviceData);
                    await Task.Delay(TimeSpan.FromSeconds(1), program.Token);
                }
            }
        }

        private async Task CheckWonderPickOnceAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                await OpenWonderPickMenuAsync();

                await Task.Delay(TimeSpan.FromSeconds(30), program.Token);
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var bonusWonderPickResult = ImageProcessing.SearchWonderPickButton(image);
                if (bonusWonderPickResult.HasValue)
                {
                    (double alpha, Point location) = bonusWonderPickResult.Value;
                    await adbClient.ClickAsync(deviceData, location, program.Token);

                    await ClickOKAsync();
                    await Task.Delay(TimeSpan.FromSeconds(30), program.Token);

                    await ClickTopRightCardAsync();
                    await Task.Delay(TimeSpan.FromSeconds(30), program.Token);
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
                    if (Status == StatusEnum.NotRunning)
                    {
                        return;
                    }
                    await ConnectToADBInstanceAsync();
                    await CheckWonderPickOnceAsync();
                    await DisconnectFromADBInstanceAsync();
                    await Task.Delay(TimeSpan.FromMinutes(15), program.Token);
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
