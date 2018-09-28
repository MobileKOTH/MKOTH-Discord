using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using Cerlancism.ChatSystem.Core;
using Cerlancism.ChatSystem.Utilities;
using Newtonsoft.Json;

namespace Cerlancism.ChatSystem
{
    using static Extensions.StringExtensions;
    using static Extensions.GenericExtensions;

    public partial class Chat
    {
        private void LogMessage(object logObject)
            => LogMessage(JsonConvert.SerializeObject(logObject, Formatting.Indented));

        private void LogMessage(string log)
            => Log?.Invoke($"[ChatSystem] {log}");

        public static string RemovePunctuations(string message)
            => new string(message.Where(c => !char.IsPunctuation(c)).ToArray());

        public static string RemovePunctuationsAndLower(string message)
            => RemovePunctuations(message).ToLower();

        private float ComputeScore(in string history, in string[] words, in int wordCount)
        {
            var matchCount = 0f;
            var historyLowerCase = history.ToLower();

            foreach (var word in words)
            {
                if (historyLowerCase.Contains(word))
                {
                    matchCount++;
                }
            }

            return matchCount / wordCount;
        }

        private bool IsGettingRephraseOrResponse(int wordCount)
        {
            var randomsource = new Random().NextDouble();

            switch (wordCount)
            {
                case 1:

                case 2:
                    return randomsource > 0.2;

                case 3:
                    return randomsource > 0.66;

                default:
                    return false;
            }
        }
    }
}
