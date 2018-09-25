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

    public partial class Chat : IDisposable
    {
        public event Action<string> Log;

        private static int lastId = default;
        private static ulong previousUserId = default;
        private static string previousMessage = default;

        private static readonly string[] startWithIgnoreList = { ".", ">", "?", "!", "/" };

        private string connectionString;

        private LiteDatabase ChatDatabase => _chatDatabase ?? GetAndSetDataBase(out _chatDatabase, connectionString);
        private LiteCollection<History> ChatCollection => _chatCollection ?? ChatDatabase.GetAndSetCollection(out _chatCollection);

        private LiteDatabase _chatDatabase;
        private LiteCollection<History> _chatCollection;

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
                    var lastMessage = GetLastMessage();
                    lastMessage.Message += " " + message;
                    ChatCollection.Update(lastMessage);
                    return;
                }
            }
            lastId = ChatCollection.Insert(new History { Message = message });
            previousUserId = userId;
            previousMessage = message;
        }

        public History GetLastMessage()
        {            
            lastId = lastId == default ? ChatCollection.FindOne(Query.All(Query.Descending)).Id : lastId;
            return ChatCollection.FindById(lastId);
        }

        public string Reply(string message)
        {
            message = TrimMessage(message).ToLower();
            var analysis = Analyse(message);
            var choosenOnes = GetResults(analysis, message);
            var choosenOne = choosenOnes.SelectRandom();
            var reply = GetTriggerOrResponse(choosenOne, message);
            return reply;
        }

        public IEnumerable<Result> Analyse(string message)
        {
            var history = ChatCollection.FindAll().ToList();
            var analysed = new ConcurrentQueue<Result>();
            var count = history.Count;

            Parallel.ForEach(history, (item, state, index) =>
            {
                var score = ComputeScore(item.Message, message);
                var i = (int)index;
                var result = new Result
                {
                    Score = score,
                    Trigger = i == 0 ? null : history[i - 1],
                    Rephrase = item,
                    Response = i == count - 1 ? null : history[i + 1],
                };

                analysed.Enqueue(result);
            });

            return analysed.AsEnumerable();
        }

        public IEnumerable<Result> GetResults(IEnumerable<Result> analysis, string message)
        {
            var wordCount = message.GetWordCount();
            float wordCountMatch = wordCount;
            float matchRate = 0.9f;
            var results = new ConcurrentQueue<Result>();
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

            return analysis.Where(x => x.Score >= matchRate);

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

        public string GetMessageById(int id)
        {
            return ChatCollection.FindById(id).Message;
        }

        public void Dispose()
        {
            ChatDatabase.Dispose();
        }
    }
}
