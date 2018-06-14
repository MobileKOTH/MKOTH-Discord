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

        private static ProgramConfiguration _config = JsonConvert.DeserializeObject<ProgramConfiguration>(File.ReadAllText(Directories.ConfigFile));
        public static ProgramConfiguration Config { get => _config; }

        public static readonly string BuildVersion = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}." + Config.BuildNumber.ToString().PadLeft(4, '0');
        public static readonly DateTime DeploymentTime = DateTime.Now;

        public static int CurrentTypingSecond = 0;

        public static DiscordSocketClient Client;
        public static IUser BotOwner;

        private static Timer SecondCounter = new Timer(1000);

        public static class MKOTHGuild
        {
            public static SocketGuild Guild;
            public static SocketTextChannel Official, Casual, Suggestions, PlayerID, ModLog, Leave;
            public static SocketRole ChatMods, VIP, Stupid, Member, Peasant, Vassal, Squire, Noble, King, Knight, Pending;
            public static Emote UpArrowEmote, DownArrowEmote;
        }

        public static class TestGuild
        {
            public static SocketGuild Guild;
            public static SocketTextChannel BotTest;
        }

        public static Task Load(DiscordSocketClient client)
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

                SocketGuild guild;

                // MKOTH Discord Server
                MKOTHGuild.Guild = client.GetGuild(271109067261476866UL);
                guild = MKOTHGuild.Guild;

                MKOTHGuild.Official = guild.TextChannels.Single(x => x.Id.Equals(347258242277310465UL));
                MKOTHGuild.Casual = guild.TextChannels.Single(x => x.Id.Equals(347166773642133515UL));
                MKOTHGuild.Suggestions = guild.TextChannels.Single(x => x.Id.Equals(347272877134839810UL));
                MKOTHGuild.PlayerID = guild.TextChannels.Single(x => x.Id.Equals(357201006301282309UL));
                MKOTHGuild.ModLog = guild.TextChannels.Single(x => x.Id.Equals(349960496591667202UL));
                MKOTHGuild.Leave = guild.TextChannels.Single(x => x.Id.Equals(347273557061009409UL));

                MKOTHGuild.ChatMods = guild.Roles.Single(x => x.Name.Contains("Chat Mods"));
                MKOTHGuild.Stupid = guild.Roles.Single(x => x.Name.Contains("I am stupid"));
                MKOTHGuild.VIP = guild.Roles.Single(x => x.Name.Contains("Peers of the Realm"));
                MKOTHGuild.Member = guild.Roles.Single(x => x.Name.Contains("MKOTH Members"));
                MKOTHGuild.Peasant = guild.Roles.Single(x => x.Name.Contains("MKOTH Peasants"));
                MKOTHGuild.Vassal = guild.Roles.Single(x => x.Name.Contains("MKOTH Vassals"));
                MKOTHGuild.Squire = guild.Roles.Single(x => x.Name.Contains("MKOTH Squires"));
                MKOTHGuild.Noble = guild.Roles.Single(x => x.Name.Contains("MKOTH Nobles"));
                MKOTHGuild.King = guild.Roles.Single(x => x.Name.Contains("MKOTH King"));
                MKOTHGuild.Knight = guild.Roles.Single(x => x.Name.Contains("MKOTH Knights"));
                MKOTHGuild.Pending = guild.Roles.Single(x => x.Name.Contains("Pending"));

                MKOTHGuild.UpArrowEmote = guild.Emotes.Single(x => x.Name == "uparrow");
                MKOTHGuild.DownArrowEmote = guild.Emotes.Single(x => x.Name == "downarrow");

                // Test Server
                TestGuild.Guild = client.GetGuild(270838709287387136UL);
                guild = TestGuild.Guild;

                TestGuild.BotTest = guild.TextChannels.Single(x => x.Id.Equals(360352712619065345UL));

                // Player Data
                Player.Load().GetAwaiter().GetResult();

                if (Program.FirstArgument == "Restarted")
                {
                    TestGuild.BotTest.SendMessageAsync("The Bot has restarted");
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

        private static void HandleTimeCounter(object sender, ElapsedEventArgs e)
        {
            CurrentTypingSecond = CurrentTypingSecond > 0 ? CurrentTypingSecond - 1 : 0;
        }

        public struct ProgramConfiguration
        {
            public int BuildNumber { get; set; }
            public string Token { get; set; }
            public List<ulong> Moderators { get; set; }
        }

        public static void IncreaseBuild() => _config.BuildNumber++;
        public static void SaveConfig() => File.WriteAllText(Directories.ConfigFile, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
}
