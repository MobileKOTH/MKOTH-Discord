using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class ReadyHandler : DiscordClientEventHandlerBase
    {
        private readonly IServiceProvider services;

        private async Task RunTests()
        {
            //await ApplicationContext.MKOTHHQGuild.Test.SendMessageAsync($"Appsettings ```json\n {services.GetScoppedSettings<AppSettings>().ToString()} ```");

            await Task.CompletedTask;
        }

        public ReadyHandler(DiscordSocketClient client, ActivityCycler _, SeriesService __, IServiceProvider services) : base(client)
        {
            ApplicationContext.DiscordClient = client;
            this.services = services;
            this.client.Ready += HandleReady;
        }

        private Task HandleReady()
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


                switch (Program.FirstArgument)
                {
                    case "Restarted":
                        {
                            var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument)) as SocketTextChannel ?? ApplicationContext.MKOTHHQGuild.Log;
                            restartChannel.SendMessageAsync("Bot has restarted");
                            break;
                        }
                    case "Updated":
                        {
                            var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument)) as SocketTextChannel ?? ApplicationContext.MKOTHHQGuild.Log;
                            restartChannel.SendMessageAsync($"Bot updated: {File.ReadAllText("../updatelog.txt").SliceBack(1900).MarkdownCodeBlock("c")}");
                            break;
                        }
                    default:
                        {
                            if (Program.FirstArgument == null)
                            {
                                break;
                            }
                            ApplicationContext.MKOTHHQGuild.Log.SendMessageAsync("Something happened: " + Program.FirstArgument);
                            break;
                        }
                }
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

            _ = RunTests();

            return Task.CompletedTask;
        }
    }
}
