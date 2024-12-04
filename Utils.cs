using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using System.Diagnostics;

namespace TCGPocketAutomation
{
    public static class Utils
    {
        public static void ExecuteCmd(string cmd)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", $"/C {cmd}");
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo = startInfo;
            process.Start();
        }

        public static DeviceData GetDeviceDataFrom(AdbClient adbClient, string key)
        {
            foreach (DeviceData device in adbClient.GetDevices())
            {
                if (device.Serial == key)
                {
                    return device;
                }
            }
            throw new Exception($"Could not find {key} in list of adb devices");
        }

        public static void StartADBServer()
        {
            if (!AdbServer.Instance.GetStatus().IsRunning)
            {
                AdbServer server = new AdbServer();
                StartServerResult resultStartServer = server.StartServer("adb", false);
                if (resultStartServer != StartServerResult.Started)
                {
                    throw new Exception("Can't start adb server, make sure you add adb.exe to your PATH");
                }
            }
        }
    }
}
