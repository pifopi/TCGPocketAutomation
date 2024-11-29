using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using System.Diagnostics;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public static class Utils
    {
        public static void ExecuteCmd(string cmd)
        {
            Process process = new()
            {
                StartInfo = new("cmd.exe", $"/C {cmd}")
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            process.Start();
        }

        public static async Task<DeviceData> GetDeviceDataFromAsync(AdbClient adbClient, string key, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                foreach (DeviceData device in await adbClient.GetDevicesAsync())
                {
                    if (device.Serial == key)
                    {
                        return device;
                    }
                }
            }
        }

        public static void StartADBServer()
        {
            if (!AdbServer.Instance.GetStatus().IsRunning)
            {
                AdbServer server = new();
                StartServerResult resultStartServer = server.StartServer("adb", false);
                if (resultStartServer != StartServerResult.Started)
                {
                    throw new Exception("Can't start adb server, make sure you add adb.exe to your PATH");
                }
            }
        }

        public static async Task<OpenCvSharp.Mat> GetImageAsync(AdbClient adbClient, DeviceData deviceData)
        {
            Framebuffer framebuffer = await adbClient.GetFrameBufferAsync(deviceData);
            if (framebuffer.Header.Red.Length != 8 ||
                framebuffer.Header.Green.Length != 8 ||
                framebuffer.Header.Blue.Length != 8 ||
                framebuffer.Header.Alpha.Length != 8 ||
                framebuffer.Header.Red.Offset != 0 ||
                framebuffer.Header.Green.Offset != 8 ||
                framebuffer.Header.Blue.Offset != 16 ||
                framebuffer.Header.Alpha.Offset != 24)
            {
                throw new Exception($"The screenshot color are not encoded in the expected way (framebuffer:{framebuffer})");
            }

            int height = (int)framebuffer.Header.Height;
            int width = (int)framebuffer.Header.Width;
            OpenCvSharp.Mat mat = new(height, width, OpenCvSharp.MatType.CV_8UC3);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sourceIndex = (y * width + x) * 4;

                    if (framebuffer.Data == null)
                    {
                        throw new Exception($"The screenshot data buffer is null (framebuffer:{framebuffer})");
                    }
                    byte red = framebuffer.Data[sourceIndex + 0];
                    byte green = framebuffer.Data[sourceIndex + 1];
                    byte blue = framebuffer.Data[sourceIndex + 2];

                    mat.Set(y, x, new OpenCvSharp.Vec3b(blue, green, red));
                }
            }
            return mat;
        }
    }
}
