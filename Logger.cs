using Discord;
using Discord.WebSocket;
using System.IO;

namespace TCGPocketAutomation
{
    public class Logger
    {
        private DiscordSocketClient client = new DiscordSocketClient();
        private List<string> waitingMessages = new List<string>();
        private StreamWriter streamWriter = new StreamWriter("log.txt");

        public async Task Log(string message)
        {
            await LogOnFile(message);
            await LogOnDiscord(message);
        }

        private async Task LogOnFile(string message)
        {
            streamWriter.WriteLine(message);
            await streamWriter.FlushAsync();
        }

        private async Task LogOnDiscord(string message)
        {
            switch (client.LoginState)
            {
                case LoginState.LoggingIn:
                case LoginState.LoggingOut:
                    waitingMessages.Add(message);
                    break;
                case LoginState.LoggedOut:
                    waitingMessages.Add(message);
                    await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
                    await client.StartAsync();
                    client.Ready += SendWaitingMessages;
                    break;
                case LoginState.LoggedIn:
                    IMessageChannel channel = await client.GetChannelAsync(1294233612727750721) as IMessageChannel;
                    await channel.SendMessageAsync(message);
                    break;
            }
        }

        private async Task SendWaitingMessages()
        {
            foreach (string waitingMessage in waitingMessages)
            {
                await LogOnDiscord(waitingMessage);
            }
            waitingMessages.Clear();
        }
    }
}
