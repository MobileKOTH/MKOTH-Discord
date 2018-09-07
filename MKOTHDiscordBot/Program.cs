using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MKOTHDiscordBot.Modules;
using MKOTHDiscordBot.Utilities;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

namespace MKOTHDiscordBot
{
    class Program
    {
        public static bool ReplyToTestServer = true;
        public static bool TestMode = false;
        public static string FirstArgument = null;
        public static string SecondArgument = null;

        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

        public static void Main(string[] args) 
            => new Program().MainAsync(args)
            .GetAwaiter()
            .GetResult();

        public async Task MainAsync(string[] args)
        {
            FirstArgument = args.FirstOrDefault();
            SecondArgument = args.Length > 1 ? args[1] : null;
            Console.WriteLine(args.Length > 0 ? "Arguments: " + string.Join(", ", args) : "No start up arguments");
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            Console.WriteLine(RuntimeInformation.ProcessArchitecture);
            Console.WriteLine(RuntimeInformation.OSDescription);

            Chat.LoadHistory();

#if DEBUG
            Console.WriteLine("Debug Build");
            checkForTestMode();
            Globals.Config.BuildNumber++;
            Globals.SaveConfig();
#else
            Console.WriteLine("Release Build");
#endif

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
            });
            commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug
            });
            services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .BuildServiceProvider();
            await InitialiseAsync();

            client.Log += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(msg.ToString());
                Console.ResetColor();
                return Task.CompletedTask;
            };

            commands.Log += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(msg.ToString());
                Console.ResetColor();
                return Task.CompletedTask;
            };

            await client.LoginAsync(TokenType.Bot, Globals.Config.Token);
            await client.StartAsync();

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
            client.MessageReceived += HandleMessageAsync;
            client.ReactionAdded += Handlers.Reaction.Handle;
            client.ReactionRemoved += Handlers.Reaction.Handle;
            client.Ready += () => Globals.Load(ref client);
            client.UserJoined += (user) => HandleChatSaveUpdateMKOTH(user);
            client.UserLeft += Handlers.Leaver.Handle;
            client.Disconnected += (e) => Task.Run(() => FirstArgument = e.Message + e.StackTrace.MarkdownCodeBlock("diff"));

            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Timer statustimer = new Timer();
            statustimer.Elapsed += async(_, __) => { if (!TestMode) await Responder.ChangeStatus(client); };
            statustimer.Interval = 15000;
            statustimer.Start();

            Timer savechatupdatemkothtimer = new Timer();
            savechatupdatemkothtimer.Elapsed += (_, __) => HandleChatSaveUpdateMKOTH();
            savechatupdatemkothtimer.Interval = 60000;
            savechatupdatemkothtimer.Start();

            Timer downloadplayerdatatimer = new Timer();
            downloadplayerdatatimer.Elapsed += async (_, __) => { if (!TestMode) await Player.Load(); };
            downloadplayerdatatimer.Interval = 300000;
            downloadplayerdatatimer.Start();

            SpamWatch.Start();

            Task HandleChatSaveUpdateMKOTH(SocketGuildUser user = null)
            {
                if (user != null && user?.Guild.Id != Globals.MKOTHGuild.Guild.Id)
                {
                    return Task.CompletedTask;
                }
                Chat.SaveHistory();
                if (!TestMode)
                {
                    var task = Management.UpdateMKOTHAsync();
                }
                return Task.CompletedTask;
            }
        }

        private async Task HandleMessageAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            // No handle to own or null message.
            if (message.Author.Id == client.CurrentUser.Id) return;
            if (message == null) return;

            var context = new SocketCommandContext(client, message);
            int argPos = 0;

            Action rateLimitMessage = () => _ = Responder.SendToContext(context, "You are now rate limited");
            
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
            if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                if (SpamWatch.Watch(message.Author.Id, rateLimitMessage)) return;
                _ = Chat.ReplyAsync(context, message.Content);
                return;
            }
            else if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                if (SpamWatch.Watch(message.Author.Id, rateLimitMessage)) return;
                _ = Chat.ReplyAsync(context, message.Content.Remove(0, argPos));
                return;
            }

            if (!message.Author.IsBot && !message.HasMentionPrefix(client.CurrentUser, ref argPos)) new Chat(context);

            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            if (SpamWatch.Watch(message.Author.Id, rateLimitMessage)) return;
            // Command handling.
            var result = await commands.ExecuteAsync(context, argPos, services);
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

                void sendHelp()
                {
                    commands.Commands
                        .Where(x => x.Name == "Help")
                        .Single(x => x.Parameters.Count == 1)
                        .ExecuteAsync(context, new object[1] { "." + commands.Search(context, argPos).Commands.First().Command.Name }, null, services);
                }
            }
            else if(result.Error == CommandError.Unsuccessful || result.Error == CommandError.Exception)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
            else if (result.Error == CommandError.UnknownCommand)
            {// Chat reply.
                if (message.HasMentionPrefix(client.CurrentUser, ref argPos) && !context.IsPrivate)
                {
                    _ = Chat.ReplyAsync(context, message.Content.Remove(0, argPos));
                }
            }
        }
    }
}
