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
    public partial class Chat
    {
        private void LogMessage(object logObject)
            => LogMessage(JsonConvert.SerializeObject(logObject, Formatting.Indented));

        private void LogMessage(string log)
            => Log?.Invoke($"[ChatSystem] {log}");

        public static string TrimMessage(string message)
            => new string(message.Where(c => !char.IsPunctuation(c)).ToArray());

        private float ComputeScore(string history, in string[] words, in int wordCount)
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

        private string GetTriggerOrResponse(AnalysisResult result, string message)
        {
            var wordCount = message.GetWordCount();
            var randomsource = new Random().NextDouble();
            ChatHistory choosen;
            switch (wordCount)
            {
                case 1:

                case 2:
                    choosen = (randomsource > 0.2) ? result.Rephrase : result.Response;
                    break;

                case 3:
                    choosen = (randomsource > 0.66) ? result.Rephrase : result.Response;
                    break;

                default:
                    choosen = result.Response;
                    break;
            }
            LogMessage((message, result));
            return choosen?.Message ?? result.Rephrase.Message;
        }
    }
}
