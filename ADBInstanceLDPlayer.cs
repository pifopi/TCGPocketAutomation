using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstance
    {
        private string _adbName = "emulator-5555";

        public string ADBName
        {
            get => _adbName;
            set { _adbName = value; OnPropertyChanged(); }
        }

        protected override string LogHeader
        {
            get => $"[{DateTime.Now}]\t{Name}\t{ADBName}";
        }

        private string LDPlayerName
        {
            get => $"\"{Name}\"";
        }

        protected override async Task ConnectToADBInstanceAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                Utils.ExecuteCmd($"ldconsole.exe launchex --name {LDPlayerName} --packagename jp.pokemon.pokemontcgp");
                await Task.Delay(60_000, cancellationTokenSource.Token);
                deviceData = Utils.GetDeviceDataFrom(adbClient, $"{ADBName}");
                await GoPastTileScreenAsync();
            }
        }

        protected override Task DisconnectFromADBInstanceAsync()
        {
            using (LogContext logContext = new LogContext(Logger.LogLevel.Info, LogHeader))
            {
                deviceData = new DeviceData();
                Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            }
            return Task.CompletedTask;
        }
    }
}
