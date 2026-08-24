using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Typography helpers that IMGUI does not provide.
    ///
    /// The single most recognisable thing about the mock-up is letter-spaced uppercase labels —
    /// stencil marking, the way it is painted on a real set. CSS gets that from one property; IMGUI
    /// has no equivalent, so the text is drawn one glyph at a time with a manual advance.
    ///
    /// That is O(characters) draw calls, which is why it is used only for short headings and never
    /// for body text or anything that repeats per row.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>
        /// Draws text with extra space between glyphs. Returns the width actually used, so callers
        /// can place a rule or a following element against the real end of the text.
        /// </summary>
        private static float DrawSpacedText(Rect area, string text, GUIStyle style, float spacing)
        {
            if (string.IsNullOrEmpty(text) || style == null)
            {
                return 0f;
            }

            float x = area.x;
            GUIContent glyph = new GUIContent();

            foreach (char c in text)
            {
                glyph.text = c.ToString();
                float w = style.CalcSize(glyph).x;

                // Spaces get the tracking too, otherwise words clump while letters spread.
                if (c != ' ')
                {
                    GUI.Label(new Rect(x, area.y, w + 2f, area.height), glyph, style);
                }

                x += w + spacing;
            }

            return x - area.x;
        }

        /// <summary>
        /// Section heading in the mock-up's manner: letter-spaced caps with a rule running from the
        /// end of the text to the right edge. The rule is what turns a label into a section divider
        /// without spending a whole row on a line.
        /// </summary>
        private void DrawSectionHeading(string text, float topGap = 0f)
        {
            if (topGap > 0f)
            {
                GUILayout.Space(topGap);
            }

            Rect row = GUILayoutUtility.GetRect(10f, 22f, GUILayout.ExpandWidth(true));

            if (!UiStyleState.IsInstrument)
            {
                // Classic keeps the plain label it always had.
                GUI.Label(row, text, MilStyle.SectionLabel);
                return;
            }

            DrawSpacedText(row, text.ToUpperInvariant(), MilStyle.SectionLabel, 2f);

            // Full-width rule *under* the caption, not a line trailing off its right edge. The
            // mock-up uses border-bottom on the group title: it closes the heading over the whole
            // column, so the eye reads "everything below this belongs together" - which a rule that
            // stops where the text stops does not do.
            Color prev = GUI.color;
            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.75f);
            GUI.DrawTexture(new Rect(row.x, row.yMax - UiTokens.Hairline, row.width, UiTokens.Hairline),
                Texture2D.whiteTexture);
            GUI.color = prev;

            // padding-bottom:6px + margin-bottom:10px from the mock-up, minus the row we just used.
            GUILayout.Space(10f);
        }

        /// <summary>
        /// Window header in the mock-up's manner: letter-spaced product name, a quiet subtitle
        /// underneath, the clock hard right. One plate for both factions — the palette already
        /// carries the side, so two separate header designs only made the window inconsistent
        /// with itself.
        /// </summary>
        private void DrawInstrumentHeader(Rect header)
        {
            Color prev = GUI.color;

            float pad = UiTokens.GapGroup;
            Rect titleRow = new Rect(header.x + pad, header.y + 6f, header.width - pad * 2f, 15f);

            GUI.color = new Color(MilStyle.AccentBright.r, MilStyle.AccentBright.g, MilStyle.AccentBright.b, 1f);
            float used = DrawSpacedText(titleRow, "S&M-PRT", MilStyle.SectionLabel, 2.4f);
            GUI.color = Color.white;

            // Subtitle continues on the same line, dimmed — it is context, not a second heading.
            // Only the version number: the full DisplayVersion carries "(experimental, SPT 4.1)",
            // which is useful in a log line and far too long for a window title.
            string shortVersion = DisplayVersion.Split(' ')[0];
            float subX = titleRow.x + used + 10f;
            float subW = Mathf.Max(0f, header.xMax - 200f - subX);

            if (subW > 40f)
            {
                GUI.Label(new Rect(subX, titleRow.y, subW, titleRow.height),
                    "PORTABLE RADIO TRANSMITTER · " + shortVersion, MilStyle.DimLabel);
            }

            Rect subRow = new Rect(header.x + pad, header.y + 22f, header.width - pad * 2f - 190f, 14f);
            GUI.Label(subRow, GetLocalCallsignAndFaction(), MilStyle.DimLabel);

            DrawClockReadout(new Rect(header.xMax - 190f, header.y + 20f, 150f, 16f));

            // Rule along the bottom of the plate, the way the mock-up separates chrome from content.
            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 1f);
            GUI.DrawTexture(new Rect(header.x, header.yMax - UiTokens.Hairline, header.width, UiTokens.Hairline),
                Texture2D.whiteTexture);

            GUI.color = prev;
        }

        private int _notifPreviewIndex;

        /// <summary>
        /// Live notification preview in the settings tab.
        ///
        /// Notifications only appear in a raid, and only when something happens — which meant the
        /// only way to judge a style was to load in and wait for an event. This posts a real one
        /// through the real notification path, so the preview is the thing itself rather than a
        /// drawing of it, and the style and opacity settings above are seen working immediately.
        /// </summary>
        private void DrawNotificationPreview()
        {
            GUILayout.Space(UiTokens.GapUnit);
            DrawSectionHeading(L("УВЕДОМЛЕНИЯ", "NOTIFICATIONS", "MELDUNGEN", "NOTIFICACIONES",
                "NOTIFICATIONS", "POWIADOMIENIA", "NOTIFICHE", "OZNÁMENÍ"));

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("Показать пример", "Post a sample", "Beispiel anzeigen", "Mostrar ejemplo",
                "Afficher un exemple", "Pokaż przykład", "Mostra un esempio", "Zobrazit ukázku"),
                MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));

            if (GUILayout.Button(L("ПОКАЗАТЬ", "SHOW", "ZEIGEN", "MOSTRAR",
                "AFFICHER", "POKAŻ", "MOSTRA", "ZOBRAZIT"), MilStyle.Button, GUILayout.Height(22f)))
            {
                PostSampleNotification();
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Cycles the thirteen events the mod can actually raise, across all seven categories -
        /// power, channel, mode, charge, alarm, link, recording. Cycling only one per category
        /// would leave the longest strings untested, and message width is exactly what breaks a
        /// notification style: the sample set is the stress test as much as the demonstration.
        /// </summary>
        private void PostSampleNotification()
        {
            NotifyKind[] kinds =
            {
                NotifyKind.Success, NotifyKind.Info,                      // power
                NotifyKind.Info, NotifyKind.Error,                        // channel
                NotifyKind.Info, NotifyKind.Info,                         // mode
                NotifyKind.Warning, NotifyKind.Error, NotifyKind.Error,   // charge
                NotifyKind.Warning,                                       // alarm
                NotifyKind.Success, NotifyKind.Info,                      // link
                NotifyKind.Success,                                       // recording
            };

            string[] samples =
            {
                L("Рация включена", "Radio on", "Funkgerät an", "Radio encendida",
                    "Radio allumée", "Radiotelefon włączony", "Radio accesa", "Vysílačka zapnuta"),
                L("Рация выключена", "Radio off", "Funkgerät aus", "Radio apagada",
                    "Radio éteinte", "Radiotelefon wyłączony", "Radio spenta", "Vysílačka vypnuta"),

                L("Выбрана рация", "Radio selected", "Gerät gewählt", "Radio seleccionada",
                    "Radio sélectionnée", "Wybrano radiotelefon", "Radio selezionata", "Vysílačka vybrána"),
                L("Нет рации в снаряжении", "No radio equipped", "Kein Funkgerät angelegt", "Sin radio equipada",
                    "Aucune radio équipée", "Brak radiotelefonu", "Nessuna radio equipaggiata", "Žádná vysílačka"),

                L("Дуплекс включён", "Duplex on", "Duplex an", "Dúplex activado",
                    "Duplex activé", "Dupleks włączony", "Duplex attivo", "Duplex zapnut"),
                L("Дуплекс выключен", "Duplex off", "Duplex aus", "Dúplex desactivado",
                    "Duplex désactivé", "Dupleks wyłączony", "Duplex disattivo", "Duplex vypnut"),

                L("Низкий заряд батареи", "Battery low", "Akku schwach", "Batería baja",
                    "Batterie faible", "Niski poziom baterii", "Batteria scarica", "Slabá baterie"),
                L("Батареи разряжены", "Batteries dead", "Akkus leer", "Baterías agotadas",
                    "Batteries à plat", "Baterie wyczerpane", "Batterie esaurite", "Baterie vybité"),
                L("Батарея не установлена", "No battery fitted", "Kein Akku eingesetzt", "Sin batería instalada",
                    "Aucune batterie installée", "Brak baterii", "Batteria non installata", "Baterie nevložena"),

                L("Сплошные помехи", "Jammed", "Starke Störung", "Interferencia total",
                    "Brouillage total", "Silne zakłócenia", "Interferenza totale", "Silné rušení"),

                L("Связь восстановлена", "Link restored", "Verbindung wieder da", "Enlace restablecido",
                    "Liaison rétablie", "Łączność przywrócona", "Collegamento ripristinato", "Spojení obnoveno"),
                L("Канал занят", "Channel busy", "Kanal belegt", "Canal ocupado",
                    "Canal occupé", "Kanał zajęty", "Canale occupato", "Kanál obsazen"),

                L("Запись сохранена", "Recording saved", "Aufnahme gespeichert", "Grabación guardada",
                    "Enregistrement sauvegardé", "Nagranie zapisane", "Registrazione salvata", "Nahrávka uložena"),
            };

            int i = _notifPreviewIndex % samples.Length;
            _notifPreviewIndex++;

            Notify(samples[i], kinds[i]);
        }

        /// <summary>
        /// Bordered panel behind a group of settings, matching the mock-up's cards. Drawn behind the
        /// content, so callers reserve the rect first and fill it afterwards.
        /// </summary>
        private static void DrawGroupPanel(Rect area)
        {
            if (!UiStyleState.IsInstrument)
            {
                return;
            }

            DrawRoundedPanel(area,
                new Color(MilStyle.Panel.r, MilStyle.Panel.g, MilStyle.Panel.b, 0.55f),
                new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.85f),
                3f, UiTokens.Hairline);
        }
    }
}
