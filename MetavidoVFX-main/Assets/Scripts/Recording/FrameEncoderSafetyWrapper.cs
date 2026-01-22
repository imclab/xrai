// FrameEncoderSafetyWrapper - Prevents NullReferenceException in Metavido FrameEncoder
// The Metavido package's FrameEncoder crashes if _xrSource is not assigned.
// This wrapper disables FrameEncoder until AR is actually ready.
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace XRRAI.Recording
{
    /// <summary>
    /// Prevents FrameEncoder NullReferenceException by disabling it until AR is ready.
    /// Add this component to any GameObject with FrameEncoder.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before FrameEncoder
    public class FrameEncoderSafetyWrapper : MonoBehaviour
    {
        private MonoBehaviour _frameEncoder;
        private AROcclusionManager _occlusionManager;
        private bool _wasEnabled;
        private bool _arReady;

        void Awake()
        {
            // Find FrameEncoder on this GameObject
            _frameEncoder = GetComponent("Metavido.Encoder.FrameEncoder") as MonoBehaviour;
            if (_frameEncoder == null)
            {
                // Try by type name
                foreach (var comp in GetComponents<MonoBehaviour>())
                {
                    if (comp != null && comp.GetType().Name == "FrameEncoder")
                    {
                        _frameEncoder = comp;
                        break;
                    }
                }
            }

            if (_frameEncoder != null)
            {
                _wasEnabled = _frameEncoder.enabled;
                _frameEncoder.enabled = false; // Disable until AR ready
                Debug.Log("[FrameEncoderSafety] Disabled FrameEncoder until AR is ready");
            }

            _occlusionManager = FindFirstObjectByType<AROcclusionManager>();
        }

        void Update()
        {
            if (_arReady || _frameEncoder == null) return;

            // Check if AR depth is available
            bool depthReady = false;
            if (_occlusionManager != null)
            {
                try
                {
                    var depth = _occlusionManager.environmentDepthTexture;
                    depthReady = depth != null;
                }
                catch
                {
                    depthReady = false;
                }
            }

            if (depthReady && _wasEnabled)
            {
                _frameEncoder.enabled = true;
                _arReady = true;
                Debug.Log("[FrameEncoderSafety] AR ready - enabled FrameEncoder");
            }
        }

        void OnDestroy()
        {
            // Restore original state
            if (_frameEncoder != null && _wasEnabled)
            {
                _frameEncoder.enabled = _wasEnabled;
            }
        }
    }
}
#endif
