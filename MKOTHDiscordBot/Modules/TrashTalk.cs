using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace MKOTHDiscordBot.Modules
{
    [Summary("The debug module for the chat system.")]
    [Remarks("Module Y")]
    public class TrashTalk : ModuleBase<SocketCommandContext>
    {
        [Command("TrashInfo", RunMode = RunMode.Async)]
        [Summary("Displays the possible response the bot will give when being pinged with the content.")]
        [Alias("ti")]
        [RequireBotTest]
        public async Task TrashInfo([Remainder] string message)
        {
            DateTime start = DateTime.Now;
            EmbedBuilder embed = new EmbedBuilder();
            IUserMessage msg;

            message = Chat.TrimMessage(message);
            string[] words = message.ToLower().Split(' ');
            if (words.Length == 1)
            {
                if (words[0].Length < 2)
                {
                    msg = await ReplyAsync("Too little content");
                }
            }

            var (triggers, _) = Chat.ProcessResponses(message);
            var (possiblereplies, matchRate) = Chat.GetPossibleReplies(message, triggers);
            if (matchRate <= 0)
            {
                await ReplyAsync($"No trigger key found for:\n \"{message}\"");
            }

            List<string> historyClone;
            List<string> historyClone2;
            lock (Chat.History)
            {
                historyClone = new List<string>(Chat.History);
                historyClone2 = new List<string>(Chat.History);
            }
            for (int i = 0; i < possiblereplies.Take(25).Count(); i++)
            {
                int index = historyClone.IndexOf(possiblereplies[i].Message);
                historyClone[index] = null;
                string trigger = historyClone2[index - 1];
                string rephrase = historyClone2[index];
                string response = historyClone2[index + 1];
                trigger = trigger.SliceBack(100);
                rephrase = rephrase.SliceBack(100);
                response = response.SliceBack(100);

                embed.AddField(
                    string.Format("{0:N2}%",
                    possiblereplies[i].Matchrate * 100),
                    $"`#{index - 1}` {trigger}\n" +
                    $"`#{index}` {rephrase}\n" +
                    $"`#{index + 1}` {response}");

                var omission = possiblereplies.Take(25).Count() < possiblereplies.Count ? possiblereplies.Count - possiblereplies.Take(25).Count() : 0;
                embed.WithFooter($"Results: {possiblereplies.Take(25).Count()}" + (omission > 0 ? $", omitted: {omission}" : ""));
            }
            embed.Title = "Trigger, rephrase and reply pool";
            embed.Description = "**Match %** `#ID Trigger` `#ID Rephrase` `#ID Reply`";
            await ReplyAsync($"`Process time: {(DateTime.Now - start).TotalMilliseconds.ToString()} ms`\nTrash info for:\n\"{message.SliceBack(100)}\"", false, embed.Build());
            await Task.CompletedTask;
            return;
        }

        [Command("TrashMessage", RunMode = RunMode.Async)]
        [Summary("Displays the message content of the trash Id.")]
        [Alias("tm")]
        [RequireBotTest]
        public async Task TrashMessage(int id)
        {
            var message = Chat.History[id];
            await ReplyAsync($"`Message Id: #{id}`\n\n", embed: new EmbedBuilder().WithDescription(message.SliceBack(1900)).Build());
        }

        [Command("SaveChat")]
        [RequireDeveloper]
        public async Task SaveChat()
        {
            Chat.SaveHistory();
            await ReplyAsync("Done.");
        }
    }
}
