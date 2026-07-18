using System;
using UnityEngine;
namespace RadioMod.Client
{
    public class RadioVoiceFilter : MonoBehaviour
    {
        public enum Mode { Passthrough, Clear, Static, Silent }
        public struct Profile
        {
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
            public static readonly Profile Default = new Profile
            {
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
        }
        private volatile int _mode = (int)Mode.Passthrough;
        private volatile float _ratio;
        private volatile float _noiseScale = 1f;
        private Profile _profile = Profile.Default;
        private readonly object _profileLock = new object();
        private int _sampleRate = 48000;
        private float[] _lpState;
        private float[] _hpPrevIn;
        private float[] _hpPrevOut;
        private float[] _noiseLp;
        private double _phase;
        private readonly System.Random _rnd = new System.Random();
        private bool _inDropout;
        private int _dropoutSamplesRemaining;
        private int _dropoutRerollCountdown;
        private volatile bool _combatAmbience;
        private bool _inCombatBurst;
        private int _combatBurstSamplesRemaining;
        private int _combatBurstRerollCountdown;
        private float _combatBurstLp;
        private volatile float _hiddenNoiseAmp;
        private float[] _hiddenNoiseLp;
        private void Awake()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            if (_sampleRate <= 0)
            {
                _sampleRate = 48000;
            }
        }
        public void SetState(Mode mode, float ratio, float noiseScale, Profile profile, bool combatAmbience = false, float hiddenNoiseAmp = 0f)
        {
            _mode = (int)mode;
            _ratio = Mathf.Clamp01(ratio);
            _noiseScale = Mathf.Clamp01(noiseScale);
            _combatAmbience = combatAmbience;
            _hiddenNoiseAmp = Mathf.Max(0f, hiddenNoiseAmp);
            lock (_profileLock)
            {
                _profile = profile;
            }
        }
        private void OnAudioFilterRead(float[] data, int channels)
        {
            int mode = _mode;
            if (mode == (int)Mode.Passthrough)
            {
                return;
            }
            if (mode == (int)Mode.Silent)
            {
                Array.Clear(data, 0, data.Length);
                return;
            }
            if (_lpState == null || _lpState.Length < channels)
            {
                _lpState = new float[channels];
                _hpPrevIn = new float[channels];
                _hpPrevOut = new float[channels];
                _noiseLp = new float[channels];
                _hiddenNoiseLp = new float[channels];
            }
            Profile p;
            lock (_profileLock)
            {
                p = _profile;
            }
            float r = _ratio;
            bool staticMode = mode == (int)Mode.Static;
            float lpCutoff = staticMode ? p.StaticLpCutoff : Mathf.Lerp(p.LpCutoffNear, p.LpCutoffFar, r);
            float lpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * lpCutoff / _sampleRate);
            float hpCoef = Mathf.Exp(-2f * Mathf.PI * p.HpCutoffHz / _sampleRate);
            float drive = staticMode ? p.StaticDrive : Mathf.Lerp(p.DriveNear, p.DriveFar, r);
            float carrierHz = Mathf.Lerp(p.CarrierHzNear, p.CarrierHzFar, r);
            float ringMix = staticMode ? p.StaticRingMix : Mathf.Lerp(p.RingMixNear, p.RingMixFar, r);
            float noiseAmp = (staticMode ? p.StaticNoiseAmp : Mathf.Lerp(p.NoiseAmpNear, p.NoiseAmpFar, r)) * _noiseScale;
            float noiseLpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * p.NoiseLpCutoffHz / _sampleRate);
            float voiceGain = staticMode ? p.StaticVoiceGain : Mathf.Lerp(p.VoiceGainNear, p.VoiceGainFar, r);
            float dropoutChance = staticMode ? p.StaticDropoutChance : Mathf.Lerp(p.DropoutChanceNear, p.DropoutChanceFar, r);
            int rerollWindowSamples = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * 0.03f));
            bool combatAmbience = _combatAmbience;
            int combatRerollWindowSamples = Mathf.Max(1, Mathf.RoundToInt(_sampleRate * 0.15f));
            float combatBurstLpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * 3000f / _sampleRate);
            float hiddenNoiseAmp = _hiddenNoiseAmp * _noiseScale;
            float hiddenNoiseLpCoef = 1f - Mathf.Exp(-2f * Mathf.PI * 1200f / _sampleRate);
            int frames = data.Length / channels;
            for (int i = 0; i < frames; i++)
            {
                float carrier = Mathf.Sin((float)(2.0 * Math.PI * carrierHz * (_phase / _sampleRate)));
                if (_dropoutSamplesRemaining > 0)
                {
                    _dropoutSamplesRemaining--;
                }
                else
                {
                    _inDropout = false;
                }
                if (--_dropoutRerollCountdown <= 0)
                {
                    _dropoutRerollCountdown = rerollWindowSamples;
                    if (!_inDropout && dropoutChance > 0f && _rnd.NextDouble() < dropoutChance)
                    {
                        _inDropout = true;
                        _dropoutSamplesRemaining = Mathf.RoundToInt(_sampleRate * (float)(0.02 + _rnd.NextDouble() * 0.13));
                    }
                }
                float dropoutGain = _inDropout ? 0.08f : 1f;
                if (_combatBurstSamplesRemaining > 0)
                {
                    _combatBurstSamplesRemaining--;
                }
                else
                {
                    _inCombatBurst = false;
                }
                if (--_combatBurstRerollCountdown <= 0)
                {
                    _combatBurstRerollCountdown = combatRerollWindowSamples;
                    if (combatAmbience && !_inCombatBurst && _rnd.NextDouble() < 0.3)
                    {
                        _inCombatBurst = true;
                        _combatBurstSamplesRemaining = Mathf.RoundToInt(_sampleRate * (float)(0.025 + _rnd.NextDouble() * 0.045));
                    }
                }
                float combatBurstSample = 0f;
                if (_inCombatBurst)
                {
                    float white = (float)(_rnd.NextDouble() * 2.0 - 1.0);
                    _combatBurstLp += combatBurstLpCoef * (white - _combatBurstLp);
                    combatBurstSample = _combatBurstLp * 0.4f;
                }
                for (int ch = 0; ch < channels; ch++)
                {
                    int idx = i * channels + ch;
                    float x = data[idx];
                    _lpState[ch] += lpCoef * (x - _lpState[ch]);
                    float lp = _lpState[ch];
                    float hp = hpCoef * (_hpPrevOut[ch] + lp - _hpPrevIn[ch]);
                    _hpPrevIn[ch] = lp;
                    _hpPrevOut[ch] = hp;
                    float v = Mathf.Lerp(hp, hp * carrier, ringMix);
                    v = (float)Math.Tanh(v * drive) * voiceGain * dropoutGain;
                    float white2 = (float)(_rnd.NextDouble() * 2.0 - 1.0);
                    _noiseLp[ch] += noiseLpCoef * (white2 - _noiseLp[ch]);
                    v += _noiseLp[ch] * noiseAmp;
                    v += combatBurstSample;
                    if (hiddenNoiseAmp > 0f)
                    {
                        float hiddenWhite = (float)(_rnd.NextDouble() * 2.0 - 1.0);
                        _hiddenNoiseLp[ch] += hiddenNoiseLpCoef * (hiddenWhite - _hiddenNoiseLp[ch]);
                        v += _hiddenNoiseLp[ch] * hiddenNoiseAmp;
                    }
                    data[idx] = Mathf.Clamp(v, -1f, 1f);
                }
                _phase += 1.0;
                if (_phase >= _sampleRate)
                {
                    _phase -= _sampleRate;
                }
            }
        }
    }
}
