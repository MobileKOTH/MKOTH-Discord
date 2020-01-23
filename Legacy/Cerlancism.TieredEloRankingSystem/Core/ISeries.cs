using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public interface ISeries
    {
        int Id { get; set; }
        DateTime TimeStamp { get; set; }

        IPlayer Challenger { get; set; }
        IPlayer Defender { get; set; }

        int ChallengerWins { get; set; }
        int DefenderWins { get; set; }

        string GameCode { get; set; }

        void Validate(RankingProcessor processor);
    }
}
