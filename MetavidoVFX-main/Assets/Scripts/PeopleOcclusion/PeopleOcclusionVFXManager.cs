using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.VFX;
using MetavidoVFX.VFX;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(ARCameraBackground))]
[RequireComponent(typeof(AROcclusionManager))]
public class PeopleOcclusionVFXManager : MonoBehaviour
{
    struct ThreadSize
    {
        public int x;
        public int y;
        public int z;

        public ThreadSize(uint x, uint y, uint z)
        {
            this.x = (int)x;
            this.y = (int)y;
            this.z = (int)z;
        }
    }

    [SerializeField]
    VisualEffectAsset m_VfxAsset;
    [SerializeField]
    ComputeShader m_ComputeShader;
    
    [Header("VFX Swapping")]
    [SerializeField]
    VisualEffectAsset[] m_VfxAssets; // Array of VFX assets to swap between
    int m_CurrentVfxIndex = 0;

    ARCameraBackground m_CameraBackground;
    AROcclusionManager m_OcclusionManager;
    Camera m_Camera;
    RenderTexture m_CaptureTexture, m_PositionTexture;
    RenderTexture m_VelocityTexture, m_PreviousPositionTexture;
    VisualEffect m_VfxInstance;
    GameObject m_VfxGameObject;
    int m_Kernel, m_VelocityKernel;
    ThreadSize m_ThreadSize;

    // Debug
    float m_LastLogTime;
    int m_FrameCount;
    bool m_FirstTextureReceived;
    bool m_VfxInitialized;

    void Log(string msg)
    {
        if (!VFXBinderManager.SuppressPeopleVFXLogs)
            Debug.Log(msg);
    }

