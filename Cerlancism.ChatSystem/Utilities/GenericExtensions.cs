using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cerlancism.ChatSystem.Utilities
{
    public static class GenericExtensions
    {
        static Random Random = new Random();
        public static T SelectRandom<T>(this IEnumerable<T> collection, Random rng = null)
        {
            return collection.ElementAt(((int)(((rng ?? Random).NextDouble() * collection.Count()))));
        }

        public static int GetWordCount(this string str)
            => GetWordCount(str, null).wordCount;

        public static (int wordCount, string[] splits) GetWordCount(this string str, string[] splits = null)
        {
            splits = str.Split(' ');
            return (splits.Length, splits);
        }
    }
}
