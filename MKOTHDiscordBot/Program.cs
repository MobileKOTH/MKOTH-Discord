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
using System.Configuration;

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
            SecondArgument = args.ElementAtOrDefault(1);
            Console.WriteLine(args.Length > 0 ? "Arguments: " + string.Join(", ", args) : "No start up arguments");
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            Console.WriteLine(RuntimeInformation.ProcessArchitecture);
            Console.WriteLine(RuntimeInformation.OSDescription);

            Chat.LoadHistory();

#if DEBUG
            Console.WriteLine("Debug Build");
            checkForTestMode();
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var buildNumberStr = config.AppSettings.Settings["BuildNumber"].Value;
            var buildNumber = int.Parse(buildNumberStr);
            config.AppSettings.Settings["BuildNumber"].Value = (buildNumber + 1).ToString();
            config.Save(ConfigurationSaveMode.Modified);
            config.SaveAs(System.IO.Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName + @"\App.config", ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
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

            await client.LoginAsync(TokenType.Bot, ApplicationContext.Config.Token);
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

            Handlers.Message.Initialise(services);

            // Discord client events.
            client.MessageReceived += Handlers.Message.Handle;
            client.ReactionAdded += Handlers.Reaction.Handle;
            client.ReactionRemoved += Handlers.Reaction.Handle;
            client.Ready += () => ApplicationContext.Load(client);
            client.UserJoined += (user) => Task.CompletedTask;
            client.UserLeft += Handlers.Leaver.Handle;
            client.Disconnected += (e) => Task.Run(() => FirstArgument = e.Message + e.StackTrace.MarkdownCodeBlock("diff"));

            Timer statustimer = new Timer(15000);
            statustimer.Elapsed += async(_, __) => { if (!TestMode) await Responder.ChangeStatus(client); };
            statustimer.Start();

            SpamWatch.Start();
        }
    }
}
