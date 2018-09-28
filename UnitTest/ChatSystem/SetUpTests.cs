using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem;
using Cerlancism.ChatSystem.Core;
using Cerlancism.ChatSystem.Utilities;
using System.Diagnostics;
using System.Threading.Tasks;
using LiteDB;

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class SetUpTests
    {
        [TestMethod]
        public void MigrationTest()
        {
            // Arrange
            var fileName = "ChatHistory";
            var path = Directory.GetCurrentDirectory() + $@"\{fileName}.db";
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            // Act
            Migration.Migrate(fileName);

            // Assert
            Assert.AreEqual(File.Exists(path), true);
            using (var db = new LiteDatabase(path))
            {
                var historyCollection = db.GetCollection<Entry>();
                Assert.AreNotEqual(null, historyCollection);

                historyCollection.EnsureIndex(x => x.Id);
                db.Shrink();

                var count = historyCollection.Count();
                Console.WriteLine($"Count: {count}");

                var lastDocument = historyCollection.FindOne(Query.All(Query.Descending));
                Console.WriteLine($"Last id: {lastDocument.Id}");
            }

            // Clean Up
            //CleanUp();

            void CleanUp()
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public async Task GetLastMessageTestAsync()
        {
            var stopwatch = new Stopwatch();
            using (Chat chatSystem = new Chat("ChatHistory.db"))
            {
                stopwatch.Start();
                var lastMessage = await chatSystem.GetLastChatHistoryAsync();
                stopwatch.Stop();

                Assert.AreEqual(lastMessage != null, true);

                Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms {lastMessage.Id} {lastMessage.Message}");
            }
        }
    }
}
