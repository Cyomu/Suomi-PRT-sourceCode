using System;
using System.Collections.Generic;
using EFT;
using EFT.UI.Screens;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Everything the mod draws while a raid is running: the indicator stack, the battery and
    /// signal readouts, the live audio badges, the tuning sweep and the overlay notifications,
    /// plus the <c>OnGUI</c> entry point that scales and orders them.
    ///
    /// Split out of Plugin.cs verbatim as the first step of the Classic/Instrument style seam —
    /// no logic was changed in the move. The second step lifts this code out of the plugin class
    /// entirely, behind a renderer interface fed by a frame model.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>Charge readout drawn just above the indicator row, in the selected style.</summary>
        private void DrawBatteryIndicator(float rightEdge, float bottomY, float fraction)
        {
            if (_batteryLabelStyle == null)
            {
                _batteryLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                };
            }

            float opacity = IndicatorAlpha();
            Color charge = BatteryChargeColor(fraction);
            Color prev = GUI.color;

            BatteryIndicatorStyle style = _batteryIndicatorStyle.Value;
            bool showCell = style == BatteryIndicatorStyle.Cell || style == BatteryIndicatorStyle.CellAndPercent;
            bool showBar = style == BatteryIndicatorStyle.Bar || style == BatteryIndicatorStyle.BarAndPercent;
            bool showPercent = style == BatteryIndicatorStyle.Percent
                || style == BatteryIndicatorStyle.CellAndPercent
                || style == BatteryIndicatorStyle.BarAndPercent;

            float x = rightEdge;

            if (showPercent)
            {
                // Inset readout with a charge-coloured spine and a fill that tracks the level,
                // so the number reads as an instrument rather than plain text on the HUD.
                const float labelWidth = 44f;
                const float labelHeight = 15f;
                x -= labelWidth;
                Rect box = new Rect(x, bottomY - labelHeight - 1f, labelWidth, labelHeight);

                GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.85f * opacity);
                GUI.DrawTexture(box, Texture2D.whiteTexture);

                GUI.color = new Color(charge.r, charge.g, charge.b, 0.16f * opacity);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width * Mathf.Clamp01(fraction), box.height), Texture2D.whiteTexture);

                // Full frame, not just a left spine — the open right edge was what looked unfinished.
                GUI.color = new Color(charge.r, charge.g, charge.b, opacity);
                GUI.DrawTexture(new Rect(box.x, box.y, 2f, box.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.xMax - 1f, box.y, 1f, box.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.x, box.yMax - 1f, box.width, 1f), Texture2D.whiteTexture);

                _batteryLabelStyle.normal.textColor = new Color(charge.r, charge.g, charge.b, opacity);
                GUI.Label(box, Mathf.RoundToInt(fraction * 100f) + "%", _batteryLabelStyle);

                x -= 5f;
            }

            if (showCell)
            {
                const float w = 26f;
                const float h = 12f;
                const int cells = 4;
                x -= w + 3f;
                Rect body = new Rect(x, bottomY - h - 1f, w, h);

                // Contact nub on the right, then the shell, then the charge segments.
                GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, opacity);
                GUI.DrawTexture(new Rect(body.xMax + 1f, body.y + 3f, 2f, h - 6f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(body.x, body.y, body.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(body.x, body.yMax - 1f, body.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(body.x, body.y, 1f, body.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(body.xMax - 1f, body.y, 1f, body.height), Texture2D.whiteTexture);

                float inner = body.width - 4f;
                float cellW = (inner - (cells - 1)) / cells;
                float lit = fraction * cells;

                for (int i = 0; i < cells; i++)
                {
                    float fill = Mathf.Clamp01(lit - i);
                    if (fill <= 0f)
                    {
                        continue;
                    }

                    GUI.color = new Color(charge.r, charge.g, charge.b, opacity * (0.45f + 0.55f * fill));
                    GUI.DrawTexture(new Rect(body.x + 2f + i * (cellW + 1f), body.y + 2f, cellW * fill, body.height - 4f), Texture2D.whiteTexture);
                }
            }

            if (showBar)
            {
                const float w = 44f;
                const float h = 5f;
                const int cells = 8;
                x -= w;
                Rect bar = new Rect(x, bottomY - h - 4f, w, h);

                GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.8f * opacity);
                GUI.DrawTexture(new Rect(bar.x - 1f, bar.y - 1f, bar.width + 2f, bar.height + 2f), Texture2D.whiteTexture);

                float cellW = (w - (cells - 1)) / cells;
                float lit = fraction * cells;

                for (int i = 0; i < cells; i++)
                {
                    float fill = Mathf.Clamp01(lit - i);
                    GUI.color = fill > 0f
                        ? new Color(charge.r, charge.g, charge.b, opacity * (0.4f + 0.6f * fill))
                        : new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.5f * opacity);
                    GUI.DrawTexture(new Rect(bar.x + i * (cellW + 1f), bar.y, cellW, bar.height), Texture2D.whiteTexture);
                }
            }

            GUI.color = prev;
        }

        private GUIStyle _batteryLabelStyle;

        private void DrawDot(Texture2D dot, float x, float y, float diameter, Color color)
        {
            float opacity = IndicatorAlpha();
            color.a *= opacity;


            if (IndicatorLampsAreRound())
            {
                GUI.color = color;
                GUI.DrawTexture(new Rect(x, y, diameter, diameter), dot);
                return;
            }

            // Angular segment: a filled core inside a darker frame, matching the panel language
            // used by the recordings window and the notification chassis.
            Rect cell = new Rect(x, y + 1f, diameter, diameter - 2f);

            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.75f * opacity);
            GUI.DrawTexture(new Rect(cell.x - 1f, cell.y - 1f, cell.width + 2f, cell.height + 2f), Texture2D.whiteTexture);

            GUI.color = color;
            GUI.DrawTexture(cell, Texture2D.whiteTexture);
        }

        private bool TryGetBestSignalQuality(out float quality)
        {
            quality = 1f;
            bool found = false;

            foreach (string name in RadioSpeakerNames)
            {
                if (!_lastMode.TryGetValue(name, out RadioVoiceFilter.Mode mode) || mode == RadioVoiceFilter.Mode.Silent)
                {
                    continue;
                }

                float ratio = _lastRatio.TryGetValue(name, out float r) ? r : 1f;
                if (!found || ratio < quality)
                {
                    quality = ratio;
                    found = true;
                }
            }

            return found;
        }

        private static bool IsEscMenuOpen()
        {
            try
            {
                EFT.UI.Screens.IBaseScreenController<EEftScreenType> controller = EFT.UI.Screens.EftScreenManager.Instance?.CurrentBaseScreenController;
                return controller != null && controller.ScreenType == EEftScreenType.Settings;
            }
            catch
            {
                return false;
            }
        }

        private float _lastIndicatorActivityTime;
        private float _tuningSweepStartTime;

        private float IndicatorAlpha()
        {
            return _indicatorOpacity.Value * GetIndicatorFade();
        }

        /// <summary>
        /// Cosmetic dial sweep played when the active radio changes: a tick scale slides past a
        /// fixed centre needle and settles, the way a real set looks while being tuned.
        /// </summary>
        private void DrawTuningSweep()
        {
            if (!_showTuningSweep.Value || _tuningSweepStartTime <= 0f)
            {
                return;
            }

            const float duration = 0.7f;
            float t = (Time.unscaledTime - _tuningSweepStartTime) / duration;
            if (t >= 1f)
            {
                _tuningSweepStartTime = 0f;
                return;
            }

            // Ease-out: fast sweep that settles onto the new frequency.
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float alpha = Mathf.Sin(t * Mathf.PI) * _indicatorOpacity.Value;

            const float w = 190f;
            const float h = 26f;
            Rect area = new Rect((Screen.width - w) / 2f, Screen.height * 0.62f, w, h);

            // The sweep is an overlay like the notifications, so it follows the notification
            // theme rather than the recordings-window theme.
            MilStyle.Palette pal = _notificationTheme.Value == NotificationTheme.FollowWindow
                ? MilStyle.GetPalette(MilStyle.IsBear)
                : MilStyle.GetPalette(_notificationTheme.Value == NotificationTheme.BEAR);

            Color prev = GUI.color;

            GUI.color = new Color(pal.Bg.r, pal.Bg.g, pal.Bg.b, 0.8f * alpha);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(pal.Border.r, pal.Border.g, pal.Border.b, 0.9f * alpha);
            GUI.DrawTexture(new Rect(area.x, area.y, area.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x, area.yMax - 1f, area.width, 1f), Texture2D.whiteTexture);

            // Scale ticks scrolling past; offset eases to a stop.
            float offset = (1f - eased) * 120f;
            GUI.BeginGroup(area);
            for (int i = -14; i < 28; i++)
            {
                float x = i * 9f - Mathf.Repeat(offset, 9f) + area.width / 2f - 60f;
                if (x < 0f || x > area.width)
                {
                    continue;
                }

                bool major = i % 5 == 0;
                float tickH = major ? h * 0.5f : h * 0.28f;
                GUI.color = new Color(pal.Accent.r, pal.Accent.g, pal.Accent.b, (major ? 0.75f : 0.4f) * alpha);
                GUI.DrawTexture(new Rect(x, (h - tickH) / 2f, 1f, tickH), Texture2D.whiteTexture);
            }
            GUI.EndGroup();

            // Fixed centre needle.
            GUI.color = new Color(pal.SignalBright.r, pal.SignalBright.g, pal.SignalBright.b, alpha);
            GUI.DrawTexture(new Rect(area.center.x - 1f, area.y - 3f, 2f, area.height + 6f), Texture2D.whiteTexture);

            GUI.color = prev;
        }

        /// <summary>
        /// Indicators sit at full brightness during activity and ease down to a dim resting level
        /// after a few quiet seconds, so they stop competing with the game during downtime.
        /// </summary>
        private float GetIndicatorFade()
        {
            if (!_fadeIdleIndicators.Value)
            {
                return 1f;
            }

            const float holdSeconds = 3f;
            const float fadeSeconds = 1.5f;
            const float restingLevel = 0.35f;

            float idle = Time.unscaledTime - _lastIndicatorActivityTime - holdSeconds;
            if (idle <= 0f)
            {
                return 1f;
            }

            return Mathf.Lerp(1f, restingLevel, Mathf.Clamp01(idle / fadeSeconds));
        }

        /// <summary>
        /// Topmost Y reached by the indicator stack this frame, in the indicators' own unscaled
        /// local coordinates (i.e. before <see cref="_indicatorScale"/> is applied). Screen.height
        /// (bottom of screen) when nothing was drawn. DrawNotification reads this — converted to
        /// real screen pixels via the indicator scale, then back into its own local space via the
        /// notification scale — so the two stacks never draw on top of each other regardless of how
        /// differently they're scaled.
        /// </summary>
        private float _indicatorStackTopY = float.MaxValue;

        private void DrawIndicators()
        {
            // Instrument replaces the whole stack with a single chassis; Classic below is untouched.
            if (UiStyleState.IsInstrument)
            {
                DrawInstrumentIndicators();
                return;
            }

            _indicatorStackTopY = Screen.height;

            if (_radioLocation != RadioLocation.Ready || IsEscMenuOpen())
            {
                return;
            }

            const float diameter = 10f;
            const float spacing = 18f;
            const float rowGap = 6f;
            const float marginRight = 20f;
            const float marginBottom = 36f;

            bool showPower = _showPowerIndicator.Value;

            bool showDuplex = _showDuplexIndicator.Value && _radioOn;
            bool showTalking = _showTalkingIndicator.Value && _txChannel != null;
            bool channelBusy = RadioSpeakerNames.Count > 0;
            bool showBusy = _showBusyIndicator.Value && channelBusy;
            float quality = 1f;
            bool showSignal = _showSignalIndicator.Value && channelBusy && TryGetBestSignalQuality(out quality);

            // Any traffic counts as activity and resets the idle timer.
            if (showTalking || channelBusy)
            {
                _lastIndicatorActivityTime = Time.unscaledTime;
            }

            bool talkingAndBusyBoth = showTalking && showBusy;
            int talkBusySlots = talkingAndBusyBoth ? 2 : (showTalking || showBusy ? 1 : 0);

            int rowCount = talkBusySlots + (showDuplex ? 1 : 0) + (showPower ? 1 : 0);
            if (rowCount == 0 && !showSignal)
            {
                return;
            }

            Texture2D dot = GetIndicatorDotTexture();
            Color prevGuiColor = GUI.color;

            float rightEdge = Screen.width - marginRight;
            float rowY = Screen.height - marginBottom - diameter;

            // In Badges mode the lamp row is suppressed; the labelled meters carry the state.
            if (rowCount > 0 && IndicatorLampsVisible())
            {
                float rowWidth = (rowCount - 1) * spacing + diameter;
                float x = rightEdge - rowWidth;

                if (talkingAndBusyBoth)
                {
                    DrawDot(dot, x, rowY, diameter, _colorTalking.Value);
                    x += spacing;
                    DrawDot(dot, x, rowY, diameter, _colorBusy.Value);
                    x += spacing;
                }
                else if (showTalking)
                {
                    DrawDot(dot, x, rowY, diameter, _colorTalking.Value);
                    x += spacing;
                }
                else if (showBusy)
                {
                    DrawDot(dot, x, rowY, diameter, _colorBusy.Value);
                    x += spacing;
                }

                if (showDuplex)
                {
                    Color duplexColor = _duplexMode == DuplexMode.Simplex ? _colorSimplex.Value : new Color(0.85f, 0.85f, 0.85f);
                    DrawDot(dot, x, rowY, diameter, duplexColor);
                    x += spacing;
                }

                if (showPower)
                {
                    DrawDot(dot, x, rowY, diameter, _radioOn ? _colorOn.Value : new Color(0.5f, 0.5f, 0.5f));
                }
            }

            // Battery sits directly above the lamp row, below any audio badges.
            float batteryTop = rowY - rowGap - (showSignal ? 14f : 0f);
            float topY = rowY;
            if (_showBatteryIndicator.Value && _radioOn && TryGetRadioBatteryCharge(out float batteryFraction))
            {
                DrawBatteryIndicator(rightEdge, batteryTop, batteryFraction);
                topY = batteryTop;
                batteryTop -= 18f;
            }

            if (IndicatorBadgesVisible())
            {
                float badgeY = batteryTop;

                if (showTalking)
                {
                    DrawAudioBadge(rightEdge, badgeY,
                        L("ПЕРЕДАЧА", "ON AIR", "SENDEN", "EMISIÓN", "ÉMISSION", "NADAJE", "IN ONDA", "VYSÍLÁM"),
                        _colorTalking.Value, _txBands, _localMicRecorder.InputLevel);
                    topY = badgeY;
                    badgeY -= 20f;
                }

                if (showBusy)
                {
                    DrawAudioBadge(rightEdge, badgeY,
                        L("ПРИЁМ", "RECEIVING", "EMPFANG", "RECEPCIÓN", "RÉCEPTION", "ODBIÓR", "RICEZIONE", "PŘÍJEM"),
                        _colorBusy.Value, _rxBands, GetLoudestRemoteLevel());
                    topY = badgeY;
                }
            }

            if (showSignal)
            {

                float fill = Mathf.Clamp01(1f - quality);

                if (_signalIndicatorStyle.Value == SignalIndicatorStyle.AntennaBars)
                {
                    DrawSignalAntennaBars(rightEdge, rowY, rowGap, fill);
                }
                else
                {
                    DrawSignalFillBar(rightEdge, rowY, rowGap, fill);
                }
            }

            // Badge/battery labels sit a bit above their anchor point rather than exactly on it —
            // pad the reserved area so notifications clear the actual glyphs, not just the anchors.
            _indicatorStackTopY = topY - 20f;

            GUI.color = prevGuiColor;
        }

        private const int VuBandCount = 5;
        private readonly float[] _txBands = new float[VuBandCount];
        private readonly float[] _rxBands = new float[VuBandCount];
        private GUIStyle _transmitBadgeStyle;

        /// <summary>
        /// Drives a bank of level bars from a real audio level. Bands sit at rising thresholds so
        /// quiet speech lights only the first ones and loud speech pushes the whole bank up, the way
        /// a hardware VU meter behaves. Each band falls back gradually instead of snapping to zero.
        /// </summary>
        private static void UpdateVuBands(float[] bands, float level)
        {
            float fallPerSecond = 2.4f * Time.unscaledDeltaTime;

            for (int i = 0; i < bands.Length; i++)
            {
                // Later bands need progressively more signal before they light up.
                float threshold = i / (float)bands.Length;
                float target = Mathf.InverseLerp(threshold, threshold + 0.35f, level);

                bands[i] = target > bands[i]
                    ? target
                    : Mathf.Max(target, bands[i] - fallPerSecond);
            }
        }

        private float GetLoudestRemoteLevel()
        {
            float loudest = 0f;
            foreach (string name in RadioSpeakerNames)
            {
                if (_radioFilters.TryGetValue(name, out RadioVoiceFilter filter) && filter != null && filter.OutputLevel > loudest)
                {
                    loudest = filter.OutputLevel;
                }
            }

            return loudest;
        }

        /// <summary>
        /// Live "on air" / "receiving" badge: a chassis with a level meter driven by the actual
        /// audio, so your own transmission is never confused with incoming traffic.
        /// </summary>
        private void DrawAudioBadge(float rightEdge, float bottomY, string label, Color accent, float[] bands, float level)
        {
            if (_transmitBadgeStyle == null)
            {
                _transmitBadgeStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                };
            }

            UpdateVuBands(bands, level);

            const float badgeHeight = 17f;
            const float meterWidth = 34f;
            const float textPadding = 8f;

            float opacity = IndicatorAlpha();

            // Width follows the localised label so translations never get clipped.
            _transmitBadgeStyle.fontSize = 10;
            float labelWidth = _transmitBadgeStyle.CalcSize(new GUIContent(label)).x;
            float badgeWidth = textPadding + labelWidth + 8f + meterWidth + 7f;

            Rect badge = new Rect(rightEdge - badgeWidth, bottomY - badgeHeight, badgeWidth, badgeHeight);
            Color prev = GUI.color;

            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.85f * opacity);
            GUI.DrawTexture(badge, Texture2D.whiteTexture);

            // Left edge brightens with the signal rather than pulsing on a timer.
            GUI.color = new Color(accent.r, accent.g, accent.b, opacity * (0.4f + 0.6f * Mathf.Clamp01(level * 2f)));
            GUI.DrawTexture(new Rect(badge.x, badge.y, 2f, badge.height), Texture2D.whiteTexture);

            GUI.color = new Color(accent.r, accent.g, accent.b, 0.35f * opacity);
            GUI.DrawTexture(new Rect(badge.x, badge.y, badge.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(badge.x, badge.yMax - 1f, badge.width, 1f), Texture2D.whiteTexture);

            _transmitBadgeStyle.normal.textColor = new Color(accent.r, accent.g, accent.b, opacity);
            GUI.Label(new Rect(badge.x + textPadding, badge.y, labelWidth + 2f, badge.height), label, _transmitBadgeStyle);

            DrawVuMeter(new Rect(badge.xMax - meterWidth - 5f, badge.y + 3f, meterWidth, badge.height - 6f), bands, accent, opacity);

            GUI.color = prev;
        }

        private static void DrawVuMeter(Rect area, float[] bands, Color accent, float opacity)
        {
            float slot = area.width / bands.Length;
            float barWidth = Mathf.Max(2f, slot - 1.5f);

            for (int i = 0; i < bands.Length; i++)
            {
                float value = Mathf.Clamp01(bands[i]);
                float h = Mathf.Max(1f, value * area.height);
                float bx = area.x + i * slot;

                // Unlit remainder of the column, so the meter has visible headroom.
                GUI.color = new Color(accent.r, accent.g, accent.b, 0.16f * opacity);
                GUI.DrawTexture(new Rect(bx, area.y, barWidth, area.height), Texture2D.whiteTexture);

                GUI.color = new Color(accent.r, accent.g, accent.b, opacity * (0.55f + 0.45f * value));
                GUI.DrawTexture(new Rect(bx, area.yMax - h, barWidth, h), Texture2D.whiteTexture);
            }
        }

        private void DrawSignalFillBar(float rightEdge, float rowY, float rowGap, float fill)
        {
            const float barWidth = 44f;
            const float barHeight = 5f;
            const int cells = 8;
            const float cellGap = 1f;

            float opacity = IndicatorAlpha();
            float barY = rowY - barHeight - rowGap;
            float barX = rightEdge - barWidth;

            // Recessed track, so the meter reads as an instrument rather than a flat grey strip.
            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.8f * opacity);
            GUI.DrawTexture(new Rect(barX - 1f, barY - 1f, barWidth + 2f, barHeight + 2f), Texture2D.whiteTexture);

            Color fillColor = _colorSignalBar.Value;
            float cellWidth = (barWidth - cellGap * (cells - 1)) / cells;
            float litCells = Mathf.Clamp01(fill) * cells;

            for (int i = 0; i < cells; i++)
            {
                float cellX = barX + i * (cellWidth + cellGap);
                float lit = Mathf.Clamp01(litCells - i);

                GUI.color = lit > 0f
                    ? new Color(fillColor.r, fillColor.g, fillColor.b, fillColor.a * opacity * (0.4f + 0.6f * lit))
                    : new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.5f * opacity);

                GUI.DrawTexture(new Rect(cellX, barY, cellWidth, barHeight), Texture2D.whiteTexture);
            }
        }

        private static readonly float[] AntennaBarHeights = { 3f, 5f, 7f, 9f };

        private void DrawSignalAntennaBars(float rightEdge, float rowY, float rowGap, float fill)
        {
            const float barWidth = 4f;
            const float barGap = 2f;
            int barCount = AntennaBarHeights.Length;
            float totalWidth = barCount * barWidth + (barCount - 1) * barGap;
            float baseline = rowY - rowGap;
            float startX = rightEdge - totalWidth;

            int filledCount = Mathf.CeilToInt(Mathf.Clamp01(fill) * barCount);

            float opacity = IndicatorAlpha();
            Color emptyColor = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.55f * opacity);
            Color filledColor = _colorSignalBar.Value;
            filledColor.a *= opacity;

            for (int i = 0; i < barCount; i++)
            {
                float barHeight = AntennaBarHeights[i];
                float barX = startX + i * (barWidth + barGap);
                float barY = baseline - barHeight;
                Rect bar = new Rect(barX, barY, barWidth, barHeight);

                if (i < filledCount)
                {
                    // Soft halo under the lit bars so signal strength reads at a glance.
                    GUI.color = new Color(filledColor.r, filledColor.g, filledColor.b, filledColor.a * 0.22f);
                    GUI.DrawTexture(new Rect(bar.x - 1f, bar.y - 1f, bar.width + 2f, bar.height + 2f), Texture2D.whiteTexture);

                    GUI.color = filledColor;
                    GUI.DrawTexture(bar, Texture2D.whiteTexture);
                    continue;
                }

                // Empty slots are drawn as outlines rather than solid grey blocks.
                GUI.color = emptyColor;
                GUI.DrawTexture(new Rect(bar.x, bar.yMax - 1f, bar.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(bar.x, bar.y, 1f, bar.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(bar.xMax - 1f, bar.y, 1f, bar.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width, 1f), Texture2D.whiteTexture);
            }
        }

        private void OnGUI()
        {
            // Indicators and notifications draw during a raid, where the recordings window never
            // runs — so the theme has to be resolved here rather than inside that window.
            MilStyle.ApplyTheme(ResolveTheme());

            DrawRaidReviewBrowser();

            if (GetLocalPlayer() == null)
            {
                return;
            }

            Matrix4x4 prevUiMatrix = GUI.matrix;

            // Pivoted on the bottom-right corner, which is where every indicator/notification rect
            // is anchored — scaling around the screen origin (0,0) instead would push them further
            // from that corner as they grow and fly off-screen at anything above 1x.
            Vector2 pivot = new Vector2(Screen.width, Screen.height);

            if (Mathf.Abs(_indicatorScale.Value - 1f) > 0.001f)
            {
                GUIUtility.ScaleAroundPivot(new Vector2(_indicatorScale.Value, _indicatorScale.Value), pivot);
            }

            DrawIndicators();
            DrawTuningSweep();

            GUI.matrix = prevUiMatrix;

            // How far up from the bottom-right corner, in real screen pixels, the indicator stack
            // actually reaches once its own scale is applied — notifications use this to keep their
            // own stack from starting below that point, so bumping either slider independently can
            // never make the two overlap.
            float indicatorRealClearance = (Screen.height - _indicatorStackTopY) * _indicatorScale.Value;

            if (Mathf.Abs(_notificationScale.Value - 1f) > 0.001f)
            {
                GUIUtility.ScaleAroundPivot(new Vector2(_notificationScale.Value, _notificationScale.Value), pivot);
            }

            DrawNotification(indicatorRealClearance);

            GUI.matrix = prevUiMatrix;
        }

        private const float NotificationBaseMarginBottom = 120f;
        private const float NotificationIndicatorGap = 12f;

        private void DrawNotification(float indicatorRealClearance)
        {
            float now = Time.time;
            _notifications.RemoveAll(item => item.ExpireTime - now <= 0f);

            if (_notifications.Count == 0)
            {
                return;
            }

            if (_notificationStyle == null)
            {
                _notificationStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                };

                _notificationTagStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    wordWrap = false,
                };
            }

            // Convert the indicator stack's real-pixel reach back into notifications' own (possibly
            // differently scaled) local space, then use whichever margin is bigger — the usual fixed
            // one, or the one that actually clears the indicators this frame.
            float requiredLocalMargin = indicatorRealClearance / Mathf.Max(0.01f, _notificationScale.Value) + NotificationIndicatorGap;
            float effectiveMarginBottom = Mathf.Max(NotificationBaseMarginBottom, requiredLocalMargin);

            // Newest sits at the bottom of the stack, older ones ride above it.
            for (int i = 0; i < _notifications.Count; i++)
            {
                int slotFromBottom = _notifications.Count - 1 - i;
                DrawSingleNotification(_notifications[i], slotFromBottom, now, effectiveMarginBottom);
            }
        }

        private void DrawSingleNotification(OverlayNotification n, int slotFromBottom, float now, float marginBottom)
        {
            float remaining = n.ExpireTime - now;
            float elapsed = now - n.StartTime;
            float slideT = Mathf.Clamp01(elapsed / NotificationSlideInSeconds);
            slideT = slideT * slideT * (3f - 2f * slideT);

            float fadeOut = remaining < NotificationFadeSeconds ? remaining / NotificationFadeSeconds : 1f;
            float alpha = Mathf.Min(slideT, fadeOut);

            NotificationStyle style = _notificationStyleMode.Value;
            bool compact = style == NotificationStyle.ThemedCompact || style == NotificationStyle.MinimalCompact
                || style == NotificationStyle.StripCompact;
            bool showChrome = style == NotificationStyle.Themed || style == NotificationStyle.ThemedCompact
                || style == NotificationStyle.Plate;
            bool minimal = style == NotificationStyle.Minimal || style == NotificationStyle.MinimalCompact
                || style == NotificationStyle.Strip || style == NotificationStyle.StripCompact;

            // Instrument-only looks. Strip is a single accent rule with the text riding on it;
            // Plate is the full chassis with stencil corners on every palette, not just BEAR.
            bool stripStyle = style == NotificationStyle.Strip || style == NotificationStyle.StripCompact;
            bool plateStyle = style == NotificationStyle.Plate;

            if ((stripStyle || plateStyle) && !UiStyleState.IsInstrument)
            {
                // The extra looks belong to Instrument. Under Classic they fall back to the nearest
                // frozen equivalent rather than rendering something Classic never had.
                compact = style == NotificationStyle.StripCompact;
                showChrome = plateStyle;
                minimal = stripStyle;
                stripStyle = false;
                plateStyle = false;
            }

            alpha *= _notificationOpacity.Value;

            // Notifications can run a different faction theme than the window.
            MilStyle.Palette pal = _notificationTheme.Value == NotificationTheme.FollowWindow
                ? MilStyle.GetPalette(MilStyle.IsBear)
                : MilStyle.GetPalette(_notificationTheme.Value == NotificationTheme.BEAR);

            float boxWidth = compact ? 250f : 340f;
            float lineHeight = compact ? 18f : 22f;
            const float paddingH = 10f;
            float paddingV = compact ? 5f : 7f;
            float accentWidth = compact ? 3f : 4f;
            const float marginRight = 20f;

            float boxHeight = lineHeight + paddingV * 2f;
            float targetX = Screen.width - boxWidth - marginRight;
            float startX = Screen.width + 20f;
            float x = Mathf.Lerp(startX, targetX, slideT);
            float y = Screen.height - marginBottom - boxHeight - slotFromBottom * (boxHeight + 4f);
            Rect box = new Rect(x, y, boxWidth, boxHeight);

            Color prevGuiColor = GUI.color;

            Color accentColor = n.Color;
            accentColor.a = alpha;

            // Chassis. Colours always come from the theme; the style decides how much is drawn.
            // Instrument uses the generated rounded panel and a gradient over it, the same treatment
            // the HUD chassis gets — a notification is a small instrument, not a coloured box.
            bool textured = UiStyleState.IsInstrument && !minimal;

            if (textured)
            {
                DrawRoundedPanel(box,
                    new Color(pal.Bg.r, pal.Bg.g, pal.Bg.b, 0.93f * alpha),
                    new Color(pal.Border.r, pal.Border.g, pal.Border.b, alpha),
                    3f, UiTokens.Hairline);

                DrawGradient(new Rect(box.x + 1f, box.y + 1f, box.width - 2f, box.height - 2f),
                    new Color(1f, 1f, 1f, 0.05f * alpha), new Color(0f, 0f, 0f, 0.14f * alpha));

                // Halo behind the accent spine, so the type colour reads as a lit edge rather than
                // as a painted stripe. This is the part a flat rectangle simply cannot do.
                DrawGlow(new Rect(box.x - 8f, box.y - 4f, 26f, box.height + 8f),
                    new Color(accentColor.r, accentColor.g, accentColor.b, 0.35f * alpha));
            }
            else
            {
                GUI.color = new Color(pal.Bg.r, pal.Bg.g, pal.Bg.b, (showChrome ? 0.93f : 0.8f) * alpha);
                GUI.DrawTexture(box, Texture2D.whiteTexture);

                if (showChrome)
                {
                    GUI.color = new Color(pal.Panel.r, pal.Panel.g, pal.Panel.b, 0.85f * alpha);
                    GUI.DrawTexture(new Rect(box.x + accentWidth, box.y, box.width - accentWidth, box.height), Texture2D.whiteTexture);
                }
            }

            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(box.x, box.y, accentWidth, box.height), Texture2D.whiteTexture);

            // The rounded panel already carries its own border, so the manual one would double it.
            if (showChrome && !textured)
            {
                GUI.color = new Color(pal.Border.r, pal.Border.g, pal.Border.b, alpha);
                GUI.DrawTexture(new Rect(box.x, box.y, box.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.x, box.yMax - 1f, box.width, 1f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.xMax - 1f, box.y, 1f, box.height), Texture2D.whiteTexture);
            }

            if (stripStyle)
            {
                // A single rule instead of a box. The accent still carries the message type, which
                // is why the type tag is dropped: colour already says it.
                GUI.color = accentColor;
                GUI.DrawTexture(new Rect(box.x + accentWidth, box.yMax - UiTokens.Rule,
                    box.width - accentWidth, UiTokens.Rule), Texture2D.whiteTexture);
            }

            if (plateStyle && UiStyleState.IsInstrument)
            {
                // Plate is the emphatic style, so its corners get their own glow rather than
                // relying on the one behind the accent spine.
                DrawGlow(new Rect(box.xMax - 22f, box.y - 6f, 30f, 30f),
                    new Color(pal.SignalBright.r, pal.SignalBright.g, pal.SignalBright.b, 0.3f * alpha));
            }

            if ((showChrome && pal.IsBear) || plateStyle)
            {
                // Stencil corner ticks, matching the recordings window chassis.
                GUI.color = new Color(pal.SignalBright.r, pal.SignalBright.g, pal.SignalBright.b, alpha);
                GUI.DrawTexture(new Rect(box.xMax - 9f, box.y, 9f, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.xMax - 2f, box.y, 2f, 9f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.xMax - 9f, box.yMax - 2f, 9f, 2f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(box.xMax - 2f, box.yMax - 9f, 2f, 9f), Texture2D.whiteTexture);
            }

            if (showChrome)
            {
                // Remaining-time bar: shows at a glance how long the message will stay up.
                // Against the notification own duration, not the global default — a five-second
                // warning measured on a 2.5-second scale would sit full for half its life.
                float lifeFraction = Mathf.Clamp01(remaining / Mathf.Max(0.01f, n.Duration));
                GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f * alpha);
                GUI.DrawTexture(new Rect(box.x + accentWidth, box.yMax - 2f, (box.width - accentWidth) * lifeFraction, 2f), Texture2D.whiteTexture);
            }

            GUI.color = prevGuiColor;

            // Type tag: lets the message class be read at a glance without parsing the text.
            // Minimal drops it entirely; the accent colour still carries the type.
            float textX = box.x + accentWidth + paddingH;

            if (!minimal)
            {
                float tagWidth = compact ? 36f : 44f;
                Rect tagRect = new Rect(box.x + accentWidth + 6f, box.y + paddingV + 1f, tagWidth, lineHeight - 2f);

                GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.22f * alpha);
                GUI.DrawTexture(tagRect, Texture2D.whiteTexture);
                GUI.color = accentColor;
                GUI.DrawTexture(new Rect(tagRect.x, tagRect.y, 1f, tagRect.height), Texture2D.whiteTexture);
                GUI.color = prevGuiColor;

                Color tagColor = accentColor;
                tagColor.a = alpha;
                _notificationTagStyle.normal.textColor = tagColor;
                GUI.Label(tagRect, NotifyTag(n.Kind), _notificationTagStyle);

                textX = tagRect.xMax + 8f;
            }

            GUI.color = prevGuiColor;
            // Font size follows the style, so it has to be refreshed rather than set once at
            // creation — the option can be changed at any time from the F12 menu.
            _notificationStyle.fontSize = compact ? 11 : 13;
            _notificationTagStyle.fontSize = compact ? 9 : 10;

            Rect textRect = new Rect(textX, box.y + paddingV, box.xMax - textX - paddingH, lineHeight);

            // White is the "no explicit colour" default; render those in the theme's text colour
            // so plain messages stop looking like stock Unity labels.
            // The accent already carries the colour coding, so the message itself stays neutral
            // and legible rather than being tinted.
            Color textColor = pal.TextPrimary;
            textColor.a = alpha;

            Rect shadowRect = new Rect(textRect.x + 1f, textRect.y + 1f, textRect.width, textRect.height);
            Color shadowColor = pal.Bg;
            shadowColor.a = alpha * 0.8f;
            _notificationStyle.normal.textColor = shadowColor;
            GUI.Label(shadowRect, n.Message, _notificationStyle);

            _notificationStyle.normal.textColor = textColor;
            GUI.Label(textRect, n.Message, _notificationStyle);
        }
    }
}
