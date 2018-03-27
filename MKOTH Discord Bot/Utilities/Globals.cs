using System;
using System.Linq;
using System.IO;
using System.Timers;
using System.Threading.Tasks;
using System.Reflection;
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
            public static class FoldersNames
            {
                public const string DATA = @"\Data\";
                public const string LOGS = @"\Logs\";
                public const string CHAT = @"\Logs\";
            }

            public static class FileNames
            {
                public const string CONFIG_JSON = "Config.json";
                public const string GENERALLOGS_MD = "General Logs.md";
                public const string CHATLOGS_TXT = "Chat Logs.md";
            }

            public static readonly string Root = Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\";
            public static readonly string DataFolder = Root + FoldersNames.DATA;
            public static readonly string ChatFolder = DataFolder + FoldersNames.CHAT;
            public static readonly string LogsFolder = Root + FoldersNames.LOGS;

            public static readonly string ConfigFile = Root + FileNames.CONFIG_JSON;
            public static readonly string GeneralLogsFile = LogsFolder + FileNames.GENERALLOGS_MD;
            public static readonly string ChatLogsFile = LogsFolder + FileNames.CHATLOGS_TXT;
        }

        private static ProgramConfiguration _config = JsonConvert.DeserializeObject<ProgramConfiguration>(File.ReadAllText(Directories.ConfigFile));
        public static ProgramConfiguration Config { get => _config; }

        public static readonly string BuildVersion = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}." + Config.BuildNumber.ToString().PadLeft(4, '0');
        public static readonly DateTime DeploymentTime = DateTime.Now;

        public static int CurrentTypingSecond = 0;

        public static IUser BotOwner;

        private static Timer SecondCounter = new Timer(1000);

        public static class MKOTHGuild
        {
            public static SocketGuild Guild;
            public static SocketChannel Official, Casual, PlayerID, ModLog;
            public static SocketRole ChatMods, Stupid, Member, Peasant, Vassal, Squire, Noble, King;
        }

        public static class TestGuild
        {
            public static SocketGuild Guild;
            public static SocketChannel BotTest;
        }

        public static void Load(DiscordSocketClient client)
        {
            try
            {
                SecondCounter.Elapsed += HandleTimeCounter;
                SecondCounter.Start();

                SocketGuild guild;

                // MKOTH Discord Server
                MKOTHGuild.Guild = client.GetGuild(271109067261476866UL);
                guild = MKOTHGuild.Guild;

                MKOTHGuild.Official = guild.Channels.Single(x => x.Id.Equals(347258242277310465UL));
                MKOTHGuild.Casual = guild.Channels.Single(x => x.Id.Equals(347166773642133515UL));
                MKOTHGuild.PlayerID = guild.Channels.Single(x => x.Id.Equals(357201006301282309UL));
                MKOTHGuild.ModLog = guild.Channels.Single(x => x.Id.Equals(349960496591667202));

                MKOTHGuild.ChatMods = guild.Roles.Single(x => x.Name.Contains("Chat Mods"));
                MKOTHGuild.Stupid = guild.Roles.Single(x => x.Name.Contains("I am stupid"));
                MKOTHGuild.Member = guild.Roles.Single(x => x.Name.Contains("MKOTH Members"));
                MKOTHGuild.Peasant = guild.Roles.Single(x => x.Name.Contains("MKOTH Peasants"));
                MKOTHGuild.Vassal = guild.Roles.Single(x => x.Name.Contains("MKOTH Vassals"));
                MKOTHGuild.Squire = guild.Roles.Single(x => x.Name.Contains("MKOTH Squire"));
                MKOTHGuild.Noble = guild.Roles.Single(x => x.Name.Contains("MKOTH Nobles"));
                MKOTHGuild.King = guild.Roles.Single(x => x.Name.Contains("MKOTH King"));

                // Test Server
                TestGuild.Guild = client.GetGuild(270838709287387136UL);
                guild = TestGuild.Guild;

                TestGuild.BotTest = guild.Channels.Single(x => x.Id.Equals(360352712619065345UL));

                // Owner
                BotOwner = client.GetApplicationInfoAsync().Result.Owner;

                // Player Data
                var playerloadtask = PlayerCode.Load();

                Logger.Debug(BuildVersion, nameof(BuildVersion));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.AddLine() + e.StackTrace);
                Console.WriteLine("Failed loading context!");
                client.LogoutAsync();
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        private static void HandleTimeCounter(object sender, ElapsedEventArgs e)
        {
            CurrentTypingSecond = CurrentTypingSecond > 0 ? CurrentTypingSecond - 1 : 0;
        }

        public struct ProgramConfiguration
        {
            public int BuildNumber { get; set; }
            public string Token { get; set; }
        }

        public static void IncreaseBuild() => _config.BuildNumber++;
        public static void SaveConfig() => File.WriteAllText(Directories.ConfigFile, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
}
