using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Exceptions
{
    public class PlayerInvalidException : Exception
    {
        public PlayerInvalidException(string message = "Player not found.") : base(message)
        {

        }
    }
}
