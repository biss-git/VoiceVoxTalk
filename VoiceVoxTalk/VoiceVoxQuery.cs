using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceVoxTalk
{
    class VoiceVoxQuery
    {
        public object accent_phrases { get; set; }
        public double speedScale { get; set; }
        public double pitchScale { get; set; }
        public double intonationScale { get; set; }
        public double volumeScale { get; set; }
        public double prePhonemeLength { get; set; }
        public double postPhonemeLength { get; set; }
        public int outputSamplingRate { get; set; }
        public bool outputStereo { get; set; }
        public string kana { get; set; }
    }
}
