using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem.OpenAIExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Chat;

namespace Cerlancism.ChatSystem.OpenAIExtensions.Tests
{
    [TestClass()]
    public class ChatMessageWithNameTests
    {
        [TestMethod()]
        public async Task ChatMessageWithNameTest()
        {
            var api = new OpenAIAPI("");
            var message = new ChatMessageWithName(ChatMessageRole.User, "", "");
            Assert.IsTrue(true);
            await Task.CompletedTask;
        }
    }
}