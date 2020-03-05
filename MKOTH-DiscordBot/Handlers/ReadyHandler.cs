using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class ReadyHandler : DiscordClientEventHandlerBase
    {
        private readonly ActivityCycler activityCycler;
        private readonly DiscordLogger discordLogger;

        private async Task RunTests()
        {
            var chn = client.GetChannel(0) as ITextChannel;
            var msgs = chn.GetMessagesAsync();

            await Task.CompletedTask;
        }

        public ReadyHandler(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            ApplicationContext.DiscordClient = client;

            discordLogger = services.GetService<DiscordLogger>();
            activityCycler = services.GetService<ActivityCycler>();
            client.Ready += HandleReady;
        }

        private Task HandleReady()
        {
            try
            {
                _ = activityCycler.ChangeActivityAsync();

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
                            var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument)) as SocketTextChannel ?? discordLogger.LogChannel;
                            restartChannel.SendMessageAsync("Bot has restarted");
                            break;
                        }
                    case "Updated":
                        {
                            var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument)) as SocketTextChannel ?? discordLogger.LogChannel;
                            restartChannel.SendMessageAsync($"Bot updated: {File.ReadAllText("../updatelog.txt").SliceBack(1900).MarkdownCodeBlock("c")}");
                            break;
                        }
                    default:
                        {
                            if (Program.FirstArgument == null)
                            {
                                break;
                            }
                            discordLogger.Log("Something happened: " + Program.FirstArgument);
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
