// AROcclusionDebugController - Toggle AR occlusion for VFX debugging
// Allows disabling mesh occlusion, depth occlusion, and forcing VFX visibility

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;

namespace XRRAI.Debugging
{
    /// <summary>
    /// Debug controller for toggling AR occlusion features.
    /// Use during development to ensure VFX are visible regardless of AR occlusion.
    /// </summary>
    public class AROcclusionDebugController : MonoBehaviour
    {
        [Header("Occlusion Toggles")]
        [Tooltip("Disable AR mesh occlusion (mesh renderer visibility)")]
        [SerializeField] bool _disableMeshOcclusion = false;

        [Tooltip("Disable AR depth occlusion (AROcclusionManager)")]
        [SerializeField] bool _disableDepthOcclusion = false;

        [Tooltip("Force all VFX to render in front (render queue override)")]
        [SerializeField] bool _forceVFXForeground = false;

        [Header("Keyboard Shortcuts")]
        [SerializeField] KeyCode _toggleMeshOcclusionKey = KeyCode.O;
        [SerializeField] KeyCode _toggleDepthOcclusionKey = KeyCode.P;
        [SerializeField] KeyCode _toggleVFXForegroundKey = KeyCode.F;
        [SerializeField] bool _enableKeyboardShortcuts = true;

        [Header("Auto-Find")]
        [SerializeField] AROcclusionManager _occlusionManager;
        [SerializeField] ARMeshManager _meshManager;

        [Header("Status")]
        [SerializeField] bool _showDebugUI = true;

        // Cached state
        bool _meshOcclusionWasEnabled;
        bool _depthOcclusionWasEnabled;
        int[] _originalRenderQueues;
        VisualEffect[] _cachedVFX;

        void Start()
        {
            // Auto-find managers
            _occlusionManager ??= FindFirstObjectByType<AROcclusionManager>();
            _meshManager ??= FindFirstObjectByType<ARMeshManager>();

            // Cache original states
            if (_meshManager != null)
                _meshOcclusionWasEnabled = _meshManager.enabled;

            // Apply initial settings
            ApplyOcclusionSettings();
        }

        void Update()
        {
            if (_enableKeyboardShortcuts)
            {
                if (Input.GetKeyDown(_toggleMeshOcclusionKey))
                {
                    _disableMeshOcclusion = !_disableMeshOcclusion;
                    ApplyMeshOcclusionSetting();
                    LogStatus("Mesh Occlusion", !_disableMeshOcclusion);
                }

                if (Input.GetKeyDown(_toggleDepthOcclusionKey))
                {
                    _disableDepthOcclusion = !_disableDepthOcclusion;
                    ApplyDepthOcclusionSetting();
                    LogStatus("Depth Occlusion", !_disableDepthOcclusion);
                }

                if (Input.GetKeyDown(_toggleVFXForegroundKey))
                {
                    _forceVFXForeground = !_forceVFXForeground;
                    ApplyVFXForegroundSetting();
                    LogStatus("VFX Foreground", _forceVFXForeground);
                }
            }
        }

        void ApplyOcclusionSettings()
        {
            ApplyMeshOcclusionSetting();
            ApplyDepthOcclusionSetting();
            ApplyVFXForegroundSetting();
        }

        void ApplyMeshOcclusionSetting()
        {
            if (_meshManager == null) return;

            if (_disableMeshOcclusion)
            {
                // Disable mesh manager and hide all mesh renderers
                _meshManager.enabled = false;
                SetMeshRenderersVisible(false);
            }
            else
            {
                _meshManager.enabled = _meshOcclusionWasEnabled;
                SetMeshRenderersVisible(true);
            }
        }

        void SetMeshRenderersVisible(bool visible)
        {
            // Find all AR mesh renderers and toggle visibility
            if (_meshManager == null) return;

            foreach (var filter in _meshManager.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.enabled = visible;
            }
        }

        void ApplyDepthOcclusionSetting()
        {
            if (_occlusionManager == null) return;

            if (_disableDepthOcclusion)
            {
                // Disable occlusion by setting mode to None (if available)
                // This prevents depth-based occlusion of virtual objects
                _occlusionManager.requestedEnvironmentDepthMode =
                    UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Disabled;
                _occlusionManager.requestedOcclusionPreferenceMode =
                    UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.NoOcclusion;
            }
            else
            {
                // Re-enable with best available mode
                _occlusionManager.requestedEnvironmentDepthMode =
                    UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
                _occlusionManager.requestedOcclusionPreferenceMode =
                    UnityEngine.XR.ARSubsystems.OcclusionPreferenceMode.PreferEnvironmentOcclusion;
            }
        }

