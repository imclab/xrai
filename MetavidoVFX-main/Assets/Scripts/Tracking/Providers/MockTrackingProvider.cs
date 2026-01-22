// MockTrackingProvider - Simulated tracking for Editor testing (spec-008)
// PRIORITY: -100 (lowest - only used when no real providers available)
// Enables zero-hardware testing, CI/CD, and rapid iteration

using System;
using UnityEngine;

namespace MetavidoVFX.Tracking.Providers
{
    /// <summary>
    /// Mock tracking provider for Editor testing.
    ///
    /// Features:
    /// - Procedural body pose animation (idle sway)
    /// - Simulated depth/stencil textures
    /// - Keyboard controls for pose manipulation
    /// - Recording playback (TrackingSimulator integration)
    ///
    /// Usage:
    /// - Auto-activates in Editor when no real providers available
    /// - Press F1 to force-select in runtime
    /// - Use TrackingManager.ForceProvider<MockTrackingProvider>()
    /// </summary>
    [TrackingProvider("mock", priority: -100)]
    public class MockTrackingProvider : ITrackingProvider
    {
        public string Id => "mock";
        public int Priority => -100;  // Lowest priority
        public Platform SupportedPlatforms => Platform.Editor | Platform.All;
        public TrackingCap Capabilities => TrackingCap.Body | TrackingCap.Depth | TrackingCap.Keypoints;

        private BodyPoseData _bodyData;
        private DepthData _depthData;
        private bool _initialized;
        private float _animationTime;

        // Mock texture for depth simulation
        private Texture2D _mockDepthTexture;
        private Texture2D _mockStencilTexture;

        public bool IsAvailable => _initialized && (Application.isEditor || Debug.isDebugBuild);

        public event Action<TrackingCap> OnCapabilitiesChanged;
        public event Action OnTrackingLost;
        public event Action OnTrackingFound;

        public void Initialize()
        {
            // Create mock textures
            CreateMockTextures();

            // Initialize body pose with T-pose
            InitializeTPose();

            _initialized = true;
            Debug.Log("[MockTrackingProvider] Initialized - zero-hardware testing enabled");
            Debug.Log("  Controls: WASD = move, QE = rotate, Space = wave, R = reset");

            OnTrackingFound?.Invoke();
        }

        private void CreateMockTextures()
        {
            // Create simple depth texture (gradient)
            _mockDepthTexture = new Texture2D(256, 192, TextureFormat.RFloat, false);
            var depthPixels = new float[256 * 192];
            for (int y = 0; y < 192; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    // Simple depth gradient (0.5m to 3m)
                    depthPixels[y * 256 + x] = 0.5f + (y / 192f) * 2.5f;
                }
            }
            _mockDepthTexture.SetPixelData(depthPixels, 0);
            _mockDepthTexture.Apply();

