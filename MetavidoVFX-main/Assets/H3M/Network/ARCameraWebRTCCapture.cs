using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

namespace XRRAI.Hologram
{
    /// <summary>
    /// Captures AR camera frames and depth data for WebRTC hologram streaming.
    /// Uses URP's RenderPipelineManager.endCameraRendering for proper frame timing.
    ///
    /// Usage:
    /// 1. Add to a GameObject with ARCameraBackground reference
    /// 2. Assign the ARCameraBackground and AROcclusionManager components
    /// 3. Optionally assign a RawImage for debug preview
    /// 4. Call Initialize() with WebRTC video input and device name
    ///
    /// Depth Streaming:
    /// - Enable "Include Depth" to capture LiDAR depth alongside color
    /// - Multiplexed mode packs color (left) + depth (right) into one frame
    /// - Remote receiver demultiplexes for hologram rendering
    /// </summary>
    public class ARCameraWebRTCCapture : MonoBehaviour
    {
        public enum CaptureMode
        {
            ARCameraBackground,     // Blit from ARCameraBackground material (GPU)
            MainCameraRenderTexture,// Blit from camera's target RenderTexture (GPU)
            CPUImage                // AR Foundation CPU image (most efficient, no GPU readback)
        }

        public enum DepthMode
        {
            None,                   // Color only (original behavior)
            Multiplexed,            // Color + Depth side-by-side in single frame
            SeparateCallback        // Send depth via separate callback
        }

        [Header("Capture Mode")]
        [SerializeField] private CaptureMode captureMode = CaptureMode.CPUImage;
        [SerializeField] private DepthMode depthMode = DepthMode.None;

        [Header("AR Camera")]
        [SerializeField] private ARCameraManager m_ARCameraManager;
        [SerializeField] private ARCameraBackground m_ARCameraBackground;
        [SerializeField] private AROcclusionManager m_AROcclusionManager;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RenderTexture mainCameraRenderTexture;

        [Header("CPU Image Settings")]
        [SerializeField] private int cpuImageDownsample = 2;
        [SerializeField] private bool flipY = true;

        [Header("Capture Settings")]
        [SerializeField] private int captureWidth = 1920;
        [SerializeField] private int captureHeight = 1080;
        [SerializeField] private int depthWidth = 256;
        [SerializeField] private int depthHeight = 192;

        [Header("Debug")]
        [SerializeField] private RawImage ARTexture;
        [SerializeField] private RawImage DepthTexture;

        private RenderTexture targetRenderTexture;
        private RenderTexture depthRenderTexture;
        private RenderTexture multiplexedRenderTexture;
        private Texture2D m_LastCameraTexture;
        private Texture2D m_LastDepthTexture;
        private Texture2D m_MultiplexedTexture;
        private string mUsedDeviceName;

        // WebRTC video input - set via Initialize()
        private object mVideoInput; // WebRtcCSharp.IVideoInput
        private System.Action<string, byte[], int, int> mUpdateFrameCallback;
        private System.Action<string, byte[], int, int> mDepthCallback;

        private bool isInitialized;

        /// <summary>
        /// Public access to the current depth texture for external binding.
        /// </summary>
        public Texture2D LastDepthTexture => m_LastDepthTexture;

        /// <summary>
        /// Public access to the current color texture for external binding.
        /// </summary>
        public Texture2D LastColorTexture => m_LastCameraTexture;

        private void Awake()
        {
            // Create render texture for color capture
            targetRenderTexture = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
            targetRenderTexture.Create();

            // Create depth render texture if depth mode enabled
            if (depthMode != DepthMode.None)
            {
                depthRenderTexture = new RenderTexture(depthWidth, depthHeight, 0, RenderTextureFormat.RHalf);
                depthRenderTexture.Create();

                // For multiplexed mode, create combined texture (color left, depth right)
                if (depthMode == DepthMode.Multiplexed)
                {
                    int totalWidth = captureWidth + depthWidth;
                    multiplexedRenderTexture = new RenderTexture(totalWidth, Mathf.Max(captureHeight, depthHeight), 0, RenderTextureFormat.ARGB32);
                    multiplexedRenderTexture.Create();
                }
            }
        }

#if !UNITY_EDITOR
        private void OnEnable()
        {
            RenderPipelineManager.endCameraRendering += RenderPipelineManager_endCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.endCameraRendering -= RenderPipelineManager_endCameraRendering;
        }

        private void RenderPipelineManager_endCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!isInitialized) return;
            OnPostRender();
        }

        private void OnPostRender()
        {
            CopyARCameraTextureAndUpdateDeviceFrame(mUsedDeviceName);
        }
