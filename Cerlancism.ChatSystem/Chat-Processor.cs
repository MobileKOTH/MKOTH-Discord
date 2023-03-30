using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Cerlancism.ChatSystem.Model;
using Newtonsoft.Json;

namespace Cerlancism.ChatSystem
{
    using static Extensions.GenericExtensions;
    using static Extensions.StringExtensions;

    public partial class Chat
    {
        private void LogMessage(object logObject)
            => LogMessage(JsonConvert.SerializeObject(logObject, Formatting.Indented));

        private void LogMessage(string log)
            => Log?.Invoke($"[ChatSystem]\n```json\n{log}\n```");

        public static string PurgeMessage(string message)
            => Regex.Replace(RemovePunctuations(message)
                .Trim()
                .Trim('\n'), @"\s+", " ");

        public static string RemovePunctuations(string message)
            => new string(message.Select(c => char.IsPunctuation(c) ? ' ' : c)
                .ToArray());

        private static bool IsSentenceLengthMultiple(string message, int wordCount, int mutiplier = 4)
            => message.GetWordCount() > wordCount * mutiplier;

        public async Task<(int wordCount, IEnumerable<Analysis> analysis)> AnalyseAsync(string message)
        {
            var history = HistoryCache;
            var (wordCount, words) = message.GetWordCount(null);
            var indexes = ParallelEnumerable.Range(1, history.Count - 2);

            var analysed = indexes.Select(index =>
            {
                var entry = history[index];
                return new Analysis
                {
                    Score = ComputeScore(entry.Message, words, wordCount),
                    Trigger = history[index - 1],
                    Rephrase = entry,
                    Response = history[index + 1]
                };
            }).ToArray();

            history = null;
            indexes = null;
            await Task.CompletedTask;

            return (wordCount, analysed);
        }

        public IEnumerable<Analysis> GetResults(int wordCount, IEnumerable<Analysis> analysis)
        {
            var query = analysis.AsParallel();
            float wordCountTarget = wordCount;
            float matchRate = 0.8f;

            while (!query.Any(x => x.Score >= matchRate))
            {
                wordCountTarget--;
                matchRate = NextMatchRate();

                if (matchRate <= 0)
                {
                    var full = query.ToArray();
                    analysis = null;
                    query = null;
                    return full; 
                }
            }

            float NextMatchRate()
            {
                if (wordCount > 4)
                {
                    return matchRate - 0.15f;
                }
                else
                {
                    return wordCountTarget / wordCount;
                }
            }

            var results = query.Where(x => x.Score >= matchRate).ToArray();
            analysis = null;
            query = null;
            return results;
        }

        public string GetRandomReply(int wordCount, IEnumerable<Analysis> analysis, out Analysis result)
        {
            result = analysis.SelectRandom();
            var rephraseOrResponse = IsGettingRephraseOrResponse(wordCount);
            var reply = rephraseOrResponse ? result.Rephrase.Message : result.Response.Message;
            analysis = null;

            return reply;
        }

        private float ComputeScore(string history, string[] words, int wordCount)
        {
            var matchCount = 0f;

            if (IsSentenceLengthMultiple(history, wordCount))
            {
                return 0;
            }

            foreach (var word in words)
            {
                if (history.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
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
