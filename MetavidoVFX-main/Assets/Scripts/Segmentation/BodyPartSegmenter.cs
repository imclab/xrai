// BodyPartSegmenter.cs
// Integrates BodyPixSentis ML model for 24-part body segmentation
// Outputs: MaskTexture (24 body parts), Keypoints (17 pose landmarks)

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using MetavidoVFX.VFX;

#if BODYPIX_AVAILABLE
using BodyPix;
#endif

namespace MetavidoVFX.Segmentation
{
    /// <summary>
    /// Provides 24-part body segmentation using BodyPixSentis ML model.
    /// Outputs can be bound to VFX Graph for segment-specific effects.
    ///
    /// Body Parts (24 total):
    /// - Face: LeftFace, RightFace
    /// - Arms: LeftUpperArmFront/Back, RightUpperArmFront/Back,
    ///         LeftLowerArmFront/Back, RightLowerArmFront/Back
    /// - Hands: LeftHand, RightHand
    /// - Torso: TorsoFront, TorsoBack
    /// - Legs: LeftUpperLegFront/Back, RightUpperLegFront/Back,
    ///         LeftLowerLegFront/Back, RightLowerLegFront/Back
    /// - Feet: LeftFeet, RightFeet
    /// </summary>
    public class BodyPartSegmenter : MonoBehaviour
    {
        void Log(string msg) { if (!VFXBinderManager.SuppressBodySegmenterLogs) Debug.Log(msg); }

        [Header("Input Source")]
        [SerializeField] private ARCameraBackground _arCameraBackground;
        [SerializeField] private Camera _arCamera;

        [Header("BodyPix Configuration")]
#if BODYPIX_AVAILABLE
        [SerializeField] private ResourceSet _resourceSet;
#endif
        [SerializeField] private int _inferenceWidth = 512;
        [SerializeField] private int _inferenceHeight = 384;

        [Header("Output")]
        [SerializeField] private bool _debugVisualization = false;

        [Header("Performance")]
        [SerializeField] private bool _runEveryFrame = true;
        [SerializeField] private int _skipFrames = 0;

#if BODYPIX_AVAILABLE
        private BodyDetector _detector;
#endif
        private RenderTexture _cameraCapture;
        private int _frameCounter;
        private bool _initialized;

        // Public outputs
        public RenderTexture MaskTexture { get; private set; }
        public GraphicsBuffer KeypointBuffer { get; private set; }
        public bool IsReady => _initialized;

        // Cached keypoint data for CPU access
        private Vector3[] _keypointPositions = new Vector3[17];
        private float[] _keypointScores = new float[17];

        /// <summary>
        /// Get world position of a keypoint (requires depth texture for 3D)
        /// </summary>
        public Vector3 GetKeypointPosition(int index)
        {
            if (index < 0 || index >= 17) return Vector3.zero;
            return _keypointPositions[index];
        }

        /// <summary>
        /// Get confidence score of a keypoint (0-1)
        /// </summary>
        public float GetKeypointScore(int index)
        {
            if (index < 0 || index >= 17) return 0f;
            return _keypointScores[index];
        }

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
#if BODYPIX_AVAILABLE
            if (_resourceSet == null)
            {
                Debug.LogWarning("[BodyPartSegmenter] ResourceSet not assigned. Disabling component.");
                Debug.LogWarning("[BodyPartSegmenter] To enable: Drag ResourceSet from Packages/jp.keijiro.bodypix/ResourceSet/ to Inspector");
                enabled = false;
                return;
            }

            // Validate ResourceSet has required references
            if (_resourceSet.model == null || _resourceSet.preprocess == null)
            {
                Debug.LogWarning("[BodyPartSegmenter] ResourceSet missing model or shaders. Use asset from package, not copied.");
                enabled = false;
                return;
            }

            // Find AR components if not assigned
            if (_arCameraBackground == null)
                _arCameraBackground = FindFirstObjectByType<ARCameraBackground>();
            if (_arCamera == null)
                _arCamera = _arCameraBackground?.GetComponent<Camera>();

            if (_arCameraBackground == null)
            {
                Debug.LogError("[BodyPartSegmenter] ARCameraBackground not found!");
                return;
            }

