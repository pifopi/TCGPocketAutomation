using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.DeviceCommands;
using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public abstract class ADBInstance : INotifyPropertyChanged
    {
        public enum Program
        {
            CheckWonderPickPeriodically,
            CheckWonderPickOnce
        }

        private string _name = "New instance";
        private bool _isRunning = false;
        private Program _selectedProgram = Program.CheckWonderPickPeriodically;

        protected AdbClient adbClient = new();
        protected DeviceData deviceData = new();

        private CancellationTokenSource programCts = new();

        private SemaphoreSlim instanceSemaphore = new(1, 1);

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotRunning));
            }
        }

        public bool IsNotRunning
        {
            get => !IsRunning;
        }

        public static ObservableCollection<Program> ProgramList { get; set; } = new ObservableCollection<Program>(Enum.GetValues(typeof(Program)).Cast<Program>());

        public Program SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); }
        }

        protected abstract string LogHeader { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StartProgram()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader, $"StartProgram {SelectedProgram}");
            IsRunning = true;
            programCts = new CancellationTokenSource();
            switch (SelectedProgram)
            {
                case Program.CheckWonderPickPeriodically:
                    Task.Run(StartCheckWonderPickPeriodicallyAsync);
                    break;
                case Program.CheckWonderPickOnce:
                    Task.Run(StartCheckWonderPickOnceAsync);
                    break;
            }
        }

        public void StopProgram()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader, $"StopProgram {SelectedProgram}");
            programCts.Cancel();
            programCts.Dispose();
            IsRunning = false;
        }

        protected virtual async Task StartInstanceAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await instanceSemaphore.WaitAsync(token);
        }

        protected virtual Task StopInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            instanceSemaphore.Release();
            return Task.CompletedTask;
        }

        protected abstract Task ConnectToADBInstanceAsync(CancellationToken token);

        protected abstract Task DisconnectFromADBInstanceAsync();

        private async Task StartApplicationAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            // StartAppAsync does not work properly, so do it by hand
            Utils.ExecuteCmd($"adb -s {deviceData.Serial} shell am start -n jp.pokemon.pokemontcgp/com.unity3d.player.UnityPlayerActivity");
            await WaitForTitleScreenAsync(TimeSpan.FromMinutes(1), token);
        }

        private async Task StopApplicationAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await adbClient.StopAppAsync(deviceData, "jp.pokemon.pokemontcgp");
        }

        protected async Task WaitForTitleScreenAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchTitleScreenResult = ImageProcessing.SearchTitleScreen(image);
                if (searchTitleScreenResult.HasValue)
                {
                    (double alpha, Point location) = searchTitleScreenResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        protected async Task GoPastTitleScreenAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

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
                    await adbClient.ClickAsync(deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task OpenWonderPickMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

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
                    await adbClient.ClickAsync(deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task ClickOKAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var searchPastOkResult = ImageProcessing.SearchWhiteScreen(image);
                if (searchPastOkResult.HasValue)
                {
                    (double alpha, Point location) = searchPastOkResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    return;
                }

                var searchOkResult = ImageProcessing.SearchOKButton(image);
                if (searchOkResult.HasValue)
                {
                    (double alpha, Point location) = searchOkResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task ClickTopRightCardAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
            var searchCardResult = ImageProcessing.SearchCard(image);
            if (searchCardResult.HasValue)
            {
                (double alpha, Point location) = searchCardResult.Value;
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                await adbClient.ClickAsync(deviceData, location, token);
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }

        protected async Task ReturnToMainMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
                var mainMenuResult = ImageProcessing.SearchWonderPickButton(image);
                if (mainMenuResult.HasValue)
                {
                    (double alpha, Point location) = mainMenuResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found in location:{location} (alpha:{alpha})");
                    await Task.Delay(TimeSpan.FromSeconds(10), linkedCts.Token);
                    break;
                }

                var registernewCardResult = ImageProcessing.SearchRegisterNewCardButton(image);
                if (registernewCardResult.HasValue)
                {
                    (double alpha, Point location) = registernewCardResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await adbClient.ClickAsync(deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
                }

                Logger.Log(Logger.LogLevel.Info, LogHeader, "Going back");
                await adbClient.ClickBackButtonAsync(deviceData, linkedCts.Token);
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
            }
        }

        private async Task CheckWonderPickOnceAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await GoPastTitleScreenAsync(TimeSpan.FromSeconds(30), token);
            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(30), token);
            await OpenWonderPickMenuAsync(TimeSpan.FromSeconds(30), token);

            await Task.Delay(TimeSpan.FromSeconds(30), token);
            OpenCvSharp.Mat image = await Utils.GetImageAsync(adbClient, deviceData);
            var bonusWonderPickResult = ImageProcessing.SearchBonusWonderPick(image);
            if (bonusWonderPickResult.HasValue)
            {
                (double alpha, Point location) = bonusWonderPickResult.Value;
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Found bonus wonder pick in location:{location} (alpha:{alpha})");
                await adbClient.ClickAsync(deviceData, location, token);

                await ClickOKAsync(TimeSpan.FromSeconds(30), token);
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                await ClickTopRightCardAsync(token);
                await Task.Delay(TimeSpan.FromSeconds(30), token);
            }
            await ReturnToMainMenuAsync(TimeSpan.FromMinutes(2), token);
        }

        public async Task StartCheckWonderPickPeriodicallyAsync()
        {
            while (!programCts.IsCancellationRequested)
            {
                bool wait = true;
                try
                {
                    await StartInstanceAsync(programCts.Token);
                    await ConnectToADBInstanceAsync(programCts.Token);
                    await StartApplicationAsync(programCts.Token);
                    await CheckWonderPickOnceAsync(programCts.Token);
                }
                catch (Exception exception)
                {
                    Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
                    wait = false;
                }
                finally
                {
                    await StopApplicationAsync();
                    await DisconnectFromADBInstanceAsync();
                    await StopInstanceAsync();
                }

                if (wait)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(15), programCts.Token);
                    }
                    catch (Exception exception)
                    {
                        Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
                    }
                }
            }
        }

        public async Task StartCheckWonderPickOnceAsync()
        {
            try
            {
                await StartInstanceAsync(programCts.Token);
                await ConnectToADBInstanceAsync(programCts.Token);
                await StartApplicationAsync(programCts.Token);
                await CheckWonderPickOnceAsync(programCts.Token);
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.Settings.DiscordUserId}> An exception has been raised:{exception}");
            }
            finally
            {
                await StopApplicationAsync();
                await DisconnectFromADBInstanceAsync();
                await StopInstanceAsync();
            }
            StopProgram();
        }
    }

    public abstract class ADBInstanceViaIP : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;

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

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            string resultConnect = await adbClient.ConnectAsync(IP, Port, token);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            deviceData = await Utils.GetDeviceDataFromAsync(adbClient, $"{IP}:{Port}", TimeSpan.FromMinutes(1), token);
            await Task.Delay(TimeSpan.FromSeconds(30), token);
        }

        protected override async Task DisconnectFromADBInstanceAsync()
        {
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
            }
        }
    }

    public abstract class ADBInstanceViaSerial : ADBInstance
    {
        private string _serialName = "emulator-5555";

        public string SerialName
        {
            get => _serialName;
            set { _serialName = value; OnPropertyChanged(); }
        }

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            deviceData = await Utils.GetDeviceDataFromAsync(adbClient, SerialName, TimeSpan.FromMinutes(1), token);
            await Task.Delay(TimeSpan.FromSeconds(30), token);
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            deviceData = new DeviceData();
            return Task.CompletedTask;
        }
    }
}
