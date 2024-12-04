using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        private string LDPlayerName
        {
            get => $"\"{Name}\"";
        }

        public string ADBName
        {
            get => _adbName;
            set { _adbName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{ADBName}";
        }

        protected override async Task ConnectToADBInstance()
        {
            await logger.Log($"{LogHeader} - ConnectToADBInstance Begin");
            Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
            await Task.Delay(60_000, cancellationTokenSource.Token);
            deviceData = Utils.GetDeviceDataFrom(adbClient, $"{ADBName}");
            await GoPastTileScreen();
            await logger.Log($"{LogHeader} - ConnectToADBInstance End");
        }

        protected override async Task DisconnectFromADBInstance()
        {
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance Begin");
            deviceData = new DeviceData();
            Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            await logger.Log($"{LogHeader} - DisconnectFromADBInstance End");
        }
    }
}
