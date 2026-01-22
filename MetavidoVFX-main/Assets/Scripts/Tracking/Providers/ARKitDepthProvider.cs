// ARKitDepthProvider - Wraps ARDepthSource for ITrackingProvider interface (spec-008)

using System;
using UnityEngine;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// ARKit depth provider wrapping existing ARDepthSource singleton.
    /// Provides DepthData to tracking consumers.
    /// </summary>
    [TrackingProvider("arkit-depth", priority: 100)]
    public class ARKitDepthProvider : ITrackingProvider
    {
        public string Id => "arkit-depth";
        public int Priority => 100;
        public Platform SupportedPlatforms => Platform.iOS | Platform.Editor;
        public TrackingCap Capabilities => TrackingCap.Depth | TrackingCap.HumanDepth;

        DepthData _cachedData;
        bool _initialized;

        public bool IsAvailable
        {
            get
            {
#if UNITY_IOS || UNITY_EDITOR
                return _initialized && ARDepthSource.Instance != null;
#else
                return false;
#endif
            }
        }

        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        public void Initialize()
        {
            _initialized = true;
            // ARDepthSource initializes itself as singleton
        }

        public void Update()
        {
            if (!IsAvailable) return;

            var source = ARDepthSource.Instance;
            if (source == null) return;

            bool wasValid = _cachedData.IsValid;
            _cachedData.IsValid = source.DepthMap != null;

            if (_cachedData.IsValid)
            {
                _cachedData.DepthTexture = source.DepthMap;
                _cachedData.StencilTexture = source.StencilMap;
                _cachedData.ColorTexture = source.ColorMap;
                _cachedData.InverseView = source.InverseView;
                _cachedData.RayParams = source.RayParams;
                _cachedData.Width = source.PositionMap?.width ?? 256;
                _cachedData.Height = source.PositionMap?.height ?? 192;
                _cachedData.DepthRange = new Vector2(0.1f, 10f);
                _cachedData.Timestamp = Time.time;

                if (!wasValid)
                    OnTrackingFound?.Invoke();
            }
            else if (wasValid)
            {
                OnTrackingLost?.Invoke();
            }
        }

        public void Shutdown()
        {
            _initialized = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(DepthData) && _cachedData.IsValid)
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
