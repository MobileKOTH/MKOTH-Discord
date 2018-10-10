using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Actions;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Exceptions;
using Cerlancism.TieredEloRankingSystem.Models;

namespace Cerlancism.TieredEloRankingSystem
{
    using static Extensions.LinkedListExtensions;
    using static Extensions.FuncExtensions;

    using static Utilities.PercentileCalculator;

    public partial class RankingProcessor
    {
        public IEnumerable<IPlayer> GetAllPlayers()
            => PlayerCollection.Value
            .IncludeAll()
            .FindAll();

        public IEnumerable<ActivePlayer> GetActivePlayers()
            => GetAllPlayers()
            .Where(x => !(x is HolidayPlayer))
            .Where(x => !(x is RemovedPlayer))
            .Select(x => x as ActivePlayer);

        /// <summary>
        /// Checks the player names, throws exception if not valid.
        /// </summary>
        /// <exception cref="PlayerNameException"></exception>
        /// <param name="name"></param>
        public void ValidateName(string name)
        {
            if (name.Length > 30)
            {
                throw new PlayerNameException("Player name too long, must be within 30 characters");
            }
            if (name.Length < 3)
            {
                throw new PlayerNameException("Player name too short, must be more than 3 characters");
            }
            if (GetAllPlayers().Any(x => x.Name.ToLower() == name.ToLower()))
            {
                throw new PlayerNameException($"Player name already exist for {name}");
            }
        }

        public void ValidateDiscordId(ulong id)
        {
            if (GetAllPlayers().Any(x => x.DiscordId == id))
            {
                throw new PlayerInvalidException("Player already registered with this Discord account.");
            }
        }

        public IAction AddPlayer(string name, ulong discordId)
        {
            var action = new AddPlayerAction
            {
                PlayerName = name,
                DiscordId = discordId
            };
            return PerformAction(action);
        }

        internal ActivePlayer AddPlayerInternal(string name, ulong discordId)
        {
            ValidateName(name);
            ValidateDiscordId(discordId);

            var player = new ActivePlayer
            {
                Name = name,
                DiscordId = discordId,
                TierName = Tiers.Value.Last.Value.Name,
            };

            player.Id = AddPlayerInternal(player).Id;

            return player;
        }

        internal ActivePlayer AddPlayerInternal(ActivePlayer player)
        {
            player.Id = PlayerCollection.Value.Insert(player);
            var rank = new PlayerRank { Player = player };
            RankingCollection.Value.Insert(rank);
            return player;
        }

        internal IPlayer DeletePlayerInternal(int id)
        {
            var player = PlayerCollection.Value.FindById(id);
            if (player == null)
            {
                throw new PlayerInvalidException();
            }
            var rankings = Rankings;
            rankings.Remove(rankings.Single(x => x.Player.Id == id));
            var listings = Enumerable.Range(1, rankings.Count);
            var newRankings = listings.Zip(rankings, (i, r) => new PlayerRank
            {
                Position = i,
                Player = r.Player
            });

            database.DropCollection(RankingCollection.Value.Name);
            RankingCollection.Value.InsertBulk(newRankings);
            PlayerCollection.Value.Delete(player.Id);
            return player;
        }

        public TierConfiguration GetTier(IPlayer player)
            => GuildSetting.Value.Tiers.Single(x => x.Name == player.TierName);

        public LinkedListNode<TierConfiguration> GetTierNode(IPlayer player)
            => Tiers.Value.Find(GetTier(player));

        public LinkedListNode<TierConfiguration> GetTierNode(TierConfiguration tier)
            => Tiers.Value.Find(tier);

        public int GetTierDistance(IPlayer left, IPlayer right)
            => GetTierNode(left)
            .GetNodeDistance(GetTierNode(right));

        public bool IsSameTier(IPlayer left, IPlayer right)
            => GetTierDistance(left, right) == 0;

        public float GetTargetElo(IPlayer player)
            => GetTierTargetElo(GetTier(player));

        public float GetTierTargetElo(TierConfiguration tier)
        {
            var activePlayers = GetActivePlayers();
            var activeElos = activePlayers.Where(x => x.EloHistory.Count > GuildSetting.Value.EloTBDGames)
                .Select(x => x.EloHistory.Last().Elo);
            var percentile = GetPercentile(activeElos, tier.CapPercent.Value);
            return percentile;
        }
    }
}
