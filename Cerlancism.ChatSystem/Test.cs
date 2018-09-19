using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using LiteDB;
using Cerlancism.ChatSystem.Core;
using System.Threading.Tasks;

namespace Cerlancism.ChatSystem
{
    public static class Tester
    {
        public static void TestMethod()
        {
            Console.WriteLine($"Test {typeof(Tester).FullName}");

            var obj = JsonConvert.DeserializeObject<dynamic>("{\"Test\": \"Test\"}");
            Console.WriteLine(obj.Test);

            using (var db = new LiteDatabase("Test.db"))
            {
                var collection = db.GetCollection<History>("test");
                collection.Insert(new History { Message = "Test"});
                Console.WriteLine($"test: {collection.FindAll().First().Message}");
                db.DropCollection(collection.Name);
            }
        }

        public static void Migrate()
        {
            var json = $"[{File.ReadAllText("ChatHistory.dat")}]";
            var historyList = JsonConvert.DeserializeObject<List<string>>(json)
                .Select(x => new History
                {
                    Message = x
                });

            using (var db = new LiteDatabase("ChatHistory.db"))
            {
                var chatHistoryCollection = db.GetCollection<History>();
                chatHistoryCollection.InsertBulk(historyList);
            }
        }

        public static void ReadDatabase()
        {
            using (var db = new LiteDatabase("ChatHistory.db"))
            {
                var chatHistoryCollection = db.GetCollection<History>();
                var historyList = chatHistoryCollection.FindAll();
                var longSentences = historyList
                    .AsParallel()
                    .Where(x => x.Message.Contains("cy"))
                    .ToList();
                
                Console.WriteLine(longSentences.Count());
            }
        }
    }
}
