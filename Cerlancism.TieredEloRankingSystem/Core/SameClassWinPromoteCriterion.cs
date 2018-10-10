using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public class SameClassWinPromoteCriterion : IPromoteCriterion
    {
        public int WinStreaksRequired { get; set; }

        public bool CanPromote(IPlayer player, RankingProcessor processor)
        {
            return player.WinStreaksSameClass + player.WinStreaksMatureClass >= WinStreaksRequired;
        }
    }
}
