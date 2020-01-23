using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Cerlancism.TieredEloRankingSystem.Models;
using Cerlancism.TieredEloRankingSystem.Core;
using LiteDB;

namespace Cerlancism.TieredEloRankingSystem
{
    using static Extensions.FuncExtensions;

    public partial class RankingProcessor
    {
        private LiteQueryable<PlayerRank> RankQuery
            => repository.Query<PlayerRank>()
            .Include(x => x.Player);

        public LinkedList<PlayerRank> Rankings
            => RankQuery
            .ToEnumerable()
            .Forward(x => new LinkedList<PlayerRank>(x));

        internal void UpdateRankingsInternal(LinkedList<PlayerRank> rankings)
        {
            var range = Enumerable.Range(1, rankings.Count);
            var newRankings = range.Zip(rankings, (i, rank) =>
            {
                rank.Player.Rank.Position = i;
                var newRank = new PlayerRank
                {
                    Position = i,
                    Player = rank.Player
                };
                repository.Update(rank.Player as IPlayer);
                return newRank;
            });
            var rankCollection = Database.GetCollection<PlayerRank>();
            Database.DropCollection(rankCollection.Name);
            rankCollection.InsertBulk(newRankings);
            var actives = GetActivePlayers();
        }

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
