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
    public class RankingService : IRankingService
    {
        const double Elo_K_Factor = 40;
        const int Rank_Show_Games = 5;

        private readonly string endPoint;
        private readonly string adminKey;

        private readonly DiscordSocketClient client;
        private readonly SeriesService seriesService;
        private List<SeriesPlayer> seriesPlayers;

        private ulong productionGuildId;
        private ulong rankingChannel;
        private readonly RestClient restClient;

        private IGuild ProductionGuild => client.GetGuild(productionGuildId);
        private ITextChannel RankingChannel => client.GetChannel(rankingChannel) as ITextChannel;

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

        public IEnumerable<KeyValuePair<int, SeriesPlayer>> FullRanking
        {
            get
            {
                var playCountGroup = seriesPlayers.GroupBy(x =>
                {
                    var c = 0;
                    foreach (var y in seriesService.SeriesHistory)
                    {
                        if (y.WinnerId == x.Id || y.LoserId == x.Id)
                        {
                            if ((c += y.Wins + y.Losses) >= Rank_Show_Games)
                            {
                                return c;
                            }
                        }
                    }
                    return c;
                });
                int rank = 1;
                foreach (var rankGroup in playCountGroup.OrderByDescending(x => x.Key))
                {
                    if (rankGroup.Key >= 5)
                    {
                        foreach (var player in rankGroup)
                        {
                            yield return new KeyValuePair<int, SeriesPlayer>(rank++, player);
                        }
                    }
                    else
                    {
                        foreach (var player in rankGroup)
                        {
                            yield return new KeyValuePair<int, SeriesPlayer>(-(5 - rankGroup.Key), player);
                        }
                    }
                }
            }
        }


        public SeriesPlayer CreatePlayer(ulong id, string name)
        {
            return new SeriesPlayer
            {
                Elo = 1200,
                Id = id.ToString(),
                Name = name
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

        public async Task UpdateFullLeaderBoard()
        {
            var messages = (await RankingChannel.GetMessagesAsync(100).FlattenAsync()).Where(x => x.Author.Id == client.CurrentUser.Id);

            var playerRanking = seriesPlayers.Select((x, i) => new KeyValuePair<int, SeriesPlayer>(i + 1, x)).ToDictionary(x => x.Key, x => x.Value);
            // var playerRanking = FullRanking.ToList();
            var chunksize = 50;
            for (int i = 0, m = 0; i < playerRanking.Count; i += chunksize, m++)
            {
                var fixMsg = "Due an apparent discord caching bug, " +
                    "if the list contains invalid players and they are still present in the server, " +
                    "to fix this: move to another channel and scroll through the entire discord user list at the right and return to this channel.";
                var embed = new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle("Leaderboard")
                    .WithDescription(PrintRankingList(playerRanking.Skip(i).Take(chunksize)))
                    .WithFooter("Updated At")
                    .WithTimestamp(DateTime.Now)
                    .Build();

                var targetMessage = messages.ElementAtOrDefault(m) as IUserMessage;

                if (targetMessage != default)
                {
                    await targetMessage.ModifyAsync(x => {
                        x.Content = i == 0 ? fixMsg : string.Empty;
                        x.Embed = embed;
                    });;
                }
                else
                {
                    await RankingChannel.SendMessageAsync(text: i == 0 ? fixMsg : string.Empty, embed: embed);
                }
            }
        }

        public string getPlayerMention(string id)
        {
            return client.GetUser(ulong.Parse(id))?.Mention ?? $"<@{id}>";
        }

        public string PrintRankingList(IEnumerable<KeyValuePair<int, SeriesPlayer>> list)
        {
            return list.Select(x => x.Key > 0 
            ? $"`#{x.Key.ToString("D2")}` `ELO: {x.Value.Elo.ToString("N2")}` {getPlayerMention(x.Value.Id)}"
            : $"`Unranked {-x.Key}` `ELO: {x.Value.Elo.ToString("N2")}` {getPlayerMention(x.Value.Id)}").JoinLines();
        }

        private void ProcessSeries(Series series)
        {
            var winner = seriesPlayers.Find(x => x.Id.ToString() == series.WinnerId);
            var loser = seriesPlayers.Find(x => x.Id.ToString() == series.LoserId);
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
    }
}
