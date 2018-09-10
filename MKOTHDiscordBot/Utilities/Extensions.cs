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
        #region IComparable -------------------------------------------------------------------------------
        public static bool IsInRange(this IComparable number, IComparable lower, IComparable upper)
            => number.CompareTo(lower) >= 0 && number.CompareTo(upper) <= 0;

        public static bool IsInRangeOffset(this IComparable number, IComparable reference, IComparable offset)
            => IsInRange(number, Convert.ToDecimal(reference) - Convert.ToDecimal(offset), Convert.ToDecimal(reference) + Convert.ToDecimal(offset));
        #endregion

        #region String ----------------------------------------------------------------------------
        public static string AddLine(this string str)
        {
            str += Environment.NewLine;
            return str;
        }

        public static string AddMarkDownLine(this string str)
        {
            str += "  ".AddLine();
            return str;
        }

        public static string AddTab(this string str)
        {
            str += "\t";
            return str;
        }

        public static string AddSpace(this string str)
        {
            str += " ";
            return str;
        }

        public static string SliceBack(this string str, int limit, string leftOverCover = "...")
        {
            str = str.Length > limit ? str.Substring(0, limit - leftOverCover.Length) + leftOverCover : str;
            return str;
        }

        public static string SliceFront(this string str, int limit, string leftOverCover = "...")
        {
            str = limit >= str.Length ? str : leftOverCover + str.Substring(str.Length - limit - leftOverCover.Length);
            return str;
        }

        public static string MarkdownCodeBlock(this string str, string lang = null)
            => Format.Code(str, lang ?? "");

        public static string MarkdownCodeLine(this string str)
            => Format.Code(str);

        public static string WrapAround(this string str, string start, string end = null)
            => $"{start}{str}{end ?? start}";

        public static int GetWordCount(this string str)
            => GetWordCount(str, null).wordCount;

        public static (int wordCount, string[] splits) GetWordCount(this string str, string[] splits = null)
        {
            splits = str.Split(' ');
            return (splits.Length, splits);
        }
        #endregion

        #region IEnumberable ----------------------------------------------------------------------
        static Random Random = new Random();
        public static T SelectRandom<T>(this IEnumerable<T> collection, Random rng = null)
        {
            return collection.ElementAt(((int)(((rng ?? Random).NextDouble() * collection.Count()))));
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
            => timespan.GetRoundedYears() >= 2 ? timespan.GetRoundedYears() + " years " + timespan.Subtract(new TimeSpan(timespan.GetRoundedYears() * 365, 0, 0, 0)).AsRoundedDuration() :
            timespan.GetRoundedMonths() > 2 ? timespan.GetRoundedMonths() + " months " + timespan.Subtract(new TimeSpan(timespan.GetRoundedMonths() * 30, 0, 0, 0)).AsRoundedDuration() :
            timespan.TotalHours >= 48 ? (int)timespan.TotalHours / 24 + " days" :
            timespan.TotalMinutes >= 120 ? (int)timespan.TotalMinutes / 60 + " hours" :
            timespan.TotalSeconds >= 120 ? (int)timespan.TotalSeconds / 60 + " minutes" :
            (int)timespan.TotalSeconds + " seconds";

        public static int GetRoundedYears(this TimeSpan timeSpan)
            => (int)timeSpan.GetTotalYears();

        public static double GetTotalYears(this TimeSpan timeSpan)
            => timeSpan.TotalDays / 365;

        public static int GetRoundedMonths(this TimeSpan timeSpan)
            => (int)timeSpan.GetTotalMonths();

        public static double GetTotalMonths(this TimeSpan timeSpan)
            => timeSpan.TotalDays / 30;
        #endregion

        #region IGuildUser ------------------------------------------------------------------------
        public static string GetDisplayName(this IGuildUser user)
            => user.Nickname ?? user.Username;
        #endregion

        #region CommandInfo -----------------------------------------------------------------------
        public static string GetCommandParametersInfo(this CommandInfo command)
            => string.Join(" ", command.Parameters.Select(x => x.Name.WrapAround("<", ">")));
        #endregion
    }
}
