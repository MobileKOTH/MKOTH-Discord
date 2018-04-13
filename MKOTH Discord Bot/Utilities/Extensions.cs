using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot
{
    public static class Extensions
    {
        #region String ----------------------------------------------------------------------------
        public static string AddLine(this String str)
        {
            str += Environment.NewLine;
            return str;
        }

        public static string AddMarkDownLine(this String str)
        {
            str += "  ".AddLine();
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

        public static string SliceBack(this String str, int limit, string leftOverCover = "...")
        {
            str = str.Length > limit ? str.Substring(0, limit - leftOverCover.Length) + leftOverCover : str;
            return str;
        }

        public static string SliceFront(this String str, int limit, string leftOverCover = "...")
        {
            str = limit >= str.Length ? str : leftOverCover + str.Substring(str.Length - limit);
            return str;
        }

        public static string MarkdownCodeBlock(this String str, string lang = null)
        {
            return $"```{(lang == null ? "" : lang + "\n")}{str}\n```";
        }

        public static string MarkdownCodeLine(this String str)
        {
            return $"`{str}`";
        }
        #endregion

        #region IGuildUser ------------------------------------------------------------------------
        public static string GetDisplayName(this IGuildUser user)
        {
            return user.Nickname ?? user.Username;
        }
        #endregion

        #region CommandInfo -----------------------------------------------------------------------
        public static string GetCommandParametersInfo(this CommandInfo command)
        {
            string info = "";
            command.Parameters.ToList().ForEach(x =>
            {
                info += $"<{x.Name}> ";
            });
            return info;
        }
        #endregion
    }
}
