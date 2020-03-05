using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using MKOTHDiscordBot.Models;
using MKOTHDiscordBot.Properties;
using MKOTHDiscordBot.Utilities;

using RestSharp;

namespace MKOTHDiscordBot.Services
{
    public enum Tiers
    {
        King,
        Nobles = 1400,
        Squires = 1300,
        Vassals = 1250,
        Peasants
    }
    public class RankingService : IRankingService
    {
        public const double Elo_K_Factor = 75;
        public const int Rank_Show_Games = 5;

        private readonly string endPoint;
        private readonly string adminKey;

        private readonly DiscordSocketClient client;
        private readonly SeriesService seriesService;
        private List<SeriesPlayer> seriesPlayers;

        private ulong productionGuildId;
        private ulong rankingChannel;
        private readonly RestClient restClient;

        private IGuild ProductionGuild => client.GetGuild(productionGuildId);
        public ITextChannel RankingChannel => client.GetChannel(rankingChannel) as ITextChannel;

        public IEnumerable<SeriesPlayer> SeriesPlayers => seriesPlayers;

        private const string collectionName = "_players";

        private bool clientReady = false;

        public event Func<Task> Updated;

        public RankingService(IServiceProvider services, IOptions<AppSettings> appSettings, IOptions<Credentials> credentials)
        {
            endPoint = appSettings.Value.ConnectionStrings.AppsScript;
            adminKey = credentials.Value.AppsScriptAdminKey;

            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<SeriesService>();
            productionGuildId = appSettings.Value.Settings.ProductionGuild.Id;
            rankingChannel = appSettings.Value.Settings.ProductionGuild.Ranking;

            restClient = new RestClient(endPoint);

            seriesService.Updated += Refresh;
            client.Ready += () => Task.Run(() =>
            {
                clientReady = true;
                _ = Refresh();
            });
        }

        public Tiers PlayerTier(double elo) => elo switch
        {
            double x when x >= (int)Tiers.Nobles => (SeriesPlayers.First().Elo == x && SeriesPlayers.Count(y => y.Elo >= 1400) > 2) ? Tiers.King : Tiers.Nobles,
            double x when x >= (int)Tiers.Squires => Tiers.Squires,
            double x when x >= (int)Tiers.Vassals => Tiers.Vassals,
            _ => Tiers.Peasants
        };

        public string TierIcon(Tiers tier) => tier switch
        {
            Tiers.King => "👑",
            Tiers.Nobles => "💰",
            Tiers.Squires => "⚔",
            Tiers.Vassals => "⚒",
            _ => "🛠",
        };

        public SeriesPlayer CreatePlayer(ulong id, string name)
        {
            return new SeriesPlayer
            {
                Elo = 1200,
                Id = id.ToString(),
                Name = name,
                Points = 0
            };
        }

        public async Task AddSeriesAsync(Series series)
        {
            foreach (var item in new ulong[] { ulong.Parse(series.WinnerId), ulong.Parse(series.LoserId)})
            {
                if (!SeriesPlayers.Any(x => x.Id == item.ToString()))
                {
                    seriesPlayers.Add(CreatePlayer(item, TryGetPlayerName(await ProductionGuild.GetUsersAsync(), item)));
                }
            }
            ProcessSeries(series);
            seriesPlayers = seriesPlayers.OrderByDescending(x => x.Elo).ToList();
            await UpdateFullLeaderBoard();
        }

