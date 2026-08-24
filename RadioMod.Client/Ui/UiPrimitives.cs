using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Vector shapes IMGUI does not provide.
    ///
    /// The transport glyphs used to be text — ▶ ▮▮ ■ from the Geometric Shapes block — which works
    /// only as long as the chosen font actually carries them, and their size and weight are whatever
    /// the font decided rather than what the layout wants. The mock-up draws them as geometry, so
    /// they are geometry here too: exact size, exact alignment, no font dependency at all.
    ///
    /// IMGUI can only fill axis-aligned rectangles, so a triangle is drawn as a stack of one-pixel
    /// rows. At the sizes involved (12 px) that is a dozen draw calls per glyph and visually
    /// identical to a filled path.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>
        /// Filled triangle from three points, scan-converted row by row. Rows are snapped to whole
        /// pixels — a half-pixel row renders as a grey smear and makes a 12 px glyph look blurred.
        /// </summary>
        private static void FillTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;

            float top = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float bottom = Mathf.Max(a.y, Mathf.Max(b.y, c.y));

            int y0 = Mathf.FloorToInt(top);
            int y1 = Mathf.CeilToInt(bottom);

            for (int y = y0; y < y1; y++)
            {
                float scan = y + 0.5f;

                float xMin = float.MaxValue;
                float xMax = float.MinValue;

                // Intersect the scanline with each edge; a triangle gives at most two crossings.
                AccumulateEdge(a, b, scan, ref xMin, ref xMax);
                AccumulateEdge(b, c, scan, ref xMin, ref xMax);
                AccumulateEdge(c, a, scan, ref xMin, ref xMax);

                if (xMax > xMin)
                {
                    GUI.DrawTexture(new Rect(xMin, y, xMax - xMin, 1f), Texture2D.whiteTexture);
                }
            }

            GUI.color = prev;
        }

        private static void AccumulateEdge(Vector2 p, Vector2 q, float scan, ref float xMin, ref float xMax)
        {
            // A horizontal edge never defines a crossing — the two vertical edges already do.
            if (Mathf.Approximately(p.y, q.y))
            {
                return;
            }

            if (scan < Mathf.Min(p.y, q.y) || scan >= Mathf.Max(p.y, q.y))
            {
                return;
            }

            float t = (scan - p.y) / (q.y - p.y);
            float x = p.x + (q.x - p.x) * t;

            if (x < xMin) xMin = x;
            if (x > xMax) xMax = x;
        }

        private static void FillRect(Rect r, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>
        /// The transport glyphs, drawn inside a 12x12 box exactly as the mock-up's SVG paths define
        /// them. Every glyph is expressed in that box and scaled, so a button can change size without
        /// the glyphs drifting out of proportion with each other.
        /// </summary>
        private enum TransportGlyph { ToStart, Back, Play, Pause, Stop, Forward, ToEnd }

        private static void DrawTransportGlyph(Rect box, TransportGlyph glyph, Color color)
        {
            // Centre a square 12x12 field inside whatever rect the button gave us.
            float side = Mathf.Min(box.width, box.height);
            float ox = box.x + (box.width - side) / 2f;
            float oy = box.y + (box.height - side) / 2f;
            float u = side / 12f;

            Vector2 P(float x, float y) => new Vector2(ox + x * u, oy + y * u);
            Rect R(float x, float y, float w, float h) => new Rect(ox + x * u, oy + y * u, w * u, h * u);

            switch (glyph)
            {
                case TransportGlyph.ToStart:
                    // M2 1h2v10H2z + M11 1v10L5 6z
                    FillRect(R(2f, 1f, 2f, 10f), color);
                    FillTriangle(P(11f, 1f), P(11f, 11f), P(5f, 6f), color);
                    break;

                case TransportGlyph.Back:
                    // M11 1v10L6 6z + M6 1v10L1 6z
                    FillTriangle(P(11f, 1f), P(11f, 11f), P(6f, 6f), color);
                    FillTriangle(P(6f, 1f), P(6f, 11f), P(1f, 6f), color);
                    break;

                case TransportGlyph.Play:
                    FillTriangle(P(2f, 1f), P(2f, 11f), P(10f, 6f), color);
                    break;

                case TransportGlyph.Pause:
                    // M2 1h3v10H2z + M7 1h3v10H7z
                    FillRect(R(2f, 1f, 3f, 10f), color);
                    FillRect(R(7f, 1f, 3f, 10f), color);
                    break;

                case TransportGlyph.Stop:
                    // M2 2h8v8H2z
                    FillRect(R(2f, 2f, 8f, 8f), color);
                    break;

                case TransportGlyph.Forward:
                    // M1 1v10l5-5z + M6 1v10l5-5z
                    FillTriangle(P(1f, 1f), P(1f, 11f), P(6f, 6f), color);
                    FillTriangle(P(6f, 1f), P(6f, 11f), P(11f, 6f), color);
                    break;

                case TransportGlyph.ToEnd:
                    // Mirror of ToStart: the bar sits on the right.
                    FillTriangle(P(1f, 1f), P(1f, 11f), P(7f, 6f), color);
                    FillRect(R(8f, 1f, 2f, 10f), color);
                    break;
            }
        }

        /// <summary>
        /// A transport button: chrome from the shared button style, glyph drawn on top. Disabled
        /// buttons get the muted ink the mock-up gives its <c>.ghost</c> variant rather than being
        /// hidden, so the row keeps its shape when a control is unavailable.
        /// </summary>
        private static bool TransportButton(TransportGlyph glyph, float width = 30f, float height = 26f)
        {
            Rect r = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));

            bool pressed = GUI.Button(r, GUIContent.none, MilStyle.GlyphButton);

            Color ink = GUI.enabled
                ? MilStyle.Ink
                : new Color(MilStyle.TextMuted.r, MilStyle.TextMuted.g, MilStyle.TextMuted.b, 0.65f);

            DrawTransportGlyph(r, glyph, ink);
            return pressed;
        }
    }
}
