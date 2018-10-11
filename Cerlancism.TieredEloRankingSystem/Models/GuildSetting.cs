using System;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Cerlancism.TieredEloRankingSystem.Core;

namespace Cerlancism.TieredEloRankingSystem.Models
{
    public class GuildSetting
    {
        [BsonId]
        public ulong GuildId { get; set; }

        public TierConfiguration[] Tiers { get; set; } = new TierConfiguration[]
        {
            new TierConfiguration
            {
                Name = "King",
                Icon = "👑",
                AllowHoliday = false,
                DemotionDays = 30,
                HardCap = 1,
                RewardPoint = 10,
                PromotePoint = 12,
                ChallengeRestriction = ChallengeRestriction.OneTierBelow,
                RankedCanTakeOverTiersBelow = 1
            },
            new TierConfiguration
            {
                Name = "Noble",
                Icon = "🥂",
                AllowHoliday = false,
                DemotionDays = 30,
                CapPercent = 0.1f,
                RewardPoint = 8,
                PromotePoint = 10,
                ChallengeRestriction = ChallengeRestriction.OneTierBelow,
                RankedCanTakeOverTiersBelow = 0,
                AutoPromoteCriterion = new SameClassWinPromoteCriterion
                {
                    WinStreaksRequired = 2
                }
            },
            new TierConfiguration
            {
                Name = "Squire",
                Icon = "⚔",
                AllowHoliday = true,
                IsEloTier = true,
                DemotionDays = 3,
                CapPercent = 1f/3f,
                RewardPoint = 6,
                PromotePoint = 8,
                ChallengeRestriction = ChallengeRestriction.NoRestriction,
                RankedCanTakeOverTiersBelow = 1,
                AutoPromoteCriterion = new MatureClassWinPromoteCriterion
                {
                    WinStreaksRequired = 3
                }
             },
            new TierConfiguration
            {
                Name = "Vassal",
                Icon = "🗡",
                AllowHoliday = true,
                IsEloTier = true,
                DemotionDays = 7,
                CapPercent = 0.75f,
                RewardPoint = 4,
                PromotePoint = 5,
                ChallengeRestriction = ChallengeRestriction.NoRestriction,
                RankedCanTakeOverTiersBelow = 1,
                AutoPromoteCriterion = new LowerClassWinEloPromoteCriterion
                {
                    WinStreaksRequired = 3
                }
            },
            new TierConfiguration
            {
                Name = "Peasant",
                Icon = "🔪",
                AllowHoliday = true,
                RewardPoint = 3,
                PromotePoint = 3,
                AutoPromoteCriterion = new SameClassWinEloPromoteCriterion
                {
                    WinStreaksRequired = 1
                }
            }
        };

        public int HolidayModeDays { get; set; } = 30;
        public float EloKFactor { get; set; } = 32;
        public int EloTBDGames { get; set; } = 5;

        public ulong SeriesLogChannelId { get; set; }
        public ulong RankingChannelId { get; set; }
        public ulong AdminLogChannelId { get; set; }

        public ulong[] LastRankingMessagesId { get; set; }

        public GuildSetting(ulong guildId, ulong defaultChannelId)
        {
            GuildId = guildId;
            SeriesLogChannelId = defaultChannelId;
            RankingChannelId = defaultChannelId;
            AdminLogChannelId = defaultChannelId;
        }

        public GuildSetting()
        {

        }
    }
}
