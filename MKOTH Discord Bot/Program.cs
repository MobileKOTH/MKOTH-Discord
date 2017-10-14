using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net.Providers.WS4Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTH_Discord_Bot
{
    class Program
    {
        public bool TestMode = false;

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

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
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            var message = messageParam as SocketUserMessage;
            if (message.Author.Id == _client.CurrentUser.Id) return; //No reply to self
            Console.WriteLine(message.Timestamp.ToLocalTime() + " User: " + message.Author.Username + "\t Message: " + message.Content);
            if (message == null) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new SocketCommandContext(_client, message);
            if (!TestMode && (context.Guild.Id == 270838709287387136L))
            {
                return;
            }
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
    }
}
