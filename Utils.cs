using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using System.Diagnostics;

namespace TCGPocketAutomation
{
    public class Utils
    {
        public static void ExecuteCmd(string cmd)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C {cmd}";
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
    }
}
