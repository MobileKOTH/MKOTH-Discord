using System;
using System.Collections.Generic;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Core;

namespace Cerlancism.TieredEloRankingSystem.Models
{
    public class RankedSeries : ISeries
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public IPlayer Challenger { get; set; }
        public IPlayer Defender { get; set; }
        public int ChallengerWins { get; set; }
        public int DefenderWins { get; set; }
        public int Draws { get; set; }
        public string GameCode { get; set; }

        public void Validate(RankingProcessor processor)
        {
            var setting = processor.GuildSetting.Value;
            var tiers = setting.Tiers;
            throw new NotImplementedException();
        }
    }
}
