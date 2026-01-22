// BeatDetector - Onset detection for audio-reactive VFX (spec-007)
// Uses spectral flux algorithm for reliable beat detection on mobile

using UnityEngine;

namespace XRRAI.Audio
{
    /// <summary>
    /// Spectral flux based beat detector.
    /// Detects onsets in the bass frequency band (20-200Hz).
    /// Mobile-optimized with ~0.1ms overhead.
    /// </summary>
    public class BeatDetector
    {
        // Configuration
        readonly int _historySize;
        readonly float _thresholdMultiplier;
        readonly float _pulseDecayTime;
        readonly int _bassEndBin;

        // State
        float[] _previousSpectrum;
        readonly float[] _fluxHistory;
        int _historyIndex;
        float _currentPulse;
        float _beatIntensity;
        float _lastBeatTime;
        bool _isOnset;

        // Public outputs
        public float BeatPulse => _currentPulse;
        public float BeatIntensity => _beatIntensity;
        public bool IsOnset => _isOnset;
        public float TimeSinceLastBeat => Time.time - _lastBeatTime;

        // Settings exposed for tuning
        public float ThresholdMultiplier { get; set; }
        public float PulseDecayTime { get; set; }
        public float MinBeatInterval { get; set; }

        /// <summary>
        /// Create a beat detector with configurable parameters.
        /// </summary>
        /// <param name="sampleCount">FFT size (must match AudioBridge)</param>
        /// <param name="sampleRate">Audio sample rate (typically 44100 or 48000)</param>
        /// <param name="historySize">Frames of flux history for adaptive threshold (default: 43 ~= 1 second at 60fps)</param>
        /// <param name="thresholdMultiplier">Multiplier for adaptive threshold (default: 1.5)</param>
        /// <param name="pulseDecayTime">Time for pulse to decay from 1→0 (default: 0.1s)</param>
        public BeatDetector(
            int sampleCount = 1024,
            int sampleRate = 44100,
            int historySize = 43,
            float thresholdMultiplier = 1.5f,
            float pulseDecayTime = 0.1f)
        {
            _historySize = historySize;
            _thresholdMultiplier = thresholdMultiplier;
            _pulseDecayTime = pulseDecayTime;

            ThresholdMultiplier = _thresholdMultiplier;
            PulseDecayTime = _pulseDecayTime;
            MinBeatInterval = 0.1f; // Min 100ms between beats (max 600 BPM)

            // Calculate bass band end bin (200Hz)
            // Bin frequency = bin * sampleRate / sampleCount
            // So bin = targetFreq * sampleCount / sampleRate
            _bassEndBin = Mathf.Clamp((int)(200f * sampleCount / sampleRate), 4, sampleCount / 4);

            _previousSpectrum = new float[_bassEndBin];
            _fluxHistory = new float[_historySize];
            _historyIndex = 0;
            _lastBeatTime = -1f;
        }

        /// <summary>
        /// Process a new spectrum frame and detect beats.
        /// Call this once per frame with fresh spectrum data.
        /// </summary>
        /// <param name="spectrum">FFT spectrum data (from AudioSource.GetSpectrumData)</param>
        public void Process(float[] spectrum)
        {
            if (spectrum == null || spectrum.Length < _bassEndBin) return;

            // 1. Compute spectral flux (positive energy delta in bass band)
            float flux = ComputeSpectralFlux(spectrum);

            // 2. Update flux history for adaptive threshold
            _fluxHistory[_historyIndex] = flux;
            _historyIndex = (_historyIndex + 1) % _historySize;

            // 3. Compute adaptive threshold
            float threshold = ComputeAdaptiveThreshold();

            // 4. Detect onset
            _isOnset = flux > threshold && (Time.time - _lastBeatTime) > MinBeatInterval;

            if (_isOnset)
            {
                _currentPulse = 1f;
                _beatIntensity = Mathf.Clamp01(flux / Mathf.Max(threshold, 0.001f) - 0.5f);
                _lastBeatTime = Time.time;
            }

            // 5. Decay pulse
            float decayRate = 1f / Mathf.Max(PulseDecayTime, 0.01f);
            _currentPulse = Mathf.Max(0, _currentPulse - decayRate * Time.deltaTime);

            // 6. Store current spectrum for next frame
            System.Array.Copy(spectrum, _previousSpectrum, _bassEndBin);
        }

        /// <summary>
        /// Compute spectral flux (sum of positive energy deltas in bass band).
        /// </summary>
        float ComputeSpectralFlux(float[] spectrum)
        {
            float flux = 0;
            for (int i = 0; i < _bassEndBin; i++)
            {
                float diff = spectrum[i] - _previousSpectrum[i];
                if (diff > 0) // Half-wave rectification - only positive changes
                {
                    flux += diff;
                }
            }
            return flux;
        }

        /// <summary>
        /// Compute adaptive threshold from flux history.
        /// </summary>
        float ComputeAdaptiveThreshold()
        {
            float sum = 0;
            int count = 0;
            for (int i = 0; i < _historySize; i++)
            {
                if (_fluxHistory[i] > 0)
                {
                    sum += _fluxHistory[i];
                    count++;
                }
            }

            if (count == 0) return 0.001f;

            float average = sum / count;
            return average * ThresholdMultiplier;
        }

        /// <summary>
        /// Reset beat detector state.
        /// </summary>
        public void Reset()
        {
            System.Array.Clear(_previousSpectrum, 0, _previousSpectrum.Length);
            System.Array.Clear(_fluxHistory, 0, _fluxHistory.Length);
            _historyIndex = 0;
            _currentPulse = 0;
            _beatIntensity = 0;
            _lastBeatTime = -1f;
            _isOnset = false;
        }

        /// <summary>
        /// Debug info for tuning.
        /// </summary>
        public string GetDebugInfo()
        {
            return $"BeatPulse: {_currentPulse:F2}, Intensity: {_beatIntensity:F2}, " +
                   $"LastBeat: {TimeSinceLastBeat:F2}s ago, Threshold: {ComputeAdaptiveThreshold():F4}";
        }
    }
}
