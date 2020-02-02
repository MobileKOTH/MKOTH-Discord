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
    public class RankingService
    {
        const double Elo_K_Factor = 40;
        private readonly string endPoint;
        private readonly string adminKey;

        private readonly DiscordSocketClient client;
        private readonly SeriesService seriesService;
        private List<SeriesPlayer> seriesPlayers;

        private ulong productionGuildId;
        private readonly RestClient restClient;

        private IGuild ProductionGuild => client.GetGuild(productionGuildId);

        public IEnumerable<SeriesPlayer> SeriesPlayers => seriesPlayers;

        private const string collectionName = "_players";

        private bool clientReady = false;
        public RankingService(IServiceProvider services, IOptions<AppSettings> appSettings, IOptions<Credentials> credentials)
        {
            endPoint = appSettings.Value.ConnectionStrings.AppsScript;
            adminKey = credentials.Value.AppsScriptAdminKey;

            client = services.GetService<DiscordSocketClient>();
            seriesService = services.GetService<SeriesService>();
            productionGuildId = appSettings.Value.Settings.ProductionGuild.Id;

            restClient = new RestClient(endPoint);

            seriesService.Updated += Refresh;
            client.Ready += () => Task.Run(() =>
            {
                clientReady = true;
                _ = Refresh();
            }) ;
        }

        public async Task Refresh()
        {
            if (!(seriesService.Ready && clientReady))
            {
                return;
            }
            var guildUsers = await ProductionGuild.GetUsersAsync();
            seriesPlayers = seriesService.AllPlayers.Select(x => new SeriesPlayer 
            { 
                Elo = 1200, 
                Id = x.ToString(), 
                Name = guildUsers.FirstOrDefault(y => y.Id == x)?.GetDisplayName() ?? x.ToString()
            }).ToList();

            foreach (var series in seriesService.SeriesHistory)
            {
                var winner = seriesPlayers.Find(x => x.Id.ToString() == series.WinnerId);
                var loser = seriesPlayers.Find(x => x.Id.ToString() == series.LoserId);
                var (eloLeft, eloRight) = EloCalculator.Calculate(Elo_K_Factor, winner.Elo, loser.Elo, series.Wins, series.Losses, series.Draws);
                winner.Elo = eloLeft;
                loser.Elo = eloRight;
            }

            seriesPlayers = seriesPlayers.OrderByDescending(x => x.Elo).ToList();

            Logger.Debug("Refreshed", "Ranking");

            var request = new RestRequest()
                   .AddQueryParameter("admin", adminKey)
                   .AddQueryParameter("spreadSheet", collectionName)
                   .AddQueryParameter("operation", "all")
                   .AddJsonBody(seriesPlayers.Select(x => new Player { Id = x.Id, Name = x.Name}).OrderBy(x => x.Name).ToArray());
            var response = await restClient.PostAsync<dynamic>(request);

            Logger.Debug(response, "Player List Update");
        }
    }
}