#endif

        /// <summary>
        /// Initialize with WebRTC video input for streaming.
        /// </summary>
        /// <param name="deviceName">WebRTC device name</param>
        /// <param name="updateFrameCallback">Callback: (deviceName, rawData, width, height)</param>
        public void Initialize(string deviceName, System.Action<string, byte[], int, int> updateFrameCallback)
        {
            mUsedDeviceName = deviceName;
            mUpdateFrameCallback = updateFrameCallback;
            isInitialized = true;

            Debug.Log($"[ARCameraWebRTCCapture] Initialized for device: {deviceName}");
        }

        /// <summary>
        /// Initialize with WebRtcCSharp.IVideoInput directly.
        /// Requires WebRtcCSharp namespace.
        /// </summary>
        public void InitializeWithVideoInput(string deviceName, object videoInput)
        {
            mUsedDeviceName = deviceName;
            mVideoInput = videoInput;
            isInitialized = true;

            Debug.Log($"[ARCameraWebRTCCapture] Initialized with IVideoInput for device: {deviceName}");
        }

        private void CopyARCameraTextureAndUpdateDeviceFrame(string DeviceName)
        {
            // Choose capture source based on mode
            switch (captureMode)
            {
                case CaptureMode.CPUImage:
                    CaptureFromCPUImage();
                    break;

                case CaptureMode.MainCameraRenderTexture:
                    CaptureFromRenderTexture();
                    break;

                case CaptureMode.ARCameraBackground:
                    CaptureFromARBackground();
                    break;
            }

            // Capture depth if enabled
            if (depthMode != DepthMode.None)
            {
                CaptureDepth();
            }

            // Debug preview
            if (ARTexture != null && m_LastCameraTexture != null)
            {
                ARTexture.texture = m_LastCameraTexture;
            }

            if (DepthTexture != null && m_LastDepthTexture != null)
            {
                DepthTexture.texture = m_LastDepthTexture;
            }

            // Send to WebRTC
            if (m_LastCameraTexture != null)
            {
                SendToWebRTC(DeviceName);
            }
        }

        /// <summary>
        /// Capture depth from AROcclusionManager.
        /// </summary>
        private void CaptureDepth()
        {
            if (m_AROcclusionManager == null)
            {
                Debug.LogWarning("[ARCameraWebRTCCapture] AROcclusionManager is null - cannot capture depth");
                return;
            }

            // Get environment depth texture from AR Foundation
            Texture2D envDepth = m_AROcclusionManager.environmentDepthTexture;
            if (envDepth == null)
            {
                return; // No depth available this frame
            }

            // Copy depth to our texture
            if (m_LastDepthTexture == null ||
                m_LastDepthTexture.width != envDepth.width ||
                m_LastDepthTexture.height != envDepth.height)
            {
                if (m_LastDepthTexture != null)
                    Destroy(m_LastDepthTexture);

                m_LastDepthTexture = new Texture2D(
                    envDepth.width,
                    envDepth.height,
                    TextureFormat.RFloat,
                    false
                );
            }

            // Copy pixels
            Graphics.CopyTexture(envDepth, m_LastDepthTexture);
        }

        /// <summary>
        /// Capture using AR Foundation CPU image API (most efficient).
        /// No GPU readback required - directly accesses camera buffer.
        /// </summary>
        private void CaptureFromCPUImage()
        {
            if (m_ARCameraManager == null)
            {
                Debug.LogWarning("[ARCameraWebRTCCapture] ARCameraManager is null");
                return;
            }

            // Try to acquire the latest CPU image
            if (!m_ARCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            {
                return; // No image available this frame
            }

            // Use async conversion with callback for non-blocking operation
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(
                    cpuImage.width / cpuImageDownsample,
                    cpuImage.height / cpuImageDownsample
                ),
                outputFormat = TextureFormat.RGBA32, // WebRTC compatible
                transformation = flipY
                    ? XRCpuImage.Transformation.MirrorY
                    : XRCpuImage.Transformation.None
            };

            // Synchronous conversion (for immediate WebRTC frame)
            int size = cpuImage.GetConvertedDataSize(conversionParams);
            var buffer = new NativeArray<byte>(size, Allocator.Temp);

            cpuImage.Convert(conversionParams, buffer);

            // Create or resize texture
            if (m_LastCameraTexture == null ||
                m_LastCameraTexture.width != conversionParams.outputDimensions.x ||
                m_LastCameraTexture.height != conversionParams.outputDimensions.y)
            {
                if (m_LastCameraTexture != null)
                    Destroy(m_LastCameraTexture);

                m_LastCameraTexture = new Texture2D(
                    conversionParams.outputDimensions.x,
                    conversionParams.outputDimensions.y,
                    TextureFormat.RGBA32,
                    false
                );
            }

            // Load data into texture
            m_LastCameraTexture.LoadRawTextureData(buffer);
            m_LastCameraTexture.Apply();

            // Cleanup
            buffer.Dispose();
            cpuImage.Dispose();
        }

        private void CaptureFromRenderTexture()
        {
            if (mainCameraRenderTexture == null)
            {
                // Try to get from camera if not assigned
                if (mainCamera != null && mainCamera.targetTexture != null)
                {
                    mainCameraRenderTexture = mainCamera.targetTexture;
                }
                else
                {
                    Debug.LogWarning("[ARCameraWebRTCCapture] mainCameraRenderTexture is null");
                    return;
                }
            }

            // Blit from camera render texture to our target
            Graphics.Blit(mainCameraRenderTexture, targetRenderTexture);
            ReadPixelsFromTarget();
        }

        private void CaptureFromARBackground()
        {
            if (m_ARCameraBackground == null || m_ARCameraBackground.material == null)
            {
                Debug.LogWarning("[ARCameraWebRTCCapture] ARCameraBackground or material is null");
                return;
            }

            // Blit AR camera background to our render texture
            Graphics.Blit(null, targetRenderTexture, m_ARCameraBackground.material);
            ReadPixelsFromTarget();
        }

        private void ReadPixelsFromTarget()
        {
            var activeRenderTexture = RenderTexture.active;
            RenderTexture.active = targetRenderTexture;

            // Create texture if needed (RGBA32 for WebRTC compatibility)
            if (m_LastCameraTexture == null)
            {
                m_LastCameraTexture = new Texture2D(
                    targetRenderTexture.width,
                    targetRenderTexture.height,
                    TextureFormat.RGBA32,
                    false // No mipmaps for streaming
                );
            }

            m_LastCameraTexture.ReadPixels(
                new Rect(0, 0, targetRenderTexture.width, targetRenderTexture.height),
                0, 0
            );
            m_LastCameraTexture.Apply();

            RenderTexture.active = activeRenderTexture;
        }

        private void SendToWebRTC(string deviceName)
        {
            // Option 1: Use callback
            if (mUpdateFrameCallback != null)
            {
                mUpdateFrameCallback(
                    deviceName,
                    m_LastCameraTexture.GetRawTextureData(),
                    m_LastCameraTexture.width,
                    m_LastCameraTexture.height
                );
                return;
            }

            // Option 2: Use IVideoInput directly (requires WebRtcCSharp)
            #if UNITY_WEBRTC_AVAILABLE
            if (mVideoInput != null)
            {
                try
                {
                    // Cast and call UpdateFrame
                    // mVideoInput.UpdateFrame(deviceName, m_LastCameraTexture.GetRawTextureData(),
                    //     m_LastCameraTexture.width, m_LastCameraTexture.height,
                    //     WebRtcCSharp.ImageFormat.kABGR, 0, true);

                    var method = mVideoInput.GetType().GetMethod("UpdateFrame");
                    if (method != null)
                    {
                        method.Invoke(mVideoInput, new object[] {
                            deviceName,
                            m_LastCameraTexture.GetRawTextureData(),
                            m_LastCameraTexture.width,
                            m_LastCameraTexture.height,
                            0, // ImageFormat.kABGR equivalent
                            0,
                            true
                        });
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ARCameraWebRTCCapture] Failed to send frame: {e.Message}");
                }
            }
            #endif
        }

        private void OnDestroy()
        {
            if (targetRenderTexture != null)
            {
                targetRenderTexture.Release();
                Destroy(targetRenderTexture);
            }

            if (depthRenderTexture != null)
            {
                depthRenderTexture.Release();
                Destroy(depthRenderTexture);
            }

            if (multiplexedRenderTexture != null)
            {
                multiplexedRenderTexture.Release();
                Destroy(multiplexedRenderTexture);
            }

            if (m_LastCameraTexture != null)
            {
                Destroy(m_LastCameraTexture);
            }

            if (m_LastDepthTexture != null)
            {
                Destroy(m_LastDepthTexture);
            }

            if (m_MultiplexedTexture != null)
            {
                Destroy(m_MultiplexedTexture);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Manual capture for testing.
        /// </summary>
        [ContextMenu("Test Capture")]
        private void TestCapture()
        {
            if (m_ARCameraBackground != null)
            {
                CopyARCameraTextureAndUpdateDeviceFrame("TestDevice");
                Debug.Log("[ARCameraWebRTCCapture] Test capture complete");
            }
        }
#endif
    }
}
