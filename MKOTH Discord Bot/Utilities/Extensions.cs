using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public static class Extensions
    {
        public static string AddLine(this String str)
        {
            str += Environment.NewLine;
            return str;
        }

        public static string AddTab(this String str)
        {
            str += "\t";
            return str;
        }

        public static string AddSpace(this String str)
        {
            str += " ";
            return str;
        }
    }
}
