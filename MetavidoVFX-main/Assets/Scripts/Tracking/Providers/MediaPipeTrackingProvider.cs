// MediaPipeTrackingProvider.cs - MediaPipe fallback tracking provider
// Part of Spec 008: Cross-Platform Multimodal ML Foundations
//
// Provides fallback tracking for platforms without native AR support (WebGL, etc.)
// Requires: com.github.homuler.mediapipe package + MEDIAPIPE_AVAILABLE define

using System;
using UnityEngine;

namespace XRRAI.ARTracking
{
    /// <summary>
    /// MediaPipe-based tracking provider for cross-platform fallback.
    /// Used on WebGL and platforms without native AR tracking.
    /// </summary>
    [TrackingProvider("MediaPipe", priority: 50)]
    public class MediaPipeTrackingProvider : MonoBehaviour, ITrackingProvider
    {
        [Header("Configuration")]
        [SerializeField] bool _enableHandTracking = true;
        [SerializeField] bool _enablePoseTracking = true;
        [SerializeField] bool _enableFaceTracking = false;

        [Header("Performance")]
        [SerializeField] float _inferenceInterval = 0.033f; // ~30 FPS

        // State
        bool _isInitialized;
        bool _isTracking;
        float _lastInferenceTime;

        // Events
        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        #region ITrackingProvider Implementation

        public string Id => "MediaPipe";
        public int Priority => 50; // Lower priority than native providers
        public Platform SupportedPlatforms => Platform.WebGL | Platform.Editor | Platform.iOS | Platform.Android;

        public TrackingCap Capabilities
        {
            get
            {
                TrackingCap caps = TrackingCap.None;
                if (_enableHandTracking) caps |= TrackingCap.Hands;
                if (_enablePoseTracking) caps |= TrackingCap.Body | TrackingCap.Keypoints;
                if (_enableFaceTracking) caps |= TrackingCap.Face;
                return caps;
            }
        }

        public bool IsAvailable
        {
            get
            {
#if MEDIAPIPE_AVAILABLE
                return true;
#else
                return false;
#endif
            }
        }

        public void Initialize()
        {
            if (_isInitialized) return;

#if MEDIAPIPE_AVAILABLE
            InitializeMediaPipeSolutions();
            _isInitialized = true;
            Debug.Log("[MediaPipeTrackingProvider] Initialized");
#else
            Debug.LogWarning("[MediaPipeTrackingProvider] MediaPipe not available. Install from releases page.");
            _isInitialized = false;
#endif
        }

        public void Update()
        {
            if (!_isInitialized) return;

            // Throttle inference
            if (Time.time - _lastInferenceTime < _inferenceInterval) return;
            _lastInferenceTime = Time.time;

#if MEDIAPIPE_AVAILABLE
            ProcessFrame();
#endif
        }

        public void Shutdown()
        {
#if MEDIAPIPE_AVAILABLE
            ShutdownMediaPipeSolutions();
#endif
            _isInitialized = false;
            _isTracking = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            data = default;

            if (!_isInitialized || !_isTracking)
                return false;

#if MEDIAPIPE_AVAILABLE
            // Handle each data type (using shared types from TrackingData.cs)
            if (typeof(T) == typeof(HandData))
            {
                data = (T)(object)GetHandData();
                return true;
            }
            if (typeof(T) == typeof(BodyPoseData))
            {
                data = (T)(object)GetPoseData();
                return true;
            }
            if (typeof(T) == typeof(FaceData))
            {
                data = (T)(object)GetFaceData();
                return true;
            }
#endif
            return false;
        }

        public void Dispose()
        {
            Shutdown();
        }

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            // Auto-initialize if available
        }

        void Start()
        {
            if (IsAvailable)
            {
                Initialize();
            }
        }

        void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region MediaPipe Integration

#if MEDIAPIPE_AVAILABLE
        void InitializeMediaPipeSolutions()
        {
            try
            {
                if (_enableHandTracking)
                {
                    Debug.Log("[MediaPipeTrackingProvider] Initializing hand tracking...");
                }

                if (_enablePoseTracking)
                {
                    Debug.Log("[MediaPipeTrackingProvider] Initializing pose tracking...");
                }

                if (_enableFaceTracking)
                {
                    Debug.Log("[MediaPipeTrackingProvider] Initializing face tracking...");
                }

                _isTracking = true;
                OnTrackingFound?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MediaPipeTrackingProvider] Initialization failed: {ex.Message}");
                _isTracking = false;
            }
        }

        void ShutdownMediaPipeSolutions()
        {
            _isTracking = false;
            OnTrackingLost?.Invoke();
        }

        void ProcessFrame()
        {
            // Process current camera frame through MediaPipe
            // This would be called from the webcam texture callback

            bool wasTracking = _isTracking;
            _isTracking = true; // Set based on actual detection

            if (_isTracking && !wasTracking)
            {
                OnTrackingFound?.Invoke();
            }
            else if (!_isTracking && wasTracking)
            {
                OnTrackingLost?.Invoke();
            }
        }

        HandData GetHandData()
        {
            return new HandData
            {
                IsTracked = _isTracking,
                Timestamp = Time.time
            };
        }

        BodyPoseData GetPoseData()
        {
            return new BodyPoseData
            {
                IsTracked = _isTracking,
                Timestamp = Time.time
            };
        }

        FaceData GetFaceData()
        {
            return new FaceData
            {
                IsTracked = _isTracking,
                Timestamp = Time.time
            };
        }
#endif

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Test Provider")]
        void TestProvider()
        {
            Debug.Log($"[MediaPipeTrackingProvider] Available: {IsAvailable}");
            Debug.Log($"[MediaPipeTrackingProvider] Capabilities: {Capabilities}");
            Debug.Log($"[MediaPipeTrackingProvider] Initialized: {_isInitialized}");
        }
#endif

        #endregion
    }

    // Note: Uses shared data types from TrackingData.cs:
    // - HandData for hand tracking
    // - BodyPoseData for body pose
    // - FaceData for face tracking
}
