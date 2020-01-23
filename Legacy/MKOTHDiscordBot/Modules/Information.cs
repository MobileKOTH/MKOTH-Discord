using Discord;
using Discord.Commands;
using MKOTHDiscordBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Provides the various information about MKOTH.")]
    [Remarks("Module C")]
    public class Information : ModuleBase<SocketCommandContext>
    {
        private Lazy<RankingSystem> rankingSystemLazy;
        private RankingSystem RankingSystem => rankingSystemLazy.Value;

        public Information(IServiceProvider service)
        {
            rankingSystemLazy = new Lazy<RankingSystem>(() => new RankingSystem(Context.Guild.Id, service));
        }

        [Command("DiscordServer")]
        [Alias("OfficialServer", "OfficialServerLink")]
        [Summary("Gets the Discord invite link to Official MKOTH server.")]
        public async Task DiscordServer() 
            => await ReplyAsync("https://discord.me/MKOTH");

        [Command("Rank")]
        public async Task Rank()
        {
            var rankings = RankingSystem.Processor.Rankings;

            var topTenField = rankings.Select(x =>
            {
                var name = $"<@!{x.Player.DiscordId}>";
                var rank = x.Position.ToString().PadLeft(2, '0');
                var elo = x.Player.EloHistory.Count < RankingSystem.Processor.GuildSetting.Value.EloTBDGames
                ? "TBD: " + (RankingSystem.Processor.GuildSetting.Value.EloTBDGames - x.Player.EloHistory.Count).ToString().PadLeft(2, '0')
                : x.Player.EloHistory.Last().Elo.ToString("N2");

                return $"`#{rank}`\t" +
                $"`{elo}`\t`{x.Player.Points.ToString().PadLeft(2, '0').PadRight(3, ' ')}p`" +
                $"\t {RankingSystem.Processor.GetTier(x.Player).Icon} {name}";
            }).Forward(x => StringUtilities.LineJoin(x));

            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/341163606605299716/352026213306335234/crown.png")
                .WithAuthor("Leaderboard", "https://cdn.discordapp.com/attachments/341163606605299716/352026221481164801/ranking.png")
                .AddField("Top ten", topTenField);

            await ReplyAsync(string.Empty, embed: embed.Build());
        }
    }
}
