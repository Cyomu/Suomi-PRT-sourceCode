using System;
using BepInEx.Configuration;

namespace RadioMod.Client
{
    /// <summary>
    /// Visual style of the mod's own interface. Classic is the look shipped up to 1.0.0-E and is
    /// frozen: it must keep rendering exactly as it did before the style seam existed. Instrument
    /// is the military-radio redesign and is the only style allowed to grow new settings.
    /// </summary>
    internal enum UiStyle
    {
        Classic,
        Instrument,
    }

    /// <summary>
    /// Process-wide access to the selected style. The renderers sit behind interfaces and must not
    /// reach back into the plugin instance, so the style is resolved through here rather than passed
    /// down every draw call.
    ///
    /// <see cref="Changed"/> fires when the player switches styles at runtime; cached
    /// <c>GUIStyle</c>s and generated textures are rebuilt from that, so switching needs no restart.
    /// </summary>
    internal static class UiStyleState
    {
        private static ConfigEntry<UiStyle> _entry;

        /// <summary>Raised after the style changed. Subscribers drop whatever they cached.</summary>
        public static event Action Changed;

        /// <summary>
        /// Falls back to Classic while unbound: everything drawn before <see cref="Bind"/> runs
        /// must look like the old build, never like a half-initialised new one.
        /// </summary>
        public static UiStyle Current => _entry == null ? UiStyle.Classic : _entry.Value;

        public static bool IsInstrument => Current == UiStyle.Instrument;

        public static void Bind(ConfigEntry<UiStyle> entry)
        {
            if (_entry != null)
            {
                _entry.SettingChanged -= OnSettingChanged;
            }

            _entry = entry;

            if (_entry != null)
            {
                _entry.SettingChanged += OnSettingChanged;
            }
        }

        private static void OnSettingChanged(object sender, EventArgs args)
        {
            Changed?.Invoke();
        }
    }
}
