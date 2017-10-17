using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Discord.Commands;

namespace MKOTH_Discord_Bot
{
    public class Chat
    {
        public static List<string> History { get; set; } = new List<string>() ;

        private string message;

        public Chat(string message)
        {
            this.message = message;
            if (!(message.StartsWith(".") || message.StartsWith(">")))
            {
                History.Add(message);
            }
        }

        public static void LoadHistory()
        {
            string json = File.ReadAllText("ChatHistory.json");
            History = JsonConvert.DeserializeObject<List<string>>(json);
        }

        public static void SaveHistory()
        {
            var json = JsonConvert.SerializeObject(History, Formatting.Indented);
            File.WriteAllText("ChatHistory.json", json);
        }

        public static async Task Reply(SocketCommandContext context, string message)
        {
            DateTimeOffset starttime = DateTime.Now;
            List<string> possiblereplies = new List<string>();
            string[] words = message.ToLower().Split(' ');
            int wordcount = words.Length;
            double matchcount = 0;
            bool istyping = false;
            bool nextispossible = false;
            foreach (var history in History)
            {
                if (nextispossible)
                {
                    possiblereplies.Add(history.ToLower());
                    if (!istyping)
                    {
                        istyping = true;
                        await context.Channel.TriggerTypingAsync();
                    }
                }
                foreach (var word in words)
                {
                    if (history.ToLower().Contains(word))
                    {
                        matchcount++;
                    }
                }
                if (matchcount / wordcount > 0.8)
                {
                    nextispossible = true;
                }
            }
            foreach (var item in possiblereplies)
            {
                Console.WriteLine(item);
            }
            string reply = possiblereplies[((int)(new Random().NextDouble() * possiblereplies.Count()))];
            await context.Channel.SendMessageAsync(reply);
        }
    }
}
