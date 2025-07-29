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
using System.Net.Http;
using System.Text;
using Cerlancism.ChatSystem.Model;

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
        private readonly OpenAIAPI deepseekClient;

        private const int ReferenceChatContextSizeLimit = 10;
        //private const int ChatContextMessageLengthLimit = 512;
        private const int CurrentChatContextTotalLengthLimit = 4096;

        private string chatModel { get; set; }
        public const int InitialModerationDelay = 500;

        public class DeepseekHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name)
            {
                var client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(300)
                };

                return client;
            }
        }

        public ChatService(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            responseService = services.GetService<ResponseService>();
            discordLogger = services.GetService<DiscordLogger>();

            officialGuild = appSettings.Value.Settings.ProductionGuild.Id;
            officialChat = appSettings.Value.Settings.ProductionGuild.Official;

            openAIClient = new OpenAIAPI(services.GetScoppedSettings<Credentials>().OPENAI_API_KEY)
            {
                ApiUrlFormat = "https://api.openai.com/{0}/{1}"
            };
            deepseekClient = new OpenAIAPI(services.GetScoppedSettings<Credentials>().DEEPSEEK_API_KEY)
            {
                ApiUrlFormat = "https://api.deepseek.com/{0}/{1}",
                HttpClientFactory = new DeepseekHttpClientFactory()
            };

            chatModel = services.GetScoppedSettings<AppSettings>().Settings.ChatModel;

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
            var delayTask = Task.Delay(InitialModerationDelay);
            var chatTimer = new Stopwatch();
            Logger.Debug(message, "[ChatMessage]");
            // Step 1: Moderate Input
            if (await IsInputFlaggedAsync(message))
            {
                await HandleFlaggedInputAsync(context);
                return;
            }
            // Step 2: Purge and Generate References
            var purgedMessage = Chat.PurgeMessage(message);
            
            Logger.Debug(purgedMessage, "[PurgedMessage]");
            chatTimer.Start();

            var referenceGenerator = ChatSystem.GenerateResults(purgedMessage);
            // Step 3: Fetch and Filter Recent Messages
            var filteredMessages = await FetchAndFilterMessagesAsync(context);
            // Ensure there's at least one message
            if (!filteredMessages.Any())
            {
                filteredMessages.Add((0, "test", "Test Message"));
            }
            // Step 4: Moderate Filtered Messages
            var acceptableMessages = await ModerateMessagesAsync(filteredMessages);
            // Step 5: Moderate References
            var acceptableReferences = await ModerateReferencesAsync(referenceGenerator);

            chatTimer.Stop();
            Logger.Debug(chatTimer.Elapsed, $"[ModerateReferencesAsync Time]");

            // Step 6: Build Chat Messages
            var chatMessages = BuildChatMessages(context, acceptableMessages, acceptableReferences, message);
            // Step 7: Get Chat Completion with Retries
            var chatResult = await GetChatCompletionWithRetriesAsync(chatMessages);

            if (chatResult == null)
            {
                throw new Exception("Unable to get chat response, please try again later.");
            }

            // Step 8: Process Chat Result
            var reply = ProcessChatResult(chatResult, chatMessages, context);
            // Step 9: Log Chat and Reply
            LogChatResult(chatMessages, chatResult, reply);

            // Optional: Moderate Output (Commented Out)
            // if (await IsOutputFlaggedAsync(reply))
            // {
            //     await HandleFlaggedOutputAsync(context);
            //     return;
            // }

            // Step 10: Send Reply
            await delayTask;
            await SendReplyAsync(context, reply);
        }

        #region Step 1: Input Moderation

        private async Task<bool> IsInputFlaggedAsync(string message)
        {
            var moderationResponse = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(message));
            return moderationResponse.Results.FirstOrDefault()?.Flagged ?? false;
        }

        private async Task HandleFlaggedInputAsync(SocketCommandContext context)
        {
            var flaggedResult = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(context.Message.Content));
            Logger.Log($"Flagged input: {flaggedResult.Results[0].MainContentFlag}\n{context.Message.Content}", LogType.TrashReply);
            var embed = new EmbedBuilder()
                .WithDescription("Input rejected by moderator")
                .Build();
            await responseService.SendToChannelAsync(context.Channel as ITextChannel, null, embed);
        }

        #endregion

        #region Step 3: Fetch and Filter Messages

        private async Task<List<(ulong id, string name, string message)>> FetchAndFilterMessagesAsync(SocketCommandContext context)
        {
            var targetGuild = context.Client.GetGuild(context.Guild.Id);
            var targetChannel = context.Channel;
            var lastMessages = await targetChannel.GetMessagesAsync().FlattenAsync();
            int contextLength = 0;

            var filteredMessages = lastMessages.Skip(1)
                .Where(x => MessageFilter(context, x))
                .Select(message => ProcessMessage(message, targetGuild))
                .TakeWhile(msg =>
                {
                    contextLength += msg.message.Length;
                    return contextLength <= CurrentChatContextTotalLengthLimit;
                })
                .Take(30)
                .Reverse()
                .ToList();

            return filteredMessages;
        }

        private bool MessageFilter(SocketCommandContext context, IMessage x)
        {
            if (string.IsNullOrEmpty(x.Content))
                return false;

            if (x.Author.IsWebhook)
                return true;

            if (x.Author.IsBot && x.Author.Id != context.Client.CurrentUser.Id)
                return false;

            if (x.Author.Id == context.Client.CurrentUser.Id && (string.IsNullOrEmpty(x.Content) || x.Embeds.Count > 0))
                return false;

            return true;
        }

        private (ulong id, string name, string message) ProcessMessage(IMessage message, SocketGuild guild)
        {
            var user = guild.GetUser(message.Author.Id);
            var displayName = user?.GetDisplayName() ?? message.Author.Username;
            var messageContent = message.Content;

            foreach (var mentionId in message.MentionedUserIds)
            {
                var mentionUser = guild.GetUser(mentionId);
                var mentionDisplay = mentionUser?.GetDisplayName() ?? mentionUser?.Username ?? "";
                messageContent = messageContent.Replace($"<@{mentionId}", $"<@!{mentionId}");
                messageContent = messageContent.Replace(mentionUser?.Mention ?? $"<@!{mentionId}>", mentionDisplay);
            }

            return (message.Author.Id, displayName, messageContent);
        }

        #endregion

        #region Step 4: Moderate Messages

        private async Task<List<(ulong id, string name, string message)>> ModerateMessagesAsync(List<(ulong id, string name, string message)> messages)
        {
            var messagesToModerate = messages.Select(x => x.message).ToArray();
            var moderationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(messagesToModerate));
            var acceptableMessages = messages.Where((x, i) => !moderationResults.Results[i].Flagged).ToList();
            return acceptableMessages;
        }

        #endregion

        #region Step 5: Moderate References

        private async Task<List<string>> ModerateReferencesAsync(Task<(int, IEnumerable<Analysis>)> referenceGenerator)
        {
            var (_, referenceResults) = await referenceGenerator;
            var referencesToModerate = referenceResults
                .Take(ReferenceChatContextSizeLimit)
                .SelectMany(x => new[]
                {
                    x.Trigger.Message.SliceBack(128),
                    x.Rephrase.Message.SliceBack(128),
                    x.Response.Message.SliceBack(128)
                })
                .ToArray();

            var referenceModerationResults = await openAIClient.Moderation.CallModerationAsync(new ModerationRequest(referencesToModerate));
            var acceptableReferences = referencesToModerate
                .Where((x, i) => !referenceModerationResults.Results[i].Flagged)
                .ToList();

            return acceptableReferences;
        }

        #endregion

        #region Step 6: Build Chat Messages

        private List<ChatMessage> BuildChatMessages(
            SocketCommandContext context,
            List<(ulong id, string name, string message)> acceptableMessages,
            List<string> acceptableReferences,
            string originalMessage)
        {
            var referenceChat = string.Join("\n", acceptableReferences);
            var systemInstruction = $"You are a Discord chat bot {context.Client.CurrentUser.Username} " +
                $"enhanced with LLM Model ({chatModel}) of a competitive gaming community for BTD Battles, " +
                $"called MKOTH (Mobile King of the Hill). " +
                $"You behave casually and use a Discord gamer tone.";

            var referenceChatInstruction = $"With the style, tone, information and knowledge of the following context:\n\n{referenceChat}\n";
            var replyChatInstruction = $"Timestamp: {DateTime.Now} Give your fun and goofy short response or quick live reaction, unless requested otherwise, to:";

            var chatMessages = new List<ChatMessage>
            {
                new ChatMessage(ChatMessageRole.System, systemInstruction)
            };

            var chatUserMessages = new List<ChatMessageWithName>();

            foreach (var item in acceptableMessages)
            {
                var lastChat = chatMessages.Last();
                if (item.id == context.Client.CurrentUser.Id)
                {
                    if (lastChat.Role == ChatMessageRole.Assistant)
                    {
                        lastChat.Content += "\n\n" + item.message;
                    }
                    else
                    {
                        chatMessages.Add(new ChatMessage(ChatMessageRole.Assistant, item.message));
                    }
                }
                else
                {
                    if (lastChat.Role == ChatMessageRole.User)
                    {
                        lastChat.Content += $"\n\n{item.name}: {item.message}";
                    }
                    else
                    {
                        var chatMessage = new ChatMessageWithName(ChatMessageRole.User, item.name, $"{item.name}: {item.message}");
                        chatUserMessages.Add(chatMessage);
                        chatMessages.Add(chatMessage);
                    }
                }
            }

            var lastUserDisplay = context.Guild.GetUser(context.Message.Author.Id)?.GetDisplayName() ?? context.Message.Author.Username;
            var lastMessageContent = $"{referenceChatInstruction}\n{replyChatInstruction}\n{lastUserDisplay}: {originalMessage}";
            var lastMessage = new ChatMessageWithName(ChatMessageRole.User, lastUserDisplay, lastMessageContent);

            var finalChat = chatMessages.Last();
            if (finalChat.Role == ChatMessageRole.User)
            {
                finalChat.Content += "\n\n" + lastMessage.Content;
            }
            else
            {
                chatMessages.Add(lastMessage);
            }

            if (chatMessages.ElementAt(1)?.Role == ChatMessageRole.Assistant)
            {
                chatMessages.Insert(1, new ChatMessage(ChatMessageRole.User, "-"));
            }

            return chatMessages;
        }

        #endregion

        #region Step 7: Get Chat Completion with Retries

        private async Task<ChatResult> GetChatCompletionWithRetriesAsync(List<ChatMessage> chatMessages, int maxRetries = 3)
        {
            var chatRequest = new ChatRequest
            {
                Model = chatModel,
                Temperature = 1,
                MaxTokens = 1024,
                Messages = chatMessages.ToArray()
            };

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var chatResult = await deepseekClient.Chat.CreateChatCompletionAsync(chatRequest);
                    if (chatResult != null || chatResult.Choices?.Count < 1)
                    {
                        return chatResult;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Chat completion attempt {attempt} failed: {ex.Message}", LogType.TrashReply);
                }

                Logger.Log($"Retrying chat completion (Attempt {attempt})", LogType.TrashReply);
            }

            return null;
        }

        #endregion

        #region Step 8: Process Chat Result

        private string ProcessChatResult(ChatResult chatResult, List<ChatMessage> chatMessages, SocketCommandContext context)
        {
            var rawReply = chatResult.Choices[0].Message.Content.Trim();
            var reply = rawReply;

            // Revert user names in the reply
            var userChats = chatMessages
                .OfType<ChatMessageWithName>()
                .GroupBy(x => x.Name)
                .Select(g => g.First());

            foreach (var userChat in userChats)
            {
                reply = userChat.RevertName(reply);
            }

            // Escape @ mentions
            reply = reply.Replace("@", "`@`");

            return reply;
        }

        #endregion

        #region Step 9: Log Chat Result

        private void LogChatResult(List<ChatMessage> chatMessages, ChatResult chatResult, string reply)
        {
            var chatLog = new StringBuilder();
            chatLog.AppendLine("Prompt:");
            for (int i = 0; i < chatMessages.Count; i++)
            {
                var msg = chatMessages[i];
                var name = msg is ChatMessageWithName msgWithName ? msgWithName.Name : "N/A";
                chatLog.AppendLine($"{i}. [{msg.Role}] {name}: {msg.Content}");
                chatLog.AppendLine(new string('-', 32));
            }

            chatLog.AppendLine($"[Reply Raw] {chatResult.Choices[0].Message.Content.Trim()}");
            chatLog.AppendLine("--------");
            chatLog.AppendLine($"[Reply Out] {reply}");
            chatLog.AppendLine("--------");

            Logger.Log(chatLog.ToString(), LogType.TrashReply);
            Logger.Log(chatResult.Usage, LogType.TrashReply);
        }

        #endregion

        #region Step 10: Send Reply

        private async Task SendReplyAsync(SocketCommandContext context, string reply)
        {
            const int maxChunkSize = 2000;
            for (int i = 0; i < reply.Length; i += maxChunkSize)
            {
                var chunk = reply.Substring(i, Math.Min(maxChunkSize, reply.Length - i));
                await responseService.SendToContextAsync(context, chunk);
            }
        }

        #endregion

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
