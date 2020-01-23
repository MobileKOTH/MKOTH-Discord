using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Cerlancism.ChatSystem;
using MKOTHDiscordBot.Properties;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Options;

using UwuTranslator = MKOTHDiscordBot.Utilities.UwuTranslator;

namespace MKOTHDiscordBot.Services
{
    public class ChatService : IDisposable
    {
        public Chat ChatSystem;

        private readonly ResponseService responseService;

        public ChatService(ResponseService responseService, IOptions<AppSettings> appSettings)
        {
            this.responseService = responseService;

            ChatSystem = new Chat(appSettings.Value.ConnectionStrings.ChatHistory);
            ChatSystem.Log += HandleLog;
        }

        public async Task AddSync(SocketCommandContext context)
        {
            if (context.IsPrivate)
            {
                return;
            }

            if (context.User.IsWebhook)
            {
                return;
            }

            var message = context.Message.Content;
            if (context.Message.MentionedUsers.Count > 0)
            {
                if (context.Message.MentionedUsers.Contains(context.Client.CurrentUser))
                {
                    return;
                }
                string CleanMessage = context.Message.Content;
                foreach (var user in context.Message.MentionedUsers)
                {
                    CleanMessage = CleanMessage.Replace("<@" + user.Id.ToString(), "<@!" + user.Id.ToString());
                    CleanMessage = CleanMessage.Replace(user.Mention, user.Username);
                }
                message = CleanMessage.Trim();
            }
            message = message.Replace("@", "`@`");

            await ChatSystem.AddAsync(context.User.Id, message);
        }

        public async Task ReplyAsync(SocketCommandContext context, string message)
        {
            if (message.Length < 2)
            {
                return;
            }

            var typing = responseService.StartTypingAsync(context);

            var delay = Task.Delay(500);
            var reply = await ChatSystem.ReplyAsync(message);
            reply = UwuTranslator.Translate(reply);
            reply = reply.SliceBack(1900);

            await delay;

            await responseService.SendToContextAsync(context, reply, typing);

            if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                await responseService.SendToChannelAsync(ApplicationContext.MKOTHHQGuild.Log, "DM chat received:", new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription(message)
                    .AddField("Response", reply)
                    .Build());
            }
        }

        void HandleLog(string log)
        {
            Logger.Log(log, LogType.TrashReply);
        }

        public void Dispose()
        {
            Logger.Debug("Disposed", "ChatSystem");
            ChatSystem.Log -= HandleLog;
            ChatSystem.Dispose();
        }
    }
}
