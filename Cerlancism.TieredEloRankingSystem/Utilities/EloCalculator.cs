using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Utilities
{
    public static class EloCalculator
    {
        public enum MatchState
        {
            LeftWin,
            RightWin,
            Draw
        }

        public static (float eloLeft, float eloRight) Calculate(float KFactor, float eloLeft, float eloRight, int winsLeft, int winsRight, int draws = 0)
        {
            throw new NotImplementedException();
        }

        public static (float eloLeft, float eloRight) Calculate(float KFactor, float eloLeft, float eloRight, MatchState matchState)
        {
            throw new NotImplementedException();
        }
    }
}
