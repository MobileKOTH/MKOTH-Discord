using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace MKOTHDiscordBot.Handlers
{
    public class MessageHandler : DiscordClientEventHandlerBase
    {
        public static bool ReplyToTestServer = true;

        private IServiceProvider services;
        private CommandService commands;
        private SpamWatch spamWatch;
        private ResponseService responseService;

        private ulong currentUserId;

        public MessageHandler(
            DiscordSocketClient client,
            CommandService commands,
            IServiceProvider services,
            SpamWatch spamWatch,
            ResponseService responseService) : base(client)
        {
            this.services = services;
            this.commands = commands;
            this.spamWatch = spamWatch;
            this.responseService = responseService;

            this.client.MessageReceived += Handle;
            this.client.Ready += async () => currentUserId = await Task.FromResult(this.client.CurrentUser.Id);
        }

        Action RateLimitMessage(SocketCommandContext context) => ()
            => _ = responseService.SendToContextAsync(context, "You are now rate limited");

        async Task Handle(SocketMessage socketMessage)
        {
            // No handle to null or own message.
            if (!(socketMessage is SocketUserMessage message))
            {
                return;
            }

            if (message.Author.Id == currentUserId)
            {
                return;
            }

            var context = new SocketCommandContext(client, message);
            int argPos = 0;

            // Debug log non dm messages.
            if (!context.IsPrivate)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(message.Timestamp.ToLocalTime() + "\tUser: " + message.Author.Username + "\nMessage: " + message.Content);
                Console.ResetColor();
            }
            // Special test mode command handling.
            if (!ReplyToTestServer && message.Content == ".settest")
            {
                ReplyToTestServer = true;
                await context.Channel.SendMessageAsync("Replying to test server");
                return;
            }
            // Special test mode to not handle certain messages.
            if (!context.IsPrivate)
            {
                if (!Program.TestMode && !ReplyToTestServer && (context.Guild.Id == ApplicationContext.TestGuild.Guild.Id))
                {
                    return;
                }

                if (Program.TestMode && (context.Guild.Id == ApplicationContext.MKOTHGuild.Guild.Id))
                {
                    return;
                }
            }
            else if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                if (Program.TestMode)
                {
                    return;
                }
            }
            // Chat handling in DM.
            if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                if (spamWatch.Watch(message.Author.Id, RateLimitMessage(context)))
                {
                    return;
                }

                chatReply(message.Content);
                return;
            }
            else if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                if (spamWatch.Watch(message.Author.Id, RateLimitMessage(context)))
                {
                    return;
                }

                chatReply(message.Content.Remove(0, argPos));
                return;
            }

            using (var chatService = services.GetRequiredService<ChatService>())
            {
                if (!message.Author.IsBot && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
                {
                    await chatService.AddSync(context);
                }
            }

            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            {
                return;
            }

            if (spamWatch.Watch(message.Author.Id, RateLimitMessage(context)))
            {
                return;
            }
            // Command handling.
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (context.IsPrivate && message.Author.Id != ApplicationContext.BotOwner.Id)
            {
                await responseService.SendToChannelAsync(ApplicationContext.TestGuild.BotTest, "DM command received:", new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithDescription(message.Content)
                    .Build());
            }

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                if (result.Error == CommandError.BadArgCount || result.Error == CommandError.ParseFailed || result.Error == CommandError.ObjectNotFound)
                {
                    if (commands.Search(context, argPos).Commands.Count(x => x.Command.Remarks != null) > 0 && result.Error == CommandError.ObjectNotFound)
                    {
                        await context.Channel.SendMessageAsync("Execution failed, please refer to the command infomation.");
                        sendHelp();
                        return;
                    }
                    else if (result.Error != CommandError.ObjectNotFound)
                    {
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                        sendHelp();
                        return;
                    }
                }
                await context.Channel.SendMessageAsync(result.ErrorReason);
                return;
            }
            else if (result.Error == CommandError.Unsuccessful || result.Error == CommandError.Exception)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
            else if (result.Error == CommandError.UnknownCommand)
            {// Chat reply.
                if (message.HasMentionPrefix(client.CurrentUser, ref argPos) && !context.IsPrivate)
                {
                    chatReply(message.Content.Remove(0, argPos));
                }
            }

            void sendHelp()
            {
                _ = commands.Commands
                    .Where(x => x.Name == "Help")
                    .Single(x => x.Parameters.Count == 1)
                    .ExecuteAsync(context, new object[1] { "." + commands.Search(context, argPos).Commands.First().Command.Name }, null, services);
            }

            void chatReply(string input)
            {
                _ = commands.Commands
                    .Where(x => x.Name == "Reply")
                    .Single()
                    .ExecuteAsync(context, new object[1] { input }, null, services);
            }
        }
    }
}
