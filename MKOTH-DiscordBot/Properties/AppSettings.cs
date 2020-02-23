using System.Collections.Generic;
using Newtonsoft.Json;

namespace MKOTHDiscordBot.Properties
{
    using static JsonConvert;

    public class AppSettings
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public Settings Settings { get; set; }

        public override string ToString()
        {
            return SerializeObject(this, Formatting.Indented);
        }
    }

    public class ConnectionStrings
    {
        public string ChatHistory { get; set; }
        public string ApplicationDb { get; set; }
        public string AppsScript { get; set; }
    }


    public class Settings
    {
        public string DefaultCommandPrefix { get; set; }
        public Developmentguild DevelopmentGuild { get; set; }
        public Productionguild ProductionGuild { get; set; }
    }


    public class Developmentguild
    {
        public ulong Id { get; set; }
        public ulong Test { get; set; }
        public ulong Log { get; set; }
    }

    public class Productionguild
    {
        public ulong Id { get; set; }
        public ulong MemberRole { get; set; }
        public ulong Official { get; set; }
        public ulong Leave { get; set; }
        public ulong Series { get; set; }
        public ulong Ranking { get; set; }

    }

}
