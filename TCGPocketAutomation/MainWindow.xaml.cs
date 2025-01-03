using NLog;
using NLog.Config;
using System.Collections.ObjectModel;
using System.Windows;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ADBInstanceMuMu> ADBInstancesMuMu { get; set; } = [];
        public ObservableCollection<ADBInstanceBlueStacks> ADBInstancesBlueStacks { get; set; } = [];
        public ObservableCollection<ADBInstanceLDPlayer> ADBInstancesLDPlayer { get; set; } = [];
        public ObservableCollection<ADBInstanceRealPhoneViaIP> ADBInstancesRealPhoneViaIP { get; set; } = [];
        public MainWindow()
        {
            InitializeComponent();

            SettingsManager.LoadSettings(@"config\settings.json");

            LogManager.Configuration = new XmlLoggingConfiguration(@"config\NLog.config");

            Logger.Log(Logger.LogLevel.Info, "", "-------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info, "", $"<@{SettingsManager.Settings.DiscordUserId}> Starting the program");

            ADBInstanceMuMu.SetMaxParallelInstance(SettingsManager.Settings.MuMuMaxParallelInstance);
            foreach (var s in SettingsManager.Settings.MuMuInstances)
            {
                ADBInstancesMuMu.Add(new ADBInstanceMuMu
                {
                    Name = s.Name,
                    MuMuId = s.MuMuId
                });
            }

            ADBInstanceBlueStacks.SetMaxParallelInstance(SettingsManager.Settings.BlueStacksMaxParallelInstance);
            foreach (var s in SettingsManager.Settings.BlueStacksInstances)
            {
                ADBInstancesBlueStacks.Add(new ADBInstanceBlueStacks
                {
                    Name = s.Name,
                    BlueStacksName = s.BlueStacksName,
                    IP = s.IP,
                    Port = s.Port
                });
            }

            ADBInstanceLDPlayer.SetMaxParallelInstance(SettingsManager.Settings.LDPlayerMaxParallelInstance);
            foreach (var s in SettingsManager.Settings.LDPlayerInstances)
            {
                ADBInstancesLDPlayer.Add(new ADBInstanceLDPlayer
                {
                    Name = s.Name,
                    SerialName = s.SerialName
                });
            }

            foreach (var s in SettingsManager.Settings.RealPhoneInstances)
            {
                ADBInstancesRealPhoneViaIP.Add(new ADBInstanceRealPhoneViaIP
                {
                    Name = s.Name,
                    IP = s.IP,
                    Port = s.Port
                });
            }

            Utils.StartADBServer();

            DataContext = this;
        }

        static private ADBInstance AsADBInstance(object sender)
        {
            if (sender == null)
            {
                throw new Exception("Null sender");
            }
            FrameworkElement frameworkElement = (FrameworkElement)sender;
            return (ADBInstance)frameworkElement.DataContext;
        }

        public void StartProgram(object sender, RoutedEventArgs e)
        {
            AsADBInstance(sender).StartProgram();
        }

        public void StopProgram(object sender, RoutedEventArgs e)
        {
            AsADBInstance(sender).StopProgram();
        }
    }
}