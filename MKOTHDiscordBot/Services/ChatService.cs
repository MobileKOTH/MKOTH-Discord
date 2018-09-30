using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using Cerlancism.ChatSystem;
using Discord.Commands;
using Discord;

namespace MKOTHDiscordBot.Services
{
    public class ChatService : IDisposable
    {
        public string ConnectionString => ConfigurationManager.ConnectionStrings["ChatHistory"].ConnectionString;

        public Chat ChatSystem;

        private ResponseService responseService;
        private Stopwatch stopWatch;

        public ChatService(ResponseService responseService)
        {
            this.responseService = responseService;

            ChatSystem = new Chat(ConnectionString);
            ChatSystem.Log += HandleLog;
        }

        public async Task AddSync(SocketCommandContext context)
        {
            if (context.IsPrivate) return;
            if (context.User.IsWebhook) return;
            if (context.Channel.Id != ApplicationContext.MKOTHGuild.Official.Id) return;

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
            if (context.Channel.Id == ApplicationContext.MKOTHGuild.Suggestions.Id)
            {
                return;
            }

            if (message.Length < 2)
            {
                return;
            }

            var typing = responseService.StartTypingAsync(context);

            stopWatch = Stopwatch.StartNew();

            var reply = await ChatSystem.ReplyAsync(message);
            reply = reply.SliceBack(1900);

            stopWatch.Stop();

            await responseService.SendToContextAsync(context, reply, typing);
            if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                await responseService.SendToChannelAsync(ApplicationContext.TestGuild.BotTest, "DM chat received:", new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription(message)
                    .AddField("Response", reply)
                    .Build());
            }
        }

        void HandleLog(string log)
        {
            var time = stopWatch.Elapsed.TotalMilliseconds;
            var fullLog = $"{log} \n" +
                $"**Time Used:** `{time}` ms";
            Logger.Log(fullLog, LogType.TrashReply);
        }

        public void Dispose()
        {
            Logger.Debug("Disposed", "ChatSystem");
            ChatSystem.Log -= HandleLog;
            ChatSystem.Dispose();
        }
    }
}
