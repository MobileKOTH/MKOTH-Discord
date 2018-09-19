using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem;
using Cerlancism.ChatSystem.Core;
using System.Diagnostics;

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class SetUp
    {
        //[TestMethod]
        public void StartUpTest()
        {
            Tester.TestMethod();
        }

        //[TestMethod]
        public void MigrateTest()
        {
            Tester.Migrate();
        }

        [TestMethod]
        public void ReadDatabaseTest()
        {
            Tester.ReadDatabase();
        }

        [TestMethod]
        public void GetLastMessageTest()
        {
            var stopwatch = new Stopwatch();
            using (Chat chatSystem = new Chat("ChatHistory.db"))
            {
                stopwatch.Start();
                var lastMessage = chatSystem.GetLastMessage();
                stopwatch.Stop();

                Assert.AreEqual(lastMessage != null, true);

                Console.WriteLine($"{stopwatch.Elapsed.TotalMilliseconds} ms {lastMessage.Id} {lastMessage.Message}");
            }
        }
    }
}