        void ApplyVFXForegroundSetting()
        {
            _cachedVFX = FindObjectsByType<VisualEffect>(FindObjectsSortMode.None);

            foreach (var vfx in _cachedVFX)
            {
                var renderer = vfx.GetComponent<VFXRenderer>();
                if (renderer == null) continue;

                if (_forceVFXForeground)
                {
                    // Store original render queues if not already stored
                    if (_originalRenderQueues == null || _originalRenderQueues.Length != _cachedVFX.Length)
                    {
                        _originalRenderQueues = new int[_cachedVFX.Length];
                        for (int i = 0; i < _cachedVFX.Length; i++)
                        {
                            var r = _cachedVFX[i].GetComponent<VFXRenderer>();
                            if (r != null && r.sharedMaterial != null)
                                _originalRenderQueues[i] = r.sharedMaterial.renderQueue;
                        }
                    }

                    // Force to overlay render queue (renders last, on top of everything)
                    if (renderer.sharedMaterial != null)
                        renderer.sharedMaterial.renderQueue = 4000; // Overlay
                }
                else
                {
                    // Restore original render queue
                    if (_originalRenderQueues != null)
                    {
                        int idx = System.Array.IndexOf(_cachedVFX, vfx);
                        if (idx >= 0 && idx < _originalRenderQueues.Length && renderer.sharedMaterial != null)
                            renderer.sharedMaterial.renderQueue = _originalRenderQueues[idx];
                    }
                }
            }
        }

        void LogStatus(string feature, bool enabled)
        {
            string status = enabled ? "ENABLED" : "DISABLED";
            UnityEngine.Debug.Log($"[AROcclusionDebug] {feature}: {status}");
        }

        void OnGUI()
        {
            if (!_showDebugUI) return;

            float x = Screen.width - 220;
            float y = 10;
            float w = 210;
            float h = 22;

            GUI.Box(new Rect(x - 5, y - 5, w + 10, 120), "");

            GUI.Label(new Rect(x, y, w, h), "AR OCCLUSION DEBUG");
            y += h;

            // Mesh occlusion toggle
            string meshStatus = _disableMeshOcclusion ? "OFF" : "ON";
            GUI.color = _disableMeshOcclusion ? Color.yellow : Color.green;
            if (GUI.Button(new Rect(x, y, w, h), $"Mesh Occlusion: {meshStatus} [{_toggleMeshOcclusionKey}]"))
            {
                _disableMeshOcclusion = !_disableMeshOcclusion;
                ApplyMeshOcclusionSetting();
            }
            y += h + 2;

            // Depth occlusion toggle
            string depthStatus = _disableDepthOcclusion ? "OFF" : "ON";
            GUI.color = _disableDepthOcclusion ? Color.yellow : Color.green;
            if (GUI.Button(new Rect(x, y, w, h), $"Depth Occlusion: {depthStatus} [{_toggleDepthOcclusionKey}]"))
            {
                _disableDepthOcclusion = !_disableDepthOcclusion;
                ApplyDepthOcclusionSetting();
            }
            y += h + 2;

            // VFX foreground toggle
            string vfxStatus = _forceVFXForeground ? "ON" : "OFF";
            GUI.color = _forceVFXForeground ? Color.cyan : Color.white;
            if (GUI.Button(new Rect(x, y, w, h), $"VFX Foreground: {vfxStatus} [{_toggleVFXForegroundKey}]"))
            {
                _forceVFXForeground = !_forceVFXForeground;
                ApplyVFXForegroundSetting();
            }
            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            // Restore original settings
            if (_meshManager != null)
            {
                _meshManager.enabled = _meshOcclusionWasEnabled;
                SetMeshRenderersVisible(true);
            }

            // Restore VFX render queues
            _forceVFXForeground = false;
            ApplyVFXForegroundSetting();
        }

        // Public API for runtime control
        public void SetMeshOcclusionEnabled(bool enabled)
        {
            _disableMeshOcclusion = !enabled;
            ApplyMeshOcclusionSetting();
        }

        public void SetDepthOcclusionEnabled(bool enabled)
        {
            _disableDepthOcclusion = !enabled;
            ApplyDepthOcclusionSetting();
        }

        public void SetVFXForeground(bool foreground)
        {
            _forceVFXForeground = foreground;
            ApplyVFXForegroundSetting();
        }

        public void DisableAllOcclusion()
        {
            _disableMeshOcclusion = true;
            _disableDepthOcclusion = true;
            ApplyOcclusionSettings();
        }

        public void EnableAllOcclusion()
        {
            _disableMeshOcclusion = false;
            _disableDepthOcclusion = false;
            ApplyOcclusionSettings();
        }
    }
}