        public async Task PostAsync()
        {
            try
            {
                var request = new RestRequest()
                   .AddQueryParameter("admin", adminKey)
                   .AddQueryParameter("spreadSheet", collectionName)
                   .AddQueryParameter("operation", "all")
                   .AddJsonBody(seriesPlayers.Select(x => new Player { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name).ToArray());
                var response = await restClient.PostAsync<dynamic>(request);
                Logger.Debug(response, "Remote Player List Updated");
            }
            catch (Exception e)
            {
                Logger.Debug(e.Message, "Player list post error");
            }
        }

        public async Task Refresh()
        {
            if (!(seriesService.Ready && clientReady))
            {
                return;
            }
            var guildUsers = await ProductionGuild.GetUsersAsync();
            seriesPlayers = seriesService.AllPlayers
                .Select(x => CreatePlayer(x, TryGetPlayerName(guildUsers, x)))
                .ToList();

            foreach (var series in seriesService.SeriesHistory)
            {
                ProcessSeries(series);
            }

            seriesPlayers = seriesPlayers.OrderByDescending(x => x.Elo).ToList();

            Logger.Debug("Refreshed", "Ranking");

            await Task.WhenAll(PostAsync(), UpdateFullLeaderBoard());

            _ = Updated.Invoke();
        }

        private async Task<IUserMessage> GetOrCreateRankingMessage(Queue<IMessage> messageQueue)
        {
            if (messageQueue.Count > 0)
            {
                return await Task.FromResult(messageQueue.Dequeue() as IUserMessage);
            }
            else
            {
                return await RankingChannel.SendMessageAsync("Reserved");
            }
        }

        public async Task UpdateFullLeaderBoard()
        {
            var messages = new Queue<IMessage>((await RankingChannel.GetMessagesAsync(100).FlattenAsync())
                .Where(x => x.Author.Id == client.CurrentUser.Id).Reverse());

            var headerTime = await GetOrCreateRankingMessage(messages);

            var headerEmbed = new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription("Formatting maybe incorrect in mobile.")
                .WithFooter("Updated At")
                .WithTimestamp(DateTime.Now);

            await headerTime.ModifyAsync(x =>
            {
                x.Content = Format.Bold("Leaderboard");
                x.Embed = headerEmbed.Build();
            });

            var titleHeaders = await GetOrCreateRankingMessage(messages);

            await titleHeaders.ModifyAsync(x => x.Content = $"`#00` | `{"Elo".PadRight(8, ' ')}` | `Pts` | `Name`");

            var playerRanking = seriesPlayers.Select((x, i) => new KeyValuePair<int, SeriesPlayer>(i + 1, x)).ToDictionary(x => x.Key, x => x.Value);

            var tiers = playerRanking.GroupBy(x => PlayerTier(x.Value.Elo));

            static string getTierHeader(Tiers tier) => $"{Format.Underline(Format.Bold(tier.ToString("G").PadRight(66, ' ')))}";

            var lines = tiers.Select(x => $"{getTierHeader(x.Key)}\n" + PrintRankingList(x).JoinLines() + "\n")
                .JoinLines()
                .Split("\n");

            var chunksize = 30;
            for (int i = 0, m = 0; i < lines.Length; i += chunksize, m++)
            {
                var targetMessage = await GetOrCreateRankingMessage(messages);

                await targetMessage.ModifyAsync(x =>
                {
                    x.Content = lines.Skip(i).Take(chunksize).JoinLines();
                });
            }
        }

        public string getPlayerMention(string id)
        {
            return client.GetUser(ulong.Parse(id))?.Mention ?? $"<@{id}>";
        }

        public IEnumerable<string> PrintRankingList(IEnumerable<KeyValuePair<int, SeriesPlayer>> list)
        {
            foreach (var player in list)
            {
                yield return $"`#{player.Key.ToString("D2")}` | " +
                    $"`{player.Value.Elo.ToString("N2").PadLeft(8, '0')}` | " +
                    $"`{player.Value.Points.ToString().PadLeft(3, '0')}` | " +
                    $"{TierIcon(PlayerTier(player.Value.Elo))} {getPlayerMention(player.Value.Id)}";
            }
        }

        private void ProcessSeries(Series series)
        {
            var winner = seriesPlayers.Find(x => x.Id.ToString() == series.WinnerId);
            var loser = seriesPlayers.Find(x => x.Id.ToString() == series.LoserId);

            if (winner.Elo < loser.Elo)
            {
                winner.Points += 5;
            }
            else
            {
                winner.Points += 1;
            }

            if (winner.Elo > 1300 && (winner.Elo - loser.Elo) >= 300)
            {
                loser.Points -= 5;
            }

            if (loser.Points < 0)
            {
                Logger.Log($"{loser.Name} Negative Elo at Series {series.Id}", LogType.Error);
            }

            var (eloLeft, eloRight) = EloCalculator.Calculate(Elo_K_Factor, winner.Elo, loser.Elo, series.Wins, series.Losses, series.Draws);
            winner.Elo = eloLeft;
            loser.Elo = eloRight;
        }

        // Users might leave the guild hence longer able to get their name, will fallback to their id.
        private string TryGetPlayerName(IReadOnlyCollection<IGuildUser> users, ulong id)
        {
            return users.FirstOrDefault(y => y.Id == id)?.GetDisplayName() ?? id.ToString();
        }
    }

    public interface IRankingService
    {
        IEnumerable<SeriesPlayer> SeriesPlayers { get; }
        event Func<Task> Updated;
        Task Refresh();
        Tiers PlayerTier(double elo);
    }
}
