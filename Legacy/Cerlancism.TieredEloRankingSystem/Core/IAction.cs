using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Core
{
    public interface IAction
    {
        int Id { get; set; }
        DateTime TimeStamp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the current action.</returns>
        IAction Execute(RankingProcessor processor);

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns the undo action.</returns>
        IAction Undo(RankingProcessor processor);
    }
}
