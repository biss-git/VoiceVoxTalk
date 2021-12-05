using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace VoiceVoxTalk
{
    internal class ProcessManager
    {
        public string ExePath { get; set; }

        public Process Process { get; set; }
    }
}
