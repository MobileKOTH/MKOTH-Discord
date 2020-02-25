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
            var sh = services.GetService<SeriesService>();
            var ch = client.GetChannel(681002605438435328) as ITextChannel;
            var msgs = (await ch.GetMessagesAsync().FlattenAsync()).Where(x => x.Author.Id == client.CurrentUser.Id);

            //foreach (IUserMessage msg in msgs)
            //{
            //    var embed = msg.Embeds.FirstOrDefault();

            //    if (embed != null && embed.Description.StartsWith("Id: "))
            //    {
            //        var idString = embed.Description.Substring(4, 4);
            //        var id = int.Parse(idString);

            //        var series = sh.SeriesHistory.FirstOrDefault(x => x.Id == id);

            //        string getUserMention(string userId) => $"<@{userId}>";

            //        if (series != null)
            //        {
            //            await msg.ModifyAsync(x => x.Embed = embed.ToEmbedBuilder().WithDescription($"Id: {series.Id.ToString("D4")}\n" +
            //                $"Winner: {getUserMention(series.WinnerId)}\n" +
            //                $"Loser: {getUserMention(series.LoserId)}\n" +
            //                $"Score: {series.Wins}-{series.Losses} Draws: {series.Draws}\n" +
            //                $"Invite Code: {series.ReplayId}").Build());
            //        }
            //    }
            //}
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
