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

namespace MKOTH_Discord_Bot
{
    class Program
    {
        public enum StatusMessages { HELP, INVOLVE, INFO , ENCOURAGE };
        public bool TestMode = false;
        public static bool ReplyToTestServer = true;

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        private Timer timer;
        private StatusMessages status = StatusMessages.HELP;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
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

            string token = File.ReadAllText("token.txt"); // Remember to keep this private!
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        public async Task InstallCommandsAsync()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleCommandAsync;

            timer = new Timer();
            timer.Elapsed += HandleStatusUpdateAsync;
            timer.Interval = 30000; // in miliseconds
            timer.Start();

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
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
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;

            if (message.Author.Id == _client.CurrentUser.Id) return; //No reply to self
            if (message == null) return;

            // Create a Command Context
            var context = new SocketCommandContext(_client, message);

            if (context.IsPrivate)
            {
                var channel = _client.GetGuild(270838709287387136).GetChannel(360352712619065345) as ISocketMessageChannel;
                var embed = new EmbedBuilder().WithAuthor(message.Author).WithDescription(message.Content).Build();
                await channel.SendMessageAsync("DM Received: \n", embed: embed);
                Console.ForegroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(message.Timestamp.ToLocalTime() + "\tUser: " + message.Author.Username + "\nMessage: " + message.Content);
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message.Timestamp.ToLocalTime() + "\tUser: " + message.Author.Username + "\nMessage: " + message.Content);
            Console.ResetColor();

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;

            if (!ReplyToTestServer && message.Content == ".settest")
            {
                ReplyToTestServer = !ReplyToTestServer;
                await context.Channel.SendMessageAsync("Replying to test server");
                return;
            }
            if (!TestMode && !ReplyToTestServer && (context.Guild.Id == 270838709287387136L)) return;
            else if (TestMode && (context.Guild.Id != 270838709287387136L)) return;

            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async void HandleStatusUpdateAsync(object sender, EventArgs e)
        {
            status = (int)status + 1 < (Enum.GetValues(typeof(StatusMessages)).Length) ? status + 1 : 0;
            Console.WriteLine(status);
            switch (status)
            {
                case StatusMessages.HELP:
                    await _client.SetGameAsync("a Series | .mkothhelp for help");
                    break;

                case StatusMessages.INVOLVE:
                    await _client.SetGameAsync("with MKOTH Members!");
                    break;

                case StatusMessages.INFO:
                    await _client.SetGameAsync("MKOTH | .info for information");
                    break;

                case StatusMessages.ENCOURAGE:
                    await _client.SetGameAsync("Ranked Series for ELO Display!");
                    break;
            }
        }
    }
}
