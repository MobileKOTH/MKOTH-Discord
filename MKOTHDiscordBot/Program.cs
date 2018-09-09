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
        public static bool TestMode = false;

        public static string FirstArgument = null;
        public static string SecondArgument = null;

        private static DiscordSocketClient client;
        private static CommandService commands;
        private static IServiceProvider services;

        public static async Task Main(string[] args)
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

        public async static Task InitialiseAsync()
        {
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            Handlers.Message.Initialise(client, commands, services);

            // Discord client events.
            client.MessageReceived += Handlers.Message.Handle;
            client.ReactionAdded += Handlers.Reaction.Handle;
            client.ReactionRemoved += Handlers.Reaction.Handle;
            client.Ready += () => Globals.Load(ref client);
            client.UserJoined += (user) => HandleChatSaveUpdateMKOTH(user);
            client.UserLeft += Handlers.Leaver.Handle;
            client.Disconnected += (e) => Task.Run(() => FirstArgument = e.Message + e.StackTrace.MarkdownCodeBlock("diff"));

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
    }
}
