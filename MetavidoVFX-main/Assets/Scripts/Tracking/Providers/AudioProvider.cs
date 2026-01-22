// AudioProvider - Wraps AudioBridge for ITrackingProvider interface (spec-008)

using System;
using UnityEngine;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// Audio provider wrapping existing AudioBridge.
    /// Provides AudioData (FFT, beat detection) to tracking consumers.
    /// </summary>
    [TrackingProvider("audio", priority: 50)]
    public class AudioProvider : ITrackingProvider
    {
        public string Id => "audio";
        public int Priority => 50;
        public Platform SupportedPlatforms => Platform.All;
        public TrackingCap Capabilities => TrackingCap.Audio;

        AudioData _cachedData;
        AudioBridge _bridge;
        bool _initialized;

        public bool IsAvailable => _initialized && _bridge != null;

        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        public void Initialize()
        {
            _bridge = UnityEngine.Object.FindFirstObjectByType<AudioBridge>();
            _initialized = true;

            if (_bridge != null)
                OnTrackingFound?.Invoke();
        }

        public void Update()
        {
            if (_bridge == null)
            {
                _bridge = UnityEngine.Object.FindFirstObjectByType<AudioBridge>();
                if (_bridge != null)
                    OnTrackingFound?.Invoke();
                return;
            }

            // Read from global shader properties set by AudioBridge
            _cachedData.Volume = Shader.GetGlobalFloat("_AudioVolume");
            _cachedData.Bands = Shader.GetGlobalVector("_AudioBands");
            _cachedData.BeatPulse = Shader.GetGlobalFloat("_BeatPulse");
            _cachedData.BeatIntensity = Shader.GetGlobalFloat("_BeatIntensity");
            _cachedData.IsValid = true;
            _cachedData.Timestamp = Time.time;
        }

        public void Shutdown()
        {
            _initialized = false;
            _bridge = null;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(AudioData) && _cachedData.IsValid)
            {
                data = (T)(object)_cachedData;
                return true;
            }
            data = default;
            return false;
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
