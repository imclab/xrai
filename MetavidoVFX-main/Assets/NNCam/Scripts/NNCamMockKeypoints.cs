// Mock keypoint data for Editor testing
// Provides fake face keypoints when BodyPartSegmenter isn't ready

using UnityEngine;
using UnityEngine.VFX;

namespace MetavidoVFX.NNCam
{
    /// <summary>
    /// Provides mock keypoint data for Editor testing.
    /// Simulates a face in the center of the screen.
    /// </summary>
    [ExecuteAlways]
    public class NNCamMockKeypoints : MonoBehaviour
    {
        [Header("Target")]
        public VisualEffect targetVFX;

        [Header("Mock Settings")]
        [Range(0f, 1f)] public float confidence = 0.9f;
        public bool animatePosition = true;
        public float animationSpeed = 0.5f;

        GraphicsBuffer _keypointBuffer;
        Vector4[] _keypointData;
        const int KEYPOINT_COUNT = 17;

        // COCO keypoint indices
        const int NOSE = 0, LEFT_EYE = 1, RIGHT_EYE = 2;
        const int LEFT_EAR = 3, RIGHT_EAR = 4;

        void OnEnable()
        {
            if (targetVFX == null)
                targetVFX = GetComponent<VisualEffect>();

            _keypointBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                KEYPOINT_COUNT,
                sizeof(float) * 4
            );
            _keypointData = new Vector4[KEYPOINT_COUNT];
        }

        void OnDisable()
        {
            _keypointBuffer?.Release();
            _keypointBuffer = null;
        }

        void Update()
        {
            if (targetVFX == null || _keypointBuffer == null) return;

#if BODYPIX_AVAILABLE
            // Skip if real segmenter is ready
            var segmenter = FindFirstObjectByType<MetavidoVFX.Segmentation.BodyPartSegmenter>();
            if (segmenter != null && segmenter.IsReady) return;
#endif

            // Generate mock keypoints (UV coords 0-1)
            float t = animatePosition ? Time.time * animationSpeed : 0;
            float offsetX = Mathf.Sin(t) * 0.05f;
            float offsetY = Mathf.Cos(t * 0.7f) * 0.03f;

            // Face centered in screen
            float centerX = 0.5f + offsetX;
            float centerY = 0.5f + offsetY;

            // Nose
            _keypointData[NOSE] = new Vector4(centerX, centerY, confidence, 0);

            // Eyes (slightly above and to sides of nose)
            _keypointData[LEFT_EYE] = new Vector4(centerX - 0.03f, centerY + 0.02f, confidence, 0);
            _keypointData[RIGHT_EYE] = new Vector4(centerX + 0.03f, centerY + 0.02f, confidence, 0);

            // Ears
            _keypointData[LEFT_EAR] = new Vector4(centerX - 0.06f, centerY, confidence * 0.8f, 0);
            _keypointData[RIGHT_EAR] = new Vector4(centerX + 0.06f, centerY, confidence * 0.8f, 0);

            // Fill rest with low confidence (body keypoints)
            for (int i = 5; i < KEYPOINT_COUNT; i++)
            {
                _keypointData[i] = new Vector4(0.5f, 0.3f, 0.1f, 0);
            }

            _keypointBuffer.SetData(_keypointData);

            // Bind to VFX
            if (targetVFX.HasGraphicsBuffer("KeypointBuffer"))
            {
                targetVFX.SetGraphicsBuffer("KeypointBuffer", _keypointBuffer);
            }
        }
    }
}
