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
        public ObservableCollection<Instance> Instances { get; set; }

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

            //Instances = new ObservableCollection<Instance>();
            Instances = new ObservableCollection<Instance>
            {
                new Instance { Name = "Compte 01", UseLDPlayer = false, IP = "192.168.1.63"},
                new Instance { Name = "Compte 02 - Mewtwo", Port = 5554 },
                new Instance { Name = "Compte 03 - Mewtwo", Port = 5556 },
                new Instance { Name = "Compte 04 - Mewtwo", Port = 5558 },
                new Instance { Name = "Compte 05 - Pikachu", Port = 5560 },
                new Instance { Name = "Compte 06 - Pikachu", Port = 5562 },
                new Instance { Name = "Compte 07 - Pikachu", Port = 5564 },
                new Instance { Name = "Compte 08 - Dracaufeu", Port = 5566 },
                new Instance { Name = "Compte 09 - Dracaufeu", Port = 5568 },
                new Instance { Name = "Compte 10 - Dracaufeu", Port = 5570 }
            };
            DataContext = this;
        }

        static private Instance AsInstance(object sender)
        {
            if (sender == null)
            {
                throw new Exception("Null sender");
            }
            FrameworkElement frameworkElement = (FrameworkElement)sender;
            return (Instance)frameworkElement.DataContext;
        }

        public void Stop(object sender, RoutedEventArgs e)
        {
            AsInstance(sender).Stop();
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