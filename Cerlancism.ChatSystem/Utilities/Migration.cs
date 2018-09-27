using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cerlancism.ChatSystem.Core;
using LiteDB;
using Newtonsoft.Json;

namespace Cerlancism.ChatSystem.Utilities
{
    public static class Migration
    {
        public static void Migrate(string fileName = "ChatHistory")
        {
            var json = $"[{File.ReadAllText($"{fileName}.dat")}]";
            var historyList = JsonConvert.DeserializeObject<List<string>>(json)
                .Where(x => !string.IsNullOrEmpty(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new Entry
                {
                    Message = x
                });

            using (var db = new LiteDatabase($"{fileName}.db"))
            {
                var chatHistoryCollection = db.GetCollection<Entry>();
                db.DropCollection(chatHistoryCollection.Name);
                chatHistoryCollection.InsertBulk(historyList);
            }
        }
    }
}
