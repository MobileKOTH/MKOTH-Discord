using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System.IO;
using System.Diagnostics;
using System.Timers;

namespace MKOTH_Discord_Bot.Utilities
{
    //A data holder class to store global variables and discord context
    public static class ContextPools
    {
        public static readonly string DataPath = 
            Directory.GetParent(Directory.GetCurrentDirectory()).FullName + @"\Data\";
        public static readonly ProgramConfiguration Config = 
            Newtonsoft.Json.JsonConvert.DeserializeObject<ProgramConfiguration>(File.ReadAllText(ContextPools.DataPath + "Config.json"));
        public static readonly string BuildVersion =
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major}." +
            $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor}." +
            Config.Buildnumber.ToString().PadLeft(4, '0');
        public static readonly DateTime DeploymentTime = DateTime.Now;

        public static int CurrentTypingSecond = 0;

        public static IUser BotOwner;

        private static Timer SecondCounter = new Timer(1000);

        public static class MKOTHGuild
        {
            public static SocketGuild Guild;
            public static SocketChannel Official, Casual, PlayerID;
            public static SocketRole ChatMods, Member, Peasant, Vassal, Squire, Noble, King;
        }

        public static class TestGuild
        {
            public static SocketGuild Guild;
            public static SocketChannel BotTest;
        }

        public static async void Load(DiscordSocketClient client)
        {
            SecondCounter.Elapsed += HandleTimeCounter;

            SocketGuild guild;

            //MKOTH Discord Server
            MKOTHGuild.Guild = client.GetGuild(271109067261476866UL);
            guild = MKOTHGuild.Guild;

            MKOTHGuild.Official = guild.Channels.FirstOrDefault(x => x.Id.Equals(347258242277310465UL));
            MKOTHGuild.Casual = guild.Channels.FirstOrDefault(x => x.Id.Equals(347166773642133515UL));
            MKOTHGuild.PlayerID = guild.Channels.FirstOrDefault(x => x.Id.Equals(357201006301282309UL));

            MKOTHGuild.ChatMods = guild.Roles.FirstOrDefault(x => x.Name.Contains("Chat Mods"));
            MKOTHGuild.Member = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Members"));
            MKOTHGuild.Peasant = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Peasants"));
            MKOTHGuild.Vassal = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Vassals"));
            MKOTHGuild.Squire = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Squire"));
            MKOTHGuild.Noble = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH Nobles"));
            MKOTHGuild.King = guild.Roles.FirstOrDefault(x => x.Name.Contains("MKOTH King"));

            //Test Server
            TestGuild.Guild = client.GetGuild(270838709287387136UL);
            guild = TestGuild.Guild;

            TestGuild.BotTest = guild.Channels.FirstOrDefault(x => x.Id.Equals(360352712619065345UL));

            //Owner
            BotOwner = (await client.GetApplicationInfoAsync()).Owner;

            Logger.Debug(BuildVersion, nameof(BuildVersion));
        }

        private static void HandleTimeCounter(object sender, ElapsedEventArgs e)
        {
            CurrentTypingSecond = CurrentTypingSecond > 0 ? CurrentTypingSecond - 1 : 0;
        }

        public class ProgramConfiguration
        {
            private int buildnumber;
            private string token;

            public int Buildnumber { get => buildnumber; set => buildnumber = value; }
            public string Token { get => token; set => token = value; }

            public static void Save()
            {
                if (Program.TestMode)
                {
                    Config.buildnumber += 1;
                }
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(Config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(DataPath + "Config.json", json);
            }
        }
    }
}
