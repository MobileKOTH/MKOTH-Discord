using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Extensions
{
    public static class StringExtensions
    {
        public static int GetWordCount(this string str)
            => GetWordCount(str, null).wordCount;

        public static (int wordCount, string[] splits) GetWordCount(this string str, string[] splits = null)
            => str.Split(' ')
            .Select(x => x.Replace("\n", ""))
            .Where(x => !x.IsNullOrEmptyOrWhiteSpace())
            .ToArray()
            .Forward(x => (x.Length, x));


        public static bool CaseIgnoreContains(this string source, string toCheck)
            => source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool IsNullOrEmptyOrWhiteSpace(this string str)
            => string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
    }
}
