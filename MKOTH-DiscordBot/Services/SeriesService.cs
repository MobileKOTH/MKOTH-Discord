using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using System.Linq;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Models;
using MKOTHDiscordBot.Properties;
using RestSharp;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

namespace MKOTHDiscordBot.Services
{
    public class SeriesService
    {
        private readonly string endPoint;
        private readonly string adminKey;
        private readonly RankingService rankingService;

        private readonly RestClient restClient;

        private List<Series> seriesList;
        private List<Series> pendingList;

        private int? nextId = null;

        public int NextId => nextId.HasValue ? (int)(nextId = nextId.Value + 1) : (int)(nextId = seriesList.Max(x => x.Id) + 1);

        public SeriesService(IServiceProvider services, IOptions<AppSettings> appSettings, IOptions<Credentials> credentials)
        {
            rankingService = services.GetService<RankingService>();
            endPoint = appSettings.Value.ConnectionStrings.AppsScript;
            adminKey = credentials.Value.AppsScriptAdminKey;

            restClient = new RestClient(endPoint);

            pendingList = new List<Series>();
            _ = Refresh();
        }

        public async Task Refresh()
        {
            var request = new RestRequest()
                //.AddQueryParameter("admin", adminKey)
                .AddQueryParameter("spreadSheet", "_series")
                .AddQueryParameter("operation", "all");
            var response = await restClient.GetAsync<List<Series>>(request);

            seriesList = response;

            Logger.Debug(response.Count, "Series Service Refresh Data Size");
        }

        public void Post()
        {
            var request = new RestRequest()
                   .AddQueryParameter("admin", adminKey)
                   .AddQueryParameter("spreadSheet", "_series")
                   .AddQueryParameter("operation", "all")
                   .AddJsonBody(seriesList);
            var response = restClient.Post<List<Series>>(request);

            Logger.Debug(response.Data, "Series Service Update");
        }

        public Series MakeSeries(ulong winner, ulong loser, int wins, int losses)
        {
            return new Series
            {
                Id = NextId,
                Date = DateTime.Now,
                WinnerId = winner,
                LoserId = loser,
                Wins = wins,
                Losses = losses
            };
        }

        public void AddPending(Series series)
        {
            pendingList.Add(series);
        }

        public void Approve(int id)
        {
            var series = pendingList.Find(x => x.Id == id);

            if (series == default)
            {
                return;
            }

            pendingList.Remove(series);
            seriesList.Add(series);
            Post();
        }

        public void AdminCreate(Series series)
        {
            seriesList.Add(series);
            Post();
        }

        public void Remove(int id)
        {
            var series = seriesList.Find(x => x.Id == id);
            seriesList.Remove(series);
            Post();
            Refresh();
        }

        public bool HasNewPlayer(Series series)
        {
            if (seriesList.Count(x => series.WinnerId == x.WinnerId || series.WinnerId == x.LoserId) == 0)
            {
                return true;
            }
            if (seriesList.Count(x => series.LoserId == x.LoserId || series.LoserId == x.WinnerId) == 0)
            {
                return true;
            }
            return false;
        }

        public bool CanApprove(Series series, IGuildUser user)
        {
            if (HasNewPlayer(series))
            {
                return user.GuildPermissions.Administrator;
            }
            else
            {
                return series.LoserId == user.Id || user.GuildPermissions.Administrator;
            }
        }
    }
}
