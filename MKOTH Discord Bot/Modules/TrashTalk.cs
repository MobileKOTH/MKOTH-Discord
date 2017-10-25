using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTH_Discord_Bot
{
    public class TrashTalk : ModuleBase<SocketCommandContext>
    {
        [Command("TrashInfo")]
        [Summary("")]
        [Alias("ti")]
        public async Task TrashInfo([Remainder] string message)
        {
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            if (Context.Guild.Id != Utilities.ContextPools.TestGuild.Guild.Id)
            {
                return;
            }

            List<TrashReply> triggers = new List<TrashReply>();
            List<TrashReply> replies = new List<TrashReply>();
            List<TrashReply> possiblereplies = new List<TrashReply>();
            Chat.ProcessResponses(ref message, ref triggers, ref replies);
            string[] words = message.ToLower().Split(' ');
            if (words.Length == 1)
            {
                if (words[0].Length < 2)
                {
                    msg = await ReplyAsync("Too little content");
                }
            }

            int wordcount = words.Length;
            bool foundreply = false;
            double wordcountmatch = wordcount;
            double matchrate = 1;
            do
            {
                matchrate = (wordcountmatch - ((wordcount - 4) > 0 ? (wordcount - 4) : 0)) / wordcount;
                foreach (var trashreply in triggers)
                {
                    if (trashreply.Matchrate >= matchrate)
                    {
                        possiblereplies.Add(trashreply);
                        foundreply = true;
                    }
                }
                if (wordcountmatch <= 0)
                {
                    msg = await ReplyAsync("No trigger key found for: \n" + message);
                    return;
                }
                wordcountmatch--;
            } while (!foundreply);
            for (int i = 0; i < (possiblereplies.Count > 25 ? 25 : possiblereplies.Count); i++)
            {
                if (possiblereplies[i].Message.Length > 200)
                {
                    possiblereplies[i].Message = possiblereplies[i].Message.Substring(0, 200) + "...";
                }
                embed.AddField("ID: " + triggers.IndexOf(possiblereplies[i]).ToString().AddSpace() + "Match: " + possiblereplies[i].Matchrate.ToString(), possiblereplies[i].Message);
            }
            embed.Title = "Triggers IDs and Keys";
            await ReplyAsync("Triggers and Replies for:\n" + message,false, embed.Build());

            possiblereplies = new List<TrashReply>();
            embed = new EmbedBuilder();

            wordcount = words.Length;
            foundreply = false;
            wordcountmatch = wordcount;
            matchrate = 1;
            do
            {
                matchrate = (wordcountmatch - ((wordcount - 4) > 0 ? (wordcount - 4) : 0)) / wordcount;
                foreach (var trashreply in replies)
                {
                    if (trashreply.Matchrate >= matchrate)
                    {
                        possiblereplies.Add(trashreply);
                        foundreply = true;
                    }
                }
                if (wordcountmatch <= 0)
                {
                    break;
                }
                wordcountmatch--;
            } while (!foundreply);
            for (int i = 0; i < (possiblereplies.Count > 25 ? 25 : possiblereplies.Count); i++)
            {
                if (possiblereplies[i].Message.Length > 200)
                {
                    possiblereplies[i].Message = possiblereplies[i].Message.Substring(0, 200) + "...";
                }
                embed.AddField("ID: " + replies.IndexOf(possiblereplies[i]).ToString().AddSpace() + "Match: " + possiblereplies[i].Matchrate.ToString(), possiblereplies[i].Message);
            }
            embed.Title = "Response IDs and Keys";
            await ReplyAsync("", false, embed.Build());
        }
    }
}
