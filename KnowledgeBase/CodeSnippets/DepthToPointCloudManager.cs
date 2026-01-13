using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Manages depth-to-point-cloud conversion using compute shader
/// Optimized for 60 FPS on A14+ devices with configurable point cloud limits
/// </summary>
public class DepthToPointCloudManager : MonoBehaviour
{
    [Header("Compute Shader")]
    [SerializeField] private ComputeShader depthToPointCloudShader;
    
    [Header("Performance Settings")]
    [SerializeField] private int maxPointCount = 200000; // 200k points for A14+
    [SerializeField] private bool enableNormals = true;
    [SerializeField] private float normalSampleDistance = 1.0f;
    [SerializeField] private float depthScale = 1.0f;
    
    [Header("Depth Filtering")]
    [SerializeField] private float minDepth = 0.1f;
    [SerializeField] private float maxDepth = 10.0f;
    
    [Header("Performance Monitoring")]
    [SerializeField] private bool enableProfiling = true;
    [SerializeField] private int targetFrameRate = 60;
    
    // AR Foundation components
    private ARCameraManager cameraManager;
    private AROcclusionManager occlusionManager;
    
    // Compute shader kernels
    private int kernelDepthToPointCloud;
    private int kernelDepthToPointCloudAppend;
    private int kernelClearPointCount;
    
    // Compute buffers
    private ComputeBuffer pointCloudBuffer;
    private ComputeBuffer pointCountBuffer;
    private ComputeBuffer appendBuffer;
    
    // Shader property IDs
    private readonly int _DepthTexture = Shader.PropertyToID("_DepthTexture");
    private readonly int _CameraIntrinsicsMatrix = Shader.PropertyToID("_CameraIntrinsicsMatrix");
    private readonly int _InverseCameraIntrinsicsMatrix = Shader.PropertyToID("_InverseCameraIntrinsicsMatrix");
    private readonly int _ViewToWorldMatrix = Shader.PropertyToID("_ViewToWorldMatrix");
    private readonly int _PointCloudBuffer = Shader.PropertyToID("_PointCloudBuffer");
    private readonly int _PointCountBuffer = Shader.PropertyToID("_PointCountBuffer");
    private readonly int _AppendBuffer = Shader.PropertyToID("_AppendBuffer");
    private readonly int _MaxPointCount = Shader.PropertyToID("_MaxPointCount");
    private readonly int _TextureSize = Shader.PropertyToID("_TextureSize");
    private readonly int _MinDepth = Shader.PropertyToID("_MinDepth");
    private readonly int _MaxDepth = Shader.PropertyToID("_MaxDepth");
    private readonly int _DepthScale = Shader.PropertyToID("_DepthScale");
    private readonly int _GenerateNormals = Shader.PropertyToID("_GenerateNormals");
    private readonly int _NormalSampleDistance = Shader.PropertyToID("_NormalSampleDistance");
    
    // Performance tracking
    private float lastFrameTime;
    private float averageFrameTime;
    private int frameCount;
    private bool isPerformanceOptimized = true;
    
    // Point cloud data structure
    public struct PointCloudData
    {
        public Vector3 position;
        public Vector3 normal;
    }
    
    // Events
    public event Action<PointCloudData[]> OnPointCloudGenerated;
    public event Action<int> OnPointCountChanged;
    
    void Start()
    {
        InitializeComponents();
        InitializeComputeShader();
        OptimizeForDevice();
    }
    
    void InitializeComponents()
    {
        cameraManager = FindObjectOfType<ARCameraManager>();
        occlusionManager = FindObjectOfType<AROcclusionManager>();
        
        if (cameraManager == null)
        {
            Debug.LogError("ARCameraManager not found!");
            enabled = false;
            return;
        }
        
        if (occlusionManager == null)
        {
            Debug.LogError("AROcclusionManager not found!");
            enabled = false;
            return;
        }
        
        // Enable depth if not already enabled
        if (occlusionManager.requestedHumanDepthMode == HumanDepthMode.Disabled)
        {
            occlusionManager.requestedHumanDepthMode = HumanDepthMode.Best;
        }
        
        if (occlusionManager.requestedEnvironmentDepthMode == EnvironmentDepthMode.Disabled)
        {
            occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
        }
    }
    
    void InitializeComputeShader()
    {
        if (depthToPointCloudShader == null)
        {
            Debug.LogError("DepthToPointCloud compute shader not assigned!");
            enabled = false;
            return;
        }
        
        // Find kernel indices
        kernelDepthToPointCloud = depthToPointCloudShader.FindKernel("DepthToPointCloud");
        kernelDepthToPointCloudAppend = depthToPointCloudShader.FindKernel("DepthToPointCloudAppend");
        kernelClearPointCount = depthToPointCloudShader.FindKernel("ClearPointCount");
        
        // Create compute buffers
        CreateComputeBuffers();
    }
    
