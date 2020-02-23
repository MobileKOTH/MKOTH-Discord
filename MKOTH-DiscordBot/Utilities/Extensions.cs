using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static string AsJoin(this string str, IEnumerable<string> lines)
        {
            return string.Join(str, lines);
        }

        public static string JoinLines(this IEnumerable<string> lines, string seperator = "\n")
        {
            return seperator.AsJoin(lines);
        }

        public static string SliceBackByLine(this string str, int limit, string cover = "\n...")
        {
            return str.SliceBackByLine(limit, out _, cover);
        }

        public static string SliceBackByLine(this string str, int limit, out int omission, string cover = "\n...")
        {
            return TrimLines(str.Split('\n'), limit, cover, out omission).JoinLines() + cover;
        }

        public static string SliceFrontByLine(this string str, int limit, string cover = "...\n")
        {
            return str.SliceFrontByLine(limit, out int discard, cover);
        }

        public static string SliceFrontByLine(this string str, int limit, out int omission, string cover = "...\n")
        {
            return cover + TrimLines(str.Split('\n').Reverse(), limit, cover, out omission).Reverse().JoinLines();
        }

        public static bool EqualsIgnoreCase(this string str, string other)
        {
            return str.Equals(other, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnoreCase(this string str, string other)
        {
            return str.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> TrimLines(IEnumerable<string> lines, int limit, string cover, out int omission)
        {
            var length = 0;
            var result = lines.TakeWhile(x => ((length += x.Length + 1) + cover.Length) <= limit).ToArray();
            omission = lines.Count() - result.Length;
            return result;
        }

        public static string MarkdownCodeBlock(this string str, string lang = null)
            => $"```{(lang == null ? "" : lang)}\n{str}\n```";

        public static string MarkdownCodeLine(this string str) 
            => $"`{str}`";

        public static string WrapAround(this string str, string start, string end = null)
            => $"{start}{str}{end ?? start}";

        public static string PascalToSentence(this string str)
        {
            return Regex.Replace(str, "[a-z][A-Z]", m => m.Value[0] + " " + char.ToLower(m.Value[1]));
        }

        #endregion


        #region IEnumberable ----------------------------------------------------------------------

        public static T SelectRandom<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(((int)new Random().NextDouble()) * collection.Count());
        }

        public static List<List<T>> Split<T>(this List<T> list, int splitCount)
        {
            int size = list.Count / splitCount;
            var lists = new List<List<T>>();
            for (int i = 0; i + 1 < list.Count; i += size)
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
        {
            return user.Nickname ?? user.Username;
        }

        #endregion


        #region IMessageChannel -------------------------------------------------------------------

        public static string GetMention(this IMessageChannel channel)
            => (channel as IMentionable)?.Mention ?? channel.Name;

        #endregion


        #region CommandInfo -----------------------------------------------------------------------

        public static string GetCommandParametersInfo(this CommandInfo command)
            => string.Join(" ", command.Parameters.Select(x => x.Name.WrapAround("<", ">")));

        #endregion


        #region Func ------------------------------------------------------------------------------

        public static TResult Forward<T, TResult>(this T value, Func<T, TResult> function) =>
            function(value);

        #endregion


        #region Precondition ----------------------------------------------------------------------

        public static string GetDescription(this PreconditionAttribute precondition)
        {
            switch (precondition)
            {
                case RequireContextAttribute attribute:
                    return "Require " + attribute.Contexts.ToString() + " Context";

                case RequireUserPermissionAttribute attribute:
                    return "Require User "
                        + (attribute.ChannelPermission.HasValue
                        ? attribute.ChannelPermission.Value.ToString()
                        : attribute.GuildPermission.Value.ToString())
                        + " Permission";

                case RequireBotPermissionAttribute attribute:
                    return "Require Bot "
                        + (attribute.ChannelPermission.HasValue
                        ? attribute.ChannelPermission.Value.ToString()
                        : attribute.GuildPermission.Value.ToString())
                        + " Permission";
                default:
                    return precondition.ToString();
            }
        }

        #endregion
    }
}
