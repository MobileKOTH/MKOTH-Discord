using System;
using System.Text.RegularExpressions;

namespace MKOTHDiscordBot.Utilities
{
    public static class UwuTranslator
    {
        static string[] Faces = { "(・`ω´・)", ";;w;;", "owo", "UwU", ">w<", "^w^" };
        public static string Translate(string input)
        {
            var result = input;
            result = Regex.Replace(result, "(?:r|l)", "w", RegexOptions.ECMAScript);
            result = Regex.Replace(result, "(?:R|L)", "W", RegexOptions.ECMAScript);
            result = Regex.Replace(result, "n([aeiou])", "ny$1", RegexOptions.ECMAScript);
            result = Regex.Replace(result, "N([aeiou])", "Ny$1", RegexOptions.ECMAScript);
            result = Regex.Replace(result, "N([AEIOU])", "Ny$1", RegexOptions.ECMAScript);
            result = Regex.Replace(result, "ove", "uv", RegexOptions.ECMAScript);
            result = Regex.Replace(result, @"\!+", " " + Faces[(int)Math.Floor(new Random().NextDouble() * Faces.Length)] + " ", RegexOptions.ECMAScript);

            return result;
        }
    }
}
