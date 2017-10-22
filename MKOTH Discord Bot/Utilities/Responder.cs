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
                Console.WriteLine(e.StackTrace);
            }
        }

        public static void SendToContext (SocketCommandContext context, string reply)
        {
            GlobalTryCatch(async () =>
            {
                await Task.Delay(500);
                await context.Channel.SendMessageAsync(reply);
            });
        }

        public static void SendToGuildUser(SocketGuildUser user, string message)
        {
            GlobalTryCatch(async () =>
            {
                Console.WriteLine("Sent DM to " + user.Username + " (" + user.Nickname + ") " + "\n" +
                        "Message: " + message);
                await user.SendMessageAsync(message);

            });
        }

        public static void SendToUser(SocketUser user, string message)
        {
            GlobalTryCatch(async () =>
            {
                Console.WriteLine("Sent DM to " + user.Username + user.Discriminator + "\n" +
                        "Message: " + message);
                await user.SendMessageAsync(message);

            });
        }

        public static void ChangeStatus(StatusMessages status, DiscordSocketClient client)
        {
            status = (int)status + 1 < (Enum.GetValues(typeof(StatusMessages)).Length) ? status + 1 : 0;
            GlobalTryCatch(async () =>
            {
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
            });
        }






    }
}
