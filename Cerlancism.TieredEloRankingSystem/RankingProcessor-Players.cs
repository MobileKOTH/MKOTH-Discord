using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Actions;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Exceptions;
using Cerlancism.TieredEloRankingSystem.Models;
using LiteDB;

namespace Cerlancism.TieredEloRankingSystem
{
    using static Extensions.LinkedListExtensions;
    using static Extensions.FuncExtensions;

    using static Utilities.PercentileCalculator;

    public partial class RankingProcessor
    {
        private LiteQueryable<IPlayer> PlayerQuery
            => repository.Query<IPlayer>()
            .Include(x => x.Rank);

        public IEnumerable<IPlayer> GetAllPlayers()
            => PlayerQuery
            .ToEnumerable();

        public IEnumerable<ActivePlayer> GetActivePlayers()
            => PlayerQuery
            .Where(x => !(x is HolidayPlayer))
            .Where(x => !(x is RemovedPlayer))
            .ToEnumerable()
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
            if (name.Length < 2)
            {
                throw new PlayerNameException("Player name too short, must be more than 2 characters");
            }
            if (repository.SingleOrDefault<IPlayer>(x => x.Name.ToLower() == name.ToLower()) != null)
            {
                throw new PlayerNameException($"Player name already exist for {name}");
            }
        }

        public void ValidateDiscordId(ulong id)
        {
            if (repository.SingleOrDefault<IPlayer>(x => x.DiscordId == id) != null)
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

        internal IPlayer AddPlayerInternal(IPlayer player)
        {
            player.Id = repository.Insert(player);
            player.Rank = new PlayerRank { Player = player as ActivePlayer };
            player.Rank.Position = repository.Insert(player.Rank);
            repository.Update(player);
            return player;
        }

        public IAction RemovePlayer(ulong discordId)
        {
            var player = PlayerQuery.Where(x => x.DiscordId == discordId).SingleOrDefault();
            if (player == default)
            {
                throw new PlayerInvalidException("Player not found.");
            }

            var removePlayer = Utilities.TypeCaster.CastToNew<RemovedPlayer>(player);
            removePlayer.RemovedDate = DateTime.Now;

            var action = new RemovePlayerAction
            {
                OldState = player,
                Player = removePlayer
            };

            return PerformAction(action);
        }

        public IPlayer RemovePlayerInternal(IPlayer player)
        {
            repository.Update(player);
            if (repository.FirstOrDefault<PlayerRank>(x => x.Player.Id == player.Id) != null)
            {
                repository.Delete<PlayerRank>(player.Rank.Position);
                UpdateRankingsInternal(Rankings);
            }
            return player;
        }

        internal IPlayer DeletePlayerInternal(int id)
        {
            var player = PlayerQuery.SingleById(id);
            if (player == null)
            {
                throw new PlayerInvalidException();
            }

            var rankings = Rankings;
            rankings.Remove(rankings.Single(x => x.Player.Id == id));
            UpdateRankingsInternal(rankings);

            repository.Delete<IPlayer>(player.Id);
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
