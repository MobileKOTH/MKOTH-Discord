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
        [Alias("Rank", "lb", "Rankings", "Leaderboard")]
        [Summary("Gets the officially updated ranking infomation. Use with a input `<user>` to check the player's rank.")]
        public async Task Ranking(IUser user = null)
        {
            var player = Player.Fetch((user ?? Context.User).Id);
            string topTenField = "";
            Player.List
                .Where(x => x.Rank >= 1)
                .OrderBy(x => x.Rank)
                .Take(10)
                .ToList()
                .ForEach(x =>
                {
                    topTenField += x.GetRankFieldString(identify: x.Rank == player.Rank);
                });

            EmbedBuilder embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/352026213306335234/crown.png")
                .WithUrl("https://docs.google.com/spreadsheets/d/1VRfWwvRSMQizzBanGNRMFVzoYFthrsNKzOgF5wKVM5I")
                .WithAuthor("MKOTH Leaderboard", "https://cdn.discordapp.com/attachments/341163606605299716/352026221481164801/ranking.png")
                .WithDescription(
                "You can also visit <#347259261921001472> to find out the ranks.\n" +
                "Click [here](https://docs.google.com/spreadsheets/d/1VRfWwvRSMQizzBanGNRMFVzoYFthrsNKzOgF5wKVM5I) to access the full ranking and statistics.")
                .AddField("Top ten", topTenField);

            if (!player.IsUnknown && player.Rank > 10)
            {
                var orderedRank = Player.List.OrderBy(x => x.Rank);
                var previous = orderedRank.First(x => x.Rank == player.Rank - 1);
                var next = orderedRank.FirstOrDefault(x => x.Rank == player.Rank + 1);
                string playerField = previous.GetRankFieldString() + player.GetRankFieldString(true) + (next == null ? "" : next.GetRankFieldString());
                embed.AddField((user == null ? "Your rank" : "The player's rank"), playerField);
            }
            if (player.IsHoliday)
            {
                var orderedRank = Player.List.OrderBy(x => x.Class).Where(x => x.IsHoliday).ToList();
                var playerindex = orderedRank.FindIndex(x => x == player);
                var previous = orderedRank[playerindex - 1];
                var next = playerindex + 1 >= orderedRank.Count ? null : orderedRank[playerindex + 1];
                string playerField = previous.GetRankFieldString(false, true) + player.GetRankFieldString(true, true) + (next == null ? "" : next.GetRankFieldString(false, true));
                embed.AddField((user == null ? "You are" : "The player is") + " in holiday mode", playerField);
            }

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("SeriesForm")]
        [Alias("sf", "SubmissionForm", "SubmitForm")]
        [Summary("Gets the link for MKOTH series submission form")]
        public async Task SeriesForm()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithUrl("https://docs.google.com/forms/d/e/1FAIpQLSdGJnCOl0l5HjxuYexVV_sOKPR1iScq3eiSxGiqKULX3zG4-Q/viewform")
                .WithTitle("MKOTH Series Submission")
                .WithDescription(
                "**Make sure you and your opponent have enough points if the series requires, or else the series will be downgraded to point series!**\n" +
                "Please read the series submission rules at <#347259261921001472>.\n" +
                "Remember to get your personal submission identification code via `.myid` command.\n\n" +
                "Click [here](https://docs.google.com/forms/d/e/1FAIpQLSdGJnCOl0l5HjxuYexVV_sOKPR1iScq3eiSxGiqKULX3zG4-Q/viewform) for the submission form.");

            await ReplyAsync(string.Empty, embed: embed.Build());
        }

        [Command("SeriesPoints")]
        [Alias("pointsystem","ps")]
        [Summary("List the MKOTH series point system rules.")]
        public async Task SeriesPoints()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .AddField("Cost points",
                "KING SERIES : 15 for challenger\n" +
                "KNIGHT SERIES: 18 for challenger\n" +
                "VASSAL vs SQUIRE RANKED SERIES : 12 for VASSAL\n" +
                "PEASANT vs VASSAL RANKED SERIES : 6 for PEASANT\n" +
                "SAME CLASS vs SAME CLASS RANKED SERIES : FREE")
                .AddField("Reward points",
                "KING SERIES any winner : 7\n" +
                "KNIGHT SERIES - Knight wins: 5\n" +
                "RANKED SERIES win vs NOBLEMAN: 7\n" +
                "RANKED SERIES win vs SQUIRE: 5\n" +
                "RANKED SERIES win vs VASSAL: 3\n" +
                "RANKED SERIES win vs PEASANT: 2\n" +
                "POINT SERIES win vs ANY: 2");

            await ReplyAsync(string.Empty, embed: embed.Build());
        }

        [Command("SignUp")]
        [Summary("Gets the MKOTH sign up form.")]
        public async Task SignUp()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithUrl("https://docs.google.com/forms/d/e/1FAIpQLSfj9lTpuVJmet-ceh8ac3IVBNAPR-XpR_sLeOefEZt5vH4vUw/viewform")
                .WithTitle("MKOTH Sign Up")
                .WithDescription(
                "**Alternate accounts will result in a permanent ban.**\n" +
                "Please familiarise with the ranking system and series rules of MKOTH at <#347259261921001472> or " +
                "[MKOTH Webpage](https://mobilekoth.wordpress.com/ranking-system/) before signing up.\n\n" +
                "Click [here](https://docs.google.com/forms/d/e/1FAIpQLSfj9lTpuVJmet-ceh8ac3IVBNAPR-XpR_sLeOefEZt5vH4vUw/viewform) for the sign up form.");

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("Invite")]
        [Alias("InviteLink")]
        [Summary("Gets the Discord invite link to this server.")]
        public async Task Invite()
        {
            await ReplyAsync("https://discord.gg/JBDHjHF");
        }
    }
}
