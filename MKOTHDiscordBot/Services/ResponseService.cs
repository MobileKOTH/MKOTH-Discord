using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Timers;

namespace MKOTHDiscordBot.Services
{
    [SingletonService("Helper class to send discord messages.")]
    public class ResponseService
    {
        public static ResponseService Instance { get; private set; }

        private StatusCycler statusCycler;

        public ResponseService(StatusCycler statusCycler)
        {
            Instance = this;

            this.statusCycler = statusCycler;

            Logger.Debug("Started", "Responder Service");
        }

        public async Task TriggerTypingAsync(SocketCommandContext context)
        {
            try
            {
                if (ApplicationContext.CurrentTypingSecond == 0)
                {
                    ApplicationContext.CurrentTypingSecond = 10;
                    await context.Channel.TriggerTypingAsync();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public async Task SendToContextAsync(SocketCommandContext context, string reply)
        {
            try
            {
                ApplicationContext.CurrentTypingSecond = 0;
                await context.Channel.SendMessageAsync(reply);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public async Task SendToChannelAsync(SocketTextChannel channel, string message, Embed embed = null)
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
    }
}
