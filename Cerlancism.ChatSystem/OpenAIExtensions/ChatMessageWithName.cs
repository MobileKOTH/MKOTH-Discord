using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json;

using OpenAI_API.Chat;

namespace Cerlancism.ChatSystem.OpenAIExtensions
{
    public class ChatMessageWithName : ChatMessage
    {
        [JsonProperty("user")]
        public string Name { get; set; }

        public ChatMessageWithName(ChatMessageRole role, string name, string content): base (role, content)
        {
            Name = name;
        }
    }
}
