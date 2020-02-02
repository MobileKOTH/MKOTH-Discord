using System;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Contains the commands for MKOTH series and player activities.")]
    [Remarks("Module B")]
    [RequireContext(ContextType.Guild)]
    public class Competitive : InteractiveBase
    {
        private readonly DiscordSocketClient client;
        private readonly SubmissionRateLimiter submissionRateLimiter;
        private readonly SeriesService seriesService;
        private readonly RankingService rankingService;
        private readonly ITextChannel logChannel;
        public Competitive(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<SeriesService>();
            rankingService = services.GetService<RankingService>();
            submissionRateLimiter = services.GetService<SubmissionRateLimiter>();
            logChannel = (ITextChannel)client.GetChannel(appSettings.Value.Settings.DevelopmentGuild.Test);
        }
        [Command("Submit")]
        public async Task Submit()
        {
            if (submissionRateLimiter.Audit(Context))
            {
                return;
            }

            await ReplyAsync("Test Submit");
        }

        [Command("reaction")]
        public async Task Test_ReactionReply()
        {
            var msg = await logChannel.SendMessageAsync("test");
            await msg.AddReactionAsync(new Emoji("👍"));
            await msg.AddReactionAsync(new Emoji("👎"));
            var callback = new InlineReactionCallback(Interactive, Context, new ReactionCallbackData("text", null, false, true)
                .WithCallback(new Emoji("👍"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👍"))
                .WithCallback(new Emoji("👎"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👎")));
            Interactive.AddReactionCallback(msg, callback);
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [Summary("Administrator command to create a series, bypassing all restrictions.")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, string inviteCode = "NA")
        {
            await CreateSeries(winner, loser, wins, loss, 0, inviteCode);
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
        {
            if (wins <= loss)
            {
                await ReplyAsync("Wins must greater than losses");
                return;
            }

            var series = seriesService.MakeSeries(winner.Id, loser.Id, wins, loss, draws, inviteCode);
            await seriesService.AdminCreateAsync(series);
            var embed = new EmbedBuilder()
                .WithDescription($"Id: {series.Id.ToString("D4")}\n Winner: <@!{series.WinnerId}>\n Loser: <@!{series.LoserId}>\n Score: {wins}-{loss} Draws: {draws}\n Invite Code: {inviteCode}")
                .WithColor(Color.Orange);
            await ReplyAsync(embed: embed.Build());
        }

        [Command("SeriesHistory")]
        [Alias("sh")]
        public async Task Serieshistory()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Series History")
                .WithDescription(seriesService.LastSeriesHistoryLines(10).JoinLines());
            await ReplyAsync(embed: embed.Build());
        }

        [Command("Leaderboard")]
        [Alias("r", "rankings", "rank", "ranking", "lb")]
        public async Task Ranking()
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Leaderboard")
                .WithDescription(rankingService.SeriesPlayers.Select((x, i) => $"`#{(i + 1).ToString("D2")}` `ELO: {x.Elo.ToString("N2")}` <@!{x.Id}>").JoinLines());
            await ReplyAsync(embed: embed.Build());
        }
    }
}
