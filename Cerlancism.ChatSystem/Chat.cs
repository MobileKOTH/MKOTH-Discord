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
            if (startWithIgnoreList.Any(x => message.StartsWith(x)) || message.IsNullOrEmptyOrWhiteSpace())
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
        {
            var cleanedMessage = RemovePunctuationsAndLower(message);
            var (wordCount, analysis) = await AnalyseAsync(cleanedMessage);
            var results = GetResults(wordCount, analysis);
            var choosen = GetRandomReply(wordCount, results, out Analysis result);

            LogMessage(new
            {
                Message = message,
                Result = result,
                Reply = choosen
            });

            return choosen;
        }
        //=> Funcify<string, string>(RemovePunctuationsAndLower)
        //.Then(Analyse)
        //.Then(GetResults)
        //.Then(GetRandomReply)(message);

        //=> TrimMessage(message)
        //.ToLower()
        //.Forward(Analyse)
        //.Forward(x => GetResults(x, message))
        //.Forward(x => x.SelectRandom())
        //.Forward(x => GetTriggerOrResponse(x, message));

        //=> GetTriggerOrResponse(GetResults(Analyse(TrimMessage(message).ToLower()), message).SelectRandom(), message);

        public async Task<(int wordCount, IEnumerable<Analysis> analysis)> AnalyseAsync(string message)
        {
            var history = ChatCollection.FindAll().ToArray();
            var analysed = new ConcurrentBag<Analysis>();
            var (wordCount, words) = message.GetWordCount(null);
            var trimed = history
                .Take(history.Length - 1)
                .Skip(1);

            Parallel.ForEach(trimed, (item, state, index) =>
            {
                var score = ComputeScore(item.Message, words, wordCount);
                var result = new Analysis
                {
                    Score = score,
                    Trigger = history[index],
                    Rephrase = history[index + 1],
                    Response = history[index + 2],
                };

                analysed.Add(result);
            });

            await Task.CompletedTask;

            return (wordCount, analysed);
        }

        //IEnumerable<Analysis> Results(ConcurrentBag<Analysis> analysis)
        //{
        //    while (!analysis.IsEmpty)
        //    {
        //        analysis.TryTake(out Analysis result);
        //        yield return result;
        //    }
        //}

        public IEnumerable<Analysis> GetResults(int wordCount, IEnumerable<Analysis> analysis)
        {
            var query = analysis.AsParallel();
            float wordCountTarget = wordCount;
            float matchRate = 0.9f;

            while (!query.Any(x => x.Score >= matchRate))
            {
                wordCountTarget--;
                matchRate = NextMatchRate();

                if (matchRate <= 0)
                {
                    return query;
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

            var results = query.Where(x => x.Score >= matchRate);

            return results;
        }

        public string GetRandomReply(int wordCount, IEnumerable<Analysis> analysis, out Analysis result)
        {
            result = analysis.SelectRandom();
            var rephraseOrResponse = IsGettingRephraseOrResponse(wordCount);
            var reply = rephraseOrResponse ? result.Rephrase.Message : result.Response.Message;

            return reply;
        }

        public void Dispose()
        {
            _chatDatabase?.Dispose();
            _chatDatabase = null;
            _chatCollection = null;
        }
    }
}
