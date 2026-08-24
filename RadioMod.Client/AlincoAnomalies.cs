using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// The Alinco's three quiet anomalies. They exist only where the airwaves are already dead — the
    /// Labyrinth and the Labs. Everywhere else the radio is an ordinary mediocre C+ set with no
    /// tricks at all; confining them to those maps makes them land far harder than spreading them
    /// across every raid would.
    ///
    /// None of them gives the player any advantage. No positions, no bot warnings, no information of
    /// any kind — the moment an anomaly becomes useful, the Alinco becomes mandatory and the other
    /// twelve radios stop mattering.
    ///
    /// There is no setting for any of this. An anomaly you can switch off is a feature; this is meant
    /// to be a property of the radio, like its range.
    /// </summary>
    public partial class Plugin
    {
        private float _anomalyNextWhisper;
        private float _anomalyIndicatorUntil;
        private float _anomalyNextIndicatorLie;
        private float _anomalyIndicatorStrength;

        /// <summary>Whisper interval. Rare on purpose: it should be doubted, not expected.</summary>
        private const float WhisperMinSeconds = 110f;
        private const float WhisperMaxSeconds = 260f;

        private const float IndicatorLieMinSeconds = 40f;
        private const float IndicatorLieMaxSeconds = 130f;

        /// <summary>True while the player is somewhere the Alinco misbehaves.</summary>
        private bool AlincoAnomalyActive =>
            _cachedIsInRaid && _cachedInAlincoAnomalyZone && _radioOn && LocalAlincoActive();

        /// <summary>
        /// Runs the anomalies. Called once per frame from the normal update path; everything is
        /// timer-driven so it costs nothing on the maps where it does not apply.
        /// </summary>
        private void UpdateAlincoAnomalies()
        {
            if (!AlincoAnomalyActive)
            {
                _anomalyNextWhisper = 0f;
                _anomalyNextIndicatorLie = 0f;
                _anomalyIndicatorUntil = 0f;
                return;
            }

            float now = Time.unscaledTime;

            if (_anomalyNextWhisper <= 0f)
            {
                _anomalyNextWhisper = now + Random.Range(WhisperMinSeconds, WhisperMaxSeconds);
                _anomalyNextIndicatorLie = now + Random.Range(IndicatorLieMinSeconds, IndicatorLieMaxSeconds);
                return;
            }

            // The whisper only happens while nobody is actually talking — it is meant to be mistaken
            // for a transmission, which cannot work if a real one is in progress.
            if (now >= _anomalyNextWhisper && RadioSpeakerNames.Count == 0 && _txChannel == null)
            {
                PlayWhisper();
                _anomalyNextWhisper = now + Random.Range(WhisperMinSeconds, WhisperMaxSeconds);
            }

            if (now >= _anomalyNextIndicatorLie)
            {
                _anomalyIndicatorUntil = now + Random.Range(0.4f, 1.6f);
                _anomalyIndicatorStrength = Random.Range(0.15f, 0.8f);
                _anomalyNextIndicatorLie = now + Random.Range(IndicatorLieMinSeconds, IndicatorLieMaxSeconds);
            }
        }

        /// <summary>
        /// Is the signal meter currently lying? Deliberately independent of the whisper: if the
        /// needle twitched exactly when something was heard it would be an honest readout of the
        /// whisper, and would stop being an anomaly at all.
        /// </summary>
        private bool AnomalyIndicatorLying(out float strength)
        {
            strength = _anomalyIndicatorStrength;
            return AlincoAnomalyActive && Time.unscaledTime < _anomalyIndicatorUntil;
        }

        /// <summary>
        /// Corrupts a nameplate string. Used only while an anomaly is active, and only on the
        /// chassis plate — never on anything the player needs in order to act.
        /// </summary>
        private static string CorruptText(string text, float amount)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            const string glitch = "▓▒░#=-";
            char[] chars = text.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != ' ' && Random.value < amount)
                {
                    chars[i] = glitch[Random.Range(0, glitch.Length)];
                }
            }

            return new string(chars);
        }

        /// <summary>
        /// A short, low, detuned pair of tones under a swell of filtered noise. It is played through
        /// the preview audio source, not through <see cref="RadioVoiceFilter"/>, which matters:
        ///
        /// - it never reaches the raid recorder, so no clip appears on disk without a source;
        /// - it raises no notification and adds nobody to <see cref="RadioSpeakerNames"/>;
        /// - it does not reset the indicator idle timer.
        ///
        /// In other words it is a local sound, not an incoming transmission, and every part of the
        /// mod that reacts to traffic stays unaware of it.
        /// </summary>
        private void PlayWhisper()
        {
            const int rate = 48000;
            const float seconds = 1.1f;
            int n = Mathf.CeilToInt(seconds * rate);
            float[] buf = new float[n];
            System.Random rng = new System.Random(Random.Range(1, 99999));

            float p1 = 0f;
            float p2 = 0f;
            float lp = 0f;
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * 700f / rate);

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;

                // Slow swell in and out: nothing here should have a hard edge.
                float env = Mathf.Sin(t * Mathf.PI);

                p1 += Mathf.Lerp(190f, 172f, t) / rate;
                p2 += Mathf.Lerp(287f, 301f, t) / rate;
                if (p1 > 1f) { p1 -= 1f; }
                if (p2 > 1f) { p2 -= 1f; }

                float tone = Mathf.Sin(p1 * 2f * Mathf.PI) * 0.5f + Mathf.Sin(p2 * 2f * Mathf.PI) * 0.35f;

                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                lp += (white - lp) * a;

                buf[i] = (tone * 0.35f + lp * 0.5f) * env * 0.5f;
            }

            PlayPreview(new WavData { Samples = buf, Channels = 1, SampleRate = rate },
                "alinco_whisper", Mathf.Clamp01(_receiveVolume.Value * 0.22f));

            LogVerbose("PRT: Alinco anomaly — whisper");
        }
    }
}
