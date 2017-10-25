using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

namespace MKOTH_Discord_Bot.Utilities
{
    public enum StatusMessages { HELP, INVOLVE, INFO, ENCOURAGE };

    public static class Responder
    {
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

        public static void SendToGuildUser(SocketGuildUser user, string message)
        {

        }

        public static void SendToUser(SocketUser user, string message)
        {

        }

        public static async Task ChangeStatus(StatusMessages status, DiscordSocketClient client)
        {
            try
            {
                status = (int)status + 1 < (Enum.GetValues(typeof(StatusMessages)).Length) ? status + 1 : 0;
                Console.WriteLine(status);
                switch (status)
                {
                    case StatusMessages.HELP:
                        await client.SetGameAsync("a Series | .mkothhelp for help");
                        break;

                    case StatusMessages.INVOLVE:
                        await client.SetGameAsync("with MKOTH Members!");
                        break;

                    case StatusMessages.INFO:
                        await client.SetGameAsync("MKOTH | .info for information");
                        break;

                    case StatusMessages.ENCOURAGE:
                        await client.SetGameAsync("Ranked Series for ELO Display!");
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
