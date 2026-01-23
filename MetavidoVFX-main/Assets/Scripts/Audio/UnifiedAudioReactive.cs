// UnifiedAudioReactive.cs - Unified 8-band audio reactive system
// Consolidates AudioBridge + BrushAudioReactive into single FFT pipeline
// Based on keijiro's Reaktion pattern with 8 logarithmic frequency bands
//
// ARCHITECTURE:
// - Single FFT computation shared by ALL consumers (VFX, Brushes, etc.)
// - 8 logarithmic frequency bands (like Reaktion/Open Brush)
// - Beat detection with spectral flux algorithm
// - Global shader properties for VFX Graph access
// - Direct API for brush modulation
// - Auto-binding support for runtime VFX spawning

using UnityEngine;
using System;
using System.Collections;

namespace XRRAI.Audio
{
    /// <summary>
    /// Unified audio reactive system providing 8-band FFT analysis for all consumers.
    /// Replaces separate AudioBridge + BrushAudioReactive with single shared pipeline.
    /// </summary>
    public class UnifiedAudioReactive : MonoBehaviour
    {
        public static UnifiedAudioReactive Instance { get; private set; }

        #region Configuration

        public enum AudioInputMode { AudioClip, Microphone }

        [Header("Audio Input")]
        [SerializeField] AudioInputMode _inputMode = AudioInputMode.AudioClip;
        [SerializeField] AudioSource _audioSource;
        [SerializeField] string _microphoneDevice = "";

        [Header("FFT Analysis")]
        [Tooltip("FFT sample count (power of 2). Higher = more precision, more CPU.")]
        [Range(512, 8192)]
        [SerializeField] int _sampleCount = 1024;
        [SerializeField] FFTWindow _fftWindow = FFTWindow.BlackmanHarris;

        [Header("8-Band Configuration")]
        [Tooltip("Sensitivity multiplier for all bands")]
        [Range(0.1f, 10f)]
        [SerializeField] float _sensitivity = 1f;

        [Tooltip("Smoothing factor (0 = instant, 1 = very smooth)")]
        [Range(0f, 0.99f)]
        [SerializeField] float _smoothing = 0.8f;

        [Header("Beat Detection")]
        [SerializeField] bool _enableBeatDetection = true;
        [Tooltip("Frames of flux history for adaptive threshold (~1 second at 60fps)")]
        [Range(15, 120)]
        [SerializeField] int _beatHistorySize = 43;
        [Range(1f, 3f)]
        [SerializeField] float _beatThreshold = 1.5f;
        [Range(0.05f, 0.5f)]
        [SerializeField] float _pulseDecayTime = 0.1f;
        [Range(0.05f, 0.5f)]
        [SerializeField] float _minBeatInterval = 0.1f;

        [Header("Audio Data Texture")]
        [Tooltip("Enable 4x2 texture output for VFX without exposed properties")]
        [SerializeField] bool _enableAudioTexture = true;

        [Header("Legacy Compatibility")]
        [Tooltip("Also set legacy 4-band global properties (_AudioBands)")]
        [SerializeField] bool _enableLegacy4Band = true;

        [Header("Debug")]
        [SerializeField] bool _verboseLogging = false;

        #endregion

        #region Runtime Data

        // FFT data (single allocation, reused)
        float[] _samples;
        float[] _spectrum;

        // 8 frequency bands (logarithmic distribution)
        float[] _rawBands = new float[8];
        float[] _smoothedBands = new float[8];

        // Legacy 4-band mapping
        float _subBass, _bass, _mids, _treble;

        // Volume/RMS
        float _rawVolume;
        float _smoothedVolume;

        // Peak tracking
        float _peakLevel;
        const float PeakDecay = 0.995f;

        // Beat detection
        BeatDetector _beatDetector;
        float _beatPulse;
        float _beatIntensity;
        bool _isOnset;
        float _timeSinceLastBeat = 999f;

        // Audio texture (4x2 = 8 bands + metadata)
        Texture2D _audioTexture;
        Color[] _audioPixels;

        // Microphone state
        bool _microphoneActive;
        string _activeMicDevice;
        Coroutine _micInitCoroutine;

