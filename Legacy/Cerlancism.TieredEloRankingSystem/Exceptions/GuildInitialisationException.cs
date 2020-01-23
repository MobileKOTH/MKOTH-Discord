using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Exceptions
{
    public class GuildInitialisationException : Exception
    {
        public GuildInitialisationException(string message = "Error: Guild setting not initialised. Use `.initialise` command to initialise the guild.") 
            : base(message)
        {

        }
    }
}
