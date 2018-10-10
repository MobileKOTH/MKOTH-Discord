using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Cerlancism.TieredEloRankingSystem.Extensions
{
    public static class LinkedListExtensions
    {
        public static int GetNodeDistance<T>(this LinkedListNode<T> current, LinkedListNode<T> target)
        {
            if (current == target)
            {
                return 0;
            }

            var value = countNextOrPrevious(x => x.Next);

            if (value.HasValue)
            {
                return value.Value;
            }
            else
            {
                return countNextOrPrevious(x => x.Previous).Value;
            }

            int? countNextOrPrevious(Func<LinkedListNode<T>, LinkedListNode<T>> selector)
            {
                var node = current;
                var count = 0;

                while (selector(node) != null)
                {
                    count--;
                    node = selector(node);
                    if (node == target)
                    {
                        return count;
                    }
                }

                return null;
            }
        }
    }
}
