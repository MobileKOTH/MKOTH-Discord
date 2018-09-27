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

        public void Add(ulong userId, string message)
        {
            if (startWithIgnoreList.Any(x => message.StartsWith(x)) || message == "")
            {
                return;
            }

            if (previousMessage == message)
            {
                return;
            }

            if (previousUserId != default && previousUserId == userId)
            {
                var lastMessage = GetLastChatHistory();
                lastMessage.Message += " " + message;
                ChatCollection.Update(lastMessage);
                return;
            }

            lastId = ChatCollection.Insert(new Entry
            {
                Message = message
            });

            previousUserId = userId;
            previousMessage = message;
        }

        public Entry GetChatHistoryById(int id)
            => ChatCollection.FindById(id);

        public Entry GetLastChatHistory()
            => GetChatHistoryById(lastId = lastId == default ? ChatCollection.FindOne(Query.All(Query.Descending)).Id : lastId);

        public string Reply(string message)
            => Funcify<string, string>(TrimeAndLower)
            .Then(Analyse)
            .Then(GetRandomResult)
            .Then(GetRephraseOrResponse)(message);

        public string TrimeAndLower(string message)
            => TrimMessage(message).ToLower();

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
            => (message, Analyse(in message));

        public IEnumerable<Analysis> Analyse(in string message)
        {
            var history = ChatCollection.FindAll().ToArray();
            var analysed = new ConcurrentBag<Analysis>();
            var count = history.Length;
            var (wordCount, words) = message.GetWordCount(null);

            Parallel.ForEach(history, (item, state, index) =>
            {
                var score = ComputeScore(item.Message, words, wordCount);
                var result = new Analysis
                {
                    Score = score,
                    Trigger = index == 0 ? default : history[index - 1],
                    Rephrase = item,
                    Response = index + 1 == count ? default : history[index + 1],
                };

                analysed.Add(result);
            });

            return analysed;
        }

        public (string message, Analysis result) GetRandomResult((string message, IEnumerable<Analysis> analysis) input)
            => (input.message, GetResults(input.message, input.analysis).SelectRandom());

        public IEnumerable<Analysis> GetResults(string message, IEnumerable<Analysis> analysis)
        {
            var query = analysis.AsParallel();
            var wordCount = message.GetWordCount();
            float wordCountTarget = wordCount;
            float matchRate = 0.9f;

            while (!query.Any(x => x.Score >= matchRate))
            {
                wordCountTarget--;
                matchRate = NextMatchRate();

                if (matchRate <= 0)
                {
                    return analysis;
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

            return query.Where(x => x.Score >= matchRate);
        }

        public void Dispose()
        {
            ChatDatabase.Dispose();
        }
    }
}
