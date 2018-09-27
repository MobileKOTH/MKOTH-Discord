using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Utilities
{
    public static class FuncUtilities
    {
        public static Func<In, Out> Funcify<In, Out>(Func<In, Out> function)
            => function;
    }
}
