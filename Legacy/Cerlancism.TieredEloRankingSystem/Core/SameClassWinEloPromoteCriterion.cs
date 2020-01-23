using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public class SameClassWinEloPromoteCriterion : IPromoteCriterion
    {
        public int WinStreaksRequired { get; set; }

        public bool CanPromote(IPlayer player, RankingProcessor processor)
        {
            return (player.WinStreaksSameClass + player.WinStreaksMatureClass + player.WinStreaksAboveClass) >= WinStreaksRequired
                && player.EloHistory.Count > processor.GuildSetting.Value.EloTBDGames
                && player.EloHistory.Last().Elo >= processor.GetTargetElo(player);
        }
    }
}
