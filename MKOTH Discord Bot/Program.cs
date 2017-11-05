using System;
using System.Timers;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net.Providers.WS4Net;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot
{
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);
        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;
        enum CtrlType { CTRL_C_EVENT = 0, CTRL_BREAK_EVENT = 1, CTRL_CLOSE_EVENT = 2, CTRL_LOGOFF_EVENT = 5, CTRL_SHUTDOWN_EVENT = 6}


        public static bool ReplyToTestServer = true;
        public static bool TestMode = false;
        public static ulong OwnerID = 0;


        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            Chat.LoadHistory();

            checkfortestmode();

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                WebSocketProvider = WS4NetProvider.Instance,
                LogLevel = LogSeverity.Debug
            });
            _commands = new CommandService();
            _services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .BuildServiceProvider();
            await InstallCommandsAsync();

            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot, ContextPools.Config.Token);
            await _client.StartAsync();
            OwnerID = (await _client.GetApplicationInfoAsync()).Owner.Id;
            Console.WriteLine(OwnerID);

            await Task.Delay(-1);

            void checkfortestmode()
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
        }

        public async Task InstallCommandsAsync()
        {
            _client.MessageReceived += HandleCommandAsync;
            _client.Ready += LoadContext;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            Timer statustimer = new Timer();
            statustimer.Elapsed += HandleStatusUpdateAsync;
            statustimer.Interval = 30000; // in miliseconds
            statustimer.Start();

            Timer savechatupdatemkothtimer = new Timer();
            savechatupdatemkothtimer.Elapsed += HandleChatSaveUpdateMKOTH;
            savechatupdatemkothtimer.Interval = 60000;
            savechatupdatemkothtimer.Start();

            Timer downloadplayerdatatimer = new Timer();
            downloadplayerdatatimer.Elapsed += HandlePlayerDataDownload;
            downloadplayerdatatimer.Interval = 300000;
            downloadplayerdatatimer.Start();
        }

        private async void HandlePlayerDataDownload(object sender, ElapsedEventArgs e)
        {
            if (!TestMode)
            {
                await PlayerCode.Load();
            }
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:
                    ContextPools.ProgramConfiguration.Save();
                    return true;
                default:
                    return false;
            }
        }

        private async void HandleStatusUpdateAsync(object sender, EventArgs e)
        {
            if (!TestMode)
            {
                await Responder.ChangeStatus(_client);
            }
        }

        private void HandleChatSaveUpdateMKOTH(object sender, EventArgs e)
        {
            Chat.SaveHistory();
            if (!TestMode)
            {
                var task = Management.UpdateMKOTH(null);
            }
        }

        private Task LoadContext()
        {
            ContextPools.Load(_client);
            return Task.CompletedTask;
        }

        private Task Log(LogMessage msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg.ToString());
            Console.ResetColor();
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            if (message.Author.Id == _client.CurrentUser.Id) return; //No handle to own message
            if (message == null) return;

            var context = new SocketCommandContext(_client, message);
            int argPos = 0;

            if (context.IsPrivate)
            {
                var channel = _client.GetGuild(270838709287387136).GetChannel(360352712619065345) as ISocketMessageChannel;
                var embed = new EmbedBuilder().WithAuthor(message.Author).WithDescription(message.Content).Build();
                if (!(context.User.Id == OwnerID))
                {
                    try
                    {
                        await channel.SendMessageAsync("DM Received: \n", embed: embed);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.StackTrace);
                    }
                }
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(message.Timestamp.ToLocalTime() + "\tUser: " + message.Author.Username + "\nMessage: " + message.Content);
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine(message.Timestamp.ToLocalTime() + "\tUser: " + message.Author.Username + "\nMessage: " + message.Content);
                Console.ResetColor();
            }

            if (!ReplyToTestServer && message.Content == ".settest")
            {
                ReplyToTestServer = true;
                await context.Channel.SendMessageAsync("Replying to test server");
                return;
            }

            if (!context.IsPrivate)
            {
                if (!TestMode && !ReplyToTestServer && (context.Guild.Id == 270838709287387136UL)) return;
                if (TestMode && (context.Guild.Id == 271109067261476866UL)) return;
            }
            else if (context.IsPrivate && context.User.Id != OwnerID)
            {
                if (TestMode) return;
            }

            if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await Chat.Reply(context, message.Content);
            }
            else if (context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && message.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                await Chat.Reply(context, message.Content.Remove(0, argPos));
            }

            if (!message.Author.IsBot && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) new Chat(context);

            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
                return;
            }

            if (message.HasMentionPrefix(_client.CurrentUser, ref argPos) && !context.IsPrivate)
            {
                string msg = message.Content.Remove(0, argPos);
                await Chat.Reply(context, msg);
            }
        }
    }
}
