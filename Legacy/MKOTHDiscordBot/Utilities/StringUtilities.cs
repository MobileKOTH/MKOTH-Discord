using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public static class StringUtilities
    {
        public static string LineJoin(IEnumerable<string> strings, string seperator = "\n")
            => string.Join(seperator, strings);
    }
}
