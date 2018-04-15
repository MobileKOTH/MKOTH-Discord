using Discord.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Management;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides the various information about MKOTH.")]
    [Remarks("Module B")]
    public class Information : ModuleBase<SocketCommandContext>
    {
        [Command("Ranking")]
        [Alias("rank", "lb", "Ranking")]
        [Summary("Gets the officially updated ranking infomation.")]
        public async Task Ranking()
        {
            var player = Player.Fetch(Context.User.Id);
            string topTenField = "";
            Player.List
                .Where(x => x.Rank >= 1)
                .OrderBy(x => x.Rank)
                .Take(10)
                .ToList()
                .ForEach(x =>
                {
                    topTenField += x.GetRankFieldString(x.Rank == player.Rank);
                });

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithUrl("https://docs.google.com/spreadsheets/d/1VRfWwvRSMQizzBanGNRMFVzoYFthrsNKzOgF5wKVM5I")
                .WithTitle("Full ranking sheet")
                .WithDescription("[MKOTH Series Data](https://docs.google.com/spreadsheets/d/1VRfWwvRSMQizzBanGNRMFVzoYFthrsNKzOgF5wKVM5I)")
                .AddField("Top ten", topTenField);

            if (!player.IsUnknown && player.Rank > 10)
            {
                var orderedRank = Player.List.OrderBy(x => x.Rank);
                var previous = orderedRank.First(x => x.Rank == player.Rank - 1);
                var next = orderedRank.First(x => x.Rank == player.Rank + 1);
                string playerField = previous.GetRankFieldString() + player.GetRankFieldString(true) + next == null ? "" : next.GetRankFieldString();
                embed.AddField("Your rank", playerField);
            }
            if (player.IsHoliday)
            {
                var orderedRank = Player.List.OrderBy(x => x.PlayerClass).Where(x => x.IsHoliday).ToList();
                var playerindex = orderedRank.FindIndex(x => x == player);
                var previous = orderedRank[playerindex - 1];
                var next = playerindex + 1 >= orderedRank.Count ? null : orderedRank[playerindex + 1];
                string playerField = previous.GetRankFieldString(false, true) + player.GetRankFieldString(true, true) + (next == null ? "" : next.GetRankFieldString(false, true));
                embed.AddField("You are in holiday mode", playerField);
            }

            await ReplyAsync(string.Empty, embed: embed.Build());
        }
    }
}
