using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Cerlancism.TieredEloRankingSystem.Core;

namespace Cerlancism.TieredEloRankingSystem.Models
{
    public class PlayerRank
    {
        [BsonId]
        public int Position { get; set; }

        [BsonRef("IPlayer")]
        public ActivePlayer Player { get; set; }
    }
}
