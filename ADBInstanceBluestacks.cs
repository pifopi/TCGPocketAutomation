using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceBluestacks : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;
        private string _bluestacksName = "Pie64";

        public string BluestacksName
        {
            get => _bluestacksName;
            set { _bluestacksName = value; OnPropertyChanged(); }
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

        protected override string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{IP}:{Port}\t{BluestacksName}";
        }

        protected override async Task ConnectToADBInstance()
        {
            await logger.Log($"{LogHeader} - ConnectToADBInstance Begin");
            Utils.ExecuteCmd($"HD-Player.exe --instance {BluestacksName} --cmd launchAppWithBsx --package \"jp.pokemon.pokemontcgp\" --source desktop_shortcut");
            await Task.Delay(60_000, cancellationTokenSource.Token);
            string resultConnect = adbClient.Connect(IP, Port);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            deviceData = Utils.GetDeviceDataFrom(adbClient, $"{IP}:{Port}");
            await GoPastTileScreen();
            await logger.Log($"{LogHeader} - ConnectToADBInstance End");
        }

        protected override async Task DisconnectFromADBInstance()
        {
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance Begin");
            deviceData = new DeviceData();
            adbClient.Disconnect(IP, Port);
            Utils.ExecuteCmd($"taskkill /fi \"WINDOWTITLE eq {Name}\" /IM \"HD-Player.exe\" /F");
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance End");
        }
    }
}
