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
            var chunksize = 50;
            for (int i = 0, m = 0; i < playerRanking.Count; i += chunksize, m++)
            {
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
                    await targetMessage.ModifyAsync(x => x.Embed = embed);
                }
                else
                {
                    await RankingChannel.SendMessageAsync(embed: embed);
                }
            }
        }

        public string PrintRankingList(IEnumerable<KeyValuePair<int, SeriesPlayer>> list)
        {
            return list.Select(x => $"`#{(x.Key).ToString("D2")}` `ELO: {x.Value.Elo.ToString("N2")}` <@!{x.Value.Id}>").JoinLines();
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
