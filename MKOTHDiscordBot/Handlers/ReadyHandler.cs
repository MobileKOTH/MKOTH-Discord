using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Handlers
{
    public class ReadyHandler : DiscordClientEventHandlerBase
    {
        public ReadyHandler(DiscordSocketClient client) : base(client)
        {
            ApplicationContext.DiscordClient = client;

            this.client = client;

            this.client.Ready += Handle;
        }

        Task Handle()
        {
            try
            {
                // Owner
                _ = client.GetApplicationInfoAsync()
                    .ContinueWith(x =>
                    {
                        ApplicationContext.BotOwner = x.Result.Owner;
                        Console.WriteLine($"Owner Id: {ApplicationContext.BotOwner.Id}");
                    });

                // Player Data
                _ = Player.Load();

                if (Program.FirstArgument == "Restarted")
                {
                    var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument));
                    if (restartChannel != null)
                    {
                        ((SocketTextChannel)restartChannel).SendMessageAsync("Bot has restarted");
                    }
                    else
                    {
                        ApplicationContext.TestGuild.BotTest.SendMessageAsync("Bot has restarted");
                    }
                }
                else if (Program.FirstArgument != null)
                {
                    ApplicationContext.TestGuild.BotTest.SendMessageAsync("Some thing happened: " + Program.FirstArgument);
                }

                Logger.Debug(ApplicationContext.BuildVersion, nameof(ApplicationContext.BuildVersion));
            }
            catch (Exception e)
            {
                client.StopAsync();
                client.LogoutAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Console.WriteLine("Failed loading context!");
                Console.ReadKey();
                Environment.Exit(0);
            }

            return Task.CompletedTask;
        }
    }
}
