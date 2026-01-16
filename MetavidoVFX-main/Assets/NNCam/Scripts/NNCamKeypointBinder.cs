// NNCam Keypoint Binder
// Adapts BodyPartSegmenter's KeypointBuffer for NNCam2 VFX
// Works with eyes_any_nncam2.vfx, joints_any_nncam2.vfx, etc.

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

#if BODYPIX_AVAILABLE
using MetavidoVFX.Segmentation;
#endif

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// VFX Property Binder that connects BodyPartSegmenter's KeypointBuffer
    /// to NNCam2 VFX effects (Eyes, Joints, etc.).
    ///
    /// The original NNCam2 BodyPixKeypointBinder used Keijiro's BodyPixInput.
    /// This adapter uses our BodyPartSegmenter which provides the same KeypointBuffer.
    /// </summary>
    [VFXBinder("NNCam/Keypoint Buffer")]
    public class NNCamKeypointBinder : VFXBinderBase
    {
        [Header("Keypoint Property")]
        [VFXPropertyBinding("GraphicsBuffer"), SerializeField]
        ExposedProperty _keypointProperty = "KeypointBuffer";

        [Header("Source")]
        [Tooltip("BodyPartSegmenter that provides KeypointBuffer. Auto-found if null.")]
#if BODYPIX_AVAILABLE
        public BodyPartSegmenter segmenter;
#endif

        [Header("Debug")]
        public bool verboseLogging = false;

#if BODYPIX_AVAILABLE
        protected override void Awake()
        {
            base.Awake();
            if (segmenter == null)
            {
                segmenter = FindFirstObjectByType<BodyPartSegmenter>();
            }
        }
#endif

        public override bool IsValid(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            return segmenter != null &&
                   segmenter.IsReady &&
                   segmenter.KeypointBuffer != null &&
                   component.HasGraphicsBuffer(_keypointProperty);
#else
            return false;
#endif
        }

        public override void UpdateBinding(VisualEffect component)
        {
#if BODYPIX_AVAILABLE
            if (segmenter == null || !segmenter.IsReady || segmenter.KeypointBuffer == null)
                return;

            component.SetGraphicsBuffer(_keypointProperty, segmenter.KeypointBuffer);

            if (verboseLogging && Time.frameCount % 120 == 0)
            {
                Debug.Log($"[NNCamKeypointBinder] Bound KeypointBuffer to {component.name}");
            }
#endif
        }

        public override string ToString()
        {
#if BODYPIX_AVAILABLE
            string status = segmenter != null && segmenter.IsReady ? "Ready" : "Not Ready";
            return $"NNCam Keypoint : {_keypointProperty} ({status})";
#else
            return "NNCam Keypoint : BODYPIX_AVAILABLE not defined";
#endif
        }
    }
}
