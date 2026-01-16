using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.XR.ARFoundation;
using MetavidoVFX.VFX;

namespace Metavido {

[AddComponentMenu("VFX/Property Binders/ARKit Metavido Binder")]
[VFXBinder("ARKit Metavido")]
public sealed class ARKitMetavidoBinder : VFXBinderBase
{
    [SerializeField] AROcclusionManager _occlusionManager = null;
    [SerializeField] ARCameraTextureProvider _textureProvider = null;
    [SerializeField] Camera _camera = null;

    public string ColorMapProperty
      { get => (string)_colorMapProperty;
        set => _colorMapProperty = value; }

    public string DepthMapProperty
      { get => (string)_depthMapProperty;
        set => _depthMapProperty = value; }

    public string InverseViewProperty
      { get => (string)_inverseViewProperty;
        set => _inverseViewProperty = value; }

    public string RayParamsProperty
      { get => (string)_rayParamsProperty;
        set => _rayParamsProperty = value; }

    public string DepthRangeProperty
      { get => (string)_depthRangeProperty;
        set => _depthRangeProperty = value; }

    public string StencilMapProperty
      { get => (string)_stencilMapProperty;
        set => _stencilMapProperty = value; }

    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    ExposedProperty _colorMapProperty = "ColorMap";

    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    ExposedProperty _depthMapProperty = "DepthMap";

    [VFXPropertyBinding("UnityEngine.Matrix4x4"), SerializeField]
    ExposedProperty _inverseViewProperty = "InverseView";

    [VFXPropertyBinding("UnityEngine.Vector4"), SerializeField]
    ExposedProperty _rayParamsProperty = "RayParams";

    [VFXPropertyBinding("UnityEngine.Vector2"), SerializeField]
    ExposedProperty _depthRangeProperty = "DepthRange";

    [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
    ExposedProperty _stencilMapProperty = "StencilMap";

    [Header("Body Tracking Mode")]
    [Tooltip("Use stencil masking with environment depth for body-only particles")]
    [SerializeField] bool _useStencilMasking = true;  // ENABLED: Mask env depth with stencil

    [Header("Masking Shader")]
    [SerializeField] Shader _depthMaskShader = null;

    static int _logCount = 0;
    static Texture2D _fallbackDepth = null;
    static string _lastStatus = "Initializing...";
    static bool _showDebugGUI = true;
    static System.IO.StreamWriter _logWriter;
    static string _logPath;

    static void InitFileLog()
    {
        if (_logWriter != null) return;
        _logPath = System.IO.Path.Combine(Application.persistentDataPath, "arkit_debug.log");
        try {
            _logWriter = new System.IO.StreamWriter(_logPath, false);
            _logWriter.AutoFlush = true;
            _logWriter.WriteLine($"=== ARKitBinder Log v2.0 Started {System.DateTime.Now} ===");
            _logWriter.WriteLine($"=== FIX: EnsureMaskingResources called BEFORE null check ===");
        } catch { }
    }

    static void FileLog(string msg)
    {
        if (VFXBinderManager.SuppressARKitBinderLogs) return;
        InitFileLog();
        try { _logWriter?.WriteLine($"[{Time.frameCount}] {msg}"); } catch { }
        // Debug.Log($"[ARKitBinder] {msg}");
    }

    // Log interception
    void OnEnable() { Application.logMessageReceived += HandleLog; }
    void OnDisable() { Application.logMessageReceived -= HandleLog; }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        FileLog($"[{type}] {logString}");
        if (type == LogType.Error || type == LogType.Exception)
        {
            // FileLog(stackTrace);
        }
    }

    // Stencil masking resources
    Material _maskMaterial = null;
    RenderTexture _maskedDepthRT = null;

    void EnsureFallbackTexture()
    {
        if (_fallbackDepth == null)
        {
            // Create a simple gradient depth texture for testing
            _fallbackDepth = new Texture2D(256, 192, TextureFormat.RHalf, false);
            Color[] pixels = new Color[256 * 192];
            for (int y = 0; y < 192; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    // Create depth gradient - closer in center
                    float cx = (x - 128f) / 128f;
                    float cy = (y - 96f) / 96f;
                    float dist = Mathf.Sqrt(cx * cx + cy * cy);
                    float depth = 0.5f + dist * 2f; // 0.5m to 2.5m
                    pixels[y * 256 + x] = new Color(depth, 0, 0, 1);
                }
            }
            _fallbackDepth.SetPixels(pixels);
            _fallbackDepth.Apply();
            Debug.Log("[ARKitBinder] Created fallback depth texture 256x192");
        }
    }

    public override bool IsValid(VisualEffect component)
    {
        // Very lenient - just need camera to bind parameters
        bool hasCamera = _camera != null;
        bool hasDepthProp = component.HasTexture(_depthMapProperty);
        bool hasSpawnProp = component.HasBool("Spawn");

        // Log detailed status
        if (_logCount++ % 120 == 0)
        {
            // Debug.Log($"[ARKitBinder] Camera={hasCamera} HasDepth={hasDepthProp} HasSpawn={hasSpawnProp} " +
                     // $"DepthProp='{_depthMapProperty}' OccMgr={_occlusionManager != null}");
        }

        // Return true if we have at least the camera - we'll handle missing VFX props gracefully
        return hasCamera;
    }

