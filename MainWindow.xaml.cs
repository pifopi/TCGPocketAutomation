using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;
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
            InitializeComponent();

            if (!AdbServer.Instance.GetStatus().IsRunning)
            {
                AdbServer server = new AdbServer();
                StartServerResult resultStartServer = server.StartServer("adb", false);
                if (resultStartServer != StartServerResult.Started)
                {
                    throw new Exception("Can't start adb server, make sure you add adb.exe to your PATH");
                }
            }

            ADBInstancesBluestacks = new ObservableCollection<ADBInstanceBluestacks>
            {
                new ADBInstanceBluestacks { Name = "Compte 01", BluestacksName = "Pie64_1", Port = 5565 },
                new ADBInstanceBluestacks { Name = "Compte 02 - Mewtwo", BluestacksName = "Pie64_2", Port = 5575 },
                new ADBInstanceBluestacks { Name = "Compte 03 - Mewtwo", BluestacksName = "Pie64_3", Port = 5585 },
                new ADBInstanceBluestacks { Name = "Compte 04 - Mewtwo", BluestacksName = "Pie64_4", Port = 5595 },
                new ADBInstanceBluestacks { Name = "Compte 05 - Pikachu", BluestacksName = "Pie64_5", Port = 5605 },
                new ADBInstanceBluestacks { Name = "Compte 06 - Pikachu", BluestacksName = "Pie64_6", Port = 5615 },
                new ADBInstanceBluestacks { Name = "Compte 07 - Pikachu", BluestacksName = "Pie64_7", Port = 5625 },
                new ADBInstanceBluestacks { Name = "Compte 08 - Dracaufeu", BluestacksName = "Pie64_8", Port = 5635 },
                new ADBInstanceBluestacks { Name = "Compte 09 - Dracaufeu", BluestacksName = "Pie64_9", Port = 5645 },
                new ADBInstanceBluestacks { Name = "Compte 10 - Dracaufeu", BluestacksName = "Pie64_10", Port = 5655 }
            };

            ADBInstancesLDPlayer = new ObservableCollection<ADBInstanceLDPlayer>
            {
                new ADBInstanceLDPlayer { Name = "Compte 02 - Mewtwo", ADBName = "emulator-5554" },
                new ADBInstanceLDPlayer { Name = "Compte 03 - Mewtwo", ADBName = "emulator-5556" },
                new ADBInstanceLDPlayer { Name = "Compte 04 - Mewtwo", ADBName = "emulator-5558" },
                new ADBInstanceLDPlayer { Name = "Compte 05 - Pikachu", ADBName = "emulator-5560" },
                new ADBInstanceLDPlayer { Name = "Compte 06 - Pikachu", ADBName = "emulator-5562" },
                new ADBInstanceLDPlayer { Name = "Compte 07 - Pikachu", ADBName = "emulator-5564" },
                new ADBInstanceLDPlayer { Name = "Compte 08 - Dracaufeu", ADBName = "emulator-5566" },
                new ADBInstanceLDPlayer { Name = "Compte 09 - Dracaufeu", ADBName = "emulator-5568" },
                new ADBInstanceLDPlayer { Name = "Compte 10 - Dracaufeu", ADBName = "emulator-5570" }
            };

            ADBInstancesRealPhoneViaIP = new ObservableCollection<ADBInstanceRealPhoneViaIP>
            {
                new ADBInstanceRealPhoneViaIP { Name = "Compte 01", IP = "192.168.1.63"},
            };

            DataContext = this;
        }

        static private ADBInstance AsInstance(object sender)
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
            AsInstance(sender).StopProgram();
        }

        public void CheckWonderPickPeriodically(object sender, RoutedEventArgs e)
        {
            AsInstance(sender).CheckWonderPickPeriodically();
        }

        public void CheckWonderPickOnce(object sender, RoutedEventArgs e)
        {
            AsInstance(sender).CheckWonderPickOnce();
        }
    }
}