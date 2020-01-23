using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class ReadyHandler : DiscordClientEventHandlerBase
    {
        private async Task RunTests()
        {
            //var testChannel = ApplicationContext.MKOTHHQGuild.Test;
            //var outputText = string.Join(", ", Utilities.EmojiPresets.Numbers);

            //var embed = new Discord.EmbedBuilder()
            //    .WithTitle(outputText)
            //    .WithDescription(outputText + $"```{outputText}``` `{outputText}`");

            //var message = await testChannel.SendMessageAsync(embed: embed.Build());
            //foreach (var item in Utilities.EmojiPresets.Numbers)
            //{
            //    _ = message.AddReactionAsync(item);
            //}

            await Task.CompletedTask;
        }

        public ReadyHandler(DiscordSocketClient client, ActivityCycler _) : base(client)
        {
            ApplicationContext.DiscordClient = client;

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

                if (Program.FirstArgument == "Restarted")
                {
                    var restartChannel = client.GetChannel(ulong.Parse(Program.SecondArgument));
                    if (restartChannel != null)
                    {
                        ((SocketTextChannel)restartChannel).SendMessageAsync("Bot has restarted");
                    }
                    else
                    {
                        ApplicationContext.MKOTHHQGuild.Log.SendMessageAsync("Bot has restarted");
                    }
                }
                else if (Program.FirstArgument != null)
                {
                    ApplicationContext.MKOTHHQGuild.Log.SendMessageAsync("Something happened: " + Program.FirstArgument);
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
