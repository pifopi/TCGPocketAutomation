using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Models;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation.TCGPocketAutomation
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

        protected AdbClient adbClient = new();
        protected DeviceData deviceData = new();

        private CancellationTokenSource programCts = new();

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
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
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            programCts.Cancel();
            programCts = new CancellationTokenSource();
            Status = StatusEnum.NotRunning;
        }

        protected abstract Task ConnectToADBInstanceAsync(CancellationTokenSource cts);

        protected abstract Task DisconnectFromADBInstanceAsync();

        protected async Task WaitForTileScreenAsync(TimeSpan timeout, CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            cts.CancelAfter(timeout);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchTitleScreenResult = ImageProcessing.SearchTitleScreen(image);
                if (searchTitleScreenResult.HasValue)
                {
                    (double alpha, Point location) = searchTitleScreenResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }

        protected async Task GoPastTileScreenAsync(TimeSpan timeout, CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            cts.CancelAfter(timeout);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchPastTitleScreenResult = ImageProcessing.SearchWhiteScreen(image);
                if (searchPastTitleScreenResult.HasValue)
                {
                    (double alpha, Point location) = searchPastTitleScreenResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }

                var searchTitleScreenResult = ImageProcessing.SearchTitleScreen(image);
                if (searchTitleScreenResult.HasValue)
                {
                    (double alpha, Point location) = searchTitleScreenResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location);
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }

        private async Task OpenWonderPickMenuAsync(TimeSpan timeout, CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            cts.CancelAfter(timeout);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchWonderPickMenuResult = ImageProcessing.SearchWonderPickMenu(image);
                if (searchWonderPickMenuResult.HasValue)
                {
                    (double alpha, Point location) = searchWonderPickMenuResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }

                var searchWonderPickButtonResult = ImageProcessing.SearchWonderPickButton(image);
                if (searchWonderPickButtonResult.HasValue)
                {
                    (double alpha, Point location) = searchWonderPickButtonResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location);
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }

        private async Task ClickOKAsync(TimeSpan timeout, CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            cts.CancelAfter(timeout);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchPastOkResult = ImageProcessing.SearchWhiteScreen(image);
                if (searchPastOkResult.HasValue)
                {
                    (double alpha, Point location) = searchPastOkResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }

                var searchOkResult = ImageProcessing.SearchOK(image);
                if (searchOkResult.HasValue)
                {
                    (double alpha, Point location) = searchOkResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location);
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), cts.Token);
            }
        }

        private async Task ClickTopRightCardAsync(CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
            var searchCardResult = ImageProcessing.SearchCard(image);
            if (searchCardResult.HasValue)
            {
                (double alpha, Point location) = searchCardResult.Value;
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                await adbClient.ClickAsync(deviceData, location);
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            }
        }

        protected async Task ReturnToMainMenuAsync(TimeSpan timeout, CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            cts.CancelAfter(timeout);
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var mainMenuResult = ImageProcessing.SearchWonderPickButton(image);
                if (mainMenuResult.HasValue)
                {
                    (double alpha, Point location) = mainMenuResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
                    break;
                }

                var registernewCardResult = ImageProcessing.SearchRegisterNewCard(image);
                if (registernewCardResult.HasValue)
                {
                    (double alpha, Point location) = registernewCardResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location);
                    await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }

                Logger.Log(Logger.LogLevel.Info, LogHeader, "Going back");
                await adbClient.ClickBackButtonAsync(deviceData);
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            }
        }

        private async Task CheckWonderPickOnceAsync(CancellationTokenSource cts)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await OpenWonderPickMenuAsync(TimeSpan.FromSeconds(30), cts);

            await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
            var bonusWonderPickResult = ImageProcessing.SearchBonusWonderPick(image);
            if (bonusWonderPickResult.HasValue)
            {
                (double alpha, Point location) = bonusWonderPickResult.Value;
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found bonus wonder pick in location:{location} (alpha:{alpha})");
                await adbClient.ClickAsync(deviceData, location, cts.Token);

                await ClickOKAsync(TimeSpan.FromSeconds(30), cts);
                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

                await ClickTopRightCardAsync(cts);
                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            }
            await ReturnToMainMenuAsync(TimeSpan.FromMinutes(2), cts);
        }

        public async Task StartCheckWonderPickPeriodicallyAsync()
        {
            StartProgram(StatusEnum.CheckWonderPickPeriodically, "CheckWonderPickPeriodically");
            while (!programCts.IsCancellationRequested)
            {
                try
                {
                    var childCts = CancellationTokenSource.CreateLinkedTokenSource(programCts.Token);
                    await ConnectToADBInstanceAsync(childCts);
                    await CheckWonderPickOnceAsync(childCts);
                    await DisconnectFromADBInstanceAsync();
                    await Task.Delay(TimeSpan.FromMinutes(15), childCts.Token);
                }
                catch (Exception exception)
                {
                    Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
                    await DisconnectFromADBInstanceAsync();
                }
            }
        }

        public async Task StartCheckWonderPickOnceAsync()
        {
            StartProgram(StatusEnum.CheckWonderPickOnce, "CheckWonderPickOnce");
            try
            {
                var childCts = CancellationTokenSource.CreateLinkedTokenSource(programCts.Token);
                await ConnectToADBInstanceAsync(childCts);
                await CheckWonderPickOnceAsync(childCts);
                await DisconnectFromADBInstanceAsync();
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
                await DisconnectFromADBInstanceAsync();
            }
            finally
            {
                StopProgram();
            }
        }
    }
}
