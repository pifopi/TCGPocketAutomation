using Discord;
using Discord.WebSocket;
using NLog;
using NLog.Targets;
using System.Runtime.CompilerServices;

namespace TCGPocketAutomation.TCGPocketAutomation
{
    public static class Logger
    {
        public enum LogLevel
        {
            Warning,
            Info,
            Debug
        }

        private static readonly NLog.Logger logger = LogManager.GetLogger("");

        public static void Log(LogLevel logLevel, string header, string message, [CallerMemberName] string methodName = "")
        {
            var fullMessage = string.IsNullOrEmpty(message) ? $"{header} | {methodName}" : $"{header} | {methodName} | {message}";
            switch (logLevel)
            {
                case LogLevel.Warning:
                    logger.Warn(fullMessage);
                    break;
                case LogLevel.Info:
                    logger.Info(fullMessage);
                    break;
                case LogLevel.Debug:
                    logger.Debug(fullMessage);
                    break;
            }
        }
    }

    public sealed class LogContext : IDisposable
    {
        private readonly Logger.LogLevel logLevel;
        private readonly string header;
        private readonly string methodName;

        public LogContext(Logger.LogLevel logLevel, string header, [CallerMemberName] string methodName = "")
        {
            Logger.Log(logLevel, header, "", $"{methodName} Begin");
            this.logLevel = logLevel;
            this.header = header;
            this.methodName = methodName;
        }

        public void Dispose()
        {
            Logger.Log(logLevel, header, "", $"{methodName} End");
        }
    }

    [Target("Discord")]
    public class DiscordLogger : AsyncTaskTarget
    {
        private static readonly DiscordSocketClient client = new();
        private static readonly List<(LogEventInfo, CancellationToken)> waitingMessages = [];

        protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            switch (client.LoginState)
            {
                case LoginState.LoggingIn:
                case LoginState.LoggingOut:
                    waitingMessages.Add((logEvent, cancellationToken));
                    break;
                case LoginState.LoggedOut:
                    waitingMessages.Add((logEvent, cancellationToken));
                    await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
                    await client.StartAsync();
                    client.Ready += SendWaitingMessagesAsync;
                    break;
                case LoginState.LoggedIn:
                    if (await client.GetChannelAsync(SettingsManager.Settings.DiscordChannelId) is IMessageChannel channel)
                    {
                        await channel.SendMessageAsync(logEvent.Message);
                    }
                    break;
            }
        }

        private async Task SendWaitingMessagesAsync()
        {
            foreach ((LogEventInfo logEvent, CancellationToken cancellationToken) in waitingMessages)
            {
                await WriteAsyncTask(logEvent, cancellationToken);
            }
            waitingMessages.Clear();
        }
    }
}
