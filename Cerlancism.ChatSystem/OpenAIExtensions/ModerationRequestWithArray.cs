using Newtonsoft.Json;

using OpenAI_API.Moderation;

using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.OpenAIExtensions
{
    public class ModerationRequestWithArray : ModerationRequest
    {
        [JsonProperty("input")]
        public new string[] Input { get; set; }

        public ModerationRequestWithArray(string[] input)
        {
            Model = OpenAI_API.Models.Model.TextModerationLatest;
            Input = input;
        }
    }
}
