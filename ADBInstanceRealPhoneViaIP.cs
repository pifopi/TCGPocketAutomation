using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
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

        protected override async Task ConnectToADBInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            string resultConnect = adbClient.Connect(IP, Port);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            needToDisconnect = true;
            DeviceData? device = await Utils.GetDeviceDataFromAsync(adbClient, $"{IP}:{Port}");
            deviceData = device.Value;
            await Task.Delay(TimeSpan.FromSeconds(10), program.Token);
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!needToDisconnect)
            {
                return Task.CompletedTask;
            }
            needToDisconnect = false;

            deviceData = new DeviceData();
            adbClient.Disconnect(IP, Port);

            return Task.CompletedTask;
        }
    }
}
