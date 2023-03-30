using Cerlancism.ChatSystem;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Cerlancism.ChatSystem.OpenAIExtensions;
using Cerlancism.ChatSystem.Model;

namespace Cerlancism.ChatSystem.Tests
{
    [TestClass()]
    public class ChatTests
    {
        [TestMethod()]
        public void SanitizeNameTest()
        {
            var input = "Your input string here-123";
            var output = ChatMessageWithName.SanitizeName(input);

            Console.WriteLine(output);
        }

        [TestMethod()]
        public async Task GenerateResultsTest()
        {
            var chat = new Chat("FileName=../Data/ChatHistory.db");

            var (wordCount, results) = await chat.GenerateResults("Wanna play?");

            foreach (var item in results.Take(10))
            {
                Console.WriteLine(item.Trigger.Message);
                Console.WriteLine("----");
                Console.WriteLine(item.Rephrase.Message);
                Console.WriteLine("----");
                Console.WriteLine(item.Response.Message);
                Console.WriteLine("========");
            }
        }
    }
}