using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MKOTHDiscordBot.Handlers;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot
{
    class Program
    {
        public static bool TestMode = false;
        public static string FirstArgument = null;
        public static string SecondArgument = null;

        static async Task Main(string[] args)
        {
            FirstArgument = args.FirstOrDefault();
            SecondArgument = args.ElementAtOrDefault(1);
            Console.Title = Assembly.GetAssembly(typeof(Program)).GetName().Name;
            Console.WriteLine(args.Length > 0 ? "Arguments: " + string.Join(", ", args) : "No start up arguments");
            Console.WriteLine(ApplicationContext.GetFrameworkDescription());
            Console.WriteLine($"CLR {Environment.Version}");
            Console.WriteLine(RuntimeInformation.ProcessArchitecture);
            Console.WriteLine(RuntimeInformation.OSDescription);
            Console.WriteLine($"Discord {ApplicationContext.DiscordVersion}");

#if DEBUG
            var version = VersionBump.Bump();
            Console.WriteLine($"Current Assembly {version}");
            CheckForTestMode();
#else
            Console.WriteLine($"Release Build {ApplicationContext.AssemblyVersion}");
#endif

            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
            });

            var commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug
            });

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

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory + @"\Properties")
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile("credentials.json", false, true)
                .Build();

            var services = new ServiceCollection()
                .Configure<AppSettings>(config)
                .Configure<Credentials>(config.GetSection("credentials"))
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<DiscordLogger>()
                .AddSingleton<ErrorResolver>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<ResponseService>()
                .AddSingleton<UsageRateLimiter>()
                .AddSingleton<SubmissionRateLimiter>()
                .AddSingleton<ActivityCycler>()
                .AddTransient<ChatService>()
                .AddTransient<IssueTracker>()
                .AddSingleton<SeriesService>()
                .BuildServiceProvider();

            var credentials = services.GetScoppedSettings<Credentials>();
            var appsettings = services.GetScoppedSettings<AppSettings>();

            // Discover all of the commands in this assembly and load them.
            var modules = await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            // Discord client events.
            DiscordClientEventHandlerBase.ConfigureCommonHandlers(services);
            client.Disconnected += (e) => Task.Run(() => FirstArgument = e.Message + e.StackTrace.MarkdownCodeBlock("diff"));

            await client.LoginAsync(TokenType.Bot, credentials.DiscordToken);
            await client.StartAsync();

            await Task.Delay(-1);
        }

#if DEBUG
        private static void CheckForTestMode()
        {
            string input;
            do
            {
                Console.WriteLine("Is this a test mode? Y/N");
                input = Console.ReadLine();
            }
            while (!input.StartsWithIgnoreCase("Y") && !input.StartsWithIgnoreCase("N") && input != "");

            if (input.StartsWithIgnoreCase("Y") || input == "")
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
}
