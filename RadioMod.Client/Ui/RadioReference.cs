using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// The RADIOS and COMPARE tabs.
    ///
    /// Every number shown here is read straight out of <see cref="RadioProfiles"/> — the same values
    /// the voice filter runs on. Nothing is duplicated into a table of its own, because a second copy
    /// of the characteristics would drift away from the balance the moment either side is touched.
    ///
    /// The mod already models thirteen radios in real detail; until now the player saw a letter.
    /// </summary>
    public partial class Plugin
    {
        // ---- journal columns ---------------------------------------------------------------------

        /// <summary>
        /// Column widths for the Instrument journal, as fractions of the text area. Time and duration
        /// are fixed-width by nature; the callsign gets whatever is left, because that is the field
        /// whose length actually varies.
        /// </summary>
        // Widths are set by the widest LOCALISED caption, not by the Russian one. "ПОЗЫВНОЙ",
        // "NOMINATIVO" and "RUFNAME" differ enough that sizing to any single language clips the
        // others — which is exactly what happened on the first pass.
        private const float ColTime = 74f;
        private const float ColDistance = 74f;
        private const float ColRadio = 118f;

        /// <summary>
        /// Header captions are set a size smaller than the data. They are read once to learn the
        /// layout and then ignored, so they can afford to be quieter — and it buys the room the
        /// longer translations need.
        /// </summary>
        private GUIStyle _journalHeadStyle;

        private GUIStyle JournalHeadStyle
        {
            get
            {
                if (_journalHeadStyle == null)
                {
                    _journalHeadStyle = new GUIStyle(MilStyle.DimLabel)
                    {
                        fontSize = UiTokens.SizeMicro,
                        alignment = TextAnchor.MiddleLeft,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                    };
                    UiTokens.WithFont(_journalHeadStyle);
                }

                return _journalHeadStyle;
            }
        }

        /// <summary>
        /// One clip as aligned columns instead of one run-on string.
        ///
        /// The composed label was readable enough on its own, but a list of twenty of them is not:
        /// nothing lines up, so the eye has to re-parse every row. Columns are what make a log
        /// scannable, which is the entire point of a log.
        /// </summary>
        /// <summary>
        /// Header for the journal columns. Offsets match <see cref="DrawJournalColumns"/> exactly —
        /// including the same 6 px inset the rows use — so the captions sit over their own columns.
        /// </summary>
        private void DrawJournalHeader(Rect area)
        {
            // Rows reserve this much on the right for the KEEP/DEL buttons.
            const float actionsWidth = 152f;
            float x = area.x + 6f;
            float width = Mathf.Max(10f, area.width - actionsWidth - 6f);

            // Below the width where the columns stop fitting, the header is dropped entirely rather
            // than printed as a row of clipped stubs. A narrow window then looks deliberate.
            if (width < ColTime + ColDistance + ColRadio + 60f)
            {
                return;
            }

            GUI.Label(new Rect(x, area.y, ColTime, area.height),
                L("ВРЕМЯ", "TIME", "ZEIT", "HORA", "HEURE", "CZAS", "ORA", "ČAS"), JournalHeadStyle);
            x += ColTime;

            float nameWidth = Mathf.Max(40f, width - ColTime - ColDistance - ColRadio);
            GUI.Label(new Rect(x, area.y, nameWidth, area.height),
                L("ПОЗЫВНОЙ", "CALLSIGN", "RUFNAME", "INDICATIVO", "INDICATIF", "ZNAK", "NOMINATIVO", "VOLAČKA"),
                JournalHeadStyle);
            x += nameWidth;

            GUI.Label(new Rect(x, area.y, ColDistance, area.height),
                L("ДИСТ", "DIST", "ENTF", "DIST", "DIST", "ODL", "DIST", "VZD"), JournalHeadStyle);
            x += ColDistance;

            GUI.Label(new Rect(x, area.y, ColRadio, area.height),
                L("РАЦИЯ", "RADIO", "GERÄT", "RADIO", "RADIO", "RADIO", "RADIO", "VYSÍLAČKA"), JournalHeadStyle);
        }

        /// <summary>
        /// Map filter bar above the journal, as an even grid rather than a wrapped row: with a
        /// wrapped row the second line ends wherever the text happens to stop and nothing lines up.
        ///
        /// Maps with no recordings today are shown greyed and unclickable instead of being hidden.
        /// A missing button reads as "the feature is gone"; a dim one reads as "nothing here yet".
        /// </summary>
        private void DrawLocationBar()
        {
            if (!UiStyleState.IsInstrument || _raidReviewAllClips == null || _raidReviewAllClips.Length == 0)
            {
                return;
            }

            // Counts come from the whole day, never from the already-filtered list — otherwise
            // picking a map would zero every other number on the bar.
            Dictionary<string, int> counts = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            foreach (RaidReviewClipInfo c in _raidReviewAllClips)
            {
                string loc = c.Location ?? "";
                counts[loc] = counts.TryGetValue(loc, out int n) ? n + 1 : 1;
            }

            List<string> maps = new List<string>(counts.Keys);
            maps.Sort(System.StringComparer.OrdinalIgnoreCase);

            const float minCell = 118f;
            int total = maps.Count + 1;

            Rect bar = GUILayoutUtility.GetRect(10f, 0f, GUILayout.ExpandWidth(true));
            int perRow = Mathf.Max(1, Mathf.FloorToInt(bar.width / minCell));
            int rows = Mathf.CeilToInt(total / (float)perRow);

            const float cellH = 20f;
            bar = GUILayoutUtility.GetRect(10f, rows * cellH, GUILayout.ExpandWidth(true));
            float cellW = bar.width / perRow;

            for (int i = 0; i < total; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                Rect cell = new Rect(bar.x + col * cellW, bar.y + row * cellH, cellW - 1f, cellH - 1f);

                bool isAll = i == 0;
                string map = isAll ? null : maps[i - 1];
                int count = isAll ? _raidReviewAllClips.Length : counts[map];
                bool active = isAll
                    ? string.IsNullOrEmpty(_raidReviewLocationFilter)
                    : string.Equals(_raidReviewLocationFilter, map, System.StringComparison.OrdinalIgnoreCase);

                string caption = (isAll
                    ? L("ВСЕ", "ALL", "ALLE", "TODAS", "TOUTES", "WSZYSTKIE", "TUTTE", "VŠE")
                    : map.ToUpperInvariant()) + "  " + count;

                if (GUI.Button(cell, caption, active ? MilStyle.Button : MilStyle.ButtonOff) && !active)
                {
                    _raidReviewLocationFilter = map;
                    ApplyRaidReviewFilterAndSort();
                }
            }

            GUILayout.Space(UiTokens.GapUnit);
        }

        private void DrawJournalColumns(Rect area, RaidReviewClipInfo clip, bool playing)
        {
            GUIStyle nameStyle = playing ? MilStyle.PlayingLabel : MilStyle.BodyLabel;

            float x = area.x;

            GUI.Label(new Rect(x, area.y, ColTime, area.height), clip.TimeText, MilStyle.DimLabel);
            x += ColTime;

            // Callsign takes the slack: it is the one field whose width genuinely varies.
            float nameWidth = Mathf.Max(40f, area.width - ColTime - ColDistance - ColRadio);
            GUI.Label(new Rect(x, area.y, nameWidth, area.height), clip.SpeakerText, nameStyle);
            x += nameWidth;

            GUI.Label(new Rect(x, area.y, ColDistance, area.height), clip.DistanceText, MilStyle.DimLabel);
            x += ColDistance;

            if (!string.IsNullOrEmpty(clip.RadioText))
            {
                GUI.Label(new Rect(x, area.y, ColRadio, area.height), clip.RadioText, MilStyle.DimLabel);
            }
        }

        private static readonly string[] TierOrder = { "C", "C+", "B", "B+", "A", "A+", "S" };

        private string[] _referenceOrder;
        private int _referenceIndex;
        private Vector2 _referenceScroll;
        private readonly List<string> _compareSelection = new List<string>();

        /// <summary>Radios sorted by tier, weakest first. Built once.</summary>
        private string[] ReferenceOrder
        {
            get
            {
                if (_referenceOrder == null)
                {
                    _referenceOrder = RadioProfiles.Keys
                        .OrderBy(tpl =>
                        {
                            int idx = System.Array.IndexOf(TierOrder, TierAttributePatch.GetTier(tpl) ?? "C");
                            return idx < 0 ? TierOrder.Length : idx;
                        })
                        .ThenBy(tpl => RadioProfiles[tpl].ClearRangeMeters)
                        .ToArray();
                }

                return _referenceOrder;
            }
        }

        private void DrawRaidReviewRadiosTab()
        {
            GUILayout.BeginHorizontal();

            // ---- list -------------------------------------------------------------------------
            GUILayout.BeginVertical(GUILayout.Width(190f));
            GUILayout.Label(ReferenceOrder.Length + L(" МОДЕЛЕЙ", " MODELS", " MODELLE", " MODELOS",
                " MODÈLES", " MODELI", " MODELLI", " MODELŮ"), MilStyle.SectionLabel);

            for (int i = 0; i < ReferenceOrder.Length; i++)
            {
                string tpl = ReferenceOrder[i];
                bool active = i == _referenceIndex;

                GUILayout.BeginHorizontal();

                // The compare tick lives in the list so a radio can be added without leaving it.
                bool picked = _compareSelection.Contains(tpl);
                if (GUILayout.Button(picked ? "✚" : "·", picked ? MilStyle.Button : MilStyle.ButtonOff,
                    GUILayout.Width(20f), GUILayout.Height(18f)))
                {
                    ToggleCompare(tpl);
                }

                if (GUILayout.Button(GetRadioDisplayName(tpl), active ? MilStyle.Button : MilStyle.ButtonOff,
                    GUILayout.Height(18f)))
                {
                    _referenceIndex = i;
                }

                GUILayout.Label(TierAttributePatch.GetTier(tpl) ?? "—", MilStyle.DimLabel, GUILayout.Width(24f));
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            // ---- plate ------------------------------------------------------------------------
            GUILayout.BeginVertical();
            DrawReferencePlate(ReferenceOrder[Mathf.Clamp(_referenceIndex, 0, ReferenceOrder.Length - 1)]);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void ToggleCompare(string tpl)
        {
            if (!_compareSelection.Remove(tpl) && _compareSelection.Count < 5)
            {
                _compareSelection.Add(tpl);
            }
        }

        private void DrawReferencePlate(string tpl)
        {
            RadioProfile p = RadioProfiles[tpl];

            GUILayout.Label(GetRadioDisplayName(tpl).ToUpperInvariant(), MilStyle.SectionLabel);
            GUILayout.Label(FamilyLabel(GetSignalFamily(tpl)), MilStyle.DimLabel);
            GUILayout.Space(6f);

            DrawSpecRow(L("ТИР", "TIER", "STUFE", "NIVEL", "RANG", "POZIOM", "LIVELLO", "STUPEŇ"),
                TierAttributePatch.GetTier(tpl) ?? "—");

            DrawSpecRow(L("ПОЛОСА", "BANDWIDTH", "BANDBREITE", "ANCHO DE BANDA",
                    "BANDE PASSANTE", "PASMO", "BANDA", "ŠÍŘKA PÁSMA"),
                Mathf.RoundToInt(p.HpCutoffHz) + " – " + Mathf.RoundToInt(p.LpCutoffNear) + " Hz");

            DrawSpecRow(L("ЧИСТАЯ СВЯЗЬ", "CLEAR RANGE", "KLARE REICHWEITE", "ALCANCE CLARO",
                    "PORTÉE CLAIRE", "CZYSTY ZASIĘG", "PORTATA PULITA", "ČISTÝ DOSAH"),
                Mathf.RoundToInt(p.ClearRangeMeters) + " m");

            DrawSpecRow(L("ПРОПАДАНИЯ", "DROPOUTS", "AUSSETZER", "CORTES",
                    "COUPURES", "ZANIKI", "INTERRUZIONI", "VÝPADKY"),
                Mathf.RoundToInt(p.DropoutChanceFar * 100f) + " %");

            GUILayout.Space(8f);

            if (TryGetRadioSpec(tpl, out RadioSpec spec))
            {
                DrawSectionHeading(L("ПАСПОРТ", "SPECIFICATIONS", "TECHNISCHE DATEN", "ESPECIFICACIONES",
                    "CARACTÉRISTIQUES", "DANE TECHNICZNE", "SPECIFICHE", "PARAMETRY"));

                DrawSpecRow(L("ПРОИЗВОДИТЕЛЬ", "MAKER", "HERSTELLER", "FABRICANTE",
                    "FABRICANT", "PRODUCENT", "PRODUTTORE", "VÝROBCE"), spec.Maker + " · " + spec.Country);
                DrawSpecRow(L("ГОД", "YEAR", "JAHR", "AÑO", "ANNÉE", "ROK", "ANNO", "ROK"), spec.Year);
                DrawSpecRow(L("ДИАПАЗОН", "BAND", "BAND", "BANDA", "BANDE", "PASMO", "BANDA", "PÁSMO"), spec.Band);
                DrawSpecRow(L("МОЩНОСТЬ", "POWER", "LEISTUNG", "POTENCIA",
                    "PUISSANCE", "MOC", "POTENZA", "VÝKON"), SpecText(spec.Power));
                DrawSpecRow(L("МОДУЛЯЦИЯ", "MODULATION", "MODULATION", "MODULACIÓN",
                    "MODULATION", "MODULACJA", "MODULAZIONE", "MODULACE"), spec.Modulation);
                DrawSpecRow(L("ВЕС", "WEIGHT", "GEWICHT", "PESO", "POIDS", "MASA", "PESO", "HMOTNOST"),
                    SpecText(spec.Weight));
                DrawSpecRow(L("КАНАЛЫ", "CHANNELS", "KANÄLE", "CANALES",
                    "CANAUX", "KANAŁY", "CANALI", "KANÁLY"), SpecText(spec.Channels));

                GUILayout.Space(8f);
            }
            else
            {
                // Alinco: no passport at all, and the absence is the point.
                DrawSectionHeading(L("ХАРАКТЕРИСТИКИ ОПРЕДЕЛИТЬ НЕ УДАЛОСЬ",
                    "SPECIFICATIONS COULD NOT BE DETERMINED", "TECHNISCHE DATEN NICHT ERMITTELBAR",
                    "NO SE PUDIERON DETERMINAR LAS ESPECIFICACIONES", "CARACTÉRISTIQUES INDÉTERMINABLES",
                    "NIE USTALONO PARAMETRÓW", "SPECIFICHE NON DETERMINABILI",
                    "PARAMETRY SE NEPODAŘILO URČIT"));

                GUILayout.Label(L("Маркировка стёрта. Совпадений в каталогах нет.",
                    "Markings filed off. No catalogue match.",
                    "Kennzeichnung entfernt. Kein Katalogtreffer.",
                    "Marcas borradas. Sin coincidencias en catálogos.",
                    "Marquages effacés. Aucune correspondance.",
                    "Oznaczenia starte. Brak dopasowania w katalogach.",
                    "Marcature rimosse. Nessuna corrispondenza.",
                    "Označení obroušeno. Bez shody v katalozích."), MilStyle.WrapLabel);

                GUILayout.Space(8f);
            }

            GUILayout.Label(L("ДАЛЬНОСТЬ СВЯЗИ", "COMMS RANGE", "REICHWEITE", "ALCANCE",
                "PORTÉE", "ZASIĘG", "PORTATA", "DOSAH"), MilStyle.DimLabel);

            DrawRangeLegend();

            Rect plot = GUILayoutUtility.GetRect(1f, 46f);
            DrawRangePlot(plot, p);

            GUILayout.Space(8f);
            DrawSectionHeading(L("ДЕМО-ПРОСЛУШКА", "AUDIO DEMO", "HÖRPROBE", "ESCUCHA DE PRUEBA",
                "ÉCOUTE DE DÉMONSTRATION", "ODSŁUCH", "ASCOLTO DIMOSTRATIVO", "UKÁZKA POSLECHU"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Дистанция", "Distance", "Entfernung", "Distancia",
                "Distance", "Odległość", "Distanza", "Vzdálenost"), MilStyle.DimLabel, GUILayout.Width(90f));

            _demoDistance = GUILayout.HorizontalSlider(_demoDistance, 0f, 1f, GUILayout.Height(20f));

            // Shown in metres of this radio's own scale, so the number means something concrete.
            GUILayout.Label(Mathf.RoundToInt(_demoDistance * p.NoiseOnlyRangeMeters) + " m",
                MilStyle.ValueLabel, GUILayout.Width(60f), GUILayout.Height(20f));

            if (GUILayout.Button(L("СЛУШАТЬ", "LISTEN", "ANHÖREN", "ESCUCHAR",
                "ÉCOUTER", "ODSŁUCHAJ", "ASCOLTA", "POSLECHNOUT"), MilStyle.Button,
                GUILayout.Width(110f), GUILayout.Height(20f)))
            {
                AuditionRadio(tpl, _demoDistance);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Сигналы", "Cues", "Signale", "Señales",
                "Signaux", "Sygnały", "Segnali", "Signály"), MilStyle.DimLabel, GUILayout.Width(90f));

            DrawCueButton(tpl, L("НАЧ. ПЕРЕДАЧИ", "TX START", "SENDEN AN", "INICIO EMISIÓN",
                "DÉBUT ÉMISSION", "POCZ. NADAWANIA", "INIZIO TX", "ZAČ. VYSÍLÁNÍ"), true, true);
            DrawCueButton(tpl, L("КОН. ПЕРЕДАЧИ", "TX END", "SENDEN AUS", "FIN EMISIÓN",
                "FIN ÉMISSION", "KON. NADAWANIA", "FINE TX", "KON. VYSÍLÁNÍ"), true, false);
            DrawCueButton(tpl, L("НАЧ. ПРИЁМА", "RX START", "EMPFANG AN", "INICIO RX",
                "DÉBUT RÉCEPTION", "POCZ. ODBIORU", "INIZIO RX", "ZAČ. PŘÍJMU"), false, true);
            DrawCueButton(tpl, L("КОН. ПРИЁМА", "RX END", "EMPFANG AUS", "FIN RX",
                "FIN RÉCEPTION", "KON. ODBIORU", "FINE RX", "KON. PŘÍJMU"), false, false);

            GUILayout.EndHorizontal();

            DrawRadioHistory(tpl);
        }

        private readonly HashSet<string> _historyOpen = new HashSet<string>();

        /// <summary>
        /// Collapsed history block, opened per radio. Closed by default because it is reading matter,
        /// not a readout — it should be there when wanted and invisible when not.
        /// </summary>
        private void DrawRadioHistory(string tpl)
        {
            string text = GetRadioHistory(tpl);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            bool open = _historyOpen.Contains(tpl);

            GUILayout.Space(UiTokens.GapUnit);

            string caption = (open ? "▾  " : "▸  ") + L("ИСТОРИЧЕСКАЯ СПРАВКА", "HISTORICAL NOTE",
                "HISTORISCHE NOTIZ", "NOTA HISTÓRICA", "NOTE HISTORIQUE", "NOTA HISTORYCZNA",
                "NOTA STORICA", "HISTORICKÁ POZNÁMKA");

            if (GUILayout.Button(caption, MilStyle.ButtonOff, GUILayout.Height(20f)))
            {
                if (open) { _historyOpen.Remove(tpl); } else { _historyOpen.Add(tpl); }
            }

            if (!open)
            {
                return;
            }

            // Running prose gets the body face and the full width, not the condensed label style:
            // narrow type is for marking, and a measure-limited column inside an already narrow
            // panel just wastes the room.
            GUILayout.Label(text, HistoryStyle);

            // Said plainly rather than left for the player to wonder about: the histories exist in
            // Russian and English only, and a machine translation of thirteen paragraphs would
            // undercut the accuracy the rest of the mod is built on.
            string code = GetLanguageCode();
            if (code != "ru" && code != "en")
            {
                GUILayout.Label(L("", "", "Nur auf Englisch verfügbar.", "Solo disponible en inglés.",
                    "Disponible en anglais uniquement.", "Dostępne tylko po angielsku.",
                    "Disponibile solo in inglese.", "K dispozici pouze anglicky."), MilStyle.DimLabel);
            }
        }

        private GUIStyle _historyStyle;

        private GUIStyle HistoryStyle
        {
            get
            {
                if (_historyStyle == null)
                {
                    _historyStyle = new GUIStyle(MilStyle.WrapLabel)
                    {
                        fontSize = UiTokens.SizeBody,
                        wordWrap = true,
                        padding = new RectOffset(8, 8, 6, 8),
                        richText = false,
                    };
                }

                return _historyStyle;
            }
        }

        private float _demoDistance = 0.35f;

        /// <summary>
        /// Plays one of the four cues for this radio, using whichever preset is actually selected.
        ///
        /// This used to always play the synthesised set, on the reasoning that the button was there
        /// to audition the new option before switching to it. That was wrong: with Classic selected
        /// the button then contradicted the setting, which reads as a broken switch rather than as a
        /// preview. A preview that ignores the setting it previews is worse than no preview.
        /// </summary>
        private void DrawCueButton(string tpl, string label, bool local, bool start)
        {
            if (!GUILayout.Button(label, MilStyle.ButtonOff, GUILayout.Height(20f)))
            {
                return;
            }

            bool classic = _soundStyle == null || _soundStyle.Value == SoundStyle.Classic;
            RadioSoundSet set = classic
                ? (_radioSoundSets.TryGetValue(tpl, out RadioSoundSet recorded) ? recorded : _defaultSoundSet)
                : GetSynthSoundSet(tpl);
            WavData wav = local
                ? (start ? set.LocalStart : set.LocalEnd)
                : (start ? set.RemoteStart : set.RemoteEnd);

            PlayPreview(wav, "preview_cue", _soundVolume.Value);
        }

        private static string FamilyLabel(SignalFamily family)
        {
            switch (family)
            {
                case SignalFamily.Digital: return "DIGITAL · DMR / TETRA / P25";
                case SignalFamily.Military: return "MILITARY";
                case SignalFamily.Cb: return "CB · AM";
                default: return "ANALOG · FM";
            }
        }

        private void DrawSpecRow(string key, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(key, MilStyle.DimLabel, GUILayout.Width(140f));
            GUILayout.Label(value, MilStyle.BodyLabel);
            GUILayout.EndHorizontal();
        }

        private const float RangeAxisMax = 1100f;

        /// <summary>
        /// Three zones on one axis. Each gets its own colour role rather than three shades of grey —
        /// the point of the plot is that the boundaries are legible at a glance.
        /// </summary>
        /// <summary>
        /// Colour key for the range plot. The zones used to be named in a single dim caption, which
        /// meant reading the plot required matching three colours to three words by position and
        /// hoping the order was the same. A swatch beside each word removes the guess.
        ///
        /// Swatches take their colour from the same roles the plot fills with, so the key cannot
        /// drift from the thing it describes.
        /// </summary>
        private void DrawRangeLegend()
        {
            Color[] colors = { MilStyle.Accent, MilStyle.Signal, MilStyle.TextMuted };
            string[] names =
            {
                L("ЧИСТО", "CLEAR", "KLAR", "LIMPIO", "CLAIR", "CZYSTO", "PULITO", "ČISTĚ"),
                L("С ШУМОМ", "NOISY", "VERRAUSCHT", "CON RUIDO", "BRUITÉ", "Z SZUMEM", "CON RUMORE", "SE ŠUMEM"),
                L("ТОЛЬКО ШУМ", "STATIC ONLY", "NUR RAUSCHEN", "SOLO RUIDO", "BRUIT SEUL", "SAM SZUM", "SOLO RUMORE", "JEN ŠUM"),
            };

            Rect row = GUILayoutUtility.GetRect(1f, 14f, GUILayout.ExpandWidth(true));
            float x = row.x;

            for (int i = 0; i < names.Length; i++)
            {
                Rect chip = new Rect(x, row.y + 3f, 9f, 9f);
                FillRect(chip, colors[i]);

                float textW = MilStyle.DimLabel.CalcSize(new GUIContent(names[i])).x;
                GUI.Label(new Rect(chip.xMax + 5f, row.y, textW, row.height), names[i], MilStyle.DimLabel);

                x = chip.xMax + 5f + textW + 14f;

                // Out of room: the remaining entries would be drawn past the panel edge, and a
                // clipped key is worse than a short one.
                if (x > row.xMax - 40f && i < names.Length - 1)
                {
                    break;
                }
            }

            GUILayout.Space(2f);
        }

        private void DrawRangePlot(Rect area, RadioProfile p)
        {
            Color prev = GUI.color;
            float W(float m) => Mathf.Clamp01(m / RangeAxisMax) * area.width;

            Rect bar = new Rect(area.x, area.y + 4f, area.width, 18f);

            GUI.color = new Color(MilStyle.Accent.r, MilStyle.Accent.g, MilStyle.Accent.b, 0.9f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, W(p.ZeroNoiseRangeMeters), bar.height), Texture2D.whiteTexture);

            GUI.color = new Color(MilStyle.SignalBright.r, MilStyle.SignalBright.g, MilStyle.SignalBright.b, 0.55f);
            GUI.DrawTexture(new Rect(bar.x + W(p.ZeroNoiseRangeMeters), bar.y,
                W(p.ClearRangeMeters) - W(p.ZeroNoiseRangeMeters), bar.height), Texture2D.whiteTexture);

            GUI.color = new Color(MilStyle.TextMuted.r, MilStyle.TextMuted.g, MilStyle.TextMuted.b, 0.45f);
            GUI.DrawTexture(new Rect(bar.x + W(p.ClearRangeMeters), bar.y,
                W(p.NoiseOnlyRangeMeters) - W(p.ClearRangeMeters), bar.height), Texture2D.whiteTexture);

            // Axis with a tick every 250 m, so the zone widths can actually be read as distances.
            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.9f);
            GUI.DrawTexture(new Rect(area.x, bar.yMax + 3f, area.width, UiTokens.Hairline), Texture2D.whiteTexture);

            for (float m = 0f; m <= 1000f; m += 250f)
            {
                GUI.color = new Color(MilStyle.TextMuted.r, MilStyle.TextMuted.g, MilStyle.TextMuted.b, 0.8f);
                GUI.DrawTexture(new Rect(area.x + W(m), bar.yMax + 3f, UiTokens.Hairline, 4f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(area.x + W(m) - 16f, bar.yMax + 7f, 34f, 14f),
                    Mathf.RoundToInt(m).ToString(), MilStyle.DimLabel);
            }

            GUI.color = prev;
        }

        // ---- compare --------------------------------------------------------------------------

        private void DrawRaidReviewCompareTab()
        {
            if (_compareSelection.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label(L("Отметьте рации значком ✚ на вкладке РАЦИИ",
                    "Tick radios with ✚ on the RADIOS tab", "Geräte auf dem Tab FUNKGERÄTE mit ✚ markieren",
                    "Marque radios con ✚ en la pestaña RADIOS", "Cochez des radios avec ✚ dans l'onglet RADIOS",
                    "Zaznacz radia znakiem ✚ w zakładce RADIOTELEFONY", "Seleziona radio con ✚ nella scheda RADIO",
                    "Označte vysílačky ✚ na kartě VYSÍLAČKY"), MilStyle.WrapLabel);
                GUILayout.FlexibleSpace();
                return;
            }

            MilStyle.PushScrollbarSkin();
            _referenceScroll = GUILayout.BeginScrollView(_referenceScroll, MilStyle.ScrollView);

            DrawCompareMetric(L("ЧИСТАЯ СВЯЗЬ", "CLEAR RANGE", "KLARE REICHWEITE", "ALCANCE CLARO",
                    "PORTÉE CLAIRE", "CZYSTY ZASIĘG", "PORTATA PULITA", "ČISTÝ DOSAH"),
                true, 900f, tpl => RadioProfiles[tpl].ClearRangeMeters, v => Mathf.RoundToInt(v) + " m");

            DrawCompareMetric(L("ПОЛОСА", "BANDWIDTH", "BANDBREITE", "ANCHO DE BANDA",
                    "BANDE PASSANTE", "PASMO", "BANDA", "ŠÍŘKA PÁSMA"),
                true, 5300f, tpl => RadioProfiles[tpl].LpCutoffNear - RadioProfiles[tpl].HpCutoffHz,
                v => Mathf.RoundToInt(v) + " Hz");

            DrawCompareMetric(L("ПРОПАДАНИЯ", "DROPOUTS", "AUSSETZER", "CORTES",
                    "COUPURES", "ZANIKI", "INTERRUZIONI", "VÝPADKY"),
                false, 45f, tpl => RadioProfiles[tpl].DropoutChanceFar * 100f, v => v.ToString("0.0") + " %");

            DrawCompareMetric(L("ШУМ", "NOISE", "RAUSCHEN", "RUIDO", "BRUIT", "SZUM", "RUMORE", "ŠUM"),
                false, 6f, tpl => RadioProfiles[tpl].NoiseAmpFar * 100f, v => v.ToString("0.00") + " %");

            GUILayout.EndScrollView();
            MilStyle.PopScrollbarSkin();
        }

        /// <summary>
        /// One metric as a bar per selected radio. Whether more is better is stated on the row —
        /// a chart where half the metrics invert without saying so misleads by tone alone.
        /// </summary>
        private void DrawCompareMetric(string title, bool moreIsBetter, float max,
            System.Func<string, float> get, System.Func<float, string> format)
        {
            string hint = moreIsBetter
                ? L("больше — лучше", "more is better", "mehr ist besser", "más es mejor",
                    "plus c'est mieux", "więcej znaczy lepiej", "più è meglio", "více je lépe")
                : L("меньше — лучше", "less is better", "weniger ist besser", "menos es mejor",
                    "moins c'est mieux", "mniej znaczy lepiej", "meno è meglio", "méně je lépe");

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, MilStyle.SectionLabel);
            GUILayout.Label(hint, MilStyle.DimLabel);
            GUILayout.EndHorizontal();

            foreach (string tpl in _compareSelection)
            {
                float value = get(tpl);

                GUILayout.BeginHorizontal();
                GUILayout.Label(GetRadioDisplayName(tpl), MilStyle.DimLabel, GUILayout.Width(150f));

                Rect track = GUILayoutUtility.GetRect(1f, 12f);
                Color prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.4f);
                GUI.DrawTexture(track, Texture2D.whiteTexture);

                float pct = Mathf.Clamp01(value / max);
                GUI.color = moreIsBetter ? MilStyle.Accent : MilStyle.SignalBright;
                GUI.DrawTexture(new Rect(track.x, track.y, track.width * pct, track.height), Texture2D.whiteTexture);
                GUI.color = prev;

                GUILayout.Label(format(value), MilStyle.BodyLabel, GUILayout.Width(74f));
                GUILayout.EndHorizontal();
            }
        }
    }
}
