using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public class Credentials
    {
        public string DiscordToken { get; set; }
        public List<ulong> Moderators { get; set; }
    }
}
