using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Optimized ARFoundation to VFX Graph bridge for Unity 6 iOS
/// Features: GPU-only processing, hand/face velocity tracking, adaptive quality
/// </summary>
[RequireComponent(typeof(AROcclusionManager))]
[RequireComponent(typeof(ARCameraManager))]
public class OptimalARVFXBridge : MonoBehaviour
{
    [Header("VFX Configuration")]
    [SerializeField] private VisualEffect vfxGraph;
    [SerializeField] private ComputeShader depthProcessor;
    
    [Header("Performance Settings")]
    [SerializeField] private bool adaptiveQuality = true;
    [SerializeField] private int targetFPS = 60;
    [SerializeField] private Vector2Int textureResolution = new Vector2Int(512, 512);
    
    [Header("Body Tracking")]
    [SerializeField] private ARHumanBodyManager bodyManager;
    [SerializeField] private bool trackHandVelocity = true;
    [SerializeField] private bool trackFaceFeatures = true;
    
    // Core components
    private AROcclusionManager occlusionManager;
    private ARCameraManager cameraManager;
    private ARFaceManager faceManager;
    
    // GPU Resources
    private RenderTexture positionRT;
    private RenderTexture velocityRT;
    private RenderTexture colorRT;
    private RenderTexture confidenceRT;
    
    // Compute shader kernels
    private int depthToWorldKernel;
    private int velocityKernel;
    private int confidenceKernel;
    
    // Performance tracking
    private float deltaTime;
    private int currentQualityLevel = 2;
    
    // Body tracking data
    private NativeArray<float3> previousHandPositions;
    private NativeArray<float3> handVelocities;
    private float3 previousFacePosition;
    
    private void Awake()
    {
        occlusionManager = GetComponent<AROcclusionManager>();
        cameraManager = GetComponent<ARCameraManager>();
        
        // Optional components
        bodyManager = GetComponentInParent<ARHumanBodyManager>();
        faceManager = GetComponentInParent<ARFaceManager>();
        
        // Enable optimal settings for iOS
        ConfigureForIOS();
    }
    
    private void ConfigureForIOS()
    {
        // Request fastest modes for real-time performance
        occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
        occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
        occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
        
        // Enable temporal smoothing for stable results
        occlusionManager.environmentDepthTemporalSmoothingEnabled = true;
        
        QualitySettings.vSyncCount = 0; // Disable VSync for maximum performance
        Application.targetFrameRate = targetFPS;
    }
    
    private void OnEnable()
    {
        // Initialize GPU resources
        InitializeRenderTextures();
        InitializeComputeShader();
        
        // Subscribe to body tracking events
        if (bodyManager != null)
        {
            bodyManager.humanBodiesChanged += OnHumanBodiesChanged;
            previousHandPositions = new NativeArray<float3>(2, Allocator.Persistent);
            handVelocities = new NativeArray<float3>(2, Allocator.Persistent);
        }
        
        if (faceManager != null)
        {
            faceManager.facesChanged += OnFacesChanged;
        }
    }
    
    private void OnDisable()
    {
        // Cleanup
        ReleaseRenderTextures();
        
        if (bodyManager != null)
        {
            bodyManager.humanBodiesChanged -= OnHumanBodiesChanged;
            previousHandPositions.Dispose();
            handVelocities.Dispose();
        }
        
        if (faceManager != null)
        {
            faceManager.facesChanged -= OnFacesChanged;
        }
    }
    
    private void InitializeRenderTextures()
    {
        var format = RenderTextureFormat.ARGBFloat;
        
        positionRT = CreateRT(format);
        velocityRT = CreateRT(format);
        colorRT = CreateRT(RenderTextureFormat.ARGB32);
        confidenceRT = CreateRT(RenderTextureFormat.RFloat);
        
        // Bind to VFX Graph
        vfxGraph.SetTexture("PositionMap", positionRT);
        vfxGraph.SetTexture("VelocityMap", velocityRT);
        vfxGraph.SetTexture("ColorMap", colorRT);
        vfxGraph.SetTexture("ConfidenceMap", confidenceRT);
    }
    
    private RenderTexture CreateRT(RenderTextureFormat format)
    {
        var rt = new RenderTexture(textureResolution.x, textureResolution.y, 0, format);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Bilinear;
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        return rt;
    }
    
    private void InitializeComputeShader()
    {
        depthToWorldKernel = depthProcessor.FindKernel("DepthToWorld");
        velocityKernel = depthProcessor.FindKernel("CalculateVelocity");
        confidenceKernel = depthProcessor.FindKernel("ProcessConfidence");
    }
    
    private void Update()
    {
        // Adaptive quality based on performance
        if (adaptiveQuality)
        {
            UpdateQualityLevel();
        }
        
        // Process depth data
        ProcessDepthData();
        
        // Update VFX parameters
        UpdateVFXParameters();
    }
    
