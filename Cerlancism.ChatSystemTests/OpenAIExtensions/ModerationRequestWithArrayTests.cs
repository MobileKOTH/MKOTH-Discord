using Microsoft.VisualStudio.TestTools.UnitTesting;
using Cerlancism.ChatSystem.OpenAIExtensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                "Sentence A",
                "Sentence B"
            };

            var result = await api.Moderation.CallModerationAsync(new ModerationRequestWithArray(inputs));

            for (int i = 0; i < result.Results.Count; i++)
            {
                var item = result.Results[i];
                Console.WriteLine($"{inputs[i]} -> {item.Flagged}");
            }
        }
    }
}