    // Compute Shader approach for robust masking
    ComputeShader _maskingCompute = null;
    int _maskKernel = -1;

    void EnsureMaskingResources(int width, int height)
    {
        // Load Compute Shader from Resources if needed
        if (_maskingCompute == null)
        {
            _maskingCompute = Resources.Load<ComputeShader>("DepthToWorld");
            if (_maskingCompute != null)
            {
                try
                {
                    _maskKernel = _maskingCompute.FindKernel("DepthToWorld");
                    FileLog("[SUCCESS] Loaded DepthToWorld.compute");
                }
                catch (System.ArgumentException)
                {
                    // Kernel not found - shader may have failed to compile in Editor
                    _maskKernel = -1;
                    FileLog("[WARNING] DepthToWorld kernel not found. May not work in Editor but will work on device.");
                }
            }
            else
            {
                Debug.LogError("[ARKitBinder] Failed to load DepthToWorld.compute from Resources!");
                FileLog("[CRITICAL] Failed to load DepthToWorld resource!");
            }
        }

        // Create or resize RenderTexture (Use ARGBHalf for max compatibility, or RHalf/RFloat if supported)
        // Using ARGBHalf to match the success pattern of OptimizedARVFXBridge's positionRT, though we only need 1 channel.
        // Actually, let's stick to RHalf but use enableRandomWrite for Compute.
        if (_maskedDepthRT == null || _maskedDepthRT.width != width || _maskedDepthRT.height != height)
        {
            if (_maskedDepthRT != null)
                _maskedDepthRT.Release();

            _maskedDepthRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
            _maskedDepthRT.enableRandomWrite = true; // Required for Compute Shader
            _maskedDepthRT.filterMode = FilterMode.Bilinear;
            _maskedDepthRT.Create();
            Debug.Log($"[ARKitBinder] Created masked using Compute {width}x{height} (ARGBFloat)");
        }
    }

    Texture CreateMaskedDepth(Texture depthTex, Texture stencilTex)
    {
        if (depthTex == null || stencilTex == null)
            return null;

        EnsureMaskingResources(depthTex.width, depthTex.height);

        if (_maskingCompute == null || _maskKernel == -1 || _maskedDepthRT == null)
        {
            // Fallback to EnvDepth if compute setup fails
             FileLog("ERROR: Compute shader not ready. Bypassing mask.");
            return depthTex;
        }

        _maskingCompute.SetTexture(_maskKernel, "_Depth", depthTex);
        _maskingCompute.SetTexture(_maskKernel, "_Stencil", stencilTex);
        _maskingCompute.SetTexture(_maskKernel, "_PositionRT", _maskedDepthRT);
        _maskingCompute.SetInt("_UseStencil", 1);

        // Match compute shader [numthreads(32,32,1)]
        int threadGroupsX = Mathf.CeilToInt(depthTex.width / 32.0f);
        int threadGroupsY = Mathf.CeilToInt(depthTex.height / 32.0f);
        _maskingCompute.Dispatch(_maskKernel, threadGroupsX, threadGroupsY, 1);

        return _maskedDepthRT;
    }

