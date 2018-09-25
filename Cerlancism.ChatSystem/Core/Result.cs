using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Core
{
    public class Result
    {
        public float Score { get; set; }
        public History Trigger { get; set; }
        public History Rephrase { get; set; }
        public History Response { get; set; }
    }
}
