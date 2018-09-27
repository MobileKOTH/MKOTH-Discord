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
    using static Utilities.DatabaseExtensions;
    using static Utilities.GenericExtensions;
    using static Utilities.FuncExtensions;

    public partial class Chat : IDisposable
    {
        public event Action<string> Log;

        private static int lastId = default;
        private static ulong previousUserId = default;
        private static string previousMessage = default;

        private static readonly string[] startWithIgnoreList = { ".", ">", "?", "!", "/" };

        private string connectionString;

        private LiteDatabase ChatDatabase => _chatDatabase ?? GetAndSetDataBase(out _chatDatabase, connectionString);
        private LiteCollection<ChatHistory> ChatCollection => _chatCollection ?? ChatDatabase.GetAndSetCollection(out _chatCollection);

        private LiteDatabase _chatDatabase;
        private LiteCollection<ChatHistory> _chatCollection;

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

            if (previousUserId != default)
            {
                if (previousUserId == userId)
                {
                    var lastMessage = GetLastChatHistory();
                    lastMessage.Message += " " + message;
                    ChatCollection.Update(lastMessage);
                    return;
                }
            }
            lastId = ChatCollection.Insert(new ChatHistory { Message = message });
            previousUserId = userId;
            previousMessage = message;
        }

        public ChatHistory GetLastChatHistory()
        {            
            lastId = lastId == default ? ChatCollection.FindOne(Query.All(Query.Descending)).Id : lastId;
            return ChatCollection.FindById(lastId);
        }

        public string Reply(string message)
            => Analyse
            .Then(x => GetResults(message, x))
            .Then(x => x.SelectRandom())
            .Then(x => GetTriggerOrResponse(x, message))(message);

        //=> TrimMessage(message)
        //.ToLower()
        //.Forward(Analyse)
        //.Forward(x => GetResults(x, message))
        //.Forward(x => x.SelectRandom())
        //.Forward(x => GetTriggerOrResponse(x, message));

        // => GetTriggerOrResponse(GetResults(Analyse(TrimMessage(message).ToLower()), message).SelectRandom(), message);

        //{
        //    message = TrimMessage(message).ToLower();
        //    var analysis = Analyse(message);
        //    var choosenOnes = GetResults(analysis, message);
        //    var choosenOne = choosenOnes.SelectRandom();
        //    var reply = GetTriggerOrResponse(choosenOne, message);
        //    return reply;
        //}

        public Func<string, IEnumerable<AnalysisResult>> Analyse => message =>
        {
            var history = ChatCollection.FindAll().ToArray();
            var analysed = new ConcurrentBag<AnalysisResult>();
            var count = history.Length;
            var (wordCount, words) = message.GetWordCount(null);

            Parallel.ForEach(history, (item, state, index) =>
            {
                var score = ComputeScore(item.Message, words, wordCount);
                var result = new AnalysisResult
                {
                    Score = score,
                    Trigger = index == 0 ? null : history[index - 1],
                    Rephrase = item,
                    Response = index + 1 == count ? null : history[index + 1],
                };

                analysed.Add(result);
            });

            return analysed;
        };

        public IEnumerable<AnalysisResult> GetResults(string message, IEnumerable<AnalysisResult> analysis)
        {
            var wordCount = message.GetWordCount();
            float wordCountMatch = wordCount;
            float matchRate = 0.9f;
            var query = analysis.AsParallel();

            while (!query.Any(x => x.Score >= matchRate))
            {
                wordCountMatch--;
                matchRate = NextMatchRate();

                if (matchRate <= 0)
                {
                    return analysis;
                }
            }

            return query.Where(x => x.Score >= matchRate);

            float NextMatchRate()
            {
                if (wordCount > 4)
                {
                    return matchRate - 0.15f;
                }
                else
                {
                    return wordCountMatch / wordCount;
                }
            }
        }

        public ChatHistory GetChatHistoryById(int id)
        {
            return ChatCollection.FindById(id);
        }

        public void Dispose()
        {
            ChatDatabase.Dispose();
        }
    }
}
