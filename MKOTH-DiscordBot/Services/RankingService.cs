using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace MKOTHDiscordBot.Services
{
    public class RankingService
    {
        private readonly SeriesService series;
        public RankingService(IServiceProvider services)
        {
            series = services.GetService<SeriesService>();
        }

        public void Refresh()
        {
            series.ToString();
        }
    }
}
