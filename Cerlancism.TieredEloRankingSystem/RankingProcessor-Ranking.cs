using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Cerlancism.TieredEloRankingSystem.Models;
using Cerlancism.TieredEloRankingSystem.Core;

namespace Cerlancism.TieredEloRankingSystem
{
    using static Extensions.FuncExtensions;

    public partial class RankingProcessor
    {
        public LinkedList<PlayerRank> Rankings
            => RankingCollection.Value
            .IncludeAll()
            .FindAll()
            .Forward(x => new LinkedList<PlayerRank>(x));

        public int GetTeirRank(LinkedList<PlayerRank> rankings, string tierName)
            => rankings.Last(x => x.Player.TierName == tierName).Position;

        public int GetRank(IPlayer player)
        {
            var rankings = Rankings;
            var rank = rankings.SingleOrDefault(x => x.Player.Id == player.Id)?.Position ?? GetTeirRank(rankings, player.TierName);
            return rank;
        }
    }
}
