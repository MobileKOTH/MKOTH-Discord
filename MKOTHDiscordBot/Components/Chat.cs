using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Utilities;

namespace MKOTHDiscordBot
{
    public class Chat
    {
        public static List<string> History { get; set; } = new List<string>();

        private static int lastSaveIndex = 0;

        private string message;
        private static SocketUser previousUser;
        private static DateTime lastSameUserChatTime = DateTime.Now;

        public Chat(SocketCommandContext context)
        {
            if (context.IsPrivate) return;
            if (context.User.IsWebhook) return;
            if (context.Channel.Id != Globals.MKOTHGuild.Official.Id) return;
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
                    CleanMessage = CleanMessage.Replace("<@" + context.Message.MentionedUsers.ElementAt(i).Id.ToString(), "<@!" + context.Message.MentionedUsers.ElementAt(i).Id.ToString());
                    CleanMessage = CleanMessage.Replace(context.Message.MentionedUsers.ElementAt(i).Mention, context.Message.MentionedUsers.ElementAt(i).Username);
                }
                message = CleanMessage.Trim();
            }
            message = message.Replace("@", "`@`");
            if (new string[] { ".", ">", "?", "!" }.Any(x => message.StartsWith(x)) || message == "")
            {
                return;
            }
            if (History.Last() == message)
            {
                return;
            }

