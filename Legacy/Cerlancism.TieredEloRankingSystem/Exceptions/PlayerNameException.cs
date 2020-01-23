using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Exceptions
{
    public class PlayerNameException : Exception
    {
        public PlayerNameException(string message) : base(message)
        {

        }
    }
}
