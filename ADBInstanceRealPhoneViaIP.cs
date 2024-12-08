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

        protected override Task ConnectToADBInstanceAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                string resultConnect = adbClient.Connect(IP, Port);
                if (resultConnect != $"connected to {IP}:{Port}" &&
                    resultConnect != $"already connected to {IP}:{Port}")
                {
                    throw new Exception(resultConnect);
                }
                needToDisconnect = true;
                deviceData = Utils.GetDeviceDataFrom(adbClient, $"{IP}:{Port}");
            }
            return Task.CompletedTask;
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            if (!needToDisconnect)
            {
                return Task.CompletedTask;
            }
            needToDisconnect = false;

            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                deviceData = new DeviceData();
                adbClient.Disconnect(IP, Port);
            }
            return Task.CompletedTask;
        }
    }
}
