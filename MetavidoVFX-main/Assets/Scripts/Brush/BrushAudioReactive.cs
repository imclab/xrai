// BrushAudioReactive.cs - Audio reactive brush modulation
// Part of Spec 011: OpenBrush Integration
//
// Processes audio input and provides modulation values for brushes.
// Ported from OpenBrush's Reaktion audio system with simplified interface.

using UnityEngine;

namespace XRRAI.BrushPainting
{
    /// <summary>
    /// Audio analysis for reactive brush effects.
    /// Provides RMS, frequency bands, peak detection, and beat detection.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BrushAudioReactive : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] AudioSource _audioSource;
        [SerializeField] bool _useExternalAudio;
        [SerializeField] int _microphoneDeviceIndex;

        [Header("Analysis Settings")]
        [SerializeField] int _sampleCount = 1024;
        [SerializeField] FFTWindow _fftWindow = FFTWindow.BlackmanHarris;
        [SerializeField] float _sensitivity = 1f;
        [SerializeField] float _smoothing = 0.8f;

        [Header("Beat Detection")]
        [SerializeField] float _beatThreshold = 1.5f;
        [SerializeField] float _beatCooldown = 0.1f;

        [Header("Debug")]
        [SerializeField] bool _logLevels;

        // Audio data
        float[] _samples;
        float[] _spectrum;
        float[] _frequencyBands = new float[8];
        float[] _smoothedBands = new float[8];

        // Analysis results
        float _rmsLevel;
        float _smoothedRms;
        float _peakLevel;
        float _peakDecay = 0.99f;
        bool _beatDetected;
        float _lastBeatTime;

        // Frequency band boundaries (logarithmic distribution)
        static readonly int[] BandBoundaries = { 2, 4, 8, 16, 32, 64, 128, 255 };

        // Properties
        public float RMS => _smoothedRms;
        public float Peak => _peakLevel;
        public bool BeatDetected => _beatDetected;
        public float[] FrequencyBands => _smoothedBands;

        void Awake()
        {
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            _samples = new float[_sampleCount];
            _spectrum = new float[_sampleCount];
        }

        void OnEnable()
        {
            if (_useExternalAudio)
                StartMicrophoneCapture();
        }

        void OnDisable()
        {
            if (_useExternalAudio)
                StopMicrophoneCapture();
        }

        void Update()
        {
            if (_audioSource == null) return;

            AnalyzeAudio();
            DetectBeat();

            if (_logLevels && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[BrushAudioReactive] RMS: {_smoothedRms:F3}, Peak: {_peakLevel:F3}, Beat: {_beatDetected}");
            }
        }

        void AnalyzeAudio()
        {
            // Get raw samples for RMS
            _audioSource.GetOutputData(_samples, 0);
            _rmsLevel = CalculateRMS(_samples) * _sensitivity;

            // Smooth RMS
            _smoothedRms = Mathf.Lerp(_rmsLevel, _smoothedRms, _smoothing);

            // Update peak with decay
            if (_rmsLevel > _peakLevel)
                _peakLevel = _rmsLevel;
            else
                _peakLevel *= _peakDecay;

            // Get spectrum data for frequency bands
            _audioSource.GetSpectrumData(_spectrum, 0, _fftWindow);
            CalculateFrequencyBands();
        }

        float CalculateRMS(float[] samples)
        {
            float sum = 0f;
            for (int i = 0; i < samples.Length; i++)
                sum += samples[i] * samples[i];
            return Mathf.Sqrt(sum / samples.Length);
        }

        void CalculateFrequencyBands()
        {
            int startIndex = 0;

            for (int band = 0; band < 8; band++)
            {
                int endIndex = Mathf.Min(BandBoundaries[band], _spectrum.Length - 1);
                float sum = 0f;
                int count = 0;

                for (int i = startIndex; i <= endIndex; i++)
                {
                    sum += _spectrum[i];
                    count++;
                }

                float bandValue = count > 0 ? (sum / count) * _sensitivity : 0f;
                _frequencyBands[band] = bandValue;

                // Smooth bands
                _smoothedBands[band] = Mathf.Lerp(bandValue, _smoothedBands[band], _smoothing);

                startIndex = endIndex + 1;
            }
        }