    void CreateComputeBuffers()
    {
        // Point cloud buffer
        pointCloudBuffer = new ComputeBuffer(maxPointCount, System.Runtime.InteropServices.Marshal.SizeOf<PointCloudData>());
        
        // Point count buffer (single uint)
        pointCountBuffer = new ComputeBuffer(1, sizeof(uint));
        
        // Append buffer (alternative approach)
        appendBuffer = new ComputeBuffer(maxPointCount, System.Runtime.InteropServices.Marshal.SizeOf<PointCloudData>(), ComputeBufferType.Append);
        
        // Clear initial count
        uint[] initialCount = { 0 };
        pointCountBuffer.SetData(initialCount);
    }
    
    void OptimizeForDevice()
    {
        // Device-specific optimizations
        string deviceModel = SystemInfo.deviceModel;
        int deviceGeneration = GetDeviceGeneration(deviceModel);
        
        if (deviceGeneration >= 14) // A14+ devices
        {
            maxPointCount = 200000; // 200k points
            enableNormals = true;
        }
        else if (deviceGeneration >= 12) // A12-A13 devices
        {
            maxPointCount = 100000; // 100k points
            enableNormals = true;
        }
        else // Older devices
        {
            maxPointCount = 50000; // 50k points
            enableNormals = false;
        }
        
        Debug.Log($"Device: {deviceModel}, Generation: A{deviceGeneration}, Max Points: {maxPointCount}");
    }
    
    int GetDeviceGeneration(string deviceModel)
    {
        // Simplified device generation detection
        if (deviceModel.Contains("iPhone12") || deviceModel.Contains("iPhone13") || deviceModel.Contains("iPhone14") || deviceModel.Contains("iPhone15"))
            return 14;
        else if (deviceModel.Contains("iPhone11") || deviceModel.Contains("iPhoneXS") || deviceModel.Contains("iPhoneXR"))
            return 12;
        else if (deviceModel.Contains("iPhoneX") || deviceModel.Contains("iPhone8"))
            return 11;
        else
            return 10; // Fallback for older devices
    }
    
    void Update()
    {
        if (enableProfiling)
        {
            TrackPerformance();
        }
        
        ProcessDepthFrame();
    }
    
    void TrackPerformance()
    {
        float currentFrameTime = Time.unscaledDeltaTime;
        frameCount++;
        
        if (frameCount == 1)
        {
            averageFrameTime = currentFrameTime;
        }
        else
        {
            averageFrameTime = (averageFrameTime * (frameCount - 1) + currentFrameTime) / frameCount;
        }
        
        float currentFPS = 1.0f / currentFrameTime;
        
        // Dynamic performance adjustment
        if (currentFPS < targetFrameRate * 0.9f && isPerformanceOptimized)
        {
            maxPointCount = Mathf.Max(maxPointCount / 2, 10000);
            Debug.Log($"Performance optimization: Reduced max points to {maxPointCount}");
            isPerformanceOptimized = false;
        }
        else if (currentFPS > targetFrameRate * 1.1f && !isPerformanceOptimized)
        {
            maxPointCount = Mathf.Min(maxPointCount * 2, 200000);
            Debug.Log($"Performance optimization: Increased max points to {maxPointCount}");
            isPerformanceOptimized = true;
        }
        
        // Reset frame count periodically
        if (frameCount >= 60)
        {
            frameCount = 0;
        }
    }
    
