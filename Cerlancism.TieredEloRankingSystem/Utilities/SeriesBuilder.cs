using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Exceptions;
using Cerlancism.TieredEloRankingSystem.Models;


namespace Cerlancism.TieredEloRankingSystem.Utilities
{
    using static TypeCaster;

    public static class SeriesBuilder
    {
        public static RankedSeries BuildRankedSeries(RankingProcessor processor, ulong player1DiscordId, ulong player2DiscordId, int leftWin, int rightWin, int draws = 0)
        {
            var (player1, player2) = ValidatePlayers(processor, player1DiscordId, player2DiscordId);
            var (challenger, defender) = OrderPlayers(processor, (player1, leftWin), (player2, rightWin));

            return new RankedSeries
            {
                Challenger = challenger.player,
                Defender = defender.player,
                ChallengerWins = challenger.wins,
                DefenderWins = defender.wins
            };
        }

        private static (IPlayer player1, IPlayer player2) ValidatePlayers(RankingProcessor processor, ulong player1DiscordId, ulong player2DiscordId)
        {
            var player1 = validatePlayer(player1DiscordId);
            var player2 = validatePlayer(player2DiscordId);

            return (player1, player1);

            IPlayer validatePlayer(ulong discordId)
            {
                var player = processor.PlayerCollection.Value.FindOne(x => x.DiscordId == discordId) ?? throw new PlayerInvalidException("Player(s) not registered.");
                if (player is RemovedPlayer)
                {
                    throw new PlayerInvalidException("Contains removed player(s).");
                }
                return player;
            }            
        }

        public static (IPlayer player1, IPlayer player2) EnsureActive(RankingProcessor processor, IPlayer player1, IPlayer player2)
        {
            player1 = ensure(player1);
            player2 = ensure(player2);

            return (player1, player2);

            IPlayer ensure(IPlayer player)
            {
                if (player is HolidayPlayer holidayPlayer)
                {
                    var active = CastToNew<ActivePlayer>(holidayPlayer);
                    active.InactiveDays = 0;

                    // TODO
                    throw new NotImplementedException();
                    return active;
                }
                return player;
            }
        }

        private static ((IPlayer player, int wins), (IPlayer player, int wins)) OrderPlayers(RankingProcessor processor, (IPlayer player, int wins) player1, (IPlayer player, int wins ) player2)
        {
            var challenger = processor.GetRank(player1.player) > processor.GetRank(player2.player) ? player1 : player2;
            var defender = processor.GetRank(player1.player) > processor.GetRank(player2.player) ? player1 : player2;
            return (challenger, defender);
        }
    }
}
