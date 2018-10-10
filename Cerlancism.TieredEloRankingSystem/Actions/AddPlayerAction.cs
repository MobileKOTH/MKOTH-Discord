using System;
using System.Collections.Generic;
using System.Text;
using Cerlancism.TieredEloRankingSystem.Core;
using Cerlancism.TieredEloRankingSystem.Models;

namespace Cerlancism.TieredEloRankingSystem.Actions
{
    public class AddPlayerAction : IAction
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public string PlayerName { get; set; }
        public ulong DiscordId { get; set; }
        public ActivePlayer Player { get; set; }

        public IAction Execute(RankingProcessor processor)
        {
            if (Player != null)
            {
                Player.Id = default;
                Player = processor.AddPlayerInternal(Player);
                return this;
            }
            Player = processor.AddPlayerInternal(PlayerName, DiscordId);
            return this;
        }

        public IAction Undo(RankingProcessor processor)
        {
            processor.DeletePlayerInternal(Player.Id);
            return this;
        }
    }
}
