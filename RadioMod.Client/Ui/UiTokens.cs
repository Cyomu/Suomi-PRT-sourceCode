using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RadioMod.Client
{
    // Nested inside Plugin because the colour roles read MilStyle, which is a private nested class.
    // Keeping the tokens in their own file preserves the separation without widening MilStyle's
    // visibility just to satisfy the compiler.
    public partial class Plugin
    {
    /// <summary>
    /// The design tokens the Instrument style is built from: one font, one type scale, one spacing
    /// grid, one set of colour roles. Classic never reads any of this — it keeps its own constants,
    /// which is what makes it impossible to break the frozen look by editing a token.
    ///
    /// Nothing here is a magic number pulled from a mockup: sizes come off a single scale and line
    /// thickness is a function of resolution, because the old hard-coded 1px rules disappear on 4K.
    /// </summary>
    internal static class UiTokens
    {
        // ---- type scale -------------------------------------------------------------------------
        // Four steps, deliberately few. Anything that does not fit one of them is a layout problem,
        // not a missing size.
        public const int SizeMicro = 9;   // stencil marking, units, tags
        public const int SizeSmall = 11;  // labels, list rows
        public const int SizeBody = 13;   // running text
        public const int SizeReadout = 16; // instrument readouts: clock, dBm, charge

        // ---- spacing grid -----------------------------------------------------------------------
        // Everything is a multiple of 2; the four names cover every gap the panels actually need.
        public const float GapTight = 2f;
        public const float GapUnit = 6f;
        public const float GapGroup = 12f;
        public const float GapSection = 20f;

        /// <summary>
        /// Hairlines vanish on high-DPI displays, so rule thickness scales with the vertical
        /// resolution instead of being fixed at one pixel. 1080p keeps the classic 1px look.
        /// </summary>
        public static float Hairline => Mathf.Max(1f, Mathf.Round(Screen.height / 1080f));

        /// <summary>Emphasised rule: frames, accent spines, meter borders.</summary>
        public static float Rule => Hairline * 2f;

        // ---- colour roles -----------------------------------------------------------------------
        // The palette already lives in MilStyle and is faction-driven; tokens only give those values
        // role names so the renderers stop reaching for "the gold one" by hand.
        public static Color Chassis => MilStyle.Bg;
        public static Color Panel => MilStyle.Panel;
        public static Color Edge => MilStyle.Border;
        public static Color Readout => MilStyle.TextPrimary;
        public static Color Dim => MilStyle.TextMuted;
        public static Color Lit => MilStyle.Accent;
        public static Color LitBright => MilStyle.AccentBright;
        public static Color Alarm => MilStyle.Signal;
        public static Color Signal => MilStyle.SignalBright;

        // ---- font -------------------------------------------------------------------------------
        private static Font _font;
        private static Font _fontBold;
        private static bool _fontResolved;

        /// <summary>
        /// Face families to look for, best first, matched by substring rather than by exact name.
        ///
        /// The probe on a live 4.1.2 build reported the EFT face as "Jovanny Lemonad - Bender" and
        /// "Jovanny Lemonad - Bender Bold" — a foundry prefix an exact-name match would never have
        /// found. Substring matching survives that and any similar renaming in a future game build.
        /// </summary>
        private static readonly string[] PreferredFamilies = { "Bender", "NotoSansDisplay", "Arial" };

        /// <summary>
        /// Bold cut of the same family, when the game ships one. Unity can synthesise bold for a
        /// dynamic font, but a real bold face is cleaner at the small sizes the panels use.
        /// </summary>
        public static Font FontBold
        {
            get
            {
                if (!_fontResolved)
                {
                    _fontResolved = true;
                    _font = ResolveFont();
                }

                return _fontBold ?? _font;
            }
        }

        /// <summary>
        /// The font used by every Instrument surface. Resolved once; null means "use Unity's
        /// default", which is exactly what Classic already renders with, so the fallback is safe.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (!_fontResolved)
                {
                    _fontResolved = true;
                    _font = ResolveFont();
                }

                return _font;
            }
        }

        /// <summary>
        /// One-shot probe. The available fonts are a property of the running game build, so they are
        /// logged once per session and read from the log rather than guessed at in code.
        /// Gated behind the same once-per-session rule as the other startup diagnostics: a
        /// per-frame version of this would be a 1.3 MB log file.
        /// </summary>
        private static Font ResolveFont()
        {
            Font[] loaded;
            try
            {
                loaded = Resources.FindObjectsOfTypeAll<Font>();
            }
            catch
            {
                return null;
            }

            if (loaded == null || loaded.Length == 0)
            {
                LogProbe("PRT: font probe found no loaded fonts; Instrument falls back to the default face.");
                return null;
            }

            Font[] usable = loaded.Where(f => f != null && !string.IsNullOrEmpty(f.name)).ToArray();

            string[] names = usable.Select(f => f.name).Distinct().OrderBy(n => n).ToArray();
            LogProbe("PRT: font probe found " + names.Length + " face(s): " + string.Join(", ", names));

            foreach (string family in PreferredFamilies)
            {
                Font[] inFamily = usable
                    .Where(f => f.name.IndexOf(family, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();

                if (inFamily.Length == 0)
                {
                    continue;
                }

                bool IsBold(Font f) => f.name.IndexOf("bold", System.StringComparison.OrdinalIgnoreCase) >= 0;

                Font regular = inFamily.FirstOrDefault(f => !IsBold(f)) ?? inFamily[0];
                _fontBold = inFamily.FirstOrDefault(IsBold);

                LogProbe("PRT: Instrument style uses '" + regular.name + "'"
                    + (_fontBold != null ? " with bold cut '" + _fontBold.name + "'." : " (no bold cut found)."));

                return regular;
            }

            // Nothing preferred is present. Rather than picking an arbitrary face — which could be an
            // icon font and would render as boxes — fall back to the default the mod already uses.
            LogProbe("PRT: no preferred font present; Instrument falls back to the default face.");
            return null;
        }

        // ResolveFont already runs once per session behind _fontResolved, so this needs no guard of
        // its own. The previous version had one, and it silently swallowed every message after the
        // first — which is why the log showed the face list but never said which font was chosen.
        private static void LogProbe(string message)
        {
            Plugin.LogAttributeDiagnostic(message);
        }

        /// <summary>
        /// Applies the token font to a style. Called when styles are built, not per frame.
        /// A null font leaves the style on Unity's default, so this is always safe to call.
        /// </summary>
        public static GUIStyle WithFont(GUIStyle style)
        {
            if (style != null && Font != null)
            {
                style.font = Font;
            }

            return style;
        }
    }
    }
}
