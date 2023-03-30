using System;
using System.Linq;
using System.Threading.Tasks;

using Cerlancism.ChatSystem;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Handlers
{
    public class MessageHandler : DiscordClientEventHandlerBase
    {
        public static bool ReplyToTestServer = true;

        private readonly CommandService commands;
        private readonly UsageRateLimiter rateLimiter;
        private readonly DiscordLogger discordLogger;
        private readonly ResponseService responseService;
        private readonly string defaultCommandPrefix;

        private readonly Lazy<TowerBanManager> lazyTowerBanManager;
        private TowerBanManager TowerBanManager => lazyTowerBanManager.Value;

        private readonly ulong testChannelId;

        public MessageHandler(IServiceProvider theServices, IOptions<AppSettings> appSettings) : base(theServices)
        {
            commands = services.GetService<CommandService>();
            rateLimiter = services.GetService<UsageRateLimiter>();
            discordLogger = services.GetService<DiscordLogger>();
            responseService = services.GetService<ResponseService>();

            lazyTowerBanManager = new Lazy<TowerBanManager>(() => services.GetService<TowerBanManager>());

            testChannelId = appSettings.Value.Settings.DevelopmentGuild.Test;

            defaultCommandPrefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;

            client.MessageReceived += HandleMessageAsync;
            commands.CommandExecuted += HandleCommandExecution;
        }

        private async Task HandleCommandExecution(Optional<CommandInfo> info, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess && result is ExecuteResult executeResult && info.IsSpecified)
            {
                await context.Channel.SendMessageAsync(executeResult.ErrorReason + executeResult.Exception.StackTrace
                    .SliceFront(1500)
                    .MarkdownCodeBlock("yaml"));
            }
        }

        private async Task HandleMessageAsync(SocketMessage socketMessage)
        {
            if (socketMessage.Author.IsBot || socketMessage.Author.IsWebhook)
            {
                return;
            }

            if (!(socketMessage is SocketUserMessage message))
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
            if (!ReplyToTestServer && message.Content == defaultCommandPrefix + "settest")
            {
                ReplyToTestServer = true;
                await context.Channel.SendMessageAsync("Replying to test channel");
                return;
            }
            // Special test mode to not handle certain messages.
            if (context.IsPrivate)
            {
                if (Program.TestMode && context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
                {
                    return;
                }
            }
            else
            {
                if (!Program.TestMode && !ReplyToTestServer && (context.Channel.Id == testChannelId))
                {
                    return;
                }

                if (Program.TestMode && (context.Channel.Id != testChannelId))
                {
                    return;
                }
            }
            // Chat handling in DM.
            if (context.IsPrivate && !message.HasStringPrefix(defaultCommandPrefix, ref argPos) && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                if (audit())
                {
                    return;
                }

                if (TowerBanManager.IsInSession(context.User))
                {
                    if (Enum.TryParse(context.Message.Content, true, out Tower tower))
                    {
                        _ = commands.Commands
                            .Single(x => x.Name == "BanTower" && x.Parameters.Any(x => x.Type == typeof(Tower)))
                            .ExecuteAsync(context, new object[1] { tower }, null, services);

                        await responseService.SendToChannelAsync(discordLogger.LogChannel, "DM ban tower received:", new EmbedBuilder()
                            .WithAuthor(message.Author)
                            .WithDescription(message.Content)
                            .Build());
                        return;
                    }

                    await context.Channel.SendMessageAsync("Please enter the number or the exact name of the tower given above.");

                    return;
                }

                chatReply(message.Content);
                return;
            }
            else
            {
                if (context.IsPrivate && !message.HasStringPrefix(defaultCommandPrefix, ref argPos) && message.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id))
                {
                    if (audit())
                    {
                        return;
                    }

                    chatReply(message.Content.Replace(client.CurrentUser.Id.ToString(), ""));
                    return;
                }
            }

            if (!message.Author.IsBot && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                using var chatService = services.GetRequiredService<ChatService>();
                await chatService.AddSync(context);
            }

            if (!message.HasMentionPrefix(client.CurrentUser, ref argPos) && message.MentionedUsers.Any(x => x.Id == client.CurrentUser.Id))
            {
                if (audit())
                {
                    return;
                }
                chatReply(message.Content.Replace(client.CurrentUser.Id.ToString(), ""));
                return;
            }

            if (!(message.HasStringPrefix(defaultCommandPrefix, ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            {
                return;
            }

            if (audit())
            {
                return;
            }

            // Command handling
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (context.IsPrivate && message.Author.Id != ApplicationContext.BotOwner.Id)
            {
                await responseService.SendToChannelAsync(discordLogger.LogChannel, "DM command received:", new EmbedBuilder()
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
                    else
                    {
                        if (result.Error != CommandError.ObjectNotFound)
                        {
                            await context.Channel.SendMessageAsync(result.ErrorReason);
                            sendHelp();
                            return;
                        }
                    }
                }
                await context.Channel.SendMessageAsync(result.ErrorReason);
                return;
            }
            else
            {
                if (result.Error == CommandError.UnknownCommand)
                {
                    if (message.HasMentionPrefix(client.CurrentUser, ref argPos))
                    {
                        chatReply(message.Content.Remove(0, argPos));
                        return;
                    }
                }
            }

            bool audit() => rateLimiter.Audit(context, responseService);

            void sendHelp()
                => _ = commands.Commands
                    .Single(x => x.Name == "Help" && x.Parameters.Count == 1)
                    .ExecuteAsync(context, new object[1] { defaultCommandPrefix + commands.Search(context, argPos).Commands.First().Command.Name }, null, services);

            void chatReply(string input)
                => _ = commands.Commands
                    .Single(x => x.Name == "TrashReply")
                    .ExecuteAsync(context, new object[1] { input }, null, services);
        }
    }
}
