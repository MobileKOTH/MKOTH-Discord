using System;
using System.Collections.Generic;
using System.Text;

namespace Cerlancism.ChatSystem.Core
{
    public class AnalysisResult
    {
        public float Score { get; set; }
        public ChatHistory Trigger { get; set; }
        public ChatHistory Rephrase { get; set; }
        public ChatHistory Response { get; set; }
    }
}
