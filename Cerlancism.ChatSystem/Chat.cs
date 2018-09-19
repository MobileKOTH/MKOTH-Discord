using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cerlancism.ChatSystem.Core;
using LiteDB;

namespace Cerlancism.ChatSystem
{
    public class Chat : IDisposable
    {
        private static BsonValue lastId = default;
        private static ulong previousUserId = default;
        private static string previousMessage = default;

        private LiteDatabase liteDatabase;

        public Chat(string connectionString)
        {
            liteDatabase = new LiteDatabase(connectionString);
        }

        public void Add(ulong userId, string message)
        {
            if (new string[] { ".", ">", "?", "!", "/" }.Any(x => message.StartsWith(x)) || message == "")
            {
                return;
            }

            if (previousMessage == message)
            {
                return;
            }

            var collection = liteDatabase.GetCollection<History>();

            if (previousUserId != default)
            {
                if (previousUserId == userId)
                {
                    var lastMessage = GetLastMessage();
                    lastMessage.Message += " " + message;
                    collection.Update(lastMessage);
                    return;
                }
            }
            lastId = collection.Insert(new History { Message = message });
            previousUserId = userId;
            previousMessage = message;
        }

        public History GetLastMessage()
        {
            var collection = liteDatabase.GetCollection<History>();
            collection.EnsureIndex(x => x.Id);
            lastId = lastId == default ? (BsonValue)collection.FindOne(Query.All(Query.Descending)).Id : lastId;
            return collection.FindById(lastId);
        }

        public string Reply(string message)
        {
            return "";
        }

        public IEnumerable<(float Score, History trigger, History rephrase, History reply)> Analyse(string message)
        {
            return null;
        }

        public string GetMessageById(int id)
        {
            var collection = liteDatabase.GetCollection<History>();
            return collection.FindById(id).Message;
        }

        public void Dispose()
        {
            liteDatabase.Dispose();
        }
    }
}