            // Create camera capture texture
            _cameraCapture = new RenderTexture(_inferenceWidth, _inferenceHeight, 0, RenderTextureFormat.ARGB32);
            _cameraCapture.Create();

            // Initialize BodyPix detector
            try
            {
                _detector = new BodyDetector(_resourceSet, _inferenceWidth, _inferenceHeight);
                MaskTexture = _detector.MaskTexture;
                KeypointBuffer = _detector.KeypointBuffer;
                _initialized = true;

                Log($"[BodyPartSegmenter] Initialized: {_inferenceWidth}x{_inferenceHeight}");
                Log($"[BodyPartSegmenter] Output mask: {MaskTexture.width}x{MaskTexture.height}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BodyPartSegmenter] Failed to initialize: {e.Message}");
            }
#else
            Debug.LogWarning("[BodyPartSegmenter] BodyPix package not available. Add BODYPIX_AVAILABLE to scripting defines.");
#endif
        }

        void Update()
        {
            if (!_initialized) return;

            // Frame skipping for performance
            if (!_runEveryFrame)
            {
                _frameCounter++;
                if (_frameCounter <= _skipFrames)
                    return;
                _frameCounter = 0;
            }

            ProcessFrame();
        }

        void ProcessFrame()
        {
#if BODYPIX_AVAILABLE
            if (_arCameraBackground == null || _arCameraBackground.material == null)
                return;

            // Capture camera frame
            Graphics.Blit(null, _cameraCapture, _arCameraBackground.material);

            // Run inference
            _detector.ProcessImage(_cameraCapture);

            // Cache keypoint data for CPU access
            CacheKeypointData();
#endif
        }

#if BODYPIX_AVAILABLE
        void CacheKeypointData()
        {
            var keypoints = _detector.Keypoints;
            for (int i = 0; i < keypoints.Length && i < 17; i++)
            {
                var kp = keypoints[i];
                // Convert from normalized coords to screen space
                _keypointPositions[i] = new Vector3(
                    kp.Position.x * Screen.width,
                    kp.Position.y * Screen.height,
                    0
                );
                _keypointScores[i] = kp.Score;
            }
        }
#endif

        void OnDestroy()
        {
#if BODYPIX_AVAILABLE
            _detector?.Dispose();
#endif
            if (_cameraCapture != null)
            {
                _cameraCapture.Release();
                Destroy(_cameraCapture);
            }
        }

        /// <summary>
        /// Push segmentation data to a VFX Graph
        /// </summary>
        public void PushToVFX(VisualEffect vfx)
        {
            if (vfx == null || !_initialized) return;

            // Body part mask (24-part segmentation)
            if (vfx.HasTexture("BodyPartMask"))
                vfx.SetTexture("BodyPartMask", MaskTexture);

            // Alternative names
            if (vfx.HasTexture("SegmentationMask"))
                vfx.SetTexture("SegmentationMask", MaskTexture);

            // Keypoint buffer for pose
            if (vfx.HasGraphicsBuffer("KeypointBuffer"))
                vfx.SetGraphicsBuffer("KeypointBuffer", KeypointBuffer);

            // Individual keypoints as Vector3 (for simpler VFX)
            PushKeypointsToVFX(vfx);
        }

        void PushKeypointsToVFX(VisualEffect vfx)
        {
            // Pose keypoints (17 total)
            TrySetKeypoint(vfx, "NosePosition", 0);
            TrySetKeypoint(vfx, "LeftEyePosition", 1);
            TrySetKeypoint(vfx, "RightEyePosition", 2);
            TrySetKeypoint(vfx, "LeftEarPosition", 3);
            TrySetKeypoint(vfx, "RightEarPosition", 4);
            TrySetKeypoint(vfx, "LeftShoulderPosition", 5);
            TrySetKeypoint(vfx, "RightShoulderPosition", 6);
            TrySetKeypoint(vfx, "LeftElbowPosition", 7);
            TrySetKeypoint(vfx, "RightElbowPosition", 8);
            TrySetKeypoint(vfx, "LeftWristPosition", 9);
            TrySetKeypoint(vfx, "RightWristPosition", 10);
            TrySetKeypoint(vfx, "LeftHipPosition", 11);
            TrySetKeypoint(vfx, "RightHipPosition", 12);
            TrySetKeypoint(vfx, "LeftKneePosition", 13);
            TrySetKeypoint(vfx, "RightKneePosition", 14);
            TrySetKeypoint(vfx, "LeftAnklePosition", 15);
            TrySetKeypoint(vfx, "RightAnklePosition", 16);
        }

