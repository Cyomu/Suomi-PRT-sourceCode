using UnityEngine;

namespace RadioMod.Client
{
    /// <summary>
    /// Tab framework for the F9 window. The window stops being a recordings browser and becomes the
    /// mod's own interface: the log, the radio reference, the comparison and the settings.
    ///
    /// The settings tab deliberately holds no state of its own — it reads and writes the very same
    /// <c>ConfigEntry</c> objects the F12 menu edits. Two surfaces over one value means nothing to
    /// synchronise and nothing to migrate, and F12 keeps working exactly as before.
    /// </summary>
    public partial class Plugin
    {
        internal enum RaidReviewTab
        {
            Recordings,
            Radios,
            Compare,
            Settings,
        }

        private RaidReviewTab _raidReviewTab = RaidReviewTab.Recordings;

        private string TabTitle(RaidReviewTab tab)
        {
            switch (tab)
            {
                case RaidReviewTab.Radios:
                    return L("РАЦИИ", "RADIOS", "FUNKGERÄTE", "RADIOS", "RADIOS", "RADIOTELEFONY", "RADIO", "VYSÍLAČKY");
                case RaidReviewTab.Compare:
                    return L("СРАВНЕНИЕ", "COMPARE", "VERGLEICH", "COMPARAR", "COMPARER", "PORÓWNANIE", "CONFRONTO", "POROVNÁNÍ");
                case RaidReviewTab.Settings:
                    return L("НАСТРОЙКИ", "SETTINGS", "EINSTELLUNGEN", "AJUSTES", "RÉGLAGES", "USTAWIENIA", "IMPOSTAZIONI", "NASTAVENÍ");
                default:
                    return L("ЗАПИСИ", "RECORDINGS", "AUFNAHMEN", "GRABACIONES", "ENREGISTREMENTS", "NAGRANIA", "REGISTRAZIONI", "NAHRÁVKY");
            }
        }

        /// <summary>
        /// Row of tabs under the header. Equal widths so the strip stays aligned in every language —
        /// the translations differ enough in length that content-sized tabs jump around.
        /// </summary>
        private void DrawRaidReviewTabBar()
        {
            RaidReviewTab[] tabs =
            {
                RaidReviewTab.Recordings, RaidReviewTab.Radios, RaidReviewTab.Compare, RaidReviewTab.Settings,
            };

            Rect strip = GUILayoutUtility.GetRect(10f, 24f, GUILayout.ExpandWidth(true));
            float slot = strip.width / tabs.Length;

            for (int i = 0; i < tabs.Length; i++)
            {
                bool active = _raidReviewTab == tabs[i];
                Rect cell = new Rect(strip.x + i * slot, strip.y, slot, strip.height);

                if (GUI.Button(cell, TabTitle(tabs[i]), active ? MilStyle.Button : MilStyle.ButtonOff) && !active)
                {
                    _raidReviewTab = tabs[i];
                }

                // Accent rule under the active tab, the way the mock-up marks selection. A filled
                // button alone reads as "pressed"; the rule reads as "you are here".
                if (active && UiStyleState.IsInstrument)
                {
                    Color prev = GUI.color;
                    GUI.color = MilStyle.SignalBright;
                    GUI.DrawTexture(new Rect(cell.x, cell.yMax - UiTokens.Rule, cell.width, UiTokens.Rule),
                        Texture2D.whiteTexture);
                    GUI.color = prev;
                }
            }

            // Rule across the full width under the strip, tying the tabs to the content below.
            if (UiStyleState.IsInstrument)
            {
                Color prevRule = GUI.color;
                GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.9f);
                GUI.DrawTexture(new Rect(strip.x, strip.yMax, strip.width, UiTokens.Hairline), Texture2D.whiteTexture);
                GUI.color = prevRule;
            }

            GUILayout.Space(UiTokens.GapGroup);
        }

        /// <summary>Everything except the recordings log, which keeps its original code path.</summary>
        private void DrawRaidReviewSecondaryTab()
        {
            switch (_raidReviewTab)
            {
                case RaidReviewTab.Settings:
                    DrawRaidReviewSettingsTab();
                    break;
                case RaidReviewTab.Radios:
                    DrawRaidReviewRadiosTab();
                    break;
                case RaidReviewTab.Compare:
                    DrawRaidReviewCompareTab();
                    break;
                default:
                    DrawRaidReviewPlaceholder();
                    break;
            }
        }

