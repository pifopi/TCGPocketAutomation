using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceRealPhoneViaIP : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;

        private bool needToDisconnect = false;

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

        protected override string LogHeader
        {
            get => $"{Name}\t{IP}:{Port}";
        }

        protected override async Task ConnectToADBInstanceAsync(CancellationToken token)
        {
            await base.ConnectToADBInstanceAsync(token);
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            string resultConnect = await adbClient.ConnectAsync(IP, Port, token);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            needToDisconnect = true;
            deviceData = await Utils.GetDeviceDataFromAsync(adbClient, $"{IP}:{Port}", TimeSpan.FromMinutes(1), token);
            await Task.Delay(TimeSpan.FromSeconds(10), token);
        }

        protected override async Task DisconnectFromADBInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!needToDisconnect)
            {
                return;
            }

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
                await Task.Delay(TimeSpan.FromSeconds(10));
                needToDisconnect = false;
                await base.DisconnectFromADBInstanceAsync();
            }
        }
    }
}
