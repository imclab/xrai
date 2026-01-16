using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.VFX;

namespace Metavido {

public class ARKitMetavidoController : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] ARSession _session;
    [SerializeField] AROcclusionManager _occlusionManager;
    [SerializeField] ARCameraManager _cameraManager;

    [Header("VFX")]
    [SerializeField] VisualEffect _vfx;

    void Start()
    {
        // Auto-find components if not assigned
        if (_session == null) _session = FindObjectOfType<ARSession>();
        if (_occlusionManager == null) _occlusionManager = FindObjectOfType<AROcclusionManager>();
        if (_cameraManager == null) _cameraManager = FindObjectOfType<ARCameraManager>();
        if (_vfx == null) _vfx = FindObjectOfType<VisualEffect>();

        // Configure Occlusion Manager for LiDAR
        if (_occlusionManager != null)
        {
            _occlusionManager.requestedEnvironmentDepthMode = UnityEngine.XR.ARSubsystems.EnvironmentDepthMode.Best;
            _occlusionManager.requestedHumanDepthMode = UnityEngine.XR.ARSubsystems.HumanSegmentationDepthMode.Best;
            _occlusionManager.requestedOcclusionPreferenceMode = OcclusionPreferenceMode.NoOcclusion; // We want raw depth data
        }
    }

    void Update()
    {
        // Optional: Toggle VFX based on tracking state
        if (_session != null && _session.subsystem != null)
        {
            bool isTracking = _session.subsystem.running;
            // _vfx.enabled = isTracking; // Or handle gracefully
        }
    }
}

} // namespace Metavido
