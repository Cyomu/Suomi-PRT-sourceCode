using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using Dissonance;
using Dissonance.Audio.Playback;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.Screens;
using Fika.Core.Main.Components;
using Fika.Core.Main.Players;
using HarmonyLib;
using UnityEngine;

namespace RadioMod.Client
{
    [BepInPlugin("com.suomi.makshepard.smprt", "S&M-PRT", "1.0.2")]
    [BepInDependency("com.fika.core")]
    // Partial: the in-raid HUD rendering lives in Plugin.Hud.cs.
    public partial class Plugin : BaseUnityPlugin
    {
        internal const string DisplayVersion = "1.0.2 (experimental, SPT 4.1)";

        internal const string TestFrequency = "144.500";
        private const float HeartbeatInterval = 60f;

        private const string BaofengTplId = "6d6f645f726164696f303031";
        private const string AzartTplId = "6d6f645f726164696f303032";

        private const string KenwoodTplId = "6d6f645f726164696f303033";
        private const string T460TplId = "6d6f645f726164696f303034";
        private const string YaesuTplId = "6d6f645f726164696f303035";
        private const string Dp4800TplId = "6d6f645f726164696f303036";
        private const string Dp4601eTplId = "6d6f645f726164696f303037";
        private const string Xts5000TplId = "6d6f645f726164696f303038";
        private const string HarrisTplId = "6d6f645f726164696f303039";

        private const string Trc83TplId = "6d6f645f726164696f303130";
        private const string AlincoTplId = "6d6f645f726164696f303131";
        private const string KenwoodProTalkTplId = "6d6f645f726164696f303132";
        private const string Mth800TplId = "6d6f645f726164696f303133";

        private static readonly HashSet<string> RadioTplIds = new HashSet<string>
        {
            BaofengTplId,
            AzartTplId,
            KenwoodTplId,
            T460TplId,
            YaesuTplId,
            Dp4800TplId,
            Dp4601eTplId,
            Xts5000TplId,
            HarrisTplId,
            Trc83TplId,
            AlincoTplId,
            KenwoodProTalkTplId,
            Mth800TplId,
        };

        private static readonly Dictionary<string, string> RadioDisplayNames = new Dictionary<string, string>
        {
            { BaofengTplId, "Baofeng UV-5R" },
            { AzartTplId, "Р-187П1 «Азарт»" },
            { KenwoodTplId, "Kenwood TH-21BT" },
            { T460TplId, "Motorola Talkabout T460" },
            { YaesuTplId, "Yaesu VX-8DR" },
            { Dp4800TplId, "Motorola DP4800" },
            { Dp4601eTplId, "Motorola DP4601e" },
            { Xts5000TplId, "Motorola XTS5000" },
            { HarrisTplId, "Harris AN/PRC-152" },
            { Trc83TplId, "Realistic TRC-83" },
            { AlincoTplId, "Alinco (Fake)" },
            { KenwoodProTalkTplId, "Kenwood ProTalk XLS" },
            { Mth800TplId, "Motorola MTH800" },
        };

        private static readonly HashSet<string> SimplexCapableTplIds = new HashSet<string>
        {
            AzartTplId,
            YaesuTplId,
            Dp4601eTplId,
            Xts5000TplId,
            HarrisTplId,
            KenwoodProTalkTplId,
            Mth800TplId,
        };

        private string _selectedRadioTplId;

        private enum RadioLocation { None, Backpack, Ready }
        private RadioLocation _radioLocation = RadioLocation.None;
        private float _nextLocationCheck;

        internal static readonly HashSet<string> RadioSpeakerNames = new HashSet<string>();
        internal static bool RadioReceiving;

        private struct RadioProfile
        {

            public float ZeroNoiseRangeMeters;
            public float ClearRangeMeters;
            public float NoiseOnlyRangeMeters;

            public float LpCutoffNear, LpCutoffFar;
            public float HpCutoffHz;
            public float DriveNear, DriveFar;
            public float CarrierHzNear, CarrierHzFar;
            public float RingMixNear, RingMixFar;
            public float VoiceGainNear, VoiceGainFar;
            public float NoiseAmpNear, NoiseAmpFar;
            public float NoiseLpCutoffHz;

            public float DropoutChanceNear, DropoutChanceFar;

            public float StaticLpCutoff;
            public float StaticDrive;
            public float StaticRingMix;
            public float StaticVoiceGain;
            public float StaticNoiseAmp;
            public float StaticDropoutChance;

            public float HiddenNoiseStartMeters;
            public float HiddenNoiseAmp;
        }

        private static readonly RadioProfile BaofengProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 50f,
            ClearRangeMeters = 275f,
            NoiseOnlyRangeMeters = 385f,
            LpCutoffNear = 3200f,
            LpCutoffFar = 900f,
            HpCutoffHz = 300f,
            DriveNear = 1.6f,
            DriveFar = 3.8f,
            CarrierHzNear = 70f,
            CarrierHzFar = 140f,
            RingMixNear = 0.08f,
            RingMixFar = 0.5f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.65f,
            NoiseAmpNear = 0.006f,
            NoiseAmpFar = 0.035f,
            NoiseLpCutoffHz = 1400f,
            DropoutChanceNear = 0.02f,
            DropoutChanceFar = 0.28f,
            StaticLpCutoff = 1200f,
            StaticDrive = 3.6f,
            StaticRingMix = 0.55f,
            StaticVoiceGain = 0.15f,
            StaticNoiseAmp = 0.1f,
            StaticDropoutChance = 0.45f,
        };

        private static readonly RadioProfile AzartProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 700f,
            ClearRangeMeters = 850f,
            NoiseOnlyRangeMeters = 1000f,
            LpCutoffNear = 4700f,
            LpCutoffFar = 1900f,
            HpCutoffHz = 170f,
            DriveNear = 0.75f,
            DriveFar = 1.85f,
            CarrierHzNear = 260f,
            CarrierHzFar = 420f,
            RingMixNear = 0.01f,
            RingMixFar = 0.28f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.84f,
            NoiseAmpNear = 0.0023f,
            NoiseAmpFar = 0.019f,
            NoiseLpCutoffHz = 3500f,
            DropoutChanceNear = 0.007f,
            DropoutChanceFar = 0.095f,
            StaticLpCutoff = 2550f,
            StaticDrive = 1.95f,
            StaticRingMix = 0.33f,
            StaticVoiceGain = 0.27f,
            StaticNoiseAmp = 0.068f,
            StaticDropoutChance = 0.19f,

