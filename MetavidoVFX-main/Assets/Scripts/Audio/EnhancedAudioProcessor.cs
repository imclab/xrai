// Enhanced Audio Processor with FFT Frequency Band Separation
// Provides bass/mid/treble separation for VFX control
// Supports microphone input and audio file playback

using UnityEngine;
using System.Collections;
using XRRAI.VFXBinders;

namespace XRRAI.Audio
{
    /// <summary>
    /// Enhanced audio analysis with FFT frequency band separation.
    /// Provides bass/mid/treble values for driving VFX parameters.
    /// </summary>
    public class EnhancedAudioProcessor : MonoBehaviour
    {
        void Log(string msg) { Debug.Log(msg); }
        void LogWarning(string msg) { Debug.LogWarning(msg); }

        [Header("Audio Input")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool useMicrophone = true;
        [SerializeField] private string microphoneDevice = "";

        [Header("FFT Settings")]
        [SerializeField] private FFTWindow fftWindow = FFTWindow.BlackmanHarris;
        [SerializeField] private int spectrumSize = 1024;

        [Header("Frequency Bands (Hz)")]
        [SerializeField] private float bassMax = 250f;
        [SerializeField] private float midMax = 2000f;
        [SerializeField] private float trebleMax = 20000f;

        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private float attackSpeed = 20f;
        [SerializeField] private float decaySpeed = 5f;

        // Public properties
        public float AudioVolume => _smoothVolume;
        public float AudioPitch => _smoothPitch;
        public float AudioBass => _smoothBass;
        public float AudioMid => _smoothMid;
        public float AudioTreble => _smoothTreble;
        public float AudioSubBass => _smoothSubBass;

        // Raw values (unsmoothed)
        public float RawVolume => _rawVolume;
        public float RawBass => _rawBass;
        public float RawMid => _rawMid;
        public float RawTreble => _rawTreble;

        // Spectrum data (for visualization)
        public float[] Spectrum => _spectrum;

        // Internal state
        private float[] _samples;
        private float[] _spectrum;
        private float _sampleRate;

        private float _rawVolume;
        private float _rawPitch;
        private float _rawBass;
        private float _rawMid;
        private float _rawTreble;
        private float _rawSubBass;

        private float _smoothVolume;
        private float _smoothPitch;
        private float _smoothBass;
        private float _smoothMid;
        private float _smoothTreble;
        private float _smoothSubBass;

        // Frequency bin calculations
        private int _bassMaxBin;
        private int _midMaxBin;
        private int _trebleMaxBin;
        private int _subBassMaxBin;

        private const float REF_VALUE = 0.1f;
        private const float THRESHOLD = 0.02f;

        private Coroutine _microphoneInitCoroutine;

        void Start()
        {
            _samples = new float[spectrumSize];
            _spectrum = new float[spectrumSize];
            _sampleRate = AudioSettings.outputSampleRate;

            // Calculate frequency bin indices
            // Each bin represents: sampleRate / spectrumSize Hz
            float binResolution = _sampleRate / (2f * spectrumSize);
            _subBassMaxBin = Mathf.Max(1, Mathf.FloorToInt(60f / binResolution));
            _bassMaxBin = Mathf.FloorToInt(bassMax / binResolution);
            _midMaxBin = Mathf.FloorToInt(midMax / binResolution);
            _trebleMaxBin = Mathf.Min(spectrumSize - 1, Mathf.FloorToInt(trebleMax / binResolution));

            Log($"[EnhancedAudio] Sample Rate: {_sampleRate}, Bin Resolution: {binResolution:F1}Hz");
            Log($"[EnhancedAudio] Bins - SubBass: 0-{_subBassMaxBin}, Bass: 0-{_bassMaxBin}, Mid: {_bassMaxBin}-{_midMaxBin}, Treble: {_midMaxBin}-{_trebleMaxBin}");

            if (useMicrophone)
            {
                _microphoneInitCoroutine = StartCoroutine(StartMicrophoneAsync());
            }
        }

        /// <summary>
        /// Non-blocking microphone initialization using coroutine.
        /// Yields each frame while waiting for microphone to become ready.
        /// </summary>
        IEnumerator StartMicrophoneAsync()
        {
            if (Microphone.devices.Length == 0)
            {
                LogWarning("[EnhancedAudio] No microphone devices found");
                yield break;
            }

            string device = string.IsNullOrEmpty(microphoneDevice) ? null : microphoneDevice;

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = Microphone.Start(device, true, 1, 44100);
            audioSource.loop = true;

            // Non-blocking wait for microphone to start (yields each frame)
            float timeout = Time.realtimeSinceStartup + 3f;
            while (Microphone.GetPosition(device) <= 0)
            {
                if (Time.realtimeSinceStartup > timeout)
                {
                    LogWarning("[EnhancedAudio] Microphone initialization timed out after 3 seconds");
                    yield break;
                }
                yield return null; // Yield to next frame instead of blocking
            }

            audioSource.Play();
            audioSource.volume = 0f; // Mute playback but still analyze

            Log($"[EnhancedAudio] Microphone started: {device ?? Microphone.devices[0]}");
        }

        void Update()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            AnalyzeAudio();
            SmoothValues();
        }

