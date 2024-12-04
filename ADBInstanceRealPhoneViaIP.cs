using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceRealPhoneViaIP : ADBInstance
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

        protected override string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{IP}:{Port}";
        }

        protected override async Task ConnectToADBInstance()
        {
            await logger.Log($"{LogHeader} - ConnectToADBInstance Begin");
            string resultConnect = adbClient.Connect(IP, Port);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            deviceData = Utils.GetDeviceDataFrom(adbClient, $"{IP}:{Port}");
            await logger.Log($"{LogHeader} - ConnectToADBInstance End");
        }

        protected override async Task DisconnectFromADBInstance()
        {
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance Begin");
            deviceData = new DeviceData();
            adbClient.Disconnect(IP, Port);
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance End");
        }
    }
}
