using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Audition buttons for the settings tab.
    ///
    /// Before this, the only way to judge receive volume, noise level or the transmission cues was
    /// to load into a raid and wait for somebody to talk. Everything here is generated on the spot
    /// from the same tables the raid uses, so what is heard in the menu is what happens in a raid.
    ///
    /// Nothing here plays through <see cref="RadioVoiceFilter"/>: that filter is attached to a live
    /// Dissonance playback object and touching it outside a raid risks the exact state bug the
    /// recordings feature already hit once. The preview instead renders the same profile numbers
    /// offline, which is the safe way to demonstrate them.
    /// </summary>
    public partial class Plugin
    {
        private const int PreviewRate = 48000;

        /// <summary>
        /// Voice-like carrier shaped by a radio profile: syllable-rate amplitude on a buzzy source,
        /// through the radio's passband, with its own noise floor and dropouts mixed in.
        /// A stand-in for speech, not a recording of it — enough to set a level by.
        /// </summary>
        private WavData RenderVoicePreview(RadioProfile p, float quality, bool voice, bool noise)
        {
            const float seconds = 1.4f;
            int n = Mathf.CeilToInt(seconds * PreviewRate);
            float[] buf = new float[n];
            System.Random rng = new System.Random(1337);

            float lpNow = Mathf.Lerp(700f, p.LpCutoffNear, Mathf.Clamp01(quality));
            float voiceGain = Mathf.Lerp(p.VoiceGainFar, p.VoiceGainNear, Mathf.Clamp01(quality));
            float noiseAmp = Mathf.Lerp(p.NoiseAmpFar, p.NoiseAmpNear, Mathf.Clamp01(quality));
            float dropChance = Mathf.Lerp(p.DropoutChanceFar, p.DropoutChanceNear, Mathf.Clamp01(quality));

            if (voice)
            {
                float phase = 0f;
                float sylEnd = 0f;
                float sylPeak = 0f;
                float sylStart = 0f;
                bool muted = false;

                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)PreviewRate;

                    if (t >= sylEnd)
                    {
                        sylStart = t;
                        sylEnd = t + 0.12f + (float)rng.NextDouble() * 0.16f;
                        sylPeak = 0.25f + (float)rng.NextDouble() * 0.35f;
                        muted = rng.NextDouble() < dropChance;
                    }

                    float sylT = Mathf.Clamp01((t - sylStart) / Mathf.Max(0.001f, sylEnd - sylStart));
                    float env = muted ? 0f : Mathf.Sin(sylT * Mathf.PI) * sylPeak;

                    phase += 118f / PreviewRate;
                    if (phase > 1f) { phase -= 1f; }
                    float saw = phase * 2f - 1f;

                    buf[i] += saw * env * voiceGain;
                }
            }

            if (noise)
            {
                float lp = 0f;
                float a = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Max(300f, p.NoiseLpCutoffHz) / PreviewRate);
                for (int i = 0; i < n; i++)
                {
                    float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                    lp += (white - lp) * a;
                    buf[i] += lp * noiseAmp * 6f;
                }
            }

            ShapeBand(buf, p.HpCutoffHz, lpNow);
            return new WavData { Samples = buf, Channels = 1, SampleRate = PreviewRate };
        }

        /// <summary>One-pole band shaping, matching what the cue synthesiser does.</summary>
        private static void ShapeBand(float[] buf, float hpCut, float lpCut)
        {
            float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Clamp(lpCut, 400f, 8000f) / PreviewRate);
            float hpA = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Clamp(hpCut, 60f, 800f) / PreviewRate);

            float lp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                lp += (buf[i] - lp) * lpA;
                buf[i] = lp;
            }

            float hp = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                hp += (buf[i] - hp) * hpA;
                buf[i] -= hp;
            }

            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = Mathf.Clamp(buf[i], -1f, 1f);
            }
        }

        /// <summary>Profile of the radio the player is actually carrying, or a sensible stand-in.</summary>
        private RadioProfile PreviewProfile()
        {
            string tpl = _selectedRadioTplId ?? _activeRadioTplId;
            if (tpl != null && RadioProfiles.TryGetValue(tpl, out RadioProfile p))
            {
                return ApplyInterferenceCharacter(tpl, p);
            }

            return BaofengProfile;
        }

        private string PreviewTpl()
        {
            string tpl = _selectedRadioTplId ?? _activeRadioTplId;
            return tpl != null && RadioProfiles.ContainsKey(tpl) ? tpl : BaofengTplId;
        }

        /// <summary>Plays a preview at a chosen volume, bypassing the usual sound-volume setting.</summary>
        private void PlayPreview(WavData wav, string name, float volume)
        {
            AudioClip clip = BuildClip(wav, name);
            if (clip == null)
            {
                return;
            }

            AudioSource source = EnsureAudioSource(ref _previewAudioSource, "RadioMod_PreviewAudio");
            source.volume = Mathf.Clamp(volume, 0f, 1f);
            source.clip = clip;
            source.Play();
        }

        private AudioSource _previewAudioSource;

        /// <summary>
        /// Small audition button placed next to a setting. Deliberately narrow: it sits inside the
        /// row it belongs to, so there is no doubt which value it is demonstrating.
        /// </summary>
        private bool DrawAuditionButton()
        {
            return GUILayout.Button("▶", MilStyle.GlyphButton, GUILayout.Width(24f), GUILayout.Height(20f));
        }

        private void AuditionReceive()
        {
            // Receive volume can be pushed above 1 to lift quiet voices, so it is clamped for the
            // preview rather than blasting the player at 5x.
            PlayPreview(RenderVoicePreview(PreviewProfile(), 0.75f, voice: true, noise: true),
                "preview_receive", Mathf.Min(1f, _receiveVolume.Value * 0.35f));
        }

        private void AuditionNoise()
        {
            PlayPreview(RenderVoicePreview(PreviewProfile(), 0.25f, voice: false, noise: true),
                "preview_noise", _noiseVolume.Value);
        }

        private void AuditionCue()
        {
            // Out of a raid there is no active radio, so GetActiveSoundSet would fall back to the
            // shared default and every radio would demo identically. The selected one is used
            // instead, which is what the player is actually looking at.
            string tpl = PreviewTpl();

            RadioSoundSet set;
            if (_soundStyle != null && _soundStyle.Value != SoundStyle.Classic)
            {
                set = GetSynthSoundSet(tpl);
            }
            else if (!_radioSoundSets.TryGetValue(tpl, out set))
            {
                set = _defaultSoundSet;
            }

            PlayPreview(set.LocalEnd, "preview_cue", _soundVolume.Value);
        }

        /// <summary>Full demo of one radio at a chosen distance, for the RADIOS tab.</summary>
        private void AuditionRadio(string tpl, float distanceFraction)
        {
            RadioProfile p = RadioProfiles.TryGetValue(tpl, out RadioProfile found) ? found : BaofengProfile;
            p = ApplyInterferenceCharacter(tpl, p);

            float quality = Mathf.Clamp01(1f - distanceFraction);
            PlayPreview(RenderVoicePreview(p, quality, voice: true, noise: true),
                "preview_radio", Mathf.Min(1f, _receiveVolume.Value * 0.35f));
        }
    }
}