            lock (History)
            {
                lastSameUserChatTime = DateTime.Now;
                if (previousUser != null)
                {
                    if (previousUser.Id == context.User.Id)
                    {
                        History[History.Count - 1] = History[History.Count - 1] + " " + message;
                        return;
                    }
                }
                History.Add(message);
            }
            previousUser = context.User;
        }

        public static void LoadHistory()
        {
            string json = "[" + File.ReadAllText(Globals.Directories.ChatHistoryFile) + "]";
            History = JsonConvert.DeserializeObject<List<string>>(json);
            lastSaveIndex = History.Count;
            previousUser = null;
        }

        public static void SaveHistory()
        {
            var idleTime = (DateTime.Now - lastSameUserChatTime).TotalSeconds;
            if (History.Count > lastSaveIndex && idleTime > 10)
            {
                var saveRange = History.GetRange(lastSaveIndex, History.Count - lastSaveIndex);
                lastSaveIndex = History.Count;
                var saveString = "";
                saveRange.ForEach(x => saveString += JsonConvert.SerializeObject(x) + ",".AddLine());
                DateTime start = DateTime.Now;
                using (StreamWriter sw = File.AppendText(Globals.Directories.ChatHistoryFile))
                {
                    sw.Write(saveString);
                }
                Logger.Log("**Time used:** `" + (DateTime.Now - start).TotalMilliseconds.ToString() + " ms`".AddMarkDownLine() +
                    "**Lines:** " + saveRange.Count.ToString().AddMarkDownLine() +
                    $"**Height:** History - {History.Count} Last Save - {History.Count - saveRange.Count} Idle Time - {idleTime} seconds", LogType.CHATSAVETIME);
                LoadHistory();
            }
        }

        public static async Task ReplyAsync(SocketCommandContext context, string message)
        {
            DateTimeOffset starttime = DateTime.Now;
            string reply = "";
            List<string> possiblereplies = new List<string>();
            List<TrashReply> rephrasepool = new List<TrashReply>();
            List<TrashReply> replypool = new List<TrashReply>();
            List<TrashReply> responsepool = new List<TrashReply>();

            if (context.Channel.Id == Globals.MKOTHGuild.Suggestions.Id)
            {
                possiblereplies.Add(context.User.Mention + ", I don't think you will need to talk to me for giving suggestions <:monekeyfacepalm:352423604216135680>");
                possiblereplies.Add(context.User.Mention + ", <:monkeyrage:352681458919407617><:monkeyrage:352681458919407617><:monkeyrage:352681458919407617><:monkeyrage:352681458919407617>, you are probably not giving a proper suggestion!");
                reply = possiblereplies.SelectRandom();
                await Responder.SendToContext(context, reply);
                return;
            }

            message = TrimMessage(message);
            string[] words = message.ToLower().Split(' ');
            if (words.Length == 1)
            {
                if (words[0].Length < 2)
                {
                    return;
                }
            }
            await Responder.TriggerTyping(context);

            ProcessResponses(message, rephrasepool, replypool);

            int wordcount = words.Length;
            string poollog = "";
            double randomsource = new Random().NextDouble();
            switch (wordcount)
            {
                case 1:
                case 2:
                    responsepool = (randomsource > 0.2) ? rephrasepool : replypool;
                    poollog = (randomsource > 0.2) ? nameof(rephrasepool) : nameof(replypool);
                    break;

                case 3:
                    responsepool = (randomsource > 0.66) ? rephrasepool : replypool;
                    poollog = (randomsource > 0.66) ? nameof(rephrasepool) : nameof(replypool);
                    break;

                default:
                    responsepool = replypool;
                    poollog = nameof(replypool);
                    break;
            }

            bool foundreply = false;
            double wordcountmatch = wordcount;
            double matchrate = 0.9;
            do
            {
                if (wordcount > 4)
                {
                    matchrate -= 0.15;
                }
                else
                {
                    matchrate = wordcountmatch / wordcount;
                }
                foreach (var trashreply in responsepool)
                {
                    if (trashreply.Matchrate >= matchrate)
                    {
                        possiblereplies.Add(trashreply.Message);
                        foundreply = true;
                    }
                }
                if (wordcountmatch <= 0)
                {
                    Logger.Log("**No chat results:** ".AddMarkDownLine() + message, LogType.NOREPLYFOUND);
                    break;
                }
                wordcountmatch--;
            } while (!foundreply);

            Console.ForegroundColor = ConsoleColor.Yellow;
            possiblereplies.Take(10).ToList().ForEach(x => Console.WriteLine(x));
            Console.ResetColor();

            if (possiblereplies.Count() == 0)
            {
                lock (History)
                {
                    possiblereplies.AddRange(History);
                }
            };

            reply = possiblereplies.SelectRandom();

            Logger.Log(
                "**Time used:** `" + ((DateTime.Now - starttime).TotalMilliseconds).ToString() + " ms`".AddMarkDownLine() +
                "**Chat Trigger:** " + message.AddMarkDownLine() +
                "**Match Rate:** " + matchrate.ToString().AddMarkDownLine() +
                "**Pool:** " + poollog.AddMarkDownLine() +
                "**Reply:** " + reply, LogType.TRASHREPLY);

            await Responder.SendToContext(context, reply);
            if (context.IsPrivate && context.User.Id != Globals.BotOwner.Id)
            {
                await Responder.SendToChannel(Globals.TestGuild.BotTest, "DM chat received:", new EmbedBuilder()
                    .WithAuthor(context.User)
                    .WithDescription(message)
                    .AddField("Response", reply)
                    .Build());
            }
        }

        public static string TrimMessage(string message)
        {
            message = message.Replace(".", "");
            message = message.Replace(",", "");
            message = message.Replace("?", "");
            message = message.Replace("!", "");
            return message;
        }

        public static void ProcessResponses(string message, List<TrashReply> triggers, List<TrashReply> replies)
        {
            List<string> historyClone;
            lock (History)
            {
                historyClone = new List<string>(History);
            }

            string[] words = message.ToLower().Split(' ');

            int splitCount = 1 + words.Length / 33;
            splitCount = splitCount > Environment.ProcessorCount ? Environment.ProcessorCount : splitCount;

            var histories = historyClone.Split(splitCount);
            var workers = new Task[splitCount];
            for (int i = 0; i < splitCount; i++)
            {
                var set =  new List<string>(histories[i]);
                workers[i] = (Task.Run(() => process(set)));
            }
            Task.WaitAll(workers);

            void process(List<string> historySet)
            {
                int wordcount = words.Length;
                double matchcount = 0;
                foreach (var history in historySet)
                {
                    lock(replies)
                    {
                        replies.Add(new TrashReply(history, matchcount / wordcount));
                    }
                    matchcount = 0;
                    if (history.Split(' ').Length > wordcount * 4) continue;
                    foreach (var word in words)
                    {
                        if (history.ToLower().Contains(word))
                        {
                            matchcount++;
                        }
                    }
                    lock(triggers)
                    {
                        triggers.Add(new TrashReply(history, matchcount / wordcount));
                    }
                }
            }
        }
    }

    public class TrashReply
    {
        public string Message { get; set; }
        public double Matchrate { get; set; }

        public TrashReply(string message, double matchrate)
        {
            Message = message;
            Matchrate = matchrate;
        }
    }
}
