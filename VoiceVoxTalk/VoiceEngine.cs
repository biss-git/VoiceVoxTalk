using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Yomiage.SDK;
using Yomiage.SDK.Common;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace VoiceVoxTalk
{
    public class VoiceEngine : VoiceEngineBase
    {
        HttpClient client;

        public VoiceEngine()
        {
            client = new HttpClient();
        }

        public override void Dispose()
        {
            base.Dispose();
            client.Dispose();
        }

        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz)
        {
            if (IsPlaying) { return new double[0]; }
            IsPlaying = true;
            int speaker = 0;
            if (mainVoice.Library.Settings.Ints.TryGetSetting("Speaker", out var speakerSetting))
            {
                speaker = speakerSetting.Value;
            }

            var portNum = 50021;
            if (mainVoice.Library.Settings.Strings.TryGetSetting("Port", out var portSetting) &&
                int.TryParse(portSetting.Value, out var port))
            {
                portNum = port;
            }


            (var fs, var wave) = await CallVoiceVoxApi(
                talkScript.OriginalText,
                portNum,
                speaker,
                mainVoice.VoiceEffect.Volume.Value,
                mainVoice.VoiceEffect.Speed.Value,
                mainVoice.VoiceEffect.Pitch.Value,
                mainVoice.VoiceEffect.Emphasis.Value,
                masterEffect.EndPause);

            setSamplingRate_Hz(fs);

            IsPlaying = false;
            return wave;
        }

        private async Task<(int, double[])> CallVoiceVoxApi(string text, int portNum = 50021, int speaker = 0, double volume = 1, double speed = 1, double pitch = 0, double emphasis = 1, double endPauseMs = 100)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (44100, new double[0]);
            }

            try
            {
                var baseUrl = $"http://127.0.0.1:{portNum}";
                var query = await client?.PostAsync($"{baseUrl}/audio_query?text={text}&speaker={speaker}", null);
                var queryJson = await query.Content.ReadAsStringAsync();
                var voiceVoxQuery = JsonUtil.DeserializeFromString<VoiceVoxQuery>(queryJson);
                if (voiceVoxQuery != null)
                {
                    voiceVoxQuery.volumeScale = volume;
                    voiceVoxQuery.speedScale = speed;
                    voiceVoxQuery.pitchScale = pitch;
                    voiceVoxQuery.intonationScale = emphasis;
                    voiceVoxQuery.postPhonemeLength = Math.Max(endPauseMs / 1000, 0.1);
                    queryJson = JsonUtil.SerializeToString(voiceVoxQuery);
                }
                var content = new StringContent(queryJson, Encoding.UTF8, "application/json");
                var result = await client?.PostAsync($"{baseUrl}/synthesis?speaker={speaker}", content);
                var bytes = await result.Content.ReadAsByteArrayAsync();

                var filePath = Path.Combine(DllDirectory, "temp_.wav");
                File.WriteAllBytes(filePath, bytes);

                using var reader = new WaveFileReader(filePath);
                var wave = new List<double>();
                int fs = reader.WaveFormat.SampleRate;
                while (reader.Position < reader.Length)
                {
                    var samples = reader.ReadNextSampleFrame();
                    wave.Add(samples.First());
                }

                return (fs, wave.ToArray());
            }
            catch (Exception e)
            {

            }

            return (44100, new double[0]);
        }

    }
}
