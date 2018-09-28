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

        public ChatService()
        {
            ChatSystem = new Chat(ConnectionString);
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
                for (int i = 0; i < context.Message.MentionedUsers.Count; i++)
                {
                    CleanMessage = CleanMessage.Replace("<@" + context.Message.MentionedUsers.ElementAt(i).Id.ToString(), "<@!" + context.Message.MentionedUsers.ElementAt(i).Id.ToString());
                    CleanMessage = CleanMessage.Replace(context.Message.MentionedUsers.ElementAt(i).Mention, context.Message.MentionedUsers.ElementAt(i).Username);
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

            await ResponseService.Instance.TriggerTypingAsync(context);

            var stopWatch = Stopwatch.StartNew();
            ChatSystem.Log += HandleLog;
            var fullLog = "";

            var reply = await ChatSystem.ReplyAsync(message);

            stopWatch.Stop();

            await ResponseService.Instance.SendToContextAsync(context, reply);
            if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                await ResponseService.Instance.SendToChannelAsync(ApplicationContext.TestGuild.BotTest, "DM chat received:", new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription(message)
                    .AddField("Response", reply)
                    .Build());
            }

            ChatSystem.Log -= HandleLog;

            void HandleLog(string log)
            {
                var time = stopWatch.Elapsed.TotalMilliseconds;
                fullLog = $"{log} \n" +
                    $"**Time Used:** `{time}` ms";
                Logger.Log(fullLog, LogType.TrashReply);
            }
        }

        public void Dispose()
        {
            Logger.Debug("Disposed", "ChatSystem");
            ChatSystem.Dispose();
            ChatSystem = null;
        }
    }
}
