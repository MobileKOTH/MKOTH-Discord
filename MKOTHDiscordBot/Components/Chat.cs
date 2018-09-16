using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MKOTHDiscordBot.Services;
using Newtonsoft.Json;

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
            if (context.Channel.Id != ApplicationContext.MKOTHGuild.Official.Id) return;
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
            string json = "[" + File.ReadAllText(Directories.ChatHistoryFile) + "]";
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
                using (StreamWriter sw = File.AppendText(Directories.ChatHistoryFile))
                {
                    sw.Write(saveString);
                }
                Logger.Log("**Time used:** `" + (DateTime.Now - start).TotalMilliseconds.ToString() + " ms`".AddMarkDownLine() +
                    "**Lines:** " + saveRange.Count.ToString().AddMarkDownLine() +
                    $"**Height:** History - {History.Count} Last Save - {History.Count - saveRange.Count} Idle Time - {idleTime} seconds", LogType.ChatSaveTime);
                LoadHistory();
            }
        }

        public static async Task ReplyAsync(SocketCommandContext context, string message)
        {
            DateTimeOffset starttime = DateTime.Now;
            string reply = "";

            if (context.Channel.Id == ApplicationContext.MKOTHGuild.Suggestions.Id)
            {
                return;
            }

            message = TrimMessage(message);
            var words = message.ToLower().Split(' ');
            if (words.Length == 1)
            {
                if (words[0].Length < 2)
                {
                    return;
                }
            }
            await ResponseService.Instance.TriggerTypingAsync(context);

            var (rephrasePool, replyPool) = ProcessResponses(message);

            var wordcount = words.Length;
            var (responsePool, poolLog) = GetResponsePool(wordcount, rephrasePool, replyPool);
            var (possibleReplies, matchRate) = GetPossibleReplies(message, responsePool);

            Console.ForegroundColor = ConsoleColor.Yellow;
            possibleReplies.Take(10).ToList().ForEach(x => Logger.Debug(x, "Trash Replies"));
            Console.ResetColor();

            if (possibleReplies.Count() == 0)
            {
                possibleReplies.AddRange(responsePool);
            };

            reply = possibleReplies.SelectRandom().Message;

            Logger.Log(
                "**Time used:** `" + ((DateTime.Now - starttime).TotalMilliseconds).ToString() + " ms`".AddMarkDownLine() +
                "**Chat Trigger:** " + message.AddMarkDownLine() +
                "**Match Rate:** " + matchRate.ToString().AddMarkDownLine() +
                "**Pool:** " + poolLog.AddMarkDownLine() +
                "**Reply:** " + reply, LogType.TrashReply);

            await ResponseService.Instance.SendToContextAsync(context, reply);
            if (context.IsPrivate && context.User.Id != ApplicationContext.BotOwner.Id)
            {
                await ResponseService.Instance.SendToChannelAsync(ApplicationContext.TestGuild.BotTest, "DM chat received:", new EmbedBuilder()
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

        public static (List<TrashReply> triggers, List<TrashReply> replies) ProcessResponses(string message)
        {
            List<TrashReply> triggers = new List<TrashReply>();
            List<TrashReply> replies = new List<TrashReply>();
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
            return (triggers, replies);

            void process(List<string> historySet)
            {
                int wordcount = words.Length;
                double matchcount = 0;
                foreach (var history in historySet)
                {
                    lock (replies)
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

        public static (List<TrashReply> responsePool, string poolLog) GetResponsePool(int wordCount, List<TrashReply> rephrasePool, List<TrashReply> replyPool)
        {
            var responsePool = new List<TrashReply>();
            string poollog = "";
            double randomsource = new Random().NextDouble();
            switch (wordCount)
            {
                case 1:
                case 2:
                    responsePool = (randomsource > 0.2) ? rephrasePool : replyPool;
                    poollog = (randomsource > 0.2) ? nameof(rephrasePool) : nameof(replyPool);
                    break;

                case 3:
                    responsePool = (randomsource > 0.66) ? rephrasePool : replyPool;
                    poollog = (randomsource > 0.66) ? nameof(rephrasePool) : nameof(replyPool);
                    break;

                default:
                    responsePool = replyPool;
                    poollog = nameof(replyPool);
                    break;
            }

            return (responsePool, poollog);
        }

        public static (List<TrashReply> possibleReplies, double matchRate) GetPossibleReplies(string message, List<TrashReply> responsePool)
        {
            var possibleReplies = new List<TrashReply>();

            bool foundreply = false;
            var wordcount = message.GetWordCount();
            double wordcountmatch = wordcount;
            double matchRate = 0.9;
            do
            {
                if (wordcount > 4)
                {
                    matchRate -= 0.15;
                }
                else
                {
                    matchRate = wordcountmatch / wordcount;
                }
                foreach (var trashreply in responsePool)
                {
                    if (trashreply.Matchrate >= matchRate)
                    {
                        possibleReplies.Add(trashreply);
                        foundreply = true;
                    }
                }
                if (wordcountmatch <= 0)
                {
                    Logger.Log("**No chat results:** ".AddMarkDownLine() + message, LogType.NoTrashReplyFound);
                    break;
                }
                wordcountmatch--;
            } while (!foundreply);

            return (possibleReplies, matchRate);
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
