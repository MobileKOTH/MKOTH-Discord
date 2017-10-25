using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Discord.Commands;
using Discord.WebSocket;
using MKOTH_Discord_Bot.Utilities;

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
            if (context.Channel.Id != 347258242277310465UL) return;
            message = context.Message.Content;
            if (context.Message.MentionedUsers.Count > 0)
            {
                if (context.Message.MentionedUsers.Contains(context.Client.CurrentUser))
                {
                    return;
                }
                string CleanMessage = context.Message.Content;
                for (int i = 0; i < context.Message.MentionedUsers.Count; i++)
                {
                    CleanMessage = CleanMessage.Replace(context.Message.MentionedUsers.ElementAt(i).Mention, context.Message.MentionedUsers.ElementAt(i).Username);
                }
                message = CleanMessage.Trim();
            }
            message = message.Replace("@", "`@`");
            if ((message.StartsWith(".") || message.StartsWith(">") || message.Equals("")))
            {
                return;
            }

            if (previousUser != null)
            {
                if (previousUser.Id == context.User.Id)
                {
                    History[History.Count - 1] = History[History.Count - 1] + " " + message;
                    return;
                }
            }
            History.Add(message);
            previousUser = context.User;
        }

        public static void LoadHistory()
        {
            string json = File.ReadAllText(Directory.GetParent(Directory.GetCurrentDirectory()).FullName + "\\Data\\ChatHistory.json");
            History = JsonConvert.DeserializeObject<List<string>>(json);
        }

        public static void SaveHistory()
        {
            var json = JsonConvert.SerializeObject(History, Formatting.Indented);
            File.WriteAllText(Directory.GetParent(Directory.GetCurrentDirectory()).FullName + "\\Data\\ChatHistory.json", json);
        }

        public static async Task Reply(SocketCommandContext context, string message)
        {
            DateTimeOffset starttime = DateTime.Now;
            string reply = "";
            List<string> possiblereplies = new List<string>();
            List<TrashReply> replypool = new List<TrashReply>();
            if (context.Channel.Id == 347258242277310465UL)
            {
                possiblereplies.Add(context.User.Mention + ", lets talk in <#347166773642133515> shall we?");
                possiblereplies.Add(context.User.Mention + ", we do not want to flood the prestigious <#347258242277310465> with our trash talks.");
                possiblereplies.Add(context.User.Mention + ", no bot use in this channel :(");
                possiblereplies.Add(context.User.Mention + ", don't talk to me here!");
                possiblereplies.Add(context.User.Mention + ", I will tell mods to mute if you keep pinging me here :rage:");
                possiblereplies.Add(context.User.Mention + ", is'nt it no bot use in <#347258242277310465> :thinking: ");
                possiblereplies.Add(context.User.Mention + ", why am I replying to you here in <#347258242277310465>");
                reply = possiblereplies[((int)((new Random().NextDouble() * possiblereplies.Count())))];
                await Responder.SendToContext(context, reply);
                return;
            }
            if (context.Channel.Id == 347272877134839810UL)
            {
                possiblereplies.Add(context.User.Mention + ", I don't think you will need to talk to me for giving suggestions <:monekeyfacepalm:352423604216135680>");
                possiblereplies.Add(context.User.Mention + ", <:monkeyrage:352681458919407617><:monkeyrage:352681458919407617><:monkeyrage:352681458919407617><:monkeyrage:352681458919407617>, you are probably not giving a proper suggestion!");
                reply = possiblereplies[((int)((new Random().NextDouble() * possiblereplies.Count())))];
                await Responder.SendToContext(context, reply);
                return;
            }
            message = message.Replace(".", "");
            message = message.Replace(",", "");
            message = message.Replace("?", "");
            message = message.Replace("!", "");
            string[] words = message.ToLower().Split(' ');
            if (words.Length == 1)
            {
                if (words[0].Length < 2)
                {
                    return;
                }
            }

            Responder.TriggerTyping(context);

            int wordcount = words.Length;
            double matchcount = 0;

            foreach (var history in History)
            {
                if (history.Split(' ').Length > wordcount * 4) continue;
                foreach (var word in words)
                {
                    if (history.ToLower().Contains(word))
                    {
                        matchcount++;
                    }
                }
                replypool.Add(new TrashReply(history, matchcount / wordcount));
                matchcount = 0;
            }
            if (replypool.Count() == 0) return;
            bool foundreply = false;
            double wordcountmatch = wordcount;
            do
            {
                foreach (var trashreply in replypool)
                {
                    if (trashreply.Matchrate >= (wordcountmatch - ((wordcount - 4) > 0 ? (wordcount - 4) : 0)) / wordcount)
                    {
                        possiblereplies.Add(trashreply.Message);
                        foundreply = true;
                    }
                }
                if (wordcountmatch <= 0)
                {
                    Logger.Log("No chat results: ".AddLine() + message, LogType.NOREPLYFOUND);
                    break;
                }
                wordcountmatch--;
            } while (!foundreply);

            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var item in possiblereplies)
            {
                Console.WriteLine(item);
            }
            Console.ResetColor();

            if (possiblereplies.Count() == 0)
            {
                possiblereplies.AddRange(History);
            };
            Logger.Log(((DateTime.Now - starttime).TotalMilliseconds).ToString().AddSpace() + "ms".AddLine() + "Chat Trigger: " + message.AddLine() + "Match Rate: " + (wordcountmatch - ((wordcount - 4) > 0 ? (wordcount - 4) : 0)) / wordcount, LogType.TRASHREPLYTIME);

            reply = possiblereplies[((int)(new Random().NextDouble() * possiblereplies.Count()))];
            await Responder.SendToContext(context, reply);
        }
    }

    public class TrashReply
    {
        private string message = "";
        private double matchrate = 0;

        public TrashReply(string message, double matchrate)
        {
            this.Message = message;
            this.Matchrate = matchrate;
        }

        public string Message { get => message; set => message = value; }
        public double Matchrate { get => matchrate; set => matchrate = value; }
    }
}
