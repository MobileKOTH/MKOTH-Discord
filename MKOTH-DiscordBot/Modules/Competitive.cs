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

using MKOTHDiscordBot.Core;
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
        private readonly ITextChannel logChannel;
        private readonly ISeriesManager seriesService;
        private readonly IRankingManager rankingService;
        private readonly TowerBanManager towerBanManager;
        private readonly RoleManager roleManager;

        private readonly Lazy<IEmote> lazyBanEmote;
        private IEmote BanEmote => lazyBanEmote.Value;

        private readonly string prefix;

        public Competitive(IServiceProvider services, IOptions<AppSettings> appSettings)
        {
            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<ISeriesManager>();
            rankingService = services.GetService<IRankingManager>();
            towerBanManager = services.GetService<TowerBanManager>();
            roleManager = services.GetService<RoleManager>();
            logChannel = client.GetChannel(appSettings.Value.Settings.DevelopmentGuild.Test) as ITextChannel;

            prefix = services.GetScoppedSettings<AppSettings>().Settings.DefaultCommandPrefix;
            lazyBanEmote = new Lazy<IEmote>(() => rankingService.RankingChannel.Guild.GetEmoteAsync(683258477421920342).Result);
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
        [Summary(
            "Challenge a user on a series, decide if to have towers banned. " +
            "Both have to agree to have towers banned.\n" +
            "Please also refer to the [series rules and ranking system](http://mobilekoth.github.io/system)."
        )]
        [Cooldown(60000, 2)]
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
                    await ReplyAsync(
                        "If a player's elo is more than 1300 and the Elo difference of the players is more than 300, " +
                        "the lower Elo player needs to have at least 5 points earned to challenge."
                    );
                    return;
                }
            }

            var challengeEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithAuthor(Context.User)
                .WithTitle("Series Challenge")
                .WithDescription(
                    Context.User.Mention + " is challenging " + user.Mention + " on a series.\n" +
                    "Engineer is banned by default unless [rule 7](https://mobilekoth.github.io/system) is applied.\n" +
                    "Please decide if to ban more towers. Both must agree or there will be no other towers banned.\n" +
                    "<:ban:683258477421920342> Ban Towers ⭕ No Ban Towers 🚶 Reject Challenge"
                )
                .WithFooter($"Both of you have {TowerBanManager.MAX_SESSION_SECONDS} seconds to decide.");

            bool leftAgree = false, rightAgree = false, denied = false;
            int voteCount = 0;

            var reactionCallBackData = new ReactionCallbackData(
                string.Empty,
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
                        .WithColor(Color.Orange)
                        .WithDescription($"{Context.User.Mention} {user.Mention} Challenge session has timed out.")
                        .Build());
                }).WithCallback(BanEmote, async (c, e) =>
                {
                    if (!(e.UserId == Context.User.Id || e.UserId == user.Id) || denied)
                    {
                        return;
                    }
                    leftAgree = leftAgree || e.UserId == Context.User.Id;
                    rightAgree = rightAgree || e.UserId == user.Id;
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
                            .WithColor(Color.Orange)
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
                if (leftAgree && rightAgree)
                {
                    await BanTowerSession(user);
                }
                else
                {
                    await ReplyAsync(embed: new EmbedBuilder()
                        .WithColor(Color.Orange)
                        .WithDescription(
                            $"{Context.User.Mention} {user.Mention} " +
                            $"There is at least 1 disagreement to have towers banned. " +
                            $"You may begin your series with no tower banned."
                        )
                        .Build());
                }
            }

            await InlineReactionReplyAsync(reactionCallBackData, false);
        }

        private async Task BanTowerSession(IGuildUser user)
        {
            if (!towerBanManager.StartSession(Context.User, user, Context.Channel as ITextChannel, out TowerBanSession session))
            {
                await ReplyAsync("Failed to start a tower banning session. Perhaps someone is already in a session, please wait for them to finish.");
                return;
            }

            var dmEmbed = ListTowers()
                .AddField(
                    "Examples",
                    $"`{prefix}{nameof(BanTower)} 1`\n" +
                    $"`{prefix}{nameof(BanTower)} Dart`\n" +
                    $"`{prefix}b 1`\n" +
                    $"`{prefix}b dart`"
                )
                .WithFooter($"You have {TowerBanManager.MAX_SESSION_SECONDS} seconds to make your choice.");

            try
            {
                var dmStarterTask = Context.User.SendMessageAsync(embed: dmEmbed.Build());
                var dmOtherTask = user.SendMessageAsync(embed: dmEmbed.Build());
                await Task.WhenAll(dmStarterTask, dmOtherTask);
                var dmStarter = await dmStarterTask;
                var dmOther = await dmOtherTask;
                var embed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithDescription(
                        $"A tower ban session has created between {Context.User.Mention} and {user.Mention}. " +
                        $"Please select a tower to ban in our DM within {TowerBanManager.MAX_SESSION_SECONDS} seconds.\n\n" +
                        $"{Context.User.Mention}: You can click [here](https://discordapp.com/channels/@me/{dmStarter.Channel.Id}) to switch to our dm.\n" +
                        $"{user.Mention}: You can click [here](https://discordapp.com/channels/@me/{dmOther.Channel.Id}) to switch to our dm."
                    );
                await ReplyAsync(embed: embed.Build());
            }
            catch (Exception e)
            {
                towerBanManager.CancelSession(session);
                await ReplyAsync($"{e.Message}.\nThis command cannot work for one who has direct message disabled.");
            }
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


        [Command("Submit")]
        [RequireContext(ContextType.Guild)]
        [Cooldown(3600000, 3, "Submission")]
        public async Task Submit(IGuildUser user, int wins, int loss, string inviteCode)
        {
            await ReplyAsync("Self series submission is not available yet.\n" +
                $"Please send a submission manually at " +
                $"{(Context.Guild.Channels.First(x => x.Name == "series-submit") as ITextChannel).Mention} for an admin to process it.");
        }

        [Command("Submit")]
        [RequireContext(ContextType.Guild)]
        [Cooldown(3600000, 3, "Submission")]
        public async Task Submit(IGuildUser user, int wins, int loss, int draws, string inviteCode)
        {
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

            var embed = new EmbedBuilder()
                .WithAuthor(user)
                .WithColor(Color.Orange)
                .WithTitle("Leaderboard")
                .WithDescription(rankingService.PrintRankingList(playerRanking.Take(10)).JoinLines());

            if (playerRanking.Values.Any(x => x.Id == user.Id.ToString()))
            {
                IConvertible getWins(IEnumerable<Series> seriesSet, string playerId) => seriesSet.Sum(x => x.WinnerId == playerId ? x.Wins : x.Losses);

                var player = playerRanking.First(x => x.Value.Id == user.Id.ToString());
                var playerId = player.Value.Id;
                var seriesHistory = seriesService.SeriesHistory.Where(x => x.WinnerId == playerId || x.LoserId == playerId);
                var gamesPlayed = seriesHistory.Sum(x => x.Wins + x.Losses);
                var seriesHistoryLast3 = seriesHistory.Reverse().Take(3).Reverse();
                var totalWins = getWins(seriesHistory, playerId);

                var playerRankoutput = $"{Format.Bold("Position")}\n{rankingService.PrintRankingList(playerRanking.Skip(player.Key - 2).Take(3)).JoinLines()}";
                var gamesPlayedOutput = $"`{gamesPlayed}` Games played";
                var winLossOutput = $"`{totalWins}-{gamesPlayed - totalWins.ToInt32(default)}` Win loss";
                var winRateOverall = getWins(seriesHistory, playerId).ToDouble(default) / gamesPlayed;
                var winRateOverallOutput = $"`{winRateOverall:P2}` Overall win rate";
                var winRateLast3 = getWins(seriesHistoryLast3, playerId).ToDouble(default) / seriesHistoryLast3.Sum(x => x.Wins + x.Losses);
                var winRateLast3Output = $"`{winRateLast3:P2}` Last 3 series win rate";
                var seriesHistoryOutput = $"{Format.Bold("Recent Series")}\n{seriesService.PrintSeriesHistoryLines(seriesHistoryLast3).JoinLines()}";
                var playerStatsOutput = string.Join('\n', gamesPlayedOutput, winLossOutput, winRateOverallOutput, winRateLast3Output, playerRankoutput, seriesHistoryOutput);
                embed.AddField("Player Statistics", playerStatsOutput);
            }
            else
            {
                embed.AddField("Player Statistics", "The player has no series history.");
            }
            await ReplyAsync(message: "Full list at " + rankingService.RankingChannel.Mention, embed: embed.Build());
        }

        [Command("SeriesHistory")]
        [Alias("sh")]
        public async Task Serieshistory(IUser user = null)
        {
            var limit = 25;
            var targetSet = user == null
                ? seriesService.SeriesHistory
                : seriesService.SeriesHistory
                .Where(x => x.WinnerId == user.Id.ToString() || x.LoserId == user.Id.ToString());
            var lines = seriesService.PrintSeriesHistoryLines(targetSet.Reverse().Take(limit).Reverse());
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Series History")
                .WithDescription($"`{targetSet.Sum(x => x.Wins + x.Losses)}` Games played\n\n" + lines.JoinLines())
                .WithFooter($"Displaying up to last {lines.Count()} series.");
            await ReplyAsync(embed: embed.Build());
        }

        [Command("Replay")]
        [Alias("r")]
        public async Task ReplayLink(string inviteCode)
        {
            inviteCode = inviteCode.ToUpper();
            if (!BattlesTV.IsValidReplayIdFormat(inviteCode))
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
        public async Task Elo(IUser otherUser, int wins = 0, int losses = 0, int draws = 0, double? kFactor = null)
        {
            await Elo(Context.User, otherUser, wins, losses, draws, kFactor);
        }

        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(IUser userA, IUser userB, int wins = 0, int losses = 0, int draws = 0, double? kFactor = null)
        {
            var playerA = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == userA.Id.ToString())?.Elo ?? 1200;
            var playerB = rankingService.SeriesPlayers.FirstOrDefault(x => x.Id == userB.Id.ToString())?.Elo ?? 1200;
            await Elo(playerA, playerB, wins, losses, draws, kFactor);
        }


        [Command("Elo")]
        [Summary("Basic Elo calculator with default K factor of 40.")]
        public async Task Elo(double a = 1200, double b = 1200, int wins = 0, int losses = 0, int draws = 0, double? kFactor = null)
        {
            kFactor ??= rankingService.ELO_KFactor;
            var (eloLeft, eloRight) = EloCalculator.Calculate(kFactor.Value, a, b, wins, losses, draws);
            var diffLeft = eloLeft - a;
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle("Elo calculator")
                .WithDescription($"`A = {a:N2}` `B = {b:N2}` `wins = {wins}` `losses = {losses}` `draws = {draws}` `K factor = {kFactor.Value}`")
                .AddField("Elo A", $"`{a:N2} -> {eloLeft:N2}`", true)
                .AddField("Elo B", $"`{b:N2} -> {eloRight:N2}`", true)
                .AddField("Difference", $"`{(diffLeft <= 0 ? "" : "+")}{diffLeft:N2}`");

            await ReplyAsync(embed: embed.Build());
        }

        [Command("Refresh")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Refresh()
        {
            var refresh = seriesService as IRefreshable;
            await ReplyAsync("Pulling from remote spreadsheet...");
            await refresh.RefreshAsync();
            await ReplyAsync("Refresh complete.");
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [Summary("Administrator command to create a series, bypassing most restrictions.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Cooldown(3600000, 10, "CreateSeries")]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, string inviteCode = "NA")
        {
            await CreateSeries(winner.Id, loser.Id, wins, loss, 0, inviteCode);
        }

        [Command("CreateSeries")]
        [Alias("cs")]
        [Summary("Administrator command to create a series, bypassing most restrictions.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Cooldown(3600000, 10, "CreateSeries")]
        public async Task CreateSeries(IGuildUser winner, IGuildUser loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
        {
            await CreateSeries(winner.Id, loser.Id, wins, loss, draws, inviteCode);
        }

        [Command("CreateSeriesForced")]
        [Alias("csf")]
        [Summary("Administrator command to create a series, even for non existant players.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Cooldown(3600000, 10, "CreateSeries")]
        public async Task CreateSeries(ulong winner, ulong loser, byte wins, byte loss, string inviteCode = "NA")
        {
            await CreateSeries(winner, loser, wins, loss, 0, inviteCode);
        }

        [Command("CreateSeriesForced")]
        [Alias("csf")]
        [Summary("Administrator command to create a series, even for non existant players.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Cooldown(3600000, 10, "CreateSeries")]
        public async Task CreateSeries(ulong winner, ulong loser, byte wins, byte loss, byte draws, string inviteCode = "NA")
        {
            if (wins < loss)
            {
                await ReplyAsync("Wins must greater than losses");
                return;
            }

            if (inviteCode != "NA")
            {
                if (!BattlesTV.IsValidReplayIdFormat(inviteCode.ToUpper()))
                {
                    await ReplyAsync("Invalid invite code.");
                    return;
                }
            }

            var series = seriesService.MakeSeries(winner, loser, wins, loss, draws, inviteCode.ToUpper());
            await seriesService.AdminCreateAsync(series);
            var embed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"Id: {series.Id:D4}\n" +
                $"Winner: {rankingService.GetPlayerMention(series.WinnerId)}\n" +
                $"Loser: {rankingService.GetPlayerMention(series.LoserId)}\n" +
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

        [Command("DeleteSeries")]
        [Alias("ds")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Cooldown(3600000, 5)]
        public async Task DeleteSeries(int id)
        {
            try
            {
                await seriesService.RemoveAsync(id);
                await ReplyAsync($"Series `#{id}` is deleted.");
            }
            catch (Exception e)
            {
                await ReplyAsync(e.Message);
            }
        }
    }
}
