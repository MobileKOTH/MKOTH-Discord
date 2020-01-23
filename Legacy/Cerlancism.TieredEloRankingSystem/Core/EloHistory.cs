using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public struct EloHistory
    {
        public DateTime TimeStamp { get; set; }
        public float Elo { get; set; }
    }
}
