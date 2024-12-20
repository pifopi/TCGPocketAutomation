﻿using NLog;
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
        public ObservableCollection<ADBInstanceBluestacks> ADBInstancesBluestacks { get; set; } = [];
        public ObservableCollection<ADBInstanceLDPlayer> ADBInstancesLDPlayer { get; set; } = [];
        public ObservableCollection<ADBInstanceRealPhoneViaIP> ADBInstancesRealPhoneViaIP { get; set; } = [];
        public MainWindow()
        {
            InitializeComponent();

            SettingsManager.LoadSettings(@"config\settings.json");

            LogManager.Configuration = new XmlLoggingConfiguration(@"config\NLog.config");

            Logger.Log(Logger.LogLevel.Info, "", "-------------------------------------------------------------------------");
            Logger.Log(Logger.LogLevel.Info, "", $"<@{SettingsManager.Settings.DiscordUserId}> Starting the program");

            ADBInstanceBluestacks.SetMaxParallelInstance(SettingsManager.Settings.BluestacksMaxParallelInstance);
            foreach (var s in SettingsManager.Settings.BluestacksInstances)
            {
                ADBInstancesBluestacks.Add(new ADBInstanceBluestacks
                {
                    Name = s.Name,
                    BluestacksName = s.BluestacksName,
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
                    ADBName = s.ADBName
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

        public void Stop(object sender, RoutedEventArgs e)
        {
            AsADBInstance(sender).StopProgram();
        }

        public async void CheckWonderPickPeriodically(object sender, RoutedEventArgs e)
        {
            await Task.Run(AsADBInstance(sender).StartCheckWonderPickPeriodicallyAsync);
        }

        public async void CheckWonderPickOnce(object sender, RoutedEventArgs e)
        {
            await Task.Run(AsADBInstance(sender).StartCheckWonderPickOnceAsync);
        }
    }
}