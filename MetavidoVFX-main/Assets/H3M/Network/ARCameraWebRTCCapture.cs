using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

namespace H3M.Network
{
    /// <summary>
    /// Captures AR camera frames and sends them to WebRTC for video streaming.
    /// Uses URP's RenderPipelineManager.endCameraRendering for proper frame timing.
    ///
    /// Usage:
    /// 1. Add to a GameObject with ARCameraBackground reference
    /// 2. Assign the ARCameraBackground component
    /// 3. Optionally assign a RawImage for debug preview
    /// 4. Call Initialize() with WebRTC video input and device name
    /// </summary>
    public class ARCameraWebRTCCapture : MonoBehaviour
    {
        public enum CaptureMode
        {
            ARCameraBackground,     // Blit from ARCameraBackground material (GPU)
            MainCameraRenderTexture,// Blit from camera's target RenderTexture (GPU)
            CPUImage                // AR Foundation CPU image (most efficient, no GPU readback)
        }

        [Header("Capture Mode")]
        [SerializeField] private CaptureMode captureMode = CaptureMode.CPUImage;

        [Header("AR Camera")]
        [SerializeField] private ARCameraManager m_ARCameraManager;
        [SerializeField] private ARCameraBackground m_ARCameraBackground;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RenderTexture mainCameraRenderTexture;

        [Header("CPU Image Settings")]
        [SerializeField] private int cpuImageDownsample = 2;
        [SerializeField] private bool flipY = true;

        [Header("Capture Settings")]
        [SerializeField] private int captureWidth = 1920;
        [SerializeField] private int captureHeight = 1080;

        [Header("Debug")]
        [SerializeField] private RawImage ARTexture;

        private RenderTexture targetRenderTexture;
        private Texture2D m_LastCameraTexture;
        private string mUsedDeviceName;

        // WebRTC video input - set via Initialize()
        private object mVideoInput; // WebRtcCSharp.IVideoInput
        private System.Action<string, byte[], int, int> mUpdateFrameCallback;

        private bool isInitialized;

        private void Awake()
        {
            // Create render texture for capture
            targetRenderTexture = new RenderTexture(captureWidth, captureHeight, 0, RenderTextureFormat.ARGB32);
            targetRenderTexture.Create();
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

            // Debug preview
            if (ARTexture != null && m_LastCameraTexture != null)
            {
                ARTexture.texture = m_LastCameraTexture;
            }

            // Send to WebRTC
            if (m_LastCameraTexture != null)
            {
                SendToWebRTC(DeviceName);
            }
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

            if (m_LastCameraTexture != null)
            {
                Destroy(m_LastCameraTexture);
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
