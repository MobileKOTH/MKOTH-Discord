using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord;
using Discord.WebSocket;

namespace MKOTHDiscordBot
{
    // A data holder class to store global variables and discord context
    public static class Globals
    {
        public static class Directories
        {
            private static class FoldersNames
            {
                public const string
                    DATA = @"\Data\",
                    LOGS = @"\Logs\";
            }

            private static class FileNames
            {
                public const string 
                    CONFIG_JSON = "Config.json",
                    GENERALLOGS_MD = "General Logs.md",
                    ERRORLOGS_MD = "Error Logs.md",
                    CHATLOGS_TXT = "Chat Logs.md",
                    CHATHISTORY_DAT = "ChatHistory.dat";
            }

            /// <summary>
            /// Full directory string paths for an application folder.
            /// </summary>
            public static readonly string 
                Root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\",
                DataFolder = Root + FoldersNames.DATA,
                LogsFolder = Root + FoldersNames.LOGS;

            /// <summary>
            /// Full directory string path of an application file.
            /// </summary>
            public static readonly string 
                ConfigFile = Root + FileNames.CONFIG_JSON,
                GeneralLogsFile = LogsFolder + FileNames.GENERALLOGS_MD,
                ErrorLogsFile = LogsFolder + FileNames.ERRORLOGS_MD,
                ChatLogsFile = LogsFolder + FileNames.CHATLOGS_TXT,
                ChatHistoryFile = DataFolder + FileNames.CHATHISTORY_DAT;
        }

        public static ProgramConfiguration Config = JsonConvert.DeserializeObject<ProgramConfiguration>(File.ReadAllText(Directories.ConfigFile));

        public static readonly string BuildVersion = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}." + Config.BuildNumber.ToString().PadLeft(4, '0');
        public static readonly DateTime DeploymentTime = DateTime.Now;

        public static int CurrentTypingSecond = 0;

        public static DiscordSocketClient Client;
        public static IUser BotOwner;

        private static Timer SecondCounter = new Timer(1000);

        public static class MKOTHGuild
        {
            public static SocketGuild Guild => Client.GetGuild(271109067261476866UL);

            public static SocketTextChannel Official => Guild.TextChannels.Single(x => x.Id.Equals(347258242277310465UL));
            public static SocketTextChannel Casual => Guild.TextChannels.Single(x => x.Id.Equals(347166773642133515UL));
            public static SocketTextChannel Suggestions => Guild.TextChannels.Single(x => x.Id.Equals(347272877134839810UL));
            public static SocketTextChannel PlayerID => Guild.TextChannels.Single(x => x.Id.Equals(357201006301282309UL));
            public static SocketTextChannel ModLog => Guild.TextChannels.Single(x => x.Id.Equals(349960496591667202UL));
            public static SocketTextChannel Leave => Guild.TextChannels.Single(x => x.Id.Equals(347273557061009409UL));

            public static SocketRole ChatMods => Guild.Roles.Single(x => x.Name.Contains("Chat Mods"));
            public static SocketRole VIP => Guild.Roles.Single(x => x.Name.Contains("Peers of the Realm"));
            public static SocketRole Stupid => Guild.Roles.Single(x => x.Name.Contains("I am stupid"));
            public static SocketRole Member => Guild.Roles.Single(x => x.Name.Contains("MKOTH Members"));
            public static SocketRole Peasant => Guild.Roles.Single(x => x.Name.Contains("MKOTH Peasants"));
            public static SocketRole Vassal => Guild.Roles.Single(x => x.Name.Contains("MKOTH Vassals"));
            public static SocketRole Squire => Guild.Roles.Single(x => x.Name.Contains("MKOTH Squires"));
            public static SocketRole Noble => Guild.Roles.Single(x => x.Name.Contains("MKOTH Nobles"));
            public static SocketRole King => Guild.Roles.Single(x => x.Name.Contains("MKOTH King"));
            public static SocketRole Knight => Guild.Roles.Single(x => x.Name.Contains("MKOTH Knights"));
            public static SocketRole Pending => Guild.Roles.Single(x => x.Name.Contains("Pending"));
            public static SocketRole Darrell => Guild.Roles.Single(x => x.Name.Contains("Darrell :)"));
            public static SocketRole Admin => Guild.Roles.Single(x => x.Name.Contains("Admin"));

            public static Emote UpArrowEmote => Guild.Emotes.Single(x => x.Name == "uparrow");
            public static Emote DownArrowEmote => Guild.Emotes.Single(x => x.Name == "downarrow");
        }

        public static class TestGuild
        {
            public static SocketGuild Guild => Client.GetGuild(270838709287387136UL);

            public static SocketTextChannel BotTest => Guild.TextChannels.Single(x => x.Id.Equals(360352712619065345UL));
        }

        public static Task Load(ref DiscordSocketClient client)
        {
            try
            {
                SecondCounter.Elapsed += HandleTimeCounter;
                SecondCounter.Start();

                Client = client;

                // Owner
                client.GetApplicationInfoAsync()
                    .ContinueWith(x =>
                    {
                        BotOwner = x.Result.Owner;
                        Console.WriteLine($"Owner Id: {BotOwner.Id}");
                    });

                // Player Data
                Player.Load().GetAwaiter().GetResult();

                if (Program.FirstArgument == "Restarted")
                {
                    var restartChannel = Client.GetChannel(ulong.Parse(Program.SecondArgument));
                    if (restartChannel != null)
                    {
                        ((SocketTextChannel)restartChannel).SendMessageAsync("Bot has restarted");
                    }
                    else
                    {
                        TestGuild.BotTest.SendMessageAsync("Bot has restarted");
                    }
                }
                else if (Program.FirstArgument != null)
                {
                    TestGuild.BotTest.SendMessageAsync("Some thing happened: " + Program.FirstArgument);
                }

                Logger.Debug(BuildVersion, nameof(BuildVersion));
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Console.WriteLine("Failed loading context!");
                client.LogoutAsync();
                client.StopAsync();
                Console.ReadKey();
                Environment.Exit(0);
            }

            return Task.CompletedTask;
        }

        private static void HandleTimeCounter(object _, ElapsedEventArgs __)
        {
            CurrentTypingSecond = CurrentTypingSecond > 0 ? CurrentTypingSecond - 1 : 0;
        }

        public class ProgramConfiguration
        {
            public int BuildNumber { get; set; }
            public string Token { get; set; }
            public List<ulong> Moderators { get; set; }
        }

        public static void SaveConfig() => File.WriteAllText(Directories.ConfigFile, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
}
