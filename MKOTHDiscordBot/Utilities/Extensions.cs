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
        #region Int -------------------------------------------------------------------------------
        public static bool IsInRange(this int number, int lower, int upper)
        {
            return (number >= lower && number <= upper);
        }

        public static bool IsInRangeOffset(this int number, int reference, int offset)
        {
            return IsInRange(number, reference - offset, reference + offset);
        }
        #endregion

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
            str = limit >= str.Length ? str : leftOverCover + str.Substring(str.Length - limit - leftOverCover.Length);
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

        public static int GetWordCount(this String str)
        {
            return str.Split(' ').Length;
        }
        #endregion

        #region IEnumberable ----------------------------------------------------------------------
        public static T SelectRandom<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(((int)((new Random().NextDouble() * collection.Count()))));
        }

        public static List<List<T>> Split<T>(this List<T> list, int splitCount)
        {
            int size = list.Count / splitCount;
            var lists = new List<List<T>>();
            for (int i = 0; i + 1 < list.Count; i+= size)
            {
                lists.Add(list.GetRange(i, Math.Min(size, list.Count - i)));
            }
            return lists;
        }
        #endregion

        #region TimeSpan --------------------------------------------------------------------------
        public static string AsRoundedDuration(this TimeSpan timespan)
        {
            return timespan.TotalDays >= 370 * 2 ? (int)timespan.TotalDays / 365+ " years" :
                timespan.TotalDays > 60 ? (int)timespan.TotalDays / 30 + " months" :
                timespan.TotalHours >= 48 ? (int)timespan.TotalHours / 24 + " days" :
                timespan.TotalMinutes >= 120 ? (int)timespan.TotalMinutes / 60 + " hours" :
                timespan.TotalSeconds >= 120 ? (int)timespan.TotalSeconds / 60 + " minutes" :
                (int)timespan.TotalSeconds + " seconds";
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
