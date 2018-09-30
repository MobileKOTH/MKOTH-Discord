using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Extensions
{
    public static class StringExtensions
    {
        public static int GetWordCount(this string str)
            => GetWordCount(str, null).wordCount;

        public static (int wordCount, string[] splits) GetWordCount(this string str, string[] splits = null)
        {
            splits = str.Split(' ');
            return (splits.Length, splits);
        }

        public static bool CaseIgnoreContains(this string source, string toCheck)
        {
            return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsNullOrEmptyOrWhiteSpace(this string str)
            => string.IsNullOrEmpty(str) || string.IsNullOrWhiteSpace(str);
    }
}
