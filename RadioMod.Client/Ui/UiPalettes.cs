using System.Collections.Generic;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// The seven palettes.
    ///
    /// Until now the mod had two, keyed off a single <c>bool bear</c>. That flag carried two separate
    /// meanings at once: which colours to use, and whether the chrome is stencil corner brackets or a
    /// clean thin frame. Those are genuinely different questions — TerraGroup is not BEAR, but it
    /// wants the same clean frame UNTAR does — so the palette now says both things explicitly.
    ///
    /// Values are the ones designed in the mock-up, carried across unchanged so the window and the
    /// mock-up cannot drift apart on colour.
    /// </summary>
    public partial class Plugin
    {
        internal struct ThemePalette
        {
            public Color Bg;
            public Color Panel;
            public Color Border;
            public Color BtnFill;
            public Color Accent;
            public Color AccentBright;
            public Color Signal;
            public Color SignalBright;
            public Color TextPrimary;
            public Color TextMuted;

            /// <summary>Ink for text drawn on top of a filled button — the palette's own darkest.</summary>
            public Color Ink;

            /// <summary>
            /// Stencil corner brackets rather than a clean full frame. A property of the palette,
            /// not of the faction: the two clean ones are the institutional palettes.
            /// </summary>
            public bool Stencil;
        }

        private static Color Hex(string rgb)
        {
            // Short, allocation-free enough for a table built once at startup.
            int r = System.Convert.ToInt32(rgb.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(rgb.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(rgb.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }

        private static readonly Dictionary<UiTheme, ThemePalette> Palettes = new Dictionary<UiTheme, ThemePalette>
        {
            [UiTheme.BEAR] = new ThemePalette
            {
                Bg = Hex("0b0e07"), Panel = Hex("1b2113"), Border = Hex("3a4526"), BtnFill = Hex("4a5a28"),
                Accent = Hex("8fbf5a"), AccentBright = Hex("b8e67a"),
                Signal = Hex("c1272d"), SignalBright = Hex("e8c349"),
                TextPrimary = Hex("d6dcc4"), TextMuted = Hex("6e7a55"), Ink = Hex("0b0e07"), Stencil = true,
            },

            // Cold graphite with coyote tan — the only neutral set of the seven, which is why it
            // sits comfortably next to every other palette without competing with any of them.
            [UiTheme.USEC] = new ThemePalette
            {
                Bg = Hex("0d0f10"), Panel = Hex("191c1e"), Border = Hex("343a3d"), BtnFill = Hex("7d6544"),
                Accent = Hex("b09068"), AccentBright = Hex("dcc39c"),
                Signal = Hex("c25b3a"), SignalBright = Hex("e6e2da"),
                TextPrimary = Hex("ddd9d2"), TextMuted = Hex("78736b"), Ink = Hex("0d0f10"), Stencil = true,
            },

            // The original blue. It was always peacekeeper rather than contractor, so it belongs here.
            [UiTheme.UNTAR] = new ThemePalette
            {
                Bg = Hex("101822"), Panel = Hex("1b2836"), Border = Hex("2e4155"), BtnFill = Hex("4b92db"),
                Accent = Hex("7fb6e8"), AccentBright = Hex("c5e2fa"),
                Signal = Hex("d9534f"), SignalBright = Hex("ffffff"),
                TextPrimary = Hex("e4edf5"), TextMuted = Hex("7c93a8"), Ink = Hex("06101a"), Stencil = false,
            },

            [UiTheme.RUAF] = new ThemePalette
            {
                Bg = Hex("12130d"), Panel = Hex("1e2015"), Border = Hex("3b3d26"), BtnFill = Hex("55572f"),
                Accent = Hex("a8a45c"), AccentBright = Hex("d6d089"),
                Signal = Hex("cc3b2a"), SignalBright = Hex("cc3b2a"),
                TextPrimary = Hex("ddd9c0"), TextMuted = Hex("77754f"), Ink = Hex("12130d"), Stencil = true,
            },

            [UiTheme.BlackDivision] = new ThemePalette
            {
                Bg = Hex("0a0908"), Panel = Hex("171514"), Border = Hex("332c2b"), BtnFill = Hex("4a2220"),
                Accent = Hex("b8453c"), AccentBright = Hex("e06a5e"),
                Signal = Hex("e8523f"), SignalBright = Hex("e8523f"),
                TextPrimary = Hex("ddd6d3"), TextMuted = Hex("6e6360"), Ink = Hex("0a0908"), Stencil = true,
            },

            [UiTheme.TerraGroup] = new ThemePalette
            {
                Bg = Hex("08131a"), Panel = Hex("10222c"), Border = Hex("1f3b49"), BtnFill = Hex("1c6f85"),
                Accent = Hex("35b8cf"), AccentBright = Hex("8fe6f2"),
                Signal = Hex("e8664f"), SignalBright = Hex("ffffff"),
                TextPrimary = Hex("dff0f4"), TextMuted = Hex("5d8494"), Ink = Hex("040d13"), Stencil = false,
            },

            [UiTheme.SCAV] = new ThemePalette
            {
                Bg = Hex("100c07"), Panel = Hex("1f1810"), Border = Hex("3d2f1e"), BtnFill = Hex("6b4a1e"),
                Accent = Hex("c98a35"), AccentBright = Hex("e8b45f"),
                Signal = Hex("d9612a"), SignalBright = Hex("d9612a"),
                TextPrimary = Hex("ded1b8"), TextMuted = Hex("7a6448"), Ink = Hex("100c07"), Stencil = true,
            },
        };

        internal static ThemePalette GetThemePalette(UiTheme theme)
        {
            return Palettes.TryGetValue(theme, out ThemePalette p) ? p : Palettes[UiTheme.BEAR];
        }

        /// <summary>
        /// Resolves the palette actually in force. <c>Auto</c> still follows the player's side, which
        /// is what it always did; the five added palettes are explicit choices only.
        /// </summary>
        private UiTheme ResolveTheme()
        {
            if (_uiTheme.Value != UiTheme.Auto)
            {
                return _uiTheme.Value;
            }

            TryGetLocalIdentity(out _, out EFT.EPlayerSide? side);
            return side == EFT.EPlayerSide.Usec ? UiTheme.UNTAR : UiTheme.BEAR;
        }
    }
}
