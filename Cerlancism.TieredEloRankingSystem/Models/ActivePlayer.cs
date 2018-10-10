using System;
using System.Collections.Generic;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Core;
using LiteDB;
using System.Linq;
using Newtonsoft.Json;

namespace Cerlancism.TieredEloRankingSystem.Models
{
    public class ActivePlayer : IPlayer
    {
        public int Id { get; set; }
        public ulong DiscordId { get; set; }
        public string Name { get; set; }
        public DateTime JointDate { get; set; } = DateTime.Now;
        public float Points { get; set; }
        public List<EloHistory> EloHistory { get; set; } = new List<EloHistory>() { new EloHistory { TimeStamp = DateTime.Now, Elo = 1200 } };
        public string TierName { get; set; }
        public int InactiveDays { get; set; }
        public int DemotionWarningDays { get; set; }
        public int WinStreaksAboveClass { get; set; }
        public int WinStreaksMatureClass { get; set; }
        public int WinStreaksSameClass { get; set; }
        public int WinStreaksSameOrLowerClass { get; set; }

        public override string ToString()
        {
            return $"{Name} ({TierName}) |{DiscordId}|";
        }

    }
}
