using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cerlancism.ChatSystem.Core;
using LiteDB;

namespace Cerlancism.ChatSystem
{
    using static Extensions.FuncExtensions;
    using static Extensions.GenericExtensions;
    using static Extensions.StringExtensions;

    using static Utilities.DatabaseUtilities;
    using static Utilities.FuncUtilities;

    public partial class Chat : IDisposable
    {
        public event Action<string> Log;

        private static int lastId = default;
        private static ulong previousUserId = default;
        private static string previousMessage = default;

        private static readonly string[] startWithIgnoreList = { ".", ">", "?", "!", "/" };

        private string connectionString;

        private LiteDatabase ChatDatabase => _chatDatabase ?? GetAndOutDataBase(out _chatDatabase, connectionString);
        private LiteCollection<Entry> ChatCollection => _chatCollection ?? GetAndOutCollection(ChatDatabase, out _chatCollection);

        private LiteDatabase _chatDatabase;
        private LiteCollection<Entry> _chatCollection;

        public Chat(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task AddAsync(ulong userId, string message)
        {
            if (startWithIgnoreList.Any(x => message.StartsWith(x)) || message == "")
            {
                return;
            }

            if (previousMessage == message)
            {
                return;
            }

            if (previousUserId == userId)
            {
                await UpdateMessageAsync(message);
                return;
            }

            await AddMessageAsync(userId, message);
        }

        private async Task UpdateMessageAsync(string message)
        {
            var lastMessage = await GetLastChatHistoryAsync();
            lastMessage.Message += " " + message;
            await Task.FromResult(ChatCollection.Update(lastMessage));
        }

        private async Task AddMessageAsync(ulong userId, string message)
        {
            lastId = await Task.FromResult(ChatCollection.Insert(new Entry
            {
                Message = message
            }));

            previousUserId = userId;
            previousMessage = message;
        }

        public async Task<Entry> GetChatHistoryByIdAsync(int id)
            => await Task.FromResult(ChatCollection.FindById(id));

        public async Task<Entry> GetLastChatHistoryAsync()
            => await GetChatHistoryByIdAsync(lastId = lastId == default ? ChatCollection.FindOne(Query.All(Query.Descending)).Id : lastId);

        public async Task<string> ReplyAsync(string message)
            => await Task.FromResult(Reply(message));

        public string Reply(string message)
            => Funcify<string, string>(RemovePunctuationsAndLower)
            .Then(Analyse)
            .Then(GetResults)
            .Then(GetRandomReply)(message);

        //=> TrimMessage(message)
        //.ToLower()
        //.Forward(Analyse)
        //.Forward(x => GetResults(x, message))
        //.Forward(x => x.SelectRandom())
        //.Forward(x => GetTriggerOrResponse(x, message));

        //=> GetTriggerOrResponse(GetResults(Analyse(TrimMessage(message).ToLower()), message).SelectRandom(), message);

        //{
        //    message = TrimMessage(message).ToLower();
        //    var analysis = Analyse(message);
        //    var choosenOnes = GetResults(analysis, message);
        //    var choosenOne = choosenOnes.SelectRandom();
        //    var reply = GetTriggerOrResponse(choosenOne, message);
        //    return reply;
        //}

        public (string message, IEnumerable<Analysis> analysis) Analyse(string message)
            => (message, AnalyseAsync(message).Result);

        public async Task<IEnumerable<Analysis>> AnalyseAsync(string message)
        {
            var history = await Task.FromResult(ChatCollection.FindAll());
            var analysed = new ConcurrentBag<Analysis>();
            var (wordCount, words) = message.GetWordCount(null);
            var filtered = history
                .AsParallel()
                .Where(x => FilterBySentenceLength(x.Message, wordCount))
                .ToArray();
            var count = filtered.Length;

            Parallel.ForEach(filtered, (item, state, index) =>
            {
                var score = ComputeScore(item.Message, words, wordCount);
                var result = new Analysis
                {
                    Score = score,
                    Trigger = index == 0 ? default : filtered[index - 1],
                    Rephrase = item,
                    Response = index + 1 == count ? default : filtered[index + 1],
                };

                analysed.Add(result);
            });

            return analysed;
        }

        public string GetRandomReply((bool rephraseOrResponse, string message, IEnumerable<Analysis> analysis) input)
        {
            var (rephraseOrResponse, message, analysis) = input;
            var choosen = analysis.SelectRandom();
            var reply = rephraseOrResponse ? choosen.Rephrase.Message : (choosen.Response == null ? choosen.Rephrase.Message : choosen.Response.Message);

            LogMessage(new
            {
                Message = message,
                Result = choosen,
                Reply = reply
            });

            return reply;
        }

        public (bool rephraseOrResponse, string message, IEnumerable<Analysis> results) GetResults((string message, IEnumerable<Analysis> analysis) input)
            => GetResults(input.message, input.analysis);

        public (bool rephraseOrResponse, string message, IEnumerable<Analysis> results) GetResults(string message, IEnumerable<Analysis> analysis)
        {
            var query = analysis.AsParallel();
            var wordCount = message.GetWordCount();
            var rephraseOrResponse = IsGettingRephraseOrResponse(wordCount);
            float wordCountTarget = wordCount;
            float matchRate = 0.9f;

            while (!query.Any(x => x.Score >= matchRate))
            {
                wordCountTarget--;
                matchRate = NextMatchRate();

                if (matchRate <= 0)
                {
                    return (rephraseOrResponse, message, analysis);
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

            var results = query.Where(x => x.Score >= matchRate)
                .Where(x => rephraseOrResponse ? FilterBySentenceLength(x.Rephrase.Message, wordCount) : true);

            return (rephraseOrResponse, message, results);
        }

        private static bool FilterBySentenceLength(string message, int wordCount)
        {
            return message.GetWordCount() <= wordCount * 4;
        }

        public void Dispose()
        {
            _chatDatabase?.Dispose();
        }
    }
}
