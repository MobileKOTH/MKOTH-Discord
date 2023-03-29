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
            var api = new OpenAIAPI(APIAuthentication.LoadFromEnv());

            var results = await api.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                MaxTokens = 256,
                Temperature = 1,
                Messages = new []
                {
                    new ChatMessage(ChatMessageRole.System, "Join the conversion in this work place chat"),
                    new ChatMessageWithName(ChatMessageRole.User, "John", "Hello everyone"),
                    new ChatMessageWithName(ChatMessageRole.User, "Edward", "Good morning all"),
                    new ChatMessageWithName(ChatMessageRole.User, "Boss", "Is John and Amy in the chat?"),
                }
            });

            var reply = results.Choices[0].Message;
            Console.WriteLine($"{reply.Role}: {reply.Content.Trim()}");

            await Task.CompletedTask;
        }
    }
}