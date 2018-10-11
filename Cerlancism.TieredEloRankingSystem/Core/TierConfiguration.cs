using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public enum ChallengeRestriction
    {
        NoRestriction,
        OneTierBelow,
        SameTier
    }

    public class TierConfiguration
    {
        public ulong RoleId { get; set; }
        public string Icon { get; set; }
        public string Name { get; set; }
        public bool AllowHoliday { get; set; }
        public bool IsEloTier { get; set; }
        public int DemotionDays { get; set; }
        public float? CapPercent { get; set; }
        public int? HardCap { get; set; }
        public int PromotePoint { get; set; }
        public int RewardPoint { get; set; }
        public float? TargetElo { get; set; }
        public IPromoteCriterion AutoPromoteCriterion { get; set; }
        public ChallengeRestriction ChallengeRestriction { get; set; }
        public int RankedCanTakeOverTiersBelow { get; set; }

        public override bool Equals(object obj)
        {
            var configuration = obj as TierConfiguration;
            return configuration != null &&
                   Name == configuration.Name;
        }

        public override int GetHashCode()
        {
            return 539060726 + EqualityComparer<string>.Default.GetHashCode(Name);
        }

        public static bool operator ==(TierConfiguration configuration1, TierConfiguration configuration2)
        {
            return EqualityComparer<TierConfiguration>.Default.Equals(configuration1, configuration2);
        }

        public static bool operator !=(TierConfiguration configuration1, TierConfiguration configuration2)
        {
            return !(configuration1 == configuration2);
        }
    }
}
