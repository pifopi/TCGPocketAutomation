using System.Collections.ObjectModel;
using System.Windows;

namespace TCGPocketAutomation
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
            Logger.Log(Logger.LogLevel.Info, "", "-------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info, "", "Starting the program");
            InitializeComponent();

            Utils.StartADBServer();

            ADBSettings settings = SettingsManager.LoadSettings();

            ADBInstanceBluestacks.SetMaxParallelInstance(settings.BluestacksMaxParallelInstance);
            ADBInstancesBluestacks = new ObservableCollection<ADBInstanceBluestacks>(
                settings.BluestacksInstances.Select(s => new ADBInstanceBluestacks
                {
                    Name = s.Name,
                    BluestacksName = s.BluestacksName,
                    IP = s.IP,
                    Port = s.Port
                }));

            ADBInstanceLDPlayer.SetMaxParallelInstance(settings.LDPlayerMaxParallelInstance);
            ADBInstancesLDPlayer = new ObservableCollection<ADBInstanceLDPlayer>(
                settings.LDPlayerInstances.Select(s => new ADBInstanceLDPlayer
                {
                    Name = s.Name,
                    ADBName = s.ADBName
                }));

            ADBInstancesRealPhoneViaIP = new ObservableCollection<ADBInstanceRealPhoneViaIP>(
                settings.RealPhoneInstances.Select(s => new ADBInstanceRealPhoneViaIP
                {
                    Name = s.Name,
                    IP = s.IP,
                    Port = s.Port
                }));

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
            await AsADBInstance(sender).StartCheckWonderPickPeriodicallyAsync();
        }

        public async void CheckWonderPickOnce(object sender, RoutedEventArgs e)
        {
            await AsADBInstance(sender).StartCheckWonderPickOnceAsync();
        }
    }
}