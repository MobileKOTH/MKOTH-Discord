using System;
using System.Collections.Generic;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Core;

namespace Cerlancism.TieredEloRankingSystem.Models
{
    public class RemovedPlayer : IPlayer
    {
        public int Id { get; set; }
        public ulong DiscordId { get; set; }
        public string Name { get; set; }
        public DateTime JointDate { get; set; }
        public float Points { get; set; }
        public List<EloHistory> EloHistory { get; set; }
        public string TierName { get; set; }
        public int DemotionWarningDays { get; set; }
        public int WinStreaksAboveClass { get; set; }
        public int WinStreaksMatureClass { get; set; }
        public int WinStreaksSameClass { get; set; }
        public int WinStreaksSameOrLowerClass { get; set; }

        public PlayerRank Rank { get; set; }

        public DateTime RemovedDate { get; set; }
    }
}
