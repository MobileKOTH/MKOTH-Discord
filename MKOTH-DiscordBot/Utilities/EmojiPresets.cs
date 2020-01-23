using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace MKOTHDiscordBot.Utilities
{
    public static class EmojiPresets
    {
        public static IEnumerable<Emoji> Numbers => lazyNumbers.Value;
        private static readonly Lazy<Emoji[]> lazyNumbers = new Lazy<Emoji[]>(() =>
        {
            IEnumerable<Emoji> numbers()
            {
                for (char code = '\u0030'/* 0 */; code <= '\u0039' /* 9 */; code++)
                {
                    yield return new Emoji($"{code}\uFE0F\u20E3");
                }
                yield return new Emoji("\uD83D\uDD1F"); /* 10 */
            }
            return numbers().ToArray();
        });
    }
}
