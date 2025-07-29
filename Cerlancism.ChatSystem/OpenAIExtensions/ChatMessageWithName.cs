using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using OpenAI_API.Chat;

namespace Cerlancism.ChatSystem.OpenAIExtensions
{
    public class ChatMessageWithName : ChatMessage
    {
        [JsonIgnore]
        protected string RawName { get; set; }

        public ChatMessageWithName(ChatMessageRole role, string name, string content): base (role, content)
        {
            RawName = name;
            Name = SanitizeName(RawName);
        }

        public string RevertName(string input)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return input;
            }

            return input.Replace(Name, RawName);
        }

        public static string SanitizeName(string name)
        {
            var sanitizedString = Regex.Replace(name.Replace(" ", "_"), @"[^\w-]", "");

            // Truncate the sanitized string if it is longer than 64 characters
            if (sanitizedString.Length > 64)
            {
                sanitizedString = sanitizedString.Substring(0, 64);
            }

            // Check if the sanitized string matches the regex pattern
            if (!Regex.IsMatch(sanitizedString, @"^[a-zA-Z0-9_-]{1,64}$"))
            {
                sanitizedString = Regex.Replace(sanitizedString, @"[^a-zA-Z0-9]", "");
            }

            return sanitizedString;
        }
    }
}
