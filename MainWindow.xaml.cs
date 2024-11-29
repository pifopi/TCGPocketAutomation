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

            //Instances = new ObservableCollection<Instance>();
            Instances = new ObservableCollection<Instance>
            {
                new Instance { Name = "Compte 1", Port = 5585 },
                new Instance { Name = "Compte 2 - Mewtwo", Port = 5595 },
                new Instance { Name = "Compte 3 - Mewtwo", Port = 5605 },
                new Instance { Name = "Compte 4 - Mewtwo", Port = 5615 },
                new Instance { Name = "Compte 5 - Pikachu", Port = 5625 },
                new Instance { Name = "Compte 6 - Pikachu", Port = 5635 },
                new Instance { Name = "Compte 7 - Pikachu", Port = 5645 },
                new Instance { Name = "Compte 8 - Dracaufeu", Port = 5655 },
                new Instance { Name = "Compte 9 - Dracaufeu", Port = 5665 },
                new Instance { Name = "Compte 10 - Dracaufeu", Port = 5675 }
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

        public void Connect(object sender, RoutedEventArgs e)
        {
            AsInstance(sender).Connect();
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