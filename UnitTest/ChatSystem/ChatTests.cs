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

namespace UnitTest.ChatSystem
{
    [TestClass]
    public class ChatTests
    {
        [TestMethod]
        public void ChatSimpleReplyTest()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message!";

                // Act
                chat.Log += log => Console.WriteLine(log);

                var response = chat.Reply(trigger);


                // Assert
                Assert.AreNotEqual(response, null);
                Console.WriteLine(response);
            }
        }

        [TestMethod]
        public void ChatSimpleAnalysisTest()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message!";

                // Act
                var analysis = chat.Analyse(trigger);
                var responses = chat.GetResults(trigger, analysis);

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

                var response = chat.Reply(trigger);


                // Assert
                Assert.AreNotEqual(response, null);
                Console.WriteLine(response);
            }
        }

        [TestMethod]
        public void ChatLengthyAnalysisTest()
        {
            using (var chat = new Chat("ChatHistory.db"))
            {
                // Arrange
                var trigger = "Hello this is a test message Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message! Hello this is a test message!";

                // Act
                var analysis = chat.Analyse(trigger);
                var responses = chat.GetResults(trigger, analysis);

                // Assert
                Assert.AreNotEqual(responses, null);
                Console.WriteLine(JsonConvert.SerializeObject(responses.Take(25), Formatting.Indented));
            }
        }
    }
}
