using System.Collections.Generic;

namespace MKOTHDiscordBot
{
    public class Credentials
    {
        public string DiscordToken { get; set; }
        public List<ulong> Moderators { get; set; }
    }
}
