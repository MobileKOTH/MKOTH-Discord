using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Model
{
    public class Analysis
    {
        public float Score { get; set; }
        public Entry Trigger { get; set; }
        public Entry Rephrase { get; set; }
        public Entry Response { get; set; }
    }
}
