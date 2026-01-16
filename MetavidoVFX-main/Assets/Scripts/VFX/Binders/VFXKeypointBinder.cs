// VFX Keypoint Binder - Binds BodyPartSegmenter keypoints to VFX Graph
// Based on NNCam2's BodyPixKeypointBinder pattern
// Compatible with Get Keypoint VFX operator for body joint effects

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace MetavidoVFX.VFX.Binders
{
    /// <summary>
    /// Binds the KeypointBuffer from BodyPartSegmenter to VFX Graph.
    /// Use with "Get Keypoint" VFX operator to access individual joint positions.
    ///
    /// Keypoint indices (17 total):
    /// 0: Nose, 1: Left Eye, 2: Right Eye, 3: Left Ear, 4: Right Ear
    /// 5: Left Shoulder, 6: Right Shoulder, 7: Left Elbow, 8: Right Elbow
    /// 9: Left Wrist, 10: Right Wrist, 11: Left Hip, 12: Right Hip
    /// 13: Left Knee, 14: Right Knee, 15: Left Ankle, 16: Right Ankle
    /// </summary>
    [AddComponentMenu("VFX/Property Binders/MetavidoVFX/Keypoint Binder")]
    [VFXBinder("MetavidoVFX/Keypoint")]
    public class VFXKeypointBinder : VFXBinderBase
    {
        public string Property
        {
            get => (string)_property;
            set => _property = value;
        }

        [VFXPropertyBinding("GraphicsBuffer"), SerializeField]
        ExposedProperty _property = "KeypointBuffer";

#if BODYPIX_AVAILABLE
        [Tooltip("Reference to BodyPartSegmenter that provides keypoint data")]
        public Segmentation.BodyPartSegmenter Target = null;

        public override bool IsValid(VisualEffect component)
            => Target != null &&
               Target.KeypointBuffer != null &&
               component.HasGraphicsBuffer(_property);

        public override void UpdateBinding(VisualEffect component)
        {
            if (Target != null && Target.KeypointBuffer != null)
            {
                component.SetGraphicsBuffer(_property, Target.KeypointBuffer);
            }
        }

        public override string ToString()
            => $"Keypoints : '{_property}' -> {Target?.name ?? "(null)"}";
#else
        public override bool IsValid(VisualEffect component) => false;
        public override void UpdateBinding(VisualEffect component) { }
        public override string ToString() => "Keypoints : BODYPIX_AVAILABLE not defined";
#endif
    }
}
