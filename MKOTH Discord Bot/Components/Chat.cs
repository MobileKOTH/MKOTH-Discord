using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;

namespace MKOTH_Discord_Bot
{
    public class Chat
    {
        public static List<string> History { get; set; } = new List<string>() ;

        private static List<int> executionTimeHistory = new List<int>();

        private string message;
        private static SocketUser previousUser;

        public Chat(SocketCommandContext context)
        {
            if (context.IsPrivate) return;
            if (context.User.IsWebhook) return;
            if (context.Guild.Id != 271109067261476866UL) return;
            if (context.Message.MentionedUsers.Count > 0) return;

            message = context.Message.Content;
            if (previousUser != null)
            {
                if (previousUser.Id == context.User.Id)
                {
                    History[History.Count - 1] = History[History.Count - 1] + " " + message;
                    return;
                }
            }
            
            if (!(message.StartsWith(".") || message.StartsWith(">") || message.Equals("")))
            {
                History.Add(message);
                previousUser = context.User;
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
                    nextispossible = false;
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
                matchcount = 0;
            }
            if (possiblereplies.Count() == 0)
            {
                return;
            }
            string reply = possiblereplies[((int)(new Random().NextDouble() * possiblereplies.Count()))];
            executionTimeHistory.Add((int)(DateTime.Now - starttime).TotalMilliseconds);
            await context.Channel.SendMessageAsync(reply);
            foreach (var item in possiblereplies)
            {
                Console.WriteLine(item);
            }
            var json = JsonConvert.SerializeObject(executionTimeHistory, Formatting.Indented);
            File.WriteAllText("ExecutionTimeHistory.json", json);
        }
    }
}
