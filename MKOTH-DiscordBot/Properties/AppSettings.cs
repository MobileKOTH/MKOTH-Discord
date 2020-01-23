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
    }

    public class Settings
    {
        public string DefaultCommandPrefix { get; set; }
    }
}