    public override void UpdateBinding(VisualEffect component)
    {
        try {
            // Ensure fallback texture exists
            EnsureFallbackTexture();

            // 1. Capture State Safely
            if (_camera == null || _occlusionManager == null) return;

            Texture rawEnvDepth = null;
            Texture stencil = null;
            try {
                rawEnvDepth = _occlusionManager.environmentDepthTexture;
                stencil = _occlusionManager.humanStencilTexture;
            } catch { }

            // 2. Compute Transform Matrices
            var proj = _camera.projectionMatrix;
            var worldToLocal = _camera.transform.worldToLocalMatrix;
            var invVP = (proj * worldToLocal).inverse;
            var iview = _camera.cameraToWorldMatrix;

            // 3. Compute Ray Parameters (Legacy Path Support)
            // Format: (0, 0, tan(vfov/2) * aspect, tan(vfov/2))
            // This matches HologramRenderer.cs and H3MLiDARCapture.cs standards.
            float fovV = _camera.fieldOfView * Mathf.Deg2Rad;
            float tanV = Mathf.Tan(fovV * 0.5f);
            float tanH = tanV * _camera.aspect;
            Vector4 rayParams = new Vector4(0, 0, tanH, tanV);

            // 4. Modern Path: Compute Position Map
            Texture positionMap = null;
            string depthSource = "none";
            bool isHumanTracking = false;

            if (rawEnvDepth != null)
            {
                EnsureMaskingResources(rawEnvDepth.width, rawEnvDepth.height);
                if (_maskingCompute != null && _maskKernel != -1)
                {
                    // Pass params to Compute Shader (Matches DepthToWorld.compute property names)
                    _maskingCompute.SetMatrix("_InvVP", invVP);
                    _maskingCompute.SetMatrix("_ProjectionMatrix", proj);
                    _maskingCompute.SetVector("_DepthRange", new Vector4(0.1f, 5.0f, 0.5f, 0)); // Min, Max, StencilThreshold

                    _maskingCompute.SetTexture(_maskKernel, "_Depth", rawEnvDepth);
                    _maskingCompute.SetTexture(_maskKernel, "_Stencil", (stencil != null ? stencil : Texture2D.whiteTexture));
                    _maskingCompute.SetTexture(_maskKernel, "_PositionRT", _maskedDepthRT);
                    _maskingCompute.SetInt("_UseStencil", _useStencilMasking && stencil != null ? 1 : 0);

                    // Dispatch (32x32 threads)
                    int gx = Mathf.CeilToInt(rawEnvDepth.width / 32.0f);
                    int gy = Mathf.CeilToInt(rawEnvDepth.height / 32.0f);
                    _maskingCompute.Dispatch(_maskKernel, gx, gy, 1);

                    positionMap = _maskedDepthRT;
                    depthSource = "POS_MAP(Computed)";
                    isHumanTracking = _useStencilMasking && (stencil != null);
                }
            }

            // 5. Fallback logic
            if (positionMap == null)
            {
                // If we can't compute PositionMap, use raw depth if property exists
                depthSource = (rawEnvDepth != null) ? "ENV_DEPTH(Raw)" : "FALLBACK(Gradient)";
            }

            // 6. Bind to VFX Graph
            // A. Color Map
            if (_textureProvider != null && _textureProvider.Texture != null && component.HasTexture(_colorMapProperty))
                component.SetTexture(_colorMapProperty, _textureProvider.Texture);

            // B. Depth Map (Raw Depth or Fallback)
            Texture depthToBind = (rawEnvDepth != null) ? rawEnvDepth : _fallbackDepth;
            if (component.HasTexture(_depthMapProperty))
                component.SetTexture(_depthMapProperty, depthToBind);

            // C. Position Map (Computed World Positions)
            if (positionMap != null && component.HasTexture("PositionMap"))
                component.SetTexture("PositionMap", positionMap);
            else if (positionMap == null && component.HasTexture("PositionMap") && _logCount % 120 == 0)
                FileLog("WARNING: VFX has PositionMap but computation failed!");

            // D. Stencil Map
            if (stencil != null && component.HasTexture(_stencilMapProperty))
                component.SetTexture(_stencilMapProperty, stencil);

            // E. Matrix Parameters
            if (component.HasMatrix4x4(_inverseViewProperty))
                component.SetMatrix4x4(_inverseViewProperty, iview);

            if (component.HasMatrix4x4("ProjectionMatrix"))
                component.SetMatrix4x4("ProjectionMatrix", proj);

            // F. Vector Parameters
            if (component.HasVector4(_rayParamsProperty))
                component.SetVector4(_rayParamsProperty, rayParams);

            if (component.HasVector2(_depthRangeProperty))
                component.SetVector2(_depthRangeProperty, new Vector2(0.1f, 10.0f));

            // G. Control Flags
            bool shouldSpawn = (rawEnvDepth != null);
            if (component.HasBool("Spawn"))
                component.SetBool("Spawn", shouldSpawn);

            // Force human flag for tracking assets
            if (component.HasBool("Human"))
                component.SetBool("Human", isHumanTracking || !isHumanTracking); // Typically true if we have any tracking

            // 7. Status & Logging
            _logCount++;
            _lastStatus = $"Active: {depthSource}\n" +
                          $"Body: {(isHumanTracking ? "YES" : "NO")}\n" +
                          $"Particles: {component.aliveParticleCount}";

            if (_logCount % 60 == 1)
                FileLog($"Update: {_lastStatus.Replace("\n", " | ")}");
        }
        catch (System.Exception e) {
            FileLog($"[CRITICAL] UpdateBinding crashed: {e.Message}\n{e.StackTrace}");
        }
    }

    void OnDestroy()
    {
        if (_maskedDepthRT != null)
        {
            _maskedDepthRT.Release();
            _maskedDepthRT = null;
        }
        if (_maskMaterial != null)
        {
            Destroy(_maskMaterial);
            _maskMaterial = null;
        }
    }

    public override string ToString()
      => $"ARKit Metavido : {_colorMapProperty}, {_depthMapProperty}";

    void OnGUI()
    {
        if (!_showDebugGUI) return;

        // Large, visible debug overlay
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold
        };
        style.normal.textColor = Color.yellow;

        // Semi-transparent background
        GUI.backgroundColor = new Color(0, 0, 0, 0.7f);

        float boxWidth = 450;
        float boxHeight = 200;
        Rect bgRect = new Rect(Screen.width - boxWidth - 20, 50, boxWidth, boxHeight);
        GUI.Box(bgRect, "");

        GUILayout.BeginArea(new Rect(Screen.width - boxWidth - 10, 60, boxWidth - 20, boxHeight - 20));
        GUILayout.Label("=== BODY TRACKING ===", style);
        style.fontSize = 26;
        GUILayout.Label(_lastStatus, style);
        GUILayout.EndArea();
    }
}

} // namespace Metavido
