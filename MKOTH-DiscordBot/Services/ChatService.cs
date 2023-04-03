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

using System.Diagnostics;

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

        private const int ReferenceChatContextSizeLimit = 16;
        //private const int ChatContextMessageLengthLimit = 512;
        private const int CurrentChatContextTotalLengthLimit = 4096;

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
            var delay = Task.Delay(500);
            var typing = responseService.StartTypingAsync(context);
            var moderatedInput = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(message));

            Logger.Debug(message, "[ChatMessage]");

            if (moderatedInput.Results[0].Flagged)
            {
                Logger.Log($"Flagged input: {moderatedInput.Results[0].MainContentFlag}\n{message}", LogType.TrashReply);
                typing.Dispose();
                var embed = new EmbedBuilder()
                    .WithDescription($"Input rejected by moderator")
                    .Build();
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, embed); ;
                return;
            }

            var purgedMessage = Chat.PurgeMessage(message);
            Logger.Debug(purgedMessage, "[PurgedMessage]");
            var referenceGenerator = ChatSystem.GenerateResults(purgedMessage);

            var targetGuild = context.Client.GetGuild(context.Guild.Id);
            var targetChannel = context.Channel;
            var lastMessages = await targetChannel.GetMessagesAsync().FlattenAsync();
            var contextLength = 0;
            var filteredMessage = lastMessages.Skip(1)
                .Where(x =>
                {
                    if (string.IsNullOrEmpty(x.Content))
                    {
                        return false;
                    }
                    if (x.Author.IsWebhook)
                    {
                        return true;
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
                .Select(x =>
                {
                    var user = targetGuild.GetUser(x.Author.Id);
                    var displayName = user?.GetDisplayName() ?? x.Author.Username;
                    var messageContent = x.Content;
                    foreach (var mentionId in x.MentionedUserIds)
                    {
                        var mentionUser = targetGuild.GetUser(mentionId);
                        var mentionDisplay = mentionUser?.GetDisplayName() ?? mentionUser?.Username ?? "";
                        messageContent = messageContent.Replace("<@" + mentionId, "<@!" + mentionId);
                        messageContent = messageContent.Replace(mentionUser?.Mention ?? ("<@!" + mentionId + ">"), mentionDisplay);
                    }
                    //messageContent = x.Author.Id == context.Client.CurrentUser.Id ? messageContent : messageContent.SliceBack(ChatContextMessageLengthLimit);
                    return (id: x.Author.Id, name: displayName, message: messageContent);
                })
                .TakeWhile(x =>
                {
                    if ((contextLength += x.message.Length) > CurrentChatContextTotalLengthLimit)
                    {
                        return false;
                    }
                    return true;
                })
                .Reverse()
                .ToList();
            if (filteredMessage.Count == 0)
            {
                filteredMessage.Add((0, "test", "Test Message"));
            }

            var toModerateMessages = filteredMessage.Select(x => x.message).ToArray();

            var chatTimer = new Stopwatch();
            chatTimer.Start();
            var moderationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequestWithArray(toModerateMessages));
            chatTimer.Stop();
            Logger.Debug(chatTimer.Elapsed, $"[ModerationChannel Time]");
            chatTimer.Reset();

            var acceptableMessages = filteredMessage.Where((x, i) => !moderationResults.Results[i].Flagged).ToList();

            var (_, refenceResults) = await referenceGenerator;
            var toModerateReferences = refenceResults.Take(ReferenceChatContextSizeLimit)
                .Select(x => new[]
                {
                    x.Trigger.Message.SliceBack(128),
                    x.Rephrase.Message.SliceBack(128),
                    x.Response.Message.SliceBack(128)
                })
                .SelectMany(x => x)
                .ToArray();

            chatTimer.Start();
            var referenceModerationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequestWithArray(toModerateReferences));
            chatTimer.Stop();
            Logger.Debug(chatTimer.Elapsed, $"[ModerationReference Time]");
            chatTimer.Reset();

            var acceptableReferences = toModerateReferences.Where((x, i) => !referenceModerationResults.Results[i].Flagged).ToList();
            var referenceChat = acceptableReferences.JoinLines();

            var chatMessages = new List<ChatMessage>();
            var chatUserMessages = new List<ChatMessageWithName>();

            var systemInstruction = $"You are a Discord chat bot {context.Client.CurrentUser.Username}#{context.Client.CurrentUser.Discriminator} "
                + $"enhanced with ChatGPT of a competitive gaming community for BTD Battles, "
                + $"called MKOTH (Mobile King of the Hill). "
                + $"You behave casually and use a Discord gamer tone. ";
            var referenceChatInstruction = $"With the style, tone, information and knowledge of the following context:\n\n{referenceChat}\n";
            var replyChatInstruction = "Give your fun and goofy short response or quick live reaction to:";

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
            chatMessages.Add(new ChatMessage(ChatMessageRole.User, referenceChatInstruction));
            chatMessages.Add(new ChatMessage(ChatMessageRole.User, replyChatInstruction));
            
            var lastUserDisplay = targetGuild.GetUser(context.Message.Author.Id)?.GetDisplayName() ?? context.Message.Author.Username;
            var lastMessage = new ChatMessageWithName(ChatMessageRole.User, lastUserDisplay, message);
            chatUserMessages.Add(lastMessage);
            
            chatMessages.Add(lastMessage);

            var chatMetricsLog = $"References: {acceptableReferences.Count}/{toModerateReferences.Length}/{refenceResults.Count() * 3} [{refenceResults.Max(x => x.Score):F2},{refenceResults.Min(x => x.Score):F2}] ({referenceChat.Length}) " +
                $"UserChats: {chatUserMessages.Count} ({chatUserMessages.Sum(x => x.Content.Length)}) " +
                $"PromptChats: {chatMessages.Count} ({chatMessages.Sum(x => x.Content.Length)})";
            Logger.Log(chatMetricsLog, LogType.TrashReply);

            chatTimer.Start();
            var chatResult = await openAIClient.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.ChatGPTTurbo,
                Temperature = 1,
                MaxTokens = 256,
                Messages = chatMessages.ToArray()
            });
            chatTimer.Stop();
            Logger.Log($"[ChatGPT Time] {chatTimer.Elapsed}", LogType.TrashReply);
            chatTimer.Reset();

            var rawreply = chatResult.Choices[0].Message.Content.Trim();
            var reply = rawreply;

            foreach (var userChat in chatUserMessages.GroupBy(x => x.Name).Select(x => x.First()))
            {
                reply = userChat.RevertName(reply);
            }
            reply = reply.Replace("@", "`@`").SliceBack(2000);

            var chatLog = $"Prompt:\n" +
                $"{chatMessages.Select((x, i) => $"{i}. [{x.Role}] {(x as ChatMessageWithName)?.Name ?? "N/A"}: {x.Content}\n").JoinLines(new string('-', 32) + "\n")}\n" +
                $"[Reply Raw] {rawreply}\n--------\n" +
                $"[Reply Out] {reply}\n--------";
            Logger.Log(chatLog, LogType.TrashReply);
            Logger.Log(chatResult.Usage, LogType.TrashReply);

            var outputModeration = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(reply));
            if (outputModeration.Results[0].Flagged)
            {
                Logger.Log($"Flagged output: {outputModeration.Results[0].MainContentFlag}\n", LogType.TrashReply);
                typing.Dispose();
                var embed = new EmbedBuilder()
                    .WithDescription($"Output rejected by moderator")
                    .Build();
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, embed);
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
