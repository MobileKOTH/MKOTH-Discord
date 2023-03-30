using Cerlancism.ChatSystem;
using Cerlancism.ChatSystem.OpenAIExtensions;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;

using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Services
{
    public class ChatService : IDisposable
    {
        public readonly Chat ChatSystem;

        private readonly ResponseService responseService;
        private readonly DiscordLogger discordLogger;
        private readonly ulong officialGuild;
        private readonly ulong officialChat;

        private readonly OpenAIAPI openAIClient;

        private const int ChatContextSizeLimit = 10;
        private const int ChatContextMessageLengthLimit = 200;

        public ChatService(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            responseService = services.GetService<ResponseService>();
            discordLogger = services.GetService<DiscordLogger>();

            officialGuild = appSettings.Value.Settings.ProductionGuild.Id;
            officialChat = appSettings.Value.Settings.ProductionGuild.Official;

            openAIClient = new OpenAIAPI(services.GetScoppedSettings<Credentials>().OPENAI_API_KEY);

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

            if (context.Channel.Id != officialChat)
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
            if (context.IsPrivate)
            {
                return;
            }
#if !DEBUG
            if (context.Guild.Id != officialChat)
            {
                return;
            }
#endif
            var delay = Task.Delay(500);
            var moderatedInput = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(message));

            if (moderatedInput.Results[0].Flagged)
            {
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, new EmbedBuilder()
                    .WithDescription($"Flagged input: {moderatedInput.Results[0].MainContentFlag}")
                    .Build()); ;
                return;
            }

            var typing = responseService.StartTypingAsync(context);

            var purgedMessage = Chat.PurgeMessage(message);

            var referenceGenerator = ChatSystem.GenerateResults(purgedMessage);

            var targetGuild = context.Client.GetGuild(officialGuild);
            var targetChannel = context.Channel;
            var lastMessages = await targetChannel.GetMessagesAsync().FlattenAsync();
            var filteredMessage = lastMessages.Skip(1)
                .TakeWhile(x =>
                {
                    if (string.IsNullOrEmpty(x.Content))
                    {
                        return false;
                    }
                    if (x.Author.IsBot && x.Author.Id != context.Client.CurrentUser.Id)
                    {
                        return false;
                    }
                    else if (x.Author.Id == context.Client.CurrentUser.Id && (string.IsNullOrEmpty(x.Content) || x.Embeds.Count > 0))
                    {
                        return false;
                    }
                    return true;
                })
                .Take(ChatContextSizeLimit)
                .Reverse()
                .Select(x =>
                {
                    var user = targetGuild.GetUser(x.Author.Id);
                    var name = user?.GetDisplayName() ?? x.Author.Username;
                    return (id: x.Author.Id, name: name, message: x.Content.SliceBack(ChatContextMessageLengthLimit));
                })
                .ToList();

            if (filteredMessage.Count == 0)
            {
                filteredMessage.Add((0, "test", "Test Message"));
            }

            var toModerateMessages = filteredMessage.Select(x => x.message).ToArray();
            var moderationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequestWithArray(toModerateMessages));
            var acceptableMessages = filteredMessage.Where((x, i) => !moderationResults.Results[i].Flagged);

            var (_, refenceResults) = await referenceGenerator;
            var toModerateReferences = refenceResults.Take(ChatContextSizeLimit)
                .Select(x => new[]
                {
                    x.Trigger.Message,
                    x.Rephrase.Message,
                    x.Response.Message
                })
                .SelectMany(x => x)
                .ToArray();
            var referenceModerationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequestWithArray(toModerateReferences));
            var referenceInstruction = toModerateReferences.Where((x, i) => !referenceModerationResults.Results[i].Flagged).JoinLines();

            var systemInstruction = $"You are a Discord chat bot powered by ChatGPT for a competitive gaming community for Bloons Tower Defense Battles, "
                + $"called Mobile King of the Hill, also known as MKOTH. "
                + $"You can behave casually, use Discord gamer tone and be more acceptable to some toxity in the chat."
                + $"Here is a reference of their past chat activity, take strong consideration of this in your response:\n ${referenceInstruction}";


            var chatMessages = new List<ChatMessage>();
            var chatUserMessages = new List<ChatMessageWithName>();

            chatMessages.Add(new ChatMessage(ChatMessageRole.System, systemInstruction));

            foreach (var item in acceptableMessages)
            {
                if (item.id == context.Client.CurrentUser.Id)
                {
                    chatMessages.Add(new ChatMessage(ChatMessageRole.Assistant, item.message));
                }
                else
                {
                    var chatMessage = new ChatMessageWithName(ChatMessageRole.User, item.name, item.message);
                    chatUserMessages.Add(chatMessage);
                    chatMessages.Add(chatMessage);
                }
            }

            chatMessages.Add(new ChatMessageWithName(
                ChatMessageRole.User, 
                targetGuild.GetUser(context.Message.Author.Id)?.GetDisplayName() ?? context.Message.Author.Username, purgedMessage));

            var chatResult = await openAIClient.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.ChatGPTTurbo,
                Temperature = 1,
                MaxTokens = 1024,
                Messages = chatMessages.ToArray()
            });

            var reply = chatResult.Choices[0].Message.Content.Trim();

            if ((await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(reply))).Results[0].Flagged)
            {
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, new EmbedBuilder()
                    .WithDescription($"Flagged output: {moderatedInput.Results[0].MainContentFlag}")
                    .Build());
                typing.Dispose();
                return;
            }

            await delay;
            await responseService.SendToContextAsync(context, reply, typing);
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
