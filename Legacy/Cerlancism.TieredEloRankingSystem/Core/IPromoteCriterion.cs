using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{

    public interface IPromoteCriterion
    {
        bool CanPromote(IPlayer player, RankingProcessor processor);
    }
}
