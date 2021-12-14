using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Yomiage.SDK;
using Yomiage.SDK.Common;
using Yomiage.SDK.Config;
using Yomiage.SDK.Talk;
using Yomiage.SDK.VoiceEffects;

namespace VoiceVoxTalk
{
    public class VoiceEngine : VoiceEngineBase
    {
        HttpClient client;
        ProcessManager processManager = new ProcessManager();

        public VoiceEngine()
        {
            client = new HttpClient();
        }

        public override void Initialize(string configDirectory, string dllDirectory, EngineConfig config)
        {
            base.Initialize(configDirectory, dllDirectory, config);

            Task.Run(async() =>
            {
                try
                {
                    var portNum = Settings.GetPortNum();
                    var baseUrl = $"http://127.0.0.1:{portNum}";
                    var result = await client?.GetAsync($"{baseUrl}/version");
                }
                catch (Exception)
                {
                    Boot();
                }
            });
        }

        private void Boot()
        {
            try
            {
                var exePath = Settings.GetExePath();
                if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
                {
                    if (processManager.ExePath != exePath)
                    {
                        processManager.ExePath = exePath;
                        if (processManager.Process != null)
                        {
                            processManager.Process.Kill();
                            processManager.Process = null;
                        }

                        var processStartInfo = new ProcessStartInfo()
                        {
                            FileName = exePath,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                        };
                        processManager.Process = Process.Start(processStartInfo);
                        processManager.Process.WaitForInputIdle();
                    }
                }
            }
            catch
            {

            }
        }

        public override void Dispose()
        {
            base.Dispose();
            client.Dispose();
            processManager.Process?.Kill();
        }

        public override async Task<double[]> Play(VoiceConfig mainVoice, VoiceConfig subVoice, TalkScript talkScript, MasterEffectValue masterEffect, Action<int> setSamplingRate_Hz, Action<double[]> submitWavePart)
        {
            if (IsPlaying) { return new double[0]; }
            IsPlaying = true;

            int speaker = mainVoice.Library.Settings.GetSpeaker();
            var portNum = Settings.GetPortNum();

            (var fs, var wave) = await CallVoiceVoxApi(
                talkScript,
                portNum,
                speaker,
                mainVoice.VoiceEffect.Volume.Value,
                mainVoice.VoiceEffect.Speed.Value,
                mainVoice.VoiceEffect.Pitch.Value,
                mainVoice.VoiceEffect.Emphasis.Value,
                masterEffect.EndPause + talkScript.EndSection.Pause.Span_ms,
                subVoice?.Library?.Settings?.GetSpeaker(),
                Settings.GetMorphRatio());

            setSamplingRate_Hz(fs);

            IsPlaying = false;
            return wave;
        }

        private async Task<(int, double[])> CallVoiceVoxApi(
            TalkScript talkScript,
            int portNum = 50021,
            int speaker = 0,
            double volume = 1,
            double speed = 1,
            double pitch = 0,
            double emphasis = 1,
            double endPauseMs = 100,
            int? subSpeaker = null,
            double morphRatio = 0.5)
        {
            var text = talkScript?.GetYomiForAquesTalkLike();
            text = text.Replace("_ン", "ン");

            if (string.IsNullOrWhiteSpace(text))
            {
                return (44100, new double[0]);
            }

            try
            {
                var baseUrl = $"http://127.0.0.1:{portNum}";
                var query = await client?.PostAsync($"{baseUrl}/accent_phrases?text={text}&speaker={speaker}&is_kana=true", null);


                var queryJson = await query.Content.ReadAsStringAsync();
                File.WriteAllText(Path.Combine(DllDirectory, "OriginalQuery.json"), LintJson(queryJson));
                var accentPhrases = JsonUtil.DeserializeFromString<List<AccentPhrase>>(queryJson);
                if (accentPhrases == null)
                {
                    return (44100, new double[0]);
                }

                {
                    for (int i = 0; i < Math.Min(talkScript.Sections.Count, accentPhrases.Count); i++)
                    {
                        var section1 = talkScript.Sections[i];
                        var section2 = accentPhrases[i];

                        var sectionPitch = section1.Pitch != null ? section1.Pitch.Value : 0.0;
                        var sectionSpeed = section1.Speed != null ? section1.Speed.Value : 1.0;

                        // イントネーション、子音長さ、母音長さの適用
                        for (int j = 0; j < Math.Min(section1.Moras.Count, section2.moras.Count); j++)
                        {
                            var mora1 = section1.Moras[j];
                            var mora2 = section2.moras[j];
                            mora2.ShiftPitch(mora1.GetPitch() + sectionPitch);
                            mora2.ShiftConsonantLength(mora1.GetConsonantLength());
                            mora2.GainConsonantLength(1.0 / sectionSpeed);
                            mora2.ShiftVowelLength(mora1.GetVowelLength());
                            mora2.GainVowelLength(1.0 / sectionSpeed);
                        }

                        // ポーズの設定
                        if (i > 0 && section1.Pause.Span_ms > 0)
                        {
                            // EndSectionのポーズはここには含まれない。EndSection のポーズは endPauseMs に含まれる。
                            accentPhrases[i - 1].pause_mora = new VoiceVoxMora(section1.Pause.Span_ms / 1000.0);
                        }
                    }

                }

                var voiceVoxQuery = new VoiceVoxQuery()
                {
                    accent_phrases = accentPhrases,
                    volumeScale = volume,
                    speedScale = speed,
                    pitchScale = pitch,
                    intonationScale = emphasis,
                    postPhonemeLength = Math.Max(endPauseMs / 1000, 0.1),
                };

                queryJson = JsonUtil.SerializeToString(voiceVoxQuery);
                File.WriteAllText(Path.Combine(DllDirectory, "ModifiedQuery.json"), LintJson(queryJson));
                var content = new StringContent(queryJson, Encoding.UTF8, "application/json");

                byte[] bytes;

                if(subSpeaker == null)
                {
                    var result = await client?.PostAsync($"{baseUrl}/synthesis?speaker={speaker}", content);
                    bytes = await result.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    // モーフィング
                    var result = await client?.PostAsync($"{baseUrl}/synthesis_morphing?base_speaker={speaker}&target_speaker={subSpeaker}&morph_rate={morphRatio}", content);
                    bytes = await result.Content.ReadAsByteArrayAsync();
                }

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
            catch (Exception)
            {
            }

            return (44100, new double[0]);
        }


        public static string LintJson(string jsonText)
        {
            try
            {
                var options1 = new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                };
                var x = JsonSerializer.Deserialize<object>(jsonText, options1);

                var options2 = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                };
                jsonText = JsonSerializer.Serialize(x, options2);

            }
            catch (Exception)
            {
            }
            return jsonText;
        }
    }
}
