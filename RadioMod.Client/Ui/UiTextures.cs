using System.Collections.Generic;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Procedurally generated textures for the Instrument look.
    ///
    /// IMGUI has no rounded corners, no gradients and no shadows — but it will happily draw any
    /// texture, and textures are something we can build ourselves. Everything the mock-up has and
    /// the window lacked comes from here: rounded bordered panels, a vertical chassis gradient and
    /// a soft glow.
    ///
    /// Two rules keep this cheap. Textures are generated once and cached by their parameters, never
    /// per frame; and rounded panels are drawn as nine-slice, so one small texture stretches to any
    /// size without the corners deforming.
    /// </summary>
    public partial class Plugin
    {
        private static readonly Dictionary<string, Texture2D> _uiTextureCache = new Dictionary<string, Texture2D>();

        /// <summary>Drops every generated texture. Called when the palette or style changes.</summary>
        private static void ClearUiTextureCache()
        {
            foreach (Texture2D tex in _uiTextureCache.Values)
            {
                if (tex != null)
                {
                    Object.Destroy(tex);
                }
            }

            _uiTextureCache.Clear();
        }

        private static Texture2D Cached(string key, System.Func<Texture2D> build)
        {
            if (_uiTextureCache.TryGetValue(key, out Texture2D found) && found != null)
            {
                return found;
            }

            Texture2D made = build();
            made.hideFlags = HideFlags.HideAndDontSave;
            made.wrapMode = TextureWrapMode.Clamp;
            made.filterMode = FilterMode.Bilinear;
            _uiTextureCache[key] = made;
            return made;
        }

        private static string Key(string name, Color a, Color b, float p1, float p2)
        {
            return name + "|" + ColorUtility.ToHtmlStringRGBA(a) + "|" + ColorUtility.ToHtmlStringRGBA(b)
                + "|" + p1.ToString("0.##") + "|" + p2.ToString("0.##");
        }

        /// <summary>
        /// Rounded panel with a border, built for nine-slice stretching. The texture is only as big
        /// as it needs to be to hold two corners plus a pixel of middle, which is what makes an
        /// arbitrarily large panel cost nothing.
        /// </summary>
        private static Texture2D RoundedPanel(Color fill, Color border, float radius, float borderWidth)
        {
            return Cached(Key("panel", fill, border, radius, borderWidth), () =>
            {
                int r = Mathf.Max(2, Mathf.RoundToInt(radius));
                int size = r * 2 + 3;
                Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                Color[] px = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // Distance from the rounded rectangle's edge, negative inside.
                        float dx = Mathf.Max(Mathf.Abs(x - (size - 1) / 2f) - ((size - 1) / 2f - r), 0f);
                        float dy = Mathf.Max(Mathf.Abs(y - (size - 1) / 2f) - ((size - 1) / 2f - r), 0f);
                        float dist = Mathf.Sqrt(dx * dx + dy * dy) - r;

                        // One pixel of feather instead of a hard step: without it the curve looks
                        // like a staircase at the sizes these panels are drawn at.
                        float inside = Mathf.Clamp01(-dist);
                        float onBorder = Mathf.Clamp01(1f - Mathf.Abs(dist + borderWidth * 0.5f) / (borderWidth * 0.5f + 0.5f));

                        Color c = Color.Lerp(fill, border, onBorder);
                        c.a = fill.a * inside;
                        c.a = Mathf.Max(c.a, border.a * onBorder * inside);
                        px[y * size + x] = c;
                    }
                }

                tex.SetPixels(px);
                tex.Apply();
                return tex;
            });
        }

        /// <summary>Vertical gradient, one pixel wide. Stretched across whatever it fills.</summary>
        private static Texture2D VerticalGradient(Color top, Color bottom)
        {
            return Cached(Key("grad", top, bottom, 0f, 0f), () =>
            {
                const int h = 64;
                Texture2D tex = new Texture2D(1, h, TextureFormat.ARGB32, false);

                for (int y = 0; y < h; y++)
                {
                    // Texture space runs bottom-up, GUI space top-down.
                    tex.SetPixel(0, y, Color.Lerp(bottom, top, y / (float)(h - 1)));
                }

                tex.Apply();
                return tex;
            });
        }

        /// <summary>
        /// Soft radial glow with a transparent edge. Used under lit elements, where the mock-up has
        /// a blurred halo that a flat rectangle cannot imitate.
        /// </summary>
        private static Texture2D SoftGlow(Color colour)
        {
            return Cached(Key("glow", colour, Color.clear, 0f, 0f), () =>
            {
                const int size = 32;
                Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                float centre = (size - 1) / 2f;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre)) / centre;
                        // Squared falloff reads as light; linear reads as a badly drawn circle.
                        float a = Mathf.Clamp01(1f - d);
                        tex.SetPixel(x, y, new Color(colour.r, colour.g, colour.b, colour.a * a * a));
                    }
                }

                tex.Apply();
                return tex;
            });
        }

        /// <summary>Nine-slice border for the rounded panel textures generated above.</summary>
        private static RectOffset PanelBorder(float radius)
        {
            int r = Mathf.Max(2, Mathf.RoundToInt(radius)) + 1;
            return new RectOffset(r, r, r, r);
        }

        private static GUIStyle _roundedPanelStyle;
        private static float _roundedPanelRadius = -1f;

        /// <summary>
        /// Draws a rounded, bordered panel of any size. Uses a GUIStyle rather than DrawTexture
        /// because only a style carries nine-slice information — DrawTexture would stretch the
        /// corners along with the middle and turn the radius into an ellipse.
        /// </summary>
        private static void DrawRoundedPanel(Rect area, Color fill, Color border, float radius = 3f, float borderWidth = 1f)
        {
            Texture2D tex = RoundedPanel(fill, border, radius, borderWidth);

            if (_roundedPanelStyle == null || !Mathf.Approximately(_roundedPanelRadius, radius))
            {
                _roundedPanelStyle = new GUIStyle { border = PanelBorder(radius) };
                _roundedPanelRadius = radius;
            }

            _roundedPanelStyle.normal.background = tex;

            Color prev = GUI.color;
            GUI.color = Color.white;
            _roundedPanelStyle.Draw(area, GUIContent.none, false, false, false, false);
            GUI.color = prev;
        }

        private static void DrawGradient(Rect area, Color top, Color bottom)
        {
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(area, VerticalGradient(top, bottom), ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        private static void DrawGlow(Rect area, Color colour)
        {
            Color prev = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(area, SoftGlow(colour), ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }
    }
}
