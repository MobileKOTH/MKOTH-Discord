using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Utilities
{
    public enum StatusMessages { INFO, KING, HELP, GAMESCOUNT };

    public static class Responder
    {
        private static StatusMessages status = StatusMessages.HELP;
        
        /**
        private static void GlobalTryCatch(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }
            **/

        public static async Task TriggerTyping(SocketCommandContext context)
        {
            try
            {
                if (ContextPools.CurrentTypingSecond == 0)
                {
                    ContextPools.CurrentTypingSecond = 10;
                    await context.Channel.TriggerTypingAsync();
                }
                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }

        public static async Task SendToContext (SocketCommandContext context, string reply)
        {
            try
            {
                ContextPools.CurrentTypingSecond = 0;
                await Task.Delay(500);
                await context.Channel.SendMessageAsync(reply);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }

        public static async Task SendToChannel (SocketTextChannel channel, string message)
        {
            try
            {
                await channel.SendMessageAsync(message, false, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }


        public static void SendToGuildUser(SocketGuildUser user, string message)
        {

        }

        public static void SendToUser(SocketUser user, string message)
        {

        }

        public static async Task ChangeStatus(DiscordSocketClient client)
        {
            try
            {
                status = (int)status + 1 < (Enum.GetValues(typeof(StatusMessages)).Length) ? status + 1 : 0;
                Console.WriteLine(status);
                switch (status)
                {
                    case StatusMessages.HELP:
                        await client.SetGameAsync("| .mkothhelp for help");
                        break;

                    case StatusMessages.INFO:
                        await client.SetGameAsync("| .info for information");
                        break;

                    case StatusMessages.KING:
                        var kingname = Player.List.First(x => x.Playerclass == PlayerClass.KING).Name;
                        var kingstatus = "King: " + kingname.Slice(18);
                        await client.SetGameAsync(kingstatus);
                        break;

                    case StatusMessages.GAMESCOUNT:
                        var count = Player.List.Sum(x => (x.Wins + x.Loss + x.Draws) / 2);
                        var gamestatus = count + " total games played";
                        await client.SetGameAsync(gamestatus);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Logger.Log(e.Message.AddLine() + e.StackTrace, LogType.ERROR);
            }
        }






    }
}
