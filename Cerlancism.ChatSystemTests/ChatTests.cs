using Cerlancism.ChatSystem;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Cerlancism.ChatSystem.OpenAIExtensions;

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
    }
}