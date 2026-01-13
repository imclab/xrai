using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

namespace ARFoundationVFX
{
    /// <summary>
    /// Production-ready ARFoundation to VFX Graph bridge optimized for iOS
    /// No CPU readback, full GPU pipeline, tested on iPhone 12 Pro and newer
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [RequireComponent(typeof(ARCameraBackground))]
    [RequireComponent(typeof(AROcclusionManager))]
    public class ARFoundationVFXBridge : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("VFX Configuration")]
        [SerializeField] private VisualEffect vfxPrefab;
        [SerializeField] private ComputeShader depthProcessor;
        [SerializeField] private bool createVFXOnStart = true;
        
        [Header("Resolution Settings")]
        [SerializeField] private Vector2Int textureResolution = new Vector2Int(512, 512);
        [SerializeField] private bool matchScreenAspectRatio = true;
        
        [Header("Depth Processing")]
        [SerializeField] private float depthScale = 1.0f;
        [SerializeField] private float nearClipPlane = 0.1f;
        [SerializeField] private float farClipPlane = 10.0f;
        
        [Header("Performance")]
        [SerializeField] private bool enableAdaptiveQuality = true;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private float qualityUpdateInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private Material debugMaterial;
        
        #endregion
        
        #region Private Fields
        
        // AR Components
        private Camera arCamera;
        private ARCameraBackground cameraBackground;
        private AROcclusionManager occlusionManager;
        private ARCameraManager cameraManager;
        
        // VFX
        private VisualEffect vfxInstance;
        
        // Render Textures
        private RenderTexture positionTexture;
        private RenderTexture colorTexture;
        private RenderTexture velocityTexture;
        private RenderTexture normalTexture;
        private RenderTexture captureTexture;
        
        // Compute Shader
        private int kernelDepthToWorld;
        private int kernelProcessVelocity;
        private int kernelGenerateNormals;
        private ComputeBuffer matricesBuffer;
        
        // Thread Groups
        private int threadGroupsX;
        private int threadGroupsY;
        
        // Performance
        private float lastQualityUpdate;
        private int currentQualityLevel = 2; // 0-3
        private float averageDeltaTime;
        
        // State
        private bool isInitialized;
        private Matrix4x4 previousVPMatrix;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get required components
            arCamera = GetComponent<Camera>();
            cameraBackground = GetComponent<ARCameraBackground>();
            occlusionManager = GetComponent<AROcclusionManager>();
            cameraManager = GetComponentInParent<ARCameraManager>() ?? 
                          FindObjectOfType<ARCameraManager>();
            
            if (!ValidateComponents())
            {
                enabled = false;
                return;
            }
            
            // Configure for iOS
            ConfigureARForIOS();
        }
        
        private void OnEnable()
        {
            // Initialize on first frame to ensure AR session is ready
            StartCoroutine(InitializeDelayed());
        }
        
        private void OnDisable()
        {
            Cleanup();
        }
        
        private void Update()
        {
            if (!isInitialized) return;
            
            // Update performance metrics
            if (enableAdaptiveQuality)
            {
                UpdateQualityLevel();
            }
            
            // Process depth data
            ProcessDepthData();
        }
        
        private void LateUpdate()
        {
            if (!isInitialized) return;
            
            // Capture camera texture after all rendering
            CaptureBackgroundTexture();
        }
        
        #endregion
        
        #region Initialization
        
        private IEnumerator InitializeDelayed()
        {
            // Wait for AR session to be ready
            yield return new WaitForSeconds(0.5f);
            
            // Validate depth support
            var descriptor = occlusionManager.descriptor;
            if (descriptor == null)
            {
                Debug.LogError("Occlusion not supported on this device");
                enabled = false;
                yield break;
            }
            
            // Check for human segmentation support
            bool supportsHumanSegmentation = 
                descriptor.supportsHumanSegmentationStencilImage &&
                descriptor.supportsHumanSegmentationDepthImage;
            
            // Check for environment depth (LiDAR)
            bool supportsEnvironmentDepth = 
                descriptor.supportsEnvironmentDepthImage;
            
            if (!supportsHumanSegmentation && !supportsEnvironmentDepth)
            {
                Debug.LogError("Neither human segmentation nor environment depth supported");
                enabled = false;
                yield break;
            }
            
            Initialize();
        }
        
        private void Initialize()
        {
            // Setup render textures
            CreateRenderTextures();
            
            // Setup compute shader
            InitializeComputeShader();
            
            // Create VFX instance
            if (createVFXOnStart && vfxPrefab != null)
            {
                CreateVFXInstance();
            }
            
            isInitialized = true;
            Debug.Log("ARFoundationVFXBridge initialized successfully");
        }
        
        private void ConfigureARForIOS()
        {
            // Request fastest modes for best performance
            occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
            occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
            occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            
            // Enable temporal smoothing for stable results
            occlusionManager.environmentDepthTemporalSmoothingEnabled = true;
            
            // iOS-specific camera settings
            if (cameraManager != null)
            {
                cameraManager.requestedFacingDirection = CameraFacingDirection.World;
                cameraManager.autoFocusRequested = true;
            }
            
            // Performance settings
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
        
        private bool ValidateComponents()
        {
            if (depthProcessor == null)
            {
                Debug.LogError("Depth processor compute shader is not assigned!");
                return false;
            }
            
            if (vfxPrefab == null && createVFXOnStart)
            {
                Debug.LogWarning("VFX prefab not assigned, VFX instance will not be created");
            }
            
            return true;
        }
        
        #endregion
        
        #region Render Texture Management
        
        private void CreateRenderTextures()
        {
            // Calculate resolution with aspect ratio
            var resolution = textureResolution;
            if (matchScreenAspectRatio)
            {
                float aspectRatio = (float)Screen.width / Screen.height;
                resolution.x = textureResolution.x;
                resolution.y = Mathf.RoundToInt(resolution.x / aspectRatio);
            }
            
            // Position texture (world space positions)
            positionTexture = CreateRenderTexture(resolution, RenderTextureFormat.ARGBFloat);
            
            // Color texture (camera image masked by stencil)
            colorTexture = CreateRenderTexture(resolution, RenderTextureFormat.ARGB32);
            
            // Velocity texture (motion vectors)
            velocityTexture = CreateRenderTexture(resolution, RenderTextureFormat.ARGBHalf);
            
            // Normal texture (surface normals)
            normalTexture = CreateRenderTexture(resolution, RenderTextureFormat.ARGBHalf);
            
            // Capture texture (full camera image)
            captureTexture = new RenderTexture(Screen.width, Screen.height, 0);
            captureTexture.Create();
            
            // Calculate thread groups
            threadGroupsX = Mathf.CeilToInt(resolution.x / 8.0f);
            threadGroupsY = Mathf.CeilToInt(resolution.y / 8.0f);
        }
        
        private RenderTexture CreateRenderTexture(Vector2Int resolution, RenderTextureFormat format)
        {
            var rt = new RenderTexture(resolution.x, resolution.y, 0, format);
            rt.enableRandomWrite = true;
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.autoGenerateMips = false;
            rt.Create();
            return rt;
        }
        
        private void ResizeRenderTextures(Vector2Int newResolution)
        {
            // Release old textures
            ReleaseRenderTextures();
            
            // Update resolution
            textureResolution = newResolution;
            
            // Create new textures
            CreateRenderTextures();
            
            // Update VFX bindings
            if (vfxInstance != null)
            {
                BindTexturesToVFX(vfxInstance);
            }
        }
        
        #endregion
        
        #region Compute Shader Setup
        
        private void InitializeComputeShader()
        {
            // Find kernels
            kernelDepthToWorld = depthProcessor.FindKernel("DepthToWorld");
            kernelProcessVelocity = depthProcessor.FindKernel("ProcessVelocity");
            kernelGenerateNormals = depthProcessor.FindKernel("GenerateNormals");
            
            // Create matrices buffer
            matricesBuffer = new ComputeBuffer(3, sizeof(float) * 16);
            
            // Set constant parameters
            depthProcessor.SetFloat("_DepthScale", depthScale);
            depthProcessor.SetFloat("_NearPlane", nearClipPlane);
            depthProcessor.SetFloat("_FarPlane", farClipPlane);
            
            // Bind output textures
            depthProcessor.SetTexture(kernelDepthToWorld, "_PositionOutput", positionTexture);
            depthProcessor.SetTexture(kernelDepthToWorld, "_ColorOutput", colorTexture);
            depthProcessor.SetTexture(kernelProcessVelocity, "_VelocityOutput", velocityTexture);
            depthProcessor.SetTexture(kernelGenerateNormals, "_NormalOutput", normalTexture);
        }
        
        #endregion
        
        #region Depth Processing
        
        private void ProcessDepthData()
        {
            // Get depth textures
            var humanStencil = occlusionManager.humanStencilTexture;
            var humanDepth = occlusionManager.humanDepthTexture;
            var environmentDepth = occlusionManager.environmentDepthTexture;
            
            // Check if we have any depth data
            bool hasHumanData = humanStencil != null && humanDepth != null;
            bool hasEnvironmentData = environmentDepth != null;
            
            if (!hasHumanData && !hasEnvironmentData)
            {
                return;
            }
            
            // Update matrices
            UpdateMatrices();
            
            // Process depth to world positions
            ProcessDepthToWorld(humanStencil, humanDepth, environmentDepth);
            
            // Process velocity
            ProcessVelocity();
            
            // Generate normals
            GenerateNormals();
            
            // Update VFX
            if (vfxInstance != null)
            {
                vfxInstance.SendEvent("OnDepthUpdate");
            }
        }
        
        private void ProcessDepthToWorld(Texture humanStencil, Texture humanDepth, Texture environmentDepth)
        {
            // Bind input textures
            if (humanStencil != null)
                depthProcessor.SetTexture(kernelDepthToWorld, "_HumanStencil", humanStencil);
            
            if (humanDepth != null)
                depthProcessor.SetTexture(kernelDepthToWorld, "_HumanDepth", humanDepth);
            
            if (environmentDepth != null)
                depthProcessor.SetTexture(kernelDepthToWorld, "_EnvironmentDepth", environmentDepth);
            
            if (captureTexture != null)
                depthProcessor.SetTexture(kernelDepthToWorld, "_CameraTexture", captureTexture);
            
            // Set flags
            depthProcessor.SetBool("_HasHumanData", humanStencil != null && humanDepth != null);
            depthProcessor.SetBool("_HasEnvironmentData", environmentDepth != null);
            
            // Dispatch
            depthProcessor.Dispatch(kernelDepthToWorld, threadGroupsX, threadGroupsY, 1);
        }
        
        private void ProcessVelocity()
        {
            depthProcessor.SetTexture(kernelProcessVelocity, "_PositionInput", positionTexture);
            depthProcessor.SetFloat("_DeltaTime", Time.deltaTime);
            depthProcessor.Dispatch(kernelProcessVelocity, threadGroupsX, threadGroupsY, 1);
        }
        
        private void GenerateNormals()
        {
            depthProcessor.SetTexture(kernelGenerateNormals, "_PositionInput", positionTexture);
            depthProcessor.Dispatch(kernelGenerateNormals, threadGroupsX, threadGroupsY, 1);
        }
        
        private void UpdateMatrices()
        {
            // Calculate matrices
            var vpMatrix = arCamera.projectionMatrix * arCamera.worldToCameraMatrix;
            var invVPMatrix = vpMatrix.inverse;
            var cameraToWorld = arCamera.cameraToWorldMatrix;
            
            // Update buffer
            var matrices = new Matrix4x4[] { invVPMatrix, cameraToWorld, previousVPMatrix };
            matricesBuffer.SetData(matrices);
            
            // Bind to compute shader
            depthProcessor.SetBuffer(kernelDepthToWorld, "_Matrices", matricesBuffer);
            depthProcessor.SetBuffer(kernelProcessVelocity, "_Matrices", matricesBuffer);
            
            // Store for next frame
            previousVPMatrix = vpMatrix;
        }
        
        private void CaptureBackgroundTexture()
        {
            if (cameraBackground.material != null && captureTexture != null)
            {
                Graphics.Blit(null, captureTexture, cameraBackground.material);
            }
        }
        
        #endregion
        
        #region VFX Integration
        
        private void CreateVFXInstance()
        {
            if (vfxInstance != null) return;
            
            // Create VFX instance
            var go = Instantiate(vfxPrefab.gameObject, transform.parent);
            vfxInstance = go.GetComponent<VisualEffect>();
            
            // Position at origin
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            
            // Bind textures
            BindTexturesToVFX(vfxInstance);
            
            // Set initial parameters
            vfxInstance.SetInt("MaxParticles", textureResolution.x * textureResolution.y);
            vfxInstance.SetVector2("Resolution", new Vector2(textureResolution.x, textureResolution.y));
        }
        
        public void BindTexturesToVFX(VisualEffect vfx)
        {
            if (vfx == null) return;
            
            vfx.SetTexture("PositionMap", positionTexture);
            vfx.SetTexture("ColorMap", colorTexture);
            vfx.SetTexture("VelocityMap", velocityTexture);
            vfx.SetTexture("NormalMap", normalTexture);
        }
        
        #endregion
        
        #region Performance Management
        
        private void UpdateQualityLevel()
        {
            if (Time.time - lastQualityUpdate < qualityUpdateInterval) return;
            
            // Calculate average frame time
            averageDeltaTime = averageDeltaTime * 0.9f + Time.deltaTime * 0.1f;
            float currentFPS = 1f / averageDeltaTime;
            
            // Determine if we need to adjust quality
            if (currentFPS < targetFrameRate * 0.8f && currentQualityLevel > 0)
            {
                SetQualityLevel(currentQualityLevel - 1);
            }
            else if (currentFPS > targetFrameRate * 0.95f && currentQualityLevel < 3)
            {
                SetQualityLevel(currentQualityLevel + 1);
            }
            
            lastQualityUpdate = Time.time;
        }
        
        private void SetQualityLevel(int level)
        {
            currentQualityLevel = Mathf.Clamp(level, 0, 3);
            
            Vector2Int newResolution = textureResolution;
            switch (currentQualityLevel)
            {
                case 0: // Low
                    newResolution = new Vector2Int(256, 256);
                    break;
                case 1: // Medium
                    newResolution = new Vector2Int(384, 384);
                    break;
                case 2: // High
                    newResolution = new Vector2Int(512, 512);
                    break;
                case 3: // Ultra
                    newResolution = new Vector2Int(768, 768);
                    break;
            }
            
            if (newResolution != textureResolution)
            {
                ResizeRenderTextures(newResolution);
            }
            
            if (vfxInstance != null)
            {
                vfxInstance.SetFloat("QualityLevel", currentQualityLevel / 3f);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        private void Cleanup()
        {
            isInitialized = false;
            
            // Release render textures
            ReleaseRenderTextures();
            
            // Release compute buffer
            matricesBuffer?.Release();
            matricesBuffer = null;
            
            // Destroy VFX instance
            if (vfxInstance != null)
            {
                Destroy(vfxInstance.gameObject);
                vfxInstance = null;
            }
        }
        
        private void ReleaseRenderTextures()
        {
            positionTexture?.Release();
            colorTexture?.Release();
            velocityTexture?.Release();
            normalTexture?.Release();
            captureTexture?.Release();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Manually set VFX instance if not using prefab
        /// </summary>
        public void SetVFXInstance(VisualEffect vfx)
        {
            vfxInstance = vfx;
            if (isInitialized)
            {
                BindTexturesToVFX(vfxInstance);
            }
        }
        
        /// <summary>
        /// Get current quality level (0-3)
        /// </summary>
        public int GetQualityLevel() => currentQualityLevel;
        
        /// <summary>
        /// Get current FPS
        /// </summary>
        public float GetCurrentFPS() => 1f / averageDeltaTime;
        
        /// <summary>
        /// Get position texture for custom processing
        /// </summary>
        public RenderTexture GetPositionTexture() => positionTexture;
        
        /// <summary>
        /// Get color texture
        /// </summary>
        public RenderTexture GetColorTexture() => colorTexture;
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!showDebugInfo || !isInitialized) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"FPS: {GetCurrentFPS():F1}");
            GUILayout.Label($"Quality Level: {currentQualityLevel}");
            GUILayout.Label($"Resolution: {textureResolution.x}x{textureResolution.y}");
            GUILayout.Label($"Particles: {textureResolution.x * textureResolution.y:N0}");
            
            var hasHuman = occlusionManager.humanStencilTexture != null;
            var hasEnv = occlusionManager.environmentDepthTexture != null;
            GUILayout.Label($"Human Depth: {(hasHuman ? "Active" : "Inactive")}");
            GUILayout.Label($"Environment Depth: {(hasEnv ? "Active" : "Inactive")}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
}