    private void ProcessDepthData()
    {
        var humanStencil = occlusionManager.humanStencilTexture;
        var humanDepth = occlusionManager.humanDepthTexture;
        var envDepth = occlusionManager.environmentDepthTexture;
        
        if (humanStencil == null || humanDepth == null) return;
        
        // Get camera matrices
        var camera = cameraManager.GetComponent<Camera>();
        var vpMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
        var invVPMatrix = vpMatrix.inverse;
        
        // Set compute shader parameters
        depthProcessor.SetMatrix("_InvVPMatrix", invVPMatrix);
        depthProcessor.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        depthProcessor.SetVector("_DepthRange", new Vector4(0.1f, 10f, 0, 0));
        
        // Bind textures
        depthProcessor.SetTexture(depthToWorldKernel, "_HumanDepth", humanDepth);
        depthProcessor.SetTexture(depthToWorldKernel, "_HumanStencil", humanStencil);
        depthProcessor.SetTexture(depthToWorldKernel, "_PositionRT", positionRT);
        
        if (envDepth != null)
        {
            depthProcessor.SetTexture(depthToWorldKernel, "_EnvDepth", envDepth);
        }
        
        // Dispatch
        int threadGroupsX = Mathf.CeilToInt(textureResolution.x / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(textureResolution.y / 8.0f);
        depthProcessor.Dispatch(depthToWorldKernel, threadGroupsX, threadGroupsY, 1);
        
        // Process velocity if tracking
        if (trackHandVelocity || trackFaceFeatures)
        {
            ProcessVelocityData();
        }
    }
    
    private void ProcessVelocityData()
    {
        depthProcessor.SetTexture(velocityKernel, "_PositionRT", positionRT);
        depthProcessor.SetTexture(velocityKernel, "_VelocityRT", velocityRT);
        depthProcessor.SetFloat("_DeltaTime", Time.deltaTime);
        
        // Add hand velocities if available
        if (handVelocities.IsCreated)
        {
            depthProcessor.SetVector("_LeftHandVelocity", handVelocities[0]);
            depthProcessor.SetVector("_RightHandVelocity", handVelocities[1]);
        }
        
        int threadGroupsX = Mathf.CeilToInt(textureResolution.x / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(textureResolution.y / 8.0f);
        depthProcessor.Dispatch(velocityKernel, threadGroupsX, threadGroupsY, 1);
    }
    
    private void OnHumanBodiesChanged(ARHumanBodiesChangedEventArgs args)
    {
        if (!trackHandVelocity) return;
        
        foreach (var humanBody in args.updated)
        {
            var joints = humanBody.joints;
            if (!joints.IsCreated) continue;
            
            // Track hand positions for velocity
            if (joints.Length > (int)JointIndices3D.LeftHand)
            {
                var leftHand = joints[(int)JointIndices3D.LeftHand];
                var rightHand = joints[(int)JointIndices3D.RightHand];
                
                // Calculate velocities
                handVelocities[0] = (leftHand.anchorPose.position - previousHandPositions[0]) / Time.deltaTime;
                handVelocities[1] = (rightHand.anchorPose.position - previousHandPositions[1]) / Time.deltaTime;
                
                // Update previous positions
                previousHandPositions[0] = leftHand.anchorPose.position;
                previousHandPositions[1] = rightHand.anchorPose.position;
                
                // Send to VFX
                vfxGraph.SetVector3("LeftHandVelocity", handVelocities[0]);
                vfxGraph.SetVector3("RightHandVelocity", handVelocities[1]);
            }
        }
    }
    
    private void OnFacesChanged(ARFacesChangedEventArgs args)
    {
        if (!trackFaceFeatures) return;
        
        foreach (var face in args.updated)
        {
            // Calculate face velocity
            var faceVelocity = (face.transform.position - previousFacePosition) / Time.deltaTime;
            previousFacePosition = face.transform.position;
            
            // Extract face features
            var leftEye = face.leftEye;
            var rightEye = face.rightEye;
            
            if (leftEye != null && rightEye != null)
            {
                var eyeDistance = Vector3.Distance(leftEye.position, rightEye.position);
                vfxGraph.SetFloat("EyeDistance", eyeDistance);
                vfxGraph.SetVector3("FaceVelocity", faceVelocity);
                
                // Blink detection
                vfxGraph.SetFloat("LeftEyeOpenness", leftEye.localScale.y);
                vfxGraph.SetFloat("RightEyeOpenness", rightEye.localScale.y);
            }
        }
    }
    
    private void UpdateVFXParameters()
    {
        // Update particle count based on quality
        int particleCount = textureResolution.x * textureResolution.y;
        particleCount = Mathf.RoundToInt(particleCount * GetQualityMultiplier());
        
        vfxGraph.SetInt("MaxParticles", particleCount);
        vfxGraph.SetFloat("QualityLevel", currentQualityLevel / 3f);
        
        // Send events
        vfxGraph.SendEvent("OnDepthUpdate");
    }
    
    private void UpdateQualityLevel()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        
        if (fps < targetFPS * 0.8f && currentQualityLevel > 0)
        {
            currentQualityLevel--;
            AdjustQuality();
        }
        else if (fps > targetFPS * 0.95f && currentQualityLevel < 3)
        {
            currentQualityLevel++;
            AdjustQuality();
        }
    }
    
    private void AdjustQuality()
    {
        switch (currentQualityLevel)
        {
            case 0: // Low
                textureResolution = new Vector2Int(256, 256);
                break;
            case 1: // Medium
                textureResolution = new Vector2Int(384, 384);
                break;
            case 2: // High
                textureResolution = new Vector2Int(512, 512);
                break;
            case 3: // Ultra
                textureResolution = new Vector2Int(768, 768);
                break;
        }
        
        // Recreate render textures at new resolution
        ReleaseRenderTextures();
        InitializeRenderTextures();
    }
    
    private float GetQualityMultiplier()
    {
        switch (currentQualityLevel)
        {
            case 0: return 0.25f;
            case 1: return 0.5f;
            case 2: return 0.75f;
            case 3: return 1.0f;
            default: return 0.5f;
        }
    }
    
    private void ReleaseRenderTextures()
    {
        positionRT?.Release();
        velocityRT?.Release();
        colorRT?.Release();
        confidenceRT?.Release();
    }
}