using AdvancedSharpAdbClient.Models;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceLDPlayer : ADBInstanceViaSerial
    {
        private static SemaphoreSlim emulatorSemaphore = new(0, 1);
        private bool hasTakenEmulatorSemaphore = false;

        protected override string LogHeader
        {
            get => $"{Name}\t{SerialName}";
        }

        private string LDPlayerName
        {
            get => $"\"{Name}\"";
        }

        public static void SetMaxParallelInstance(int maxParallelInstance)
        {
            emulatorSemaphore = new SemaphoreSlim(maxParallelInstance, maxParallelInstance);
        }

        protected override async Task StartInstanceAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await base.StartInstanceAsync(token);
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Waiting for a semaphore ({emulatorSemaphore.CurrentCount} available)");
            await emulatorSemaphore.WaitAsync(token);
            hasTakenEmulatorSemaphore = true;
            token.ThrowIfCancellationRequested();
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Got a semaphore ({emulatorSemaphore.CurrentCount} available)");

            Utils.ExecuteCmd($"ldconsole.exe launch --name {LDPlayerName}");
            await Task.Delay(TimeSpan.FromSeconds(30), token);
        }

        protected override async Task StopInstanceAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            if (!hasTakenEmulatorSemaphore)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"No semaphore to release ({emulatorSemaphore.CurrentCount} available)");
                return;
            }

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"One semaphore to release ({emulatorSemaphore.CurrentCount} available)");
            hasTakenEmulatorSemaphore = false;

            deviceData = new DeviceData();
            Utils.ExecuteCmd($"ldconsole.exe quit --name {LDPlayerName}");
            await Task.Delay(TimeSpan.FromSeconds(10));

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Releasing a semaphore ({emulatorSemaphore.CurrentCount} available)");
            emulatorSemaphore.Release();
            await base.StopInstanceAsync();
        }
    }
}
