﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cerlancism.ChatSystem.Extensions
{
    public static class GenericExtensions
    {
        static Random Random = new Random();
        public static T SelectRandom<T>(this IEnumerable<T> collection, Random rng = null)
            => collection.ElementAt((int)(rng ?? Random).NextDouble() * collection.Count());
    }
}
