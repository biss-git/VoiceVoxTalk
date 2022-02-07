using System;
using System.Collections.Generic;
using System.Text;

namespace VoiceVoxTalk
{
    class VoiceVoxQuery
    {
        public List<AccentPhrase> accent_phrases { get; set; }
        public double speedScale { get; set; } = 1;
        public double pitchScale { get; set; } = 0;
        public double intonationScale { get; set; } = 1;
        public double volumeScale { get; set; } = 1;
        public double prePhonemeLength { get; set; } = 0.1;
        public double postPhonemeLength { get; set; } = 0.1;
        public int outputSamplingRate { get; set; } = 24000;
        public bool outputStereo { get; set; } = false;
        public string kana { get; set; } = "カナ";
    }

    internal class AccentPhrase
    {
        public List<VoiceVoxMora> moras { get; set; }
        public int accent { get; set; }
        public VoiceVoxMora pause_mora { get; set; }

    }

    internal class VoiceVoxMora
    {
        public string text { get; set; }
        public string consonant { get; set; }
        public double? consonant_length { get; set; }
        public string vowel { get; set; }
        public double? vowel_length { get; set; }
        public double pitch { get; set; }

        public VoiceVoxMora()
        {
        }

        public VoiceVoxMora(double pause_length_sec)
        {
            text = "、";
            vowel = "pau";
            vowel_length = pause_length_sec;
        }

        public void ShiftPitch(double value)
        {
            pitch = Math.Clamp(pitch + value, 0, 20);
        }

        /// <summary>
        /// 抑揚を適用する
        /// </summary>
        /// <param name="emphasis">抑揚（1でそのまま）</param>
        /// <param name="basePitch">基本ピッチ</param>
        public void EmphasisPitch(double emphasis, double basePitch)
        {
            if (pitch > 0 && basePitch > 0)
            {
                pitch = Math.Clamp(basePitch + (pitch - basePitch) * emphasis, 0, 20);
            }
        }

        public void ShiftConsonantLength(double value)
        {
            if (consonant_length != null)
            {
                consonant_length = Math.Clamp(consonant_length.Value + value, 0, 10);
            }
        }
        public void GainConsonantLength(double value)
        {
            if (consonant_length != null)
            {
                consonant_length = Math.Clamp(consonant_length.Value * value, 0, 10);
            }
        }

        public void ShiftVowelLength(double value)
        {
            if (vowel_length != null)
            {
                vowel_length = Math.Clamp(vowel_length.Value + value, 0, 10);
            }
        }

        public void GainVowelLength(double value)
        {
            if (vowel_length != null)
            {
                vowel_length = Math.Clamp(vowel_length.Value * value, 0, 10);
            }
        }

    }
}
