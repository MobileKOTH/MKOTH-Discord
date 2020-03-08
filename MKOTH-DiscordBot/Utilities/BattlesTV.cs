using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MKOTHDiscordBot.Utilities
{
    public static class BattlesTV
    {
        public static bool IsValidReplayIdFormat(string id)
        {
            return Regex.IsMatch(id, "^[A-Z]{7}$");
        }
    }
}