        /// <summary>
        /// Honest placeholder for the tabs whose content is a later phase. It states what will be
        /// here rather than pretending to be broken or empty.
        /// </summary>
        private void DrawRaidReviewPlaceholder()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                L("РАЗДЕЛ В РАЗРАБОТКЕ", "SECTION IN PROGRESS", "BEREICH IN ARBEIT", "SECCIÓN EN DESARROLLO",
                    "SECTION EN COURS", "SEKCJA W BUDOWIE", "SEZIONE IN LAVORAZIONE", "SEKCE SE PŘIPRAVUJE"),
                MilStyle.SectionLabel);
            GUILayout.Label(
                _raidReviewTab == RaidReviewTab.Radios
                    ? L("Справочник тринадцати раций: дальность, полоса, демо-прослушка.",
                        "Reference for all thirteen radios: range, bandwidth, audio demo.",
                        "Referenz für alle dreizehn Geräte: Reichweite, Bandbreite, Hörprobe.",
                        "Referencia de trece radios: alcance, ancho de banda, escucha.",
                        "Référence des treize radios : portée, bande passante, écoute.",
                        "Katalog trzynastu radiotelefonów: zasięg, pasmo, odsłuch.",
                        "Riferimento di tredici radio: portata, banda, ascolto.",
                        "Přehled třinácti vysílaček: dosah, pásmo, poslech.")
                    : L("Сравнение выбранных раций по дальности, полосе, пропаданиям и шуму.",
                        "Compare selected radios by range, bandwidth, dropouts and noise.",
                        "Vergleich nach Reichweite, Bandbreite, Aussetzern und Rauschen.",
                        "Comparación por alcance, ancho de banda, cortes y ruido.",
                        "Comparaison par portée, bande passante, coupures et bruit.",
                        "Porównanie zasięgu, pasma, zaników i szumu.",
                        "Confronto per portata, banda, interruzioni e rumore.",
                        "Porovnání dosahu, pásma, výpadků a šumu."),
                MilStyle.WrapLabel);
            GUILayout.FlexibleSpace();
        }

        /// <summary>
        /// Settings shown inside the mod's own window. These are the same entries as in F12 — nothing
        /// is hidden there, both surfaces edit one value.
        /// </summary>
        /// <summary>
        /// Width below which the settings tab drops to a single column. Taken from the mock-up, which
        /// collapses its grid at the same point: the stand needs 300 px and the settings rows need a
        /// label column plus a control, and squeezing both into less than this truncates the labels.
        /// </summary>
        private const float SettingsTwoColumnMinWidth = 860f;

        private bool _settingsTwoColumn;

        private void DrawRaidReviewSettingsTab()
        {
            _settingsTwoColumn = _raidReviewWindowRect.width >= SettingsTwoColumnMinWidth;

            if (_settingsTwoColumn)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
            }

            MilStyle.PushScrollbarSkin();
            _raidReviewSettingsScroll = GUILayout.BeginScrollView(_raidReviewSettingsScroll, MilStyle.ScrollView);

            DrawSectionHeading(L("ВНЕШНИЙ ВИД", "APPEARANCE", "DARSTELLUNG", "APARIENCIA",
                "APPARENCE", "WYGLĄD", "ASPETTO", "VZHLED"));

            DrawEnumRow(
                L("Стиль интерфейса", "UI style", "UI-Stil", "Estilo de interfaz",
                    "Style d'interface", "Styl interfejsu", "Stile interfaccia", "Styl rozhraní"),
                _uiStyle.Value == UiStyle.Instrument ? "INSTRUMENT" : "CLASSIC",
                () => _uiStyle.Value = _uiStyle.Value == UiStyle.Instrument ? UiStyle.Classic : UiStyle.Instrument);

            if (UiStyleState.IsInstrument)
            {
                DrawEnumCycleRow(
                    L("Шкала сигнала", "Signal readout", "Signalanzeige", "Medidor de señal",
                        "Indicateur de signal", "Wskaźnik sygnału", "Misuratore segnale", "Měřič signálu"),
                    _instrumentSignalStyle);

                DrawEnumCycleRow(
                    L("Индикаторы состояния", "State readout", "Statusanzeige", "Indicadores de estado",
                        "Indicateurs d'état", "Wskaźniki stanu", "Indicatori di stato", "Indikátory stavu"),
                    _instrumentStateStyle);

                // Only offered when a battery mod actually gives the radio slots - without one the
                // readout never appears, and a setting for something invisible is just noise.
                if (BatteryReadoutAvailable)
                {
                    DrawEnumCycleRow(
                        L("Индикатор батареи", "Battery readout", "Akkuanzeige", "Indicador de batería",
                            "Indicateur de batterie", "Wskaźnik baterii", "Indicatore batteria", "Indikátor baterie"),
                        _instrumentBatteryStyle);
                }

                DrawSliderRow(
                    L("Прозрачность", "Opacity", "Deckkraft", "Opacidad",
                        "Opacité", "Przezroczystość", "Opacità", "Průhlednost"),
                    _indicatorOpacity, 0.1f, 1f);

            }

            DrawSliderRow(L("Масштаб индикаторов", "Indicator scale", "Anzeigenskalierung", "Escala de indicadores",
                "Échelle des indicateurs", "Skala wskaźników", "Scala indicatori", "Měřítko indikátorů"), _indicatorScale, 0.5f, 2f);

            DrawEnumCycleRow(L("Палитра", "Palette", "Palette", "Paleta",
                "Palette", "Paleta", "Palette", "Paleta"), _uiTheme);

            DrawEnumRow(L("Язык", "Language", "Sprache", "Idioma", "Langue", "Język", "Lingua", "Jazyk"),
                _uiLanguageOverride.Value.ToString().ToUpperInvariant(), CycleUiLanguage);

            GUILayout.Space(12f);
            DrawSectionHeading(L("ИНДИКАТОРЫ", "INDICATORS", "ANZEIGEN", "INDICADORES",
                "INDICATEURS", "WSKAŹNIKI", "INDICATORI", "INDIKÁTORY"));

            DrawBoolRow(L("Питание", "Power", "Strom", "Energía", "Alimentation", "Zasilanie", "Alimentazione", "Napájení"), _showPowerIndicator);
            DrawBoolRow(L("Передача", "Transmitting", "Senden", "Emisión", "Émission", "Nadawanie", "Trasmissione", "Vysílání"), _showTalkingIndicator);
            DrawBoolRow(L("Приём", "Receiving", "Empfang", "Recepción", "Réception", "Odbiór", "Ricezione", "Příjem"), _showBusyIndicator);
            DrawBoolRow(L("Дуплекс", "Duplex", "Duplex", "Dúplex", "Duplex", "Dupleks", "Duplex", "Duplex"), _showDuplexIndicator);
            DrawBoolRow(L("Сигнал", "Signal", "Signal", "Señal", "Signal", "Sygnał", "Segnale", "Signál"), _showSignalIndicator);
            DrawBoolRow(L("Затухание в покое", "Fade when idle", "Ausblenden bei Ruhe", "Atenuar en reposo",
                "Estomper au repos", "Zanikanie w spoczynku", "Dissolvenza a riposo", "Slábnutí v klidu"), _fadeIdleIndicators);
            DrawBoolRow(L("Развёртка настройки", "Tuning sweep", "Abstimmlauf", "Barrido de sintonía",
                "Balayage d'accord", "Przemiatanie strojenia", "Spazzata di sintonia", "Ladicí přeběh"), _showTuningSweep);

            DrawEnumCycleRow(L("Стиль индикаторов", "Indicator style", "Anzeigenstil", "Estilo de indicadores",
                "Style d'indicateurs", "Styl wskaźników", "Stile indicatori", "Styl indikátorů"), _indicatorStyle);
            DrawEnumCycleRow(L("Стиль шкалы сигнала", "Signal meter style", "Signalanzeigenstil", "Estilo del medidor",
                "Style de l'indicateur", "Styl wskaźnika sygnału", "Stile misuratore", "Styl měřiče"), _signalIndicatorStyle);

            if (BatteryReadoutAvailable)
            {
                DrawEnumCycleRow(L("Стиль заряда", "Battery style", "Akkuanzeigenstil", "Estilo de batería",
                    "Style de batterie", "Styl baterii", "Stile batteria", "Styl baterie"), _batteryIndicatorStyle);
            }

            // Battery controls exist only while the battery mod does — without it the radios never
            // discharge, so offering the toggle would promise a readout that can never appear.
            if (BatteryReadoutAvailable)
            {
                DrawBoolRow(L("Заряд батареи", "Battery charge", "Akkuladung", "Carga de batería",
                    "Charge de batterie", "Poziom baterii", "Carica batteria", "Nabití baterie"), _showBatteryIndicator);
            }

            GUILayout.Space(12f);
            DrawSectionHeading(L("ЦВЕТА ИНДИКАТОРОВ", "INDICATOR COLOURS", "ANZEIGENFARBEN", "COLORES DE INDICADORES",
                "COULEURS DES INDICATEURS", "KOLORY WSKAŹNIKÓW", "COLORI INDICATORI", "BARVY INDIKÁTORŮ"));

            DrawColorRow(L("Питание", "Power", "Strom", "Energía", "Alimentation", "Zasilanie", "Alimentazione", "Napájení"), _colorOn);
            DrawColorRow(L("Передача", "Transmitting", "Senden", "Emisión", "Émission", "Nadawanie", "Trasmissione", "Vysílání"), _colorTalking);
            DrawColorRow(L("Приём", "Receiving", "Empfang", "Recepción", "Réception", "Odbiór", "Ricezione", "Příjem"), _colorBusy);
            DrawColorRow(L("Дуплекс", "Duplex", "Duplex", "Dúplex", "Duplex", "Dupleks", "Duplex", "Duplex"), _colorSimplex);
            DrawColorRow(L("Шкала сигнала", "Signal bar", "Signalbalken", "Barra de señal",
                "Barre de signal", "Pasek sygnału", "Barra segnale", "Pruh signálu"), _colorSignalBar);
            DrawColorRow(L("Выбор рации", "Radio selection", "Geräteauswahl", "Selección de radio",
                "Sélection de radio", "Wybór radia", "Selezione radio", "Výběr vysílačky"), _colorSelect);

            GUILayout.Space(12f);
            DrawSectionHeading(L("УВЕДОМЛЕНИЯ", "NOTIFICATIONS", "MELDUNGEN", "NOTIFICACIONES",
                "NOTIFICATIONS", "POWIADOMIENIA", "NOTIFICHE", "OZNÁMENÍ"));

            DrawBoolRow(L("Показывать", "Show", "Anzeigen", "Mostrar", "Afficher", "Pokazuj", "Mostra", "Zobrazit"), _showNotifications);
            DrawSliderRow(L("Масштаб", "Scale", "Skalierung", "Escala", "Échelle", "Skala", "Scala", "Měřítko"), _notificationScale, 0.5f, 2f);
            DrawSliderRow(L("Прозрачность", "Opacity", "Deckkraft", "Opacidad", "Opacité", "Przezroczystość", "Opacità", "Průhlednost"), _notificationOpacity, 0.1f, 1f);

            // Strip / StripCompact / Plate are the Instrument-only looks; under Classic they fall
            // back to the nearest frozen equivalent, so the row is safe to show in either style.
            DrawEnumCycleRow(L("Стиль уведомлений", "Notification style", "Meldungsstil", "Estilo de notificación",
                "Style de notification", "Styl powiadomień", "Stile notifiche", "Styl oznámení"), _notificationStyleMode);
            DrawEnumCycleRow(L("Тема уведомлений", "Notification theme", "Meldungsthema", "Tema de notificación",
                "Thème de notification", "Motyw powiadomień", "Tema notifiche", "Motiv oznámení"), _notificationTheme);


            GUILayout.Space(12f);
            DrawSectionHeading(L("ЗВУК", "SOUND", "TON", "SONIDO", "SON", "DŹWIĘK", "AUDIO", "ZVUK"));

            DrawSliderRow(
                L("Громкость приёма", "Receive volume", "Empfangslautstärke", "Volumen de recepción",
                    "Volume de réception", "Głośność odbioru", "Volume ricezione", "Hlasitost příjmu"),
                _receiveVolume, 0.05f, 5f, AuditionReceive);

            DrawSliderRow(
                L("Громкость шума", "Noise volume", "Rauschlautstärke", "Volumen de ruido",
                    "Volume du bruit", "Głośność szumu", "Volume rumore", "Hlasitost šumu"),
                _noiseVolume, 0.05f, 1f, AuditionNoise);

            DrawSliderRow(
                L("Громкость звуков", "Sound volume", "Effektlautstärke", "Volumen de efectos",
                    "Volume des effets", "Głośność efektów", "Volume effetti", "Hlasitost efektů"),
                _soundVolume, 0.05f, 1f, AuditionCue);

            DrawEnumCycleRow(L("Сигналы связи", "Transmission cues", "Sendesignale", "Señales de transmisión",
                "Signaux d'émission", "Sygnały nadawania", "Segnali di trasmissione", "Signály vysílání"),
                _soundStyle, SoundStyleName);

            GUILayout.Space(12f);
            DrawSectionHeading(L("РАЦИЯ", "RADIO", "FUNK", "RADIO", "RADIO", "RADIO", "RADIO", "VYSÍLAČKA"));

            DrawBoolRow(L("Боевой эмбиент", "Ambient combat sound", "Gefechtsatmosphäre", "Sonido de combate ambiental",
                "Ambiance de combat", "Odgłosy walki w tle", "Audio di combattimento", "Zvuky boje v pozadí"), _ambientCombatSoundEnabled);

            DrawEnumCycleRow(L("Характер помех", "Interference character", "Störungscharakter", "Carácter de interferencia",
                "Caractère des parasites", "Charakter zakłóceń", "Carattere del disturbo", "Charakter rušení"),
                _interferenceCharacter, InterferenceCharacterName);

            GUILayout.Space(12f);
            DrawSectionHeading(L("ЗАПИСИ", "RECORDINGS", "AUFNAHMEN", "GRABACIONES",
                "ENREGISTREMENTS", "NAGRANIA", "REGISTRAZIONI", "NAHRÁVKY"));

            DrawBoolRow(L("Записывать переговоры", "Record radio comms", "Funkverkehr aufzeichnen", "Grabar comunicaciones",
                "Enregistrer les communications", "Nagrywaj łączność", "Registra le comunicazioni", "Nahrávat spojení"), _recordRadioComms);
            DrawBoolRow(L("Спектрограмма вместо волны", "Spectrogram instead of waveform", "Spektrogramm statt Wellenform",
                "Espectrograma en vez de onda", "Spectrogramme au lieu de l'onde", "Spektrogram zamiast fali",
                "Spettrogramma invece dell'onda", "Spektrogram místo vlny"), _spectrogramWaveform);
            DrawBoolRow(L("Новые сверху", "Newest first", "Neueste zuerst", "Más recientes primero",
                "Plus récents d'abord", "Najnowsze na górze", "Più recenti prima", "Nejnovější první"), _raidReviewNewestFirst);
            DrawBoolRow(L("Группировать по локации", "Group by location", "Nach Ort gruppieren", "Agrupar por ubicación",
                "Grouper par lieu", "Grupuj po lokacji", "Raggruppa per luogo", "Seskupit podle lokace"), _raidReviewSortByLocation);
            DrawBoolRow(L("Автопереход", "Auto-advance", "Automatisch weiter", "Avance automático",
                "Avance automatique", "Automatyczne przejście", "Avanzamento automatico", "Automatický posun"), _raidReviewAutoAdvance);
            DrawSliderRow(L("Громкость воспроизведения", "Playback volume", "Wiedergabelautstärke", "Volumen de reproducción",
                "Volume de lecture", "Głośność odtwarzania", "Volume di riproduzione", "Hlasitost přehrávání"),
                _raidReviewPlaybackVolume, 0f, 5f);

            GUILayout.Space(12f);
            DrawSectionHeading(L("УПРАВЛЕНИЕ", "CONTROLS", "STEUERUNG", "CONTROLES",
                "COMMANDES", "STEROWANIE", "COMANDI", "OVLÁDÁNÍ"));
            GUILayout.Label(L("Нажмите на клавишу, затем нужную. Esc — отмена.",
                "Click a binding, then press the new key. Esc cancels.",
                "Belegung anklicken, dann Taste drücken. Esc bricht ab.",
                "Haga clic y pulse la nueva tecla. Esc cancela.",
                "Cliquez puis appuyez sur la touche. Esc annule.",
                "Kliknij, potem naciśnij klawisz. Esc anuluje.",
                "Clicca, poi premi il tasto. Esc annulla.",
                "Klikněte a stiskněte klávesu. Esc zruší."), MilStyle.WrapLabel);

            DrawHotkeyRow(L("Вкл/выкл рацию", "Toggle radio", "Funk ein/aus", "Encender radio",
                "Activer la radio", "Włącz radio", "Accendi radio", "Zapnout vysílačku"), _radioToggleModifier);
            DrawHotkeyRow(L("Сменить рацию", "Select radio", "Gerät wechseln", "Cambiar radio",
                "Changer de radio", "Zmień radio", "Cambia radio", "Změnit vysílačku"), _selectRadioModifier);
            DrawHotkeyRow(L("Режим дуплекса", "Duplex mode", "Duplexmodus", "Modo dúplex",
                "Mode duplex", "Tryb dupleksu", "Modalità duplex", "Režim duplex"), _duplexModeModifier);

            // Preview last, the way the mock-up ends its settings column. In two-column mode it is
            // in the stand instead, so it is skipped here rather than drawn twice.
            if (!_settingsTwoColumn)
            {
                GUILayout.Space(12f);
                DrawSectionHeading(L("ПРЕВЬЮ", "PREVIEW", "VORSCHAU", "VISTA PREVIA",
                    "APERÇU", "PODGLĄD", "ANTEPRIMA", "NÁHLED"));

                if (UiStyleState.IsInstrument)
                {
                    Rect preview = GUILayoutUtility.GetRect(1f, InstrumentPreviewHeight() + UiTokens.GapUnit);
                    DrawInstrumentPreview(preview);
                }

                DrawNotificationPreview();
            }

            GUILayout.EndScrollView();
            MilStyle.PopScrollbarSkin();

            if (_settingsTwoColumn)
            {
                GUILayout.EndVertical();
                DrawSettingsStand();
                GUILayout.EndHorizontal();
            }
        }

        private Vector2 _raidReviewSettingsScroll;

        /// <summary>Fixed width of the stand, matching the mock-up track.</summary>
        private const float SettingsStandWidth = 300f;

        /// <summary>
        /// Right-hand column of the settings tab: the live chassis and the notification sample,
        /// framed as one panel.
        ///
        /// It is deliberately outside the scroll view. The whole reason for a preview is to watch it
        /// change while you turn the knobs, and in a single scrolling column the control being
        /// adjusted and the thing it affects are rarely on screen together.
        ///
        /// Only drawn when the window is wide enough - below that the same content appears inline in
        /// the single column instead, so nothing is ever lost, only moved.
        /// </summary>
        private void DrawSettingsStand()
        {
            GUILayout.BeginVertical(GUILayout.Width(SettingsStandWidth));

            Rect panel = GUILayoutUtility.GetRect(SettingsStandWidth, 0f,
                GUILayout.Width(SettingsStandWidth), GUILayout.ExpandHeight(true));

            DrawRoundedPanel(panel,
                new Color(MilStyle.Panel.r, MilStyle.Panel.g, MilStyle.Panel.b, 0.55f),
                new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.85f),
                3f, UiTokens.Hairline);

            const float pad = 12f;
            GUILayout.BeginArea(new Rect(panel.x + pad, panel.y + pad,
                panel.width - pad * 2f, Mathf.Max(0f, panel.height - pad * 2f)));

            DrawSectionHeading(L("ЖИВОЕ ПРЕВЬЮ", "LIVE PREVIEW", "LIVE-VORSCHAU", "VISTA EN VIVO",
                "APERÇU EN DIRECT", "PODGLĄD NA ŻYWO", "ANTEPRIMA DAL VIVO", "ŽIVÝ NÁHLED"));

            if (UiStyleState.IsInstrument)
            {
                float slot = SettingsStandWidth - 24f;
                Rect preview = GUILayoutUtility.GetRect(1f, InstrumentPreviewHeight(slot) + UiTokens.GapUnit);
                DrawInstrumentPreview(preview);
            }
            else
            {
                // Classic has no chassis to draw - saying so beats an empty frame that reads as a
                // rendering failure.
                GUILayout.Label(L("Превью корпуса доступно в стиле INSTRUMENT.",
                    "The chassis preview is available in the INSTRUMENT style.",
                    "Die Gehäuse-Vorschau gibt es im Stil INSTRUMENT.",
                    "La vista del chasis está disponible en el estilo INSTRUMENT.",
                    "L'aperçu du châssis existe dans le style INSTRUMENT.",
                    "Podgląd obudowy jest dostępny w stylu INSTRUMENT.",
                    "L'anteprima dello chassis è disponibile nello stile INSTRUMENT.",
                    "Náhled šasi je dostupný ve stylu INSTRUMENT."), MilStyle.WrapLabel);
            }

            DrawNotificationPreview();

            GUILayout.EndArea();
            GUILayout.EndVertical();
        }


        /// <summary>
        /// Generic enum cycler. Every style setting in the mod is a small enum, so one row type
        /// covers all of them instead of a bespoke handler per setting.
        /// </summary>
        private void DrawEnumCycleRow<T>(string label, BepInEx.Configuration.ConfigEntry<T> entry,
            System.Func<T, string> nameOf = null)
            where T : struct, System.Enum
        {
            T[] all = (T[])System.Enum.GetValues(typeof(T));
            string shown = nameOf != null ? nameOf(entry.Value) : entry.Value.ToString().ToUpperInvariant();

            DrawEnumRow(label, shown, () =>
            {
                int next = (System.Array.IndexOf(all, entry.Value) + 1) % all.Length;
                entry.Value = all[next];
            });
        }

        /// <summary>
        /// Названия пресетов звука. Не просто "ALT 1 / ALT 2": подпись говорит, по какому признаку
        /// пресет строит звук, иначе выбирать между четырьмя вариантами приходится вслепую.
        /// </summary>
        private string SoundStyleName(SoundStyle style)
        {
            switch (style)
            {
                case SoundStyle.PerRadio:
                    return L("НОВЫЕ · ПО ТИПУ СВЯЗИ", "NEW · BY STANDARD", "NEU · NACH STANDARD", "NUEVAS · POR ESTÁNDAR",
                        "NOUVEAUX · PAR NORME", "NOWE · WG STANDARDU", "NUOVI · PER STANDARD", "NOVÉ · PODLE STANDARDU");
                case SoundStyle.PerRadioAlt:
                    return L("НОВЫЕ · ПО ЭПОХЕ", "NEW · BY ERA", "NEU · NACH EPOCHE", "NUEVAS · POR ÉPOCA",
                        "NOUVEAUX · PAR ÉPOQUE", "NOWE · WG EPOKI", "NUOVI · PER EPOCA", "NOVÉ · PODLE DOBY");
                case SoundStyle.PerRadioAlt2:
                    return L("НОВЫЕ · ПО КОРПУСУ", "NEW · BY BUILD", "NEU · NACH BAUART", "NUEVAS · POR CONSTRUCCIÓN",
                        "NOUVEAUX · PAR FACTURE", "NOWE · WG BUDOWY", "NUOVI · PER COSTRUZIONE", "NOVÉ · PODLE KONSTRUKCE");
                default:
                    return L("КЛАССИЧЕСКИЕ", "CLASSIC", "KLASSISCH", "CLÁSICAS",
                        "CLASSIQUES", "KLASYCZNE", "CLASSICI", "KLASICKÉ");
            }
        }

        private string InterferenceCharacterName(InterferenceCharacter mode)
        {
            switch (mode)
            {
                case InterferenceCharacter.PerFamily:
                    return L("НОВЫЙ · ПО ТИПУ СВЯЗИ", "NEW · BY STANDARD", "NEU · NACH STANDARD", "NUEVO · POR ESTÁNDAR",
                        "NOUVEAU · PAR NORME", "NOWY · WG STANDARDU", "NUOVO · PER STANDARD", "NOVÝ · PODLE STANDARDU");
                case InterferenceCharacter.PerBand:
                    return L("НОВЫЙ · ПО ДИАПАЗОНУ", "NEW · BY BAND", "NEU · NACH BAND", "NUEVO · POR BANDA",
                        "NOUVEAU · PAR BANDE", "NOWY · WG PASMA", "NUOVO · PER BANDA", "NOVÝ · PODLE PÁSMA");
                case InterferenceCharacter.PerPower:
                    return L("НОВЫЙ · ПО МОЩНОСТИ", "NEW · BY POWER", "NEU · NACH LEISTUNG", "NUEVO · POR POTENCIA",
                        "NOUVEAU · PAR PUISSANCE", "NOWY · WG MOCY", "NUOVO · PER POTENZA", "NOVÝ · PODLE VÝKONU");
                default:
                    return L("КЛАССИЧЕСКИЙ", "CLASSIC", "KLASSISCH", "CLÁSICO",
                        "CLASSIQUE", "KLASYCZNY", "CLASSICO", "KLASICKÝ");
            }
        }

        private BepInEx.Configuration.ConfigEntry<KeyCode> _capturingKey;

        /// <summary>
        /// Key rebinding inside the window. Click the button, press a key.
        ///
        /// The capture reads <c>Event.current</c> rather than polling Input: while the window is
        /// modal the game's own input is suppressed, and the layout event stream is the only place
        /// the keystroke still reliably arrives.
        /// </summary>
        private void DrawHotkeyRow(string label, BepInEx.Configuration.ConfigEntry<KeyCode> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));

            bool capturing = ReferenceEquals(_capturingKey, entry);

            if (capturing && Event.current != null && Event.current.type == EventType.KeyDown)
            {
                KeyCode pressed = Event.current.keyCode;

                if (pressed == KeyCode.Escape)
                {
                    _capturingKey = null;
                }
                else if (pressed != KeyCode.None)
                {
                    entry.Value = pressed;
                    _capturingKey = null;
                }

                Event.current.Use();
            }

            string caption = capturing
                ? L("НАЖМИТЕ КЛАВИШУ…", "PRESS A KEY…", "TASTE DRÜCKEN…", "PULSE UNA TECLA…",
                    "APPUYEZ SUR UNE TOUCHE…", "NACIŚNIJ KLAWISZ…", "PREMI UN TASTO…", "STISKNĚTE KLÁVESU…")
                : entry.Value.ToString();

            if (GUILayout.Button(caption, capturing ? MilStyle.Button : MilStyle.ButtonOff,
                GUILayout.Height(20f), GUILayout.Width(190f)))
            {
                _capturingKey = capturing ? null : entry;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void CycleUiLanguage()
        {
            UiLanguage[] all = (UiLanguage[])System.Enum.GetValues(typeof(UiLanguage));
            _uiLanguageOverride.Value = all[(System.Array.IndexOf(all, _uiLanguageOverride.Value) + 1) % all.Length];
        }

        private void DrawBoolRow(string label, BepInEx.Configuration.ConfigEntry<bool> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));

            string on = L("ВКЛ", "ON", "AN", "SÍ", "OUI", "WŁ", "ON", "ZAP");
            string off = L("ВЫКЛ", "OFF", "AUS", "NO", "NON", "WYŁ", "OFF", "VYP");

            if (GUILayout.Button(entry.Value ? on : off,
                entry.Value ? MilStyle.Button : MilStyle.ButtonOff, GUILayout.Height(20f), GUILayout.Width(72f)))
            {
                entry.Value = !entry.Value;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Colour picker built from three channel sliders and a live swatch. IMGUI has no colour
        /// field, and a palette of presets would take away exactly the freedom the F12 colour
        /// entries already give — so the channels are exposed directly.
        /// </summary>
        private void DrawColorRow(string label, BepInEx.Configuration.ConfigEntry<Color> entry)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));

            Color c = entry.Value;

            Rect swatch = GUILayoutUtility.GetRect(22f, 18f, GUILayout.Width(22f), GUILayout.Height(18f));
            Color prev = GUI.color;
            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 1f);
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = new Color(c.r, c.g, c.b, 1f);
            GUI.DrawTexture(new Rect(swatch.x + 1f, swatch.y + 1f, swatch.width - 2f, swatch.height - 2f), Texture2D.whiteTexture);
            GUI.color = prev;

            float r = GUILayout.HorizontalSlider(c.r, 0f, 1f, GUILayout.Height(18f));
            float g = GUILayout.HorizontalSlider(c.g, 0f, 1f, GUILayout.Height(18f));
            float b = GUILayout.HorizontalSlider(c.b, 0f, 1f, GUILayout.Height(18f));

            if (!Mathf.Approximately(r, c.r) || !Mathf.Approximately(g, c.g) || !Mathf.Approximately(b, c.b))
            {
                // Alpha is left alone: the indicators drive it from their own opacity setting.
                entry.Value = new Color(r, g, b, c.a);
            }

            GUILayout.Label(ColorHex(entry.Value), MilStyle.DimLabel, GUILayout.Width(62f));
            GUILayout.EndHorizontal();
        }

        private static string ColorHex(Color c)
        {
            return "#" + Mathf.RoundToInt(c.r * 255f).ToString("X2")
                       + Mathf.RoundToInt(c.g * 255f).ToString("X2")
                       + Mathf.RoundToInt(c.b * 255f).ToString("X2");
        }

        /// <summary>Width of the label column. One value everywhere, so every row lines up.</summary>
        private const float SettingLabelWidth = 190f;

        private void DrawEnumRow(string label, string value, System.Action onToggle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));
            if (GUILayout.Button(value, MilStyle.Button, GUILayout.Height(22f)))
            {
                onToggle();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(UiTokens.GapTight);
        }

        private void DrawSliderRow(string label, BepInEx.Configuration.ConfigEntry<float> entry, float min, float max,
            System.Action audition = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));
            float value = GUILayout.HorizontalSlider(entry.Value, min, max, GUILayout.Height(22f));
            if (!Mathf.Approximately(value, entry.Value))
            {
                entry.Value = value;
            }
            GUILayout.Label(entry.Value.ToString("0.00"), MilStyle.ValueLabel, GUILayout.Width(52f), GUILayout.Height(22f));

            // Audition sits inside the row it belongs to, so there is no doubt which value it demos.
            if (audition != null && DrawAuditionButton())
            {
                audition();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(UiTokens.GapTight);
        }

        private void DrawReadOnlyRow(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, MilStyle.DimLabel, GUILayout.Width(SettingLabelWidth));
            GUILayout.Label(value, MilStyle.DimLabel);
            GUILayout.EndHorizontal();
        }

        /// <summary>Close button and resize grip, shared by every tab.</summary>
        private void DrawRaidReviewFooter(Rect fullRect)
        {
            GUILayout.Space(6f);
            if (GUILayout.Button(L("Закрыть", "Close", "Schließen", "Cerrar", "Fermer", "Zamknij", "Chiudi", "Zavřít").ToUpperInvariant(),
                MilStyle.Button, GUILayout.Height(24f)))
            {
                CloseRaidReviewBrowser();
            }

            DrawResizeGripVisual(fullRect);
        }
    }
}