    void LogWarning(string msg)
    {
        if (!VFXBinderManager.SuppressPeopleVFXLogs)
            Debug.LogWarning(msg);
    }

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_CameraBackground = GetComponent<ARCameraBackground>();
        m_OcclusionManager = GetComponent<AROcclusionManager>();
        Log("[PeopleVFX] Awake - Components acquired");
    }

    void OnEnable()
    {
        Log($"[PeopleVFX] OnEnable - Screen: {Screen.width}x{Screen.height}");

        // Create VFX GameObject and component at runtime
        m_VfxGameObject = new GameObject("PeopleVFX");
        m_VfxGameObject.transform.SetParent(transform.parent);
        m_VfxInstance = m_VfxGameObject.AddComponent<VisualEffect>();
        m_VfxInstance.visualEffectAsset = m_VfxAsset;

        Log($"[PeopleVFX] VFX Asset: {(m_VfxAsset != null ? m_VfxAsset.name : "NULL")}");
        Log($"[PeopleVFX] VFX Instance: {(m_VfxInstance != null ? "Created" : "NULL")}");

        m_CaptureTexture = new RenderTexture(Screen.width, Screen.height, 16);
        m_CaptureTexture.Create();

        Log($"[PeopleVFX] Capture texture created: {m_CaptureTexture.width}x{m_CaptureTexture.height}");

        SetupComputeShader();
    }

    void OnDisable()
    {
        Log("[PeopleVFX] OnDisable - Cleaning up");
        if (m_VfxGameObject != null)
            Destroy(m_VfxGameObject);
        if (m_CaptureTexture != null)
            m_CaptureTexture.Release();
        if (m_PositionTexture != null)
            m_PositionTexture.Release();
        if (m_VelocityTexture != null)
            m_VelocityTexture.Release();
        if (m_PreviousPositionTexture != null)
            m_PreviousPositionTexture.Release();
    }

    void Update()
    {
        m_FrameCount++;

        Texture2D stencilTexture = m_OcclusionManager.humanStencilTexture;
        Texture2D depthTexture = m_OcclusionManager.humanDepthTexture;

        // Log texture status every 2 seconds
        if (Time.time - m_LastLogTime > 2f)
        {
            m_LastLogTime = Time.time;

            string stencilInfo = stencilTexture != null
                ? $"{stencilTexture.width}x{stencilTexture.height} ({stencilTexture.format})"
                : "NULL";
            string depthInfo = depthTexture != null
                ? $"{depthTexture.width}x{depthTexture.height} ({depthTexture.format})"
                : "NULL";

            Log($"[PeopleVFX] Frame {m_FrameCount} | Stencil: {stencilInfo} | Depth: {depthInfo}");

            if (m_VfxInstance != null)
            {
                Log($"[PeopleVFX] VFX alive count: {m_VfxInstance.aliveParticleCount}");
            }
        }

        if (stencilTexture == null || depthTexture == null)
        {
            if (!m_FirstTextureReceived && m_FrameCount % 60 == 0)
            {
                // LogWarning($"[PeopleVFX] Waiting for ARKit textures... (frame {m_FrameCount})");
            }
            return;
        }

        if (!m_FirstTextureReceived)
        {
            m_FirstTextureReceived = true;
            Log($"[PeopleVFX] ✓ First ARKit textures received!");
            Log($"[PeopleVFX] Stencil: {stencilTexture.width}x{stencilTexture.height} format={stencilTexture.format}");
            Log($"[PeopleVFX] Depth: {depthTexture.width}x{depthTexture.height} format={depthTexture.format}");
            
            // Re-initialize VFX now that textures are available
            InitializeVFXWithTextures();
        }

        Matrix4x4 invVPMatrix = (m_Camera.projectionMatrix * m_Camera.transform.worldToLocalMatrix).inverse;

        if (m_ComputeShader != null && m_Kernel >= 0 && m_PositionTexture != null && m_ThreadSize.x > 0 && m_ThreadSize.y > 0)
        {
            // Copy current position to previous before updating
            if (m_PreviousPositionTexture != null)
            {
                Graphics.Blit(m_PositionTexture, m_PreviousPositionTexture);
            }

            // Position kernel
            m_ComputeShader.SetTexture(m_Kernel, "DepthTexture", depthTexture);
            m_ComputeShader.SetTexture(m_Kernel, "StencilTexture", stencilTexture);
            m_ComputeShader.SetMatrix("InvVPMatrix", invVPMatrix);
            m_ComputeShader.SetMatrix("ProjectionMatrix", m_Camera.projectionMatrix);
            m_ComputeShader.SetVector("DepthRange", new Vector4(0.1f, 5.0f, 0.5f, 0));

            int groupsX = Mathf.CeilToInt(m_PositionTexture.width / m_ThreadSize.x);
            int groupsY = Mathf.CeilToInt(m_PositionTexture.height / m_ThreadSize.y);
            m_ComputeShader.Dispatch(m_Kernel, groupsX, groupsY, 1);

            // Velocity kernel
            if (m_VelocityKernel >= 0 && m_VelocityTexture != null && m_PreviousPositionTexture != null)
            {
                m_ComputeShader.SetTexture(m_VelocityKernel, "PositionTexture", m_PositionTexture);
                m_ComputeShader.SetTexture(m_VelocityKernel, "PreviousPositionTexture", m_PreviousPositionTexture);
                m_ComputeShader.SetTexture(m_VelocityKernel, "VelocityTexture", m_VelocityTexture);
                m_ComputeShader.SetFloat("DeltaTime", Time.deltaTime);
                m_ComputeShader.Dispatch(m_VelocityKernel, groupsX, groupsY, 1);
            }
        }

        if (m_VfxInstance != null)
        {
            // Guard against missing VFX properties to avoid console errors
            if (m_VfxInstance.HasTexture("Color Map"))
                m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
            if (m_VfxInstance.HasTexture("Stencil Map"))
                m_VfxInstance.SetTexture("Stencil Map", stencilTexture);
            if (m_VfxInstance.HasTexture("Position Map"))
                m_VfxInstance.SetTexture("Position Map", m_PositionTexture);

            // Only set velocity if the VFX has this property exposed
            if (m_VelocityTexture != null && m_VfxInstance.HasTexture("Velocity Map"))
            {
                m_VfxInstance.SetTexture("Velocity Map", m_VelocityTexture);
            }
        }
    }
    
    /// <summary>
    /// Initialize VFX with textures once ARKit data is available.
    /// This fixes the timing issue where VFX was created before AR textures were ready.
    /// </summary>
    void InitializeVFXWithTextures()
    {
        if (m_VfxInstance == null || m_VfxInitialized) return;

        // Guard against missing VFX properties to avoid console errors
        if (m_PositionTexture != null)
        {
            if (m_VfxInstance.HasTexture("Position Map"))
                m_VfxInstance.SetTexture("Position Map", m_PositionTexture);
            else
                LogWarning($"[PeopleVFX] VFX '{m_VfxInstance.visualEffectAsset?.name}' missing 'Position Map' property");
        }
        if (m_CaptureTexture != null)
        {
            if (m_VfxInstance.HasTexture("Color Map"))
                m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
            else
                LogWarning($"[PeopleVFX] VFX '{m_VfxInstance.visualEffectAsset?.name}' missing 'Color Map' property");
        }

        m_VfxInstance.Reinit(); // Force VFX to reinitialize with new textures
        m_VfxInstance.Play();

        m_VfxInitialized = true;
        Log("[PeopleVFX] ✓ VFX reinitialized with AR textures");
    }
    
    /// <summary>
    /// Swap to the next VFX asset in the array
    /// </summary>
    public void SwapToNextVFX()
    {
        if (m_VfxAssets == null || m_VfxAssets.Length == 0) return;
        
        m_CurrentVfxIndex = (m_CurrentVfxIndex + 1) % m_VfxAssets.Length;
        SwapVFX(m_VfxAssets[m_CurrentVfxIndex]);
    }
    
    /// <summary>
    /// Swap to a specific VFX asset by index
    /// </summary>
    public void SwapVFX(int index)
    {
        if (m_VfxAssets == null || index < 0 || index >= m_VfxAssets.Length) return;
        
        m_CurrentVfxIndex = index;
        SwapVFX(m_VfxAssets[index]);
    }
    
    /// <summary>
    /// Swap to a specific VFX asset
    /// </summary>
    public void SwapVFX(VisualEffectAsset newAsset)
    {
        if (newAsset == null || m_VfxInstance == null) return;
        
        Log($"[PeopleVFX] Swapping VFX to: {newAsset.name}");
        
        // Stop current VFX
        m_VfxInstance.Stop();
        
        // Swap asset
        m_VfxInstance.visualEffectAsset = newAsset;
        
        // Rebind textures with guards to avoid console errors
        if (m_PositionTexture != null)
        {
            if (m_VfxInstance.HasTexture("Position Map"))
                m_VfxInstance.SetTexture("Position Map", m_PositionTexture);
            else
                LogWarning($"[PeopleVFX] Swapped VFX '{newAsset.name}' missing 'Position Map' property");
        }
        if (m_CaptureTexture != null)
        {
            if (m_VfxInstance.HasTexture("Color Map"))
                m_VfxInstance.SetTexture("Color Map", m_CaptureTexture);
            else
                LogWarning($"[PeopleVFX] Swapped VFX '{newAsset.name}' missing 'Color Map' property");
        }
        
        // Reinitialize and play
        m_VfxInstance.Reinit();
        m_VfxInstance.Play();
        
        Log($"[PeopleVFX] ✓ VFX swapped successfully");
    }
    
    /// <summary>
    /// Get the current VFX index
    /// </summary>
    public int CurrentVFXIndex => m_CurrentVfxIndex;
    
    /// <summary>
    /// Get the total number of available VFX assets
    /// </summary>
    public int VFXCount => m_VfxAssets?.Length ?? 0;

    void SetupComputeShader()
    {
        // Position texture
        m_PositionTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_PositionTexture.enableRandomWrite = true;
        m_PositionTexture.filterMode = FilterMode.Bilinear;
        m_PositionTexture.wrapMode = TextureWrapMode.Clamp;
        m_PositionTexture.Create();

        // Previous position texture (for velocity calculation)
        m_PreviousPositionTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_PreviousPositionTexture.filterMode = FilterMode.Bilinear;
        m_PreviousPositionTexture.wrapMode = TextureWrapMode.Clamp;
        m_PreviousPositionTexture.Create();

        // Velocity texture
        m_VelocityTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat);
        m_VelocityTexture.enableRandomWrite = true;
        m_VelocityTexture.filterMode = FilterMode.Bilinear;
        m_VelocityTexture.wrapMode = TextureWrapMode.Clamp;
        m_VelocityTexture.Create();

        Log($"[PeopleVFX] Position texture: {m_PositionTexture.width}x{m_PositionTexture.height}");
        Log($"[PeopleVFX] Velocity texture: {m_VelocityTexture.width}x{m_VelocityTexture.height}");

        // Position kernel
        m_Kernel = m_ComputeShader.FindKernel("GeneratePositionTexture");
        uint threadSizeX, threadSizeY, threadSizeZ;
        m_ComputeShader.GetKernelThreadGroupSizes(m_Kernel, out threadSizeX, out threadSizeY, out threadSizeZ);
        m_ThreadSize = new ThreadSize(threadSizeX, threadSizeY, threadSizeZ);

        // Velocity kernel
        m_VelocityKernel = m_ComputeShader.FindKernel("CalculateVelocity");

        Log($"[PeopleVFX] Position kernel: {m_Kernel}, Velocity kernel: {m_VelocityKernel}");
        Log($"[PeopleVFX] Thread size: {threadSizeX}x{threadSizeY}x{threadSizeZ}");

        m_ComputeShader.SetTexture(m_Kernel, "PositionTexture", m_PositionTexture);

        // Bind textures with guards to avoid console errors
        if (m_VfxInstance.HasTexture("Position Map"))
        {
            m_VfxInstance.SetTexture("Position Map", m_PositionTexture);
            Log("[PeopleVFX] ✓ Position Map bound to VFX");
        }
        else
        {
            LogWarning($"[PeopleVFX] VFX '{m_VfxInstance.visualEffectAsset?.name}' missing 'Position Map' property");
        }

        // Only set velocity if VFX has this property exposed
        if (m_VfxInstance.HasTexture("Velocity Map"))
        {
            m_VfxInstance.SetTexture("Velocity Map", m_VelocityTexture);
            Log("[PeopleVFX] ✓ Velocity Map bound to VFX");
        }
        else
        {
            Log("[PeopleVFX] ⚠ VFX does not expose 'Velocity Map' - velocity available but not bound");
        }

        Log("[PeopleVFX] ✓ Compute shader setup complete (with velocity)");
    }

    void LateUpdate()
    {
        if (m_CameraBackground.material != null)
        {
            Graphics.Blit(null, m_CaptureTexture, m_CameraBackground.material);
        }
    }
}
