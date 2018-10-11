using System;
using System.Collections.Generic;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Models;
using System.Linq;

namespace Cerlancism.TieredEloRankingSystem.Actions
{
    public class RemovePlayerAction : IAction
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public IPlayer OldState { get; set; }
        public RemovedPlayer Player { get; set; }

        public IAction Execute(RankingProcessor processor)
        {
            processor.RemovePlayerInternal(Player);
            return this;
        }

        public IAction Undo(RankingProcessor processor)
        {
            processor.repository.Update(OldState);
            var ranking = processor.Rankings;
            if (OldState is ActivePlayer activePlayer)
            {
                var rank = new PlayerRank { Player = activePlayer };
                ranking.AddAfter(ranking.Find(ranking.First(x => x.Position == OldState.Rank.Position - 1)), rank);
                processor.UpdateRankingsInternal(ranking);
            }
            return this;
        }
    }
}