        void AnalyzeAudio()
        {
            // Get raw samples for volume calculation
            audioSource.GetOutputData(_samples, 0);

            // Calculate RMS (volume)
            float sum = 0f;
            for (int i = 0; i < spectrumSize; i++)
            {
                sum += _samples[i] * _samples[i];
            }
            float rms = Mathf.Sqrt(sum / spectrumSize);
            float db = 20f * Mathf.Log10(rms / REF_VALUE);
            db = Mathf.Clamp(db, -80f, 20f);
            _rawVolume = Remap(db, -20f, 10f, 0f, 1f);

            // Get spectrum data for frequency analysis
            audioSource.GetSpectrumData(_spectrum, 0, fftWindow);

            // Calculate frequency bands
            _rawSubBass = CalculateBandAverage(0, _subBassMaxBin);
            _rawBass = CalculateBandAverage(0, _bassMaxBin);
            _rawMid = CalculateBandAverage(_bassMaxBin, _midMaxBin);
            _rawTreble = CalculateBandAverage(_midMaxBin, _trebleMaxBin);

            // Find dominant pitch
            _rawPitch = CalculateDominantPitch();
        }

        float CalculateBandAverage(int startBin, int endBin)
        {
            if (startBin >= endBin || endBin >= spectrumSize) return 0f;

            float sum = 0f;
            float maxVal = 0f;

            for (int i = startBin; i < endBin; i++)
            {
                sum += _spectrum[i];
                if (_spectrum[i] > maxVal) maxVal = _spectrum[i];
            }

            // Use combination of average and peak for responsive feel
            float avg = sum / (endBin - startBin);
            float combined = (avg + maxVal) * 0.5f;

            // Normalize to 0-1 range (spectrum values are typically 0-0.1)
            return Mathf.Clamp01(combined * 10f);
        }

        float CalculateDominantPitch()
        {
            float maxVal = 0f;
            int maxIndex = 0;

            for (int i = 1; i < spectrumSize; i++)
            {
                if (_spectrum[i] > maxVal && _spectrum[i] > THRESHOLD)
                {
                    maxVal = _spectrum[i];
                    maxIndex = i;
                }
            }

            if (maxIndex == 0) return 0f;

            // Parabolic interpolation for better accuracy
            float freqIndex = maxIndex;
            if (maxIndex > 0 && maxIndex < spectrumSize - 1)
            {
                float dL = _spectrum[maxIndex - 1] / _spectrum[maxIndex];
                float dR = _spectrum[maxIndex + 1] / _spectrum[maxIndex];
                freqIndex += 0.5f * (dR * dR - dL * dL);
            }

            float frequency = freqIndex * _sampleRate / (2f * spectrumSize);

            // Normalize pitch to 0-1 (20Hz - 2000Hz typical range)
            return Remap(frequency, 20f, 2000f, 0f, 1f);
        }

        void SmoothValues()
        {
            float dt = Time.deltaTime;

            // Use attack/decay for more responsive feel
            _smoothVolume = SmoothWithAttackDecay(_smoothVolume, _rawVolume, dt);
            _smoothPitch = Mathf.Lerp(_smoothPitch, _rawPitch, dt * smoothSpeed);
            _smoothBass = SmoothWithAttackDecay(_smoothBass, _rawBass, dt);
            _smoothMid = SmoothWithAttackDecay(_smoothMid, _rawMid, dt);
            _smoothTreble = SmoothWithAttackDecay(_smoothTreble, _rawTreble, dt);
            _smoothSubBass = SmoothWithAttackDecay(_smoothSubBass, _rawSubBass, dt);
        }

        float SmoothWithAttackDecay(float current, float target, float dt)
        {
            float speed = target > current ? attackSpeed : decaySpeed;
            return Mathf.Lerp(current, target, dt * speed);
        }

        float Remap(float value, float srcMin, float srcMax, float dstMin, float dstMax)
        {
            value = Mathf.Clamp(value, srcMin, srcMax);
            float t = (value - srcMin) / (srcMax - srcMin);
            return dstMin + t * (dstMax - dstMin);
        }

        /// <summary>
        /// Get all frequency band values as a Vector4 (bass, mid, treble, volume)
        /// </summary>
        public Vector4 GetBandValues()
        {
            return new Vector4(_smoothBass, _smoothMid, _smoothTreble, _smoothVolume);
        }

        /// <summary>
        /// Push audio values to a VFX Graph
        /// </summary>
        public void PushToVFX(UnityEngine.VFX.VisualEffect vfx)
        {
            if (vfx == null) return;

            if (vfx.HasFloat("AudioVolume"))
                vfx.SetFloat("AudioVolume", _smoothVolume);

            if (vfx.HasFloat("AudioPitch"))
                vfx.SetFloat("AudioPitch", _smoothPitch);

            if (vfx.HasFloat("AudioBass"))
                vfx.SetFloat("AudioBass", _smoothBass);

            if (vfx.HasFloat("AudioMid"))
                vfx.SetFloat("AudioMid", _smoothMid);

            if (vfx.HasFloat("AudioTreble"))
                vfx.SetFloat("AudioTreble", _smoothTreble);

            if (vfx.HasFloat("AudioSubBass"))
                vfx.SetFloat("AudioSubBass", _smoothSubBass);
        }

        void OnDestroy()
        {
            // Stop initialization coroutine if still running
            if (_microphoneInitCoroutine != null)
            {
                StopCoroutine(_microphoneInitCoroutine);
                _microphoneInitCoroutine = null;
            }

            if (useMicrophone && Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
            }
        }
    }
}
