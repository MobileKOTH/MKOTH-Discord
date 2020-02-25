using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
using MKOTHDiscordBot.Utilities;

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

/*
        [Command("Reaction")]
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
*/

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

            if (inviteCode != "NA")
            {
                if (!isValidReplayId(inviteCode.ToUpper()))
                {
                    await ReplyAsync("Invalid invite code.");
                    return;
                }
            }

            var series = seriesService.MakeSeries(winner.Id, loser.Id, wins, loss, draws, inviteCode.ToUpper());
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
            var limit = 30;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Series History")
                .WithDescription(seriesService.LastSeriesHistoryLines(limit).JoinLines())
                .WithFooter($"Displaying up to last {limit} series.");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("Leaderboard")]
        [Alias("rankings", "rank", "ranking", "lb")]
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
                if (player.Key > 10)
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
        public async Task BanTower(IGuildUser user)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync($"Use `{prefix}{nameof(Challenge)}` to challenge someone on a series and to start a ban tower session." +
                    "\nYou can only select a tower to ban in our DM.");
            }
            return;
        }

        [Command("BanTower")]
        [Alias("b", "bt", "ban")]
        public async Task BanTower(Tower tower)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync($"Use `{prefix}{nameof(Challenge)}` to challenge someone on a series and to start a ban tower session." + 
                    "\nYou can only select a tower to ban in our DM.");
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

        [Command("Challenge")]
        [Alias("ch")]
        [RequireContext(ContextType.Guild)]
        public async Task Challenge(IGuildUser user)
        {
            if (user == Context.User)
            {
                await ReplyAsync("You cannot choose yourself.");
                return;
            }

            if (user.RoleIds.Any(x => x == 347300513110294528))
            {
                await ReplyAsync("You cannot challenge a VIP in this server");
                return;
            }

            var lastDaySeries = seriesService.SeriesHistory.Reverse().TakeWhile(x => (DateTime.Now - x.Date).TotalHours < 12);
            var players = new[] { user.Id, Context.User.Id };
            var conflictSeries = lastDaySeries.FirstOrDefault(x => players.Contains(ulong.Parse(x.WinnerId)) && players.Contains(ulong.Parse(x.LoserId)));
            if (conflictSeries != default)
            {
                await ReplyAsync($"You already played this person less than a day ago, " +
                    $"please wait for {(conflictSeries.Date.AddHours(12) - DateTime.Now).AsRoundedDuration()}.");
                return;
            }

            var challengeEmbed = new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithTitle("Series Challenge")
                .WithDescription("You are challenging " + user.Mention + " on a series.\n" +
                "Do you both agree to vote for towers to ban?\n" +
                "✅ Agree ❌ Disagree 🚶 Reject Challenge")
                .WithFooter($"Both of you have {TowerBanManager.MAX_SESSION_SECONDS} seconds to decide.");

            bool leftAgree = false, rightAgree = false, denied = false;
            int voteCount = 0;

            var reactionCallBackData = new ReactionCallbackData(string.Empty,
                embed: challengeEmbed.Build(),
                false, // Expires after use
                true,  // Single use per user
                TimeSpan.FromSeconds(TowerBanManager.MAX_SESSION_SECONDS),
                async timeOut =>
                {
                    if (voteCount == 2 || denied)
                    {
                        return;
                    }
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithDescription($"{Context.User.Mention} {user.Mention} Challenge session has timed out.").Build());
                }).WithCallback(new Emoji("✅"), async (c, e) =>
                {
                    if (e.UserId == Context.User.Id || e.UserId == user.Id)
                    {
                        if (e.UserId == Context.User.Id)
                        {
                            leftAgree = true;
                        }
                        if (e.UserId == user.Id)
                        {
                            rightAgree = true;
                        }
                        voteCount++;
                    }
                    if (leftAgree && rightAgree)
                    {
                        await BanTowerSession(user);
                    }
                })
                .WithCallback(new Emoji("❌"), async (c, e) =>
                {
                    if (e.UserId == Context.User.Id || e.UserId == user.Id)
                    {
                        voteCount++;
                        if (denied)
                        {
                            return;
                        }
                        await ReplyAsync(embed: new EmbedBuilder()
                          .WithDescription($"{Context.User.Mention} {user.Mention} One of you disagreed to have towers banned. " +
                          $"You may begin your series without tower bans.")
                          .Build());
                        denied = true;
                    }
                })
                .WithCallback(new Emoji("🚶"), async (c, e) =>
                {
                    if (e.UserId == Context.User.Id || e.UserId == user.Id)
                    {
                        voteCount++;
                        if (denied)
                        {
                            return;
                        }
                        await ReplyAsync(embed: new EmbedBuilder()
                          .WithDescription($"{e.User.Value.Mention} has rejected the challenge")
                          .Build());
                        denied = true;
                    }
                });

            await InlineReactionReplyAsync(reactionCallBackData, false);
        }

        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(double a = 1200, double b = 1200, byte wins = 0, byte losses = 0, byte draws = 0, double kFactor = 40)
        {
            var (eloLeft, eloRight) = EloCalculator.Calculate(kFactor, a, b, wins, losses, draws);
            var diffLeft = eloLeft - a;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Elo calculator")
                .WithDescription($"`A = {a}` `B = {b}` `wins = {wins}` `losses = {losses}` `draws = {draws}` `K factor = {kFactor}`")
                .AddField("Elo A", $"`{a.ToString("N2")} -> {eloLeft.ToString("N2")}`", true)
                .AddField("Elo B", $"`{b.ToString("N2")} -> {eloRight.ToString("N2")}`", true)
                .AddField("Difference", $"`{(diffLeft <= 0 ? "" : "+")}{diffLeft.ToString("N2")}`");

            await ReplyAsync(embed: embed.Build());
        }

        private async Task BanTowerSession(IGuildUser user)
        {
            if (!towerBanManager.StartSession(Context.User, user, Context.Channel as ITextChannel))
            {
                await ReplyAsync("Failed to start ban tower session. Perhaps someone is already in a session, please wait for them to finish.");
                return;
            }

            var dmEmbed = ListTowers()
                .AddField("Examples",
                $"`{prefix}{nameof(BanTower)} 1`\n" +
                $"`{prefix}{nameof(BanTower)} Dart`\n" +
                $"`{prefix}b 1`\n" +
                $"`{prefix}b dart`")
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

        private bool isValidReplayId(string id)
        {
            return Regex.IsMatch(id, "^[A-Z]{7}$");
        }

        [Command("Replay")]
        [Alias("r")]
        public async Task ReplayLink(string inviteCode)
        {
            inviteCode = inviteCode.ToUpper();
            if (!isValidReplayId(inviteCode))
            {
                await ReplyAsync("Invalid invite code.");
                return;
            }
            await ReplyAsync("https://battles.tv/watch/" + inviteCode);
        }

        [Command("Replay")]
        [Alias("r")]
        public async Task ReplayLink(int seriesId)
        {
            var series = seriesService.SeriesHistory.FirstOrDefault(x => x.Id == seriesId);

            if (series == default)
            {
                await ReplyAsync("Series not found.");
                return;
            }

            if (series.ReplayId == "NA")
            {
                await ReplyAsync("Series does not contain a replay");
                return;
            }

            await ReplyAsync("https://battles.tv/watch/" + series.ReplayId);
        }
    }
}
