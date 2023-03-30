﻿using Cerlancism.ChatSystem;
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
            if (context.Channel.Id != officialChat)
            {
                await responseService.SendToContextAsync(context, "I can only chat in MKOTH Official Chat");
                return;
            }
#endif
            var delay = Task.Delay(500);
            var moderatedInput = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(message));

            Logger.Debug(message, "[ChatMessage]");

            if (moderatedInput.Results[0].Flagged)
            {
                Logger.Debug($"Flagged input: {moderatedInput.Results[0].MainContentFlag}");
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, new EmbedBuilder()
                    .WithDescription($"Input rejected by moderator")
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
                .Where(x =>
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
                    var displayName = user?.GetDisplayName() ?? x.Author.Username;
                    var messageContent = x.Content;

                    foreach (var mentionId in x.MentionedUserIds)
                    {
                        var mentionUser = targetGuild.GetUser(mentionId);
                        var mentionDisplay = mentionUser?.GetDisplayName() ?? mentionUser.Username;
                        messageContent = messageContent.Replace("<@" + mentionId.ToString(), "<@!" + mentionId.ToString());
                        messageContent = messageContent.Replace(mentionUser.Mention, mentionDisplay);
                    }

                    return (id: x.Author.Id, name: displayName, message: messageContent.SliceBack(ChatContextMessageLengthLimit));
                })
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
            var toModerateReferences = refenceResults.Take(ChatContextSizeLimit)
                .Select(x => new[]
                {
                    x.Trigger.Message,
                    x.Rephrase.Message,
                    x.Response.Message
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

            chatMessages.Add(new ChatMessage(ChatMessageRole.System,
                $"You are a Discord chat bot enhanced with ChatGPT of a competitive gaming community for BTD Battles, "
                + $"called MKOTH (Mobile King of the Hill). "
                + $"You behave casually and use a Discord gamer tone. "
            ));
            chatMessages.Add(new ChatMessage(ChatMessageRole.User,
                $"With the style, tone, information and knowledge of the following context:\n\n{referenceChat}\n\n"
            ));

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

            chatMessages.Add(new ChatMessage(ChatMessageRole.User, "Give your funny and goofy live reaction and response to:"));

            var lastMessage = new ChatMessageWithName(ChatMessageRole.User,
                targetGuild.GetUser(context.Message.Author.Id)?.GetDisplayName() ?? context.Message.Author.Username, 
                purgedMessage
            );

            chatUserMessages.Add(lastMessage);
            chatMessages.Add(lastMessage);

            Logger.Debug($"References: {acceptableReferences.Count} ({referenceChat.Length}) " +
                $"UserChats: {chatUserMessages.Count} ({chatUserMessages.Sum(x => x.Content.Length)}) " +
                $"PromptChats: {chatMessages.Count} ({chatMessages.Sum(x => x.Content.Length)})", "[ChatContext]");

            chatTimer.Start();
            var chatResult = await openAIClient.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.ChatGPTTurbo,
                Temperature = 1,
                MaxTokens = 2048,
                Messages = chatMessages.ToArray()
            });
            chatTimer.Stop();
            Logger.Debug(chatTimer.Elapsed, $"[ChatGPT Time]");
            chatTimer.Reset();

            Logger.Debug(chatResult.Usage, "[ChatGPT Usage]");
            var reply = chatResult.Choices[0].Message.Content.Trim();

            foreach (var userChat in chatUserMessages)
            {
                reply = userChat.RevertName(reply);
            }

            var outputModeration = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(reply));
            if (outputModeration.Results[0].Flagged)
            {
                Logger.Debug($"Flagged output: {outputModeration.Results[0].MainContentFlag}");
                await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, new EmbedBuilder()
                    .WithDescription($"Output rejected by moderator")
                    .Build());
                typing.Dispose();
                return;
            }

            await delay;
            await responseService.SendToContextAsync(context, reply.Replace("@", "`@`").SliceBack(2000), typing);
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