        // 8-band frequency boundaries (logarithmic, in FFT bins for 1024@44100Hz)
        // Band 0: ~0-86Hz (Sub-bass)
        // Band 1: ~86-172Hz (Bass)
        // Band 2: ~172-344Hz (Low-mids)
        // Band 3: ~344-689Hz (Mids)
        // Band 4: ~689-1378Hz (High-mids)
        // Band 5: ~1378-2756Hz (Presence)
        // Band 6: ~2756-5512Hz (Brilliance)
        // Band 7: ~5512-11025Hz (Air)
        static readonly int[] BandBoundaries = { 2, 4, 8, 16, 32, 64, 128, 256 };

        #endregion

        #region Shader Property IDs

        // 8-band properties
        static readonly int _AudioBand0ID = Shader.PropertyToID("_AudioBand0");
        static readonly int _AudioBand1ID = Shader.PropertyToID("_AudioBand1");
        static readonly int _AudioBand2ID = Shader.PropertyToID("_AudioBand2");
        static readonly int _AudioBand3ID = Shader.PropertyToID("_AudioBand3");
        static readonly int _AudioBand4ID = Shader.PropertyToID("_AudioBand4");
        static readonly int _AudioBand5ID = Shader.PropertyToID("_AudioBand5");
        static readonly int _AudioBand6ID = Shader.PropertyToID("_AudioBand6");
        static readonly int _AudioBand7ID = Shader.PropertyToID("_AudioBand7");
        static readonly int _AudioBands8ID = Shader.PropertyToID("_AudioBands8"); // Vector4 x2

        // Legacy 4-band
        static readonly int _AudioBandsID = Shader.PropertyToID("_AudioBands");

        // Volume and beat
        static readonly int _AudioVolumeID = Shader.PropertyToID("_AudioVolume");
        static readonly int _AudioPeakID = Shader.PropertyToID("_AudioPeak");
        static readonly int _BeatPulseID = Shader.PropertyToID("_BeatPulse");
        static readonly int _BeatIntensityID = Shader.PropertyToID("_BeatIntensity");

        #endregion

        #region Public Properties

        // 8 frequency bands (0-1 range)
        public float Band0 => _smoothedBands[0]; // Sub-bass
        public float Band1 => _smoothedBands[1]; // Bass
        public float Band2 => _smoothedBands[2]; // Low-mids
        public float Band3 => _smoothedBands[3]; // Mids
        public float Band4 => _smoothedBands[4]; // High-mids
        public float Band5 => _smoothedBands[5]; // Presence
        public float Band6 => _smoothedBands[6]; // Brilliance
        public float Band7 => _smoothedBands[7]; // Air

        /// <summary>All 8 bands as array (read-only copy)</summary>
        public float[] Bands => (float[])_smoothedBands.Clone();

        /// <summary>Direct reference to bands (no allocation, read-only)</summary>
        public ReadOnlySpan<float> BandsDirect => _smoothedBands;

        // Legacy 4-band compatibility
        public float SubBass => _subBass;
        public float Bass => _bass;
        public float Mids => _mids;
        public float Treble => _treble;

        // Volume
        public float Volume => _smoothedVolume;
        public float RawVolume => _rawVolume;
        public float Peak => _peakLevel;

        // Beat detection
        public float BeatPulse => _beatPulse;
        public float BeatIntensity => _beatIntensity;
        public bool IsOnset => _isOnset;
        public float TimeSinceLastBeat => _timeSinceLastBeat;
        public bool BeatDetectionEnabled => _enableBeatDetection;

        // Audio texture
        public Texture2D AudioTexture => _audioTexture;

        // Input state
        public AudioInputMode InputMode => _inputMode;
        public bool IsMicrophoneActive => _microphoneActive;
        public AudioSource AudioSource => _audioSource;

        #endregion

        #region Events

        /// <summary>Fired when a beat is detected</summary>
        public event Action<float> OnBeat;

