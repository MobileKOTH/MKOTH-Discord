using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTHDiscordBot.Services
{
    [SingletonService("Helper class to send Discord messages.")]
    public class ResponseService
    {
        public static ResponseService Instance { get; private set; }

        private int currentTypingSecond = 0;
        private Timer secondCounter = new Timer(1000);

        public ResponseService()
        {
            if (Instance != null)
            {
                return;
            }

            secondCounter.Elapsed += HandleTimeCounter;
            secondCounter.Start();

            Instance = this;

            Logger.Debug("Started", "Responder Service");
        }

        public async Task TriggerTypingAsync(SocketCommandContext context)
        {
            try
            {
                if (currentTypingSecond == 0)
                {
                    currentTypingSecond = 10;
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
                currentTypingSecond = 0;
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

        private void HandleTimeCounter(object _, ElapsedEventArgs __)
        {
            currentTypingSecond = currentTypingSecond > 0 ? currentTypingSecond - 1 : 0;
        }
    }
}
