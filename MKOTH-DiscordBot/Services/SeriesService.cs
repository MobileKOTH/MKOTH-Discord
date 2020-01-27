using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Microsoft.Extensions.Options;
using MKOTHDiscordBot.Models;
using MKOTHDiscordBot.Properties;

namespace MKOTHDiscordBot.Services
{
    public class SeriesService
    {
        private readonly string endPoint;
        private readonly string adminKey;

        private readonly List<Series> seriesList;
        public SeriesService(IServiceProvider services, IOptions<AppSettings> appSettings, IOptions<Credentials> credentials)
        {
            endPoint = appSettings.Value.ConnectionStrings.AppsScript;
            adminKey = credentials.Value.AppsScriptAdminKey;
        }

        public void Refresh()
        {

        }
    }
}
