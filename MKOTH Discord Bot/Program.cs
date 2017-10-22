using System;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net.Providers.WS4Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using System.Timers;
using MKOTH_Discord_Bot.Utilities;

namespace MKOTH_Discord_Bot
{
    class Program
    {
        public static bool ReplyToTestServer = true;
        public static ulong OwnerID = 0;

        public bool TestMode = false;


        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private StatusMessages status = StatusMessages.HELP;


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

            await _client.LoginAsync(TokenType.Bot, Config.Token);
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
                } while (input != "Y" && input != "N" && input != "");
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

            Timer statustimer = new Timer();
            statustimer.Elapsed += HandleStatusUpdateAsync;
            statustimer.Interval = 30000; // in miliseconds
            statustimer.Start();

            Timer savechattimer = new Timer();
            savechattimer.Elapsed += HandleChatSave;
            savechattimer.Interval = 60000;
            savechattimer.Start();
        }

        private Task LoadContext()
        {
            ContextPools.Load(_client);
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
            else if ( context.IsPrivate && !(message.HasCharPrefix('.', ref argPos)) && message.HasMentionPrefix(_client.CurrentUser, ref argPos))
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

        private void HandleStatusUpdateAsync(object sender, EventArgs e)
        {
            Responder.ChangeStatus(status ,_client);
        }

        private void HandleChatSave(object sender, EventArgs e)
        {
            Chat.SaveHistory();
        }

        private Task Log(LogMessage msg)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg.ToString());
            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
