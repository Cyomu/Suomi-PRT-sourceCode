using System;
using System.Collections.Generic;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Per-radio start/end cues, synthesised instead of recorded.
    ///
    /// Thirteen radios × four events is fifty-two audio files. As parameters it is thirteen rows:
    /// the signalling family gives the character (an analogue roger beep, a DMR chirp, a TETRA
    /// double pip, a P25 low pair, a military sync burst), and the radio's own passband detunes it
    /// so that two DMR sets are still told apart.
    ///
    /// Two things were learned from the working prototype and are load-bearing here:
    ///
    /// 1. Every transmit cue opens with a physical PTT click — a broadband snap plus a short body
    ///    thump. A thumb on a rubber key is what actually starts a transmission.
    /// 2. The cues must be shaped by the radio's own passband. In the prototype the tones initially
    ///    bypassed it and the result sounded like a games console, not a radio. That is why
    ///    everything here runs through <see cref="Shape"/> before it is handed back.
    ///
    /// The recorded sets are untouched and remain the default: this is an option, not a replacement.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>
        /// Four presets for the start/end cues. The three synthesised ones are not cosmetic variants
        /// of each other — each derives the cue from a different real property of the radio, so they
        /// disagree in ways a player can hear and reason about.
        /// </summary>
        internal enum SoundStyle
        {
            /// <summary>Four recorded sets shared between thirteen radios. Behaviour up to 1.0.0.</summary>
            Classic,

            /// <summary>By signalling standard: what the radio speaks. Analogue beep, DMR chirp, TETRA pips.</summary>
            PerRadio,

            /// <summary>By era: an 1980s set clunks, a 2010s set chirps. Driven by the year in the specs.</summary>
            PerRadioAlt,

            /// <summary>By build: a heavy high-power set keys deep and loud, a light consumer one thin.</summary>
            PerRadioAlt2,
        }

        private BepInEx.Configuration.ConfigEntry<SoundStyle> _soundStyle;

        private const int SynthRate = 48000;

        private readonly Dictionary<string, RadioSoundSet> _synthSoundSets = new Dictionary<string, RadioSoundSet>();

        /// <summary>One element of a cue: a tone, a noise burst, or the mechanical click.</summary>
        private struct Cue
        {
            public char Kind;      // 't' tone, 'n' noise, 'c' click
            public float Freq;     // tone: start frequency
            public float FreqTo;   // tone: sweep target, 0 = steady
            public float Dur;
            public float Gain;
            public float Body;     // click: frequency of the housing thump
        }

        private static Cue Tone(float f, float dur, float to = 0f, float gain = 0.22f)
            => new Cue { Kind = 't', Freq = f, FreqTo = to, Dur = dur, Gain = gain };

        private static Cue Noise(float dur, float gain)
            => new Cue { Kind = 'n', Dur = dur, Gain = gain };

        private static Cue Click(float gain, float body)
            => new Cue { Kind = 'c', Dur = 0.035f, Gain = gain, Body = body };

        /// <summary>
        /// Cue tables by family. TX events start with a click because you press your own key; RX
        /// events do not, because you never hear the other operator's thumb — you hear their squelch.
        /// </summary>
        private static Cue[] CueSet(SignalFamily family, bool local, bool start)
        {
            switch (family)
            {
                case SignalFamily.Digital:
                    if (local && start) { return new[] { Click(0.4f, 180f), Tone(1900f, 0.035f, 2400f) }; }
                    if (local) { return new[] { Click(0.32f, 165f), Tone(1600f, 0.05f, 2100f) }; }
                    if (start) { return new[] { Tone(2100f, 0.028f), Tone(1700f, 0.028f) }; }
                    return new[] { Tone(1500f, 0.045f, 1150f) };

                case SignalFamily.Military:
                    if (local && start) { return new[] { Click(0.68f, 205f), Noise(0.035f, 0.2f), Tone(1200f, 0.03f) }; }
                    if (local) { return new[] { Click(0.54f, 190f), Tone(1000f, 0.05f), Tone(1000f, 0.05f) }; }
                    if (start) { return new[] { Tone(1400f, 0.02f), Noise(0.05f, 0.2f), Tone(1000f, 0.03f) }; }
                    return new[] { Tone(1000f, 0.04f), Tone(760f, 0.06f) };

                case SignalFamily.Cb:
                    // AM: no tones at all, just the key and a bad-tempered squelch.
                    if (local && start) { return new[] { Click(0.78f, 110f), Noise(0.03f, 0.34f) }; }
                    if (local) { return new[] { Click(0.62f, 95f), Noise(0.06f, 0.28f) }; }
                    if (start) { return new[] { Noise(0.11f, 0.46f) }; }
                    return new[] { Noise(0.16f, 0.34f) };

                default:
                    if (local && start) { return new[] { Click(0.55f, 150f), Noise(0.02f, 0.22f) }; }
                    if (local) { return new[] { Click(0.44f, 122f), Tone(1750f, 0.09f), Noise(0.03f, 0.14f) }; }
                    if (start) { return new[] { Noise(0.07f, 0.34f) }; }
                    return new[] { Noise(0.10f, 0.26f) };
            }
        }

        /// <summary>
        /// Stable per-radio detune in Hz, derived from the radio's own high-pass corner. Two sets in
        /// the same family stay recognisably that family while never sounding identical.
        /// </summary>
        private static float CueDetune(RadioProfile p) => (p.HpCutoffHz - 250f) * 0.55f;

        /// <summary>
        /// Era preset. What a radio sounds like when you key it changed over three decades: an
        /// eighties set is a mechanical switch and a squelch crash with no tone at all, a nineties
        /// service radio adds a plain beep, a modern one answers with a short electronic chirp.
        /// Driven by the year from the verified specs, so the ordering is the real one.
        /// </summary>
        private static Cue[] CueSetByEra(string tplId, bool local, bool start)
        {
            float era = SpecEra(tplId);

            if (era < 0.35f)
            {
                // Pre-1993: mechanical. Heavy key, no electronics answering back.
                if (local && start) { return new[] { Click(0.8f, 105f), Noise(0.035f, 0.3f) }; }
                if (local) { return new[] { Click(0.66f, 92f), Noise(0.07f, 0.26f) }; }
                if (start) { return new[] { Noise(0.12f, 0.44f) }; }
                return new[] { Noise(0.17f, 0.32f) };
            }

            if (era < 0.7f)
            {
                // Mid era: a plain single beep, the sound most people picture as "a radio".
                if (local && start) { return new[] { Click(0.55f, 140f), Tone(1200f, 0.03f) }; }
                if (local) { return new[] { Click(0.45f, 125f), Tone(1750f, 0.08f) }; }
                if (start) { return new[] { Noise(0.06f, 0.3f), Tone(1400f, 0.02f) }; }
                return new[] { Tone(1100f, 0.05f), Noise(0.05f, 0.18f) };
            }

            // Modern: short, clean, electronic. The key barely makes a sound.
            if (local && start) { return new[] { Click(0.3f, 190f), Tone(2000f, 0.03f, 2500f) }; }
            if (local) { return new[] { Click(0.26f, 175f), Tone(1700f, 0.04f, 2200f) }; }
            if (start) { return new[] { Tone(2200f, 0.022f), Tone(1800f, 0.022f) }; }
            return new[] { Tone(1600f, 0.035f, 1250f) };
        }

        /// <summary>
        /// Build preset. A 1.2 kg military handheld and a 155 g belt radio do not key alike: mass
        /// decides how deep and solid the switch sounds, transmit power decides how much the set
        /// announces itself. Both come from the verified specs.
        /// </summary>
        private static Cue[] CueSetByBuild(string tplId, bool local, bool start)
        {
            float bulk = SpecBulk(tplId);
            float watts = SpecPowerWatts(tplId);

            // Heavier housing rings lower; a light plastic shell clicks high and thin.
            float body = Mathf.Lerp(230f, 88f, bulk);
            float clickGain = Mathf.Lerp(0.3f, 0.85f, bulk);

            // More power, more confident answer-back tone.
            float toneGain = Mathf.Clamp(0.12f + watts * 0.03f, 0.12f, 0.3f);
            float toneFreq = Mathf.Lerp(1500f, 900f, Mathf.Clamp01(watts / 6f));

            if (local && start) { return new[] { Click(clickGain, body), Noise(0.025f, 0.18f + bulk * 0.2f) }; }
            if (local) { return new[] { Click(clickGain * 0.82f, body * 0.9f), Tone(toneFreq, 0.07f, 0f, toneGain) }; }
            if (start) { return new[] { Noise(0.05f + bulk * 0.06f, 0.3f) }; }
            return new[] { Tone(toneFreq * 0.8f, 0.05f, 0f, toneGain * 0.8f), Noise(0.07f, 0.2f) };
        }

        /// <summary>Picks the cue table for the preset in force.</summary>
        private Cue[] CueSetFor(string tplId, bool local, bool start)
        {
            switch (_soundStyle != null ? _soundStyle.Value : SoundStyle.Classic)
            {
                case SoundStyle.PerRadioAlt:
                    return CueSetByEra(tplId, local, start);
                case SoundStyle.PerRadioAlt2:
                    return CueSetByBuild(tplId, local, start);
                default:
                    return CueSet(GetSignalFamily(tplId), local, start);
            }
        }


        private WavData Render(Cue[] cues, RadioProfile profile)
        {
            float detune = CueDetune(profile);

            float total = 0.06f;
            foreach (Cue c in cues) { total += c.Dur + 0.012f; }

            int length = Mathf.CeilToInt(total * SynthRate);
            float[] buf = new float[length];
            System.Random rng = new System.Random(Mathf.RoundToInt(profile.HpCutoffHz) * 7919);

            int cursor = 0;
            foreach (Cue c in cues)
            {
                int n = Mathf.CeilToInt(c.Dur * SynthRate);

                switch (c.Kind)
                {
                    case 't':
                        RenderTone(buf, cursor, n, c, detune);
                        break;

                    case 'n':
                        RenderNoise(buf, cursor, n, c.Gain, rng);
                        break;

                    default:
                        RenderClick(buf, cursor, c, rng);
                        break;
                }

                cursor += n + Mathf.CeilToInt(0.012f * SynthRate);
                if (cursor >= length) { break; }
            }

            Shape(buf, profile);
            Normalise(buf, 0.85f);

            return new WavData { Samples = buf, Channels = 1, SampleRate = SynthRate };
        }

        private static void RenderTone(float[] buf, int at, int n, Cue c, float detune)
        {
            float phase = 0f;
            for (int i = 0; i < n && at + i < buf.Length; i++)
            {
                float t = i / (float)n;
                float f = Mathf.Max(90f, (c.FreqTo > 0f ? Mathf.Lerp(c.Freq, c.FreqTo, t) : c.Freq) + detune);
                phase += f / SynthRate;
                if (phase > 1f) { phase -= 1f; }

                // Triangle rather than square: a square wave through a small speaker is exactly the
                // chiptune sound the prototype started with.
                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;

                // Soft attack, exponential tail — a hard edge clicks on every tone boundary.
                float env = t < 0.15f ? t / 0.15f : Mathf.Exp(-4f * (t - 0.15f));
                buf[at + i] += tri * env * c.Gain;
            }
        }

        private static void RenderNoise(float[] buf, int at, int n, float gain, System.Random rng)
        {
            for (int i = 0; i < n && at + i < buf.Length; i++)
            {
                float env = Mathf.Exp(-5f * (i / (float)n));
                buf[at + i] += (float)(rng.NextDouble() * 2.0 - 1.0) * env * gain;
            }
        }

        /// <summary>Broadband snap plus a damped low oscillation for the body of the housing.</summary>
        private static void RenderClick(float[] buf, int at, Cue c, System.Random rng)
        {
            int snap = Mathf.CeilToInt(0.006f * SynthRate);
            for (int i = 0; i < snap && at + i < buf.Length; i++)
            {
                float env = Mathf.Exp(-40f * (i / (float)snap));
                buf[at + i] += (float)(rng.NextDouble() * 2.0 - 1.0) * env * c.Gain;
            }

            int bodyLen = Mathf.CeilToInt(0.03f * SynthRate);
            float phase = 0f;
            for (int i = 0; i < bodyLen && at + i < buf.Length; i++)
            {
                float t = i / (float)bodyLen;
                float f = Mathf.Lerp(c.Body, c.Body * 0.55f, t);
                phase += f / SynthRate;
                if (phase > 1f) { phase -= 1f; }

                float tri = 4f * Mathf.Abs(phase - 0.5f) - 1f;
                buf[at + i] += tri * Mathf.Exp(-9f * t) * c.Gain * 0.5f;
            }
        }

        /// <summary>
        /// Runs the cue through the radio's own passband. This is the step that makes the result
        /// sound like it came out of the set rather than out of a synthesiser.
        /// </summary>
        private static void Shape(float[] buf, RadioProfile profile)
        {
            float lpCut = Mathf.Clamp(profile.LpCutoffNear, 800f, 8000f);
            float hpCut = Mathf.Clamp(profile.HpCutoffHz, 80f, 800f);

            float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * lpCut / SynthRate);
            float hpA = 1f - Mathf.Exp(-2f * Mathf.PI * hpCut / SynthRate);

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

            // Mild saturation: a small speaker never reproduces a transient cleanly.
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (float)Math.Tanh(buf[i] * 1.7);
            }
        }

        private static void Normalise(float[] buf, float peak)
        {
            float max = 0f;
            for (int i = 0; i < buf.Length; i++)
            {
                float a = Mathf.Abs(buf[i]);
                if (a > max) { max = a; }
            }

            if (max <= 0.0001f)
            {
                return;
            }

            float k = peak / max;
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] *= k;
            }
        }

        /// <summary>
        /// Synthesised set for one radio, built once and cached — rendering four cues is cheap but
        /// not free, and it must never happen while a transmission is starting.
        /// </summary>
        private RadioSoundSet GetSynthSoundSet(string tplId)
        {
            // Keyed by preset as well as radio: without this, switching presets would keep serving
            // whichever cues happened to be rendered first.
            string key = tplId + "#" + (_soundStyle != null ? _soundStyle.Value.ToString() : "Classic");
            if (_synthSoundSets.TryGetValue(key, out RadioSoundSet cached))
            {
                return cached;
            }

            RadioProfile profile = RadioProfiles.TryGetValue(tplId, out RadioProfile p) ? p : BaofengProfile;

            RadioSoundSet set = new RadioSoundSet
            {
                LocalStart = Render(CueSetFor(tplId, true, true), profile),
                LocalEnd = Render(CueSetFor(tplId, true, false), profile),
                RemoteStart = Render(CueSetFor(tplId, false, true), profile),
                RemoteEnd = Render(CueSetFor(tplId, false, false), profile),
            };

            _synthSoundSets[key] = set;
            LogVerbose("PRT: synthesised cue set for " + GetRadioDisplayName(tplId) + " (" + key + ")");
            return set;
        }
    }
}
