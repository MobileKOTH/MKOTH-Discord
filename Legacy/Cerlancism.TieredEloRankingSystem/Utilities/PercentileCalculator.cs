using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cerlancism.TieredEloRankingSystem.Utilities
{
    public static class PercentileCalculator
    {
        public static float GetPercentile(IEnumerable<float> sequence, float percentile)
        {
            var elements = sequence.ToArray();
            Array.Sort(elements);
            float realIndex = percentile * (elements.Length - 1);
            int index = (int)realIndex;
            float frac = realIndex - index;
            if (index + 1 < elements.Length)
                return elements[index] * (1 - frac) + elements[index + 1] * frac;
            else
                return elements[index];
        }
    }
}
