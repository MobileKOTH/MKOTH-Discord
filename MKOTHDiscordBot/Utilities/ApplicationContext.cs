using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace MKOTHDiscordBot
{
    // A data holder class to store global variables and discord context
    public static class ApplicationContext
    {
        public static IReadOnlyList<Type> CommonClasses => 
            Assembly.GetEntryAssembly()
            .GetTypes()
            .Where(x => x.IsClass && !x.IsNested).ToImmutableArray();

        public static Credentials Credentials => JsonConvert.DeserializeObject<Credentials>(File.ReadAllText(Directories.ConfigFile));

        public static string BuildVersion => $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}." +  ConfigurationManager.AppSettings["BuildNumber"].PadLeft(4, '0');
        public static readonly DateTime DeploymentTime = DateTime.Now;

        public static DiscordSocketClient DiscordClient;
        public static IUser BotOwner;

        public static class MKOTHGuild
        {
            public static SocketGuild Guild => DiscordClient.GetGuild(271109067261476866UL);

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
            public static SocketGuild Guild => DiscordClient.GetGuild(270838709287387136UL);

            public static SocketTextChannel BotTest => Guild.TextChannels.Single(x => x.Id.Equals(360352712619065345UL));
        }
    }
}
