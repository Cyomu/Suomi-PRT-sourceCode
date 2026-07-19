using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [BepInPlugin("com.suomi.radiomod", "Suomi-PRT", "0.9.7")]
    [BepInDependency("com.fika.core")]
    public class Plugin : BaseUnityPlugin
    {
        internal const string DisplayVersion = "0.9.7";

        internal const string TestFrequency = "144.500";
        private const float HeartbeatInterval = 3f;

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
        private ConfigEntry<float> _soundVolume;
        private ConfigEntry<float> _receiveVolume;
        private ConfigEntry<float> _noiseVolume;
        private ConfigEntry<bool> _ambientCombatSoundEnabled;
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

        private const float SpeakingHoldSeconds = 0.25f;
        private readonly Dictionary<string, float> _lastOnFreqTime = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> _remoteSpeakingState = new Dictionary<string, bool>();

        private readonly HashSet<string> _remoteStartPlayed = new HashSet<string>();
        private readonly List<RemoteChannel> _speakingChannelsBuffer = new List<RemoteChannel>();
        private readonly List<string> _speakingScratch = new List<string>();

        private readonly Dictionary<string, RadioVoiceFilter> _radioFilters = new Dictionary<string, RadioVoiceFilter>();
        private CoopHandler _coopHandler;

        private AudioSource _audioSource;
        private WavData _onSound;
        private WavData _offSound;

        private WavData _switchModeSound;

        private readonly Dictionary<string, RadioSoundSet> _radioSoundSets = new Dictionary<string, RadioSoundSet>();
        private RadioSoundSet _defaultSoundSet;
        private string _activeRadioTplId;

        private struct WavData
        {
            public float[] Samples;
            public int Channels;
            public int SampleRate;
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
        }

        private sealed class FileLogListener : ILogListener
        {
            private readonly StreamWriter _writer;
            private readonly string _sourceName;

            public FileLogListener(string filePath, string sourceName)
            {
                _sourceName = sourceName;
                _writer = new StreamWriter(filePath, append: false) { AutoFlush = true };
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

        private static ConfigDescription Desc(string text, int order)
        {
            return new ConfigDescription(text, null, new ConfigurationManagerAttributes { Order = order });
        }

        private static ConfigDescription Desc(string text, AcceptableValueBase range, int order)
        {
            return new ConfigDescription(text, range, new ConfigurationManagerAttributes { Order = order });
        }

        private void Awake()
        {
            string logPath = Path.Combine(Path.GetDirectoryName(Info.Location), "prt-fika.log");
            _fileLogListener = new FileLogListener(logPath, Logger.SourceName);
            BepInEx.Logging.Logger.Listeners.Add(_fileLogListener);

            _radioToggleModifier = Config.Bind(
                "1. Hotkeys",
                "Toggle Radio",
                KeyCode.RightControl,
                Desc("Turn the radio on/off (default: K)", 40));

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

            _receiveVolume = Config.Bind(
                "2. Volume",
                "Receive Volume",
                1f,
                Desc("Volume of the other person's voice as heard over the radio", new AcceptableValueRange<float>(0.05f, 1f), 30));

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

            _showNotifications = Config.Bind(
                "3. Radio",
                "Show Notifications",
                true,
                Desc("Show on-screen notifications about radio state (on/off, selection, mode). Custom overlay, no vanilla notification sound.", 20));

            _ambientCombatSoundEnabled = Config.Bind(
                "3. Radio",
                "Ambient Combat Sound (Experimental)",
                true,
                Desc("Simulated background combat sound in the radio channel: if there was recent gunfire near the speaker, "
                    + "a procedural crackle is mixed into their transmission. This is a SIMULATION (proximity gunfire "
                    + "detection), not real ambient audio capture.", 10));

            _indicatorOpacity = Config.Bind(
                "4. Indicators",
                "Indicator Opacity",
                1f,
                Desc("Opacity of the status dots (minimum 5%)", new AcceptableValueRange<float>(0.05f, 1f), 60));

            _showTalkingIndicator = Config.Bind(
                "4. Indicators",
                "Talking Indicator",
                true,
                Desc("Dot that appears only while YOU are transmitting on the radio.", 55));

            _showPowerIndicator = Config.Bind(
                "4. Indicators",
                "Power Indicator",
                true,
                Desc("Small dot showing whether the radio is on (green) or off (gray)", 50));

            _showDuplexIndicator = Config.Bind(
                "4. Indicators",
                "Half-duplex/Duplex Indicator",
                true,
                Desc("Small dot showing the current Half-duplex/Duplex mode. Only visible while the radio is on.", 40));

            _showBusyIndicator = Config.Bind(
                "4. Indicators",
                "Channel Busy Indicator",
                true,
                Desc("Red dot that appears only while someone is transmitting on your frequency.", 20));

            _showSignalIndicator = Config.Bind(
                "4. Indicators",
                "Signal Strength Indicator",
                true,
                Desc("Shows signal quality of the current incoming transmission (see style below). Only appears while you "
                    + "can actually hear someone. Independent toggle from the other indicators.", 10));

            _signalIndicatorStyle = Config.Bind(
                "4. Indicators",
                "Signal Indicator Style",
                SignalIndicatorStyle.Bar,
                Desc("Bar = a single fillable strip. AntennaBars = classic phone-style signal bars.", 5));

            _colorOn = Config.Bind(
                "5. Colors",
                "Radio-On Color",
                Color.green,
                Desc("Color used for the Power indicator (radio on) and the matching notification text", 50));

            _colorSelect = Config.Bind(
                "5. Colors",
                "Radio Selection Color",
                Color.cyan,
                Desc("Color used for radio-selection notification text", 40));

            _colorSimplex = Config.Bind(
                "5. Colors",
                "Duplex Mode Color",
                Color.yellow,
                Desc("Color used for the Half-duplex/Duplex indicator (Duplex) and the matching notification text", 30));

            _colorBusy = Config.Bind(
                "5. Colors",
                "Channel Busy Color",
                Color.red,
                Desc("Color used for the Channel Busy indicator", 20));

            _colorSignalBar = Config.Bind(
                "5. Colors",
                "Signal Bar Fill Color",
                Color.white,
                Desc("Fill color of the signal strength indicator (bar or antenna style)", 10));

            _colorTalking = Config.Bind(
                "5. Colors",
                "Talking Indicator Color",
                new Color(1f, 0.55f, 0f),
                Desc("Color used for the Talking indicator (you are transmitting)", 5));

            _harmony = new Harmony("com.radiomod.client.combatambience");
            _harmony.PatchAll();

            Logger.LogInfo("PRT " + DisplayVersion + " loaded");

            _onSound = LoadWavData("on.wav");
            _offSound = LoadWavData("off.wav");
            _switchModeSound = LoadWavData("swtch.wav");

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

            if (data.Length < 44 || data[0] != 'R' || data[1] != 'I' || data[2] != 'F' || data[3] != 'F')
            {
                Logger.LogWarning("PRT: not a WAV file: " + fileName);
                return false;
            }

            channels = BitConverter.ToInt16(data, 22);
            sampleRate = BitConverter.ToInt32(data, 24);
            short bitsPerSample = BitConverter.ToInt16(data, 34);
            if (bitsPerSample != 16)
            {
                Logger.LogWarning("PRT: only 16-bit PCM is supported, but " + fileName + " is " + bitsPerSample + "-bit");
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
                Logger.LogWarning("PRT: data chunk not found in " + fileName);
                return false;
            }

            int sampleCount = dataSize / 2;
            samples = new float[sampleCount];
            float peak = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float f = BitConverter.ToInt16(data, dataOffset + i * 2) / 32768f;
                samples[i] = f;
                float abs = Mathf.Abs(f);
                if (abs > peak)
                {
                    peak = abs;
                }
            }

            Logger.LogInfo("PRT: WAV loaded from embedded resource: " + fileName
                + " | channels=" + channels + " | rate=" + sampleRate + " | samples=" + sampleCount
                + " | peak=" + peak.ToString("0.00"));
            return true;
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
            Logger.LogInfo("PRT: (re)created " + objName);
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
            Logger.LogInfo("PRT: playing sound " + name + ", volume=" + _soundVolume.Value
                + " | isPlaying=" + source.isPlaying);
        }

        private struct OverlayNotification
        {
            public string Message;
            public Color Color;
            public float StartTime;
            public float ExpireTime;
        }

        private const float NotificationDurationSeconds = 2.5f;
        private const float NotificationFadeSeconds = 0.5f;
        private const float NotificationSlideInSeconds = 0.25f;

        private OverlayNotification? _currentNotification;
        private GUIStyle _notificationStyle;

        private static string GetLanguageCode()
        {
            try
            {
                string code = LocaleManagerClass.LocaleManagerClass?.String_1;
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

        private static string L(string ru, string en, string ge, string es, string fr, string pl, string it, string cz)
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

        private void Notify(string message, Color? textColor = null)
        {
            Logger.LogInfo("PRT: notification shown");
            if (!_showNotifications.Value)
            {
                return;
            }

            float now = Time.time;
            _currentNotification = new OverlayNotification
            {
                Message = message,
                Color = textColor ?? Color.white,
                StartTime = now,
                ExpireTime = now + NotificationDurationSeconds
            };
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

        private void DrawDot(Texture2D dot, float x, float y, float diameter, Color color)
        {
            color.a *= _indicatorOpacity.Value;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, diameter, diameter), dot);
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
                GInterface495<EEftScreenType> controller = CurrentScreenSingletonClass.Instance?.CurrentBaseScreenController;
                return controller != null && controller.ScreenType == EEftScreenType.Settings;
            }
            catch
            {
                return false;
            }
        }

        private void DrawIndicators()
        {

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

            if (rowCount > 0)
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

            GUI.color = prevGuiColor;
        }

        private void DrawSignalFillBar(float rightEdge, float rowY, float rowGap, float fill)
        {
            const float barWidth = 40f;
            const float barHeight = 4f;
            float barY = rowY - barHeight - rowGap;
            float barX = rightEdge - barWidth;

            GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.6f * _indicatorOpacity.Value);
            GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

            Color fillColor = _colorSignalBar.Value;
            fillColor.a *= _indicatorOpacity.Value;
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(barX, barY, barWidth * fill, barHeight), Texture2D.whiteTexture);
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

            Color emptyColor = new Color(0.4f, 0.4f, 0.4f, 0.5f * _indicatorOpacity.Value);
            Color filledColor = _colorSignalBar.Value;
            filledColor.a *= _indicatorOpacity.Value;

            for (int i = 0; i < barCount; i++)
            {
                float barHeight = AntennaBarHeights[i];
                float barX = startX + i * (barWidth + barGap);
                float barY = baseline - barHeight;

                GUI.color = i < filledCount ? filledColor : emptyColor;
                GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);
            }
        }

        private void OnGUI()
        {

            if (GetLocalPlayer() == null)
            {
                return;
            }

            DrawIndicators();
            DrawNotification();
        }

        private void DrawNotification()
        {
            if (_currentNotification == null)
            {
                return;
            }

            OverlayNotification n = _currentNotification.Value;
            float now = Time.time;
            float remaining = n.ExpireTime - now;
            if (remaining <= 0f)
            {
                _currentNotification = null;
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
            }

            float elapsed = now - n.StartTime;
            float slideT = Mathf.Clamp01(elapsed / NotificationSlideInSeconds);
            slideT = slideT * slideT * (3f - 2f * slideT);

            float fadeOut = remaining < NotificationFadeSeconds ? remaining / NotificationFadeSeconds : 1f;
            float alpha = Mathf.Min(slideT, fadeOut);

            const float boxWidth = 340f;
            const float lineHeight = 22f;
            const float paddingH = 10f;
            const float paddingV = 6f;
            const float accentWidth = 3f;
            const float marginRight = 20f;
            const float marginBottom = 120f;

            float boxHeight = lineHeight + paddingV * 2f;
            float targetX = Screen.width - boxWidth - marginRight;
            float startX = Screen.width + 20f;
            float x = Mathf.Lerp(startX, targetX, slideT);
            float y = Screen.height - marginBottom - boxHeight;

            Color prevGuiColor = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.55f * alpha);
            GUI.DrawTexture(new Rect(x, y, boxWidth, boxHeight), Texture2D.whiteTexture);

            Color accentColor = n.Color;
            accentColor.a = alpha;
            GUI.color = accentColor;
            GUI.DrawTexture(new Rect(x, y, accentWidth, boxHeight), Texture2D.whiteTexture);

            GUI.color = prevGuiColor;

            Rect textRect = new Rect(x + accentWidth + paddingH, y + paddingV, boxWidth - accentWidth - paddingH * 2f, lineHeight);

            Color textColor = n.Color;
            textColor.a = alpha;

            Rect shadowRect = new Rect(textRect.x + 1f, textRect.y + 1f, textRect.width, textRect.height);
            Color shadowColor = Color.black;
            shadowColor.a = alpha * 0.7f;
            _notificationStyle.normal.textColor = shadowColor;
            GUI.Label(shadowRect, n.Message, _notificationStyle);

            _notificationStyle.normal.textColor = textColor;
            GUI.Label(textRect, n.Message, _notificationStyle);
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

        private bool IsVanillaVoipActive()
        {
            Player local = GetLocalPlayer();
            return local != null && local.VoipController != null
                && local.VoipController.Status.Value == EVoipControllerStatus.Talking;
        }

        private void Update()
        {
            DissonanceComms comms = DissonanceComms.Instance;

            if (Time.time >= _nextHeartbeat)
            {
                _nextHeartbeat = Time.time + HeartbeatInterval;
                Logger.LogInfo("PRT: heartbeat, DissonanceComms.Instance = " + (comms == null ? "null" : "OK")
                    + ", radioOn = " + _radioOn + ", radioLocation = " + _radioLocation);
            }

            if (comms == null)
            {
                _wasVanillaTalking = false;
                return;
            }

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
                    "Radio spenta (non nell'equipaggiamento)", "Vysílačka vypnuta (není ve výbavě)"), null);
            }

            SetMicMuted(_radioOn && _radioLocation == RadioLocation.Backpack);
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

            if (_selectedRadioTplId == null || !existsAnywhere.Contains(_selectedRadioTplId))
            {
                List<string> selectable = CollectSelectableRadioTpls(eq);
                if (selectable.Count == 0)
                {

                    _selectedRadioTplId = null;
                    return RadioLocation.None;
                }

                _selectedRadioTplId = selectable[0];
                if (selectable.Count > 1)
                {
                    Notify(L("Выбрана рация: ", "Radio selected: ",
                        "Funkgerät ausgewählt: ", "Radio seleccionada: ", "Radio sélectionnée : ",
                        "Wybrano radiotelefon: ", "Radio selezionata: ", "Vybrána vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId)
                        + L(" (R.Shift+VOIP — сменить)", " (R.Shift+VOIP to switch)",
                            " (R.Shift+VOIP zum Wechseln)", " (R.Shift+VOIP para cambiar)",
                            " (R.Shift+VOIP pour changer)", " (R.Shift+VOIP, aby zmienić)",
                            " (R.Shift+VOIP per cambiare)", " (R.Shift+VOIP pro přepnutí)"), _colorSelect.Value);
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
                    "Nessun accesso all'inventario", "Žádný přístup k inventáři"), null);
                return;
            }

            List<string> available = CollectSelectableRadioTpls(local.Inventory.Equipment);
            if (available.Count == 0)
            {
                Notify(L("Нет рации в разгрузке/кармане для выбора", "No radio in vest/pocket to select",
                    "Kein Funkgerät in Weste/Tasche zum Auswählen", "No hay radio en el chaleco/bolsillo para seleccionar",
                    "Aucune radio dans le gilet/la poche à sélectionner", "Brak radiotelefonu w kamizelce/kieszeni do wyboru",
                    "Nessuna radio nel gilet/tasca da selezionare", "Žádná vysílačka ve vestě/kapse k výběru"), null);
                return;
            }

            if (available.Count == 1)
            {
                _selectedRadioTplId = available[0];
                Notify(L("Только одна рация доступна: ", "Only one radio available: ",
                    "Nur ein Funkgerät verfügbar: ", "Solo hay una radio disponible: ",
                    "Une seule radio disponible : ", "Dostępny tylko jeden radiotelefon: ",
                    "È disponibile solo una radio: ", "K dispozici je pouze jedna vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId), _colorSelect.Value);
                _radioLocation = GetRadioLocation();
                return;
            }

            int currentIndex = _selectedRadioTplId != null ? available.IndexOf(_selectedRadioTplId) : -1;
            int nextIndex = (currentIndex + 1) % available.Count;
            _selectedRadioTplId = available[nextIndex];
            Notify(L("Выбрана рация: ", "Radio selected: ",
                        "Funkgerät ausgewählt: ", "Radio seleccionada: ", "Radio sélectionnée : ",
                        "Wybrano radiotelefon: ", "Radio selezionata: ", "Vybrána vysílačka: ") + GetRadioDisplayName(_selectedRadioTplId)
                + " (" + (nextIndex + 1) + "/" + available.Count + ")", _colorSelect.Value);

            _radioLocation = GetRadioLocation();
        }

        private static string GetRadioDisplayName(string tplId)
        {
            if (tplId != null && RadioDisplayNames.TryGetValue(tplId, out string name))
            {
                return name;
            }

            return "неизвестная рация";
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
                    Logger.LogInfo("PRT DEBUG: slot " + slotType + " is empty (ContainedItem == null)");
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
                Logger.LogInfo("PRT DEBUG: slot " + slotType + " contains " + contained.StringTemplateId
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
                            "Accendi prima la radio", "Nejprve zapněte vysílačku"), null);
                        return;
                    }

                    if (_activeRadioTplId == null || !SimplexCapableTplIds.Contains(_activeRadioTplId))
                    {
                        Notify(L("Переключение режима недоступно.", "Mode switching unavailable.",
                            "Moduswechsel nicht verfügbar.", "Cambio de modo no disponible.",
                            "Changement de mode indisponible.", "Zmiana trybu niedostępna.",
                            "Cambio modalità non disponibile.", "Přepnutí režimu není k dispozici."), null);
                        return;
                    }

                    if (_radioLocation == RadioLocation.Backpack)
                    {
                        Notify(L("Рация в рюкзаке — нет доступа", "Radio is in backpack — no access",
                            "Funkgerät ist im Rucksack — kein Zugriff", "La radio está en la mochila — sin acceso",
                            "La radio est dans le sac à dos — aucun accès", "Radiotelefon jest w plecaku — brak dostępu",
                            "La radio è nello zaino — nessun accesso", "Vysílačka je v batohu — žádný přístup"), null);
                        return;
                    }

                    _duplexMode = _duplexMode == DuplexMode.HalfDuplex ? DuplexMode.Simplex : DuplexMode.HalfDuplex;
                    Notify(_duplexMode == DuplexMode.Simplex
                        ? L("Режим: Дуплекс", "Mode: Duplex", "Modus: Duplex", "Modo: Dúplex", "Mode : Duplex", "Tryb: Dupleks", "Modalità: Duplex", "Režim: Duplex")
                        : L("Режим: Полудуплекс", "Mode: Half-duplex", "Modus: Halbduplex", "Modo: Semidúplex", "Mode : Semi-duplex", "Tryb: Półdupleks", "Modalità: Semiduplex", "Režim: Poloduplexní"),
                        _duplexMode == DuplexMode.Simplex ? _colorSimplex.Value : (Color?)null);
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
                            "Nessuna radio nell'equipaggiamento", "Žádná vysílačka ve výbavě"), null);
                        return;
                    }

                    if (_radioLocation == RadioLocation.Backpack)
                    {
                        Notify(L("Рация в рюкзаке — нет доступа", "Radio is in backpack — no access",
                            "Funkgerät ist im Rucksack — kein Zugriff", "La radio está en la mochila — sin acceso",
                            "La radio est dans le sac à dos — aucun accès", "Radiotelefon jest w plecaku — brak dostępu",
                            "La radio è nello zaino — nessun accesso", "Vysílačka je v batohu — žádný přístup"), null);
                        return;
                    }

                    _radioOn = !_radioOn;
                    ApplyReceiveState(comms);
                    Notify(_radioOn
                        ? L("Рация включена", "Radio on", "Funkgerät an", "Radio encendida", "Radio allumée", "Radiotelefon włączony", "Radio accesa", "Vysílačka zapnuta")
                        : L("Рация выключена", "Radio off", "Funkgerät aus", "Radio apagada", "Radio éteinte", "Radiotelefon wyłączony", "Radio spenta", "Vysílačka vypnuta"),
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
            Logger.LogInfo("PRT: microphone " + (muted ? "muted (receive only)" : "unmuted"));
        }

        private void StartRadioTransmit(DissonanceComms comms)
        {
            _txChannel = comms.RoomChannels.Open(TestFrequency, positional: false);
            PlayClip(GetActiveSoundSet().LocalStart, "local_start");
            Logger.LogInfo("PRT: transmitting on " + TestFrequency + " (vanilla VOIP + radio)");
        }

        private void StopRadioTransmit(DissonanceComms comms)
        {
            if (_txChannel != null)
            {
                comms.RoomChannels.Close(_txChannel.Value);
                _txChannel = null;
            }

            PlayClip(GetActiveSoundSet().LocalEnd, "local_end");
            Logger.LogInfo("PRT: transmission ended");
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
                Logger.LogInfo("PRT: subscribed to receive on frequency " + TestFrequency);
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
                }
                else if (!active && wasSpeaking)
                {
                    RadioSpeakerNames.Remove(kv.Key);

                    if (_remoteStartPlayed.Remove(kv.Key))
                    {
                        PlayClip(GetActiveSoundSet().RemoteEnd, "remote_end");
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
            src.volume = _receiveVolume.Value;
            src.mute = false;
            src.bypassListenerEffects = true;
            src.bypassReverbZones = true;

            RadioVoiceFilter filter = GetOrAddFilter(player.Name, src.gameObject);

            RadioProfile profile = GetEffectiveProfile(player.Name);
            float distance = GetDistanceToPlayer(player.Name);
            RadioVoiceFilter.Mode mode;
            float ratio = 0f;

            if (_txChannel != null && _duplexMode == DuplexMode.HalfDuplex)
            {

                mode = RadioVoiceFilter.Mode.Silent;
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

            filter.SetState(mode, ratio, _noiseVolume.Value, ToFilterProfile(profile), combatAmbience, hiddenNoiseAmp);

            _lastRatio[player.Name] = mode == RadioVoiceFilter.Mode.Static ? 1f : ratio;

            if (!_lastMode.TryGetValue(player.Name, out RadioVoiceFilter.Mode prev) || prev != mode)
            {
                _lastMode[player.Name] = mode;
                Logger.LogInfo("PRT: '" + player.Name + "' distance="
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

