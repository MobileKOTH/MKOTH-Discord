using Newtonsoft.Json;

namespace MKOTHDiscordBot.Properties
{
    using static JsonConvert;

    public class Credentials
    {
        public string TestToken { get; set; }
        public string DiscordToken { get; set; }
        public string AppsScriptAdminKey { get; set; }

        public override string ToString()
        {
            return SerializeObject(this, Formatting.Indented);
        }
    }
}
