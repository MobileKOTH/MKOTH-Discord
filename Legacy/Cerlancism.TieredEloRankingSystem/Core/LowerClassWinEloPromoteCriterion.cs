using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public class LowerClassWinEloPromoteCriterion : IPromoteCriterion
    {
        public int WinStreaksRequired { get; set; }

        public bool CanPromote(IPlayer player, RankingProcessor processor)
        {
            return (player.WinStreaksMatureClass + player.WinStreaksAboveClass + player.WinStreaksSameOrLowerClass) >= WinStreaksRequired
                && player.EloHistory.Count > processor.GuildSetting.Value.EloTBDGames 
                && player.EloHistory.Last().Elo >= processor.GetTargetElo(player);
        }
    }
}
