using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Modules
{
    [Summary("The debug module for the chat system.")]
    [Remarks("Module Y")]
    public class TrashTalk : ModuleBase<SocketCommandContext>, IDisposable
    {
        private ChatService chatService;

        public TrashTalk(ChatService chatService)
        {
            this.chatService = chatService;
        }

        [Command("TrashInfo", RunMode = RunMode.Async)]
        [Summary("Displays the possible response the bot will give when being pinged with the content.")]
        [Alias("ti")]
        [RequireBotTest]
        public async Task TrashInfo([Remainder] string message)
        {
            DateTime start = DateTime.Now;
            var analysis = await chatService.ChatSystem.AnalyseAsync(message);
            var results = chatService.ChatSystem.GetResults(message, analysis).results;
            var takeResults = results.Take(25);
            var embed = new EmbedBuilder();

            foreach (var item in takeResults)
            {
                embed.AddField(
                    string.Format("{0:N2}%",
                    item.Score * 100),
                    $"`#{item.Trigger?.Id}` {item.Trigger?.Message.SliceBack(100)}\n" +
                    $"`#{item.Rephrase.Id}` {item.Rephrase.Message.SliceBack(100)}\n" +
                    $"`#{item.Response?.Id}` {item.Response?.Message.SliceBack(100)}");

                var omission = takeResults.Count() < results.Count() ? results.Count() - takeResults.Count() : 0;
                embed.WithFooter($"Results: {takeResults.Count()}" + (omission > 0 ? $", omitted: {omission}" : ""));
            }

            embed.Title = "Trigger, rephrase and reply pool";
            embed.Description = "**Match %** `#ID Trigger` `#ID Rephrase` `#ID Reply`";
            await ReplyAsync($"`Process time: {(DateTime.Now - start).TotalMilliseconds.ToString()} ms`\nTrash info for:\n\"{message.SliceBack(100)}\"", false, embed.Build());
        }

        [Command("TrashMessage", RunMode = RunMode.Async)]
        [Summary("Displays the message content of the trash Id.")]
        [Alias("tm")]
        [RequireBotTest]
        public async Task TrashMessage(int id)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription("".SliceBack(1900));
            await ReplyAsync($"`Message Id: #{id}`", embed: embed.Build());
        }

        public void Dispose()
        {
            chatService.Dispose();
        }
    }
}
