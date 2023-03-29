using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Chat;
using System.Collections.Generic;
using System.Linq;

namespace Cerlancism.ChatSystem.OpenAIExtensions.Tests
{
    [TestClass()]
    public class ChatMessageWithNameTests
    {
        [TestMethod()]
        public async Task ChatMessageWithNameTest()
        {
            var api = new OpenAIAPI(APIAuthentication.LoadFromEnv());


            var chatMessages = new[]
            {
                new ChatMessageWithName(ChatMessageRole.User, "John Doe", "Hello everyone"),
                new ChatMessageWithName(ChatMessageRole.User, "Edward Bob", "Good morning all"),
                new ChatMessageWithName(ChatMessageRole.User, "Boss", "Attendance report in chat now"),
            };

            var messages = new []
            {
                new ChatMessage(ChatMessageRole.System, "Join the conversion in this work place chat")
            };

            var results = await api.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                MaxTokens = 256,
                Temperature = 0.7,
                Messages = messages.Concat(chatMessages).ToArray()
            });

            var reply = results.Choices[0].Message;
            var output = chatMessages.Aggregate(reply.Content.Trim(), (text, chatMessage) => chatMessage.RevertName(text));

            Console.WriteLine($"{reply.Role}:\n{output}");

            await Task.CompletedTask;
        }
    }
}