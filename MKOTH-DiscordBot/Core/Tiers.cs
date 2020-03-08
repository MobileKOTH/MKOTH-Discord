using System;
using System.Collections.Generic;
using System.Text;
using MKOTHDiscordBot.Models;
using System.Linq;

namespace MKOTHDiscordBot.Core
{
    public interface ITier
    {
        string Name { get; set; }
        string Icon { get; set; }
        double EloRequirement { get; set; }
    }

    public enum Tiers
    {
        King,
        Nobles = 1400,
        Squires = 1300,
        Vassals = 1250,
        Peasants
    }


    public static class TeirsExtensions
    {
        public static string TierIcon(this Tiers tier) => tier switch
        {
            Tiers.King => "👑",
            Tiers.Nobles => "💰",
            Tiers.Squires => "⚔",
            Tiers.Vassals => "⚒",
            _ => "🛠",
        };

        public static Tiers PlayerTeir(this IEnumerable<SeriesPlayer> seriesPlayers, string id) => seriesPlayers.First(x => x.Id == id).Elo switch
        {
            double x when x >= (int)Tiers.Nobles => (seriesPlayers.First().Elo == x && seriesPlayers.Count(y => y.Elo >= 1400) > 2) ? Tiers.King : Tiers.Nobles,
            double x when x >= (int)Tiers.Squires => Tiers.Squires,
            double x when x >= (int)Tiers.Vassals => Tiers.Vassals,
            _ => Tiers.Peasants
        };

        public static ITier ToTierModel(this Tiers tier)
        {
            return new TiersModel { Name = tier.ToString("g"), Icon = tier.TierIcon(), EloRequirement = (double)tier };
        }

        private class TiersModel : ITier
        {
            public string Name { get; set; }
            public string Icon { get; set; }
            public double EloRequirement { get; set; }
        }
    }
}
