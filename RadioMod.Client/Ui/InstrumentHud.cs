using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// The Instrument HUD: one chassis in the corner instead of four independent groups.
    ///
    /// Two things separate it from Classic beyond the paint. First, state is carried by shape as
    /// well as colour — Classic distinguishes transmitting, receiving, duplex and power purely by
    /// the colour of a dot, which fails at a glance and fails entirely for a colour-blind player.
    /// Second, sizes and rules come from <see cref="UiTokens"/>, so the panel does not turn into
    /// hairlines on a 4K display.
    ///
    /// Classic's own drawing is untouched; this runs only when UI Style is Instrument.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>Signal readout styles that exist only in the Instrument look.</summary>
        internal enum InstrumentSignalStyle
        {
            SMeter,
            Bars,
            Dbm,
            ArcGauge,
            VuNeedle,
        }

        /// <summary>
        /// How the TX / RX / DPX / PWR row is presented. Instrument only; Classic keeps its own
        /// five-value IndicatorStyle untouched.
        ///
        /// Every one of these carries state by shape as well as colour, which is the whole reason
        /// the row exists — Classic distinguishes the four states by dot colour alone.
        /// </summary>
        internal enum InstrumentStateStyle
        {
            /// <summary>Lit and unlit labels. The most legible, and the default.</summary>
            LcdReadout,

            /// <summary>Round lamps, closest to the classic look.</summary>
            Dots,

            /// <summary>Angular segments — the same information, harder edges.</summary>
            Blocks,

            /// <summary>Boxed captions, like markings stencilled onto a panel.</summary>
            Stencil,

            /// <summary>Filled plates: the loudest, for players who want it readable at a glance.</summary>
            Pills,
        }

        /// <summary>Charge presentation in the Instrument look.</summary>
        internal enum InstrumentBatteryStyle
        {
            Segments,
            Percent,
            Volts,
        }

        private BepInEx.Configuration.ConfigEntry<InstrumentStateStyle> _instrumentStateStyle;
        private BepInEx.Configuration.ConfigEntry<InstrumentBatteryStyle> _instrumentBatteryStyle;

        private BepInEx.Configuration.ConfigEntry<InstrumentSignalStyle> _instrumentSignalStyle;
        private BepInEx.Configuration.ConfigEntry<bool> _showRadioNameplate;
        private BepInEx.Configuration.ConfigEntry<float> _previewScale;

        /// <summary>
        /// Charge readouts only mean something while ozen-BatteriesNotIncluded is installed. Without
        /// it the radios never discharge, so every battery control and row is hidden rather than
        /// showing a permanent 100%.
        /// </summary>
        private static bool BatteryReadoutAvailable => TierAttributePatch.IsBatteryModPresent();

        private const float ChassisWidth = 236f;
        private const float ChassisMarginRight = 20f;
        private const float ChassisMarginBottom = 36f;

        /// <summary>
        /// Everything the chassis needs to draw itself, and nothing else. Small on purpose: this is
        /// the seam that lets the same renderer serve the raid and the settings preview, so the
        /// preview cannot drift from the real thing — it is not a separate mock-up, it is the same
        /// code fed different numbers.
        /// </summary>
        private struct HudState
        {
            public bool Talking;
            public bool Busy;
            public bool RadioOn;
            public bool Duplex;
            public bool ShowSignal;
            public bool ShowBattery;
            public float Quality;
            public float Battery;
            public float[] TxBands;
            public float[] RxBands;
            public string Model;
            public string Tier;
        }

        /// <summary>
        /// Draws the whole HUD. Returns the topmost Y it reached so notifications can clear it,
        /// mirroring what <c>DrawIndicators</c> reports through <c>_indicatorStackTopY</c>.
        /// </summary>
        private void DrawInstrumentIndicators()
        {
            if (_radioLocation != RadioLocation.Ready || IsEscMenuOpen())
            {
                _indicatorStackTopY = Screen.height;
                return;
            }

            bool channelBusy = RadioSpeakerNames.Count > 0;
            bool showTalking = _showTalkingIndicator.Value && _txChannel != null;
            bool showBusy = _showBusyIndicator.Value && channelBusy;
            float quality = 1f;
            bool showSignal = _showSignalIndicator.Value && channelBusy && TryGetBestSignalQuality(out quality);

            // The Alinco meter twitches on its own inside the Labyrinth. Independent of the whisper
            // on purpose: a needle that moved exactly when something was heard would be honest.
            if (!showSignal && _showSignalIndicator.Value && AnomalyIndicatorLying(out float lie))
            {
                showSignal = true;
                quality = lie;
            }

            if (showTalking || channelBusy)
            {
                _lastIndicatorActivityTime = Time.unscaledTime;
            }

            float opacity = IndicatorAlpha();
            if (opacity <= 0.01f)
            {
                _indicatorStackTopY = Screen.height;
                return;
            }

            // Without ozen-BatteriesNotIncluded radios never run out, so a charge readout would be
            // showing a number that means nothing. The row does not exist at all in that case.
            float battery = 0f;
            bool showBattery = BatteryReadoutAvailable
                && _showBatteryIndicator.Value
                && _radioOn
                && TryGetRadioBatteryCharge(out battery);

            string tpl = _activeRadioTplId ?? _selectedRadioTplId;

            if (showTalking) { UpdateVuBands(_txBands, _localMicRecorder.InputLevel); }
            if (showBusy) { UpdateVuBands(_rxBands, GetLoudestRemoteLevel()); }

            HudState state = new HudState
            {
                Talking = showTalking,
                Busy = showBusy,
                RadioOn = _radioOn,
                Duplex = _radioOn && _duplexMode != DuplexMode.Simplex,
                ShowSignal = showSignal,
                ShowBattery = showBattery,
                Quality = quality,
                Battery = battery,
                TxBands = _txBands,
                RxBands = _rxBands,
                Model = AnomalyCorruptedModel(tpl),
                Tier = TierAttributePatch.GetTier(tpl),
            };

            float h = ChassisHeight(state);
            Rect chassis = new Rect(
                Screen.width - ChassisMarginRight - ChassisWidth,
                Screen.height - ChassisMarginBottom - h,
                ChassisWidth,
                h);

            DrawInstrumentChassis(chassis, state, opacity);
            _indicatorStackTopY = chassis.y - 6f;
        }

        /// <summary>
        /// Height built from what is actually shown, so hiding a row shrinks the chassis instead of
        /// leaving a hole — the same promise Classic's visibility toggles make.
        /// </summary>
        private float ChassisHeight(HudState s)
        {
            float h = UiTokens.GapUnit * 2f + 16f;
            if (_showRadioNameplate.Value) { h += 14f; }
            if (s.ShowSignal) { h += 22f; }
            if (s.ShowBattery) { h += 16f; }
            if (s.Talking || s.Busy) { h += 18f; }
            return h;
        }

        /// <summary>Draws the chassis anywhere. Used by both the raid HUD and the settings preview.</summary>
        private void DrawInstrumentChassis(Rect chassis, HudState s, float opacity)
        {
            Color prev = GUI.color;
            DrawChassis(chassis, opacity);

            float cursorY = chassis.y + UiTokens.GapUnit;
            float innerX = chassis.x + UiTokens.GapUnit + 2f;
            float innerW = chassis.width - (UiTokens.GapUnit + 2f) * 2f;

            if (_showRadioNameplate.Value)
            {
                DrawNameplate(new Rect(innerX, cursorY, innerW, 14f), s, opacity);
                cursorY += 14f;
            }

            DrawStateReadout(new Rect(innerX, cursorY, innerW, 16f), s, opacity);
            cursorY += 16f;

            if (s.ShowSignal)
            {
                DrawInstrumentSignal(new Rect(innerX, cursorY, innerW, 22f), s.Quality, opacity);
                cursorY += 22f;
            }

            if (s.ShowBattery)
            {
                DrawInstrumentBattery(new Rect(innerX, cursorY, innerW, 16f), s.Battery, opacity);
                cursorY += 16f;
            }

            if (s.Talking || s.Busy)
            {
                DrawInstrumentLevels(new Rect(innerX, cursorY, innerW, 18f), s, opacity);
            }

            GUI.color = prev;
        }

        private readonly float[] _previewTx = new float[VuBandCount];
        private readonly float[] _previewRx = new float[VuBandCount];

        /// <summary>
        /// Live preview for the settings tab. It is the same renderer as the raid HUD, fed synthetic
        /// numbers — so what the player tunes here is exactly what they will see in a raid, and the
        /// preview cannot silently drift from reality the way a hand-drawn mock-up would.
        ///
        /// The point of it is practical: without this, the only way to judge the HUD is to load into
        /// a raid and wait for somebody to talk.
        /// </summary>
        private void DrawInstrumentPreview(Rect area)
        {
            float t = Time.unscaledTime;

            // Synthetic traffic: speech-like level for TX, a slower conversation for RX, and a
            // signal that sweeps so every part of the S-meter gets exercised.
            float txLevel = Mathf.Clamp01(0.35f + 0.45f * Mathf.Abs(Mathf.Sin(t * 5.1f)) * Mathf.Abs(Mathf.Sin(t * 1.7f)));
            float rxLevel = Mathf.Clamp01(0.25f + 0.5f * Mathf.Abs(Mathf.Sin(t * 3.3f + 1f)));
            UpdateVuBands(_previewTx, txLevel);
            UpdateVuBands(_previewRx, rxLevel);

            string tpl = _selectedRadioTplId ?? _activeRadioTplId;

            HudState s = new HudState
            {
                Talking = true,
                Busy = true,
                RadioOn = true,
                Duplex = true,
                ShowSignal = _showSignalIndicator.Value,
                ShowBattery = BatteryReadoutAvailable && _showBatteryIndicator.Value,
                Quality = 0.5f + 0.5f * Mathf.Sin(t * 0.6f),
                Battery = 0.62f,
                TxBands = _previewTx,
                RxBands = _previewRx,
                Model = tpl != null ? GetRadioDisplayName(tpl).ToUpperInvariant() : "AN/PRC-152",
                Tier = tpl != null ? TierAttributePatch.GetTier(tpl) : "S",
            };

            float h = ChassisHeight(s);

            // Scale is capped to what the slot can actually show. At the default 2x the chassis is
            // 472 px wide while the stand offers 276 - the drawing ran off the edge and was clipped
            // by the surrounding area, which looks exactly like a broken preview rather than a
            // magnified one. Fitting the width keeps the whole set visible at any stand size.
            float scale = PreviewScale;
            if (area.width > 1f)
            {
                scale = Mathf.Min(scale, area.width / ChassisWidth);
            }

            // Drawn at true size, then scaled around its own top-left: at 1:1 a 236px panel is a
            // postage stamp on a large display, and the point of the stand is to judge details.
            Rect chassis = new Rect(
                area.x + Mathf.Max(0f, (area.width - ChassisWidth * scale) / 2f),
                area.y,
                ChassisWidth,
                h);

            Matrix4x4 prevMatrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), new Vector2(chassis.x, chassis.y));
            DrawInstrumentChassis(chassis, s, _indicatorOpacity.Value);
            GUI.matrix = prevMatrix;
        }

        /// <summary>Height the preview will occupy, so the settings layout can reserve room for it.</summary>
        /// <summary>How much the settings stand magnifies the chassis.</summary>
        private float PreviewScale => Mathf.Clamp(_previewScale != null ? _previewScale.Value : 2f, 1f, 4f);

        /// <summary>
        /// Height the preview will occupy. Takes the slot width so the reserved space matches the
        /// scale the preview will actually use - reserving for 2x and then drawing at 1.2x leaves a
        /// hole under the chassis.
        /// </summary>
        private float InstrumentPreviewHeight(float availableWidth = 0f)
        {
            float scale = PreviewScale;
            if (availableWidth > 1f)
            {
                scale = Mathf.Min(scale, availableWidth / ChassisWidth);
            }

            return scale * ChassisHeight(new HudState
            {
                Talking = true,
                Busy = true,
                ShowSignal = _showSignalIndicator.Value,
                ShowBattery = BatteryReadoutAvailable && _showBatteryIndicator.Value,
            });
        }

        /// <summary>Model name, corrupted while the Alinco anomaly is running.</summary>
        private string AnomalyCorruptedModel(string tpl)
        {
            string model = tpl != null ? GetRadioDisplayName(tpl).ToUpperInvariant() : "—";
            return AlincoAnomalyActive ? CorruptText(model, 0.12f) : model;
        }

        /// <summary>Housing: fill, frame and stencil corner ticks, all on resolution-aware rules.</summary>
        private void DrawChassis(Rect r, float opacity)
        {
            Color prev = GUI.color;

            // Body: a rounded bordered panel instead of a flat rectangle with four hairlines, and a
            // vertical gradient over it. Both are what a moulded housing actually looks like under
            // a light, and neither is expressible without a generated texture.
            Color fill = new Color(UiTokens.Chassis.r, UiTokens.Chassis.g, UiTokens.Chassis.b, 0.9f * opacity);
            Color edge = new Color(UiTokens.Edge.r, UiTokens.Edge.g, UiTokens.Edge.b, 0.95f * opacity);
            DrawRoundedPanel(r, fill, edge, 3f, UiTokens.Hairline);

            Color top = new Color(1f, 1f, 1f, 0.05f * opacity);
            Color bottom = new Color(0f, 0f, 0f, 0.16f * opacity);
            DrawGradient(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), top, bottom);

            // Stencil corners, with a soft halo behind them so they read as painted-on marking
            // catching the light rather than as two stray rectangles.
            float len = 10f;
            float thick = UiTokens.Rule;
            Color mark = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);

            DrawGlow(new Rect(r.x - 6f, r.y - 6f, 26f, 26f), new Color(mark.r, mark.g, mark.b, 0.22f * opacity));
            DrawGlow(new Rect(r.xMax - 20f, r.yMax - 20f, 26f, 26f), new Color(mark.r, mark.g, mark.b, 0.22f * opacity));

            GUI.color = mark;
            GUI.DrawTexture(new Rect(r.x, r.y, len, thick), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x, r.y, thick, len), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - len, r.yMax - thick, len, thick), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - thick, r.yMax - len, thick, len), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private GUIStyle _instPlate;
        private GUIStyle _instTier;
        private GUIStyle _instState;
        private GUIStyle _instValue;

        private void EnsureInstrumentStyles()
        {
            if (_instPlate != null)
            {
                return;
            }

            _instPlate = new GUIStyle(GUI.skin.label)
            {
                fontSize = UiTokens.SizeMicro,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
            };
            _instTier = new GUIStyle(_instPlate) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            _instState = new GUIStyle(_instPlate) { fontSize = UiTokens.SizeSmall, fontStyle = FontStyle.Bold };
            _instValue = new GUIStyle(_instPlate) { fontSize = UiTokens.SizeSmall, alignment = TextAnchor.MiddleRight };

            UiTokens.WithFont(_instPlate);
            UiTokens.WithFont(_instState);
            UiTokens.WithFont(_instValue);
            if (UiTokens.FontBold != null) { _instTier.font = UiTokens.FontBold; }
        }

        /// <summary>Model and tier of the radio actually in use — the plate on a real set.</summary>
        private void DrawNameplate(Rect r, HudState s, float opacity)
        {
            EnsureInstrumentStyles();

            string model = s.Model;
            string tier = s.Tier;

            _instPlate.normal.textColor = new Color(UiTokens.Dim.r, UiTokens.Dim.g, UiTokens.Dim.b, opacity);
            GUI.Label(new Rect(r.x, r.y, r.width - 26f, r.height), model, _instPlate);

            if (!string.IsNullOrEmpty(tier))
            {
                _instTier.normal.textColor = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);
                GUI.Label(new Rect(r.xMax - 26f, r.y, 26f, r.height), tier, _instTier);
            }
        }

        /// <summary>
        /// TX · RX · DPX · PWR. Each label is either lit or barely visible, so the state is readable
        /// from the word itself and not only from a colour — which is the point of this row.
        /// </summary>
        private void DrawStateReadout(Rect r, HudState s, float opacity)
        {
            EnsureInstrumentStyles();

            string[] labels = { "TX", "RX", "DPX", "PWR" };
            bool[] lit =
            {
                s.Talking,
                s.Busy,
                s.Duplex,
                s.RadioOn,
            };
            Color[] tint =
            {
                _colorTalking.Value,
                _colorBusy.Value,
                _colorSimplex.Value,
                _colorOn.Value,
            };

            InstrumentStateStyle style = _instrumentStateStyle != null
                ? _instrumentStateStyle.Value
                : InstrumentStateStyle.LcdReadout;

            float slot = r.width / labels.Length;
            Color prev = GUI.color;

            for (int i = 0; i < labels.Length; i++)
            {
                Rect cell = new Rect(r.x + i * slot, r.y, slot, r.height);
                Color on = new Color(tint[i].r, tint[i].g, tint[i].b, opacity);
                Color off = new Color(UiTokens.Dim.r, UiTokens.Dim.g, UiTokens.Dim.b, 0.32f * opacity);
                Color c = lit[i] ? on : off;

                switch (style)
                {
                    case InstrumentStateStyle.Dots:
                        DrawStateLamp(cell, labels[i], c, lit[i], opacity, round: true);
                        break;

                    case InstrumentStateStyle.Blocks:
                        DrawStateLamp(cell, labels[i], c, lit[i], opacity, round: false);
                        break;

                    case InstrumentStateStyle.Stencil:
                        DrawStateStencil(cell, labels[i], c, lit[i], opacity);
                        break;

                    case InstrumentStateStyle.Pills:
                        DrawStatePill(cell, labels[i], c, lit[i], opacity);
                        break;

                    default:
                        _instState.normal.textColor = c;
                        GUI.Label(cell, labels[i], _instState);
                        break;
                }
            }

            GUI.color = prev;
        }

        /// <summary>
        /// Lamp with its caption beside it. The caption is what keeps this readable without colour —
        /// a bare coloured dot is exactly the Classic problem this style set exists to fix.
        /// </summary>
        private void DrawStateLamp(Rect cell, string label, Color c, bool lit, float opacity, bool round)
        {
            const float d = 8f;
            Rect lamp = new Rect(cell.x, cell.y + (cell.height - d) / 2f, d, d);

            if (lit)
            {
                DrawGlow(new Rect(lamp.x - 5f, lamp.y - 5f, d + 10f, d + 10f),
                    new Color(c.r, c.g, c.b, 0.4f * opacity));
            }

            GUI.color = c;
            if (round)
            {
                GUI.DrawTexture(lamp, GetIndicatorDotTexture());
            }
            else
            {
                GUI.DrawTexture(lamp, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            _instState.normal.textColor = c;
            GUI.Label(new Rect(cell.x + d + 4f, cell.y, cell.width - d - 4f, cell.height), label, _instState);
        }

        /// <summary>Caption inside a thin box, the way markings are stencilled onto a panel.</summary>
        private void DrawStateStencil(Rect cell, string label, Color c, bool lit, float opacity)
        {
            Rect box = new Rect(cell.x, cell.y + 1f, cell.width - 4f, cell.height - 2f);
            float rule = UiTokens.Hairline;

            GUI.color = new Color(c.r, c.g, c.b, lit ? c.a : c.a * 0.6f);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, rule), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.yMax - rule, box.width, rule), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.y, rule, box.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.xMax - rule, box.y, rule, box.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            _instState.normal.textColor = c;
            _instState.alignment = TextAnchor.MiddleCenter;
            GUI.Label(box, label, _instState);
            _instState.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>Filled plate when lit, hollow when not. The loudest of the five.</summary>
        private void DrawStatePill(Rect cell, string label, Color c, bool lit, float opacity)
        {
            Rect box = new Rect(cell.x, cell.y + 1f, cell.width - 4f, cell.height - 2f);

            if (lit)
            {
                DrawGlow(new Rect(box.x - 6f, box.y - 4f, box.width + 12f, box.height + 8f),
                    new Color(c.r, c.g, c.b, 0.3f * opacity));
                DrawRoundedPanel(box, c, new Color(c.r, c.g, c.b, opacity), 2f, UiTokens.Hairline);
            }
            else
            {
                DrawRoundedPanel(box, new Color(c.r, c.g, c.b, 0.12f * opacity),
                    new Color(c.r, c.g, c.b, 0.5f * opacity), 2f, UiTokens.Hairline);
            }

            // Lit plates carry dark ink; unlit ones keep the accent colour on a near-empty field.
            _instState.normal.textColor = lit ? new Color(MilStyle.Ink.r, MilStyle.Ink.g, MilStyle.Ink.b, opacity) : c;
            _instState.alignment = TextAnchor.MiddleCenter;
            GUI.Label(box, label, _instState);
            _instState.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>
        /// Signal strength. All three styles are views of one number — S-units and dBm are derived
        /// from the same quality value, so the needle and the figure can never disagree.
        /// </summary>
        private void DrawInstrumentSignal(Rect r, float quality, float opacity)
        {
            EnsureInstrumentStyles();

            float strength = Mathf.Clamp01(quality);
            float sUnits = strength * 9f;
            int dbm = Mathf.RoundToInt(-93f - (9f - sUnits) * 6f);

            InstrumentSignalStyle style = _instrumentSignalStyle != null
                ? _instrumentSignalStyle.Value
                : InstrumentSignalStyle.SMeter;

            Color fill = _colorSignalBar.Value;
            fill.a *= opacity;

            switch (style)
            {
                case InstrumentSignalStyle.Dbm:
                    GUI.color = Color.white;
                    _instValue.normal.textColor = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);
                    _instPlate.normal.textColor = new Color(UiTokens.Dim.r, UiTokens.Dim.g, UiTokens.Dim.b, opacity);
                    GUI.Label(new Rect(r.x, r.y, 60f, r.height), "SIGNAL", _instPlate);
                    GUI.Label(new Rect(r.xMax - 80f, r.y, 80f, r.height), dbm + " dBm", _instValue);
                    break;

                case InstrumentSignalStyle.Bars:
                    DrawSegmentBar(new Rect(r.x, r.y + 6f, r.width, 8f), strength, fill, opacity);
                    break;

                case InstrumentSignalStyle.ArcGauge:
                    DrawArcGauge(r, strength, dbm, fill, opacity);
                    break;

                case InstrumentSignalStyle.VuNeedle:
                    DrawVuNeedle(r, strength, sUnits, fill, opacity);
                    break;

                default:
                    DrawSMeter(r, strength, sUnits, dbm, opacity);
                    break;
            }
        }

        /// <summary>
        /// Arc gauge: a thick track with the lit portion drawn over it. Built from short segments
        /// rather than a real curve — IMGUI has no line drawing, and a chain of small rectangles
        /// along the arc is indistinguishable from one at this size.
        /// </summary>
        private void DrawArcGauge(Rect r, float strength, int dbm, Color fill, float opacity)
        {
            EnsureInstrumentStyles();

            float cx = r.x + 30f;
            float cy = r.yMax - 2f;
            const float radius = 20f;
            const int steps = 24;

            for (int i = 0; i < steps; i++)
            {
                float t = i / (float)(steps - 1);
                float ang = Mathf.Lerp(-140f, -40f, t) * Mathf.Deg2Rad;
                float px = cx + Mathf.Cos(ang) * radius;
                float py = cy + Mathf.Sin(ang) * radius;

                bool on = t <= strength;
                GUI.color = on
                    ? new Color(fill.r, fill.g, fill.b, fill.a)
                    : new Color(UiTokens.Edge.r, UiTokens.Edge.g, UiTokens.Edge.b, 0.5f * opacity);

                GUI.DrawTexture(new Rect(px - 1.5f, py - 1.5f, 3.5f, 3.5f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            _instValue.normal.textColor = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);
            GUI.Label(new Rect(r.xMax - 72f, r.y, 72f, r.height), dbm + " dBm", _instValue);
        }

        /// <summary>
        /// Analogue VU: a boxed face with a needle swinging from a pivot. The box matters — without
        /// a frame the needle reads as a stray diagonal rather than as an instrument.
        /// </summary>
        private void DrawVuNeedle(Rect r, float strength, float sUnits, Color fill, float opacity)
        {
            EnsureInstrumentStyles();

            Rect face = new Rect(r.x, r.y + 1f, 74f, r.height - 2f);
            DrawRoundedPanel(face,
                new Color(UiTokens.Chassis.r, UiTokens.Chassis.g, UiTokens.Chassis.b, 0.8f * opacity),
                new Color(UiTokens.Edge.r, UiTokens.Edge.g, UiTokens.Edge.b, 0.9f * opacity),
                2f, UiTokens.Hairline);

            float pivotX = face.x + face.width / 2f;
            float pivotY = face.yMax - 3f;
            float ang = Mathf.Lerp(-60f, 60f, Mathf.Clamp01(strength)) * Mathf.Deg2Rad;

            // Drawn as a stack of short segments from the pivot outward.
            const int len = 14;
            for (int i = 2; i < len; i++)
            {
                float px = pivotX + Mathf.Sin(ang) * i;
                float py = pivotY - Mathf.Cos(ang) * i;
                GUI.color = new Color(fill.r, fill.g, fill.b, fill.a);
                GUI.DrawTexture(new Rect(px - 1f, py - 1f, 2f, 2f), Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            _instValue.normal.textColor = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);
            GUI.Label(new Rect(r.x + 78f, r.y, r.width - 78f, r.height), "S" + Mathf.RoundToInt(sUnits), _instValue);
        }

        private void DrawSegmentBar(Rect r, float value, Color fill, float opacity)
        {
            const int cells = 10;
            float gap = UiTokens.Hairline;
            float cellW = (r.width - gap * (cells - 1)) / cells;
            float lit = value * cells;

            for (int i = 0; i < cells; i++)
            {
                float amount = Mathf.Clamp01(lit - i);
                GUI.color = amount > 0f
                    ? new Color(fill.r, fill.g, fill.b, fill.a * (0.4f + 0.6f * amount))
                    : new Color(UiTokens.Edge.r, UiTokens.Edge.g, UiTokens.Edge.b, 0.5f * opacity);
                GUI.DrawTexture(new Rect(r.x + i * (cellW + gap), r.y, cellW, r.height), Texture2D.whiteTexture);
            }

            // GUI.color multiplies into every label drawn afterwards, so it has to be handed back.
            // Leaving it set is what tinted the battery row differently per signal style.
            GUI.color = Color.white;
        }

        /// <summary>Needle across an S1–S9 scale, with the same figure shown as dBm alongside.</summary>
        private void DrawSMeter(Rect r, float strength, float sUnits, int dbm, float opacity)
        {
            EnsureInstrumentStyles();

            float scaleW = r.width - 74f;
            Rect scale = new Rect(r.x, r.y + 4f, scaleW, 14f);

            // Tick scale: taller marks at S3, S6, S9 so the needle can be read without numbers.
            for (int i = 0; i <= 9; i++)
            {
                float t = i / 9f;
                bool major = i % 3 == 0;
                float th = major ? 8f : 5f;
                GUI.color = new Color(UiTokens.Dim.r, UiTokens.Dim.g, UiTokens.Dim.b, (major ? 0.75f : 0.4f) * opacity);
                GUI.DrawTexture(new Rect(scale.x + t * (scale.width - UiTokens.Hairline), scale.yMax - th,
                    UiTokens.Hairline, th), Texture2D.whiteTexture);
            }

            GUI.color = new Color(UiTokens.Edge.r, UiTokens.Edge.g, UiTokens.Edge.b, 0.8f * opacity);
            GUI.DrawTexture(new Rect(scale.x, scale.yMax, scale.width, UiTokens.Hairline), Texture2D.whiteTexture);

            float needleX = scale.x + Mathf.Clamp01(strength) * (scale.width - UiTokens.Rule);
            Color fill = _colorSignalBar.Value;

            GUI.color = new Color(fill.r, fill.g, fill.b, 0.22f * opacity);
            GUI.DrawTexture(new Rect(needleX - UiTokens.Rule, scale.y - 1f, UiTokens.Rule * 3f, scale.height + 2f), Texture2D.whiteTexture);

            GUI.color = new Color(fill.r, fill.g, fill.b, opacity);
            GUI.DrawTexture(new Rect(needleX, scale.y - 1f, UiTokens.Rule, scale.height + 2f), Texture2D.whiteTexture);

            GUI.color = Color.white;
            _instValue.normal.textColor = new Color(UiTokens.Signal.r, UiTokens.Signal.g, UiTokens.Signal.b, opacity);
            GUI.Label(new Rect(r.xMax - 72f, r.y, 72f, r.height),
                "S" + Mathf.RoundToInt(sUnits) + "  " + dbm + "dBm", _instValue);
        }

        private void DrawInstrumentBattery(Rect r, float fraction, float opacity)
        {
            EnsureInstrumentStyles();

            Color charge = BatteryChargeColor(fraction);
            GUI.color = Color.white;
            _instPlate.normal.textColor = new Color(UiTokens.Dim.r, UiTokens.Dim.g, UiTokens.Dim.b, opacity);
            GUI.Label(new Rect(r.x, r.y, 40f, r.height), "BAT", _instPlate);

            InstrumentBatteryStyle style = _instrumentBatteryStyle != null
                ? _instrumentBatteryStyle.Value
                : InstrumentBatteryStyle.Segments;

            // Percent and Volts are numeric readouts and get the full width; Segments keeps the bar
            // with the figure alongside it, because a bar with no number is hard to read precisely.
            if (style == InstrumentBatteryStyle.Segments)
            {
                Rect bar = new Rect(r.x + 42f, r.y + 5f, r.width - 42f - 44f, 7f);
                GUI.color = new Color(UiTokens.Chassis.r, UiTokens.Chassis.g, UiTokens.Chassis.b, 0.9f * opacity);
                GUI.DrawTexture(new Rect(bar.x - 1f, bar.y - 1f, bar.width + 2f, bar.height + 2f), Texture2D.whiteTexture);
                DrawSegmentBar(bar, fraction, new Color(charge.r, charge.g, charge.b, opacity), opacity);
            }

            string readout;
            if (style == InstrumentBatteryStyle.Volts)
            {
                // Nominal pack voltage sagging towards 80% as the charge runs out — the shape of an
                // alkaline discharge curve, near enough for a readout. Falls back to a single 1.5 V
                // cell when no radio has been polled yet, which is the case in the settings preview.
                float pack = BatteryPackVolts > 0.01f ? BatteryPackVolts : 1.5f;
                readout = (pack * (0.8f + 0.2f * fraction)).ToString("0.0") + "V";
            }
            else
            {
                readout = Mathf.RoundToInt(fraction * 100f) + "%";
            }

            GUI.color = Color.white;
            _instValue.normal.textColor = new Color(charge.r, charge.g, charge.b, opacity);

            Rect valueRect = style == InstrumentBatteryStyle.Segments
                ? new Rect(r.xMax - 42f, r.y, 42f, r.height)
                : new Rect(r.x + 42f, r.y, r.width - 42f, r.height);

            GUI.Label(valueRect, readout, _instValue);
        }

        /// <summary>Live level meters for own transmission and incoming traffic, side by side.</summary>
        private void DrawInstrumentLevels(Rect r, HudState s, float opacity)
        {
            float half = (r.width - UiTokens.GapUnit) / 2f;

            if (s.Talking)
            {
                DrawLevelBlock(new Rect(r.x, r.y, half, r.height), "TX", s.TxBands, _colorTalking.Value, opacity);
            }

            if (s.Busy)
            {
                float bx = s.Talking ? r.x + half + UiTokens.GapUnit : r.x;
                float bw = s.Talking ? half : r.width;
                DrawLevelBlock(new Rect(bx, r.y, bw, r.height), "RX", s.RxBands, _colorBusy.Value, opacity);
            }
        }

        private void DrawLevelBlock(Rect r, string label, float[] bands, Color accent, float opacity)
        {
            EnsureInstrumentStyles();

            GUI.color = Color.white;
            _instPlate.normal.textColor = new Color(accent.r, accent.g, accent.b, opacity);
            GUI.Label(new Rect(r.x, r.y, 22f, r.height), label, _instPlate);

            Rect meter = new Rect(r.x + 24f, r.y + 3f, r.width - 24f, r.height - 6f);
            DrawVuMeter(meter, bands, accent, opacity);
        }
    }
}
