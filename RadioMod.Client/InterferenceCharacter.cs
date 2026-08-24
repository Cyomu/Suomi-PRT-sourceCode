using System.Collections.Generic;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// How a radio falls apart at the edge of its range.
    ///
    /// The old behaviour gives every one of the thirteen the same curve: hiss that grows with
    /// distance and random punch-outs. That is how an analogue set behaves — and only an analogue
    /// set. A DMR or TETRA terminal has no hiss at all: it holds a clean channel right up to the
    /// point where the link dies, and then loses whole words at once.
    ///
    /// This is implemented by transforming the profile numbers before they reach
    /// <see cref="RadioVoiceFilter"/>, deliberately **not** by editing the DSP. The filter is the
    /// core of the mod and it stays untouched, which also means the old character is recovered
    /// exactly by not applying the transform.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>
        /// Four ways a radio can lose its signal. Like the cue presets, each of the three new ones
        /// keys off a different real property of the set, so they are genuinely different behaviours
        /// rather than three tunings of the same one.
        /// </summary>
        internal enum InterferenceCharacter
        {
            /// <summary>One hiss curve for all thirteen. The behaviour shipped up to 1.0.0.</summary>
            Classic,

            /// <summary>Degradation follows the radio's signalling family: digital cliff, analogue fade.</summary>
            PerFamily,

            /// <summary>By band: UHF gives out abruptly, VHF holds on and gets noisy.</summary>
            PerBand,

            /// <summary>By transmit power: watts decide how far the link stays intelligible.</summary>
            PerPower,
        }

        /// <summary>Signalling family, which decides how the link degrades.</summary>
        internal enum SignalFamily
        {
            Analog,
            Cb,
            Digital,
            Military,
        }

        private BepInEx.Configuration.ConfigEntry<InterferenceCharacter> _interferenceCharacter;

        /// <summary>
        /// Family per radio. Assigned from what the real sets are: MOTOTRBO is DMR, the MTH800 is a
        /// TETRA terminal, the XTS5000 runs P25 — all digital. The TRC-83 is a CB set and gets its
        /// own entry because AM behaves differently again.
        /// </summary>
        private static readonly Dictionary<string, SignalFamily> RadioFamilies = new Dictionary<string, SignalFamily>
        {
            { "6d6f645f726164696f303033", SignalFamily.Analog },   // Kenwood TH-21BT
            { "6d6f645f726164696f303130", SignalFamily.Cb },       // Realistic TRC-83
            { "6d6f645f726164696f303031", SignalFamily.Analog },   // Baofeng UV-5R
            { "6d6f645f726164696f303131", SignalFamily.Analog },   // Alinco
            { "6d6f645f726164696f303132", SignalFamily.Analog },   // Kenwood ProTalk XLS
            { "6d6f645f726164696f303034", SignalFamily.Analog },   // Motorola T460
            { "6d6f645f726164696f303035", SignalFamily.Analog },   // Yaesu VX-8DR
            { "6d6f645f726164696f303133", SignalFamily.Digital },  // Motorola MTH800 (TETRA)
            { "6d6f645f726164696f303037", SignalFamily.Digital },  // Motorola DP4601e (DMR)
            { "6d6f645f726164696f303036", SignalFamily.Digital },  // Motorola DP4800 (DMR)
            { "6d6f645f726164696f303038", SignalFamily.Digital },  // Motorola XTS5000 (P25)
            { "6d6f645f726164696f303032", SignalFamily.Military }, // Р-187П1 «Азарт»
            { "6d6f645f726164696f303039", SignalFamily.Military }, // Harris AN/PRC-152
        };

        internal static SignalFamily GetSignalFamily(string tplId)
        {
            return tplId != null && RadioFamilies.TryGetValue(tplId, out SignalFamily family)
                ? family
                : SignalFamily.Analog;
        }

        /// <summary>
        /// Reshapes a profile according to its family. Returns the profile unchanged when the player
        /// has kept the classic character, so the old sound is bit-for-bit recoverable.
        /// </summary>
        private RadioProfile ApplyInterferenceCharacter(string tplId, RadioProfile p)
        {
            if (_interferenceCharacter == null || _interferenceCharacter.Value == InterferenceCharacter.Classic)
            {
                return p;
            }

            if (_interferenceCharacter.Value == InterferenceCharacter.PerBand)
            {
                return ApplyBandCharacter(tplId, p);
            }

            if (_interferenceCharacter.Value == InterferenceCharacter.PerPower)
            {
                return ApplyPowerCharacter(tplId, p);
            }

            switch (GetSignalFamily(tplId))
            {
                case SignalFamily.Digital:
                    // No hiss to speak of, and the noise that does exist is dull rather than bright.
                    p.NoiseAmpNear *= 0.15f;
                    p.NoiseAmpFar *= 0.25f;
                    p.NoiseLpCutoffHz = Mathf.Min(p.NoiseLpCutoffHz, 900f);

                    // The channel holds its quality far longer, then collapses: dropouts near the
                    // edge are what carries the loss instead of a slow slide into noise.
                    p.DropoutChanceNear *= 0.3f;
                    p.DropoutChanceFar = Mathf.Min(0.85f, p.DropoutChanceFar * 2.4f);

                    // A vocoder does not lose treble gradually, so the far passband stays wide.
                    p.LpCutoffFar = Mathf.Max(p.LpCutoffFar, p.LpCutoffNear * 0.78f);
                    p.VoiceGainFar = Mathf.Max(p.VoiceGainFar, p.VoiceGainNear * 0.9f);

                    // Beyond the link there is nothing to hear at all — digital silence, not static.
                    p.StaticVoiceGain *= 0.25f;
                    p.StaticNoiseAmp *= 0.35f;
                    p.StaticDropoutChance = Mathf.Min(0.95f, p.StaticDropoutChance * 1.6f);
                    break;

                case SignalFamily.Military:
                    // Low noise floor, brief sync stumbles rather than fading.
                    p.NoiseAmpNear *= 0.55f;
                    p.NoiseAmpFar *= 0.6f;
                    p.DropoutChanceFar *= 0.8f;
                    p.VoiceGainFar = Mathf.Max(p.VoiceGainFar, p.VoiceGainNear * 0.8f);
                    p.StaticNoiseAmp *= 0.7f;
                    break;

                case SignalFamily.Cb:
                    // AM at its worst: loud, bright hiss and a voice that sinks into it early.
                    p.NoiseAmpNear *= 1.5f;
                    p.NoiseAmpFar *= 1.7f;
                    p.NoiseLpCutoffHz = Mathf.Max(p.NoiseLpCutoffHz, 2200f);
                    p.VoiceGainFar *= 0.8f;
                    p.DropoutChanceFar *= 0.85f;
                    break;

                default:
                    // Analogue: more hiss with distance, but fewer hard cuts — the link fades out
                    // rather than switching off, which is the whole difference from digital.
                    p.NoiseAmpFar *= 1.35f;
                    p.DropoutChanceNear *= 0.7f;
                    p.DropoutChanceFar *= 0.75f;
                    p.VoiceGainFar *= 0.88f;
                    break;
            }

            return p;
        }

        /// <summary>
        /// Band preset. Higher frequencies are stopped by structures rather than attenuated by them,
        /// so a UHF set stays clean and then gives out over a short stretch, while a VHF set keeps
        /// carrying a degraded but usable signal much further. Uses the top band figure from the
        /// verified specs.
        /// </summary>
        private static RadioProfile ApplyBandCharacter(string tplId, RadioProfile p)
        {
            float mhz = SpecTopBandMhz(tplId);
            float uhf = Mathf.InverseLerp(150f, 800f, mhz);

            // The higher the band, the narrower the window between clean and gone.
            p.ClearRangeMeters = Mathf.Lerp(p.ClearRangeMeters, p.NoiseOnlyRangeMeters * 0.88f, uhf * 0.6f);

            // VHF pays for its reach with a louder noise floor; UHF stays quiet until it stops.
            p.NoiseAmpFar *= Mathf.Lerp(1.6f, 0.55f, uhf);
            p.NoiseAmpNear *= Mathf.Lerp(1.3f, 0.7f, uhf);

            p.DropoutChanceFar *= Mathf.Lerp(0.7f, 1.7f, uhf);
            p.VoiceGainFar *= Mathf.Lerp(0.8f, 1.05f, uhf);

            return p;
        }

        /// <summary>
        /// Power preset. Watts buy intelligibility at distance and nothing else: a five-watt set
        /// stays understandable where a one-watt set is already mush, but neither changes how the
        /// noise itself sounds. The plainest of the three, and the easiest to reason about in a raid.
        /// </summary>
        private static RadioProfile ApplyPowerCharacter(string tplId, RadioProfile p)
        {
            // 1 W (ProTalk, MTH800) to 6 W (XTS5000) across the set.
            float strength = Mathf.InverseLerp(1f, 6f, SpecPowerWatts(tplId));

            p.ZeroNoiseRangeMeters *= Mathf.Lerp(0.8f, 1.25f, strength);
            p.ClearRangeMeters *= Mathf.Lerp(0.82f, 1.2f, strength);

            p.VoiceGainFar *= Mathf.Lerp(0.72f, 1.15f, strength);
            p.NoiseAmpFar *= Mathf.Lerp(1.5f, 0.7f, strength);
            p.DropoutChanceFar *= Mathf.Lerp(1.45f, 0.65f, strength);

            return p;
        }

        /// <summary>
        /// The radio whose voice is being heard. The transmitting set defines carrier and drive, so
        /// its family is the one that decides how the degradation sounds.
        /// </summary>
        private string ResolveRemoteRadioTpl(string remoteProfileId)
        {
            Fika.Core.Main.Players.FikaPlayer fp = GetFikaPlayerByProfileId(remoteProfileId);
            EFT.InventoryLogic.InventoryEquipment eq = fp?.Inventory?.Equipment;
            if (eq == null)
            {
                return _activeRadioTplId ?? _selectedRadioTplId;
            }

            foreach (string tplId in CollectSelectableRadioTpls(eq))
            {
                if (RadioProfiles.ContainsKey(tplId))
                {
                    return tplId;
                }
            }

            return _activeRadioTplId ?? _selectedRadioTplId;
        }
    }
}
