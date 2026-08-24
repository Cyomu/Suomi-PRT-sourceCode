using EFT.InventoryLogic;

namespace RadioMod.Client
{
    /// <summary>
    /// The one deliberate departure from realism in the whole mod: on the jammed maps — the Labyrinth
    /// and the Labs — every radio is drowned in interference except the Alinco, and even that one
    /// does not give a clean channel.
    ///
    /// The rule is that <b>both</b> ends must be carrying an active Alinco. That is not only a
    /// balance choice — jamming is evaluated independently on each client, and "both" is the only
    /// symmetric rule of the three possible ones. A one-sided rule ("enough if the listener has it")
    /// would need explicit synchronisation, or one player hears the other while the other hears
    /// nothing, which is miserable to reproduce and debug.
    ///
    /// Nothing here is configurable. The anomaly is a property of the radio, like its range.
    /// </summary>
    public partial class Plugin
    {
        /// <summary>
        /// Voice profile for a working Alinco link inside a jammed map. Built from the same fields every
        /// other profile uses — no new mechanics, only a different set of numbers. Heavy ring
        /// modulation and a sunken voice gain give "audible, but not quite human".
        ///
        /// Ranges are the Alinco's own: the anomaly opens a channel, it does not turn a mediocre
        /// radio into an Azart.
        /// </summary>
        private static readonly RadioProfile AlincoLabyrinthProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 75f,
            ClearRangeMeters = 300f,
            NoiseOnlyRangeMeters = 400f,

            // Narrower than the Alinco's normal passband: the voice arrives thinned out.
            LpCutoffNear = 2300f,
            LpCutoffFar = 620f,
            HpCutoffHz = 380f,

            DriveNear = 2.1f,
            DriveFar = 4.4f,

            // The carrier sits low and detuned; combined with the ring mix it is what makes the
            // voice read as wrong rather than merely noisy.
            CarrierHzNear = 47f,
            CarrierHzFar = 96f,
            RingMixNear = 0.42f,
            RingMixFar = 0.78f,

            VoiceGainNear = 0.72f,
            VoiceGainFar = 0.38f,

            NoiseAmpNear = 0.02f,
            NoiseAmpFar = 0.07f,
            NoiseLpCutoffHz = 900f,

            DropoutChanceNear = 0.06f,
            DropoutChanceFar = 0.44f,

            StaticLpCutoff = 780f,
            StaticDrive = 4.6f,
            StaticRingMix = 0.72f,
            StaticVoiceGain = 0.08f,
            StaticNoiseAmp = 0.16f,
            StaticDropoutChance = 0.6f,

            // "Hears earlier than anything else": faint traffic bleeds through well beyond the point
            // where the link itself is gone. Jammed maps only — the base Alinco profile is
            // untouched, so this never leaks onto ordinary maps.
            HiddenNoiseStartMeters = 120f,
            HiddenNoiseAmp = 0.012f,
        };

        private bool _jamWarningShown;

        /// <summary>How long the jammed-channel warning stays up. Longer than any other message.</summary>
        private const float JamWarningSeconds = 5f;

        /// <summary>
        /// Tells the player once that the channel is dead, at the moment they press transmit.
        ///
        /// Two deliberate choices. It fires on the first transmit attempt rather than on switching
        /// the radio on, because pressing the key and getting nothing back is when the player
        /// actually needs the explanation. And it fires only once per visit to the dead zone — the
        /// same warning on every press would be spam in the one place where the player is already
        /// tense.
        /// </summary>
        private void NotifyJammedOnce()
        {
            if (!_cachedInRadioDeadZone || _jamWarningShown)
            {
                return;
            }

            _jamWarningShown = true;

            // With an Alinco in hand the blanket warning would be a lie — something does get through
            // here. The wording stays vague on purpose: the player is meant to notice the anomaly,
            // not be told about it.
            Notify(_cachedInAlincoAnomalyZone && LocalAlincoActive()
                ? L("Сплошные помехи — но что-то пробивается", "Heavy interference — yet something gets through",
                    "Starke Störungen — dennoch dringt etwas durch", "Interferencia total — aun así algo se filtra",
                    "Fortes interférences — pourtant quelque chose passe", "Silne zakłócenia — a jednak coś się przebija",
                    "Forti interferenze — eppure qualcosa passa", "Silné rušení — přesto něco proniká")
                : L("Сплошные помехи — связь недоступна", "Heavy interference — no usable signal",
                    "Starke Störungen — keine Verbindung", "Interferencia total — sin señal utilizable",
                    "Fortes interférences — aucun signal exploitable", "Silne zakłócenia — brak łączności",
                    "Forti interferenze — nessun segnale utile", "Silné rušení — spojení nedostupné"),
                NotifyKind.Warning,
                null,
                JamWarningSeconds);
        }

        /// <summary>Is the local player's active radio an Alinco?</summary>
        private bool LocalAlincoActive()
        {
            string tpl = _activeRadioTplId ?? _selectedRadioTplId;
            return tpl == AlincoTplId;
        }

        /// <summary>
        /// Is the remote player carrying an active Alinco? Resolved exactly the way
        /// <see cref="GetEffectiveProfile"/> resolves their radio — first selectable one found — so
        /// the two never disagree about which radio the other end is using.
        /// </summary>
        private bool RemoteAlincoActive(string remoteProfileId)
        {
            Fika.Core.Main.Players.FikaPlayer fp = GetFikaPlayerByProfileId(remoteProfileId);
            InventoryEquipment eq = fp?.Inventory?.Equipment;
            if (eq == null)
            {
                return false;
            }

            foreach (string tplId in CollectSelectableRadioTpls(eq))
            {
                if (RadioProfiles.ContainsKey(tplId))
                {
                    return tplId == AlincoTplId;
                }
            }

            return false;
        }

        /// <summary>True while an Alinco pair is holding a link open inside a jammed map.</summary>
        private bool AlincoLinkOpen(string remoteProfileId)
        {
            return _cachedInAlincoAnomalyZone && LocalAlincoActive() && RemoteAlincoActive(remoteProfileId);
        }

        /// <summary>
        /// Whether this particular link is jammed. Replaces the old "am I in a dead zone" test:
        /// the answer now depends on who is talking, because the radio at the other end matters.
        /// </summary>
        private bool IsLinkJammed(string remoteProfileId)
        {
            return _cachedInRadioDeadZone && !AlincoLinkOpen(remoteProfileId);
        }

        /// <summary>
        /// Swaps in the Labyrinth voice profile while an Alinco pair is connected. Ranges are taken
        /// from the anomaly profile itself, so the caller does not need to know anything about it.
        /// </summary>
        private RadioProfile ApplyLabyrinthProfile(string remoteProfileId, RadioProfile profile)
        {
            return AlincoLinkOpen(remoteProfileId) ? AlincoLabyrinthProfile : profile;
        }
    }
}
