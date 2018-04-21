using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Timers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Modules;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot
{
    class Program
    {
        public static bool ReplyToTestServer = true;
        public static bool TestMode = false;
        public static string FirstArgument = null;

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public static void Main(string[] args) => new Program().MainAsync(args).GetAwaiter().GetResult();

        public async Task MainAsync(string[] args)
        {
            FirstArgument = args.FirstOrDefault();
            Console.WriteLine(FirstArgument ?? "No Startup Arguments");
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            Console.WriteLine(RuntimeInformation.ProcessArchitecture);
            Console.WriteLine(RuntimeInformation.OSDescription);
            Chat.LoadHistory();
#if DEBUG
            Console.WriteLine("Debug Build");
            checkForTestMode();
            Globals.IncreaseBuild();
            Globals.SaveConfig();
#else
            Console.WriteLine("Release Build");
#endif
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
            });
            _commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug
            });
            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();
            await InitialiseAsync();

            _client.Log += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg.ToString());
                Console.ResetColor();
                return Task.CompletedTask;
            };

            _commands.Log += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(msg.ToString());
                Console.ResetColor();
                return Task.CompletedTask;
            };

            await _client.LoginAsync(TokenType.Bot, Globals.Config.Token);
            await _client.StartAsync();

            await Task.Delay(-1);
#if DEBUG
            void checkForTestMode()
            {
                string input;
                do
                {
                    Console.WriteLine("Is this a test mode? Y/N");
                    input = Console.ReadLine();
                }
                while (input != "Y" && input != "N" && input != "");

                if (input == "Y" || input == "")
                {
                    TestMode = true;
                    Console.WriteLine("Set to test mode.");
                }
                else
                {
                    Console.WriteLine("Not a test mode.");
                }
            }
#endif
        }

        public async Task InitialiseAsync()
        {
            // Discord client events.
            _client.MessageReceived += HandleMessageAsync;
            _client.Ready += () => Globals.Load(_client);
            _client.UserJoined += (user) => { if (user.Guild.Id == Globals.MKOTHGuild.Guild.Id) HandleChatSaveUpdateMKOTH(); return Task.CompletedTask;};

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Timer statustimer = new Timer();
            statustimer.Elapsed += async(sender, evt) => { if (!TestMode) await Responder.ChangeStatus(_client); };
            statustimer.Interval = 15000;
            statustimer.Start();

            Timer savechatupdatemkothtimer = new Timer();
            savechatupdatemkothtimer.Elapsed += (sender, evt) => HandleChatSaveUpdateMKOTH();
            savechatupdatemkothtimer.Interval = 60000;
            savechatupdatemkothtimer.Start();

            Timer downloadplayerdatatimer = new Timer();
            downloadplayerdatatimer.Elapsed += async (sender, evt) => { if (!TestMode) await Player.Load(); };
            downloadplayerdatatimer.Interval = 300000;
            downloadplayerdatatimer.Start();

            SpamWatch.Start();
        }

        private void HandleChatSaveUpdateMKOTH()
        {
            Chat.SaveHistory();
            if (!TestMode)
            {
                var task = Management.UpdateMKOTHAsync(null);
            }
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            // No handle to own or null message.
            if (message.Author.Id == _client.CurrentUser.Id) return;
            if (message == null) return;

            var context = new SocketCommandContext(_client, message);
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
                if (!TestMode && !ReplyToTestServer && (context.Guild.Id == Globals.TestGuild.Guild.Id)) return;
                if (TestMode && (context.Guild.Id == Globals.MKOTHGuild.Guild.Id)) return;
            }
            else if (context.IsPrivate && context.User.Id != Globals.BotOwner.Id)
            {
                if (TestMode) return;
            }
            // Chat handling in DM.
            if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (await SpamWatch.Watch(context)) return;
                Task.Run(async () => await Chat.ReplyAsync(context, message.Content)).Start();
                return;
            }
            else if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                if (await SpamWatch.Watch(context)) return;
                Task.Run(async () => await Chat.ReplyAsync(context, message.Content.Remove(0, argPos))).Start();
                return;
            }

            if (!message.Author.IsBot && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) new Chat(context);

            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            if (await SpamWatch.Watch(context)) return;
            // Command handling.
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (context.IsPrivate && message.Author.Id != Globals.BotOwner.Id)
            {
                await Responder.SendToChannel(Globals.TestGuild.BotTest, "DM command received:", new EmbedBuilder()
                    .WithAuthor(message.Author)
                    .WithDescription(message.Content)
                    .Build());
            }
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                if (result.Error == CommandError.BadArgCount || result.Error == CommandError.ParseFailed || result.Error == CommandError.ObjectNotFound)
                {
                    if (_commands.Search(context, argPos).Commands.Count(x => x.Command.Remarks != null) > 0 && result.Error == CommandError.ObjectNotFound)
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

                async void sendHelp()
                {
                    await _commands.Commands
                        .Where(x => x.Name == "Help")
                        .Single(x => x.Parameters.Count == 1)
                        .ExecuteAsync(context, new object[1] { "." + _commands.Search(context, argPos).Commands.First().Command.Name }, null, _services);
                }
            }
            else if (result.Error == CommandError.UnknownCommand)
            {// Chat reply.
                if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) && !context.IsPrivate)
                {
                    Task.Run(async () => await Chat.ReplyAsync(context, message.Content.Remove(0, argPos))).Start();
                }
            }
        }
    }
}