        /// <summary>Fired every frame with current audio data</summary>
        public event Action<float[], float, float> OnAudioUpdate; // bands, volume, beatPulse

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UnifiedAudioReactive] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            InitializeAudio();
        }

        void Update()
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                SetZeroOutput();
                return;
            }

            ProcessAudio();
            UpdateBeatDetection();
            UpdateShaderProperties();
            UpdateAudioTexture();

            // Fire event
            OnAudioUpdate?.Invoke(_smoothedBands, _smoothedVolume, _beatPulse);
        }

        void OnDestroy()
        {
            CleanupMicrophone();
            CleanupTexture();

            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Initialization

        void InitializeAudio()
        {
            _samples = new float[_sampleCount];
            _spectrum = new float[_sampleCount];

            // Initialize beat detector
            int sampleRate = AudioSettings.outputSampleRate;
            _beatDetector = new BeatDetector(
                _sampleCount,
                sampleRate,
                _beatHistorySize,
                _beatThreshold,
                _pulseDecayTime
            );
            _beatDetector.MinBeatInterval = _minBeatInterval;

            // Initialize audio texture (4x2 for 8 bands + metadata)
            if (_enableAudioTexture)
            {
                _audioTexture = new Texture2D(4, 2, TextureFormat.RGBAFloat, false, true)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "UnifiedAudioTexture"
                };
                _audioPixels = new Color[8];
            }

            // Setup audio input
            if (_inputMode == AudioInputMode.Microphone)
            {
                _micInitCoroutine = StartCoroutine(InitializeMicrophoneAsync());
            }
            else
            {
                _audioSource ??= GetComponent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
            }

            if (_verboseLogging)
                Debug.Log($"[UnifiedAudioReactive] Initialized: {_sampleCount} samples, {sampleRate}Hz, mode: {_inputMode}");
        }

        IEnumerator InitializeMicrophoneAsync()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[UnifiedAudioReactive] No microphone found, falling back to AudioClip");
                _inputMode = AudioInputMode.AudioClip;
                _audioSource ??= GetComponent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
                yield break;
            }

            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();

            _activeMicDevice = string.IsNullOrEmpty(_microphoneDevice) ? null : _microphoneDevice;
            _audioSource.clip = Microphone.Start(_activeMicDevice, true, 1, 44100);
            _audioSource.loop = true;

            float timeout = Time.realtimeSinceStartup + 3f;
            while (Microphone.GetPosition(_activeMicDevice) <= 0)
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    Debug.LogWarning("[UnifiedAudioReactive] Microphone timeout");
                    _inputMode = AudioInputMode.AudioClip;
                    yield break;
                }
                yield return null;
            }

            _audioSource.Play();
            _audioSource.volume = 0f; // Mute playback
            _microphoneActive = true;

            Debug.Log($"[UnifiedAudioReactive] Microphone active: {_activeMicDevice ?? Microphone.devices[0]}");
        }

        #endregion

        #region Audio Processing

        void ProcessAudio()
        {
            // Get raw samples for RMS volume
            _audioSource.GetOutputData(_samples, 0);
            _rawVolume = CalculateRMS(_samples) * _sensitivity;
            _smoothedVolume = Mathf.Lerp(_rawVolume, _smoothedVolume, _smoothing);

            // Update peak with decay
            if (_rawVolume > _peakLevel)
                _peakLevel = _rawVolume;
            else
                _peakLevel *= PeakDecay;

            // Get spectrum for frequency bands
            _audioSource.GetSpectrumData(_spectrum, 0, _fftWindow);

            // Calculate 8 logarithmic bands
            int startBin = 0;
            for (int band = 0; band < 8; band++)
            {
                int endBin = Mathf.Min(BandBoundaries[band], _spectrum.Length - 1);
                float sum = 0f;
                int count = 0;

                for (int i = startBin; i <= endBin; i++)
                {
                    sum += _spectrum[i];
                    count++;
                }

                _rawBands[band] = count > 0 ? (sum / count) * _sensitivity : 0f;
                _smoothedBands[band] = Mathf.Lerp(_rawBands[band], _smoothedBands[band], _smoothing);

                startBin = endBin + 1;
            }

            // Map to legacy 4-band for compatibility
            if (_enableLegacy4Band)
            {
                _subBass = _smoothedBands[0];
                _bass = (_smoothedBands[0] + _smoothedBands[1]) * 0.5f;
                _mids = (_smoothedBands[2] + _smoothedBands[3] + _smoothedBands[4]) / 3f;
                _treble = (_smoothedBands[5] + _smoothedBands[6] + _smoothedBands[7]) / 3f;
            }
        }

        float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            return Mathf.Sqrt(sum / samples.Length);
        }

        void UpdateBeatDetection()
        {
            if (!_enableBeatDetection || _beatDetector == null)
            {
                _beatPulse = 0f;
                _beatIntensity = 0f;
                _isOnset = false;
                return;
            }

            // Update detector params if changed in inspector
            _beatDetector.ThresholdMultiplier = _beatThreshold;
            _beatDetector.PulseDecayTime = _pulseDecayTime;
            _beatDetector.MinBeatInterval = _minBeatInterval;

            // Process
            _beatDetector.Process(_spectrum);

            _beatPulse = _beatDetector.BeatPulse;
            _beatIntensity = _beatDetector.BeatIntensity;
            _isOnset = _beatDetector.IsOnset;
            _timeSinceLastBeat = _beatDetector.TimeSinceLastBeat;

            // Fire beat event
            if (_isOnset)
                OnBeat?.Invoke(_beatIntensity);
        }

        #endregion

        #region Shader Properties

        void UpdateShaderProperties()
        {
            // 8 individual band properties
            Shader.SetGlobalFloat(_AudioBand0ID, _smoothedBands[0]);
            Shader.SetGlobalFloat(_AudioBand1ID, _smoothedBands[1]);
            Shader.SetGlobalFloat(_AudioBand2ID, _smoothedBands[2]);
            Shader.SetGlobalFloat(_AudioBand3ID, _smoothedBands[3]);
            Shader.SetGlobalFloat(_AudioBand4ID, _smoothedBands[4]);
            Shader.SetGlobalFloat(_AudioBand5ID, _smoothedBands[5]);
            Shader.SetGlobalFloat(_AudioBand6ID, _smoothedBands[6]);
            Shader.SetGlobalFloat(_AudioBand7ID, _smoothedBands[7]);

            // Volume and beat
            Shader.SetGlobalFloat(_AudioVolumeID, _smoothedVolume);
            Shader.SetGlobalFloat(_AudioPeakID, _peakLevel);
            Shader.SetGlobalFloat(_BeatPulseID, _beatPulse);
            Shader.SetGlobalFloat(_BeatIntensityID, _beatIntensity);

            // Legacy 4-band (scaled by 100 for compatibility)
            if (_enableLegacy4Band)
            {
                Shader.SetGlobalVector(_AudioBandsID, new Vector4(_bass, _mids, _treble, _subBass) * 100f);
            }
        }

        void SetZeroOutput()
        {
            for (int i = 0; i < 8; i++)
            {
                _rawBands[i] = 0f;
                _smoothedBands[i] = 0f;
            }
            _rawVolume = _smoothedVolume = 0f;
            _beatPulse = _beatIntensity = 0f;
            _isOnset = false;
            _subBass = _bass = _mids = _treble = 0f;

            _beatDetector?.Reset();
            UpdateShaderProperties();
        }

        #endregion

        #region Audio Texture

        void UpdateAudioTexture()
        {
            if (!_enableAudioTexture || _audioTexture == null) return;

            // Row 0: Bands 0-3
            _audioPixels[0] = new Color(_smoothedBands[0], _smoothedBands[1], _smoothedBands[2], _smoothedBands[3]);
            // Row 0, Pixel 1: Bands 4-7
            _audioPixels[1] = new Color(_smoothedBands[4], _smoothedBands[5], _smoothedBands[6], _smoothedBands[7]);
            // Row 0, Pixel 2-3: Legacy 4-band + volume
            _audioPixels[2] = new Color(_subBass, _bass, _mids, _treble);
            _audioPixels[3] = new Color(_smoothedVolume, _peakLevel, _beatPulse, _beatIntensity);

            // Row 1: Reserved for future
            _audioPixels[4] = Color.clear;
            _audioPixels[5] = Color.clear;
            _audioPixels[6] = Color.clear;
            _audioPixels[7] = Color.clear;

            _audioTexture.SetPixels(_audioPixels);
            _audioTexture.Apply(false, false);
        }

        #endregion

        #region Public API - Brush Modulation

        /// <summary>
        /// Get modulation for brush with specific frequency band.
        /// Replaces BrushAudioReactive.GetModulation() - uses shared FFT data.
        /// </summary>
        public float GetBandModulation(int band, float sensitivity = 1f)
        {
            band = Mathf.Clamp(band, 0, 7);
            return Mathf.Clamp01(_smoothedBands[band] * sensitivity);
        }

        /// <summary>
        /// Get full brush modulation struct (compatible with BrushAudioReactive API)
        /// </summary>
        public BrushPainting.AudioModulation GetBrushModulation(BrushPainting.AudioReactiveParams audioParams)
        {
            var modulation = new BrushPainting.AudioModulation();

            switch (audioParams.Mode)
            {
                case BrushPainting.AudioReactiveMode.RMS:
                    modulation.NormalizedLevel = _smoothedVolume;
                    break;

                case BrushPainting.AudioReactiveMode.FrequencyBand:
                    int band = Mathf.Clamp(audioParams.FrequencyBand, 0, 7);
                    modulation.NormalizedLevel = _smoothedBands[band];
                    break;

                case BrushPainting.AudioReactiveMode.FFTSpectrum:
                    float sum = 0f;
                    for (int i = 0; i < 8; i++) sum += _smoothedBands[i];
                    modulation.NormalizedLevel = sum / 8f;
                    modulation.Bands = Bands; // Copy
                    break;

                case BrushPainting.AudioReactiveMode.Peak:
                    modulation.NormalizedLevel = _peakLevel;
                    break;

                case BrushPainting.AudioReactiveMode.Beat:
                    modulation.NormalizedLevel = _isOnset ? 1f : 0f;
                    modulation.BeatDetected = _isOnset;
                    break;
            }

            modulation.NormalizedLevel = Mathf.Clamp01(modulation.NormalizedLevel * audioParams.Sensitivity);
            modulation.NormalizedLevel = Mathf.Lerp(modulation.NormalizedLevel, modulation.PreviousLevel, audioParams.Smoothing);

            return modulation;
        }

        /// <summary>
        /// Get weighted average of multiple bands (for complex brush effects)
        /// </summary>
        public float GetWeightedBands(float[] weights)
        {
            if (weights == null || weights.Length != 8) return _smoothedVolume;

            float sum = 0f;
            float weightSum = 0f;
            for (int i = 0; i < 8; i++)
            {
                sum += _smoothedBands[i] * weights[i];
                weightSum += weights[i];
            }

            return weightSum > 0 ? sum / weightSum : 0f;
        }

        #endregion

        #region Runtime Configuration

        public void SetInputMode(AudioInputMode mode)
        {
            if (_inputMode == mode) return;

            CleanupMicrophone();
            _inputMode = mode;

            if (mode == AudioInputMode.Microphone)
                _micInitCoroutine = StartCoroutine(InitializeMicrophoneAsync());
            else
                _audioSource ??= GetComponent<AudioSource>() ?? FindFirstObjectByType<AudioSource>();
        }

        public void SetSensitivity(float sensitivity) => _sensitivity = Mathf.Max(0.1f, sensitivity);
        public void SetSmoothing(float smoothing) => _smoothing = Mathf.Clamp01(smoothing);
        public void SetBeatDetectionEnabled(bool enabled) => _enableBeatDetection = enabled;

        public void SetBeatParameters(float threshold, float decayTime, float minInterval)
        {
            _beatThreshold = Mathf.Clamp(threshold, 1f, 3f);
            _pulseDecayTime = Mathf.Clamp(decayTime, 0.05f, 0.5f);
            _minBeatInterval = Mathf.Clamp(minInterval, 0.05f, 0.5f);
        }

        #endregion

        #region Cleanup

        void CleanupMicrophone()
        {
            if (_micInitCoroutine != null)
            {
                StopCoroutine(_micInitCoroutine);
                _micInitCoroutine = null;
            }

            if (_microphoneActive && Microphone.IsRecording(_activeMicDevice))
            {
                Microphone.End(_activeMicDevice);
                _microphoneActive = false;
            }
        }

        void CleanupTexture()
        {
            if (_audioTexture != null)
            {
                Destroy(_audioTexture);
                _audioTexture = null;
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug Audio State")]
        void DebugAudioState()
        {
            Debug.Log("=== UnifiedAudioReactive Debug ===");
            Debug.Log($"Volume: {_smoothedVolume:F3}, Peak: {_peakLevel:F3}");
            Debug.Log($"Bands: [{string.Join(", ", Array.ConvertAll(_smoothedBands, b => b.ToString("F3")))}]");
            Debug.Log($"Beat: Pulse={_beatPulse:F3}, Intensity={_beatIntensity:F3}, Onset={_isOnset}");
            Debug.Log($"Input: {_inputMode}, Mic Active: {_microphoneActive}");
        }

        #endregion
    }
}
