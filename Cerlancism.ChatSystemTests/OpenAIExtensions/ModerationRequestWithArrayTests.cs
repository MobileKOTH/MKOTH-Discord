using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Linq;
using System.Threading.Tasks;
using OpenAI_API;
using OpenAI_API.Moderation;

namespace Cerlancism.ChatSystem.OpenAIExtensions.Tests
{
    [TestClass()]
    public class ModerationRequestWithArrayTests
    {
        [TestMethod()]
        public async Task ModerationRequestWithArrayTest()
        {
            var api = new OpenAIAPI(APIAuthentication.LoadFromEnv());

            var inputs = new[]
            {
                "A",
                "B"
            };

            var result = await api.Moderation.CallModerationAsync(new ModerationRequest(inputs));

            foreach (var (input, moderation) in inputs.Zip(result.Results))
            {
                Console.WriteLine($"{input} -> {(moderation.Flagged ? moderation.MainContentFlag : "False")}");
            }
        }
    }
}