    void ProcessDepthFrame()
    {
        // Get depth texture
        Texture2D depthTexture = null;
        
        if (occlusionManager.humanDepthTexture != null)
        {
            depthTexture = occlusionManager.humanDepthTexture;
        }
        else if (occlusionManager.environmentDepthTexture != null)
        {
            depthTexture = occlusionManager.environmentDepthTexture;
        }
        
        if (depthTexture == null)
            return;
        
        // Get camera intrinsics
        XRCameraIntrinsics intrinsics;
        if (!cameraManager.TryGetIntrinsics(out intrinsics))
        {
            Debug.LogWarning("Failed to get camera intrinsics");
            return;
        }
        
        // Create intrinsics matrix
        Matrix4x4 intrinsicsMatrix = CreateIntrinsicsMatrix(intrinsics);
        Matrix4x4 inverseIntrinsicsMatrix = intrinsicsMatrix.inverse;
        
        // Get camera transform
        Matrix4x4 viewToWorldMatrix = cameraManager.transform.localToWorldMatrix;
        
        // Clear point count
        depthToPointCloudShader.SetBuffer(kernelClearPointCount, _PointCountBuffer, pointCountBuffer);
        depthToPointCloudShader.Dispatch(kernelClearPointCount, 1, 1, 1);
        
        // Set compute shader parameters
        depthToPointCloudShader.SetTexture(kernelDepthToPointCloud, _DepthTexture, depthTexture);
        depthToPointCloudShader.SetMatrix(_CameraIntrinsicsMatrix, intrinsicsMatrix);
        depthToPointCloudShader.SetMatrix(_InverseCameraIntrinsicsMatrix, inverseIntrinsicsMatrix);
        depthToPointCloudShader.SetMatrix(_ViewToWorldMatrix, viewToWorldMatrix);
        depthToPointCloudShader.SetBuffer(kernelDepthToPointCloud, _PointCloudBuffer, pointCloudBuffer);
        depthToPointCloudShader.SetBuffer(kernelDepthToPointCloud, _PointCountBuffer, pointCountBuffer);
        depthToPointCloudShader.SetInt(_MaxPointCount, maxPointCount);
        depthToPointCloudShader.SetInts(_TextureSize, depthTexture.width, depthTexture.height);
        depthToPointCloudShader.SetFloat(_MinDepth, minDepth);
        depthToPointCloudShader.SetFloat(_MaxDepth, maxDepth);
        depthToPointCloudShader.SetFloat(_DepthScale, depthScale);
        depthToPointCloudShader.SetBool(_GenerateNormals, enableNormals);
        depthToPointCloudShader.SetFloat(_NormalSampleDistance, normalSampleDistance);
        
        // Dispatch compute shader
        int groupsX = Mathf.CeilToInt(depthTexture.width / 8.0f);
        int groupsY = Mathf.CeilToInt(depthTexture.height / 8.0f);
        depthToPointCloudShader.Dispatch(kernelDepthToPointCloud, groupsX, groupsY, 1);
        
        // Read back results
        ReadPointCloudResults();
    }
    
    Matrix4x4 CreateIntrinsicsMatrix(XRCameraIntrinsics intrinsics)
    {
        Matrix4x4 matrix = Matrix4x4.identity;
        
        // Camera intrinsics matrix:
        // [fx  0  cx  0]
        // [0  fy  cy  0]
        // [0   0   1  0]
        // [0   0   0  1]
        
        matrix[0, 0] = intrinsics.focalLength.x;
        matrix[1, 1] = intrinsics.focalLength.y;
        matrix[0, 2] = intrinsics.principalPoint.x;
        matrix[1, 2] = intrinsics.principalPoint.y;
        
        return matrix;
    }
    
    void ReadPointCloudResults()
    {
        // Read point count
        uint[] pointCount = new uint[1];
        pointCountBuffer.GetData(pointCount);
        
        if (pointCount[0] > 0)
        {
            // Read point cloud data
            PointCloudData[] pointData = new PointCloudData[pointCount[0]];
            pointCloudBuffer.GetData(pointData, 0, 0, (int)pointCount[0]);
            
            // Invoke events
            OnPointCloudGenerated?.Invoke(pointData);
            OnPointCountChanged?.Invoke((int)pointCount[0]);
        }
    }
    
    void OnDestroy()
    {
        // Clean up compute buffers
        pointCloudBuffer?.Release();
        pointCountBuffer?.Release();
        appendBuffer?.Release();
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // Pause compute shader processing
            enabled = false;
        }
        else
        {
            // Resume compute shader processing
            enabled = true;
        }
    }
    
    // Public methods for runtime configuration
    public void SetMaxPointCount(int count)
    {
        maxPointCount = Mathf.Clamp(count, 1000, 500000);
        
        // Recreate buffers with new size
        pointCloudBuffer?.Release();
        appendBuffer?.Release();
        
        pointCloudBuffer = new ComputeBuffer(maxPointCount, System.Runtime.InteropServices.Marshal.SizeOf<PointCloudData>());
        appendBuffer = new ComputeBuffer(maxPointCount, System.Runtime.InteropServices.Marshal.SizeOf<PointCloudData>(), ComputeBufferType.Append);
    }
    
    public void SetDepthRange(float min, float max)
    {
        minDepth = min;
        maxDepth = max;
    }
    
    public void EnableNormalGeneration(bool enable)
    {
        enableNormals = enable;
    }
    
    public int GetCurrentPointCount()
    {
        if (pointCountBuffer == null) return 0;
        
        uint[] pointCount = new uint[1];
        pointCountBuffer.GetData(pointCount);
        return (int)pointCount[0];
    }
    
    public float GetAverageFrameTime()
    {
        return averageFrameTime;
    }
    
    public float GetCurrentFPS()
    {
        return 1.0f / Time.unscaledDeltaTime;
    }
}
