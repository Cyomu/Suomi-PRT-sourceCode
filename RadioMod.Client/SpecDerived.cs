using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Numbers pulled out of the verified specification strings so the sound presets can be driven
    /// by what the radios actually are, rather than by a hand-written table per preset.
    ///
    /// The specs are stored as human-readable text on purpose — "5 W / 1 W", "1.2 kg", "48 / 1000" —
    /// because that is how a datasheet reads and how the user checked them. Parsing them here keeps
    /// one source of truth instead of a second, machine-shaped copy that could silently disagree.
    ///
    /// Every accessor has a defined fallback, so a radio with an unknown or oddly worded field never
    /// breaks a preset — it simply lands in the middle of the range.
    /// </summary>
    public partial class Plugin
    {
        private static readonly Regex FirstNumber = new Regex(@"(\d+(?:[.,]\d+)?)", RegexOptions.Compiled);

        private static float ParseFirstNumber(string text, float fallback)
        {
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }

            Match m = FirstNumber.Match(text);
            if (!m.Success)
            {
                return fallback;
            }

            string raw = m.Groups[1].Value.Replace(',', '.');
            return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                ? value
                : fallback;
        }

        /// <summary>Year of the model, 2005 when unknown — the middle of the set's spread.</summary>
        private static int SpecYear(string tplId)
        {
            return TryGetRadioSpec(tplId, out RadioSpec s)
                ? Mathf.RoundToInt(ParseFirstNumber(s.Year, 2005f))
                : 2005;
        }

        /// <summary>
        /// Mass in grams. Handles both "290 g" and "1.2 kg"; the Harris is the only kilogram entry
        /// and it is exactly the one that should sound heaviest, so the unit matters.
        /// </summary>
        private static float SpecWeightGrams(string tplId)
        {
            if (!TryGetRadioSpec(tplId, out RadioSpec s) || string.IsNullOrEmpty(s.Weight))
            {
                return 300f;
            }

            float value = ParseFirstNumber(s.Weight, 300f);
            return s.Weight.IndexOf("kg", System.StringComparison.OrdinalIgnoreCase) >= 0 ? value * 1000f : value;
        }

        /// <summary>
        /// Peak transmit power in watts. The strings list the high figure first ("5 W / 1 W",
        /// "0.25–5 W" being the exception), so the largest number in the string is taken.
        /// </summary>
        private static float SpecPowerWatts(string tplId)
        {
            if (!TryGetRadioSpec(tplId, out RadioSpec s) || string.IsNullOrEmpty(s.Power))
            {
                return 3f;
            }

            float best = 0f;
            foreach (Match m in FirstNumber.Matches(s.Power))
            {
                string raw = m.Groups[1].Value.Replace(',', '.');
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > best)
                {
                    best = v;
                }
            }

            return best > 0f ? best : 3f;
        }

        /// <summary>
        /// Highest frequency the set works on, in MHz. UHF and above carries less well through
        /// structures than VHF, which is what the band-driven interference preset trades on.
        /// </summary>
        private static float SpecTopBandMhz(string tplId)
        {
            if (!TryGetRadioSpec(tplId, out RadioSpec s) || string.IsNullOrEmpty(s.Band))
            {
                return 400f;
            }

            float best = 0f;
            foreach (Match m in FirstNumber.Matches(s.Band))
            {
                string raw = m.Groups[1].Value.Replace(',', '.');
                if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) && v > best)
                {
                    best = v;
                }
            }

            return best > 0f ? best : 400f;
        }

        /// <summary>0 for the oldest set in the list, 1 for the newest. Used to age the cues.</summary>
        private static float SpecEra(string tplId)
        {
            // 1980 (TRC-83) to 2016 (DP4601e) is the real spread of the thirteen.
            return Mathf.InverseLerp(1980f, 2016f, SpecYear(tplId));
        }

        /// <summary>0 for the lightest set, 1 for the heaviest. Drives how solid the key feels.</summary>
        private static float SpecBulk(string tplId)
        {
            // 155 g (ProTalk XLS) to 1200 g (AN/PRC-152).
            return Mathf.InverseLerp(155f, 1200f, SpecWeightGrams(tplId));
        }
    }
}
