using System;
using System.Collections.Generic;
using System.Text;

namespace MKOTHDiscordBot.Utilities
{
    public static class EloCalculator
    {

        public static (double eloLeft, double eloRight) Calculate(double KFactor, double eloLeft, double eloRight, int winsLeft, int winsRight, int draws = 0)
        {
            // s1 = 1 if player 1 wins, s1 = 0 if player 2 wins, s1 = 0.5 if draw
            double s1;
            for (int i = 0; i < winsLeft; i++)
            {
                s1 = 1;
                process();
            }

            for (int i = 0; i < winsRight; i++)
            {
                s1 = 0;
                process();
            }

            for (int i = 0; i < draws; i++)
            {
                s1 = 0.5;
                process();
            }

            void process()
            {
                var r1 = Math.Pow(10, eloLeft / 400.0);
                var r2 = Math.Pow(10, eloRight / 400.0);
                var s2 = Math.Abs(s1 - 1);
                eloLeft += KFactor * (s1 - (r1 / (r1 + r2)));
                eloRight += KFactor * (s2 - (r2 / (r1 + r2)));
            }

            return (eloLeft, eloRight);
        }
    }
}
