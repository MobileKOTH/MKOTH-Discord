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

        [Command("Challenge")]
        [Alias("ch")]
        [RequireContext(ContextType.Guild)]
        [Summary("Challenge a user on a series, decide if to have towers banned. Only one has to agree to have towers banned.\n" +
            "Please also refer to the [series rules and ranking system](http://mobilekoth.github.io/system).")]
        [Cooldown(10000, 2)]
        public async Task Challenge(IGuildUser user)
        {
            if (user == Context.User)
            {
                await ReplyAsync("You cannot choose yourself.");
                return;
            }

            if (user.IsBot)
            {
                await ReplyAsync("You can only challenge a human.");
                return;
            }

            if (user.RoleIds.Any(x => x == 347300513110294528))
            {
                await ReplyAsync("You cannot challenge a VIP in this server");
                return;
            }

            var lastDaySeries = seriesService.SeriesHistory.Reverse().TakeWhile(x => (DateTime.Now - x.Date).TotalHours < 12);
            var playerIds = new[] { user.Id.ToString(), Context.User.Id.ToString() };
            var conflictSeries = lastDaySeries.FirstOrDefault(x => playerIds.Contains(x.WinnerId) && playerIds.Contains(x.LoserId));
            if (conflictSeries != default)
            {
                await ReplyAsync($"You already played this person less than a day ago, " +
                    $"please wait for {(conflictSeries.Date.AddHours(12) - DateTime.Now).AsRoundedDuration()}.");
                return;
            }

            var player1 = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == Context.User.Id.ToString());
            var player2 = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == user.Id.ToString());
            var player1Elo = player1?.Elo ?? 1200;
            var player2Elo = player2?.Elo ?? 1200;
            var lower = player1Elo < player2Elo ? player1 : player2;

            if ((player1Elo > 1300 || player2Elo > 1300) && Math.Abs(player1Elo - player2Elo) >= 300)
            {
                if ((lower?.Points ?? 0) < 5)
                {
                    await ReplyAsync("If a player's elo is more than 1300 and the Elo difference of the players is more than 300, the lower Elo player needs to have at least 5 points earned to challenge.");
                    return;
                }
            }

            var banEmote = await rankingService.RankingChannel.Guild.GetEmoteAsync(683258477421920342);

            var challengeEmbed = new EmbedBuilder()
                .WithAuthor(Context.User)
                .WithTitle("Series Challenge")
                .WithDescription(Context.User.Mention + " is challenging " + user.Mention + " on a series.\n" +
                "Please decide if to ban towers. Both must respond, but only one has to agree to ban towers.\n" +
                "<:ban:683258477421920342> Ban Towers ⭕ No Ban Towers 🚶 Reject Challenge")
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
                        .WithDescription($"{Context.User.Mention} {user.Mention} Challenge session has timed out.")
                        .Build());
                }).WithCallback(banEmote, async (c, e) =>
                {
                    if (!(e.UserId == Context.User.Id || e.UserId == user.Id) || denied)
                    {
                        return;
                    }
                    leftAgree = e.UserId == Context.User.Id;
                    rightAgree = e.UserId == user.Id;
                    voteCount++;
                    await handleVote();
                })
                .WithCallback(new Emoji("⭕"), async (c, e) =>
                {
                    if (!(e.UserId == Context.User.Id || e.UserId == user.Id) || denied)
                    {
                        return;
                    }
                    voteCount++;
                    await handleVote();
                })
                .WithCallback(new Emoji("🚶"), async (c, e) =>
                {
                    if (!(e.UserId == Context.User.Id || e.UserId == user.Id) || denied)
                    {
                        return;
                    }
                    if (e.UserId == Context.User.Id || e.UserId == user.Id)
                    {
                        voteCount++;
                        denied = true;
                        await ReplyAsync(embed: new EmbedBuilder()
                          .WithDescription($"{e.User.Value.Mention} has rejected the challenge")
                          .Build());
                    }
                });

            async Task handleVote()
            {
                if (voteCount != 2)
                {
                    return;
                }
                if (leftAgree || rightAgree)
                {
                    await ReplyAsync(embed: new EmbedBuilder()
                      .WithDescription($"{Context.User.Mention} {user.Mention} One or both of you have agreed to have towers banned. ")
                      .Build());
                    await BanTowerSession(user);
                }
                else
                {
                    await ReplyAsync(embed: new EmbedBuilder()
                      .WithDescription($"{Context.User.Mention} {user.Mention} Both of you have disagreed to have towers banned. You may begin your series with no tower banned.")
                      .Build());
                }
            }

            await InlineReactionReplyAsync(reactionCallBackData, false);
        }

        private async Task BanTowerSession(IGuildUser user)
        {
            if (!towerBanManager.StartSession(Context.User, user, Context.Channel as ITextChannel))
            {
                await ReplyAsync("Failed to start a tower banning session. Perhaps someone is already in a session, please wait for them to finish.");
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
                    .WithDescription($"A tower ban session has created between {Context.User.Mention} and {user.Mention}. " +
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

        [Command("Leaderboard")]
        [Alias("rankings", "rank", "ranking", "lb")]
        public async Task Ranking(IUser user = null)
        {
            user ??= Context.User as IUser;
            var playerRanking = rankingService.SeriesPlayers.Select((x, i) => new KeyValuePair<int, SeriesPlayer>(i + 1, x)).ToDictionary(x => x.Key, x => x.Value);
            //var playerRanking = rankingService.FullRanking.ToList();

            var embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithColor(Color.Orange)
                .WithTitle("Leaderboard")
                .WithDescription(rankingService.PrintRankingList(playerRanking.Take(10)));

            if (playerRanking.Values.Any(x => x.Id == user.Id.ToString()))
            {
                IConvertible getWins(IEnumerable<Series> seriesSet, string playerId) => seriesSet.Sum(x => x.WinnerId == playerId ? x.Wins : x.Losses);

                var player = playerRanking.First(x => x.Value.Id == user.Id.ToString());
                var playerId = player.Value.Id;
                var seriesHistory = seriesService.SeriesHistory.Where(x => x.WinnerId == playerId || x.LoserId == playerId);
                var gamesPlayed = seriesHistory.Sum(x => x.Wins + x.Losses);
                var seriesHistoryLast3 = seriesHistory.Reverse().Take(3).Reverse();


                var playerRankoutput = $"{Format.Bold("Position")}\n{rankingService.PrintRankingList(playerRanking.Skip(player.Key - 2).Take(3))}";
                var gamesPlayedOutput = $"{gamesPlayed} Games played";
                var winRateOverall = getWins(seriesHistory, playerId).ToDouble(default) / gamesPlayed;
                var winRateOverallOutput = $"`{winRateOverall.ToString("P2")}` Overall win rate";
                var winRateLast3 = getWins(seriesHistoryLast3, playerId).ToDouble(default) / seriesHistoryLast3.Sum(x => x.Wins + x.Losses);
                var winRateLast3Output = $"`{winRateLast3.ToString("P2")}` Last 3 series win rate";
                var seriesHistoryOutput = $"{Format.Bold("Recent Series")}\n{seriesService.PrintSeriesHistoryLines(seriesHistoryLast3).JoinLines()}";
                var playerStatsOutput = string.Join('\n', gamesPlayedOutput, winRateOverallOutput, winRateLast3Output, playerRankoutput, seriesHistoryOutput);
                embed.AddField("Player Statistics", playerStatsOutput);
            }
            else
            {
                embed.AddField("Player Statistics", "The player has no series history.");
            }
            await ReplyAsync(message: "Full list at " + rankingService.RankingChannel.Mention,embed: embed.Build());
        }

        [Command("SeriesHistory")]
        [Alias("sh")]
        public async Task Serieshistory(IUser user = null)
        {
            var limit = 25;
            var targetSet = (user == null ? seriesService.SeriesHistory : seriesService.SeriesHistory
                .Where(x => x.WinnerId == user.Id.ToString() || x.LoserId == user.Id.ToString()))
                .Reverse()
                .Take(limit)
                .Reverse();
            var lines = seriesService
                .PrintSeriesHistoryLines(targetSet);
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Series History")
                .WithDescription(lines.JoinLines())
                .WithFooter($"Displaying up to last {lines.Count()} series.");
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
                await ReplyAsync($"Use `{prefix}{nameof(Challenge)}` to challenge someone on a series and to start a tower banning session." +
                    "\nYou can only select a tower to ban in our DM.");
            }
            return;
        }

        [Command("BanTower")]
        [Alias("b", "bt", "ban")]
        [Summary("Please enter the number or the exact name of the tower given.")]
        public async Task BanTower(Tower tower)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync($"Use `{prefix}{nameof(Challenge)}` to challenge someone on a series and to start a tower banning session." + 
                    "\nYou can only select a tower to ban in our DM.");
                return;
            }

            var session = towerBanManager.ProcessChoice(Context.User, tower);

            if (session == null)
            {
                await ReplyAsync("The tower banning session has ended or you are not in a session to select a tower to ban.");
                return;
            }
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"Click [here](https://discordapp.com/channels/{session.InitiateChannel.GuildId}/{session.InitiateChannel.Id}) to return to the channel.");
            await ReplyAsync("Banned Tower Choice: " + tower.ToString("g"), embed: embed.Build());
        }

        private bool IsValidReplayId(string id)
        {
            return Regex.IsMatch(id, "^[A-Z]{7}$");
        }

        [Command("Replay")]
        [Alias("r")]
        public async Task ReplayLink(string inviteCode)
        {
            inviteCode = inviteCode.ToUpper();
            if (!IsValidReplayId(inviteCode))
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

        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(IUser otherUser, int wins = 0, int losses = 0, int draws = 0, double kFactor = RankingService.Elo_K_Factor)
        {
            await Elo(Context.User, otherUser, wins, losses, draws, kFactor);
        }

        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(IUser userA, IUser userB, int wins = 0, int losses = 0, int draws = 0, double kFactor = RankingService.Elo_K_Factor)
        {
            var playerA = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == userA.Id.ToString())?.Elo ?? 1200;
            var playerB = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == userB.Id.ToString())?.Elo ?? 1200;
            await Elo(playerA, playerB, wins, losses, draws, kFactor);
        }


        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(double a = 1200, double b = 1200, int wins = 0, int losses = 0, int draws = 0, double kFactor = RankingService.Elo_K_Factor)
        {
            var (eloLeft, eloRight) = EloCalculator.Calculate(kFactor, a, b, wins, losses, draws);
            var diffLeft = eloLeft - a;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Elo calculator")
                .WithDescription($"`A = {a.ToString("N2")}` `B = {b.ToString("N2")}` `wins = {wins}` `losses = {losses}` `draws = {draws}` `K factor = {kFactor}`")
                .AddField("Elo A", $"`{a.ToString("N2")} -> {eloLeft.ToString("N2")}`", true)
                .AddField("Elo B", $"`{b.ToString("N2")} -> {eloRight.ToString("N2")}`", true)
                .AddField("Difference", $"`{(diffLeft <= 0 ? "" : "+")}{diffLeft.ToString("N2")}`");

            await ReplyAsync(embed: embed.Build());
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

        [Group]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        class CreateSeries: InteractiveBase
        {
            private readonly SeriesService seriesService;
            private readonly RankingService rankingService;

            public CreateSeries(IServiceProvider services, IOptions<AppSettings> appSettings)
            {
                seriesService = services.GetService<SeriesService>();
                rankingService = services.GetService<RankingService>();
            }

            private bool IsValidReplayId(string id)
            {
                return Regex.IsMatch(id, "^[A-Z]{7}$");
            }

            [Command("CreateSeries")]
            [Alias("cs")]
            [Summary("Administrator command to create a series, bypassing most restrictions.")]
            public async Task CreateSeriesCommand(IGuildUser winner, IGuildUser loser, byte wins, byte loss, string inviteCode = "NA")
            {
                await CreateSeriesCommand(winner, loser, wins, loss, 0, inviteCode);
            }

            [Command("CreateSeries")]
            [Alias("cs")]
            [Summary("Administrator command to create a series, bypassing most restrictions.")]
            public async Task CreateSeriesCommand(IGuildUser winner, IGuildUser loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
            {
                await CreateSeriesCommand(winner.Id, loser.Id, wins, loss, draws, inviteCode);
            }

            [Command("CreateSeries")]
            [Alias("cs")]
            [Summary("Administrator command to create a series, bypassing most restrictions.")]
            public async Task CreateSeriesCommand(ulong winner, ulong loser, byte wins, byte loss, string inviteCode = "NA")
            {
                await CreateSeriesCommand(winner, loser, wins, loss, 0, inviteCode);
            }

            [Command("CreateSeriesForce")]
            [Alias("csf")]
            [Summary("Administrator command to create a series, bypassing most restrictions.")]
            public async Task CreateSeriesCommand(ulong winner, ulong loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
            {
                if (wins < loss)
                {
                    await ReplyAsync("Wins must greater than losses");
                    return;
                }

                if (inviteCode != "NA")
                {
                    if (!IsValidReplayId(inviteCode.ToUpper()))
                    {
                        await ReplyAsync("Invalid invite code.");
                        return;
                    }
                }

                var series = seriesService.MakeSeries(winner, loser, wins, loss, draws, inviteCode.ToUpper());
                await seriesService.AdminCreateAsync(series);
                var embed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithDescription($"Id: {series.Id.ToString("D4")}\n" +
                    $"Winner: {rankingService.getPlayerMention(series.WinnerId)}\n" +
                    $"Loser: {rankingService.getPlayerMention(series.LoserId)}\n" +
                    $"Score: {wins}-{loss} Draws: {draws}\n" +
                    $"Invite Code: {inviteCode}\n" +
                    $"Approved By: {Context.User.Mention}");

                var embedAuthor = Context.Guild.GetUser(winner);
                if (embedAuthor != default)
                {
                    embed = embed.WithAuthor(embedAuthor);
                }
                await ReplyAsync(embed: embed.Build());
            }
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
    }
}
