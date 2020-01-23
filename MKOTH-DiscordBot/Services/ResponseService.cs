using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Services
{
    public class ResponseService
    {
        public ResponseService()
        {
            Logger.Debug("Started", "Responder Service");
        }

        public IDisposable StartTypingAsync(SocketCommandContext context)
        {
            try
            {
                return context.Channel.EnterTypingState();
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                return null;
            }
        }

        public async Task SendToContextAsync(SocketCommandContext context, string reply, IDisposable typingState = null)
        {
            try
            {
                typingState?.Dispose();
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