        void TrySetKeypoint(VisualEffect vfx, string propertyName, int index)
        {
            if (vfx.HasVector3(propertyName))
                vfx.SetVector3(propertyName, _keypointPositions[index]);

            string scoreName = propertyName.Replace("Position", "Score");
            if (vfx.HasFloat(scoreName))
                vfx.SetFloat(scoreName, _keypointScores[index]);
        }

        void OnGUI()
        {
            if (!_debugVisualization || !_initialized || MaskTexture == null) return;

            // Draw mask texture in corner for debugging
            float size = 200;
            GUI.DrawTexture(new Rect(10, 10, size, size * 0.75f), MaskTexture);

            // Draw keypoint info
            GUI.Label(new Rect(10, size * 0.75f + 20, 300, 200),
                $"BodyPix Active\nKeypoints: {GetValidKeypointCount()}/17\nMask: {MaskTexture.width}x{MaskTexture.height}");
        }

        int GetValidKeypointCount()
        {
            int count = 0;
            for (int i = 0; i < 17; i++)
            {
                if (_keypointScores[i] > 0.5f) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Body part indices for the 24-part segmentation mask.
    /// Use these to sample specific body parts from the MaskTexture.
    /// Mask value at pixel = body part index (0-23) or 255 for background.
    /// </summary>
    public static class BodyPartIndex
    {
        public const int LeftFace = 0;
        public const int RightFace = 1;
        public const int LeftUpperArmFront = 2;
        public const int LeftUpperArmBack = 3;
        public const int RightUpperArmFront = 4;
        public const int RightUpperArmBack = 5;
        public const int LeftLowerArmFront = 6;
        public const int LeftLowerArmBack = 7;
        public const int RightLowerArmFront = 8;
        public const int RightLowerArmBack = 9;
        public const int LeftHand = 10;
        public const int RightHand = 11;
        public const int TorsoFront = 12;
        public const int TorsoBack = 13;
        public const int LeftUpperLegFront = 14;
        public const int LeftUpperLegBack = 15;
        public const int RightUpperLegFront = 16;
        public const int RightUpperLegBack = 17;
        public const int LeftLowerLegFront = 18;
        public const int LeftLowerLegBack = 19;
        public const int RightLowerLegFront = 20;
        public const int RightLowerLegBack = 21;
        public const int LeftFeet = 22;
        public const int RightFeet = 23;
        public const int Background = 255;

        // Grouped accessors
        public static readonly int[] Face = { LeftFace, RightFace };
        public static readonly int[] Hands = { LeftHand, RightHand };
        public static readonly int[] Arms = {
            LeftUpperArmFront, LeftUpperArmBack, RightUpperArmFront, RightUpperArmBack,
            LeftLowerArmFront, LeftLowerArmBack, RightLowerArmFront, RightLowerArmBack
        };
        public static readonly int[] Torso = { TorsoFront, TorsoBack };
        public static readonly int[] Legs = {
            LeftUpperLegFront, LeftUpperLegBack, RightUpperLegFront, RightUpperLegBack,
            LeftLowerLegFront, LeftLowerLegBack, RightLowerLegFront, RightLowerLegBack
        };
        public static readonly int[] Feet = { LeftFeet, RightFeet };
    }

    /// <summary>
    /// Keypoint indices for the 17-point pose estimation.
    /// </summary>
    public static class KeypointIndex
    {
        public const int Nose = 0;
        public const int LeftEye = 1;
        public const int RightEye = 2;
        public const int LeftEar = 3;
        public const int RightEar = 4;
        public const int LeftShoulder = 5;
        public const int RightShoulder = 6;
        public const int LeftElbow = 7;
        public const int RightElbow = 8;
        public const int LeftWrist = 9;
        public const int RightWrist = 10;
        public const int LeftHip = 11;
        public const int RightHip = 12;
        public const int LeftKnee = 13;
        public const int RightKnee = 14;
        public const int LeftAnkle = 15;
        public const int RightAnkle = 16;
    }
}
