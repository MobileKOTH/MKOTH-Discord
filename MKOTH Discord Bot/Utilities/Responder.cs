using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Utilities
{
    public enum StatusMessageType { HELP, INFO, KING, GAMESCOUNT, SUBMIT };

    public static class Responder
    {
        private static readonly List<StatusMessageType> statusSequence = new List<StatusMessageType>
        {
            StatusMessageType.INFO,
            StatusMessageType.KING,
            StatusMessageType.GAMESCOUNT,
            StatusMessageType.SUBMIT,
        };
        private static (StatusMessageType current, StatusMessageType last) status = (StatusMessageType.HELP, statusSequence.Last());

        public static async Task TriggerTyping(SocketCommandContext context)
        {
            try
            {
                if (Globals.CurrentTypingSecond == 0)
                {
                    Globals.CurrentTypingSecond = 10;
                    await context.Channel.TriggerTypingAsync();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
            await Task.CompletedTask;
        }

        public static async Task SendToContext(SocketCommandContext context, string reply)
        {
            try
            {
                Globals.CurrentTypingSecond = 0;
                await Task.Delay(500);
                await context.Channel.SendMessageAsync(reply);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public static async Task SendToChannel(SocketTextChannel channel, string message, Embed embed = null)
        {
            try
            {
                await channel.SendMessageAsync(message, false, embed);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public static async Task ChangeStatus(DiscordSocketClient client)
        {
            try
            {
                Console.WriteLine(status);
                switch (status.current)
                {
                    case StatusMessageType.HELP:
                        await client.SetGameAsync("| .help for general help");
                        setNextStatus();
                        return;

                    case StatusMessageType.INFO:
                        await client.SetGameAsync("| .info for MKOTH help");
                        break;

                    case StatusMessageType.SUBMIT:
                        await client.SetGameAsync("| .submit to submit series");
                        break;

                    case StatusMessageType.KING:
                        var kingname = Player.List.First(x => x.Class == PlayerClass.KING).Name;
                        var kingstatus = "King: " + kingname.SliceBack(18);
                        await client.SetGameAsync(kingstatus);
                        break;

                    case StatusMessageType.GAMESCOUNT:
                        var count = Player.List.Sum(x => (x.Wins + x.Loss + x.Draws) / 2);
                        var gamestatus = count + " total games played";
                        await client.SetGameAsync(gamestatus);
                        break;
                }
                status.last = status.current;
                status.current = StatusMessageType.HELP;

                void setNextStatus()
                {
                    var currentIndex = statusSequence.IndexOf(status.last);
                    currentIndex = currentIndex + 1 == statusSequence.Count ? 0 : currentIndex + 1;
                    status.current = statusSequence[currentIndex];
                }
            }
            catch (Exception e)
            {                
                await Logger.SendError(e);
            }
        }
    }
}
