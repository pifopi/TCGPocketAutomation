using NLog.Config;
using NLog;
using System.Collections.ObjectModel;
using System.Windows;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<ADBInstanceBluestacks> ADBInstancesBluestacks { get; set; }
        public ObservableCollection<ADBInstanceLDPlayer> ADBInstancesLDPlayer { get; set; }
        public ObservableCollection<ADBInstanceRealPhoneViaIP> ADBInstancesRealPhoneViaIP { get; set; }
        public MainWindow()
        {
            InitializeComponent();

            ADBSettings settings = SettingsManager.LoadSettings();

            LogManager.Configuration = new XmlLoggingConfiguration(@"config\NLog.config");

            Logger.Log(Logger.LogLevel.Info, "", "-------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info, "", "<@282197676982927375> Starting the program");

            ADBInstanceBluestacks.SetMaxParallelInstance(settings.BluestacksMaxParallelInstance);
            ADBInstancesBluestacks = [];
            foreach (var s in settings.BluestacksInstances)
            {
                ADBInstancesBluestacks.Add(new ADBInstanceBluestacks
                {
                    Name = s.Name,
                    BluestacksName = s.BluestacksName,
                    IP = s.IP,
                    Port = s.Port
                });
            }

            ADBInstancesLDPlayer = [];
            ADBInstanceLDPlayer.SetMaxParallelInstance(settings.LDPlayerMaxParallelInstance);
            foreach (var s in settings.LDPlayerInstances)
            {
                ADBInstancesLDPlayer.Add(new ADBInstanceLDPlayer
                {
                    Name = s.Name,
                    ADBName = s.ADBName
                });
            }

            ADBInstancesRealPhoneViaIP = [];
            foreach (var s in settings.RealPhoneInstances)
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

        public void Stop(object sender, RoutedEventArgs e)
        {
            AsADBInstance(sender).StopProgram();
        }

        public async void CheckWonderPickPeriodically(object sender, RoutedEventArgs e)
        {
            await AsADBInstance(sender).StartCheckWonderPickPeriodicallyAsync().ConfigureAwait(false);
        }

        public async void CheckWonderPickOnce(object sender, RoutedEventArgs e)
        {
            await AsADBInstance(sender).StartCheckWonderPickOnceAsync().ConfigureAwait(false);
        }
    }
}