            // Create simple stencil texture (centered ellipse for human)
            _mockStencilTexture = new Texture2D(256, 192, TextureFormat.R8, false);
            var stencilPixels = new byte[256 * 192];
            for (int y = 0; y < 192; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    float nx = (x - 128f) / 50f;
                    float ny = (y - 96f) / 80f;
                    bool isHuman = (nx * nx + ny * ny) < 1f;
                    stencilPixels[y * 256 + x] = isHuman ? (byte)255 : (byte)0;
                }
            }
            _mockStencilTexture.SetPixelData(stencilPixels, 0);
            _mockStencilTexture.Apply();
        }

        private void InitializeTPose()
        {
            _bodyData.Keypoints = new Vector3[17];
            _bodyData.Confidences = new float[17];

            // T-pose positions (centered at 0, 1.7m tall)
            _bodyData.Keypoints[BodyPoseData.Nose] = new Vector3(0, 1.7f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftEye] = new Vector3(-0.03f, 1.72f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightEye] = new Vector3(0.03f, 1.72f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftEar] = new Vector3(-0.08f, 1.7f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightEar] = new Vector3(0.08f, 1.7f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftShoulder] = new Vector3(-0.2f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightShoulder] = new Vector3(0.2f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftElbow] = new Vector3(-0.45f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightElbow] = new Vector3(0.45f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftWrist] = new Vector3(-0.7f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightWrist] = new Vector3(0.7f, 1.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftHip] = new Vector3(-0.1f, 1.0f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightHip] = new Vector3(0.1f, 1.0f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftKnee] = new Vector3(-0.1f, 0.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightKnee] = new Vector3(0.1f, 0.5f, 2f);
            _bodyData.Keypoints[BodyPoseData.LeftAnkle] = new Vector3(-0.1f, 0.05f, 2f);
            _bodyData.Keypoints[BodyPoseData.RightAnkle] = new Vector3(0.1f, 0.05f, 2f);

            // All joints fully confident
            for (int i = 0; i < 17; i++)
            {
                _bodyData.Confidences[i] = 1f;
            }

            _bodyData.IsTracked = true;
            _bodyData.Timestamp = Time.time;
        }

        public void Update()
        {
            if (!_initialized) return;

            _animationTime += Time.deltaTime;

            // Animate idle sway
            AnimateIdleSway();

            // Handle keyboard input (Editor only)
#if UNITY_EDITOR
            HandleKeyboardInput();
#endif

            // Update depth data
            _depthData.DepthTexture = _mockDepthTexture;
            _depthData.StencilTexture = _mockStencilTexture;
            _depthData.InverseView = Matrix4x4.identity;
            _depthData.RayParams = new Vector4(0, 0, 1f, 0.75f);
            _depthData.DepthRange = new Vector2(0.1f, 10f);
            _depthData.Width = 256;
            _depthData.Height = 192;
            _depthData.IsValid = true;
            _depthData.Timestamp = Time.time;

            _bodyData.Timestamp = Time.time;
        }

        private void AnimateIdleSway()
        {
            // Subtle breathing/sway animation
            float breathOffset = Mathf.Sin(_animationTime * 0.5f) * 0.01f;
            float swayOffset = Mathf.Sin(_animationTime * 0.3f) * 0.02f;

            // Apply to torso joints
            for (int i = 0; i < 17; i++)
            {
                var pos = _bodyData.Keypoints[i];

                // Breathing (Y offset for upper body)
                if (i <= BodyPoseData.RightWrist)
                {
                    pos.y += breathOffset;
                }

                // Sway (X offset for all)
                pos.x += swayOffset * (1f - (i / 17f) * 0.5f);

                _bodyData.Keypoints[i] = pos;
            }
        }

        private void HandleKeyboardInput()
        {
            if (!Application.isPlaying) return;

            float moveSpeed = 0.05f;
            float rotateSpeed = 5f;

            // Movement (WASD)
            Vector3 offset = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) offset.z -= moveSpeed;
            if (Input.GetKey(KeyCode.S)) offset.z += moveSpeed;
            if (Input.GetKey(KeyCode.A)) offset.x -= moveSpeed;
            if (Input.GetKey(KeyCode.D)) offset.x += moveSpeed;

            // Apply offset to all joints
            if (offset != Vector3.zero)
            {
                for (int i = 0; i < 17; i++)
                {
                    _bodyData.Keypoints[i] += offset;
                }
            }

            // Wave animation (Space)
            if (Input.GetKey(KeyCode.Space))
            {
                float wave = Mathf.Sin(_animationTime * 8f) * 0.3f;
                _bodyData.Keypoints[BodyPoseData.RightWrist] += new Vector3(0, wave, 0);
                _bodyData.Keypoints[BodyPoseData.RightElbow] += new Vector3(0, wave * 0.5f, 0);
            }

            // Reset (R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                InitializeTPose();
                Debug.Log("[MockTrackingProvider] Reset to T-pose");
            }
        }

        public void Shutdown()
        {
            if (_mockDepthTexture != null)
            {
                UnityEngine.Object.Destroy(_mockDepthTexture);
                _mockDepthTexture = null;
            }

            if (_mockStencilTexture != null)
            {
                UnityEngine.Object.Destroy(_mockStencilTexture);
                _mockStencilTexture = null;
            }

            _initialized = false;
        }

        public bool TryGetData<T>(out T data) where T : struct, ITrackingData
        {
            if (typeof(T) == typeof(BodyPoseData) && _bodyData.IsTracked)
            {
                data = (T)(object)_bodyData;
                return true;
            }

            if (typeof(T) == typeof(DepthData) && _depthData.IsValid)
            {
                data = (T)(object)_depthData;
                return true;
            }

            data = default;
            return false;
        }

        /// <summary>
        /// Set custom pose data (for recorded session playback).
        /// </summary>
        public void SetPoseData(Vector3[] keypoints, float[] confidences)
        {
            if (keypoints.Length >= 17)
            {
                _bodyData.Keypoints = keypoints;
                _bodyData.Confidences = confidences ?? new float[17];
                _bodyData.IsTracked = true;
                _bodyData.Timestamp = Time.time;
            }
        }

        /// <summary>
        /// Trigger a simulated gesture.
        /// </summary>
        public void SimulateGesture(string gesture)
        {
            switch (gesture.ToLower())
            {
                case "wave":
                    // Will be animated in Update
                    Debug.Log("[MockTrackingProvider] Simulating wave gesture");
                    break;

                case "tpose":
                    InitializeTPose();
                    Debug.Log("[MockTrackingProvider] Reset to T-pose");
                    break;

                default:
                    Debug.LogWarning($"[MockTrackingProvider] Unknown gesture: {gesture}");
                    break;
            }
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