        void DetectBeat()
        {
            _beatDetected = false;

            // Cooldown check
            if (Time.time - _lastBeatTime < _beatCooldown)
                return;

            // Simple beat detection: RMS significantly above smoothed average
            if (_rmsLevel > _smoothedRms * _beatThreshold)
            {
                _beatDetected = true;
                _lastBeatTime = Time.time;
            }
        }

        #region Microphone Capture

        void StartMicrophoneCapture()
        {
            string[] devices = Microphone.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("[BrushAudioReactive] No microphone devices found");
                return;
            }

            int deviceIndex = Mathf.Clamp(_microphoneDeviceIndex, 0, devices.Length - 1);
            string deviceName = devices[deviceIndex];

            _audioSource.clip = Microphone.Start(deviceName, true, 1, 44100);
            _audioSource.loop = true;

            // Wait for microphone to start
            while (Microphone.GetPosition(deviceName) <= 0) { }

            _audioSource.Play();
            Debug.Log($"[BrushAudioReactive] Started microphone: {deviceName}");
        }

        void StopMicrophoneCapture()
        {
            _audioSource.Stop();
            Microphone.End(null);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get modulation values for a brush based on its audio params
        /// </summary>
        public AudioModulation GetModulation(AudioReactiveParams audioParams)
        {
            var modulation = new AudioModulation();

            switch (audioParams.Mode)
            {
                case AudioReactiveMode.RMS:
                    modulation.NormalizedLevel = _smoothedRms;
                    break;

                case AudioReactiveMode.FrequencyBand:
                    int band = Mathf.Clamp(audioParams.FrequencyBand, 0, 7);
                    modulation.NormalizedLevel = _smoothedBands[band];
                    break;

                case AudioReactiveMode.FFTSpectrum:
                    // Average of all bands for overall spectrum
                    float sum = 0f;
                    for (int i = 0; i < 8; i++)
                        sum += _smoothedBands[i];
                    modulation.NormalizedLevel = sum / 8f;
                    modulation.Bands = (float[])_smoothedBands.Clone();
                    break;

                case AudioReactiveMode.Peak:
                    modulation.NormalizedLevel = _peakLevel;
                    break;

                case AudioReactiveMode.Beat:
                    modulation.NormalizedLevel = _beatDetected ? 1f : 0f;
                    modulation.BeatDetected = _beatDetected;
                    break;
            }

            // Apply sensitivity
            modulation.NormalizedLevel = Mathf.Clamp01(modulation.NormalizedLevel * audioParams.Sensitivity);

            // Apply custom smoothing
            modulation.NormalizedLevel = Mathf.Lerp(
                modulation.NormalizedLevel,
                modulation.PreviousLevel,
                audioParams.Smoothing);

            return modulation;
        }

        /// <summary>
        /// Get a specific frequency band (0-7)
        /// </summary>
        public float GetBand(int band)
        {
            return _smoothedBands[Mathf.Clamp(band, 0, 7)];
        }

        /// <summary>
        /// Set audio sensitivity
        /// </summary>
        public void SetSensitivity(float sensitivity)
        {
            _sensitivity = Mathf.Max(0.1f, sensitivity);
        }

        /// <summary>
        /// Set smoothing factor (0 = instant, 1 = maximum smoothing)
        /// </summary>
        public void SetSmoothing(float smoothing)
        {
            _smoothing = Mathf.Clamp01(smoothing);
        }

        /// <summary>
        /// Enable/disable external audio (microphone)
        /// </summary>
        public void SetExternalAudio(bool enabled)
        {
            if (_useExternalAudio == enabled) return;

            _useExternalAudio = enabled;
            if (enabled)
                StartMicrophoneCapture();
            else
                StopMicrophoneCapture();
        }

        #endregion
    }

    /// <summary>
    /// Audio modulation values returned from analysis
    /// </summary>
    public struct AudioModulation
    {
        public float NormalizedLevel;
        public float PreviousLevel;
        public float[] Bands;
        public bool BeatDetected;
    }
}