            HiddenNoiseStartMeters = 175f,
            HiddenNoiseAmp = 0.006f,
        };

        private static readonly RadioProfile KenwoodProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 25f,
            ClearRangeMeters = 175f,
            NoiseOnlyRangeMeters = 275f,
            LpCutoffNear = 2400f,
            LpCutoffFar = 650f,
            HpCutoffHz = 350f,
            DriveNear = 2.2f,
            DriveFar = 4.5f,
            CarrierHzNear = 55f,
            CarrierHzFar = 110f,
            RingMixNear = 0.12f,
            RingMixFar = 0.6f,
            VoiceGainNear = 0.9f,
            VoiceGainFar = 0.55f,
            NoiseAmpNear = 0.01f,
            NoiseAmpFar = 0.05f,
            NoiseLpCutoffHz = 1100f,
            DropoutChanceNear = 0.04f,
            DropoutChanceFar = 0.38f,
            StaticLpCutoff = 900f,
            StaticDrive = 4.2f,
            StaticRingMix = 0.65f,
            StaticVoiceGain = 0.1f,
            StaticNoiseAmp = 0.14f,
            StaticDropoutChance = 0.55f,
        };

        private static readonly RadioProfile T460Profile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 100f,
            ClearRangeMeters = 325f,
            NoiseOnlyRangeMeters = 475f,
            LpCutoffNear = 3600f,
            LpCutoffFar = 1400f,
            HpCutoffHz = 280f,
            DriveNear = 1.2f,
            DriveFar = 2.8f,
            CarrierHzNear = 90f,
            CarrierHzFar = 170f,
            RingMixNear = 0.05f,
            RingMixFar = 0.38f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.72f,
            NoiseAmpNear = 0.004f,
            NoiseAmpFar = 0.024f,
            NoiseLpCutoffHz = 1800f,
            DropoutChanceNear = 0.01f,
            DropoutChanceFar = 0.18f,
            StaticLpCutoff = 1500f,
            StaticDrive = 2.8f,
            StaticRingMix = 0.42f,
            StaticVoiceGain = 0.22f,
            StaticNoiseAmp = 0.08f,
            StaticDropoutChance = 0.32f,
        };

        private static readonly RadioProfile YaesuProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 150f,
            ClearRangeMeters = 400f,
            NoiseOnlyRangeMeters = 515f,
            LpCutoffNear = 4200f,
            LpCutoffFar = 2000f,
            HpCutoffHz = 250f,
            DriveNear = 0.9f,
            DriveFar = 2.0f,
            CarrierHzNear = 110f,
            CarrierHzFar = 200f,
            RingMixNear = 0.03f,
            RingMixFar = 0.28f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.8f,
            NoiseAmpNear = 0.003f,
            NoiseAmpFar = 0.016f,
            NoiseLpCutoffHz = 2400f,
            DropoutChanceNear = 0.005f,
            DropoutChanceFar = 0.12f,
            StaticLpCutoff = 1900f,
            StaticDrive = 2.1f,
            StaticRingMix = 0.32f,
            StaticVoiceGain = 0.28f,
            StaticNoiseAmp = 0.065f,
            StaticDropoutChance = 0.24f,
        };

        private static readonly RadioProfile Dp4800Profile = new RadioProfile
        {

            ZeroNoiseRangeMeters = 350f,
            ClearRangeMeters = 525f,
            NoiseOnlyRangeMeters = 650f,
            LpCutoffNear = 4600f,
            LpCutoffFar = 1800f,
            HpCutoffHz = 180f,
            DriveNear = 0.8f,
            DriveFar = 1.9f,
            CarrierHzNear = 220f,
            CarrierHzFar = 350f,
            RingMixNear = 0.01f,
            RingMixFar = 0.3f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.83f,
            NoiseAmpNear = 0.0025f,
            NoiseAmpFar = 0.02f,
            NoiseLpCutoffHz = 3400f,
            DropoutChanceNear = 0.008f,
            DropoutChanceFar = 0.1f,
            StaticLpCutoff = 2500f,
            StaticDrive = 2.0f,
            StaticRingMix = 0.35f,
            StaticVoiceGain = 0.26f,
            StaticNoiseAmp = 0.07f,
            StaticDropoutChance = 0.2f,
        };

        private static readonly RadioProfile Dp4601eProfile = new RadioProfile
        {

            ZeroNoiseRangeMeters = 300f,
            ClearRangeMeters = 500f,
            NoiseOnlyRangeMeters = 625f,
            LpCutoffNear = 4900f,
            LpCutoffFar = 2200f,
            HpCutoffHz = 165f,
            DriveNear = 0.7f,
            DriveFar = 1.7f,
            CarrierHzNear = 240f,
            CarrierHzFar = 385f,
            RingMixNear = 0.005f,
            RingMixFar = 0.26f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.86f,
            NoiseAmpNear = 0.002f,
            NoiseAmpFar = 0.017f,
            NoiseLpCutoffHz = 3800f,
            DropoutChanceNear = 0.004f,
            DropoutChanceFar = 0.08f,
            StaticLpCutoff = 2750f,
            StaticDrive = 1.8f,
            StaticRingMix = 0.32f,
            StaticVoiceGain = 0.28f,
            StaticNoiseAmp = 0.06f,
            StaticDropoutChance = 0.18f,
        };

        private static readonly RadioProfile Xts5000Profile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 450f,
            ClearRangeMeters = 625f,
            NoiseOnlyRangeMeters = 750f,
            LpCutoffNear = 5050f,
            LpCutoffFar = 2400f,
            HpCutoffHz = 155f,
            DriveNear = 0.65f,
            DriveFar = 1.6f,
            CarrierHzNear = 300f,
            CarrierHzFar = 460f,
            RingMixNear = 0.002f,
            RingMixFar = 0.24f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.87f,
            NoiseAmpNear = 0.0018f,
            NoiseAmpFar = 0.015f,
            NoiseLpCutoffHz = 4000f,
            DropoutChanceNear = 0.002f,
            DropoutChanceFar = 0.07f,
            StaticLpCutoff = 2900f,
            StaticDrive = 1.7f,
            StaticRingMix = 0.28f,
            StaticVoiceGain = 0.29f,
            StaticNoiseAmp = 0.055f,
            StaticDropoutChance = 0.16f,
        };

        private static readonly RadioProfile HarrisProfile = new RadioProfile
        {

            ZeroNoiseRangeMeters = 600f,
            ClearRangeMeters = 775f,
            NoiseOnlyRangeMeters = 900f,
            LpCutoffNear = 5350f,
            LpCutoffFar = 2800f,
            HpCutoffHz = 140f,
            DriveNear = 0.45f,
            DriveFar = 1.2f,
            CarrierHzNear = 180f,
            CarrierHzFar = 300f,
            RingMixNear = 0f,
            RingMixFar = 0.12f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.92f,
            NoiseAmpNear = 0.001f,
            NoiseAmpFar = 0.01f,
            NoiseLpCutoffHz = 4400f,
            DropoutChanceNear = 0f,
            DropoutChanceFar = 0.03f,
            StaticLpCutoff = 3300f,
            StaticDrive = 1.3f,
            StaticRingMix = 0.15f,
            StaticVoiceGain = 0.36f,
            StaticNoiseAmp = 0.035f,
            StaticDropoutChance = 0.08f,
        };

        private static readonly RadioProfile Trc83Profile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 30f,
            ClearRangeMeters = 200f,
            NoiseOnlyRangeMeters = 300f,
            LpCutoffNear = 2000f,
            LpCutoffFar = 500f,
            HpCutoffHz = 380f,
            DriveNear = 2.6f,
            DriveFar = 5.0f,
            CarrierHzNear = 45f,
            CarrierHzFar = 95f,
            RingMixNear = 0.15f,
            RingMixFar = 0.68f,
            VoiceGainNear = 0.85f,
            VoiceGainFar = 0.5f,
            NoiseAmpNear = 0.013f,
            NoiseAmpFar = 0.058f,
            NoiseLpCutoffHz = 950f,
            DropoutChanceNear = 0.05f,
            DropoutChanceFar = 0.42f,
            StaticLpCutoff = 780f,
            StaticDrive = 4.6f,
            StaticRingMix = 0.7f,
            StaticVoiceGain = 0.08f,
            StaticNoiseAmp = 0.16f,
            StaticDropoutChance = 0.6f,
        };

        private static readonly RadioProfile AlincoProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 75f,
            ClearRangeMeters = 300f,
            NoiseOnlyRangeMeters = 400f,
            LpCutoffNear = 2700f,
            LpCutoffFar = 750f,
            HpCutoffHz = 330f,
            DriveNear = 1.9f,
            DriveFar = 4.1f,
            CarrierHzNear = 62f,
            CarrierHzFar = 125f,
            RingMixNear = 0.14f,
            RingMixFar = 0.62f,
            VoiceGainNear = 0.88f,
            VoiceGainFar = 0.52f,
            NoiseAmpNear = 0.011f,
            NoiseAmpFar = 0.052f,
            NoiseLpCutoffHz = 1050f,
            DropoutChanceNear = 0.045f,
            DropoutChanceFar = 0.4f,
            StaticLpCutoff = 850f,
            StaticDrive = 4.3f,
            StaticRingMix = 0.67f,
            StaticVoiceGain = 0.09f,
            StaticNoiseAmp = 0.15f,
            StaticDropoutChance = 0.57f,
        };

        private static readonly RadioProfile KenwoodProTalkProfile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 90f,
            ClearRangeMeters = 295f,
            NoiseOnlyRangeMeters = 425f,
            LpCutoffNear = 4100f,
            LpCutoffFar = 1950f,
            HpCutoffHz = 255f,
            DriveNear = 0.95f,
            DriveFar = 2.05f,
            CarrierHzNear = 100f,
            CarrierHzFar = 185f,
            RingMixNear = 0.035f,
            RingMixFar = 0.29f,
            VoiceGainNear = 1f,
            VoiceGainFar = 0.79f,
            NoiseAmpNear = 0.0032f,
            NoiseAmpFar = 0.0165f,
            NoiseLpCutoffHz = 2350f,
            DropoutChanceNear = 0.006f,
            DropoutChanceFar = 0.125f,
            StaticLpCutoff = 1870f,
            StaticDrive = 2.15f,
            StaticRingMix = 0.33f,
            StaticVoiceGain = 0.275f,
            StaticNoiseAmp = 0.067f,
            StaticDropoutChance = 0.245f,
        };

        private static readonly RadioProfile Mth800Profile = new RadioProfile
        {
            ZeroNoiseRangeMeters = 175f,
            ClearRangeMeters = 425f,
            NoiseOnlyRangeMeters = 490f,
            LpCutoffNear = 3800f,
            LpCutoffFar = 1400f,
            HpCutoffHz = 200f,
            DriveNear = 1.1f,
            DriveFar = 2.6f,
            CarrierHzNear = 130f,
            CarrierHzFar = 215f,
            RingMixNear = 0.02f,
            RingMixFar = 0.36f,
            VoiceGainNear = 0.95f,
            VoiceGainFar = 0.68f,
            NoiseAmpNear = 0.005f,
            NoiseAmpFar = 0.028f,
            NoiseLpCutoffHz = 2800f,
            DropoutChanceNear = 0.015f,
            DropoutChanceFar = 0.16f,
            StaticLpCutoff = 2100f,
            StaticDrive = 2.6f,
            StaticRingMix = 0.4f,
            StaticVoiceGain = 0.24f,
            StaticNoiseAmp = 0.075f,
            StaticDropoutChance = 0.28f,
        };

        private static readonly Dictionary<string, RadioProfile> RadioProfiles = new Dictionary<string, RadioProfile>
        {
            { BaofengTplId, BaofengProfile },
            { AzartTplId, AzartProfile },
            { KenwoodTplId, KenwoodProfile },
            { T460TplId, T460Profile },
            { YaesuTplId, YaesuProfile },
            { Dp4800TplId, Dp4800Profile },
            { Dp4601eTplId, Dp4601eProfile },
            { Xts5000TplId, Xts5000Profile },
            { HarrisTplId, HarrisProfile },
            { Trc83TplId, Trc83Profile },
            { AlincoTplId, AlincoProfile },
            { KenwoodProTalkTplId, KenwoodProTalkProfile },
            { Mth800TplId, Mth800Profile },
        };

        private ConfigEntry<KeyCode> _radioToggleModifier;
        private ConfigEntry<KeyCode> _selectRadioModifier;
        private ConfigEntry<bool> _showNotifications;
        private ConfigEntry<bool> _showPowerIndicator;
        private ConfigEntry<bool> _showDuplexIndicator;
        private ConfigEntry<bool> _showBusyIndicator;
        private ConfigEntry<bool> _showSignalIndicator;
        private ConfigEntry<bool> _showTalkingIndicator;
        private ConfigEntry<float> _indicatorOpacity;
        private ConfigEntry<SignalIndicatorStyle> _signalIndicatorStyle;
        private ConfigEntry<Color> _colorOn;
        private ConfigEntry<Color> _colorSelect;
        private ConfigEntry<Color> _colorSimplex;
        private ConfigEntry<Color> _colorBusy;
        private ConfigEntry<Color> _colorSignalBar;
        private ConfigEntry<Color> _colorTalking;

        private enum SignalIndicatorStyle { Bar, AntennaBars }

        /// <summary>
        /// How the on-screen radio state indicators are presented.
        /// Dots / Blocks: lamps only, round or angular. Badges: the labelled live audio meters only.
        /// FullDots / FullBadges: lamps plus meters, with round or angular lamps respectively.
        /// </summary>
        private enum IndicatorStyle { Dots, Blocks, Badges, FullDots, FullBadges }
        private ConfigEntry<IndicatorStyle> _indicatorStyle;

        /// <summary>Presentation of the radio battery charge readout.</summary>
        private enum BatteryIndicatorStyle { Cell, Bar, Percent, CellAndPercent, BarAndPercent }
        private ConfigEntry<bool> _showBatteryIndicator;
        private ConfigEntry<BatteryIndicatorStyle> _batteryIndicatorStyle;

        private bool IndicatorLampsVisible()
        {
            return _indicatorStyle.Value != IndicatorStyle.Badges;
        }

        private bool IndicatorLampsAreRound()
        {
            IndicatorStyle s = _indicatorStyle.Value;
            return s == IndicatorStyle.Dots || s == IndicatorStyle.FullDots;
        }

        private bool IndicatorBadgesVisible()
        {
            IndicatorStyle s = _indicatorStyle.Value;
            return s == IndicatorStyle.Badges || s == IndicatorStyle.FullDots || s == IndicatorStyle.FullBadges;
        }
        private ConfigEntry<float> _soundVolume;
        private ConfigEntry<float> _receiveVolume;
        private ConfigEntry<float> _noiseVolume;
        private ConfigEntry<bool> _ambientCombatSoundEnabled;
        private ConfigEntry<bool> _verboseLogging;
        private ConfigEntry<float> _batteryDrainMultiplier;
        private float _appliedBatteryDrainMultiplier = 1f;
        private ConfigEntry<bool> _recordRadioComms;
        private ConfigEntry<KeyboardShortcut> _raidReviewKey;
        private ConfigEntry<bool> _clearRecordingsButton;
        private ConfigEntry<CleanupFrequency> _autoCleanupFrequency;
        private ConfigEntry<int> _raidsSinceCleanup;
        private ConfigEntry<float> _raidReviewPlaybackVolume;
        private ConfigEntry<bool> _raidReviewAutoAdvance;

        private enum CleanupFrequency { Never, EveryRaid, Every3Raids, Every5Raids, Every10Raids }

        /// <summary>
        /// Master switch between the frozen pre-1.0.0-E look and the military-radio redesign.
        /// The enum itself lives in Ui/UiStyle.cs because the renderers behind the style seam
        /// need it and cannot see private members of this class.
        /// </summary>
        private ConfigEntry<UiStyle> _uiStyle;

        // Seven palettes. Auto still follows the player side; the five added ones are explicit
        // choices, because nothing in a profile says "TerraGroup".
        internal enum UiTheme { Auto, BEAR, USEC, UNTAR, RUAF, BlackDivision, TerraGroup, SCAV }
        private ConfigEntry<UiTheme> _uiTheme;

        private enum NotificationTheme { FollowWindow, BEAR, USEC }
        private ConfigEntry<NotificationTheme> _notificationTheme;
        private ConfigEntry<bool> _fadeIdleIndicators;
        private ConfigEntry<bool> _showTuningSweep;
        private ConfigEntry<bool> _spectrogramWaveform;
        private ConfigEntry<float> _indicatorScale;
        private ConfigEntry<float> _notificationScale;

        private bool _cachedInRadioDeadZone;

        /// <summary>
        /// Locations where radio traffic is drowned in interference regardless of range or radio
        /// tier. A set rather than a single id so another map can be added with one line.
        /// </summary>
        private static readonly HashSet<string> RadioDeadZoneLocations =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "labyrinth", "laboratory" };

        // The Alinco exception and its anomalies follow the jammed maps exactly — one list, not two.
        // Two sets with the same contents would be a trap: someone adds a map to one, forgets the
        // other, and the anomaly quietly stops matching the jamming.
        private bool _cachedInAlincoAnomalyZone;

        private enum UiLanguage { Auto, Russian, English, German, Spanish, French, Polish, Italian, Czech }
        private static ConfigEntry<UiLanguage> _uiLanguageOverride;
        private Harmony _harmony;

        private const float CombatAmbienceRadiusMeters = 40f;
        private const float CombatAmbienceWindowSeconds = 2.5f;

        private bool _radioOn;
        private bool _wasVanillaTalking;

        private enum DuplexMode { HalfDuplex, Simplex }
        private DuplexMode _duplexMode = DuplexMode.HalfDuplex;
        private ConfigEntry<KeyCode> _duplexModeModifier;

        private RoomChannel? _txChannel;
        private RoomMembership? _rxMembership;
        private float _nextHeartbeat;

        private Player _localPlayer;
        private string _cachedNickname;
        private EPlayerSide? _cachedSide;
        private ConfigEntry<string> _lastKnownNickname;
        private ConfigEntry<string> _lastKnownSide;

        private const float SpeakingHoldSeconds = 0.25f;
        private readonly Dictionary<string, float> _lastOnFreqTime = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _remoteSpeakingState = new Dictionary<string, bool>();

        private readonly HashSet<string> _remoteStartPlayed = new HashSet<string>();
        private readonly List<RemoteChannel> _speakingChannelsBuffer = new List<RemoteChannel>();
        private readonly List<string> _speakingScratch = new List<string>();

        private readonly Dictionary<string, RadioVoiceFilter> _radioFilters = new Dictionary<string, RadioVoiceFilter>();
        private CoopHandler _coopHandler;

        private bool _showRaidReviewBrowser;
        private string[] _raidReviewDays = new string[0];
        private int _raidReviewDayIndex;
        private RaidReviewClipInfo[] _raidReviewAllClips = new RaidReviewClipInfo[0];
        private RaidReviewClipInfo[] _raidReviewClips = new RaidReviewClipInfo[0];

        /// <summary>Selected map on the location bar, or null for all of them.</summary>
        private string _raidReviewLocationFilter;
        private float _raidReviewListRefreshTime;
        private string _raidReviewFilter = "";
        private ConfigEntry<bool> _raidReviewNewestFirst;
        private ConfigEntry<bool> _raidReviewSortByLocation;
        private bool _raidReviewMaximized;
        private Rect _raidReviewRestoreRect;
        private int _raidReviewPendingDeleteIndex = -1;
        private float _raidReviewPendingDeleteUntil;
        private Vector2 _raidReviewScroll;
        private AudioSource _raidReviewAudioSource;
        private string _raidReviewNowPlaying;
        private int _raidReviewCurrentIndex = -1;
        private bool _raidReviewPaused;
        private bool _raidReviewWasPlaying;
        private float[] _raidReviewWaveform = new float[0];
        private float[] _raidReviewRawSamples;
        private float _raidReviewAppliedVolumeFactor = 1f;
        private Rect _raidReviewWindowRect = new Rect(-1f, -1f, 460f, 400f);
        private float _raidReviewOpenTime;
        private bool _raidReviewResizing;
        private Vector2 _raidReviewResizeStartMouse;
        private Vector2 _raidReviewResizeStartSize;

        private AudioSource _audioSource;
        private WavData _onSound;
        private WavData _offSound;

        private WavData _switchModeSound;
        private WavData _lowPowerSound;

        private readonly Dictionary<string, RadioSoundSet> _radioSoundSets = new Dictionary<string, RadioSoundSet>();
        private RadioSoundSet _defaultSoundSet;
        private string _activeRadioTplId;

        private struct WavData
        {
            public float[] Samples;
            public int Channels;
            public int SampleRate;
        }

        private struct RaidReviewClipInfo
        {
            public string Path;
            public string FileName;
            public string DisplayLabel;

            // Same data as DisplayLabel, kept unjoined so the Instrument journal can lay it out in
            // columns. Classic keeps rendering the single composed string it always did.
            public string TimeText;
            public string SpeakerText;
            public string DistanceText;
            public string RadioText;

            public string SearchText;
            public string Location;
            public float TimeOfDaySeconds;
            public float DurationSeconds;
            public bool StartsNewGroup;
            public bool IsProtected;
        }

        private struct RadioSoundSet
        {
            public WavData LocalStart;
            public WavData LocalEnd;
            public WavData RemoteStart;
            public WavData RemoteEnd;
        }

        private class ConfigurationManagerAttributes
        {
            public int? Order;
            public Action<ConfigEntryBase> CustomDrawer;
            public bool? Browsable;
        }

        private sealed class FileLogListener : ILogListener
        {
            private readonly StreamWriter _writer;
            private readonly string _sourceName;

            public FileLogListener(string filePath, string sourceName)
            {
                _sourceName = sourceName;
                var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                _writer = new StreamWriter(stream) { AutoFlush = true };
            }

            public void LogEvent(object sender, LogEventArgs eventArgs)
            {
                if (eventArgs.Source == null || eventArgs.Source.SourceName != _sourceName)
                {
                    return;
                }

                _writer.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss") + "] [" + eventArgs.Level + "] " + eventArgs.Data);
            }

            public void Dispose()
            {
                _writer.Dispose();
            }
        }

        private FileLogListener _fileLogListener;

        private sealed class LocalMicRecorder : Dissonance.Audio.Capture.IMicrophoneSubscriber
        {
            public volatile bool Recording;
            private readonly object _lock = new object();
            private List<float> _buffer = new List<float>();
            public int Channels;
            public int SampleRate;

            /// <summary>
            /// Peak level of the most recent microphone buffer, 0..1. Updated regardless of whether
            /// a recording is in progress, so the transmit meter works even with recording disabled.
            /// </summary>
            public float InputLevel { get; private set; }

            public void ReceiveMicrophoneData(ArraySegment<float> buffer, NAudio.Wave.WaveFormat format)
            {
                float peak = 0f;
                for (int i = buffer.Offset; i < buffer.Offset + buffer.Count; i++)
                {
                    float a = buffer.Array[i] < 0f ? -buffer.Array[i] : buffer.Array[i];
                    if (a > peak)
                    {
                        peak = a;
                    }
                }
                InputLevel = peak;

                if (!Recording)
                {
                    return;
                }

                lock (_lock)
                {
                    Channels = format.Channels;
                    SampleRate = format.SampleRate;
                    for (int i = buffer.Offset; i < buffer.Offset + buffer.Count; i++)
                    {
                        _buffer.Add(buffer.Array[i]);
                    }
                }
            }

            public void Reset()
            {
                lock (_lock)
                {
                    _buffer.Clear();
                }
            }

            public void Flush(out float[] samples, out int channels, out int sampleRate)
            {
                lock (_lock)
                {
                    samples = _buffer.ToArray();
                    channels = Channels;
                    sampleRate = SampleRate;
                    _buffer.Clear();
                }
            }
        }

        private LocalMicRecorder _localMicRecorder = new LocalMicRecorder();
        private bool _micRecorderSubscribed;

        private static ConfigDescription Desc(string text, int order)
        {
            return new ConfigDescription(text, null, new ConfigurationManagerAttributes { Order = order });
        }

        private static ConfigDescription Desc(string text, AcceptableValueBase range, int order)
        {
            return new ConfigDescription(text, range, new ConfigurationManagerAttributes { Order = order });
        }

        private static ConfigDescription DescButton(string text, Action<ConfigEntryBase> drawer, int order)
        {
            return new ConfigDescription(text, null, new ConfigurationManagerAttributes { Order = order, CustomDrawer = drawer });
        }

        private static ConfigDescription DescHidden()
        {
            return new ConfigDescription("", null, new ConfigurationManagerAttributes { Browsable = false });
        }

        private void LogVerbose(string message)
        {
            if (_verboseLogging != null && _verboseLogging.Value)
            {
                Logger.LogInfo(message);
            }
        }

        /// <summary>
        /// Static entry point so the item-attribute patch can report what it actually sees on a
        /// radio. Always logged: it fires once per session, not per frame.
        /// </summary>
        internal static void LogAttributeDiagnostic(string message)
        {
            _instance?.Logger.LogInfo(message);
        }

        private static Plugin _instance;

        private void Awake()
        {
            _instance = this;

            // File and folder names deliberately keep the original "prt-fika" naming so that a
            // drag-and-drop update overwrites the previous install instead of leaving a second
            // copy of the mod alongside it.
            string logPath = Path.Combine(Path.GetDirectoryName(Info.Location), "prt-fika.log");
            _fileLogListener = new FileLogListener(logPath, Logger.SourceName);
            BepInEx.Logging.Logger.Listeners.Add(_fileLogListener);

            // Section order in the F12 menu follows FIRST-registration order, not the Order number
            // (that only sorts entries within a section) — so the nine Config.Bind groups below run
            // in exactly the sequence the sections should appear: Interface, Hotkeys, Volume, Radio,
            // Notifications, Indicators, Colors, Raid Recordings, Developer (Developer always last
            // on purpose). The numeric prefixes are kept in the names as well, so the intended order
            // still reads correctly anywhere the sections happen to be sorted by name instead.

            // ---- 0. Interface ----
            // A new section rather than an existing one: renaming a section orphans its values in
            // the .cfg and silently resets them, while adding one costs existing players nothing.
            _uiStyle = Config.Bind(
                "0. Interface",
                "UI Style",
                UiStyle.Classic,
                Desc("Classic is the look from previous versions and never changes. Instrument is the military-radio redesign. Switching takes effect immediately, no restart needed.", 10));

            UiStyleState.Bind(_uiStyle);

            _soundStyle = Config.Bind(
                "2. Volume",
                "Transmission Cues",
                SoundStyle.Classic,
                Desc("Start and end cues for transmitting and receiving. Classic uses the four recorded sets shared between the radios. PerRadio synthesises an individual cue for each of the thirteen: a PTT click plus the tone its signalling family would use, shaped by that radio own passband. The recorded files are kept either way.", 5));

            _interferenceCharacter = Config.Bind(
                "3. Radio",
                "Interference Character",
                InterferenceCharacter.Classic,
                Desc("How radios degrade at the edge of range. Classic is the behaviour from previous versions: one hiss curve for every radio. PerFamily makes it depend on the set - digital radios (DMR, TETRA, P25) stay clean and then drop whole words instead of hissing, analogue ones fade into noise, military ones keep a low noise floor. Does not change the radios themselves, only how their loss of signal sounds.", 40));

            _instrumentSignalStyle = Config.Bind(
                "0. Interface",
                "Signal Readout (Instrument)",
                InstrumentSignalStyle.SMeter,
                Desc("Signal presentation in the Instrument style. SMeter = needle across an S1-S9 scale with the figure in dBm. Bars = a segmented strip. Dbm = the number only. ArcGauge = a curved scale. VuNeedle = a boxed analogue meter. Ignored while UI Style is Classic.", 20));

            _instrumentStateStyle = Config.Bind(
                "0. Interface",
                "State Readout (Instrument)",
                InstrumentStateStyle.LcdReadout,
                Desc("How TX / RX / PWR / DUP are shown in the Instrument style. LcdReadout = lettering on a plate. Dots and Blocks = a lamp beside each caption. Stencil = painted markings. Pills = filled plates. Ignored while UI Style is Classic.", 22));

            _instrumentBatteryStyle = Config.Bind(
                "0. Interface",
                "Battery Readout (Instrument)",
                InstrumentBatteryStyle.Segments,
                Desc("How battery charge is shown in the Instrument style. Segments = a segmented bar with the figure. Percent = the figure only. Volts = nominal pack voltage instead of a percentage. Only visible when a battery mod gives the radio battery slots.", 24));

            _showRadioNameplate = Config.Bind(
                "0. Interface",
                "Radio Nameplate (Instrument)",
                false,
                Desc("Show the model and tier of the active radio on the chassis. Off by default: you already know which radio you are carrying, and the row costs vertical space in the corner of the screen.", 30));

            _previewScale = Config.Bind(
                "0. Interface",
                "Preview Scale",
                2f,
                Desc("How much the settings preview magnifies the chassis. Affects the preview only, never the HUD in a raid.", new AcceptableValueRange<float>(1f, 4f), 40));

            // ---- 1. Hotkeys ----
            _radioToggleModifier = Config.Bind(
                "1. Hotkeys",
                "Toggle Radio",
                KeyCode.RightControl,
                Desc("Turn the radio on/off (default: K)", 30));

            _selectRadioModifier = Config.Bind(
                "1. Hotkeys",
                "Select Radio",
                KeyCode.RightShift,
                Desc("Switch to another radio if you're carrying more than one", 20));

            _duplexModeModifier = Config.Bind(
                "1. Hotkeys",
                "Half-duplex / Duplex Mode",
                KeyCode.Return,
                Desc("Toggle Half-duplex/Duplex (whether you can hear others while transmitting; not available on every radio)", 10));

            // ---- 2. Volume ----
            _receiveVolume = Config.Bind(
                "2. Volume",
                "Receive Volume",
                1f,
                Desc("Volume of the other person's voice as heard over the radio. Above 1 boosts quiet voices but may introduce distortion.", new AcceptableValueRange<float>(0.05f, 5f), 30));

            _noiseVolume = Config.Bind(
                "2. Volume",
                "Noise Volume",
                1f,
                Desc("Radio static/noise volume (minimum 5%, default 1)", new AcceptableValueRange<float>(0.05f, 1f), 20));

            _soundVolume = Config.Bind(
                "2. Volume",
                "Sound Volume",
                1f,
                Desc("Volume of radio sound effects (on/off clicks, start/end of transmission)", new AcceptableValueRange<float>(0.05f, 1f), 10));

            // ---- 3. Radio (core behavior — not visual/notification settings) ----
            _uiLanguageOverride = Config.Bind(
                "3. Radio",
                "UI Language",
                UiLanguage.Auto,
                Desc("Language used for on-screen notifications and the Raid Recordings browser. 'Auto' follows the game's current language.", 30));


            _ambientCombatSoundEnabled = Config.Bind(
                "3. Radio",
                "Ambient Combat Sound (Experimental)",
                true,
                Desc("Simulated background combat sound in the radio channel: if there was recent gunfire near the speaker, "
                    + "a procedural crackle is mixed into their transmission. This is a SIMULATION (proximity gunfire "
                    + "detection), not real ambient audio capture.", 10));

            // ---- 4. Notifications ----
            _showNotifications = Config.Bind(
                "4. Notifications",
                "Show Notifications",
                true,
                Desc("Show on-screen notifications about radio state (on/off, selection, mode). Custom overlay, no vanilla notification sound.", 50));

            _notificationStyleMode = Config.Bind(
                "4. Notifications",
                "Notification Style",
                NotificationStyle.Themed,
                Desc("Look of the on-screen notifications. 'Themed' is the full military panel with a type tag and a remaining-time bar; 'Minimal' is just an accent stripe and the message. The 'Compact' variants of each are narrower with a smaller font, for less screen clutter. Colours always follow the BEAR/USEC theme.", 40));

            _notificationTheme = Config.Bind(
                "4. Notifications",
                "Notification Theme",
                NotificationTheme.FollowWindow,
                Desc("Colour scheme of the notifications. 'FollowWindow' uses the same faction theme as the Raid Recordings window; BEAR/USEC pins them independently, so you can pair a USEC window with BEAR notifications.", 30));

            _notificationOpacity = Config.Bind(
                "4. Notifications",
                "Notification Opacity",
                1f,
                new ConfigDescription(
                    "Overall opacity of the notification panels. Lower values make them less obtrusive during a raid.",
                    new AcceptableValueRange<float>(0.2f, 1f),
                    new ConfigurationManagerAttributes { Order = 20 }));

            _notificationScale = Config.Bind(
                "4. Notifications",
                "Notification Scale",
                1f,
                Desc("Size of the on-screen notification popups.",
                    new AcceptableValueRange<float>(0.5f, 3f), 10));

            // ---- 5. Indicators ----
            _indicatorStyle = Config.Bind(
                "5. Indicators",
                "Indicator Style",
                IndicatorStyle.FullBadges,
                Desc("Dots = classic round lamps only. Blocks = angular military segments only. Badges = only the labelled ON AIR / RECEIVING meters driven by live audio levels. FullDots = round lamps plus those meters. FullBadges = angular segments plus those meters.", 130));

            _indicatorScale = Config.Bind(
                "5. Indicators",
                "Indicator Scale",
                1f,
                Desc("Size of the on-screen status indicators (dots/badges/battery/signal). Does not affect the Raid Recordings window, which already resizes by dragging its corner.",
                    new AcceptableValueRange<float>(0.5f, 3f), 120));

            _indicatorOpacity = Config.Bind(
                "5. Indicators",
                "Indicator Opacity",
                1f,
                Desc("Opacity of the status dots (minimum 5%)", new AcceptableValueRange<float>(0.05f, 1f), 110));

            _fadeIdleIndicators = Config.Bind(
                "5. Indicators",
                "Fade Indicators When Idle",
                true,
                Desc("Dim the indicators down while nothing is happening on the channel, and bring them back to full brightness on activity. Reduces visual clutter during quiet stretches.", 100));

            _showPowerIndicator = Config.Bind(
                "5. Indicators",
                "Power Indicator",
                true,
                Desc("Small dot showing whether the radio is on (green) or off (gray)", 90));

            _showTalkingIndicator = Config.Bind(
                "5. Indicators",
                "Talking Indicator",
                true,
                Desc("Dot that appears only while YOU are transmitting on the radio.", 80));

            _showBusyIndicator = Config.Bind(
                "5. Indicators",
                "Channel Busy Indicator",
                true,
                Desc("Red dot that appears only while someone is transmitting on your frequency.", 70));

            _showDuplexIndicator = Config.Bind(
                "5. Indicators",
                "Half-duplex/Duplex Indicator",
                true,
                Desc("Small dot showing the current Half-duplex/Duplex mode. Only visible while the radio is on.", 60));

            _showSignalIndicator = Config.Bind(
                "5. Indicators",
                "Signal Strength Indicator",
                true,
                Desc("Shows signal quality of the current incoming transmission (see style below). Only appears while you "
                    + "can actually hear someone. Independent toggle from the other indicators.", 50));

            _signalIndicatorStyle = Config.Bind(
                "5. Indicators",
                "Signal Indicator Style",
                SignalIndicatorStyle.Bar,
                Desc("Bar = a single fillable strip. AntennaBars = classic phone-style signal bars.", 40));

            _showBatteryIndicator = Config.Bind(
                "5. Indicators",
                "Show Battery Charge",
                true,
                Desc("Show the active radio's battery charge on screen. Only appears when a battery mod (e.g. Batteries Not Included) actually gives the radio battery slots — without one there is nothing to display.", 30));

            _batteryIndicatorStyle = Config.Bind(
                "5. Indicators",
                "Battery Indicator Style",
                BatteryIndicatorStyle.Cell,
                Desc("Cell = a battery pictogram with charge segments. Bar = a thin segmented strip. Percent = numeric readout only. CellAndPercent / BarAndPercent add the number next to the pictogram or strip. Shown only while the radio is on.", 20));

            _showTuningSweep = Config.Bind(
                "5. Indicators",
                "Radio Tuning Sweep",
                true,
                Desc("Show a brief frequency-dial sweep on screen when switching between radios. Purely cosmetic — it changes nothing about how the radio works.", 10));

            // ---- 6. Colors ----
            _colorOn = Config.Bind(
                "6. Colors",
                "Radio-On Color",
                Color.green,
                Desc("Color used for the Power indicator (radio on) and the matching notification text", 60));

            _colorTalking = Config.Bind(
                "6. Colors",
                "Talking Indicator Color",
                new Color(1f, 0.55f, 0f),
                Desc("Color used for the Talking indicator (you are transmitting)", 50));

            _colorBusy = Config.Bind(
                "6. Colors",
                "Channel Busy Color",
                Color.red,
                Desc("Color used for the Channel Busy indicator", 40));

            _colorSimplex = Config.Bind(
                "6. Colors",
                "Duplex Mode Color",
                Color.yellow,
                Desc("Color used for the Half-duplex/Duplex indicator (Duplex) and the matching notification text", 30));

            _colorSignalBar = Config.Bind(
                "6. Colors",
                "Signal Bar Fill Color",
                Color.white,
                Desc("Fill color of the signal strength indicator (bar or antenna style)", 20));

            _colorSelect = Config.Bind(
                "6. Colors",
                "Radio Selection Color",
                Color.cyan,
                Desc("Color used for radio-selection notification text", 10));

            // ---- 7. Raid Recordings ----
            _raidReviewKey = Config.Bind(
                "7. Raid Recordings",
                "Open Raid Recordings",
                new KeyboardShortcut(KeyCode.F9),
                Desc("Key (or key combination) to open/close the Raid Recordings browser, where you can play back recorded radio comms from past raids.", 60));

            _recordRadioComms = Config.Bind(
                "7. Raid Recordings",
                "Record Radio Comms",
                false,
                Desc("Record everything you hear over the radio during a raid to a WAV file, so you can listen back to it afterwards. Off by default — opt in if you want it. Recordings are saved under the plugin folder in a 'PRT-Records' subfolder.", 50));

            _uiTheme = Config.Bind(
                "7. Raid Recordings",
                "Window Theme",
                UiTheme.Auto,
                Desc("Visual theme of the Raid Recordings window. 'Auto' follows your character's faction: BEAR shows a Russian Armed Forces field terminal, USEC shows a UNTAR peacekeeping console.", 40));

            _spectrogramWaveform = Config.Bind(
                "7. Raid Recordings",
                "Spectrogram View",
                false,
                Desc("Draw the recording as a spectrogram (frequency content over time) instead of a peak waveform. Speech and static look clearly different, making it easier to spot where someone is actually talking.", 30));

            _autoCleanupFrequency = Config.Bind(
                "7. Raid Recordings",
                "Auto-Cleanup Frequency",
                CleanupFrequency.Never,
                Desc("Automatically delete all saved raid recordings after this many completed raids. 'Never' keeps everything until you clear manually.", 20));

            _clearRecordingsButton = Config.Bind(
                "7. Raid Recordings",
                "Clear All Recordings Now",
                false,
                DescButton("Deletes every saved raid recording immediately.", DrawClearRecordingsButton, 10));

            _raidsSinceCleanup = Config.Bind("7. Raid Recordings", "RaidsSinceCleanupInternal", 0, DescHidden());
            _raidReviewPlaybackVolume = Config.Bind("7. Raid Recordings", "RaidReviewPlaybackVolumeInternal", 1f, DescHidden());
            _raidReviewAutoAdvance = Config.Bind("7. Raid Recordings", "RaidReviewAutoAdvanceInternal", true, DescHidden());
            _raidReviewNewestFirst = Config.Bind("7. Raid Recordings", "RaidReviewNewestFirstInternal", true, DescHidden());
            _raidReviewSortByLocation = Config.Bind("7. Raid Recordings", "RaidReviewSortByLocationInternal", false, DescHidden());

            // Persisted so the window can still show a callsign in a session where no raid has
            // been entered yet — the live Player object only exists inside a raid.
            _lastKnownNickname = Config.Bind("7. Raid Recordings", "LastKnownNicknameInternal", "", DescHidden());
            _lastKnownSide = Config.Bind("7. Raid Recordings", "LastKnownSideInternal", "", DescHidden());

            // ---- 8. Developer ---- (bound last on purpose: sections list in registration order,
            // so registering this section last is what puts it at the bottom of the menu.)
            _verboseLogging = Config.Bind(
                "8. Developer",
                "Verbose Logging",
                false,
                Desc("Log detailed per-frame/per-event debug info to the BepInEx console and prt-fika.log. Only enable when troubleshooting an issue — leave off for normal play to keep the log file small.", 20));

            _batteryDrainMultiplier = Config.Bind(
                "8. Developer",
                "Battery Drain Multiplier",
                1f,
                new ConfigDescription(
                    "Speeds up or slows down how fast the radio battery drains, for testing. 1 = normal. Changes are only picked up outside a raid, so the rate cannot be altered mid-raid.",
                    new AcceptableValueRange<float>(0.1f, 20f),
                    new ConfigurationManagerAttributes { Order = 10 }));

            _harmony = new Harmony("com.suomi.makshepard.smprt.patches");
            _harmony.PatchAll();
            LinkedSearchHidePatch.Apply(_harmony);
            BatteryIconIndicatorPatch.Apply(_harmony);

            Logger.LogInfo("PRT " + DisplayVersion + " loaded");

            _onSound = LoadWavData("on.wav");
            _offSound = LoadWavData("off.wav");
            _switchModeSound = LoadWavData("swtch.wav");
            _lowPowerSound = LoadWavData("low_pwr.wav");

            RadioSoundSet swSet = LoadSoundSet("sw");
            RadioSoundSet ddSet = LoadSoundSet("dd");
            RadioSoundSet abSet = LoadSoundSet("ab");
            RadioSoundSet lrSet = LoadSoundSet("lr");

            _defaultSoundSet = swSet;

            _radioSoundSets[KenwoodTplId] = ddSet;
            _radioSoundSets[BaofengTplId] = swSet;
            _radioSoundSets[T460TplId] = swSet;
            _radioSoundSets[YaesuTplId] = swSet;
            _radioSoundSets[Dp4800TplId] = swSet;
            _radioSoundSets[Dp4601eTplId] = abSet;
            _radioSoundSets[Xts5000TplId] = abSet;
            _radioSoundSets[HarrisTplId] = lrSet;
            _radioSoundSets[AzartTplId] = lrSet;

            _radioSoundSets[Trc83TplId] = ddSet;
            _radioSoundSets[AlincoTplId] = swSet;
            _radioSoundSets[KenwoodProTalkTplId] = swSet;
            _radioSoundSets[Mth800TplId] = abSet;

            Logger.LogInfo("PRT: plugin loaded");
        }

        private void OnDestroy()
        {
            if (_fileLogListener != null)
            {
                BepInEx.Logging.Logger.Listeners.Remove(_fileLogListener);
                _fileLogListener.Dispose();
                _fileLogListener = null;
            }
        }

        private RadioSoundSet LoadSoundSet(string subfolder)
        {
            return new RadioSoundSet
            {
                LocalStart = LoadWavData("local_start.wav", subfolder),
                LocalEnd = LoadWavData("local_end.wav", subfolder),
                RemoteStart = LoadWavData("remote_start.wav", subfolder),
                RemoteEnd = LoadWavData("remote_end.wav", subfolder),
            };
        }

        private WavData LoadWavData(string fileName, string subfolder = null)
        {
            if (!TryLoadWavData(fileName, subfolder, out float[] samples, out int channels, out int sampleRate))
            {
                return default;
            }

            return new WavData { Samples = samples, Channels = channels, SampleRate = sampleRate };
        }

        private bool TryLoadWavData(string fileName, string subfolder, out float[] samples, out int channels, out int sampleRate)
        {
            samples = null;
            channels = 0;
            sampleRate = 0;

            string resourceName = "RadioMod.Client.Sounds." + (subfolder != null ? subfolder + "." : "") + fileName;
            byte[] data;
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Logger.LogWarning("PRT: embedded sound not found: " + resourceName);
                    return false;
                }

                data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
            }

            if (!ParseWavBytes(data, fileName, out samples, out channels, out sampleRate))
            {
                return false;
            }

            // Verbose only: there are ~20 embedded clips, and listing every one of them buried the
            // handful of lines that actually matter on a normal startup. Failures still log a
            // warning unconditionally just above.
            LogVerbose("PRT: WAV loaded from embedded resource: " + fileName
                + " | channels=" + channels + " | rate=" + sampleRate + " | samples=" + samples.Length);
            return true;
        }

        private bool TryLoadWavFile(string path, out float[] samples, out int channels, out int sampleRate)
        {
            samples = null;
            channels = 0;
            sampleRate = 0;

            byte[] data;
            try
            {
                data = File.ReadAllBytes(path);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: could not read raid review clip file '" + path + "': " + ex.Message);
                return false;
            }

            return ParseWavBytes(data, path, out samples, out channels, out sampleRate);
        }

        private bool ParseWavBytes(byte[] data, string labelForLogging, out float[] samples, out int channels, out int sampleRate)
        {
            samples = null;
            channels = 0;
            sampleRate = 0;

            if (data.Length < 44 || data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F')
            {
                Logger.LogWarning("PRT: not a WAV file: " + labelForLogging);
                return false;
            }

            channels = BitConverter.ToInt16(data, 22);
            sampleRate = BitConverter.ToInt32(data, 24);
            short bitsPerSample = BitConverter.ToInt16(data, 34);
            if (bitsPerSample != 16)
            {
                Logger.LogWarning("PRT: only 16-bit PCM is supported, but " + labelForLogging + " is " + bitsPerSample + "-bit");
                return false;
            }

            int pos = 12;
            int dataOffset = -1;
            int dataSize = 0;
            while (pos + 8 <= data.Length)
            {
                bool isData = data[pos] == 'd' && data[pos + 1] == 'a' && data[pos + 2] == 't' && data[pos + 3] == 'a';
                int chunkSize = BitConverter.ToInt32(data, pos + 4);
                if (isData)
                {
                    dataOffset = pos + 8;
                    dataSize = Math.Min(chunkSize, data.Length - dataOffset);
                    break;
                }
                pos += 8 + chunkSize + (chunkSize & 1);
            }

            if (dataOffset < 0)
            {
                Logger.LogWarning("PRT: data chunk not found in " + labelForLogging);
                return false;
            }

            int sampleCount = dataSize / 2;
            samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = BitConverter.ToInt16(data, dataOffset + i * 2) / 32768f;
            }

            return true;
        }

        private string _raidReviewRootFolder;

        private string GetRaidReviewRootFolder()
        {
            if (_raidReviewRootFolder == null)
            {
                string pluginDir = Path.GetDirectoryName(Info.Location);
                string newFolder = Path.Combine(pluginDir, "PRT-Records");
                string oldFolder = Path.Combine(pluginDir, "RaidReviews");

                // One-time migration: an existing 'RaidReviews' folder from before the rename holds
                // real recordings the player made, not disposable cache — move it forward instead of
                // leaving it orphaned or silently starting a second, empty folder next to it.
                if (!Directory.Exists(newFolder) && Directory.Exists(oldFolder))
                {
                    try
                    {
                        Directory.Move(oldFolder, newFolder);
                    }
                    catch (Exception ex)
                    {
                        LogVerbose("PRT: could not migrate RaidReviews -> PRT-Records: " + ex.Message);
                    }
                }

                _raidReviewRootFolder = newFolder;
            }

            return _raidReviewRootFolder;
        }

        private string GetRaidReviewFolderForToday()
        {
            string dayFolder = Path.Combine(GetRaidReviewRootFolder(), DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dayFolder);
            return dayFolder;
        }

        private static string SanitizeFileNamePart(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            // "--" is the field delimiter in recording filenames (see SaveRecordingWav/ParseRecordingFileName);
            // collapse any occurrence coming from arbitrary data (e.g. a player nickname) so it can't be mistaken for one.
            return new string(chars).Replace("--", "-");
        }

        private static GameWorld GetGameWorldOrNull()
        {
            return Singleton<GameWorld>.Instantiated ? Singleton<GameWorld>.Instance : null;
        }

        private string GetCurrentLocationId()
        {
            string locationId = GetGameWorldOrNull()?.LocationId;
            return string.IsNullOrEmpty(locationId) ? "unknown" : locationId;
        }

        private void SaveRecordingWav(string speakerName, float[] samples, int channels, int sampleRate, float distanceMeters, string radioName)
        {
            try
            {
                string folder = GetRaidReviewFolderForToday();
                string distanceLabel = distanceMeters < 0f ? "unkm" : Mathf.RoundToInt(distanceMeters) + "m";
                string location = SanitizeFileNamePart(GetCurrentLocationId());
                string radio = SanitizeFileNamePart(string.IsNullOrEmpty(radioName) ? "unknown" : radioName);
                string fileName = DateTime.Now.ToString("HH-mm-ss") + "--" + SanitizeFileNamePart(speakerName) + "--" + distanceLabel + "--" + radio + "--" + location + ".wav";
                string path = Path.Combine(folder, fileName);
                WriteWavFile(path, samples, channels, sampleRate);
                LogVerbose("PRT: raid review clip saved: " + path);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: failed to save raid review clip for '" + speakerName + "': " + ex.Message);
            }
        }

        private static float[] AdaptCue(WavData cue, int targetChannels, float volume)
        {
            if (cue.Samples == null || cue.Samples.Length == 0 || cue.Channels <= 0)
            {
                return new float[0];
            }

            if (cue.Channels == targetChannels)
            {
                return ApplyPlaybackGain(cue.Samples, volume);
            }

            int frames = cue.Samples.Length / cue.Channels;
            float[] result = new float[frames * targetChannels];
            for (int f = 0; f < frames; f++)
            {
                float mono = 0f;
                for (int ch = 0; ch < cue.Channels; ch++)
                {
                    mono += cue.Samples[f * cue.Channels + ch];
                }
                mono = Mathf.Clamp((mono / cue.Channels) * volume, -1f, 1f);
                for (int ch = 0; ch < targetChannels; ch++)
                {
                    result[f * targetChannels + ch] = mono;
                }
            }
            return result;
        }

        private static float[] MixInCues(float[] voiceSamples, int channels, WavData startCue, WavData endCue, float cueVolume)
        {
            float[] startPart = AdaptCue(startCue, channels, cueVolume);
            float[] endPart = AdaptCue(endCue, channels, cueVolume);

            float[] result = new float[startPart.Length + voiceSamples.Length + endPart.Length];
            Array.Copy(startPart, 0, result, 0, startPart.Length);
            Array.Copy(voiceSamples, 0, result, startPart.Length, voiceSamples.Length);
            Array.Copy(endPart, 0, result, startPart.Length + voiceSamples.Length, endPart.Length);
            return result;
        }

        /// <summary>Transmissions shorter than this are discarded — accidental key taps would
        /// otherwise fill the folder with unusable fragments.</summary>
        private const float MinRecordingSeconds = 1.5f;

        private void ProcessAndSaveRecording(string speakerName, float[] drySamples, int channels, int sampleRate,
            RadioVoiceFilter.Mode mode, float ratio, RadioVoiceFilter.Profile profile, float distanceMeters, bool isLocal, string radioName)
        {
            if (drySamples.Length == 0)
            {
                return;
            }

            if (channels > 0 && sampleRate > 0)
            {
                float durationSeconds = drySamples.Length / (float)(channels * sampleRate);
                if (durationSeconds < MinRecordingSeconds)
                {
                    LogVerbose("PRT: skipped a " + durationSeconds.ToString("0.00") + "s transmission from '"
                        + speakerName + "' (shorter than " + MinRecordingSeconds + "s)");
                    return;
                }
            }

            RadioVoiceFilter.ApplyOffline(drySamples, channels, sampleRate, profile, mode, ratio, 1f);

            RadioSoundSet soundSet = GetActiveSoundSet();
            WavData startCue = isLocal ? soundSet.LocalStart : soundSet.RemoteStart;
            WavData endCue = isLocal ? soundSet.LocalEnd : soundSet.RemoteEnd;

            float[] withCues = MixInCues(drySamples, channels, startCue, endCue, 1f);

            SaveRecordingWav(speakerName, withCues, channels, sampleRate, distanceMeters, radioName);
        }

        private static void ParseRecordingFileName(string fileNameWithoutExt, out string time, out string speaker, out string distanceLabel, out string radio, out string location)
        {
            string[] parts = fileNameWithoutExt.Split(new[] { "--" }, StringSplitOptions.None);
            if (parts.Length < 5)
            {
                time = fileNameWithoutExt;
                speaker = "";
                distanceLabel = "";
                radio = "";
                location = "";
                return;
            }

            time = parts[0];
            speaker = parts[1];
            distanceLabel = parts[2];
            radio = parts[3];
            location = parts[4];
        }

        private static void WriteWavFile(string path, float[] samples, int channels, int sampleRate)
        {
            int dataSize = samples.Length * 2;
            int byteRate = sampleRate * channels * 2;
            short blockAlign = (short)(channels * 2);

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    short sample = (short)Mathf.Clamp(samples[i] * 32768f, -32768f, 32767f);
                    writer.Write(sample);
                }
            }
        }

        private void DrawClearRecordingsButton(ConfigEntryBase entry)
        {
            if (GUILayout.Button("Clear All Recordings Now"))
            {
                ClearAllRecordings();
            }
        }

        /// <summary>A recording is protected from cleanup by an empty marker file beside it.</summary>
        private static string ProtectionMarkerPath(string wavPath)
        {
            return wavPath + ".keep";
        }

        private static bool IsRecordingProtected(string wavPath)
        {
            return File.Exists(ProtectionMarkerPath(wavPath));
        }

        private void ToggleRecordingProtection(string wavPath)
        {
            try
            {
                string marker = ProtectionMarkerPath(wavPath);
                if (File.Exists(marker))
                {
                    File.Delete(marker);
                }
                else
                {
                    File.WriteAllText(marker, "");
                }

                RefreshRaidReviewClips();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: failed to toggle protection for '" + wavPath + "': " + ex.Message);
            }
        }

        private void DeleteSingleRecording(string wavPath)
        {
            try
            {
                if (_raidReviewNowPlaying == Path.GetFileName(wavPath))
                {
                    StopRaidReviewPlayback();
                }

                File.Delete(wavPath);

                string marker = ProtectionMarkerPath(wavPath);
                if (File.Exists(marker))
                {
                    File.Delete(marker);
                }

                Logger.LogInfo("PRT: deleted raid recording " + wavPath);
                RefreshRaidReviewClips();
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: failed to delete '" + wavPath + "': " + ex.Message);
            }
        }

        /// <summary>
        /// Deletes every recording except those carrying a protection marker. Deliberately does a
        /// per-file sweep instead of removing the whole folder tree, so protected clips survive
        /// both the manual button and the automatic post-raid cleanup.
        /// </summary>
        private void ClearAllRecordings()
        {
            try
            {
                string root = GetRaidReviewRootFolder();
                if (!Directory.Exists(root))
                {
                    RefreshRaidReviewDays();
                    return;
                }

                int deleted = 0;
                int kept = 0;

                foreach (string dayFolder in Directory.GetDirectories(root))
                {
                    foreach (string wav in Directory.GetFiles(dayFolder, "*.wav"))
                    {
                        if (!wav.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (IsRecordingProtected(wav))
                        {
                            kept++;
                            continue;
                        }

                        File.Delete(wav);
                        deleted++;
                    }

                    // Drop the day folder only once nothing of value is left in it.
                    if (Directory.GetFiles(dayFolder).Length == 0)
                    {
                        Directory.Delete(dayFolder, true);
                    }
                }

                RefreshRaidReviewDays();
                Logger.LogInfo("PRT: raid recordings cleared, deleted=" + deleted + ", kept protected=" + kept);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: failed to clear raid recordings: " + ex.Message);
            }
        }

        private int GetRaidsPerCleanup()
        {
            switch (_autoCleanupFrequency.Value)
            {
                case CleanupFrequency.EveryRaid: return 1;
                case CleanupFrequency.Every3Raids: return 3;
                case CleanupFrequency.Every5Raids: return 5;
                case CleanupFrequency.Every10Raids: return 10;
                default: return 0;
            }
        }

        private GameWorld _lastSeenGameWorld;

        private void CheckRaidEndForAutoCleanup()
        {
            GameWorld currentGameWorld = GetGameWorldOrNull();
            if (_lastSeenGameWorld != null && currentGameWorld == null)
            {
                int raidsPerCleanup = GetRaidsPerCleanup();
                if (raidsPerCleanup > 0)
                {
                    _raidsSinceCleanup.Value++;
                    if (_raidsSinceCleanup.Value >= raidsPerCleanup)
                    {
                        _raidsSinceCleanup.Value = 0;
                        ClearAllRecordings();
                    }
                }
            }

            _lastSeenGameWorld = currentGameWorld;
        }

        private void RefreshRaidReviewDays()
        {
            _raidReviewNowPlaying = null;
            string root = GetRaidReviewRootFolder();
            if (!Directory.Exists(root))
            {
                _raidReviewDays = new string[0];
                _raidReviewClips = new RaidReviewClipInfo[0];
                return;
            }

            _raidReviewDays = Directory.GetDirectories(root)
                .Select(Path.GetFileName)
                .OrderByDescending(name => name, StringComparer.Ordinal)
                .ToArray();

            _raidReviewDayIndex = 0;
            RefreshRaidReviewClips();
        }

        /// <summary>
        /// Reads only the WAV header to get a clip's length, so refreshing a folder full of
        /// recordings never has to decode any audio.
        /// </summary>
        private static float ReadWavDurationSeconds(string path)
        {
            try
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (stream.Length < 44 || new string(reader.ReadChars(4)) != "RIFF")
                    {
                        return 0f;
                    }

                    reader.ReadInt32();
                    if (new string(reader.ReadChars(4)) != "WAVE")
                    {
                        return 0f;
                    }

                    int byteRate = 0;
                    while (stream.Position + 8 <= stream.Length)
                    {
                        string chunkId = new string(reader.ReadChars(4));
                        int chunkSize = reader.ReadInt32();

                        if (chunkId == "fmt ")
                        {
                            reader.ReadInt16();
                            reader.ReadInt16();
                            reader.ReadInt32();
                            byteRate = reader.ReadInt32();
                            stream.Position += Math.Max(0, chunkSize - 12);
                        }
                        else if (chunkId == "data")
                        {
                            return byteRate > 0 ? chunkSize / (float)byteRate : 0f;
                        }
                        else
                        {
                            stream.Position += chunkSize;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // A partially written or locked file just shows no duration.
            }

            return 0f;
        }

        private static float ParseTimeOfDaySeconds(string hhmmss)
        {
            string[] parts = hhmmss.Split('-');
            if (parts.Length != 3
                || !int.TryParse(parts[0], out int h)
                || !int.TryParse(parts[1], out int m)
                || !int.TryParse(parts[2], out int s))
            {
                return 0f;
            }

            return h * 3600f + m * 60f + s;
        }

        private static RaidReviewClipInfo BuildClipInfo(string path)
        {
            ParseRecordingFileName(Path.GetFileNameWithoutExtension(path), out string time, out string speaker,
                out string distanceLabel, out string radio, out string location);

            float duration = ReadWavDurationSeconds(path);

            string radioSuffix = string.IsNullOrEmpty(radio) ? "" : "  <" + radio.ToUpperInvariant() + ">";
            string durationSuffix = duration > 0f ? "  " + FormatClipTime(duration) : "";
            string label = time + "  " + speaker.ToUpperInvariant() + "  (" + distanceLabel + ")  ["
                + location.ToUpperInvariant() + "]" + radioSuffix + durationSuffix;

            return new RaidReviewClipInfo
            {
                Path = path,
                FileName = Path.GetFileName(path),
                DisplayLabel = label,
                TimeText = time,
                SpeakerText = speaker.ToUpperInvariant(),
                DistanceText = distanceLabel,
                RadioText = string.IsNullOrEmpty(radio) ? "" : radio.ToUpperInvariant(),
                SearchText = (speaker + " " + location + " " + radio).ToLowerInvariant(),
                Location = location,
                TimeOfDaySeconds = ParseTimeOfDaySeconds(time),
                DurationSeconds = duration,
                // Resolved once per refresh: checking the marker file per row per frame would put
                // disk I/O in the render loop.
                IsProtected = IsRecordingProtected(path),
            };
        }

        private string GetCurrentDayFolder()
        {
            return _raidReviewDays.Length == 0
                ? null
                : Path.Combine(GetRaidReviewRootFolder(), _raidReviewDays[_raidReviewDayIndex]);
        }

        private void RefreshRaidReviewClips()
        {
            string dayFolder = GetCurrentDayFolder();

            // Explicit extension check: the "*.wav" pattern can also match "<name>.wav.keep"
            // marker files because of legacy 8.3 short-name matching.
            _raidReviewAllClips = dayFolder != null && Directory.Exists(dayFolder)
                ? Directory.GetFiles(dayFolder, "*.wav")
                    .Where(p => p.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    .Select(BuildClipInfo)
                    .ToArray()
                : new RaidReviewClipInfo[0];

            ApplyRaidReviewFilterAndSort();
        }

        /// <summary>
        /// Rebuilds the visible list from the loaded clips. Runs on refresh and whenever the
        /// filter or sort order changes — never per frame.
        /// </summary>
        private void ApplyRaidReviewFilterAndSort()
        {
            // Any index into the previous ordering is meaningless once the list is rebuilt.
            _raidReviewCurrentIndex = -1;

            IEnumerable<RaidReviewClipInfo> query = _raidReviewAllClips;

            // Location filter from the map bar. Applied before the text search so the counts shown
            // on the bar always describe the whole day, not whatever the search has narrowed it to.
            if (!string.IsNullOrEmpty(_raidReviewLocationFilter))
            {
                query = query.Where(c =>
                    string.Equals(c.Location, _raidReviewLocationFilter, StringComparison.OrdinalIgnoreCase));
            }

            string filter = (_raidReviewFilter ?? string.Empty).Trim().ToLowerInvariant();
            if (filter.Length > 0)
            {
                query = query.Where(c => c.SearchText != null && c.SearchText.Contains(filter));
            }

            if (_raidReviewSortByLocation.Value)
            {
                // Group all traffic from the same map together, still chronological inside a group.
                query = _raidReviewNewestFirst.Value
                    ? query.OrderBy(c => c.Location, StringComparer.OrdinalIgnoreCase)
                        .ThenByDescending(c => c.FileName, StringComparer.Ordinal)
                    : query.OrderBy(c => c.Location, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(c => c.FileName, StringComparer.Ordinal);
            }
            else
            {
                query = _raidReviewNewestFirst.Value
                    ? query.OrderByDescending(c => c.FileName, StringComparer.Ordinal)
                    : query.OrderBy(c => c.FileName, StringComparer.Ordinal);
            }

            RaidReviewClipInfo[] visible = query.ToArray();
            MarkRaidGroups(visible);

            _raidReviewClips = visible;
            _raidReviewListRefreshTime = Time.unscaledTime;
        }

        /// <summary>
        /// Flags the first row of each raid so the list can draw a separator. A raid boundary is
        /// inferred from a location change or a long gap in time, which keeps this purely derived —
        /// the stored filename format stays untouched.
        /// </summary>
        private static void MarkRaidGroups(RaidReviewClipInfo[] clips)
        {
            const float raidGapSeconds = 15f * 60f;

            for (int i = 0; i < clips.Length; i++)
            {
                if (i == 0)
                {
                    clips[i].StartsNewGroup = true;
                    continue;
                }

                bool locationChanged = clips[i].Location != clips[i - 1].Location;
                bool bigTimeGap = Mathf.Abs(clips[i].TimeOfDaySeconds - clips[i - 1].TimeOfDaySeconds) > raidGapSeconds;
                clips[i].StartsNewGroup = locationChanged || bigTimeGap;
            }
        }

        /// <summary>Plays the clip at the given position in the currently displayed list.</summary>
        private void PlayRaidReviewIndex(int index)
        {
            if (index < 0 || index >= _raidReviewClips.Length)
            {
                return;
            }

            _raidReviewCurrentIndex = index;
            PlayRaidReviewClip(_raidReviewClips[index].Path);
        }

        private void StopRaidReviewPlayback()
        {
            if (_raidReviewAudioSource != null)
            {
                _raidReviewAudioSource.Stop();
            }

            _raidReviewPaused = false;
            _raidReviewWasPlaying = false;
            _raidReviewNowPlaying = null;
            _raidReviewCurrentIndex = -1;
        }

        private void ToggleRaidReviewPause()
        {
            if (_raidReviewAudioSource == null || _raidReviewAudioSource.clip == null || _raidReviewCurrentIndex < 0)
            {
                return;
            }

            if (_raidReviewPaused)
            {
                _raidReviewAudioSource.UnPause();
                _raidReviewPaused = false;
            }
            else
            {
                _raidReviewAudioSource.Pause();
                _raidReviewPaused = true;
            }
        }

        private void SeekRaidReviewBy(float deltaSeconds)
        {
            if (_raidReviewAudioSource == null || _raidReviewAudioSource.clip == null)
            {
                return;
            }

            float length = _raidReviewAudioSource.clip.length;
            // Unity throws if time is set to exactly the clip length, so stay just short of the end.
            _raidReviewAudioSource.time = Mathf.Clamp(_raidReviewAudioSource.time + deltaSeconds, 0f, Mathf.Max(0f, length - 0.05f));
        }

        /// <summary>
        /// Detects a clip finishing on its own and advances to the next one when enabled.
        /// Paused playback also reports isPlaying == false, so the pause flag is checked first —
        /// otherwise pausing would be mistaken for the track ending.
        /// </summary>
        private void UpdateRaidReviewAutoAdvance()
        {
            if (_raidReviewAudioSource == null || _raidReviewCurrentIndex < 0 || _raidReviewPaused)
            {
                return;
            }

            bool playing = _raidReviewAudioSource.isPlaying;
            bool justFinished = _raidReviewWasPlaying && !playing;
            _raidReviewWasPlaying = playing;

            if (!justFinished)
            {
                return;
            }

            int nextIndex = _raidReviewCurrentIndex + 1;
            if (_raidReviewAutoAdvance.Value && nextIndex < _raidReviewClips.Length)
            {
                PlayRaidReviewIndex(nextIndex);
                return;
            }

            _raidReviewNowPlaying = null;
            _raidReviewCurrentIndex = -1;
        }

        private GUIStyle _waveTickStyle;

        /// <summary>Tick captions under the waveform: small, unobtrusive, never wrapped.</summary>
        private GUIStyle WaveTickStyle
        {
            get
            {
                if (_waveTickStyle == null)
                {
                    _waveTickStyle = new GUIStyle(MilStyle.DimLabel)
                    {
                        fontSize = UiTokens.SizeMicro,
                        alignment = TextAnchor.LowerLeft,
                        wordWrap = false,
                        clipping = TextClipping.Clip,
                    };
                    UiTokens.WithFont(_waveTickStyle);
                }

                return _waveTickStyle;
            }
        }

        private static string FormatClipTime(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds))
            {
                seconds = 0f;
            }

            int total = Mathf.FloorToInt(seconds);
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        private void PlayRaidReviewClip(string path)
        {
            Logger.LogInfo("PRT: Play clicked for raid review clip: " + path);

            if (!TryLoadWavFile(path, out float[] samples, out int channels, out int sampleRate))
            {
                Logger.LogWarning("PRT: failed to load raid review clip for playback: " + path);
                return;
            }

            Logger.LogInfo("PRT: raid review clip loaded, samples=" + samples.Length + " channels=" + channels + " sampleRate=" + sampleRate);

            _raidReviewRawSamples = samples;

            WavData wav = new WavData { Samples = ApplyPlaybackGain(samples, _raidReviewPlaybackVolume.Value), Channels = channels, SampleRate = sampleRate };
            AudioClip clip = BuildClip(wav, Path.GetFileName(path));
            if (clip == null)
            {
                Logger.LogWarning("PRT: BuildClip returned null for raid review clip: " + path);
                return;
            }

            _raidReviewAudioSource = EnsureAudioSource(ref _raidReviewAudioSource, "RadioMod_RaidReviewAudio");
            _raidReviewAudioSource.volume = 1f;
            _raidReviewAudioSource.clip = clip;
            _raidReviewAudioSource.Play();
            _raidReviewNowPlaying = Path.GetFileName(path);
            _raidReviewWaveform = BuildWaveformBars(samples, channels, 64);
            _raidReviewSpectrogram = _spectrogramWaveform.Value ? BuildSpectrogram(samples, channels) : null;
            _raidReviewAppliedVolumeFactor = _raidReviewPlaybackVolume.Value;
            _raidReviewPaused = false;
            _raidReviewWasPlaying = true;
            Logger.LogInfo("PRT: raid review clip playing, AudioSource.isPlaying=" + _raidReviewAudioSource.isPlaying
                + " volumeFactor=" + _raidReviewAppliedVolumeFactor);
        }

        private static float[] ApplyPlaybackGain(float[] rawSamples, float factor)
        {
            float[] outArr = new float[rawSamples.Length];
            for (int i = 0; i < rawSamples.Length; i++)
            {
                outArr[i] = Mathf.Clamp(rawSamples[i] * factor, -1f, 1f);
            }
            return outArr;
        }

        private void UpdatePlaybackVolumeIfChanged()
        {
            if (_raidReviewRawSamples == null || _raidReviewAudioSource == null || _raidReviewAudioSource.clip == null)
            {
                return;
            }

            if (Mathf.Approximately(_raidReviewAppliedVolumeFactor, _raidReviewPlaybackVolume.Value))
            {
                return;
            }

            _raidReviewAppliedVolumeFactor = _raidReviewPlaybackVolume.Value;
            _raidReviewAudioSource.clip.SetData(ApplyPlaybackGain(_raidReviewRawSamples, _raidReviewAppliedVolumeFactor), 0);
        }

        private const int SpectrogramBins = 24;
        private const int SpectrogramColumns = 96;
        private float[,] _raidReviewSpectrogram;

        /// <summary>
        /// Builds a compact spectrogram: for each time column, the magnitude of a set of frequency
        /// bins. Uses a plain DFT over a small window — it runs once per loaded clip, never per
        /// frame, so the simple implementation is cheap enough and avoids an FFT dependency.
        /// </summary>
        private static float[,] BuildSpectrogram(float[] samples, int channels)
        {
            var map = new float[SpectrogramColumns, SpectrogramBins];
            if (samples.Length == 0 || channels <= 0)
            {
                return map;
            }

            int frameCount = samples.Length / channels;
            const int window = 256;
            float max = 0f;

            for (int col = 0; col < SpectrogramColumns; col++)
            {
                int start = (int)((col / (float)SpectrogramColumns) * frameCount);
                int available = Mathf.Min(window, frameCount - start);
                if (available <= 8)
                {
                    continue;
                }

                for (int bin = 0; bin < SpectrogramBins; bin++)
                {
                    // Bins spread over the low half of the spectrum, where speech energy sits.
                    float freq = (bin + 1) * 0.5f * Mathf.PI / SpectrogramBins;
                    float re = 0f;
                    float im = 0f;

                    for (int n = 0; n < available; n++)
                    {
                        // Mono-mix the frame and apply a Hann window to limit smearing.
                        float mono = 0f;
                        for (int ch = 0; ch < channels; ch++)
                        {
                            mono += samples[(start + n) * channels + ch];
                        }
                        mono /= channels;
                        mono *= 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * n / (available - 1)));

                        float angle = freq * n;
                        re += mono * Mathf.Cos(angle);
                        im += mono * Mathf.Sin(angle);
                    }

                    float mag = Mathf.Sqrt(re * re + im * im) / available;
                    map[col, bin] = mag;
                    if (mag > max)
                    {
                        max = mag;
                    }
                }
            }

            if (max > 0f)
            {
                for (int c = 0; c < SpectrogramColumns; c++)
                {
                    for (int b = 0; b < SpectrogramBins; b++)
                    {
                        // Log-ish scaling so quiet detail stays visible next to loud peaks.
                        map[c, b] = Mathf.Clamp01(Mathf.Sqrt(map[c, b] / max));
                    }
                }
            }

            return map;
        }

        private void DrawSpectrogram(Rect area)
        {
            if (_raidReviewSpectrogram == null)
            {
                return;
            }

            float colW = area.width / SpectrogramColumns;
            float binH = area.height / SpectrogramBins;

            float progress = 0f;
            if (_raidReviewAudioSource != null && _raidReviewAudioSource.clip != null && _raidReviewAudioSource.clip.length > 0f)
            {
                progress = _raidReviewAudioSource.time / _raidReviewAudioSource.clip.length;
            }

            for (int c = 0; c < SpectrogramColumns; c++)
            {
                bool played = (c / (float)SpectrogramColumns) <= progress;
                for (int b = 0; b < SpectrogramBins; b++)
                {
                    float v = _raidReviewSpectrogram[c, b];
                    if (v <= 0.02f)
                    {
                        continue;
                    }

                    // Played region in the bright accent, upcoming region muted.
                    Color col = played ? MilStyle.AccentBright : MilStyle.TextMuted;
                    GUI.color = new Color(col.r, col.g, col.b, v);
                    GUI.DrawTexture(new Rect(area.x + c * colW, area.yMax - (b + 1) * binH, Mathf.Max(1f, colW), Mathf.Max(1f, binH)),
                        Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
        }

        private static float[] BuildWaveformBars(float[] samples, int channels, int barCount)
        {
            float[] bars = new float[barCount];
            if (samples.Length == 0 || channels <= 0)
            {
                return bars;
            }

            int frameCount = samples.Length / channels;
            int samplesPerBar = Mathf.Max(1, frameCount / barCount);

            for (int b = 0; b < barCount; b++)
            {
                int start = b * samplesPerBar;
                int end = Mathf.Min(frameCount, start + samplesPerBar);
                float peak = 0f;
                for (int f = start; f < end; f++)
                {
                    for (int ch = 0; ch < channels; ch++)
                    {
                        float v = Mathf.Abs(samples[f * channels + ch]);
                        if (v > peak)
                        {
                            peak = v;
                        }
                    }
                }
                bars[b] = peak;
            }

            return bars;
        }

        /// <summary>
        /// Click/drag anywhere on the waveform to scrub. Uses hotControl rather than plain
        /// Contains() checks so a fast drag that leaves the rect keeps seeking.
        /// </summary>
        private void HandleWaveformSeek(Rect area)
        {
            // GetControlID must run on every OnGUI pass, before any early-out: a clip finishing
            // between the Layout and Repaint events would otherwise shift control IDs and make
            // IMGUI throw. Bail only after the ID has been claimed.
            int id = GUIUtility.GetControlID(FocusType.Passive);

            if (_raidReviewAudioSource == null || _raidReviewAudioSource.clip == null || _raidReviewWaveform.Length == 0)
            {
                return;
            }

            Event e = Event.current;
            EventType type = e.GetTypeForControl(id);

            bool seeking = false;
            if (type == EventType.MouseDown && area.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = id;
                seeking = true;
                e.Use();
            }
            else if (type == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                seeking = true;
                e.Use();
            }
            else if (type == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }

            if (!seeking)
            {
                return;
            }

            float frac = Mathf.Clamp01((e.mousePosition.x - area.x) / area.width);
            float length = _raidReviewAudioSource.clip.length;
            _raidReviewAudioSource.time = Mathf.Clamp(frac * length, 0f, Mathf.Max(0f, length - 0.05f));
        }

        private void DrawWaveform(Rect area)
        {
            HandleWaveformSeek(area);

            GUI.color = MilStyle.Bg;
            GUI.DrawTexture(area, Texture2D.whiteTexture);

            // Instrument turns the plot into an instrument: division grid behind the trace and a
            // labelled time scale under it, so a position on the waveform reads as a time and not
            // just as a place. Classic keeps the plain plot it always had.
            if (UiStyleState.IsInstrument)
            {
                DrawWaveformGrid(area);
            }

            if (_raidReviewWaveform.Length == 0)
            {
                float idlePulse = 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 1.5f);
                GUI.color = new Color(MilStyle.TextMuted.r, MilStyle.TextMuted.g, MilStyle.TextMuted.b, idlePulse);
                GUI.DrawTexture(new Rect(area.x, area.y + area.height / 2f - 1f, area.width, 2f), Texture2D.whiteTexture);
                GUI.Label(new Rect(area.x, area.y + area.height / 2f - 8f, area.width, 16f),
                    L("НЕТ СИГНАЛА", "NO SIGNAL", "KEIN SIGNAL", "SIN SEÑAL", "AUCUN SIGNAL", "BRAK SYGNAŁU", "NESSUN SEGNALE", "ŽÁDNÝ SIGNÁL"),
                    MilStyle.DimLabel);
                GUI.color = Color.white;
                return;
            }

            // Spectrogram replaces the peak bars, but the playhead below is still drawn.
            if (_spectrogramWaveform.Value && _raidReviewSpectrogram != null)
            {
                DrawSpectrogram(area);
                DrawWaveformPlayhead(area);
                return;
            }

            int barCount = _raidReviewWaveform.Length;
            float barSlot = area.width / barCount;
            float barWidth = Mathf.Max(1f, barSlot * 0.5f);
            float centerY = area.y + area.height / 2f;

            // Keep the playhead visible while paused and while scrubbing a stopped clip,
            // not only during active playback.
            bool hasLoadedClip = _raidReviewAudioSource != null && _raidReviewAudioSource.clip != null && _raidReviewCurrentIndex >= 0;
            float progress = 0f;
            if (_raidReviewAudioSource != null && _raidReviewAudioSource.clip != null && _raidReviewAudioSource.clip.length > 0f)
            {
                progress = _raidReviewAudioSource.time / _raidReviewAudioSource.clip.length;
            }

            for (int i = 0; i < barCount; i++)
            {
                float amp = Mathf.Clamp01(_raidReviewWaveform[i] * 1.4f);
                float barHeight = Mathf.Max(2f, amp * (area.height - 6f));
                float x = area.x + i * barSlot + (barSlot - barWidth) / 2f;
                float y = centerY - barHeight / 2f;

                bool played = (i / (float)barCount) <= progress;
                GUI.color = played ? MilStyle.AccentBright : MilStyle.TextMuted;
                GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);
            }

            if (hasLoadedClip)
            {
                DrawWaveformPlayhead(area);
            }

            GUI.color = Color.white;
        }

        /// <summary>
        /// Elapsed time printed at the playhead, so a glance gives a number and not just a position.
        /// Instrument only; Classic keeps the bare line it always had.
        /// </summary>
        private void DrawPlayheadTimecode(Rect area)
        {
            if (!UiStyleState.IsInstrument
                || _raidReviewAudioSource == null
                || _raidReviewAudioSource.clip == null
                || _raidReviewAudioSource.clip.length <= 0f)
            {
                return;
            }

            float progress = Mathf.Clamp01(_raidReviewAudioSource.time / _raidReviewAudioSource.clip.length);
            float x = area.x + area.width * progress;

            // Flips to the left of the head near the right edge so the readout never leaves the plot.
            const float boxW = 42f;
            bool flip = x + boxW + 4f > area.xMax;
            Rect box = new Rect(flip ? x - boxW - 3f : x + 3f, area.y + 2f, boxW, 13f);

            Color prev = GUI.color;
            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.85f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(box, FormatClipTime(_raidReviewAudioSource.time), MilStyle.ValueLabel);
            GUI.color = prev;
        }

        /// <summary>
        /// Division grid and time scale for the Instrument look. Ticks are chosen from the clip
        /// length so a long recording does not end up with a wall of lines.
        /// </summary>
        private void DrawWaveformGrid(Rect area)
        {
            Color prev = GUI.color;
            float rule = UiTokens.Hairline;

            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.35f);
            for (int i = 1; i < 4; i++)
            {
                float y = area.y + area.height * i / 4f;
                GUI.DrawTexture(new Rect(area.x, y, area.width, rule), Texture2D.whiteTexture);
            }

            float total = _raidReviewAudioSource != null && _raidReviewAudioSource.clip != null
                ? _raidReviewAudioSource.clip.length
                : 0f;

            // One tick per second up to 15 s, then per five, then per ten. Keeps the scale legible
            // whether the clip is a two-second call or a two-minute one.
            float step = total <= 15f ? 1f : (total <= 60f ? 5f : 10f);

            if (total <= 0.01f)
            {
                GUI.color = prev;
                return;
            }

            for (float t = step; t < total; t += step)
            {
                float x = area.x + area.width * (t / total);
                GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.5f);
                GUI.DrawTexture(new Rect(x, area.y, rule, area.height), Texture2D.whiteTexture);

                GUI.color = Color.white;

                // Was 44x12 hard against the bottom edge, which clipped the glyphs. The label now
                // has real room and sits inside the plot, and the last tick is skipped when it
                // would run past the right edge instead of being drawn cut in half.
                float labelW = 46f;
                if (x + labelW + 3f <= area.xMax)
                {
                    GUI.Label(new Rect(x + 3f, area.yMax - 17f, labelW, 15f), FormatClipTime(t), WaveTickStyle);
                }
            }

            GUI.color = prev;
        }

        private void DrawWaveformPlayhead(Rect area)
        {
            DrawPlayheadTimecode(area);

            if (_raidReviewAudioSource == null || _raidReviewAudioSource.clip == null
                || _raidReviewAudioSource.clip.length <= 0f || _raidReviewCurrentIndex < 0)
            {
                GUI.color = Color.white;
                return;
            }

            float progress = _raidReviewAudioSource.time / _raidReviewAudioSource.clip.length;
            float headX = area.x + progress * area.width;

            for (int glow = 3; glow >= 1; glow--)
            {
                GUI.color = new Color(MilStyle.SignalBright.r, MilStyle.SignalBright.g, MilStyle.SignalBright.b, 0.12f / glow);
                GUI.DrawTexture(new Rect(headX - glow * 2f, area.y, glow * 4f, area.height), Texture2D.whiteTexture);
            }

            GUI.color = MilStyle.SignalBright;
            GUI.DrawTexture(new Rect(headX - 1f, area.y, 2f, area.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// Faction-driven visual theme for the Raid Recordings window.
        /// BEAR renders as a Russian Armed Forces field terminal (olive/khaki chassis, red-and-gold
        /// warning livery, dense stencil typography, aggressive CRT motion).
        /// USEC renders as a UNTAR peacekeeping observation console (navy/UN-blue, white rules,
        /// airy institutional layout, restrained motion).
        /// </summary>
        private static class MilStyle
        {
            public static bool IsBear = true;

            public static Color Bg;
            public static Color Panel;
            public static Color Border;
            public static Color BtnFill;
            public static Color Accent;
            public static Color AccentBright;
            public static Color Signal;
            public static Color SignalBright;
            public static Color TextPrimary;
            public static Color TextMuted;

            public static GUIStyle Window;
            public static GUIStyle SectionLabel;
            public static GUIStyle Button;
            public static GUIStyle ButtonOff;
            public static GUIStyle BodyLabel;
            public static GUIStyle DimLabel;
            public static GUIStyle PlayingLabel;
            public static GUIStyle NumberField;
            public static GUIStyle TagLabel;
            public static GUIStyle WrapLabel;
            public static GUIStyle ScrollView;
            public static GUIStyle CallsignLabel;
            public static GUIStyle ClockLabel;
            public static GUIStyle GlyphButton;
            public static GUIStyle Field;
            public static GUIStyle ValueLabel;
            public static GUIStyle UnitLabel;

            private static Texture2D _bgTex;
            private static Texture2D _btnTex;
            private static Texture2D _panelTex;
            private static Texture2D _hoverTex;
            private static Texture2D _borderTex;
            private static Texture2D _backdropTex;

            private static bool _built;
            private static UiTheme _builtTheme = (UiTheme)(-1);

            /// <summary>Darkest colour of the palette, for text drawn on a filled button.</summary>
            public static Color Ink;

            /// <summary>
            /// Styles are cached, so the cache key has to include the UI style as well as the
            /// faction — otherwise switching to Instrument would keep Classic's fonts until the
            /// player also happened to change faction.
            /// </summary>
            private static bool _builtAsInstrument;

            /// <summary>Colour set for a faction, resolved without touching the applied theme.</summary>
            public struct Palette
            {
                public Color Bg, Panel, Border, TextPrimary, Accent, SignalBright;
                public bool IsBear;
            }

            /// <summary>
            /// Notifications can run a different palette to the window, so they resolve one by
            /// name rather than reading the applied theme. Kept taking a bool because that is what
            /// the notification setting still offers: follow the window, or force one of two sides.
            /// </summary>
            public static Palette GetPalette(bool bear)
            {
                return bear
                    ? new Palette
                    {
                        Bg = new Color(0.043f, 0.055f, 0.027f),
                        Panel = new Color(0.106f, 0.129f, 0.075f),
                        Border = new Color(0.227f, 0.271f, 0.149f),
                        TextPrimary = new Color(0.839f, 0.863f, 0.769f),
                        Accent = new Color(0.561f, 0.749f, 0.353f),
                        SignalBright = new Color(0.910f, 0.765f, 0.286f),
                        IsBear = true,
                    }
                    : new Palette
                    {
                        Bg = new Color(0.063f, 0.094f, 0.133f),
                        Panel = new Color(0.106f, 0.157f, 0.212f),
                        Border = new Color(0.180f, 0.255f, 0.333f),
                        TextPrimary = new Color(0.894f, 0.929f, 0.961f),
                        Accent = new Color(0.498f, 0.714f, 0.910f),
                        SignalBright = Color.white,
                        IsBear = false,
                    };
            }

            
            
            private static Texture2D SolidTex(Color c)
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, c);
                tex.Apply();
                return tex;
            }

            /// <summary>BEAR: sparse tactical dot-grid. USEC: fine horizontal survey rules.</summary>
            private static Texture2D BackdropTex()
            {
                const int size = 8;
                var tex = new Texture2D(size, size) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
                Color mark = new Color(Accent.r, Accent.g, Accent.b, IsBear ? 0.40f : 0.16f);
                Color clear = new Color(0f, 0f, 0f, 0f);

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool on = IsBear ? (x == 0 && y == 0) : (y == 0);
                        tex.SetPixel(x, y, on ? mark : clear);
                    }
                }
                tex.Apply();
                return tex;
            }

            private static void DestroyTex(Texture2D tex)
            {
                if (tex != null)
                {
                    UnityEngine.Object.Destroy(tex);
                }
            }

            public static void ApplyTheme(UiTheme theme)
            {
                bool instrument = UiStyleState.IsInstrument;

                if (_built && _builtTheme == theme && _builtAsInstrument == instrument)
                {
                    return;
                }

                if (_built)
                {
                    DestroyTex(_bgTex);
                    DestroyTex(_btnTex);
                    DestroyTex(_panelTex);
                    DestroyTex(_hoverTex);
                    DestroyTex(_borderTex);
                    DestroyTex(_backdropTex);
                }

                ThemePalette p = GetThemePalette(theme);

                // IsBear now means "this palette uses stencil chrome", which is what every call site
                // was really asking. Keeping the name avoids churning twenty unrelated lines; the
                // meaning is documented on the field.
                IsBear = p.Stencil;
                _built = true;
                _builtTheme = theme;
                _builtAsInstrument = instrument;

                Bg = p.Bg; Panel = p.Panel; Border = p.Border; BtnFill = p.BtnFill;
                Accent = p.Accent; AccentBright = p.AccentBright;
                Signal = p.Signal; SignalBright = p.SignalBright;
                TextPrimary = p.TextPrimary; TextMuted = p.TextMuted;
                Ink = p.Ink;

                // Generated textures are built from the palette, so they have to go with it —
                // otherwise a faction switch would keep the previous side colours on every panel.
                ClearUiTextureCache();

                _bgTex = SolidTex(Bg);
                _btnTex = SolidTex(BtnFill);
                _panelTex = SolidTex(Panel);
                _hoverTex = SolidTex(IsBear ? SignalBright : AccentBright);
                _borderTex = SolidTex(Border);
                _backdropTex = BackdropTex();

                Window = new GUIStyle(GUI.skin.window)
                {
                    normal = { background = _bgTex, textColor = AccentBright },
                    onNormal = { background = _bgTex, textColor = AccentBright },
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(10, 10, 6, 10),
                };

                SectionLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = IsBear ? SignalBright : Accent },
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    padding = new RectOffset(0, 0, 2, 2),
                };

                // border is zeroed because the backgrounds are 1x1 solids — the stock skin's
                // 9-slice border would stretch their corners and reintroduce the default look.
                Button = new GUIStyle(GUI.skin.button)
                {
                    normal = { background = _btnTex, textColor = IsBear ? Color.black : Color.white },
                    hover = { background = _hoverTex, textColor = Color.black },
                    active = { background = _hoverTex, textColor = Color.black },
                    focused = { background = _btnTex, textColor = IsBear ? Color.black : Color.white },
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    border = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(2, 2, 2, 2),
                    padding = new RectOffset(4, 4, 2, 2),
                };

                BodyLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = TextPrimary } };
                DimLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = TextMuted }, fontSize = 11 };
                PlayingLabel = new GUIStyle(GUI.skin.label) { normal = { textColor = AccentBright }, fontStyle = FontStyle.Bold };

                NumberField = new GUIStyle(GUI.skin.textField)
                {
                    normal = { background = _panelTex, textColor = IsBear ? SignalBright : AccentBright },
                    focused = { background = _borderTex, textColor = IsBear ? SignalBright : AccentBright },
                    hover = { background = _panelTex, textColor = IsBear ? SignalBright : AccentBright },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(2, 2, 2, 2),
                };

                // Left-aligned input for free text, distinct from the centred numeric readout.
                Field = new GUIStyle(NumberField)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Normal,
                    normal = { background = _panelTex, textColor = TextPrimary },
                    focused = { background = _borderTex, textColor = TextPrimary },
                    hover = { background = _panelTex, textColor = TextPrimary },
                    padding = new RectOffset(6, 6, 2, 2),
                };

                // Inset instrument readout used for the selected day.
                ValueLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { background = _panelTex, textColor = IsBear ? SignalBright : AccentBright },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 11,
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(4, 4, 2, 2),
                };

                UnitLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = TextMuted },
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    padding = new RectOffset(2, 0, 0, 0),
                };

                ButtonOff = new GUIStyle(Button)
                {
                    normal = { background = _panelTex, textColor = TextMuted },
                    hover = { background = _borderTex, textColor = TextPrimary },
                    active = { background = _borderTex, textColor = TextPrimary },
                    focused = { background = _panelTex, textColor = TextMuted },
                };

                TagLabel = new GUIStyle(SectionLabel)
                {
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.MiddleCenter,
                };

                CallsignLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = IsBear ? TextMuted : Accent },
                    fontSize = 11,
                    alignment = TextAnchor.MiddleRight,
                };

                ClockLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = IsBear ? SignalBright : AccentBright },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleRight,
                    padding = new RectOffset(0, 5, 0, 0),
                };

                // Geometric glyphs sit low and off-centre inside the default button padding,
                // so the transport buttons get a padding-free, centred variant.
                GlyphButton = new GUIStyle(Button)
                {
                    padding = new RectOffset(0, 0, 0, 0),
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    contentOffset = new Vector2(0f, -1f),
                };

                WrapLabel = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = TextMuted },
                    fontSize = 11,
                    wordWrap = true,
                    padding = new RectOffset(6, 6, 0, 0),
                };

                // Unity's stock scrollbar is light grey and reads as a foreign element in both
                // themes, so the track and thumb are restyled to match the palette.
                GUIStyle scrollbar = new GUIStyle(GUI.skin.verticalScrollbar) { fixedWidth = 9f };
                scrollbar.normal.background = _panelTex;

                GUIStyle thumb = new GUIStyle(GUI.skin.verticalScrollbarThumb)
                {
                    fixedWidth = 9f,
                    border = new RectOffset(0, 0, 0, 0),
                };
                thumb.normal.background = _btnTex;
                thumb.hover.background = _hoverTex;
                thumb.active.background = _hoverTex;

                ScrollView = new GUIStyle(GUI.skin.scrollView)
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                };
                ScrollView.normal.background = null;

                _scrollbarStyle = scrollbar;
                _scrollbarThumbStyle = thumb;

                if (instrument)
                {
                    ApplyTokenFont();
                }
            }

            /// <summary>
            /// Instrument only. Classic is never handed the token font — that is the rule that keeps
            /// the frozen look impossible to break by editing a token.
            /// </summary>
            private static void ApplyTokenFont()
            {
                GUIStyle[] all =
                {
                    Window, SectionLabel, Button, ButtonOff, BodyLabel, DimLabel, PlayingLabel,
                    NumberField, TagLabel, WrapLabel, ScrollView, CallsignLabel, ClockLabel,
                    GlyphButton, Field, ValueLabel, UnitLabel,
                };

                foreach (GUIStyle style in all)
                {
                    if (style == null)
                    {
                        continue;
                    }

                    // Styles that ask for bold get the real bold cut when the game ships one;
                    // synthesised bold is muddy at 9–11px, which is most of this interface.
                    style.font = style.fontStyle == FontStyle.Bold && UiTokens.FontBold != null
                        ? UiTokens.FontBold
                        : UiTokens.Font;
                }
            }

            private static GUIStyle _scrollbarStyle;
            private static GUIStyle _scrollbarThumbStyle;
            private static GUIStyle _savedScrollbar;
            private static GUIStyle _savedScrollbarThumb;

            /// <summary>
            /// GUI.skin is process-wide, so the themed scrollbar is swapped in only around our own
            /// scroll view and restored immediately after — otherwise it would leak into the game's
            /// and other mods' IMGUI.
            /// </summary>
            public static void PushScrollbarSkin()
            {
                _savedScrollbar = GUI.skin.verticalScrollbar;
                _savedScrollbarThumb = GUI.skin.verticalScrollbarThumb;
                GUI.skin.verticalScrollbar = _scrollbarStyle;
                GUI.skin.verticalScrollbarThumb = _scrollbarThumbStyle;
            }

            public static void PopScrollbarSkin()
            {
                if (_savedScrollbar != null)
                {
                    GUI.skin.verticalScrollbar = _savedScrollbar;
                    GUI.skin.verticalScrollbarThumb = _savedScrollbarThumb;
                }
            }

            /// <summary>
            /// Header underline. BEAR uses a gold command rule, USEC a solid UN-blue bar.
            /// </summary>
            public static void DrawHeaderAccent(Rect rect)
            {
                Color prev = GUI.color;

                if (IsBear)
                {
                    // No underline at all: it sat on top of the clock readout.
                    return;
                }

                GUI.color = BtnFill;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = new Color(1f, 1f, 1f, 0.75f);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
                GUI.color = prev;
            }

            public static void DrawBackdrop(Rect rect, float alpha)
            {
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTextureWithTexCoords(rect, _backdropTex, new Rect(0f, 0f, rect.width / 8f, rect.height / 8f));
                GUI.color = prev;
            }

            /// <summary>BEAR: pronounced CRT sweep. USEC: a slow, faint scanning band.</summary>
            public static void DrawAmbientSweep(Rect rect)
            {
                float period = IsBear ? 5f : 9f;
                float peakAlpha = IsBear ? 0.05f : 0.022f;
                Color tint = IsBear ? SignalBright : Accent;

                float t = Mathf.Repeat(Time.unscaledTime, period) / period;
                float sweepY = t * rect.height;
                const float bandHalf = 30f;
                const int steps = 6;

                Color prev = GUI.color;
                GUI.BeginGroup(rect);
                for (int i = 0; i < steps; i++)
                {
                    float frac = 1f - (float)i / steps;
                    float h = bandHalf / steps;
                    GUI.color = new Color(tint.r, tint.g, tint.b, peakAlpha * frac);
                    GUI.DrawTexture(new Rect(0f, sweepY - bandHalf / 2f + i * h, rect.width, h), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(0f, sweepY + bandHalf / 2f - (i + 1) * h, rect.width, h), Texture2D.whiteTexture);
                }
                GUI.EndGroup();
                GUI.color = prev;
            }

            /// <summary>BEAR: heavy gold corner brackets. USEC: a clean thin full frame.</summary>
            public static void DrawFrame(Rect rect, float gripInset)
            {
                Color prev = GUI.color;

                if (!IsBear)
                {
                    GUI.color = new Color(Accent.r, Accent.g, Accent.b, 0.5f);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
                    GUI.color = prev;
                    return;
                }

                const float len = 14f;
                const float thick = 2f;
                float bottomLen = len - gripInset;

                GUI.color = SignalBright;
                GUI.DrawTexture(new Rect(rect.x, rect.y, len, thick), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.y, thick, len), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.xMax - len, rect.y, len, thick), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.xMax - thick, rect.y, thick, len), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - thick, len, thick), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x, rect.yMax - len, thick, len), Texture2D.whiteTexture);

                if (bottomLen > 0f)
                {
                    GUI.DrawTexture(new Rect(rect.xMax - len, rect.yMax - thick, bottomLen, thick), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.xMax - thick, rect.yMax - len, thick, bottomLen), Texture2D.whiteTexture);
                }

                GUI.color = prev;
            }

            /// <summary>BEAR: boxed panel. USEC: flat card with a single blue spine on the left.</summary>
            public static void DrawPanelBackground(Rect rect, float alpha = 1f)
            {
                Color prev = GUI.color;

                GUI.color = new Color(Panel.r, Panel.g, Panel.b, Panel.a * alpha);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);

                if (IsBear)
                {
                    GUI.color = new Color(Border.r, Border.g, Border.b, Border.a * alpha);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), Texture2D.whiteTexture);
                }
                else
                {
                    GUI.color = new Color(BtnFill.r, BtnFill.g, BtnFill.b, alpha);
                    GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);
                    GUI.color = new Color(Border.r, Border.g, Border.b, Border.a * alpha * 0.7f);
                    GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Texture2D.whiteTexture);
                }

                GUI.color = prev;
            }
        }

        private void DrawRaidReviewBrowser()
        {
            if (!_showRaidReviewBrowser || _cachedIsInRaid)
            {
                return;
            }

            MilStyle.ApplyTheme(ResolveTheme());

            if (_raidReviewWindowRect.x < 0f)
            {
                _raidReviewWindowRect = new Rect((Screen.width - _raidReviewWindowRect.width) / 2f,
                    (Screen.height - _raidReviewWindowRect.height) / 2f, _raidReviewWindowRect.width, _raidReviewWindowRect.height);
            }

            if (_raidReviewOpenTime <= 0f)
            {
                _raidReviewOpenTime = Time.unscaledTime;
            }

            const float flickerDuration = 0.12f;
            const float fadeInDuration = 0.18f;
            float t = Time.unscaledTime - _raidReviewOpenTime;

            float fade;
            if (t < flickerDuration)
            {
                float flicker = Mathf.PingPong(t * 45f, 1f);
                fade = Mathf.Lerp(0.15f, 0.85f, flicker);
            }
            else
            {
                fade = Mathf.Clamp01((t - flickerDuration) / fadeInDuration);
            }

            float scale = Mathf.Lerp(0.96f, 1f, Mathf.Clamp01((t - flickerDuration) / fadeInDuration));

            Color prevGuiColor = GUI.color;

            // Take over input for as long as the window is up. Releasing it is handled from Update,
            // not from here, so a throw further down cannot strand the player without a cursor.
            WindowModality.Open();

            if (WindowModality.DrawBlocker(_raidReviewWindowRect, 0.55f * fade))
            {
                CloseRaidReviewBrowser();
                GUI.color = prevGuiColor;
                return;
            }

            Matrix4x4 prevMatrix = GUI.matrix;
            if (scale < 1f)
            {
                Vector2 pivot = new Vector2(_raidReviewWindowRect.x + _raidReviewWindowRect.width / 2f,
                    _raidReviewWindowRect.y + _raidReviewWindowRect.height / 2f);
                GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            }

            GUI.color = new Color(1f, 1f, 1f, fade);
            _raidReviewWindowRect = GUILayout.Window(984612, _raidReviewWindowRect, DrawRaidReviewWindowContents, "", MilStyle.Window);
            GUI.matrix = prevMatrix;
            GUI.color = prevGuiColor;
        }

        /// <summary>Single close path, so every caller leaves the same state behind.</summary>
        private void CloseRaidReviewBrowser()
        {
            _showRaidReviewBrowser = false;
            _raidReviewOpenTime = 0f;
            StopRaidReviewPlayback();
            WindowModality.Close();
        }

        private const float PlaybackVolumeMaxPercent = 500f;
        private string _raidReviewVolumeInputText;

        /// <summary>
        /// Transport row: track/seek controls, elapsed-of-total readout and the auto-advance toggle.
        /// Glyphs are taken from the Geometric Shapes block so they render in Unity's default font.
        /// </summary>
        /// <summary>Thin labelled rule marking where one raid's traffic ends and the next begins.</summary>
        private void DrawRaidSeparator(string location)
        {
            GUILayout.Space(6f);
            Rect r = GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true));

            Color prev = GUI.color;
            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.9f);
            GUI.DrawTexture(new Rect(r.x, r.y + r.height / 2f, r.width, 1f), Texture2D.whiteTexture);

            string label = "  " + (string.IsNullOrEmpty(location) ? "—" : location.ToUpperInvariant()) + "  ";
            Vector2 size = MilStyle.DimLabel.CalcSize(new GUIContent(label));
            Rect labelRect = new Rect(r.x + 8f, r.y, Mathf.Min(size.x, r.width - 16f), r.height);

            GUI.color = MilStyle.Bg;
            GUI.DrawTexture(labelRect, Texture2D.whiteTexture);
            GUI.color = prev;

            GUI.Label(labelRect, label, MilStyle.DimLabel);
            GUILayout.Space(2f);
        }

        private void DrawEmptyRecordingsState()
        {
            GUILayout.Space(18f);

            GUILayout.Label(L("АРХИВ ПУСТ", "ARCHIVE EMPTY", "ARCHIV LEER", "ARCHIVO VACÍO",
                "ARCHIVE VIDE", "ARCHIWUM PUSTE", "ARCHIVIO VUOTO", "ARCHIV PRÁZDNÝ"), MilStyle.SectionLabel);

            GUILayout.Space(4f);

            string hint = (_raidReviewFilter ?? "").Trim().Length > 0
                ? L("Ничего не найдено по запросу. Очистите строку поиска.",
                    "Nothing matches your search. Clear the search box.",
                    "Keine Treffer. Suchfeld leeren.",
                    "Sin coincidencias. Borra la búsqueda.",
                    "Aucun résultat. Effacez la recherche.",
                    "Brak wyników. Wyczyść wyszukiwanie.",
                    "Nessun risultato. Svuota la ricerca.",
                    "Žádné výsledky. Vymažte hledání.")
                : L("Переговоры по рации записываются автоматически во время рейда. Проведите рейд с включённой рацией — записи появятся здесь.",
                    "Radio traffic is recorded automatically during a raid. Run a raid with your radio on and the recordings will show up here.",
                    "Funkverkehr wird während des Raids automatisch aufgezeichnet. Mit eingeschaltetem Funkgerät in den Raid — die Aufnahmen erscheinen hier.",
                    "Las comunicaciones se graban automáticamente durante la incursión. Juega con la radio encendida y aparecerán aquí.",
                    "Les communications sont enregistrées automatiquement pendant le raid. Jouez radio allumée et elles apparaîtront ici.",
                    "Łączność jest nagrywana automatycznie podczas rajdu. Zagraj z włączonym radiem, a nagrania pojawią się tutaj.",
                    "Le comunicazioni vengono registrate automaticamente durante il raid. Gioca con la radio accesa e appariranno qui.",
                    "Rádiový provoz se během raidu nahrává automaticky. Jděte do raidu se zapnutou vysílačkou a nahrávky se objeví zde.");

            GUILayout.Label(hint, MilStyle.WrapLabel);
        }

        private void OpenCurrentDayFolder()
        {
            try
            {
                string folder = GetCurrentDayFolder() ?? GetRaidReviewRootFolder();
                if (Directory.Exists(folder))
                {
                    System.Diagnostics.Process.Start(folder);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: failed to open recordings folder: " + ex.Message);
            }
        }

        private void DrawTransportBar()
        {
            bool hasClip = _raidReviewAudioSource != null && _raidReviewAudioSource.clip != null && _raidReviewCurrentIndex >= 0;
            bool hasList = _raidReviewClips.Length > 0;

            GUILayout.BeginHorizontal();

            GUI.enabled = hasList && _raidReviewCurrentIndex > 0;
            if (TransportButton(TransportGlyph.ToStart))
            {
                PlayRaidReviewIndex(_raidReviewCurrentIndex - 1);
            }

            GUI.enabled = hasClip;
            if (TransportButton(TransportGlyph.Back))
            {
                SeekRaidReviewBy(-5f);
            }

            GUI.enabled = hasClip || hasList;
            if (TransportButton(_raidReviewPaused || !hasClip ? TransportGlyph.Play : TransportGlyph.Pause))
            {
                if (hasClip)
                {
                    ToggleRaidReviewPause();
                }
                else if (hasList)
                {
                    PlayRaidReviewIndex(0);
                }
            }

            GUI.enabled = hasClip;
            if (TransportButton(TransportGlyph.Stop))
            {
                StopRaidReviewPlayback();
            }

            if (TransportButton(TransportGlyph.Forward))
            {
                SeekRaidReviewBy(5f);
            }

            GUI.enabled = hasList && _raidReviewCurrentIndex >= 0 && _raidReviewCurrentIndex < _raidReviewClips.Length - 1;
            if (TransportButton(TransportGlyph.ToEnd))
            {
                PlayRaidReviewIndex(_raidReviewCurrentIndex + 1);
            }

            GUI.enabled = true;

            GUILayout.Space(6f);

            string elapsed = hasClip ? FormatClipTime(_raidReviewAudioSource.time) : "0:00";
            string total = hasClip ? FormatClipTime(_raidReviewAudioSource.clip.length) : "0:00";
            GUILayout.Label(elapsed + " / " + total, MilStyle.ValueLabel, GUILayout.Width(86f), GUILayout.Height(22f));

            GUILayout.FlexibleSpace();

            string autoLabel = L("АВТО", "AUTO", "AUTO", "AUTO", "AUTO", "AUTO", "AUTO", "AUTO");
            GUIStyle autoStyle = _raidReviewAutoAdvance.Value ? MilStyle.Button : MilStyle.ButtonOff;
            if (GUILayout.Button(autoLabel, autoStyle, GUILayout.Width(52f), GUILayout.Height(22f)))
            {
                _raidReviewAutoAdvance.Value = !_raidReviewAutoAdvance.Value;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawPlaybackVolumeSlider()
        {
            float pct = _raidReviewPlaybackVolume.Value * 100f;

            if (_raidReviewVolumeInputText == null)
            {
                _raidReviewVolumeInputText = Mathf.RoundToInt(pct).ToString();
            }

            GUILayout.BeginHorizontal();

            Rect sliderRect = GUILayoutUtility.GetRect(200f, 22f, GUILayout.Width(200f));
            float newPct = DrawMilVolumeDial(sliderRect, pct, PlaybackVolumeMaxPercent);
            if (!Mathf.Approximately(newPct, pct))
            {
                pct = newPct;
                _raidReviewPlaybackVolume.Value = pct / 100f;
                _raidReviewVolumeInputText = Mathf.RoundToInt(pct).ToString();
            }

            GUILayout.Space(6f);

            GUI.SetNextControlName("PlaybackVolumeInput");
            string typed = GUILayout.TextField(_raidReviewVolumeInputText, 4, MilStyle.NumberField, GUILayout.Width(44f), GUILayout.Height(20f));
            if (typed != _raidReviewVolumeInputText)
            {
                _raidReviewVolumeInputText = typed;
                if (float.TryParse(typed, out float parsedPct))
                {
                    _raidReviewPlaybackVolume.Value = Mathf.Clamp(parsedPct, 0f, PlaybackVolumeMaxPercent) / 100f;
                }
            }
            GUILayout.Label("%", MilStyle.UnitLabel, GUILayout.Width(14f));

            GUILayout.EndHorizontal();

            UpdatePlaybackVolumeIfChanged();
        }

        private static float DrawMilVolumeDial(Rect rect, float valuePct, float maxPct)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            EventType typeForControl = e.GetTypeForControl(id);

            if (typeForControl == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                GUIUtility.hotControl = id;
                valuePct = Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * maxPct;
                e.Use();
            }
            else if (typeForControl == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                valuePct = Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width) * maxPct;
                e.Use();
            }
            else if (typeForControl == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                DrawSegmentedMeter(rect, valuePct, maxPct);
            }

            return valuePct;
        }

        /// <summary>
        /// Segmented LED-style level meter. Cells past the 100% mark light in the warning colour
        /// so boosting above unity gain is visually obvious.
        /// </summary>
        private static void DrawSegmentedMeter(Rect rect, float valuePct, float maxPct)
        {
            const int cells = 25;
            const float gap = 2f;

            Color prev = GUI.color;

            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            float cellWidth = (rect.width - gap * (cells - 1)) / cells;
            float litCells = Mathf.Clamp01(valuePct / maxPct) * cells;

            for (int i = 0; i < cells; i++)
            {
                float cellX = rect.x + i * (cellWidth + gap);
                float cellPct = (i + 1) / (float)cells * maxPct;
                float fill = Mathf.Clamp01(litCells - i);

                Color lit = cellPct > 100f ? MilStyle.SignalBright : MilStyle.Accent;
                GUI.color = fill > 0f
                    ? new Color(lit.r, lit.g, lit.b, 0.35f + 0.65f * fill)
                    : new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.45f);

                GUI.DrawTexture(new Rect(cellX, rect.y, cellWidth, rect.height), Texture2D.whiteTexture);
            }

            // Unity-gain reference mark.
            float unityX = rect.x + rect.width * (100f / maxPct);
            GUI.color = new Color(MilStyle.TextPrimary.r, MilStyle.TextPrimary.g, MilStyle.TextPrimary.b, 0.75f);
            GUI.DrawTexture(new Rect(unityX - 1f, rect.y - 3f, 1f, rect.height + 6f), Texture2D.whiteTexture);

            GUI.color = prev;
        }

        /// <summary>
        /// Chooses the window theme: the explicit F12 override when set, otherwise the local
        /// character's faction. USEC gets the UNTAR console; BEAR (and anything unknown,
        /// including SCAV runs) gets the Russian Armed Forces terminal.
        /// </summary>

        private string _cachedCallsignLabelKey;
        private string _cachedCallsignLabel;

        private string GetLocalCallsignAndFaction()
        {
            try
            {
                if (!TryGetLocalIdentity(out string nickname, out EPlayerSide? side))
                {
                    return "??? // ???";
                }

                string key = nickname + "|" + side;
                if (key == _cachedCallsignLabelKey)
                {
                    return _cachedCallsignLabel;
                }

                string faction;
                switch (side)
                {
                    case EPlayerSide.Usec: faction = "USEC"; break;
                    case EPlayerSide.Bear: faction = "BEAR"; break;
                    default: faction = "SCAV"; break;
                }

                _cachedCallsignLabelKey = key;
                _cachedCallsignLabel = nickname.ToUpperInvariant() + " // " + faction;
                return _cachedCallsignLabel;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PRT: exception in GetLocalCallsignAndFaction: " + ex);
                return "??? // ???";
            }
        }

        private static string LetterSpace(string s)
        {
            return string.Join(" ", s.ToCharArray());
        }

        private string _bearTitleCacheKey;
        private string _bearTitleCached;

        private string GetBearTitle(string archiveWord)
        {
            if (archiveWord != _bearTitleCacheKey)
            {
                _bearTitleCacheKey = archiveWord;
                _bearTitleCached = "★ " + LetterSpace("S&M-PRT " + archiveWord);
            }

            return _bearTitleCached;
        }

        /// <summary>
        /// Space toggles pause, arrows scrub. Handled inside OnGUI so the volume input keeps
        /// receiving its own keystrokes instead of having them swallowed as shortcuts.
        /// </summary>
        private void HandleRaidReviewHotkeys()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown)
            {
                return;
            }

            // Never steal keystrokes from a focused text field — space and arrows belong to it.
            string focused = GUI.GetNameOfFocusedControl();
            if (focused == "PlaybackVolumeInput" || focused == "RaidReviewFilter")
            {
                return;
            }

            switch (e.keyCode)
            {
                case KeyCode.Space:
                    ToggleRaidReviewPause();
                    e.Use();
                    break;
                case KeyCode.LeftArrow:
                    SeekRaidReviewBy(-5f);
                    e.Use();
                    break;
                case KeyCode.RightArrow:
                    SeekRaidReviewBy(5f);
                    e.Use();
                    break;
            }
        }

        /// <summary>Clock readout drawn as an inset instrument panel rather than a bare label.</summary>
        private static void DrawClockReadout(Rect rect)
        {
            Color prev = GUI.color;

            GUI.color = new Color(MilStyle.Bg.r, MilStyle.Bg.g, MilStyle.Bg.b, 0.85f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = new Color(MilStyle.Border.r, MilStyle.Border.g, MilStyle.Border.b, 0.9f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 2f, rect.height), Texture2D.whiteTexture);

            GUI.color = prev;
            GUI.Label(rect, DateTime.Now.ToString("dd.MM  HH:mm:ss"), MilStyle.ClockLabel);
        }

        /// <summary>
        /// Fills the screen (leaving a small margin) and restores the previous geometry on the way
        /// back, so maximising never loses the size the player had set up.
        /// </summary>
        private void ToggleRaidReviewMaximized()
        {
            if (_raidReviewMaximized)
            {
                _raidReviewWindowRect = _raidReviewRestoreRect;
                _raidReviewMaximized = false;
                return;
            }

            _raidReviewRestoreRect = _raidReviewWindowRect;
            const float margin = 30f;
            _raidReviewWindowRect = new Rect(margin, margin, Screen.width - margin * 2f, Screen.height - margin * 2f);
            _raidReviewMaximized = true;
        }

        private void DrawRaidReviewWindowContents(int windowId)
        {
            HandleRaidReviewHotkeys();

            Rect fullRect = new Rect(0f, 0f, _raidReviewWindowRect.width, _raidReviewWindowRect.height);

            MilStyle.DrawBackdrop(fullRect, MilStyle.IsBear ? 0.5f : 0.35f);
            MilStyle.DrawAmbientSweep(fullRect);
            MilStyle.DrawFrame(fullRect, RaidReviewGripSize);

            float headerHeight = MilStyle.IsBear ? 40f : 46f;
            Rect headerRect = new Rect(0f, 0f, _raidReviewWindowRect.width, headerHeight);

            Color prevColor = GUI.color;
            GUI.color = MilStyle.Panel;
            GUI.DrawTexture(headerRect, Texture2D.whiteTexture);
            GUI.color = prevColor;
            MilStyle.DrawHeaderAccent(new Rect(headerRect.x, headerRect.yMax - 3f, headerRect.width, 3f));

            string archiveWord = L("Записи с рейда", "Raid Recordings", "Aufnahmen aus dem Raid", "Grabaciones de la incursión",
                "Enregistrements de raid", "Nagrania z rajdu", "Registrazioni del raid", "Nahrávky z raidu").ToUpperInvariant();
            string onlineWord = L("АРХИВ ОНЛАЙН", "ARCHIVE ONLINE", "ARCHIV ONLINE", "ARCHIVO EN LÍNEA",
                "ARCHIVE EN LIGNE", "ARCHIWUM ONLINE", "ARCHIVIO ONLINE", "ARCHIV ONLINE");

            if (UiStyleState.IsInstrument)
            {
                // One plate for both factions: spaced title, quiet subtitle, clock hard right.
                // The mock-up reads as a single instrument rather than as two different documents
                // depending on which side the player happens to be on.
                DrawInstrumentHeader(headerRect);
            }
            else if (MilStyle.IsBear)
            {
                // Dense stencil plate: red star, hard letterspacing, blinking readiness lamp.
                GUI.Label(new Rect(10f, 4f, headerRect.width - 20f, 20f),
                    GetBearTitle(archiveWord), MilStyle.SectionLabel);

                float statusPulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                Color prevDot = GUI.color;
                GUI.color = new Color(MilStyle.AccentBright.r, MilStyle.AccentBright.g, MilStyle.AccentBright.b, 0.55f + 0.45f * statusPulse);
                GUI.DrawTexture(new Rect(10f, 24f, 6f, 6f), Texture2D.whiteTexture);
                GUI.color = prevDot;
                GUI.Label(new Rect(20f, 21f, 160f, 16f), onlineWord, MilStyle.DimLabel);

                GUI.Label(new Rect(headerRect.width - 178f, 5f, 146f, 16f), GetLocalCallsignAndFaction(), MilStyle.CallsignLabel);
                DrawClockReadout(new Rect(headerRect.width - 178f, 21f, 146f, 15f));
            }
            else
            {
                // UNTAR document header: solid blue ident block, generous spacing, steady lamp.
                Color prevTag = GUI.color;
                GUI.color = MilStyle.BtnFill;
                GUI.DrawTexture(new Rect(10f, 9f, 54f, 20f), Texture2D.whiteTexture);
                GUI.color = prevTag;

                GUI.Label(new Rect(10f, 9f, 54f, 20f), "UNTAR", MilStyle.TagLabel);

                GUI.Label(new Rect(72f, 7f, headerRect.width - 230f, 18f), "S&M-PRT  ·  " + archiveWord, MilStyle.SectionLabel);
                GUI.Label(new Rect(72f, 25f, headerRect.width - 230f, 16f), onlineWord, MilStyle.DimLabel);

                GUI.Label(new Rect(headerRect.width - 178f, 7f, 146f, 16f), GetLocalCallsignAndFaction(), MilStyle.CallsignLabel);
                DrawClockReadout(new Rect(headerRect.width - 178f, 25f, 146f, 15f));
            }

            // Maximise toggle sits above DragWindow so the click reaches the button, not the drag.
            // Close sits left of maximise, both above DragWindow so the click reaches the button.
            // The footer keeps its own Close as well: the header pair is chrome, the footer one is
            // the obvious target for someone who has just finished reading the list.
            Rect closeButtonRect = new Rect(headerRect.width - 26f, 6f, 20f, 18f);
            if (GUI.Button(closeButtonRect, "✕", MilStyle.GlyphButton))
            {
                CloseRaidReviewBrowser();
                return;
            }

            Rect maxButtonRect = new Rect(headerRect.width - 50f, 6f, 20f, 18f);
            if (GUI.Button(maxButtonRect, _raidReviewMaximized ? "▭" : "▣", MilStyle.GlyphButton))
            {
                ToggleRaidReviewMaximized();
            }

            GUI.DragWindow(headerRect);

            GUILayout.Space(headerHeight + 6f);

            DrawRaidReviewTabBar();

            // Early return rather than wrapping the log in a branch: the recordings body below is
            // unchanged code, and re-indenting two hundred lines would hide the real diff.
            if (_raidReviewTab != RaidReviewTab.Recordings)
            {
                DrawRaidReviewSecondaryTab();
                DrawRaidReviewFooter(fullRect);
                return;
            }

            GUILayout.Label(L("СПИСОК ЗАПИСЕЙ", "RECORDING LOG", "AUFZEICHNUNGSLISTE", "REGISTRO DE GRABACIONES",
                "JOURNAL D'ENREGISTREMENT", "DZIENNIK NAGRAŃ", "REGISTRO REGISTRAZIONI", "SEZNAM NAHRÁVEK"), MilStyle.SectionLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label(L("ДЕНЬ:", "DAY:", "TAG:", "DÍA:", "JOUR :", "DZIEŃ:", "GIORNO:", "DEN:"), MilStyle.DimLabel);
            if (_raidReviewDays.Length == 0)
            {
                GUILayout.Label("—", MilStyle.DimLabel);
            }
            else
            {
                GUI.enabled = _raidReviewDayIndex < _raidReviewDays.Length - 1;
                if (GUILayout.Button("◀", MilStyle.GlyphButton, GUILayout.Width(26f), GUILayout.Height(20f)))
                {
                    _raidReviewDayIndex++;
                    RefreshRaidReviewClips();
                }

                GUI.enabled = true;
                GUILayout.Label(_raidReviewDays[_raidReviewDayIndex], MilStyle.ValueLabel, GUILayout.Width(104f), GUILayout.Height(20f));

                GUI.enabled = _raidReviewDayIndex > 0;
                if (GUILayout.Button("▶", MilStyle.GlyphButton, GUILayout.Width(26f), GUILayout.Height(20f)))
                {
                    _raidReviewDayIndex--;
                    RefreshRaidReviewClips();
                }

                GUI.enabled = true;
            }

            if (GUILayout.Button(L("Обновить", "Refresh", "Aktualisieren", "Actualizar", "Actualiser", "Odśwież", "Aggiorna", "Obnovit").ToUpperInvariant(),
                MilStyle.Button, GUILayout.Height(20f)))
            {
                RefreshRaidReviewDays();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();

            GUILayout.Label(L("ПОИСК:", "SEARCH:", "SUCHE:", "BUSCAR:", "RECHERCHE :", "SZUKAJ:", "CERCA:", "HLEDAT:"),
                MilStyle.DimLabel, GUILayout.Width(58f));

            GUI.SetNextControlName("RaidReviewFilter");
            string typedFilter = GUILayout.TextField(_raidReviewFilter, MilStyle.Field, GUILayout.MinWidth(80f), GUILayout.Height(20f));
            if (typedFilter != _raidReviewFilter)
            {
                _raidReviewFilter = typedFilter;
                ApplyRaidReviewFilterAndSort();
            }

            if (GUILayout.Button(_raidReviewNewestFirst.Value ? "▼ " + L("НОВЫЕ", "NEWEST", "NEUESTE", "NUEVAS", "RÉCENTS", "NOWE", "NUOVE", "NOVÉ")
                    : "▲ " + L("СТАРЫЕ", "OLDEST", "ÄLTESTE", "ANTIGUAS", "ANCIENS", "STARE", "VECCHIE", "STARÉ"),
                MilStyle.Button, GUILayout.Width(88f), GUILayout.Height(20f)))
            {
                _raidReviewNewestFirst.Value = !_raidReviewNewestFirst.Value;
                ApplyRaidReviewFilterAndSort();
            }

            if (GUILayout.Button(L("КАРТА", "MAP", "KARTE", "MAPA", "CARTE", "MAPA", "MAPPA", "MAPA"),
                _raidReviewSortByLocation.Value ? MilStyle.Button : MilStyle.ButtonOff,
                GUILayout.Width(58f), GUILayout.Height(20f)))
            {
                _raidReviewSortByLocation.Value = !_raidReviewSortByLocation.Value;
                ApplyRaidReviewFilterAndSort();
            }

            if (GUILayout.Button(L("ПАПКА", "FOLDER", "ORDNER", "CARPETA", "DOSSIER", "FOLDER", "CARTELLA", "SLOŽKA"),
                MilStyle.Button, GUILayout.Width(62f), GUILayout.Height(20f)))
            {
                OpenCurrentDayFolder();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            Rect waveformRect = GUILayoutUtility.GetRect(10f, 50f, GUILayout.ExpandWidth(true));
            DrawWaveform(waveformRect);

            GUILayout.Space(4f);

            DrawTransportBar();

            GUILayout.Space(8f);

            GUILayout.Label(L("ГРОМКОСТЬ ВОСПРОИЗВЕДЕНИЯ", "PLAYBACK VOLUME", "WIEDERGABELAUTSTÄRKE", "VOLUMEN DE REPRODUCCIÓN",
                "VOLUME DE LECTURE", "GŁOŚNOŚĆ ODTWARZANIA", "VOLUME DI RIPRODUZIONE", "HLASITOST PŘEHRÁVÁNÍ"), MilStyle.SectionLabel);

            DrawPlaybackVolumeSlider();

            GUILayout.Space(8f);

            string keepLabel = L("СОХР", "KEEP", "HALT", "GUARD", "GARDE", "ZACH", "TIENI", "DRŽ");
            string delLabel = L("УДЛ", "DEL", "LÖS", "BOR", "SUPP", "USUŃ", "ELIM", "SMAZ");
            string sureLabel = L("ДА?", "SURE?", "OK?", "¿OK?", "SÛR ?", "NA PEWNO?", "OK?", "JISTĚ?");

            DrawLocationBar();

            // Column header, so the alignment below reads as a table rather than as coincidence.
            if (UiStyleState.IsInstrument)
            {
                DrawJournalHeader(GUILayoutUtility.GetRect(10f, 14f, GUILayout.ExpandWidth(true)));
            }

            MilStyle.PushScrollbarSkin();
            _raidReviewScroll = GUILayout.BeginScrollView(_raidReviewScroll, MilStyle.ScrollView);

            if (_raidReviewClips.Length == 0)
            {
                DrawEmptyRecordingsState();
            }

            for (int rowIndex = 0; rowIndex < _raidReviewClips.Length; rowIndex++)
            {
                RaidReviewClipInfo clip = _raidReviewClips[rowIndex];
                try
                {
                    if (clip.StartsNewGroup && rowIndex > 0)
                    {
                        DrawRaidSeparator(clip.Location);
                    }

                    bool isPlaying = _raidReviewNowPlaying == clip.FileName;
                    bool isProtected = clip.IsProtected;

                    Rect rowRect = GUILayoutUtility.GetRect(10f, 26f, GUILayout.ExpandWidth(true));

                    float appear = Mathf.Clamp01((Time.unscaledTime - _raidReviewListRefreshTime - rowIndex * 0.035f) / 0.18f);
                    if (appear <= 0f)
                    {
                        continue;
                    }
                    float slideX = (1f - appear) * 18f;
                    Rect animRowRect = new Rect(rowRect.x + slideX, rowRect.y, rowRect.width, rowRect.height);

                    MilStyle.DrawPanelBackground(animRowRect, appear);

                    Color prevRowColor = GUI.color;

                    if (animRowRect.Contains(Event.current.mousePosition))
                    {
                        GUI.color = new Color(MilStyle.Accent.r, MilStyle.Accent.g, MilStyle.Accent.b, 0.10f * appear);
                        GUI.DrawTexture(animRowRect, Texture2D.whiteTexture);
                    }

                    GUI.color = new Color(1f, 1f, 1f, appear);

                    float textX = animRowRect.x + 6f;

                    if (isPlaying)
                    {
                        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                        GUI.color = new Color(MilStyle.AccentBright.r, MilStyle.AccentBright.g, MilStyle.AccentBright.b, appear * (0.5f + 0.5f * pulse));
                        GUI.DrawTexture(new Rect(animRowRect.x + 6f, animRowRect.y + animRowRect.height / 2f - 4f, 8f, 8f), Texture2D.whiteTexture);
                        GUI.color = new Color(1f, 1f, 1f, appear);
                        textX += 14f;
                    }

                    if (isProtected)
                    {
                        GUI.color = new Color(MilStyle.SignalBright.r, MilStyle.SignalBright.g, MilStyle.SignalBright.b, appear);
                        GUI.DrawTexture(new Rect(textX, animRowRect.y + 5f, 3f, animRowRect.height - 10f), Texture2D.whiteTexture);
                        GUI.color = new Color(1f, 1f, 1f, appear);
                        textX += 8f;
                    }

                    const float actionsWidth = 152f;
                    Rect textRect = new Rect(textX, animRowRect.y, Mathf.Max(10f, animRowRect.width - actionsWidth - (textX - animRowRect.x)), animRowRect.height);

                    if (UiStyleState.IsInstrument)
                    {
                        DrawJournalColumns(textRect, clip, isPlaying);
                    }
                    else
                    {
                        GUI.Label(textRect, clip.DisplayLabel, isPlaying ? MilStyle.PlayingLabel : MilStyle.BodyLabel);
                    }

                    float bx = animRowRect.xMax - actionsWidth + 4f;
                    float by = animRowRect.y + 3f;
                    float bh = animRowRect.height - 6f;

                    if (GUI.Button(new Rect(bx, by, 32f, bh), "▶", MilStyle.GlyphButton))
                    {
                        PlayRaidReviewIndex(rowIndex);
                    }

                    if (GUI.Button(new Rect(bx + 36f, by, 54f, bh), keepLabel, isProtected ? MilStyle.Button : MilStyle.ButtonOff))
                    {
                        ToggleRecordingProtection(clip.Path);
                    }

                    bool awaitingConfirm = _raidReviewPendingDeleteIndex == rowIndex && Time.unscaledTime < _raidReviewPendingDeleteUntil;
                    if (GUI.Button(new Rect(bx + 94f, by, 54f, bh), awaitingConfirm ? sureLabel : delLabel, MilStyle.ButtonOff))
                    {
                        if (awaitingConfirm)
                        {
                            _raidReviewPendingDeleteIndex = -1;
                            DeleteSingleRecording(clip.Path);
                        }
                        else
                        {
                            // First press only arms the delete; it disarms itself after a moment.
                            _raidReviewPendingDeleteIndex = rowIndex;
                            _raidReviewPendingDeleteUntil = Time.unscaledTime + 2.5f;
                        }
                    }

                    GUI.color = prevRowColor;
                    GUILayout.Space(2f);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("PRT: failed to render raid review row for '" + clip.Path + "': " + ex);
                }
            }
            GUILayout.EndScrollView();
            MilStyle.PopScrollbarSkin();

            DrawRaidReviewFooter(fullRect);
        }

        private const float RaidReviewGripSize = 13f;

        private static void DrawResizeGripVisual(Rect windowRect)
        {
            float baseX = windowRect.xMax - RaidReviewGripSize;
            float baseY = windowRect.yMax - RaidReviewGripSize;

            Color prev = GUI.color;

            // Three stepped bars rather than a solid wedge: the wedge read as a filled corner and
            // gave no hint that it was draggable. Widths and offsets are the mock-up geometry
            // (12/8/4 px wide, 2 px tall, 4 px apart), scaled to whatever grip size is in force.
            GUI.color = new Color(MilStyle.Signal.r, MilStyle.Signal.g, MilStyle.Signal.b, 0.65f);

            float u = RaidReviewGripSize / 16f;
            float thick = Mathf.Max(1f, Mathf.Round(2f * u));
            float right = baseX + RaidReviewGripSize - Mathf.Round(2f * u);

            for (int i = 0; i < 3; i++)
            {
                float w = Mathf.Round((12f - i * 4f) * u);
                float y = baseY + RaidReviewGripSize - Mathf.Round((2f + i * 4f) * u) - thick;
                GUI.DrawTexture(new Rect(right - w, y, w, thick), Texture2D.whiteTexture);
            }

            GUI.color = prev;
        }

        private AudioClip BuildClip(WavData wav, string name)
        {
            if (wav.Samples == null)
            {
                return null;
            }

            AudioClip clip = AudioClip.Create(name, wav.Samples.Length / wav.Channels, wav.Channels, wav.SampleRate, false);
            clip.SetData(wav.Samples, 0);
            return clip;
        }

        private AudioSource EnsureAudioSource(ref AudioSource source, string objName)
        {
            if (source != null)
            {
                return source;
            }

            GameObject obj = new GameObject(objName);
            source = obj.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;

            if (UnityEngine.Object.FindObjectOfType<AudioListener>() == null)
            {
                obj.AddComponent<AudioListener>();
                LogVerbose("PRT: no AudioListener found in scene, added a fallback one on " + objName);
            }

            LogVerbose("PRT: (re)created " + objName);
            return source;
        }

        private void PlayClip(WavData wav, string name)
        {
            AudioClip clip = BuildClip(wav, name);
            if (clip == null)
            {
                Logger.LogWarning("PRT: attempted to play an unloaded sound: " + name);
                return;
            }

            AudioSource source = EnsureAudioSource(ref _audioSource, "RadioMod_ClickAudio");
            source.volume = _soundVolume.Value;
            source.clip = clip;
            source.Play();
            LogVerbose("PRT: playing sound " + name + ", volume=" + _soundVolume.Value
                + " | isPlaying=" + source.isPlaying);
        }

        /// <summary>
        /// Semantic class of a notification. Drives the tag text and the default accent colour,
        /// so messages are distinguishable without reading them.
        /// </summary>
        private enum NotifyKind { Info, Success, Warning, Error }

        /// <summary>
        /// Visual treatment of the notification panel. Independent of the BEAR/USEC theme, which
        /// controls colours — this controls how much chrome is drawn around the message.
        /// </summary>
        // Strip / StripCompact / Plate are Instrument-only; the first four are the frozen Classic set.
        private enum NotificationStyle { Themed, ThemedCompact, Minimal, MinimalCompact, Strip, StripCompact, Plate }

        private ConfigEntry<NotificationStyle> _notificationStyleMode;
        private ConfigEntry<float> _notificationOpacity;

        private struct OverlayNotification
        {
            public string Message;
            public NotifyKind Kind;
            public Color Color;
            public float StartTime;
            public float ExpireTime;

            /// <summary>Own lifetime, so the remaining-time bar is drawn against the right scale.</summary>
            public float Duration;
        }

        private const float NotificationDurationSeconds = 2.5f;
        private const float NotificationFadeSeconds = 0.5f;
        private const float NotificationSlideInSeconds = 0.25f;

        private const int MaxVisibleNotifications = 3;
        private readonly List<OverlayNotification> _notifications = new List<OverlayNotification>();
        private GUIStyle _notificationStyle;
        private GUIStyle _notificationTagStyle;

        internal static string GetLanguageCode()
        {
            if (_uiLanguageOverride != null && _uiLanguageOverride.Value != UiLanguage.Auto)
            {
                switch (_uiLanguageOverride.Value)
                {
                    case UiLanguage.Russian: return "ru";
                    case UiLanguage.German: return "ge";
                    case UiLanguage.Spanish: return "es";
                    case UiLanguage.French: return "fr";
                    case UiLanguage.Polish: return "pl";
                    case UiLanguage.Italian: return "it";
                    case UiLanguage.Czech: return "cz";
                    default: return "en";
                }
            }

            try
            {
                string code = EFT.LocalizationManager.Instance?._currentApplicationCulture;
                switch (code)
                {
                    case "ru":
                    case "ge":
                    case "es":
                    case "fr":
                    case "pl":
                    case "it":
                    case "cz":
                        return code;
                    default:
                        return "en";
                }
            }
            catch
            {
                return "en";
            }
        }

        internal static string L(string ru, string en, string ge, string es, string fr, string pl, string it, string cz)
        {
            switch (GetLanguageCode())
            {
                case "ru": return ru;
                case "ge": return ge;
                case "es": return es;
                case "fr": return fr;
                case "pl": return pl;
                case "it": return it;
                case "cz": return cz;
                default: return en;
            }
        }

        /// <summary>
        /// Queues a notification. <paramref name="overrideColor"/> exists so events that already
        /// have a user-configurable colour in F12 keep honouring it; the kind still decides the tag.
        /// </summary>
        private void Notify(string message, NotifyKind kind = NotifyKind.Info, Color? overrideColor = null,
            float? durationSeconds = null)
        {
            LogVerbose("PRT: notification shown");
            if (!_showNotifications.Value)
            {
                return;
            }

            float now = Time.time;
            _notifications.Add(new OverlayNotification
            {
                Message = message,
                Kind = kind,
                Color = overrideColor ?? DefaultNotifyColor(kind),
                StartTime = now,
                Duration = durationSeconds ?? NotificationDurationSeconds,
                ExpireTime = now + (durationSeconds ?? NotificationDurationSeconds)
            });

            // Keep the stack shallow: older entries are dropped rather than pushed off-screen.
            if (_notifications.Count > MaxVisibleNotifications)
            {
                _notifications.RemoveRange(0, _notifications.Count - MaxVisibleNotifications);
            }
        }

        private Color DefaultNotifyColor(NotifyKind kind)
        {
            switch (kind)
            {
                case NotifyKind.Success: return _colorOn.Value;
                case NotifyKind.Warning: return _colorBusy.Value;
                case NotifyKind.Error: return new Color(0.85f, 0.25f, 0.25f);
                default: return MilStyle.Accent;
            }
        }

        private static string NotifyTag(NotifyKind kind)
        {
            switch (kind)
            {
                case NotifyKind.Success: return L("ОК", "OK", "OK", "OK", "OK", "OK", "OK", "OK");
                case NotifyKind.Warning: return L("ВНИМ", "WARN", "ACHT", "AVISO", "ALERTE", "UWAGA", "AVVISO", "POZOR");
                case NotifyKind.Error: return L("ОТКАЗ", "FAIL", "FEHLER", "FALLO", "ÉCHEC", "BŁĄD", "ERRORE", "CHYBA");
                default: return L("ИНФО", "INFO", "INFO", "INFO", "INFO", "INFO", "INFO", "INFO");
            }
        }

        private Texture2D _indicatorDotTexture;

        private Texture2D GetIndicatorDotTexture()
        {
            if (_indicatorDotTexture != null)
            {
                return _indicatorDotTexture;
            }

            const int size = 32;
            _indicatorDotTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            float center = (size - 1) / 2f;
            float radius = size / 2f - 1f;
            Color32[] pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px, py), new Vector2(center, center));
                    byte alpha = (byte)(Mathf.Clamp01(radius - dist + 1f) * 255f);
                    pixels[py * size + px] = new Color32(255, 255, 255, alpha);
                }
            }

            _indicatorDotTexture.SetPixels32(pixels);
            _indicatorDotTexture.Apply();
            return _indicatorDotTexture;
        }

        private static Item FindItemByTplRecursive(Item item, string tplId)
        {
            if (item == null)
            {
                return null;
            }

            if (item.StringTemplateId == tplId)
            {
                return item;
            }

            CompoundItem compound = item as CompoundItem;
            if (compound == null)
            {
                return null;
            }

            foreach (EFT.InventoryLogic.IContainer container in compound.Containers)
            {
                foreach (Item child in container.Items)
                {
                    Item found = FindItemByTplRecursive(child, tplId);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }

        private Item FindActiveRadioItem()
        {
            Player local = GetLocalPlayer();
            if (local?.Inventory?.Equipment == null || _activeRadioTplId == null)
            {
                return null;
            }

            foreach (EquipmentSlot slotType in RadioSelectableSlots)
            {
                Item contained = local.Inventory.Equipment.GetSlot(slotType)?.ContainedItem;
                Item found = FindItemByTplRecursive(contained, _activeRadioTplId);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// Coarse battery status for the active radio, distinguishing "nothing inserted" from
        /// "inserted but dead" so the two can be reported with different wording.
        /// </summary>
        private enum BatteryPowerState
        {
            /// <summary>No battery slots at all — no battery mod installed, or not a radio.</summary>
            NoSlots,
            /// <summary>Has battery slots, but every one of them is empty.</summary>
            Missing,
            /// <summary>At least one battery is inserted, but there is no usable charge.</summary>
            Dead,
            /// <summary>Fully populated and charged, but at or below the low-battery threshold.</summary>
            Low,
            /// <summary>Fully populated and comfortably charged.</summary>
            Ok
        }

        /// <summary>
        /// Reads the active radio's battery state from the batteries sitting in its slots.
        /// Deliberately uses the game's own resource API rather than reaching into the battery mod:
        /// any mod that adds battery slots holding resource items will light this up, and with no
        /// such mod the radio simply has no slots and reports <see cref="BatteryPowerState.NoSlots"/>.
        /// </summary>
        /// <summary>
        /// Nominal terminal voltage of one cell, by template. Only the three chemistries a radio can
        /// actually take are listed; anything else falls back to 1.5 V rather than reporting zero,
        /// since an unknown cell is far more likely to be an alkaline than to be no cell at all.
        /// </summary>
        private static float NominalCellVolts(string tpl)
        {
            switch (tpl)
            {
                case "590a358486f77429692b2790": return 3.0f;  // CR123A
                case "5672cb304bdc2dc2088b456a": return 3.0f;  // CR2032
                case "5672cb124bdc2d1a0f8b4568": return 1.5f;  // AA
                default: return 1.5f;
            }
        }

        /// <summary>
        /// Nominal pack voltage of the active radio, cells assumed in series — which is how a
        /// multi-cell handheld is actually wired. Cached from the last battery poll so the readout
        /// does not have to walk the slots again on every frame.
        /// </summary>
        private float _batteryPackVolts;

        internal float BatteryPackVolts => _batteryPackVolts;

        private BatteryPowerState GetRadioBatteryState(out float fraction)
        {
            fraction = 0f;

            try
            {
                CompoundItem radio = FindActiveRadioItem() as CompoundItem;
                if (radio == null)
                {
                    return BatteryPowerState.NoSlots;
                }

                EnsureBatterySyncRegistration(radio);

                float value = 0f;
                float max = 0f;
                int batterySlots = 0;
                int emptySlots = 0;
                float packVolts = 0f;

                foreach (Slot slot in radio.Slots)
                {
                    if (!IsBatterySlot(slot))
                    {
                        continue;
                    }

                    batterySlots++;

                    Item cell = slot.ContainedItem;
                    if (cell == null)
                    {
                        emptySlots++;
                        continue;
                    }

                    if (cell.TryGetItemComponent(out ResourceComponent resource) && resource.MaxResource > 0f)
                    {
                        value += resource.Value;
                        max += resource.MaxResource;
                    }

                    packVolts += NominalCellVolts(cell.TemplateId);
                }

                if (batterySlots == 0)
                {
                    return BatteryPowerState.NoSlots;
                }

                // The radio needs every battery slot filled to run, and an empty slot means there is
                // literally a battery missing — reported the same way whether it's the only one or
                // one of several, since the fix in both cases is "insert a battery". "Dead" is
                // reserved for a fully populated radio that has simply run its batteries down.
                if (emptySlots > 0)
                {
                    fraction = 0f;
                    return BatteryPowerState.Missing;
                }

                if (max <= 0f)
                {
                    fraction = 0f;
                    return BatteryPowerState.Dead;
                }

                _batteryPackVolts = packVolts;
                fraction = Mathf.Clamp01(value / max);
                if (fraction <= 0f)
                {
                    return BatteryPowerState.Dead;
                }

                return fraction <= LowBatteryThreshold ? BatteryPowerState.Low : BatteryPowerState.Ok;
            }
            catch (Exception ex)
            {
                LogVerbose("PRT: battery state lookup failed: " + ex.Message);
                return BatteryPowerState.NoSlots;
            }
        }

        /// <summary>Kept for the HUD readout, which only ever needs the raw fraction.</summary>
        private bool TryGetRadioBatteryCharge(out float fraction)
        {
            return GetRadioBatteryState(out fraction) != BatteryPowerState.NoSlots;
        }

        private float _nextBatterySyncDiagnosticTime;
        private Type _batteryDeviceManagerType;
        private MethodInfo _batteryIsItemRegisteredMethod;
        private MethodInfo _batteryAddMethod;
        private MethodInfo _batteryGetBatterySlotsMethod;
        private Type _batteryDeviceDataType;
        private PropertyInfo _batteryDataBatteryProp;
        private PropertyInfo _batteryDataSlotCountProp;
        private PropertyInfo _batteryDataDrainProp;
        private bool _batteryReflectionFailed;

        /// <summary>
        /// Workaround for the "battery charge/FiR resets after a drop in co-op" report: the battery
        /// mod's own DeviceManager only registers a device at two points — raid start and player
        /// spawn — via an async config fetch from the server that isn't guaranteed to have finished
        /// by then. Confirmed by decompiling both DLLs and by querying
        /// /BatteriesNotIncluded/GetConfig directly against a live server: the config it serves
        /// already lists all 13 radios correctly, and mod load order is correct too, so the miss is
        /// purely a client-side timing gap in their registration, not missing data on our end.
        ///
        /// Their own <c>DeviceManager.Add</c> is public and already idempotent — it looks the item up
        /// by id first and updates in place instead of appending a second entry — so calling it
        /// ourselves whenever we notice a radio isn't registered is safe even if their own delayed
        /// registration lands afterwards; either order converges to one entry, never a duplicate.
        /// Throttled to once every few seconds per radio-active tick.
        /// </summary>
        private void EnsureBatterySyncRegistration(CompoundItem radio)
        {
            if (_batteryReflectionFailed)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now < _nextBatterySyncDiagnosticTime)
            {
                return;
            }

            _nextBatterySyncDiagnosticTime = now + 5f;

            try
            {
                if (_batteryDeviceManagerType == null)
                {
                    _batteryDeviceManagerType = AccessTools.TypeByName("BatteriesNotIncluded.Managers.DeviceManager");
                    Type commonExtensionsType = AccessTools.TypeByName("BatteriesNotIncluded.Utils.CommonExtensions");
                    _batteryDeviceDataType = AccessTools.TypeByName("BatteriesNotIncluded.Models.DeviceData");

                    if (_batteryDeviceManagerType != null)
                    {
                        _batteryIsItemRegisteredMethod = AccessTools.Method(_batteryDeviceManagerType, "IsItemRegistered", new[] { typeof(Item) });
                    }

                    if (commonExtensionsType != null)
                    {
                        _batteryGetBatterySlotsMethod = AccessTools.Method(commonExtensionsType, "GetBatterySlots", new[] { typeof(CompoundItem), typeof(int?) });
                    }

                    if (_batteryDeviceDataType != null)
                    {
                        _batteryAddMethod = AccessTools.Method(_batteryDeviceManagerType, "Add",
                            new[] { typeof(CompoundItem), typeof(Slot[]), _batteryDeviceDataType.MakeByRefType() });
                        _batteryDataBatteryProp = AccessTools.Property(_batteryDeviceDataType, "Battery");
                        _batteryDataSlotCountProp = AccessTools.Property(_batteryDeviceDataType, "SlotCount");
                        _batteryDataDrainProp = AccessTools.Property(_batteryDeviceDataType, "DrainPerSecond");
                    }

                    if (_batteryDeviceManagerType == null || _batteryIsItemRegisteredMethod == null
                        || _batteryAddMethod == null || _batteryGetBatterySlotsMethod == null
                        || _batteryDataBatteryProp == null || _batteryDataSlotCountProp == null || _batteryDataDrainProp == null)
                    {
                        LogVerbose("PRT: battery-sync workaround disabled — could not resolve one or more members via reflection");
                        _batteryReflectionFailed = true;
                        return;
                    }
                }

                UnityEngine.Object deviceManager = UnityEngine.Object.FindObjectOfType(_batteryDeviceManagerType);
                if (deviceManager == null)
                {
                    LogVerbose("PRT: battery-sync — no DeviceManager instance found in scene");
                    return;
                }

                bool registered = (bool)_batteryIsItemRegisteredMethod.Invoke(deviceManager, new object[] { radio });
                if (registered)
                {
                    return;
                }

                object[] batterySlotsArgs = { radio, null };
                object batterySlotsObj = _batteryGetBatterySlotsMethod.Invoke(null, batterySlotsArgs);
                Slot[] batterySlots = batterySlotsObj as Slot[];
                if (batterySlots == null || batterySlots.Length == 0)
                {
                    // No battery slots on this item at all (shouldn't happen for a radio with the
                    // battery mod present, but bail quietly rather than guess).
                    return;
                }

                string batteryTplId = null;
                if (batterySlots[0].Filters != null)
                {
                    foreach (var filter in batterySlots[0].Filters)
                    {
                        if (filter?.Filter == null)
                        {
                            continue;
                        }

                        foreach (var id in filter.Filter)
                        {
                            batteryTplId = id.ToString();
                            break;
                        }

                        if (batteryTplId != null)
                        {
                            break;
                        }
                    }
                }

                if (batteryTplId == null)
                {
                    LogVerbose("PRT: battery-sync — could not read a battery tpl id off the radio's own slot filter");
                    return;
                }

                object deviceData = Activator.CreateInstance(_batteryDeviceDataType);
                _batteryDataBatteryProp.SetValue(deviceData, batteryTplId);
                _batteryDataSlotCountProp.SetValue(deviceData, batterySlots.Length);
                // 0 on purpose: our own UpdateBatteryPower/drain-multiplier logic already drains the
                // radio's batteries. Letting their BatteryDrainSystem also drain at a nonzero rate
                // would double-drain it.
                _batteryDataDrainProp.SetValue(deviceData, 0f);

                object[] addArgs = { radio, batterySlots, deviceData };
                _batteryAddMethod.Invoke(deviceManager, addArgs);

                LogVerbose("PRT: battery-sync — force-registered radio=" + radio.StringTemplateId
                    + " id=" + radio.Id + " slots=" + batterySlots.Length + " battery=" + batteryTplId);
            }
            catch (Exception ex)
            {
                LogVerbose("PRT: battery-sync workaround failed: " + ex.Message);
            }
        }

        private float _nextBatteryDrainTick;
        private bool _lowPowerWarned;
        private bool _radioHasBatterySlots;

        private const float LowBatteryThreshold = 0.15f;

        /// <summary>
        /// Enforces battery power: warns once when the charge runs low and shuts the radio down
        /// when it hits zero. Radios with no battery slots at all (no battery mod installed) are
        /// treated as always powered, so a plain install behaves exactly as before.
        /// </summary>
        private void UpdateBatteryPower(DissonanceComms comms)
        {
            if (!_cachedIsInRaid)
            {
                _lowPowerWarned = false;
                return;
            }

            BatteryPowerState state = GetRadioBatteryState(out _);
            _radioHasBatterySlots = state != BatteryPowerState.NoSlots;

            if (!_radioHasBatterySlots)
            {
                return;
            }

            if (state == BatteryPowerState.Missing || state == BatteryPowerState.Dead)
            {
                if (_radioOn)
                {
                    _radioOn = false;
                    ApplyReceiveState(comms);
                    Notify(state == BatteryPowerState.Missing
                        ? L("Необходимы батарейки — рация выключена", "Batteries needed — radio shut down",
                            "Batterien benötigt — Funkgerät aus", "Se necesitan baterías — radio apagada",
                            "Piles nécessaires — radio éteinte", "Potrzebne baterie — radiotelefon wyłączony",
                            "Batterie necessarie — radio spenta", "Je potřeba baterie — vysílačka vypnuta")
                        : L("Батарея разряжена — рация выключена", "Battery dead — radio shut down",
                            "Batterie leer — Funkgerät aus", "Batería agotada — radio apagada",
                            "Batterie vide — radio éteinte", "Bateria wyczerpana — radiotelefon wyłączony",
                            "Batteria scarica — radio spenta", "Baterie vybitá — vysílačka vypnuta"),
                        NotifyKind.Error);
                    // The radio is powering down, so it plays its normal shutdown sound; the
                    // low-power tone belongs to the warning before that, not to the shutdown.
                    PlayClip(_offSound, "off");
                }

                return;
            }

            if (state == BatteryPowerState.Low)
            {
                if (!_lowPowerWarned && _radioOn)
                {
                    _lowPowerWarned = true;
                    Notify(L("Низкий заряд батареи", "Low battery", "Batterie schwach", "Batería baja",
                        "Batterie faible", "Niski poziom baterii", "Batteria quasi scarica", "Nízký stav baterie"),
                        NotifyKind.Warning);
                    PlayClip(_lowPowerSound, "low_pwr");
                }
            }
            else
            {
                // Re-arm once the pack is swapped, so the next drain warns again.
                _lowPowerWarned = false;
            }
        }

        /// <summary>True when the radio cannot be used because it has no usable battery.</summary>
        private bool IsRadioOutOfPower(out bool missing)
        {
            missing = false;

            if (!_radioHasBatterySlots)
            {
                return false;
            }

            BatteryPowerState state = GetRadioBatteryState(out _);
            missing = state == BatteryPowerState.Missing;
            return state == BatteryPowerState.Missing || state == BatteryPowerState.Dead;
        }

        /// <summary>
        /// Applies the developer drain multiplier on top of whatever the battery mod already does.
        /// The multiplier is latched outside a raid only, so it cannot be changed mid-raid to make
        /// a dying battery suddenly last longer.
        /// </summary>
        private void UpdateBatteryDrain()
        {
            if (!_cachedIsInRaid)
            {
                _appliedBatteryDrainMultiplier = _batteryDrainMultiplier.Value;
                return;
            }

            // Only the extra drain above the mod's own rate is ours to apply.
            float extra = _appliedBatteryDrainMultiplier - 1f;
            if (extra <= 0f || !_radioOn || Time.time < _nextBatteryDrainTick)
            {
                return;
            }

            _nextBatteryDrainTick = Time.time + 1f;

            try
            {
                CompoundItem radio = FindActiveRadioItem() as CompoundItem;
                if (radio == null)
                {
                    return;
                }

                foreach (Slot slot in radio.Slots)
                {
                    Item cell = slot?.ContainedItem;
                    if (cell == null || !cell.TryGetItemComponent(out ResourceComponent resource) || resource.Value <= 0f)
                    {
                        continue;
                    }

                    // Base rate is derived from the cell's own capacity so the multiplier scales
                    // proportionally regardless of which battery type is fitted.
                    float perSecond = resource.MaxResource / 3600f;
                    resource.Value = Mathf.Max(0f, resource.Value - perSecond * extra);
                }
            }
            catch (Exception ex)
            {
                LogVerbose("PRT: extra battery drain failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Explicit charge colours rather than theme colours, so the state reads the same in both
        /// factions and the percentage visibly shifts red as the pack runs down.
        /// </summary>
        /// <summary>
        /// A slot counts as a battery slot when its filter accepts one of the battery items —
        /// the same rule the battery mod itself uses, rather than matching on slot names.
        /// </summary>
        private static bool IsBatterySlot(Slot slot)
        {
            if (slot?.Filters == null)
            {
                return false;
            }

            foreach (var filter in slot.Filters)
            {
                if (filter?.Filter == null)
                {
                    continue;
                }

                foreach (var id in filter.Filter)
                {
                    string tpl = id.ToString();
                    if (tpl == AaBatteryTplId || tpl == Cr123ABatteryTplId || tpl == Cr2032BatteryTplId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private const string AaBatteryTplId = "5672cb124bdc2d1a0f8b4568";
        private const string Cr123ABatteryTplId = "590a358486f77429692b2790";
        private const string Cr2032BatteryTplId = "5672cb304bdc2dc2088b456a";

        private static Color BatteryChargeColor(float fraction)
        {
            if (fraction <= 0.15f)
            {
                return new Color(0.90f, 0.25f, 0.22f);
            }

            if (fraction <= 0.40f)
            {
                return new Color(0.95f, 0.72f, 0.20f);
            }

            return new Color(0.45f, 0.85f, 0.35f);
        }

        private float GetWavDuration(WavData wav)
        {
            if (wav.Samples == null || wav.Channels == 0)
            {
                return 0f;
            }

            return (wav.Samples.Length / wav.Channels) / (float)wav.SampleRate;
        }

        private Player GetLocalPlayer()
        {
            if (_localPlayer != null)
            {
                return _localPlayer;
            }

            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                _localPlayer = gameWorld.MainPlayer;
            }

            return _localPlayer;
        }

        private void CacheLocalIdentityIfAvailable()
        {
            // Outside a raid the answer cannot change, so stop probing once it is known.
            // Inside a raid keep refreshing, since the character (PMC vs scav) can differ.
            if (!_cachedIsInRaid && !string.IsNullOrEmpty(_cachedNickname))
            {
                return;
            }

            // Two sources, checked in order of reliability: the in-raid Player, then the backend
            // session that stays alive in the menu and hideout. Whatever is found is cached in
            // memory and persisted, so the callsign survives both leaving a raid and restarting.
            Player local = GetLocalPlayer();
            string nickname = local?.Profile?.Info?.Nickname;
            EPlayerSide? side = local?.Profile?.Info?.Side;

            if (string.IsNullOrEmpty(nickname) && !TryGetIdentityFromBackEndSession(out nickname, out side))
            {
                return;
            }

            _cachedNickname = nickname;
            _cachedSide = side;

            if (_lastKnownNickname.Value != nickname)
            {
                _lastKnownNickname.Value = nickname;
                _lastKnownSide.Value = side.ToString();
                Logger.LogInfo("PRT: local identity resolved as " + nickname + " / " + side);
            }
        }

        private bool TryGetLocalIdentity(out string nickname, out EPlayerSide? side)
        {
            Player local = GetLocalPlayer();
            if (local != null && local.Profile?.Info != null && !string.IsNullOrEmpty(local.Profile.Info.Nickname))
            {
                nickname = local.Profile.Info.Nickname;
                side = local.Profile.Info.Side;
                return true;
            }

            if (!string.IsNullOrEmpty(_cachedNickname))
            {
                nickname = _cachedNickname;
                side = _cachedSide;
                return true;
            }

            // Last resort: the identity saved during a previous session.
            if (!string.IsNullOrEmpty(_lastKnownNickname.Value))
            {
                nickname = _lastKnownNickname.Value;
                side = Enum.TryParse(_lastKnownSide.Value, out EPlayerSide storedSide) ? storedSide : (EPlayerSide?)null;
                return true;
            }

            nickname = null;
            side = null;
            return false;
        }

        /// <summary>
        /// Pulls the profile from the game's backend session, which exists in the menu and the
        /// hideout — unlike the Player object, which only exists inside a raid.
        /// </summary>
        private bool TryGetIdentityFromBackEndSession(out string nickname, out EPlayerSide? side)
        {
            nickname = null;
            side = null;

            try
            {
                ClientApplication<IClientSession> app = Singleton<ClientApplication<IClientSession>>.Instance;
                ProfileInfo info = app?.GetClientBackEndSession()?.Profile?.Info;
                if (info == null || string.IsNullOrEmpty(info.Nickname))
                {
                    return false;
                }

                nickname = info.Nickname;
                side = info.Side;
                return true;
            }
            catch (Exception ex)
            {
                LogVerbose("PRT: backend session identity lookup failed: " + ex.Message);
                return false;
            }
        }

        private string ResolveDisplayName(string profileId, string fallback)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return fallback;
            }

            try
            {
                Player local = GetLocalPlayer();
                bool isLocalProfile = local != null && local.Profile != null && local.Profile.Id == profileId;
                if (isLocalProfile && TryGetLocalIdentity(out string localNick, out _))
                {
                    return localNick;
                }

                FikaPlayer fp = GetFikaPlayerByProfileId(profileId);
                string remoteNick = fp?.Profile?.Info?.Nickname;
                if (!string.IsNullOrEmpty(remoteNick))
                {
                    return remoteNick;
                }
            }
            catch (Exception ex)
            {
                LogVerbose("PRT: nickname lookup failed for '" + profileId + "': " + ex.Message);
            }

            return fallback;
        }

        private string GetRemoteRadioTplId(string profileId)
        {
            FikaPlayer fp = GetFikaPlayerByProfileId(profileId);
            if (fp == null || fp.Inventory == null || fp.Inventory.Equipment == null)
            {
                return null;
            }

            List<string> selectable = CollectSelectableRadioTpls(fp.Inventory.Equipment);
            return selectable.Count > 0 ? selectable[0] : null;
        }

        private bool IsVanillaVoipActive()
        {
            Player local = GetLocalPlayer();
            return local != null && local.VoipController != null
                && local.VoipController.Status.Value == EVoipControllerStatus.Talking;
        }

        private void UpdateRaidReviewResize()
        {
            if (!_showRaidReviewBrowser)
            {
                _raidReviewResizing = false;

                // The safety net for modality: whatever closed the window — the button, the hotkey,
                // a raid starting, or an exception during draw — input comes back here.
                WindowModality.EnsureClosed();
                return;
            }

            Vector2 mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

            if (!_raidReviewResizing)
            {
                Rect gripRect = new Rect(_raidReviewWindowRect.xMax - RaidReviewGripSize, _raidReviewWindowRect.yMax - RaidReviewGripSize,
                    RaidReviewGripSize, RaidReviewGripSize);

                if (Input.GetMouseButtonDown(0) && gripRect.Contains(mouseGui))
                {
                    _raidReviewResizing = true;
                    _raidReviewResizeStartMouse = mouseGui;
                    _raidReviewResizeStartSize = new Vector2(_raidReviewWindowRect.width, _raidReviewWindowRect.height);
                    // Dragging the grip means the window is no longer "maximised".
                    _raidReviewMaximized = false;
                }

                return;
            }

            if (!Input.GetMouseButton(0))
            {
                _raidReviewResizing = false;
                return;
            }

            // Only a lower bound, so the window can be dragged out to any size the screen allows.
            Vector2 delta = mouseGui - _raidReviewResizeStartMouse;
            _raidReviewWindowRect.width = Mathf.Max(340f, _raidReviewResizeStartSize.x + delta.x);
            _raidReviewWindowRect.height = Mathf.Max(240f, _raidReviewResizeStartSize.y + delta.y);
        }

        /// <summary>
        /// Resolved once per frame: ApplyRadioToPlayer runs per remote speaker and would otherwise
        /// re-read the location for each of them.
        /// </summary>
        private void UpdateRadioDeadZoneState()
        {
            if (!_cachedIsInRaid)
            {
                _cachedInRadioDeadZone = false;
                _cachedInAlincoAnomalyZone = false;
                return;
            }

            // Unconditional: the Labyrinth is always jammed. The old on/off setting is gone, so the
            // Alinco anomaly cannot be switched away by accident.
            bool wasJammed = _cachedInRadioDeadZone;
            string location = GetCurrentLocationId();
            _cachedInRadioDeadZone = RadioDeadZoneLocations.Contains(location);
            _cachedInAlincoAnomalyZone = _cachedInRadioDeadZone;

            // Leaving the dead zone re-arms the warning, so the next visit explains itself again.
            if (wasJammed && !_cachedInRadioDeadZone)
            {
                _jamWarningShown = false;
            }
        }

        private static bool IsInRaid()
        {
            GameWorld gameWorld = GetGameWorldOrNull();
            if (gameWorld == null || gameWorld.MainPlayer == null)
            {
                return false;
            }

            if (string.Equals(gameWorld.LocationId, "hideout", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return gameWorld.MainPlayer.gameObject != null && gameWorld.MainPlayer.gameObject.activeInHierarchy;
        }

        private bool _cachedIsInRaid;

        private void Update()
        {
            _cachedIsInRaid = IsInRaid();
            UpdateRadioDeadZoneState();

            if (_cachedIsInRaid)
            {
                if (_showRaidReviewBrowser)
                {
                    _showRaidReviewBrowser = false;
                    _raidReviewOpenTime = 0f;
                    if (_raidReviewAudioSource != null)
                    {
                        _raidReviewAudioSource.Stop();
                    }
                }
            }
            else if (_raidReviewKey.Value.IsDown())
            {
                _showRaidReviewBrowser = !_showRaidReviewBrowser;
                if (_showRaidReviewBrowser)
                {
                    _raidReviewOpenTime = 0f;
                    RefreshRaidReviewDays();
                }
            }

            UpdateRaidReviewResize();
            UpdateAlincoAnomalies();
            UpdateRaidReviewAutoAdvance();

            CheckRaidEndForAutoCleanup();

            CacheLocalIdentityIfAvailable();
            UpdateBatteryDrain();

            DissonanceComms comms = DissonanceComms.Instance;

            if (!_micRecorderSubscribed && comms != null && comms.MicrophoneCapture != null)
            {
                comms.MicrophoneCapture.Subscribe(_localMicRecorder);
                _micRecorderSubscribed = true;
            }
            else if (_micRecorderSubscribed && comms == null)
            {
                _micRecorderSubscribed = false;
            }

            if (Time.time >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.time + HeartbeatInterval;
                LogVerbose("PRT: heartbeat, DissonanceComms.Instance = " + (comms == null ? "null" : "OK")
                    + ", radioOn = " + _radioOn + ", radioLocation = " + _radioLocation);
            }

            if (comms == null)
            {
                _wasVanillaTalking = false;
                return;
            }

            UpdateBatteryPower(comms);
            CheckRemoteSpeaking(comms);
            EnforceRadioLocation(comms);
            CheckLocalVoice(comms);
        }

        private void EnforceRadioLocation(DissonanceComms comms)
        {
            if (Time.time >= _nextLocationCheck)
            {
                _nextLocationCheck = Time.time + 0.5f;
                _radioLocation = GetRadioLocation();
            }

            if (_radioOn && _radioLocation == RadioLocation.None)
            {
                _radioOn = false;
                ApplyReceiveState(comms);
                Notify(L("Рация выключена (нет в снаряжении)", "Radio turned off (not in gear)",
                    "Funkgerät ausgeschaltet (nicht in der Ausrüstung)", "Radio apagada (no está en el equipo)",
                    "Radio éteinte (absente de l'équipement)", "Radiotelefon wyłączony (brak w ekwipunku)",
                    "Radio spenta (non nell'equipaggiamento)", "Vysílačka vypnuta (není ve výbavě)"), NotifyKind.Warning);
            }
            else if (_radioOn && _radioLocation == RadioLocation.Backpack)
            {
                _radioOn = false;
                ApplyReceiveState(comms);
                Notify(L("Рация выключена (перемещена в рюкзак)", "Radio turned off (moved to backpack)",
                    "Funkgerät ausgeschaltet (in den Rucksack verschoben)", "Radio apagada (movida a la mochila)",
                    "Radio éteinte (déplacée dans le sac à dos)", "Radiotelefon wyłączony (przeniesiony do plecaka)",
                    "Radio spenta (spostata nello zaino)", "Vysílačka vypnuta (přesunuta do batohu)"), NotifyKind.Warning);
            }
        }

        private static readonly EquipmentSlot[] RadioBearingSlots =
        {
            EquipmentSlot.TacticalVest, EquipmentSlot.ArmorVest, EquipmentSlot.Pockets, EquipmentSlot.Backpack
        };

        private static readonly EquipmentSlot[] RadioSelectableSlots =
        {
            EquipmentSlot.TacticalVest, EquipmentSlot.ArmorVest, EquipmentSlot.Pockets
        };

        private RadioLocation GetRadioLocation()
        {
            _activeRadioTplId = null;

            Player local = GetLocalPlayer();
            if (local == null || local.Inventory == null || local.Inventory.Equipment == null)
            {
                _selectedRadioTplId = null;
                return RadioLocation.None;
            }

            InventoryEquipment eq = local.Inventory.Equipment;

            List<string> existsAnywhere = CollectAllRadioTpls(eq);
            if (existsAnywhere.Count == 0)
            {
                _selectedRadioTplId = null;
                return RadioLocation.None;
            }

            List<string> selectable = CollectSelectableRadioTpls(eq);
            bool selectedMissing = _selectedRadioTplId == null || !existsAnywhere.Contains(_selectedRadioTplId);
            bool selectedInaccessible = !selectedMissing && selectable.Count > 0 && !selectable.Contains(_selectedRadioTplId);

            if (selectedMissing || selectedInaccessible)
            {
                if (selectable.Count == 0)
                {
                    _selectedRadioTplId = null;
                    return RadioLocation.None;
                }

                _selectedRadioTplId = selectable[0];
                if (selectable.Count > 1 || selectedInaccessible)
                {
                    Notify(L("Выбрана рация: ", "Radio selected: ",
                        "Funkgerät ausgewählt: ", "Radio seleccionada: ", "Radio sélectionnée : ",
                        "Wybrano radiotelefon: ", "Radio selezionata: ", "Vybrána vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId),
                        NotifyKind.Info, _colorSelect.Value);
                }
            }

            _activeRadioTplId = _selectedRadioTplId;

            if (_activeRadioTplId == null || !SimplexCapableTplIds.Contains(_activeRadioTplId))
            {
                _duplexMode = DuplexMode.HalfDuplex;
            }

            if (SlotContainsSpecificRadio(eq, EquipmentSlot.TacticalVest, _selectedRadioTplId)
                || SlotContainsSpecificRadio(eq, EquipmentSlot.ArmorVest, _selectedRadioTplId)
                || SlotContainsSpecificRadio(eq, EquipmentSlot.Pockets, _selectedRadioTplId))
            {
                return RadioLocation.Ready;
            }

            if (SlotContainsSpecificRadio(eq, EquipmentSlot.Backpack, _selectedRadioTplId))
            {
                return RadioLocation.Backpack;
            }

            return RadioLocation.None;
        }

        private void SelectNextRadio()
        {
            Player local = GetLocalPlayer();
            if (local == null || local.Inventory == null || local.Inventory.Equipment == null)
            {
                Notify(L("Нет доступа к инвентарю", "No access to inventory",
                    "Kein Zugriff auf das Inventar", "Sin acceso al inventario",
                    "Aucun accès à l'inventaire", "Brak dostępu do ekwipunku",
                    "Nessun accesso all'inventario", "Žádný přístup k inventáři"), NotifyKind.Error);
                return;
            }

            List<string> available = CollectSelectableRadioTpls(local.Inventory.Equipment);
            if (available.Count == 0)
            {
                Notify(L("Нет рации в разгрузке/кармане для выбора", "No radio in vest/pocket to select",
                    "Kein Funkgerät in Weste/Tasche zum Auswählen", "No hay radio en el chaleco/bolsillo para seleccionar",
                    "Aucune radio dans le gilet/la poche à sélectionner", "Brak radiotelefonu w kamizelce/kieszeni do wyboru",
                    "Nessuna radio nel gilet/tasca da selezionare", "Žádná vysílačka ve vestě/kapse k výběru"), NotifyKind.Warning);
                return;
            }

            if (available.Count == 1)
            {
                _selectedRadioTplId = available[0];
                Notify(L("Только одна рация доступна: ", "Only one radio available: ",
                    "Nur ein Funkgerät verfügbar: ", "Solo hay una radio disponible: ",
                    "Une seule radio disponible : ", "Dostępny tylko jeden radiotelefon: ",
                    "È disponibile solo una radio: ", "K dispozici je pouze jedna vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId),
                    NotifyKind.Info, _colorSelect.Value);
                _radioLocation = GetRadioLocation();
                return;
            }

            int currentIndex = _selectedRadioTplId != null ? available.IndexOf(_selectedRadioTplId) : -1;
            int nextIndex = (currentIndex + 1) % available.Count;
            _selectedRadioTplId = available[nextIndex];
            Notify(L("Выбрана рация: ", "Radio selected: ",
                        "Funkgerät ausgewählt: ", "Radio seleccionada: ", "Radio sélectionnée : ",
                        "Wybrano radiotelefon: ", "Radio selezionata: ", "Vybrána vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId)
                + " (" + (nextIndex + 1) + "/" + available.Count + ")", NotifyKind.Info, _colorSelect.Value);

            _tuningSweepStartTime = Time.unscaledTime;

            // A freshly selected radio starts switched off: carrying the previous radio's power
            // state over would silently leave a different device transmitting.
            if (_radioOn)
            {
                _radioOn = false;
                DissonanceComms switchComms = DissonanceComms.Instance;
                if (switchComms != null)
                {
                    ApplyReceiveState(switchComms);
                }

                PlayClip(_offSound, "off");
            }

            _radioLocation = GetRadioLocation();
        }

        private static string GetRadioDisplayName(string tplId)
        {
            if (tplId != null && RadioDisplayNames.TryGetValue(tplId, out string name))
            {
                return name;
            }

            return L("неизвестная рация", "unknown radio", "unbekanntes Funkgerät", "radio desconocida",
                "radio inconnue", "nieznany radiotelefon", "radio sconosciuta", "neznámá vysílačka");
        }

        private List<string> CollectAllRadioTpls(InventoryEquipment eq)
        {
            return CollectRadioTplsFromSlots(eq, RadioBearingSlots);
        }

        private List<string> CollectSelectableRadioTpls(InventoryEquipment eq)
        {
            return CollectRadioTplsFromSlots(eq, RadioSelectableSlots);
        }

        private List<string> CollectRadioTplsFromSlots(InventoryEquipment eq, EquipmentSlot[] slots)
        {
            var results = new List<string>();
            foreach (EquipmentSlot slotType in slots)
            {
                Item contained = eq.GetSlot(slotType)?.ContainedItem;
                if (contained != null)
                {
                    CollectRadiosRecursive(contained, results);
                }
            }

            return results;
        }

        private void CollectRadiosRecursive(Item item, List<string> results)
        {
            if (item == null)
            {
                return;
            }

            if (RadioTplIds.Contains(item.StringTemplateId) && !results.Contains(item.StringTemplateId))
            {
                results.Add(item.StringTemplateId);
            }

            CompoundItem compound = item as CompoundItem;
            if (compound == null)
            {
                return;
            }

            foreach (EFT.InventoryLogic.IContainer container in compound.Containers)
            {
                foreach (Item child in container.Items)
                {
                    CollectRadiosRecursive(child, results);
                }
            }
        }

        private bool SlotContainsSpecificRadio(InventoryEquipment eq, EquipmentSlot slotType, string tplId)
        {
            Item contained = eq.GetSlot(slotType)?.ContainedItem;
            if (contained == null)
            {
                return false;
            }

            if (contained.StringTemplateId == tplId)
            {
                return true;
            }

            CompoundItem compound = contained as CompoundItem;
            return compound != null && compound.TryFindItem(it => it != null && it.StringTemplateId == tplId, out Item _);
        }

        private RadioSoundSet GetActiveSoundSet()
        {
            // Single choke point for cue selection, which is why the synthesised sets hook in here
            // and nothing else in the sound path had to change.
            if (_activeRadioTplId != null
                && _soundStyle != null
                && _soundStyle.Value != SoundStyle.Classic
                && RadioProfiles.ContainsKey(_activeRadioTplId))
            {
                return GetSynthSoundSet(_activeRadioTplId);
            }

            if (_activeRadioTplId != null && _radioSoundSets.TryGetValue(_activeRadioTplId, out RadioSoundSet set))
            {
                return set;
            }

            return _defaultSoundSet;
        }

        private RadioProfile GetActiveProfile()
        {
            if (_activeRadioTplId != null && RadioProfiles.TryGetValue(_activeRadioTplId, out RadioProfile profile))
            {
                return profile;
            }

            return BaofengProfile;
        }

        private static RadioVoiceFilter.Profile ToFilterProfile(RadioProfile p)
        {
            return new RadioVoiceFilter.Profile
            {
                LpCutoffNear = p.LpCutoffNear,
                LpCutoffFar = p.LpCutoffFar,
                HpCutoffHz = p.HpCutoffHz,
                DriveNear = p.DriveNear,
                DriveFar = p.DriveFar,
                CarrierHzNear = p.CarrierHzNear,
                CarrierHzFar = p.CarrierHzFar,
                RingMixNear = p.RingMixNear,
                RingMixFar = p.RingMixFar,
                VoiceGainNear = p.VoiceGainNear,
                VoiceGainFar = p.VoiceGainFar,
                NoiseAmpNear = p.NoiseAmpNear,
                NoiseAmpFar = p.NoiseAmpFar,
                NoiseLpCutoffHz = p.NoiseLpCutoffHz,
                DropoutChanceNear = p.DropoutChanceNear,
                DropoutChanceFar = p.DropoutChanceFar,
                StaticLpCutoff = p.StaticLpCutoff,
                StaticDrive = p.StaticDrive,
                StaticRingMix = p.StaticRingMix,
                StaticVoiceGain = p.StaticVoiceGain,
                StaticNoiseAmp = p.StaticNoiseAmp,
                StaticDropoutChance = p.StaticDropoutChance,
            };
        }

        private bool SlotHasRadio(InventoryEquipment eq, EquipmentSlot slotType, out string foundTplId, bool debugLog = false)
        {
            foundTplId = null;
            Slot slot = eq.GetSlot(slotType);
            Item contained = slot != null ? slot.ContainedItem : null;
            if (contained == null)
            {
                if (debugLog)
                {
                    LogVerbose("PRT DEBUG: slot " + slotType + " is empty (ContainedItem == null)");
                }
                return false;
            }

            if (RadioTplIds.Contains(contained.StringTemplateId))
            {
                foundTplId = contained.StringTemplateId;
                return true;
            }

            CompoundItem compound = contained as CompoundItem;
            if (compound != null)
            {
                bool found = compound.TryFindItem(it => it != null && RadioTplIds.Contains(it.StringTemplateId), out Item foundItem);
                if (found)
                {
                    foundTplId = foundItem.StringTemplateId;
                }
                return found;
            }

            if (debugLog)
            {
                LogVerbose("PRT DEBUG: slot " + slotType + " contains " + contained.StringTemplateId
                    + " (not a radio), CompoundItem=" + (compound != null) + ", actual type=" + contained.GetType().Name);
            }

            return false;
        }

        private void DebugDumpRadioLocation(InventoryEquipment eq)
        {
            SlotHasRadio(eq, EquipmentSlot.TacticalVest, out _, true);
            SlotHasRadio(eq, EquipmentSlot.ArmorVest, out _, true);
            SlotHasRadio(eq, EquipmentSlot.Pockets, out _, true);
            SlotHasRadio(eq, EquipmentSlot.Backpack, out _, true);
        }

        private void CheckLocalVoice(DissonanceComms comms)
        {
            bool talking = IsVanillaVoipActive();
            bool risingEdge = talking && !_wasVanillaTalking;
            bool fallingEdge = !talking && _wasVanillaTalking;
            _wasVanillaTalking = talking;

            if (risingEdge)
            {
                bool toggleModifier = Input.GetKey(_radioToggleModifier.Value);
                bool selectModifier = Input.GetKey(_selectRadioModifier.Value);
                bool duplexModifier = Input.GetKey(_duplexModeModifier.Value);

                if (selectModifier)
                {
                    SelectNextRadio();
                    return;
                }

                if (duplexModifier)
                {

                    if (!_radioOn)
                    {
                        Notify(L("Сначала включите рацию", "Turn on the radio first",
                            "Schalte zuerst das Funkgerät ein", "Enciende primero la radio",
                            "Allumez d'abord la radio", "Najpierw włącz radiotelefon",
                            "Accendi prima la radio", "Nejprve zapněte vysílačku"), NotifyKind.Warning);
                        return;
                    }

                    if (_activeRadioTplId == null || !SimplexCapableTplIds.Contains(_activeRadioTplId))
                    {
                        Notify(L("Переключение режима недоступно.", "Mode switching unavailable.",
                            "Moduswechsel nicht verfügbar.", "Cambio de modo no disponible.",
                            "Changement de mode indisponible.", "Zmiana trybu niedostępna.",
                            "Cambio modalità non disponibile.", "Přepnutí režimu není k dispozici."), NotifyKind.Warning);
                        return;
                    }

                    if (_radioLocation == RadioLocation.Backpack)
                    {
                        Notify(L("Рация в рюкзаке — нет доступа", "Radio is in backpack — no access",
                            "Funkgerät ist im Rucksack — kein Zugriff", "La radio está en la mochila — sin acceso",
                            "La radio est dans le sac à dos — aucun accès", "Radiotelefon jest w plecaku — brak dostępu",
                            "La radio è nello zaino — nessun accesso", "Vysílačka je v batohu — žádný přístup"), NotifyKind.Warning);
                        return;
                    }

                    _duplexMode = _duplexMode == DuplexMode.HalfDuplex ? DuplexMode.Simplex : DuplexMode.HalfDuplex;
                    Notify(_duplexMode == DuplexMode.Simplex
                        ? L("Режим: Дуплекс", "Mode: Duplex", "Modus: Duplex", "Modo: Dúplex", "Mode : Duplex", "Tryb: Dupleks", "Modalità: Duplex", "Režim: Duplex")
                        : L("Режим: Полудуплекс", "Mode: Half-duplex", "Modus: Halbduplex", "Modo: Semidúplex", "Mode : Semi-duplex", "Tryb: Półdupleks", "Modalità: Semiduplex", "Režim: Poloduplexní"),
                        NotifyKind.Info, _duplexMode == DuplexMode.Simplex ? _colorSimplex.Value : (Color?)null);
                    PlayClip(_switchModeSound, "swtch");
                    return;
                }

                if (toggleModifier)
                {

                    if (!_radioOn && _radioLocation == RadioLocation.None)
                    {
                        Player localDebug = GetLocalPlayer();
                        if (localDebug != null && localDebug.Inventory != null && localDebug.Inventory.Equipment != null)
                        {
                            DebugDumpRadioLocation(localDebug.Inventory.Equipment);
                        }
                        Notify(L("Нет рации в снаряжении", "No radio in gear",
                            "Kein Funkgerät in der Ausrüstung", "No hay radio en el equipo",
                            "Aucune radio dans l'équipement", "Brak radiotelefonu w ekwipunku",
                            "Nessuna radio nell'equipaggiamento", "Žádná vysílačka ve výbavě"), NotifyKind.Error);
                        return;
                    }

                    if (_radioLocation == RadioLocation.Backpack)
                    {
                        Notify(L("Рация в рюкзаке — нет доступа", "Radio is in backpack — no access",
                            "Funkgerät ist im Rucksack — kein Zugriff", "La radio está en la mochila — sin acceso",
                            "La radio est dans le sac à dos — aucun accès", "Radiotelefon jest w plecaku — brak dostępu",
                            "La radio è nello zaino — nessun accesso", "Vysílačka je v batohu — žádný přístup"), NotifyKind.Warning);
                        return;
                    }

                    if (!_radioOn && IsRadioOutOfPower(out bool batteriesMissing))
                    {
                        // Silent on purpose: a dead radio has no power to make any sound at all.
                        Notify(batteriesMissing
                            ? L("Необходимы батарейки", "Batteries needed", "Batterien benötigt", "Se necesitan baterías",
                                "Piles nécessaires", "Potrzebne baterie", "Batterie necessarie", "Je potřeba baterie")
                            : L("Батарея разряжена", "Battery dead", "Batterie leer", "Batería agotada",
                                "Batterie vide", "Bateria wyczerpana", "Batteria scarica", "Baterie vybitá"),
                            NotifyKind.Error);
                        return;
                    }

                    _radioOn = !_radioOn;
                    ApplyReceiveState(comms);

                    // In a jammed location the interference warning replaces the usual "radio on"
                    // toast — two notifications back to back would just overwrite each other.
                    // The jammed-channel warning used to replace this toast. It now fires on the
                    // first transmit attempt instead — see NotifyJammedOnce — so switching the radio
                    // on always reports plainly whether it is on or off.

                    Notify(_radioOn
                        ? L("Рация включена", "Radio on", "Funkgerät an", "Radio encendida", "Radio allumée", "Radiotelefon włączony", "Radio accesa", "Vysílačka zapnuta")
                        : L("Рация выключена", "Radio off", "Funkgerät aus", "Radio apagada", "Radio éteinte", "Radiotelefon wyłączony", "Radio spenta", "Vysílačka vypnuta"),
                        _radioOn ? NotifyKind.Success : NotifyKind.Info,
                        _radioOn ? _colorOn.Value : (Color?)null);
                    PlayClip(_radioOn ? _onSound : _offSound, _radioOn ? "on" : "off");
                    return;
                }

                if (_radioOn && _radioLocation == RadioLocation.Ready)
                {
                    StartRadioTransmit(comms);
                }
            }
            else if (fallingEdge && _txChannel != null)
            {
                StopRadioTransmit(comms);
            }
        }

        private bool _micMuted;

        private void SetMicMuted(bool muted)
        {
            if (_micMuted == muted)
            {
                return;
            }

            DissonanceComms comms = DissonanceComms.Instance;
            if (comms == null)
            {
                return;
            }

            comms.IsMuted = muted;

            Player local = GetLocalPlayer();
            if (local != null && local.VoipController != null)
            {
                local.VoipController.ForceMuteVoIP(muted);
            }

            _micMuted = muted;
            LogVerbose("PRT: microphone " + (muted ? "muted (receive only)" : "unmuted"));
        }

        private void StartRadioTransmit(DissonanceComms comms)
        {
            _txChannel = comms.RoomChannels.Open(TestFrequency, positional: false);
            PlayClip(GetActiveSoundSet().LocalStart, "local_start");
            NotifyJammedOnce();
            LogVerbose("PRT: transmitting on " + TestFrequency + " (vanilla VOIP + radio)");

            if (_recordRadioComms.Value)
            {
                _localMicRecorder.Reset();
                _localMicRecorder.Recording = true;
            }
        }

        private void StopRadioTransmit(DissonanceComms comms)
        {
            if (_txChannel != null)
            {
                comms.RoomChannels.Close(_txChannel.Value);
                _txChannel = null;
            }

            PlayClip(GetActiveSoundSet().LocalEnd, "local_end");
            LogVerbose("PRT: transmission ended");

            _localMicRecorder.Recording = false;
            _localMicRecorder.Flush(out float[] recordedSamples, out int recordedChannels, out int recordedSampleRate);
            if (_recordRadioComms.Value && recordedSamples.Length > 0 && recordedChannels > 0)
            {
                string localName = ResolveDisplayName(comms.LocalPlayerName, comms.LocalPlayerName ?? "You");
                string localRadio = GetRadioDisplayName(_activeRadioTplId);
                ProcessAndSaveRecording(localName, recordedSamples, recordedChannels, recordedSampleRate,
                    RadioVoiceFilter.Mode.Clear, 0f, ToFilterProfile(GetActiveProfile()), 0f, isLocal: true, radioName: localRadio);
            }
        }

        private void ApplyReceiveState(DissonanceComms comms)
        {
            bool shouldReceive = _radioOn;

            if (!_radioOn && _txChannel != null)
            {
                comms.RoomChannels.Close(_txChannel.Value);
                _txChannel = null;
            }

            if (shouldReceive && _rxMembership == null && _txChannel == null)
            {
                _rxMembership = comms.Rooms.Join(TestFrequency);
                LogVerbose("PRT: subscribed to receive on frequency " + TestFrequency);
            }
            else if (!shouldReceive && _rxMembership != null)
            {
                comms.Rooms.Leave(_rxMembership.Value);
                _rxMembership = null;
                _remoteSpeakingState.Clear();
            }
        }

        private bool _wasReceiving;

        private void CheckRemoteSpeaking(DissonanceComms comms)
        {

            RadioReceiving = _radioOn;

            if (!RadioReceiving)
            {

                if (_wasReceiving)
                {
                    ResetAllRadioAudio();
                }
                _wasReceiving = false;

                if (RadioSpeakerNames.Count > 0)
                {
                    RadioSpeakerNames.Clear();
                    _lastOnFreqTime.Clear();
                    _remoteSpeakingState.Clear();
                    _remoteStartPlayed.Clear();
                }
                return;
            }

            _wasReceiving = true;

            float now = Time.time;

            foreach (VoicePlayerState player in comms.Players)
            {
                if (player.IsLocalPlayer)
                {
                    continue;
                }

                _speakingChannelsBuffer.Clear();
                player.GetSpeakingChannels(_speakingChannelsBuffer);

                bool onFrequency = false;
                foreach (RemoteChannel channel in _speakingChannelsBuffer)
                {
                    if (channel.Type == ChannelType.Room && channel.TargetName == TestFrequency)
                    {
                        onFrequency = true;
                        break;
                    }
                }

                if (onFrequency)
                {
                    _lastOnFreqTime[player.Name] = now;
                }

                if (_lastOnFreqTime.TryGetValue(player.Name, out float lastSeen) && (now - lastSeen) <= SpeakingHoldSeconds)
                {
                    ApplyRadioToPlayer(player);
                }
                else if (_radioFilters.TryGetValue(player.Name, out RadioVoiceFilter idle))
                {

                    idle.SetState(RadioVoiceFilter.Mode.Passthrough, 0f, _noiseVolume.Value, RadioVoiceFilter.Profile.Default);
                }
            }

            _speakingScratch.Clear();
            foreach (var kv in _lastOnFreqTime)
            {
                bool active = (now - kv.Value) <= SpeakingHoldSeconds;
                bool wasSpeaking = _remoteSpeakingState.TryGetValue(kv.Key, out bool prev) && prev;

                if (active && !wasSpeaking)
                {
                    RadioSpeakerNames.Add(kv.Key);

                    float dist = GetDistanceToPlayer(kv.Key);
                    if (dist < 0f || dist <= GetEffectiveProfile(kv.Key).NoiseOnlyRangeMeters)
                    {
                        _remoteStartPlayed.Add(kv.Key);
                        PlayClip(GetActiveSoundSet().RemoteStart, "remote_start");
                    }

                    if (_recordRadioComms.Value)
                    {
                        if (_radioFilters.TryGetValue(kv.Key, out RadioVoiceFilter startFilter))
                        {
                            startFilter.Recording = true;
                            LogVerbose("PRT: recording armed for incoming from '" + kv.Key + "'");
                        }
                        else
                        {
                            LogVerbose("PRT: recording NOT armed for incoming from '" + kv.Key + "' — no RadioVoiceFilter registered yet");
                        }
                    }
                }
                else if (!active && wasSpeaking)
                {
                    RadioSpeakerNames.Remove(kv.Key);

                    if (_remoteStartPlayed.Remove(kv.Key))
                    {
                        PlayClip(GetActiveSoundSet().RemoteEnd, "remote_end");
                    }

                    if (_radioFilters.TryGetValue(kv.Key, out RadioVoiceFilter endFilter))
                    {
                        endFilter.Recording = false;
                        endFilter.FlushRecording(out float[] recordedSamples, out int recordedChannels);
                        LogVerbose("PRT: incoming recording from '" + kv.Key + "' flushed " + recordedSamples.Length
                            + " samples (" + recordedChannels + "ch), recordRadioComms=" + _recordRadioComms.Value);
                        if (_recordRadioComms.Value && recordedSamples.Length > 0)
                        {
                            // Not endFilter.GetLastState() — by this point in the frame the idle
                            // branch above may have already reset the filter to Passthrough, which
                            // GetLastState would faithfully (and wrongly) report. These caches hold
                            // the mode the message actually played in.
                            RadioVoiceFilter.Mode lastMode = _lastMode.TryGetValue(kv.Key, out RadioVoiceFilter.Mode cachedMode)
                                ? cachedMode : RadioVoiceFilter.Mode.Clear;
                            float lastRatio = _lastRatio.TryGetValue(kv.Key, out float cachedRatio) ? cachedRatio : 0f;
                            RadioVoiceFilter.Profile lastProfile = _lastRecordProfile.TryGetValue(kv.Key, out RadioVoiceFilter.Profile cachedProfile)
                                ? cachedProfile : RadioVoiceFilter.Profile.Default;
                            string remoteName = ResolveDisplayName(kv.Key, kv.Key);
                            string remoteRadio = GetRadioDisplayName(GetRemoteRadioTplId(kv.Key));
                            LogVerbose("PRT: saving incoming recording from '" + remoteName + "' mode=" + lastMode + " ratio=" + lastRatio.ToString("0.00"));
                            ProcessAndSaveRecording(remoteName, recordedSamples, recordedChannels, AudioSettings.outputSampleRate,
                                lastMode, lastRatio, lastProfile, GetDistanceToPlayer(kv.Key), isLocal: false, radioName: remoteRadio);
                        }
                    }
                    else
                    {
                        LogVerbose("PRT: incoming recording from '" + kv.Key + "' lost — no RadioVoiceFilter found at stop");
                    }
                    _speakingScratch.Add(kv.Key);
                }

                _remoteSpeakingState[kv.Key] = active;
            }

            foreach (string name in _speakingScratch)
            {
                _lastOnFreqTime.Remove(name);
                _remoteSpeakingState.Remove(name);
            }
        }

        private bool _loggedPlaybackTypeOnce;
        private readonly Dictionary<string, RadioVoiceFilter.Mode> _lastMode = new Dictionary<string, RadioVoiceFilter.Mode>();

        private readonly Dictionary<string, float> _lastRatio = new Dictionary<string, float>();

        /// <summary>
        /// Last real (non-idle) DSP profile per remote speaker, for recordings. Needed because the
        /// filter's own state gets reset to Passthrough by the idle-fadeout branch in
        /// CheckRemoteSpeaking BEFORE the stop-transition code in that same method gets a chance to
        /// read it — RadioVoiceFilter.GetLastState() was returning the just-reset Passthrough state
        /// instead of the mode the message actually played in, so saved recordings never carried any
        /// interference. This cache isn't touched by that idle branch, so it survives.
        /// </summary>
        private readonly Dictionary<string, RadioVoiceFilter.Profile> _lastRecordProfile = new Dictionary<string, RadioVoiceFilter.Profile>();

        private void ApplyRadioToPlayer(VoicePlayerState player)
        {
            VoicePlayback playback = player.Playback as VoicePlayback;
            if (playback == null)
            {
                if (!_loggedPlaybackTypeOnce)
                {
                    _loggedPlaybackTypeOnce = true;
                    Logger.LogWarning("PRT: player.Playback is not VoicePlayback (type "
                        + (player.Playback == null ? "null" : player.Playback.GetType().FullName) + ")");
                }
                return;
            }

            AudioSource src = playback.AudioSource;
            if (src == null)
            {
                return;
            }

            src.spatialBlend = 0f;
            // The actual receive-volume boost is applied inside RadioVoiceFilter.OnAudioFilterRead
            // instead — Dissonance's own playback component also writes to AudioSource.volume every
            // frame (execution order we don't control), which was silently overwriting this.
            src.volume = 1f;
            src.mute = false;
            src.bypassListenerEffects = true;
            src.bypassReverbZones = true;

            RadioVoiceFilter filter = GetOrAddFilter(player.Name, src.gameObject);

            // Order matters: the Labyrinth profile is an anomaly and must not be reshaped by the
            // family rules, so the character transform runs first and is skipped when it applies.
            RadioProfile profile = ApplyLabyrinthProfile(
                player.Name,
                ApplyInterferenceCharacter(ResolveRemoteRadioTpl(player.Name), GetEffectiveProfile(player.Name)));
            float distance = GetDistanceToPlayer(player.Name);
            RadioVoiceFilter.Mode mode;
            float ratio = 0f;

            if (_txChannel != null && _duplexMode == DuplexMode.HalfDuplex)
            {

                mode = RadioVoiceFilter.Mode.Silent;
            }
            else if (IsLinkJammed(player.Name))
            {
                // Jammed location: pure static regardless of distance or radio tier. The one way
                // through is an Alinco at BOTH ends, which IsLinkJammed accounts for.
                mode = RadioVoiceFilter.Mode.Static;
            }
            else if (distance < 0f || distance <= profile.ZeroNoiseRangeMeters)
            {
                mode = RadioVoiceFilter.Mode.Clear;
                ratio = 0f;
            }
            else if (distance <= profile.ClearRangeMeters)
            {
                mode = RadioVoiceFilter.Mode.Clear;
                ratio = Mathf.Clamp01((distance - profile.ZeroNoiseRangeMeters) / (profile.ClearRangeMeters - profile.ZeroNoiseRangeMeters));
            }
            else if (distance <= profile.NoiseOnlyRangeMeters)
            {
                mode = RadioVoiceFilter.Mode.Static;
            }
            else
            {
                mode = RadioVoiceFilter.Mode.Silent;
            }

            bool combatAmbience = _ambientCombatSoundEnabled.Value
                && mode != RadioVoiceFilter.Mode.Silent
                && IsCombatNearbySpeaker(player.Name);

            float hiddenNoiseAmp = (profile.HiddenNoiseStartMeters > 0f && distance >= profile.HiddenNoiseStartMeters
                && mode != RadioVoiceFilter.Mode.Silent)
                ? profile.HiddenNoiseAmp
                : 0f;

            RadioVoiceFilter.Profile filterProfile = ToFilterProfile(profile);
            filter.SetState(mode, ratio, _noiseVolume.Value, filterProfile, combatAmbience, hiddenNoiseAmp, _receiveVolume.Value);

            _lastRatio[player.Name] = mode == RadioVoiceFilter.Mode.Static ? 1f : ratio;
            _lastRecordProfile[player.Name] = filterProfile;

            if (!_lastMode.TryGetValue(player.Name, out RadioVoiceFilter.Mode prev) || prev != mode)
            {
                _lastMode[player.Name] = mode;
                LogVerbose("PRT: '" + player.Name + "' distance="
                    + (distance < 0f ? "?" : distance.ToString("0")) + "m -> mode=" + mode
                    + " (ratio=" + ratio.ToString("0.00") + ")");
            }
        }

        private void ResetAllRadioAudio()
        {
            foreach (KeyValuePair<string, RadioVoiceFilter> kv in _radioFilters)
            {
                RadioVoiceFilter filter = kv.Value;
                if (filter == null)
                {
                    continue;
                }

                filter.SetState(RadioVoiceFilter.Mode.Passthrough, 0f, _noiseVolume.Value, RadioVoiceFilter.Profile.Default);

                AudioSource src = filter.GetComponent<AudioSource>();
                if (src != null)
                {
                    src.spatialBlend = 1f;
                    src.volume = 1f;
                    src.bypassListenerEffects = false;
                    src.bypassReverbZones = false;
                }
            }
        }

        private RadioVoiceFilter GetOrAddFilter(string profileId, GameObject go)
        {
            if (_radioFilters.TryGetValue(profileId, out RadioVoiceFilter existing) && existing != null && existing.gameObject == go)
            {
                return existing;
            }

            RadioVoiceFilter filter = go.GetComponent<RadioVoiceFilter>();
            if (filter == null)
            {
                filter = go.AddComponent<RadioVoiceFilter>();
            }

            _radioFilters[profileId] = filter;
            return filter;
        }

        private float GetDistanceToPlayer(string profileId)
        {
            Player local = GetLocalPlayer();
            if (local == null)
            {
                return -1f;
            }

            FikaPlayer fp = GetFikaPlayerByProfileId(profileId);
            return fp != null ? Vector3.Distance(local.Position, fp.Position) : -1f;
        }

        private FikaPlayer GetFikaPlayerByProfileId(string profileId)
        {
            if (_coopHandler == null)
            {
                CoopHandler.TryGetCoopHandler(out _coopHandler);
            }

            if (_coopHandler == null)
            {
                return null;
            }

            foreach (FikaPlayer fp in _coopHandler.HumanPlayers)
            {
                if (fp != null && fp.ProfileId == profileId)
                {
                    return fp;
                }
            }

            return null;
        }

        private bool IsCombatNearbySpeaker(string profileId)
        {
            FikaPlayer fp = GetFikaPlayerByProfileId(profileId);
            if (fp == null)
            {
                return false;
            }

            return CombatAmbiencePatch.IsCombatNearby(fp.Position, CombatAmbienceRadiusMeters, CombatAmbienceWindowSeconds);
        }

        private RadioProfile GetEffectiveProfile(string remoteProfileId)
        {
            RadioProfile mine = GetActiveProfile();

            FikaPlayer fp = GetFikaPlayerByProfileId(remoteProfileId);
            if (fp == null || fp.Inventory == null || fp.Inventory.Equipment == null
                || !TryGetAnyActiveRadioProfile(fp.Inventory.Equipment, out RadioProfile theirs))
            {
                return mine;
            }

            RadioProfile combined = mine;
            combined.ZeroNoiseRangeMeters = Mathf.Min(mine.ZeroNoiseRangeMeters, theirs.ZeroNoiseRangeMeters);
            combined.ClearRangeMeters = Mathf.Min(mine.ClearRangeMeters, theirs.ClearRangeMeters);
            combined.NoiseOnlyRangeMeters = Mathf.Min(mine.NoiseOnlyRangeMeters, theirs.NoiseOnlyRangeMeters);

            combined.CarrierHzNear = theirs.CarrierHzNear;
            combined.CarrierHzFar = theirs.CarrierHzFar;
            combined.DriveNear = theirs.DriveNear;
            combined.DriveFar = theirs.DriveFar;

            return combined;
        }

        private bool TryGetAnyActiveRadioProfile(InventoryEquipment eq, out RadioProfile profile)
        {
            profile = BaofengProfile;
            List<string> found = CollectSelectableRadioTpls(eq);
            if (found.Count == 0)
            {
                return false;
            }

            foreach (string tplId in found)
            {
                if (RadioProfiles.TryGetValue(tplId, out RadioProfile foundProfile))
                {
                    profile = foundProfile;
                    return true;
                }
            }

            return true;
        }
    }
}

