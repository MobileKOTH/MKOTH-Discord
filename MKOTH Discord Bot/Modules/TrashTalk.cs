using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MKOTHDiscordBot
{
    public class TrashTalk : ModuleBase<SocketCommandContext>
    {
        [Command("TrashInfo")]
        [Summary("")]
        [Alias("ti")]
        public async Task TrashInfo([Remainder] string message)
        {
            DateTime start = DateTime.Now;
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
                foreach (var trashreply in triggers)
                {
                    if (trashreply.Matchrate >= matchrate)
                    {
                        possiblereplies.Add(trashreply);
                        foundreply = true;
                    }
                }
                if (matchrate <= 0)
                {
                    msg = await ReplyAsync("No trigger key found for: \n" + message);
                    return;
                }
                wordcountmatch--;
            } while (!foundreply);
            for (int i = 0; i < (possiblereplies.Count > 25 ? 25 : possiblereplies.Count); i++)
            {
                int index = Chat.History.IndexOf(possiblereplies[i].Message);
                string trigger = Chat.History[index - 1];
                string rephrase = Chat.History[index];
                string response = Chat.History[index + 1];
                trigger = trigger.Length > 200 ? trigger.Substring(0, 200) + "..." : trigger;
                rephrase = rephrase.Length > 200 ? rephrase.Substring(0, 200) + "..." : rephrase;
                response = response.Length > 200 ? response.Substring(0, 200) + "..." : response;


                embed.AddField(string.Format("{0:N2}%", possiblereplies[i].Matchrate * 100), $"`#{index - 1}` {trigger}\n`#{index}` {rephrase}\n`#{index + 1}` {response}");
            }
            embed.Title = "Trigger, Rephrase and Reply Pool";
            await ReplyAsync($"`Process time: {(DateTime.Now - start).TotalMilliseconds.ToString()} ms`\nTrash info for:\n\"" + message + "\"".AddLine() + "**Match %** `#ID Trigger` `#ID Rephrase` `#ID Reply`", false, embed.Build());
        }
    }
}
