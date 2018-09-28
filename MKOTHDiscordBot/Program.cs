using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MKOTHDiscordBot.Handlers;
using MKOTHDiscordBot.Services;

using static MKOTHDiscordBot.Services.ServiceExtensions;

namespace MKOTHDiscordBot
{
    // TODO: Change config file name.
    class Program
    {
        public static bool TestMode = false;

        public static string FirstArgument = null;
        public static string SecondArgument = null;

        public static async Task Main(string[] args)
        {
            FirstArgument = args.FirstOrDefault();
            SecondArgument = args.ElementAtOrDefault(1);

            Console.WriteLine(args.Length > 0 ? "Arguments: " + string.Join(", ", args) : "No start up arguments");
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            Console.WriteLine(RuntimeInformation.ProcessArchitecture);
            Console.WriteLine(RuntimeInformation.OSDescription);

#if DEBUG
            Console.WriteLine("Debug Build");
            CheckForTestMode();
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

            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug,
                AlwaysDownloadUsers = true,
            });

            client.Log += async (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                await Console.Out.WriteLineAsync(msg.ToString());
                Console.ResetColor();
            };

            var commands = new CommandService(new CommandServiceConfig
            {
                LogLevel = LogSeverity.Debug
            });

            commands.Log += async (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                await Console.Out.WriteLineAsync(msg.ToString());
                Console.ResetColor();
            };

            var services = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(commands)
                .ConfigureSingletonServices()
                .AddTransient<ChatService>()
                .BuildServiceProvider()
                .StartForcedInstances();

            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            // Discord client events.
            DiscordClientEventHandlerBase.ConfigureCommonHandlers(services);
            client.UserJoined += (user) => Task.Run(() => Logger.Debug(user.GetDisplayName(), "User Joined")); // TODO
            client.Disconnected += (e) => Task.Run(() => FirstArgument = e.Message + e.StackTrace.MarkdownCodeBlock("diff"));

            await client.LoginAsync(TokenType.Bot, ApplicationContext.Credentials.DiscordToken);
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
}
