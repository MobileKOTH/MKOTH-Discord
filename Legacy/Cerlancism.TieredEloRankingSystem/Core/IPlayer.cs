using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Cerlancism.TieredEloRankingSystem.Models;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public interface IPlayer
    {
        int Id { get; set; }
        ulong DiscordId { get; set; }
        string Name { get; set; }
        DateTime JointDate { get; set; }
        float Points { get; set; }
        List<EloHistory> EloHistory { get; set; }
        string TierName { get; set; }
        int DemotionWarningDays { get; set; }
        int WinStreaksAboveClass { get; set; }
        int WinStreaksMatureClass { get; set; }
        int WinStreaksSameClass { get; set; }
        int WinStreaksSameOrLowerClass { get; set; }

        PlayerRank Rank { get; set; }
    }
}
