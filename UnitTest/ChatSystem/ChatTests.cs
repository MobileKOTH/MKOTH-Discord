using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem;
using Cerlancism.ChatSystem.Core;
using Cerlancism.ChatSystem.Utilities;
using System.Diagnostics;
using LiteDB;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class ChatTests
    {
        [TestMethod]
        public async Task ChatSimpleReplyTestAsync()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message!";

                // Act
                chat.Log += log => Console.WriteLine(log);

                var response = await chat.ReplyAsync(trigger);

                // Assert
                Assert.AreNotEqual(response, null);
                Console.WriteLine(response);
            }
        }

        [TestMethod]
        public async Task ChatShortReplyTestAsync()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello";

                // Act
                chat.Log += log => Console.WriteLine(log);

                var response = await chat.ReplyAsync(trigger);

                // Assert
                Assert.AreNotEqual(response, null);
                Console.WriteLine(response);
            }
        }

        [TestMethod]
        public async Task ChatSimpleAnalysisTestAsync()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message!";

                // Act
                var (wordCount, analysis) = await chat.AnalyseAsync(trigger);
                var responses = chat.GetResults(wordCount, analysis);

                // Assert
                Assert.AreNotEqual(responses, null);
                Console.WriteLine(JsonConvert.SerializeObject(responses.Take(25), Formatting.Indented));
            }
        }

        [TestMethod]
        public void ChatLengthyReplyTest()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message!";

                // Act
                chat.Log += log => Console.WriteLine(log);

                var response = chat.ReplyAsync(trigger).Result;


                // Assert
                Assert.AreNotEqual(response, null);
                Console.WriteLine(response);
            }
        }

        [TestMethod]
        public async Task ChatLengthyAnalysisTestAsync()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message!";

                // Act
                var (wordCount, analysis) = await chat.AnalyseAsync(trigger);
                var responses = chat.GetResults(wordCount, analysis);

                // Assert
                Assert.AreNotEqual(responses, null);
                Console.WriteLine(JsonConvert.SerializeObject(responses.Take(25), Formatting.Indented));
            }
        }
    }
}
