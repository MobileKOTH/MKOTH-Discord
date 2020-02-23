using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Models;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Services;

namespace MKOTHDiscordBot.Modules
{
    [Summary("Contains the commands for MKOTH series and player activities.")]
    [Remarks("Module B")]
    public class Competitive : InteractiveBase
    {
        private readonly DiscordSocketClient client;
        private readonly SubmissionRateLimiter submissionRateLimiter;
        private readonly SeriesService seriesService;
        private readonly RankingService rankingService;
        private readonly TowerBanManager towerBanManager;
        private readonly RoleManager roleManager;
        private readonly ITextChannel logChannel;

        private readonly string prefix;

        public Competitive(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<SeriesService>();
            rankingService = services.GetService<RankingService>();
            submissionRateLimiter = services.GetService<SubmissionRateLimiter>();
            towerBanManager = services.GetService<TowerBanManager>();
            roleManager = services.GetService<RoleManager>();
            logChannel = (ITextChannel)client.GetChannel(appSettings.Value.Settings.DevelopmentGuild.Test);

            prefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;
        }
        [Command("Submit")]
        [RequireContext(ContextType.Guild)]
        public async Task Submit()
        {
            if (submissionRateLimiter.Audit(Context))
            {
                return;
            }

            await ReplyAsync("Self series submission is not available yet.\n" +
                $"Please send a submission manually at " +
                $"{(Context.Guild.Channels.First(x => x.Name == "series-submit") as ITextChannel).Mention} for an admin to process it.");
        }

        //[Command("reaction")]
        //public async Task Test_ReactionReply()
        //{
        //    var msg = await logChannel.SendMessageAsync("test");
        //    await msg.AddReactionAsync(new Emoji("👍"));
        //    await msg.AddReactionAsync(new Emoji("👎"));
        //    var callback = new InlineReactionCallback(Interactive, Context, new ReactionCallbackData("text", null, false, true)
        //        .WithCallback(new Emoji("👍"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👍"))
        //        .WithCallback(new Emoji("👎"), (c, r) => c.Channel.SendMessageAsync($"{r.User.Value.Mention} replied with 👎")));
        //    Interactive.AddReactionCallback(msg, callback);
        //}

        [Command("Refresh")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Refresh()
        {
            await ReplyAsync("Pulling from remote spreadsheet...");
            await seriesService.RefreshAsync();
            await ReplyAsync("Refresh complete.");
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [Summary("Administrator command to create a series, bypassing all restrictions.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, string inviteCode = "NA")
        {
            await CreateSeries(winner, loser, wins, loss, 0, inviteCode);
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
        {
            if (wins < loss)
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

        [Command("DeleteSeries")]
        [Alias("ds")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task DeleteSeries(int id)
        {
            try
            {
                await seriesService.RemoveAsync(id);
                await ReplyAsync("Series deleted.");
            }
            catch (Exception e)
            {
                await ReplyAsync(e.Message);
            }
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
            var playerRanking = rankingService.SeriesPlayers.Select((x, i) => new KeyValuePair<int, SeriesPlayer>(i + 1, x)).ToDictionary(x => x.Key, x => x.Value);
            
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Leaderboard")
                .WithDescription(rankingService.PrintRankingList(playerRanking.Take(10)));

            if (playerRanking.Values.Any(x => x.Id == Context.User.Id.ToString()))
            {
                var player = playerRanking.First(x => x.Value.Id == Context.User.Id.ToString());
                if (player.Key > 3)
                {
                    embed.AddField("Your Position", rankingService.PrintRankingList(playerRanking.Skip(player.Key - 2).Take(3)));
                }
            }
            await ReplyAsync(embed: embed.Build());
        }

        private EmbedBuilder ListTowers()
        {
            return new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Bannable Towers")
                .WithDescription(towerBanManager.ListTowners());
        }

        [Command("BannableTowers")]
        public async Task BannableTowers()
        {
            await ReplyAsync(embed: ListTowers().Build());
        }

        [Command("BanTower")]
        [Alias("b", "bt", "ban")]
        public async Task BanTower(Tower tower)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync("You can only select a tower to ban in our DM");
                return;
            }

            var session = towerBanManager.ProcessChoice(Context.User, tower);

            if (session == null)
            {
                await ReplyAsync("The ban tower session has ended or you are not in a session to select a tower to ban.");
                return;
            }
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"Click [here](https://discordapp.com/channels/{session.InitiateChannel.GuildId}/{session.InitiateChannel.Id}) to return to the channel.");
            await ReplyAsync("Ban Tower Choice: " + tower.ToString("g"), embed: embed.Build());
        }

        [Command("BanTower")]
        [Alias("b", "bt")]
        [RequireContext(ContextType.Guild)]
        public async Task BanTower(IUser user)
        {
            //if (user.Status == UserStatus.Offline)
            //{
            //    await ReplyAsync("User is offline");
            //    return;
            //}

            if (user == Context.User)
            {
                await ReplyAsync("You cannot choose yourself.");
                return;
            }

            if (!towerBanManager.StartSession(Context.User, user, Context.Channel as ITextChannel))
            {
                await ReplyAsync("Failed to start ban tower session. Perhaps someone is already in a session, please wait for them to finish.");
                return;
            }

            var dmEmbed = ListTowers()
                .AddField("Examples", $"`{prefix}{nameof(BanTower)} 1`\n`{prefix}{nameof(BanTower)} Dart`")
                .WithFooter($"You have {TowerBanManager.MAX_SESSION_SECONDS} seconds to make your choice.");

            try
            {
                var dmStarter = await Context.User.SendMessageAsync(embed: dmEmbed.Build());
                var dmOther = await user.SendMessageAsync(embed: dmEmbed.Build());
                var embed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithDescription($"A tower ban session has created with {user.Mention}. " +
                    $"Please select a tower to ban in our DM within {TowerBanManager.MAX_SESSION_SECONDS} seconds.\n\n" +
                    $"{Context.User.Mention}: You can click [here](https://discordapp.com/channels/@me/{dmStarter.Channel.Id}) to switch to our dm.\n" +
                    $"{user.Mention}: You can click [here](https://discordapp.com/channels/@me/{dmOther.Channel.Id}) to switch to our dm.");
                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                await ReplyAsync($"{e.Message}.\nThis command cannot work for one who has direct message disabled.");
            }
        }
    }
}
