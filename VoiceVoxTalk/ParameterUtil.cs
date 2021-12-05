using System;
using System.Collections.Generic;
using System.Text;
using Yomiage.SDK.Settings;
using Yomiage.SDK.VoiceEffects;

namespace VoiceVoxTalk
{
    internal static class ParameterUtil
    {
        public static double GetPitch(this VoiceEffectValueBase effect)
        {
            var key = "pitch";
            if(effect.AdditionalEffect?.TryGetValue(key, out var value) == true &&
                value != null)
            {
                return value.Value;
            }
            return 0;
        }

        public static double GetConsonantLength(this VoiceEffectValueBase effect)
        {
            var key = "consonant_length";
            if (effect.AdditionalEffect?.TryGetValue(key, out var value) == true &&
                value != null)
            {
                return value.Value;
            }
            return 0;
        }

        public static double GetVowelLength(this VoiceEffectValueBase effect)
        {
            var key = "vowel_length";
            if (effect.AdditionalEffect?.TryGetValue(key, out var value) == true &&
                value != null)
            {
                return value.Value;
            }
            return 0;
        }

        public static int GetSpeaker(this SettingsBase settings)
        {
            var key = "Speaker";
            if (settings.Ints?.TryGetSetting(key, out var value) == true)
            {
                return value.Value;
            }
            return 0;
        }

        public static double GetMorphRatio(this SettingsBase settings)
        {
            var key = "MorphRatio";
            if (settings.Doubles?.TryGetSetting(key, out var value) == true)
            {
                return value.Value;
            }
            return 0.5;
        }

        public static int GetPortNum(this SettingsBase settings)
        {
            var key = "Port";
            if (settings.Strings?.TryGetSetting(key, out var value) == true &&
                int.TryParse(value.Value, out var port))
            {
                return port;
            }
            return 50021;
        }

        public static string GetExePath(this SettingsBase settings)
        {
            var key = "ExePath";
            if (settings.Strings?.TryGetSetting(key, out var value) == true)
            {
                return value.Value;
            }
            return string.Empty;
        }
    }
}
