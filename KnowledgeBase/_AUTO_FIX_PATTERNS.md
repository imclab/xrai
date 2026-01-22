# Auto-Fix Patterns

**Version**: 1.0 (2026-01-21)
**Purpose**: Automatically applicable fixes for common errors.
**Usage**: AI tools apply these fixes without user confirmation.

---

## Quick Reference

| Error Code | Issue | Fix |
|------------|-------|-----|
| CS0246 | Type not found | Add using statement |
| CS0103 | Name doesn't exist | Check scope/typo |
| CS1061 | No member found | Check API version |
| NullRef | Null reference | Add null guard |
| VFX Property | Property not updating | Use ExposedProperty |

---

## Unity C# Errors

### CS0246 - Type or namespace not found

**Detection**: `error CS0246.*type or namespace.*could not be found`

**Common Fixes**:
| Missing Type | Add Using |
|--------------|-----------|
| `VisualEffect` | `using UnityEngine.VFX;` |
| `ARSession` | `using UnityEngine.XR.ARFoundation;` |
| `XROrigin` | `using Unity.XR.CoreUtils;` |
| `ARCameraManager` | `using UnityEngine.XR.ARFoundation;` |
| `ARPlaneManager` | `using UnityEngine.XR.ARFoundation;` |
| `AROcclusionManager` | `using UnityEngine.XR.ARFoundation;` |
| `HandJoint` | `using UnityEngine.XR.Hands;` |
| `NativeArray` | `using Unity.Collections;` |
| `JobHandle` | `using Unity.Jobs;` |
| `float3` | `using Unity.Mathematics;` |
| `IJobParallelFor` | `using Unity.Jobs;` |
| `PhotonNetwork` | `using Photon.Pun;` |

**Auto-Apply**: Yes (add using statement at top of file)

### CS0103 - Name does not exist in current context

**Detection**: `error CS0103.*name.*does not exist`

**Common Causes**:
1. Variable not declared
2. Typo in variable name
3. Wrong scope (private when should be public)
4. Missing `this.` for class members

**Auto-Apply**: Partial (typo correction, scope suggestions)

### CS1061 - Type does not contain definition

**Detection**: `error CS1061.*does not contain a definition`

**Common Fixes**:
| Old API | New API |
|---------|---------|
| `ARSessionOrigin` | `XROrigin` (AR Foundation 5+) |
| `ARRaycastManager.Raycast` | Check `TrackableType` enum |
| `Application.isPlaying` | Use `#if UNITY_EDITOR` |

**Auto-Apply**: Partial (API migration suggestions)

---

## AR Foundation Fixes

### NullReferenceException in AR Texture Access

**Detection**: `NullReferenceException` + `Texture` + AR stack trace

**Pattern** (TryAcquire with proper disposal):
```csharp
// BEFORE (crashes)
var texture = occlusionManager.humanDepthTexture;

// AFTER (safe with XRCpuImage)
if (occlusionManager != null &&
    occlusionManager.TryAcquireHumanDepthCpuImage(out XRCpuImage image))
{
    using (image)  // CRITICAL: Must dispose to avoid resource leak
    {
        // Process image
        var texture = new Texture2D(image.width, image.height, image.format.AsTextureFormat(), false);
    }
}
```

**Source**: [Unity AR Foundation Samples - CpuImageSample.cs](https://github.com/Unity-Technologies/arfoundation-samples)

**Auto-Apply**: Yes (wrap with TryAcquire + using pattern)

### Human Stencil/Depth Texture Access

**Detection**: `humanStencilTexture` or `humanDepthTexture` returns null

**Cause**: GPU-only texture, need CPU readback or TryAcquire

**Pattern**:
```csharp
// For GPU texture (rendering only)
if (occlusionManager.humanStencilTexture != null)
{
    material.SetTexture("_StencilTex", occlusionManager.humanStencilTexture);
}

// For CPU access (pixel data)
if (occlusionManager.TryAcquireHumanStencilCpuImage(out XRCpuImage image))
{
    using (image)
    {
        // Read raw pixel data
        var rawData = image.GetPlane(0).data;
    }
}
```

**Platform**: Requires A12 Bionic chip (iPhone XS+) running iOS 13+

**Auto-Apply**: Yes (add null check or TryAcquire)

### AR Session Not Ready

**Detection**: AR operations fail at startup

**Fix**:
```csharp
IEnumerator WaitForARSession()
{
    while (ARSession.state < ARSessionState.SessionTracking)
        yield return null;
    // Now safe to use AR
}
```

**Auto-Apply**: Yes (add coroutine wrapper)

### AROcclusionManager Setup

**Detection**: Occlusion not working, depth textures null

**Fix**: Ensure proper setup
```csharp
// On AR Camera (child of XR Origin)
[RequireComponent(typeof(ARCameraManager))]
public class MyOcclusionScript : MonoBehaviour
{
    AROcclusionManager occlusionManager;

    void Awake()
    {
        occlusionManager = GetComponent<AROcclusionManager>();
        // Enable modes
        occlusionManager.requestedHumanStencilMode = HumanSegmentationStencilMode.Fastest;
        occlusionManager.requestedHumanDepthMode = HumanSegmentationDepthMode.Fastest;
    }
}
```

**Source**: [Unity AR Foundation Occlusion docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@5.0/manual/features/occlusion.html)

**Auto-Apply**: Partial (suggest setup code)

### Environment Depth (LiDAR)

**Detection**: Need world depth, not just human depth

**Pattern** (ARKit 4+ with LiDAR):
```csharp
// Environment depth includes all scene geometry
if (occlusionManager.TryAcquireEnvironmentDepthCpuImage(out XRCpuImage image))
{
    using (image)
    {
        // Full scene depth from LiDAR
    }
}

// Check for LiDAR support
if (occlusionManager.descriptor?.supportsEnvironmentDepthImage == true)
{
    occlusionManager.requestedEnvironmentDepthMode = EnvironmentDepthMode.Best;
}
```

**Source**: [Unity Blog - AR Foundation ARKit 4 Depth](https://blog.unity.com/technology/ar-foundation-support-for-arkit-4-depth)

**Auto-Apply**: Partial (suggest depth mode check)

---

## VFX Graph Fixes

### VFX Property Not Updating

**Detection**: VFX property set but visual unchanged

**Cause**: Using string instead of ExposedProperty or Shader.PropertyToID

**Pattern** (from Unity docs):
```csharp
// BEFORE (works but not optimized)
vfx.SetFloat("MyProperty", value);

// BETTER (use property ID - most optimized)
static readonly int k_MyPropertyID = Shader.PropertyToID("MyProperty");
vfx.SetFloat(k_MyPropertyID, value);

// OR use ExposedProperty (serializable)
static readonly ExposedProperty MyProp = "MyProperty";
vfx.SetFloat(MyProp, value);
```

**Source**: [Unity VFX Component API](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@7.1/manual/ComponentAPI.html)

**Auto-Apply**: Yes (convert to property ID)

### VFX SetTexture Dimension Mismatch

**Detection**: `SetTexture` logs error, texture not applied

**Cause**: Texture dimension doesn't match VFX blackboard type

**Fix**:
```csharp
// Ensure texture dimensions match
if (texture.dimension == TextureDimension.Tex2D)
{
    vfx.SetTexture(k_DepthMapID, texture);
}
```

**Auto-Apply**: Yes (add dimension check)

### VFX Buffer Size Mismatch

**Detection**: VFX particles disappear or flicker

**Fix**: Ensure buffer count matches particle capacity
```csharp
graphicsBuffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    vfx.GetInt("Capacity"),  // Match VFX capacity
    stride
);
```

**Auto-Apply**: Partial (suggest capacity check)

### VFX Send Event with Attributes

**Detection**: Need to pass data with VFX event

**Pattern** (from keijiro repos):
```csharp
VFXEventAttribute eventAttr;

void Start()
{
    eventAttr = vfx.CreateVFXEventAttribute();
}

void TriggerEffect(Vector3 position)
{
    eventAttr.SetVector3("Position", position);
    vfx.SendEvent("OnTrigger", eventAttr);
}
```

**Source**: [keijiro/VfxGraphTestbed](https://github.com/keijiro/VfxGraphTestbed)

**Auto-Apply**: Partial (suggest event attribute pattern)

### VFX GraphicsBuffer from Compute Shader

**Detection**: Need to pass compute shader data to VFX

**Pattern** (from keijiro/VfxGraphGraphicsBufferTest):
```csharp
// In Compute Shader output
GraphicsBuffer buffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    count,
    sizeof(float) * 4
);

// Bind to compute
computeShader.SetBuffer(kernel, "_OutputBuffer", buffer);
computeShader.Dispatch(kernel, groupsX, 1, 1);

// Bind to VFX
vfx.SetGraphicsBuffer("PointBuffer", buffer);
```

**Source**: [keijiro/VfxGraphGraphicsBufferTest](https://github.com/keijiro/VfxGraphGraphicsBufferTest)

**Auto-Apply**: Partial (suggest buffer binding)

---

## Thread/Dispatch Fixes

### Main Thread Dispatch

**Detection**: `UnityException.*can only be called from the main thread`

**Fix**:
```csharp
// Queue to main thread
UnityMainThreadDispatcher.Instance().Enqueue(() => {
    // Unity API calls here
});
```

**Auto-Apply**: Yes (wrap with dispatcher)

### Compute Shader Thread Mismatch

**Detection**: Compute results incorrect or partial

**Fix**: Query actual thread group size
```csharp
shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);
int groupsX = Mathf.CeilToInt(width / (float)x);
int groupsY = Mathf.CeilToInt(height / (float)y);
shader.Dispatch(kernel, groupsX, groupsY, 1);
```

**Auto-Apply**: Yes (add dynamic thread group calculation)

---

## Memory/Performance Fixes

### RenderTexture Leak

**Detection**: Memory grows over time, RenderTexture warnings

**Fix**:
```csharp
void OnDestroy()
{
    if (renderTexture != null)
    {
        renderTexture.Release();
        renderTexture = null;
    }
}
```

**Auto-Apply**: Yes (add OnDestroy cleanup)

### GraphicsBuffer Leak

**Detection**: Memory grows, buffer warnings

**Fix**:
```csharp
void OnDestroy()
{
    graphicsBuffer?.Dispose();
    graphicsBuffer = null;
}
```

**Auto-Apply**: Yes (add disposal)

---

## MCP/Integration Fixes

### MCP Server Not Responding

**Detection**: MCP tool timeout or connection refused

**Fix**:
```bash
# Kill duplicate servers
mcp-kill-dupes

# Verify server running
lsof -i :6400  # Unity MCP
lsof -i :63342 # JetBrains
```

**Auto-Apply**: Yes (run mcp-kill-dupes)

### Unity MCP Multiple Instances

**Detection**: "Multiple Unity instances connected"

**Fix**: Set active instance before operations
```
set_active_instance(name="ProjectName@hash")
```

**Auto-Apply**: Yes (auto-select most recent)

---

## Tool Usage Fixes

### Grep Instead of Glob for File Search

**Detection**: Using Grep to find files by name

**Fix**: Use Glob for filename patterns
```
# BEFORE (inefficient)
Grep pattern: "FileName"

# AFTER (correct)
Glob pattern: "**/FileName*.cs"
```

**Auto-Apply**: Suggestion only

### Read Full File When Only Part Needed

**Detection**: Read 2000+ lines, used <100

**Fix**: Add offset and limit
```
Read(file, offset=0, limit=100)
```

**Auto-Apply**: Suggestion for next time

---

## Compute Shader Fixes

### GraphicsBuffer Target Selection

**Detection**: Buffer not readable/writable from compute shader

**Pattern** (from [Catlike Coding](https://catlikecoding.com/unity/tutorials/basics/compute-shaders/)):
```csharp
// For RWStructuredBuffer in compute shader
var buffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured,
    count,
    stride
);

// For RWByteAddressBuffer in compute shader
var buffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Raw,
    count,
    sizeof(uint)
);

// For both structured AND raw access
var buffer = new GraphicsBuffer(
    GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.Raw,
    count,
    stride
);
```

**Auto-Apply**: Partial (suggest correct target)

### Compute Shader Thread Group Size

**Detection**: 64 threads is optimal default

**Pattern**:
```hlsl
// In compute shader
#pragma kernel CSMain
[numthreads(64, 1, 1)]  // 64 = optimal (AMD warp=64, NVidia warp=32)
void CSMain(uint3 id : SV_DispatchThreadID)
{
    // ...
}
```

```csharp
// C# dispatch
int threadGroups = Mathf.CeilToInt(count / 64f);
computeShader.Dispatch(kernel, threadGroups, 1, 1);
```

**Source**: [Unity Compute Shader Manual](https://docs.unity3d.com/Manual/class-ComputeShader.html)

**Auto-Apply**: Partial (suggest 64 thread default)

---

## XR Hands / Hand Tracking Fixes

### XR Hands Custom Gesture Setup

**Detection**: Need hand gesture recognition

**Pattern** (XR Hands 1.4+):
```csharp
// No code needed - use Inspector!
// 1. Create HandShape asset (define finger conditions)
// 2. Add StaticHandGesture component
// 3. Set Minimum Hold Time and Detection Interval
// 4. Subscribe to events:

public class GestureHandler : MonoBehaviour
{
    public StaticHandGesture gesture;

    void OnEnable()
    {
        gesture.GesturePerformed.AddListener(OnGesturePerformed);
        gesture.GestureEnded.AddListener(OnGestureEnded);
    }

    void OnGesturePerformed() { /* Gesture detected */ }
    void OnGestureEnded() { /* Gesture released */ }
}
```

**Source**: [Unity XR Hands Custom Gestures](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/gestures/custom-gestures.html)

**Auto-Apply**: Partial (suggest component-based approach)

### Hand Tracking Joint Access

**Detection**: Need hand joint positions

**Pattern**:
```csharp
using UnityEngine.XR.Hands;

XRHandSubsystem handSubsystem;

void Update()
{
    if (handSubsystem.leftHand.isTracked)
    {
        var wrist = handSubsystem.leftHand.GetJoint(XRHandJointID.Wrist);
        if (wrist.TryGetPose(out Pose pose))
        {
            transform.position = pose.position;
        }
    }
}
```

**Auto-Apply**: Yes (add TryGetPose pattern)

---

## Jobs/Burst Fixes

### Burst Compilation Setup

**Detection**: Job not Burst-compiled, slow performance

**Pattern**:
```csharp
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;  // Use float3, float4 for SIMD

[BurstCompile]
public struct MyJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> input;
    [WriteOnly] public NativeArray<float3> output;

    public void Execute(int index)
    {
        output[index] = math.normalize(input[index]);
    }
}
```

**Source**: [Unity Burst User Guide](https://docs.unity3d.com/Packages/com.unity.burst@1.3/manual/index.html)

**Auto-Apply**: Yes (add [BurstCompile] attribute)

### Job Scheduling Best Practice

**Detection**: JobHandle.Complete() called immediately

**Pattern**:
```csharp
JobHandle jobHandle;

void Update()
{
    // Schedule job
    var job = new MyJob { input = inputData, output = outputData };
    jobHandle = job.Schedule(count, 64);  // 64 = batch size
}

void LateUpdate()
{
    // Complete job later - gives worker threads time
    jobHandle.Complete();

    // Use results
    ProcessResults(outputData);
}
```

**Auto-Apply**: Yes (move Complete to LateUpdate)

### NativeArray Disposal

**Detection**: Memory leak warning, NativeArray not disposed

**Pattern**:
```csharp
NativeArray<float> data;

void OnEnable()
{
    data = new NativeArray<float>(1000, Allocator.Persistent);
}

void OnDisable()
{
    if (data.IsCreated)
        data.Dispose();
}
```

**Auto-Apply**: Yes (add IsCreated check + Dispose)

### Unity.Mathematics for SIMD

**Detection**: Using Vector3/Vector4 in Jobs

**Pattern**:
```csharp
// BEFORE (not SIMD optimized)
Vector3 result = Vector3.Lerp(a, b, t);

// AFTER (SIMD optimized with Burst)
using Unity.Mathematics;
float3 result = math.lerp(a, b, t);

// Best for SIMD: Use float4 (maps directly to SIMD registers)
float4 result = math.lerp(a4, b4, t);
```

**Source**: [Kodeco Unity Job System Tutorial](https://www.kodeco.com/7880445-unity-job-system-and-burst-compiler-getting-started)

**Auto-Apply**: Partial (suggest math.* equivalents)

---

## HoloKit Hand Tracking Fixes

### HoloKit Hand Tracking Setup

**Detection**: Need HoloKit hand tracking

**Pattern** (from [holokit-unity-sdk](https://github.com/holokit/holokit-unity-sdk)):
```csharp
// Add both managers to scene for tracking + gestures
// HandTrackingManager - provides joint positions
// HandGestureRecognitionManager - provides gesture events

// Access hand joint positions
using HoloKit;

HandTrackingManager handTracker;

void Update()
{
    if (handTracker.IsValid)
    {
        // Get fingertip position
        var indexTip = handTracker.GetJointPose(HandJointIndex.IndexTip);
        transform.position = indexTip.position;
    }
}
```

**Source**: [HoloKit Unity SDK](https://github.com/holokit/holokit-unity-sdk)

**Auto-Apply**: Partial (suggest component setup)

### HoloKit Finger Direction (for VFX)

**Detection**: Need finger direction for VFX effects

**Pattern** (from [finger-saber](https://github.com/realitydeslab/finger-saber)):
```csharp
// Calculate finger direction from TIP and IP joints
var tip = handTracker.GetJointPose(HandJointIndex.IndexTip);
var ip = handTracker.GetJointPose(HandJointIndex.IndexIP);

Vector3 fingerDirection = (tip.position - ip.position).normalized;

// Set VFX direction
vfx.SetVector3("Direction", fingerDirection);
vfx.SetVector3("Position", tip.position);
```

**Auto-Apply**: Partial (suggest joint calculation)

---

## Sentis/Inference Engine Fixes

### Sentis Basic Setup

**Detection**: Need ML inference in Unity

**Pattern** (Sentis 2.x / Inference Engine):
```csharp
using Unity.Sentis;  // or Unity.AI.Inference for newer versions

ModelAsset modelAsset;
Model runtimeModel;
Worker worker;

void Start()
{
    // Load model
    runtimeModel = ModelLoader.Load(modelAsset);

    // Create worker (GPU recommended)
    worker = new Worker(runtimeModel, BackendType.GPUCompute);
}

void RunInference(Texture2D input)
{
    // Create tensor from texture
    using var inputTensor = TextureConverter.ToTensor(input);

    // Run inference
    worker.Schedule(inputTensor);

    // Get output (non-blocking)
    var output = worker.PeekOutput() as Tensor<float>;

    // Read results
    var results = output.DownloadToArray();
}

void OnDestroy()
{
    worker?.Dispose();
}
```

**Source**: [Unity Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/understand-sentis-workflow.html)

**Auto-Apply**: Partial (suggest worker pattern)

### Sentis Backend Selection

**Detection**: Sentis running slow, wrong backend

**Pattern**:
```csharp
// GPU (fastest, recommended)
var worker = new Worker(model, BackendType.GPUCompute);

// CPU (fallback for compatibility)
var worker = new Worker(model, BackendType.CPU);

// Check platform support
if (SystemInfo.supportsComputeShaders)
    worker = new Worker(model, BackendType.GPUCompute);
else
    worker = new Worker(model, BackendType.CPU);
```

**Auto-Apply**: Yes (add platform check)

### Sentis Memory Management

**Detection**: Memory leak with Sentis tensors

**Pattern**:
```csharp
// ALWAYS use 'using' or manual Dispose for tensors
using var tensor = TextureConverter.ToTensor(texture);

// Or manual disposal
Tensor<float> tensor = null;
try
{
    tensor = new Tensor<float>(shape, data);
    // Use tensor
}
finally
{
    tensor?.Dispose();
}
```

**Auto-Apply**: Yes (add using/Dispose)

---

## Netcode for GameObjects Fixes

### NetworkBehaviour Basic Setup

**Detection**: Need multiplayer networking

**Pattern** (from [Netcode Docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)):
```csharp
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    // Synced variable (server authoritative)
    public NetworkVariable<int> Health = new(100);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Server-only initialization
        }
        if (IsOwner)
        {
            // Owner-only initialization
        }
    }

    // RPC: Client calls, server executes
    [ServerRpc]
    void MoveServerRpc(Vector3 position)
    {
        transform.position = position;
    }

    // RPC: Server calls, clients execute
    [ClientRpc]
    void TakeDamageClientRpc(int damage)
    {
        // Visual feedback on all clients
    }
}
```

**Auto-Apply**: Partial (suggest NetworkBehaviour pattern)

### NetworkVariable Change Callback

**Detection**: Need to react to network variable changes

**Pattern**:
```csharp
public NetworkVariable<int> Score = new(0);

public override void OnNetworkSpawn()
{
    Score.OnValueChanged += OnScoreChanged;
}

void OnScoreChanged(int oldValue, int newValue)
{
    // React to change on all clients
    Debug.Log($"Score: {oldValue} → {newValue}");
}

public override void OnNetworkDespawn()
{
    Score.OnValueChanged -= OnScoreChanged;
}
```

**Auto-Apply**: Yes (add callback pattern)

### Physics on Server Only

**Detection**: Networked physics stutter

**Pattern** (from [Unity Blog](https://blog.unity.com/games/build-a-production-ready-multiplayer-game-with-netcode-for-gameobjects)):
```csharp
// Server: Has Rigidbody, simulates physics
// Clients: No Rigidbody, receive transform from NetworkTransform

void Start()
{
    if (!IsServer)
    {
        // Remove physics on clients
        var rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);
    }
}
```

**Auto-Apply**: Partial (suggest server-authoritative physics)

---

## UI Toolkit Runtime Fixes

### UI Toolkit Basic Setup

**Detection**: Need runtime UI

**Pattern** (from [Unity UI Toolkit Manual](https://docs.unity3d.com/Manual/UIElements.html)):
```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class GameUI : MonoBehaviour
{
    [SerializeField] UIDocument uiDocument;

    VisualElement root;
    Label scoreLabel;
    Button startButton;

    void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        // Query elements by name or class
        scoreLabel = root.Q<Label>("score-label");
        startButton = root.Q<Button>("start-button");

        // Register callbacks
        startButton.clicked += OnStartClicked;
    }

    void OnDisable()
    {
        startButton.clicked -= OnStartClicked;
    }

    void OnStartClicked() { /* ... */ }
}
```

**Auto-Apply**: Partial (suggest Q<> query pattern)

### Runtime Data Binding (Unity 6)

**Detection**: Need UI data binding

**Pattern** (MVVM):
```csharp
using Unity.Properties;
using UnityEngine.UIElements;

// ViewModel with bindable properties
public class PlayerViewModel
{
    [CreateProperty]  // Compile-time binding, no reflection
    public int Health { get; set; }

    [CreateProperty]
    public string Name { get; set; }
}

// Bind to UI
void SetupBinding()
{
    var viewModel = new PlayerViewModel();
    root.dataSource = viewModel;

    // In UXML: <ui:Label binding-path="Health" />
}
```

**Source**: [Unity 6 UI Toolkit e-book](https://github.com/unity-e-book/UIToolkit)

**Auto-Apply**: Partial (suggest [CreateProperty] pattern)

### Element Pooling

**Detection**: Creating UI elements frequently, causing GC

**Pattern**:
```csharp
// Pool for list items
Queue<VisualElement> itemPool = new();

VisualElement GetPooledItem()
{
    if (itemPool.Count > 0)
        return itemPool.Dequeue();

    return new VisualElement();  // Create new only if pool empty
}

void ReturnToPool(VisualElement item)
{
    item.RemoveFromHierarchy();
    // Reset state before returning
    item.ClearClassList();
    itemPool.Enqueue(item);
}
```

**Source**: [Unity Best Practices for Managing Elements](https://docs.unity3d.com/6000.2/Documentation/Manual/UIE-best-practices-for-managing-elements.html)

**Auto-Apply**: Partial (suggest pooling pattern)

---

## Shader Graph / HLSL Fixes

### Custom Function Node HLSL File

**Detection**: Need custom HLSL in Shader Graph

**Pattern** (from [Shader Graph Docs](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.2/manual/Custom-Function-Node.html)):
```hlsl
// MyCustomFunctions.hlsl
// MUST include precision suffix (_float or _half)

#ifndef MY_CUSTOM_FUNCTIONS_INCLUDED
#define MY_CUSTOM_FUNCTIONS_INCLUDED

// Function name in Shader Graph: "MyLerp" (no suffix)
void MyLerp_float(float3 A, float3 B, float T, out float3 Out)
{
    Out = lerp(A, B, T);
}

// Half precision version
void MyLerp_half(half3 A, half3 B, half T, out half3 Out)
{
    Out = lerp(A, B, T);
}

#endif
```

**Auto-Apply**: Yes (add precision suffixes)

### Shader Graph Preview Compatibility

**Detection**: Custom function works in game but errors in preview

**Pattern**:
```hlsl
void MyFunction_float(float3 In, out float3 Out)
{
    #ifdef SHADERGRAPH_PREVIEW
        // Simplified version for preview
        Out = In;
    #else
        // Full implementation using pipeline libraries
        Out = TransformWorldToView(In);
    #endif
}
```

**Auto-Apply**: Yes (add SHADERGRAPH_PREVIEW guard)

### Texture Sampling in Custom Function

**Detection**: Texture2D not working in custom function

**Pattern**:
```hlsl
// Use Unity's texture macros
void SampleMyTexture_float(
    UnityTexture2D Tex,        // Not Texture2D
    UnitySamplerState Sampler, // Not SamplerState
    float2 UV,
    out float4 Out)
{
    Out = SAMPLE_TEXTURE2D(Tex, Sampler, UV);
}
```

**Auto-Apply**: Yes (use UnityTexture2D struct)

### Render Pipeline Detection

**Detection**: Need pipeline-specific code in shader

**Pattern**:
```hlsl
#if defined(UNIVERSAL_PIPELINE_CORE_INCLUDED)
    // URP code
    Light mainLight = GetMainLight();
#elif defined(BUILTIN_PIPELINE_CORE_INCLUDED)
    // Built-in RP code
    float3 lightDir = _WorldSpaceLightPos0.xyz;
#endif
```

**Auto-Apply**: Partial (suggest pipeline detection)

---

## Unity Mobile Optimization Fixes

### iOS/Android Build Configuration

**Detection**: Mobile performance issues

**Pattern** (from [Unity Mobile Optimization Guide](https://unity.com/resources/mobile-xr-web-game-performance-optimization-unity-6)):
```csharp
// Player Settings recommendations
// iOS: IL2CPP (faster than Mono), Metal API
// Android: IL2CPP, ARM64, Vulkan API

// Texture Import Settings
[CreateAssetMenu]
public class MobileTexturePreset : ScriptableObject
{
    // Use POT dimensions for PVRTC (iOS) / ETC2 (Android)
    // Max Size: 1024 or 512 for mobile
    // Compression: ASTC for quality, ETC2 for compatibility
}
```

**Auto-Apply**: Partial (suggest build settings)

### Object Pooling for Mobile

**Detection**: Frequent Instantiate/Destroy causing GC stutters

**Pattern**:
```csharp
public class ObjectPool<T> where T : Component
{
    Queue<T> pool = new();
    T prefab;

    public T Get()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return Object.Instantiate(prefab);
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**Source**: [Unity Mobile Optimization GitHub](https://github.com/GuardianOfGods/unity-mobile-optimization)

**Auto-Apply**: Yes (suggest pooling pattern)

### Centralized Update Manager

**Detection**: Many Update() calls causing overhead

**Pattern**:
```csharp
// Instead of 100 separate Update() methods
public class UpdateManager : MonoBehaviour
{
    static List<IUpdateable> updateables = new();

    public static void Register(IUpdateable u) => updateables.Add(u);
    public static void Unregister(IUpdateable u) => updateables.Remove(u);

    void Update()
    {
        for (int i = 0; i < updateables.Count; i++)
            updateables[i].OnUpdate();
    }
}

public interface IUpdateable { void OnUpdate(); }
```

**Auto-Apply**: Partial (suggest manager pattern)

---

## Unity WebGL Fixes

### WebGL Memory Optimization

**Detection**: "Browser could not allocate enough memory" error

**Pattern** (from [Unity WebGL Memory Blog](https://unity.com/blog/engine-platform/understanding-memory-in-unity-webgl)):
```csharp
// 1. Use smallest possible heap in Player Settings
// 2. Use Addressables for large assets
// 3. Unload unused assets aggressively

IEnumerator LoadLevel(string levelName)
{
    // Load with Addressables
    var handle = Addressables.LoadSceneAsync(levelName);
    yield return handle;

    // Force GC after load
    yield return Resources.UnloadUnusedAssets();
    System.GC.Collect();
}
```

**Auto-Apply**: Partial (suggest Addressables)

### WebGL Asset Bundle Loading

**Detection**: Large .data file, slow load times

**Pattern**:
```csharp
// Use LZ4 compression (faster than LZMA for WebGL)
// Build Settings: Compression Format = LZ4

IEnumerator LoadAssetBundle(string url)
{
    using var request = UnityWebRequestAssetBundle.GetAssetBundle(url);
    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        var bundle = DownloadHandlerAssetBundle.GetContent(request);
        // Use bundle...
        bundle.Unload(false);  // Unload when done
    }
}
```

**Source**: [Backtrace WebGL Blog](https://backtrace.io/blog/memory-and-performance-issues-in-unity-webgl-builds)

**Auto-Apply**: Partial (suggest bundle pattern)

### WebGL GC Timing

**Detection**: GC causing frame drops

**Note**: WebGL GC only runs when stack is empty (after each frame). Avoid large allocations mid-frame.

**Pattern**:
```csharp
// Pre-allocate arrays
float[] cachedArray = new float[1000];

// Use StringBuilder for strings
StringBuilder sb = new StringBuilder(256);

// Use CompareTag instead of string comparison
if (other.CompareTag("Player"))  // Good
// if (other.tag == "Player")    // Bad - allocates string
```

**Auto-Apply**: Yes (replace tag comparison)

---

## Needle Engine Fixes

### Needle Component Pattern

**Detection**: Need web export from Unity

**Pattern** (from [Needle Engine Docs](https://engine.needle.tools/docs/scripting.html)):
```typescript
// TypeScript component (matches Unity C# stub)
import { Behaviour, serializable } from "@needle-tools/engine";
import { Object3D, Vector3 } from "three";

export class MyComponent extends Behaviour {
    @serializable()
    speed: number = 5;

    @serializable(Object3D)
    target?: Object3D;

    start() {
        console.log("Component started");
    }

    update() {
        if (this.target) {
            // Use three.js API
            this.gameObject.lookAt(this.target.position);
        }
    }
}
```

**Auto-Apply**: Partial (suggest Needle pattern)

### Needle Raycasting Performance

**Detection**: Slow raycasting with skinned meshes

**Pattern**:
```typescript
// Set SkinnedMeshRenderer objects to "Ignore Raycast" layer in Unity
// Or use physics raycast instead of three.js raycast

import { Physics } from "@needle-tools/engine";

const hits = Physics.raycast(origin, direction);
// Physics raycast only hits colliders - much faster
```

**Source**: [Needle Engine FAQ](https://engine.needle.tools/docs/faq.html)

**Auto-Apply**: Yes (suggest physics raycast)

### Needle Lightmap Best Practices

**Detection**: Lighting issues in web export

**Pattern**:
```
// Unity Light Settings:
// - Use "Baked" or "Realtime" mode
// - "Mixed" is NOT supported
// - Lights set to Mixed affect objects twice in three.js
```

**Auto-Apply**: Suggestion only

---

## Open Brush Integration Fixes

### Import Open Brush Sketch

**Detection**: Need to import .glb sketch into Unity

**Pattern** (from [Open Brush Toolkit](https://github.com/icosa-foundation/open-brush-toolkit)):
```csharp
// 1. Download toolkit from GitHub releases
// 2. Import .unitypackage
// 3. Export sketch from Open Brush as .glb
// 4. Copy .glb to Unity project

// Toolkit auto-assigns correct Tilt Brush shaders on import
// Materials are preserved from original sketch
```

**Source**: [Open Brush Unity SDK Docs](https://docs.openbrush.app/user-guide/open-brush-unity-sdk)

**Auto-Apply**: Suggestion only

### Open Brush Stroke Definition (Plugin API)

**Detection**: Creating brush strokes programmatically

**Pattern** (Lua Plugin API):
```lua
-- Define stroke as list of transforms
-- Each point has position + rotation

function Main()
    local path = Path:New()

    for i = 0, 100 do
        local t = i / 100
        local pos = Vector3:New(
            math.sin(t * math.pi * 2),
            t,
            math.cos(t * math.pi * 2)
        )
        path:Add(Transform:New(pos, Rotation:Identity()))
    end

    return path  -- Returned path is drawn as stroke
end
```

**Source**: [Open Brush Plugin API](https://docs.openbrush.app/user-guide/using-plugins/writing-plugins/defining-and-drawing-brush-strokes)

**Auto-Apply**: Partial (suggest path pattern)

---

## Normcore Multiplayer Fixes

### RealtimeModel Definition

**Detection**: Need synchronized data in Normcore

**Pattern** (from [Normcore Docs](https://docs.normcore.io/realtime/synchronizing-custom-data)):
```csharp
using Normal.Realtime;
using Normal.Realtime.Serialization;

[RealtimeModel]
public partial class MyModel
{
    [RealtimeProperty(1, true)]   // (propertyID, reliable)
    private int _score;

    [RealtimeProperty(2, false)]  // unreliable for frequent updates
    private Vector3 _position;
}

// Normcore generates: Score, Position properties
// Changes auto-sync to all clients
```

**Auto-Apply**: Yes (suggest RealtimeModel pattern)

### RealtimeComponent with Ownership

**Detection**: Need ownership control in Normcore

**Pattern**:
```csharp
using Normal.Realtime;

[RealtimeModel(createMetaModel: true)]  // Enable ownership
public partial class OwnedModel
{
    [RealtimeProperty(1, true)]
    private float _health;
}

public class PlayerHealth : RealtimeComponent<OwnedModel>
{
    protected override void OnRealtimeModelReplaced(OwnedModel prev, OwnedModel current)
    {
        if (prev != null)
            prev.healthDidChange -= OnHealthChanged;

        if (current != null)
        {
            if (current.isFreshModel)
                current.health = 100f;  // Initialize

            current.healthDidChange += OnHealthChanged;
            UpdateUI(current.health);
        }
    }

    void OnHealthChanged(OwnedModel model, float value)
    {
        UpdateUI(value);
    }

    public void TakeDamage(float damage)
    {
        // Only owner can modify
        if (model.isOwnedLocallySelf)
            model.health -= damage;
    }
}
```

**Source**: [Normcore Ownership Docs](https://docs.normcore.io/room/ownership-and-lifetime-flags)

**Auto-Apply**: Yes (suggest ownership pattern)

### Normcore Instantiation

**Detection**: Multiplayer object not syncing

**Common Error**: Using `GameObject.Instantiate()` instead of `Realtime.Instantiate()`

**Pattern**:
```csharp
// WRONG - only creates locally
var obj = Instantiate(prefab);

// CORRECT - syncs across all clients
var obj = Realtime.Instantiate(
    "PrefabName",           // Must be in Resources folder
    position: spawnPoint,
    rotation: Quaternion.identity,
    ownedByClient: true     // Request ownership
);
```

**Auto-Apply**: Yes (replace Instantiate calls)

### Normcore 3 EasySync (No-Code)

**Detection**: Want quick sync without code

**Pattern**:
```
// 1. Add EasySync component to prefab
// 2. Check boxes for properties to sync
// 3. Done! No code needed

// When ready for code:
// Right-click EasySync → "Convert to Code"
// Generates RealtimeComponent + RealtimeModel
```

**Source**: [Normcore 3 What's New](https://docs.normcore.io/essentials/whats-new-in-normcore-3)

**Auto-Apply**: Suggestion only

---

## Self-Healing Triggers

| Trigger | Auto-Fix Action |
|---------|-----------------|
| Same error 3+ times | Add to this file |
| Pattern not found | Create pattern entry |
| Fix outdated | Update fix instructions |
| New Unity version | Review API changes |

---

## Unity ECS / DOTS Fixes

### ISystem Best Practice

**Detection**: Using SystemBase or Entities.ForEach (older APIs)

**Pattern** (from [Unity ECS Docs](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/ecs-workflow-intro.html)):
```csharp
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;

        // Burst-compatible job
        foreach (var (transform, speed) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<MoveSpeed>>())
        {
            transform.ValueRW.Position += speed.ValueRO.Value * dt;
        }
    }
}
```

**Auto-Apply**: Yes (suggest ISystem over SystemBase)

### EntityCommandBuffer for Structural Changes

**Detection**: Creating/destroying entities in system loop

**Pattern**:
```csharp
[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (spawner, entity) in
            SystemAPI.Query<RefRO<Spawner>>().WithEntityAccess())
        {
            // Queue structural change - don't modify directly
            ecb.Instantiate(spawner.ValueRO.Prefab);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
```

**Auto-Apply**: Yes (use ECB for structural changes)

---

## Addressables Fixes

### Async Loading with Proper Release

**Detection**: Memory leaks from unreleased Addressables

**Pattern** (from [Unity Addressables Docs](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/load-assets-asynchronous.html)):
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

AsyncOperationHandle<GameObject> handle;

async void LoadAsset()
{
    handle = Addressables.LoadAssetAsync<GameObject>("MyPrefab");
    await handle.Task;

    if (handle.Status == AsyncOperationStatus.Succeeded)
    {
        var instance = Instantiate(handle.Result);
    }
}

void OnDestroy()
{
    // CRITICAL: Release handle when done
    Addressables.Release(handle);
}
```

**Auto-Apply**: Yes (add Release in OnDestroy)

### WaitForCompletion Caution

**Detection**: Using WaitForCompletion on remote assets

**Pattern**:
```csharp
// SAFE: Local/cached assets
var handle = Addressables.LoadAssetAsync<GameObject>("LocalAsset");
var result = handle.WaitForCompletion();  // OK

// DANGEROUS: Remote assets - can cause stall
// Don't use WaitForCompletion for remote downloads!
// Use async pattern instead
```

**Auto-Apply**: Warning only

---

## ScriptableObject Architecture Fixes

### ScriptableObject Event System

**Detection**: Need decoupled event system

**Pattern** (from [ScriptableObject-Architecture](https://github.com/DanielEverland/ScriptableObject-Architecture)):
```csharp
// GameEvent.cs - Shared event channel
[CreateAssetMenu(menuName = "Events/GameEvent")]
public class GameEvent : ScriptableObject
{
    List<GameEventListener> listeners = new();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised();
    }

    public void Register(GameEventListener listener) => listeners.Add(listener);
    public void Unregister(GameEventListener listener) => listeners.Remove(listener);
}

// GameEventListener.cs - On any GameObject
public class GameEventListener : MonoBehaviour
{
    public GameEvent gameEvent;
    public UnityEvent response;

    void OnEnable() => gameEvent.Register(this);
    void OnDisable() => gameEvent.Unregister(this);
    public void OnEventRaised() => response.Invoke();
}
```

**Source**: [PaddleGameSO](https://github.com/UnityTechnologies/PaddleGameSO)

**Auto-Apply**: Partial (suggest SO event pattern)

### ScriptableObject Variable

**Detection**: Need shared variable between systems

**Pattern**:
```csharp
[CreateAssetMenu(menuName = "Variables/FloatVariable")]
public class FloatVariable : ScriptableObject
{
    public float Value;
    public event Action<float> OnValueChanged;

    public void SetValue(float value)
    {
        Value = value;
        OnValueChanged?.Invoke(value);
    }
}

// Usage: Reference same SO asset in multiple scripts
public class HealthBar : MonoBehaviour
{
    public FloatVariable playerHealth;

    void OnEnable() => playerHealth.OnValueChanged += UpdateBar;
    void OnDisable() => playerHealth.OnValueChanged -= UpdateBar;
}
```

**Auto-Apply**: Partial (suggest SO variable pattern)

---

## UniTask Fixes

### UniTask Basic Pattern

**Detection**: Using Task or coroutines where UniTask better

**Pattern** (from [UniTask GitHub](https://github.com/Cysharp/UniTask)):
```csharp
using Cysharp.Threading.Tasks;

// Zero-allocation async
async UniTaskVoid Start()
{
    await UniTask.Delay(1000);  // 1 second delay

    // Frame-based delay
    await UniTask.DelayFrame(10);

    // Wait for condition
    await UniTask.WaitUntil(() => player.IsReady);
}

// With cancellation
async UniTask LoadDataAsync(CancellationToken ct)
{
    var www = UnityWebRequest.Get(url);
    await www.SendWebRequest().WithCancellation(ct);
}
```

**Auto-Apply**: Partial (suggest UniTask)

### UniTask Cancellation

**Detection**: Async task continues after GameObject destroyed

**Pattern**:
```csharp
CancellationTokenSource cts;

void OnEnable()
{
    cts = new CancellationTokenSource();
    RunAsync(cts.Token).Forget();
}

void OnDisable()
{
    cts?.Cancel();
    cts?.Dispose();
}

async UniTaskVoid RunAsync(CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        await UniTask.Delay(100, cancellationToken: ct);
        // Work...
    }
}
```

**Auto-Apply**: Yes (add cancellation token)

---

## Input System Fixes

### Input Action in Code

**Detection**: Need runtime input handling

**Pattern** (from [Unity Input System Docs](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Actions.html)):
```csharp
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    InputAction moveAction;
    InputAction fireAction;

    void Awake()
    {
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        fireAction = new InputAction("Fire", binding: "<Mouse>/leftButton");
        fireAction.performed += OnFire;
    }

    void OnEnable()
    {
        moveAction.Enable();
        fireAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        fireAction.Disable();
    }

    void Update()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        transform.Translate(move * Time.deltaTime * 5f);
    }

    void OnFire(InputAction.CallbackContext ctx) => Debug.Log("Fire!");
}
```

**Auto-Apply**: Partial (suggest Input Action pattern)

### Input Action Phases

**Detection**: Need to handle press/hold/release

**Pattern**:
```csharp
// started = key pressed
// performed = action complete (for buttons, same as started)
// canceled = key released

action.started += ctx => Debug.Log("Started");
action.performed += ctx => Debug.Log("Performed");
action.canceled += ctx => Debug.Log("Canceled");

// For hold detection
action.started += ctx => isHolding = true;
action.canceled += ctx => isHolding = false;
```

**Auto-Apply**: Yes (add phase callbacks)

---

## Hand VFX Patterns (from TouchingHologram/HoloKit)

### Velocity-Driven VFX Emission

**Detection**: Need hand movement to drive particle spawn rate

**Pattern** (from [TouchingHologram](https://github.com/holoi/touching-hologram)):
```csharp
// HandVFXController pattern
float speed = handVelocity.magnitude;
vfx.SetFloat("SpawnRate", Mathf.Lerp(0, 1000, speed / maxSpeed));
vfx.SetVector3("EmitVelocity", handVelocity.normalized * emitSpeed);
```

**Auto-Apply**: Yes (add velocity calculation)

### Pinch-Triggered VFX Burst

**Detection**: Need gesture to trigger VFX event

**Pattern**:
```csharp
bool wasPinching = false;

void Update()
{
    if (isPinching && !wasPinching)
    {
        vfx.SendEvent("OnPinch");
        vfx.SetVector3("BurstPosition", pinchPosition);
    }
    wasPinching = isPinching;
}
```

**Auto-Apply**: Yes (add state tracking for edge detection)

---

## AR Depth Compute Shader Fixes

### UV Adjustment for ARKit Depth

**Detection**: Particles appear rotated or offset from depth

**Cause**: AR depth textures have different UV orientation/resolution than screen

**Pattern** (from [YoHana19/HumanParticleEffect](https://github.com/YoHana19/HumanParticleEffect)):
```hlsl
// UV adjustment for depth texture → screen UV
float2 adjustUV(float2 uv, float uVMultiplierPortrait) {
    // Portrait mode
    float2 forMask = float2(
        (1.0 - (uVMultiplierPortrait * 0.5f)) + (uv.x / uVMultiplierPortrait),
        uv.y
    );
    return float2(1.0 - forMask.y, 1.0 - forMask.x);
}

// C# calculation
float CalculateUVMultiplierPortrait(Texture tex) {
    float screenAspect = (float)Screen.height / Screen.width;
    float cameraTextureAspect = (float)tex.width / tex.height;
    return screenAspect / cameraTextureAspect;
}
```

**Source**: [Qiita Article](https://qiita.com/yohanashima/items/dd3f1ea20fc783bbcd8c)

**Auto-Apply**: Partial (suggest UV formula)

### ARKit Depth Scale Factor

**Detection**: Particles at wrong distance from camera

**Cause**: ARKit depth needs empirical correction

**Pattern**:
```hlsl
// 0.625 is empirical correction factor for ARKit depth
float correctedDepth = rawDepth * 0.625f;
float3 worldPos = cameraPos + rayDir * correctedDepth;
```

**Note**: Factor may vary by device - test on target hardware

**Auto-Apply**: Partial (suggest multiplier)

### Compute Thread Group 32x32 Pattern

**Detection**: "Thread group count exceeds maximum" or visual artifacts

**Cause**: Thread group size mismatch between shader and dispatch

**Pattern** (from MetavidoVFX ARDepthSource):
```hlsl
// In compute shader
[numthreads(32,32,1)]  // 1024 threads = Metal max
void DepthToWorld(uint3 id : SV_DispatchThreadID) { ... }
```

```csharp
// C# dispatch - MUST match shader
int groupsX = Mathf.CeilToInt(width / 32f);  // Use 32, not 8
int groupsY = Mathf.CeilToInt(height / 32f);
computeShader.Dispatch(kernel, groupsX, groupsY, 1);
```

**Auto-Apply**: Yes (fix thread group calculation)

### iOS Portrait Depth Rotation

**Detection**: Depth misaligned in portrait mode

**Cause**: ARKit depth is always landscape orientation

**Pattern**:
```hlsl
// Rotate UV 90° CW for portrait mode
float2 rotatedUV = float2(1.0 - uv.y, uv.x);
```

```csharp
// C# - negate tanH after rotation
if (rotateDepthTexture) {
    RayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV);
}
```

**Auto-Apply**: Partial (suggest rotation pattern)

---

## VFX Custom HLSL Fixes

### VFX HLSL Function Signature

**Detection**: Custom HLSL block not working in VFX Graph

**Cause**: Wrong function signature

**Pattern** (from [Unity Docs](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/CustomHLSL-Common.html)):
```hlsl
// ✅ CORRECT - void return, inout VFXAttributes
void CustomFunction(inout VFXAttributes attributes)
{
    attributes.position += float3(0, 1, 0);
}

// ✅ CORRECT - with additional parameters
void CustomFunction(inout VFXAttributes attributes, in float Intensity)
{
    attributes.position *= Intensity;
}

// ❌ WRONG - no return values from VFX custom functions
float3 GetOffset() { return float3(0, 1, 0); }
```

**Auto-Apply**: Yes (fix function signature)

### SampleLevel vs Sample in VFX

**Detection**: "Sample() not found" error in VFX custom HLSL

**Cause**: VFX runs in compute context - no derivatives

**Pattern**:
```hlsl
// ✅ CORRECT - SampleLevel required (no derivatives in compute)
float4 color = myTexture.SampleLevel(mySampler, uv, 0);

// ❌ WRONG - Sample() not available in compute
float4 color = myTexture.Sample(mySampler, uv);

// ✅ Alternative - Load() for exact pixel access (faster)
float value = myTexture.Load(int3(pixelX, pixelY, 0));
```

**Auto-Apply**: Yes (replace Sample with SampleLevel)

### Conditional VFX Attributes

**Detection**: Attribute error for optional attributes

**Pattern**:
```hlsl
#if VFX_HAS_ATTR_VELOCITY
    attributes.velocity = float3(0, 1, 0);
#endif

#if VFX_HAS_ATTR_COLOR
    attributes.color = float4(1, 0, 0, 1);
#endif
```

**Auto-Apply**: Yes (wrap with #if guards)

### m_ShaderFile Reference Issue

**Detection**: "cannot convert VFXAttributes to Texture2D" error

**Cause**: External HLSL file has conflicting function signatures

**Pattern** (in .vfx YAML):
```yaml
# If using inline HLSL code and getting conflicts, set to null
m_ShaderFile: {fileID: 0}

# Or if referencing file, ensure no signature conflicts
m_ShaderFile: {fileID: 10900000, guid: abc123..., type: 3}
```

**Auto-Apply**: Partial (suggest checking m_ShaderFile)

---

## VFX Global Texture Limitation

### VFX Cannot Read Global Textures

**Detection**: VFX texture property stays null despite Shader.SetGlobalTexture

**Cause**: VFX Graph cannot read textures set via Shader.SetGlobal*

**Pattern**:
```csharp
// ❌ DOES NOT WORK for VFX
Shader.SetGlobalTexture("_DepthMap", depth);

// ✅ WORKS - must set per-VFX
vfx.SetTexture("DepthMap", depth);

// Exception: Vectors/Matrices CAN be global
Shader.SetGlobalVector("_ARRayParams", rayParams);  // ✅ Works
Shader.SetGlobalMatrix("_ARInverseView", invView);  // ✅ Works
```

**Auto-Apply**: Yes (replace global texture with per-VFX)

---

## Audio VFX Memory Optimization (from LaspVFX)

### RFloat Texture Format

**Detection**: Audio data texture using RGBA (wasteful)

**Cause**: RGBA uses 16 bytes/pixel vs RFloat's 4 bytes

**Pattern** (from [keijiro/LaspVfx](https://github.com/keijiro/LaspVfx)):
```csharp
// BEFORE: RGBA (wasteful)
var texture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false);

// AFTER: RFloat (4x smaller)
var texture = new Texture2D(width, 1, TextureFormat.RFloat, false) {
    filterMode = FilterMode.Point,
    wrapMode = TextureWrapMode.Clamp
};
```

**Platform Note**: iOS Metal supports RFloat (MTLPixelFormatR32Float)

**Auto-Apply**: Yes (suggest RFloat for single-channel data)

### NativeArray + LoadRawTextureData (Zero GC)

**Detection**: SetPixels causing GC every frame

**Cause**: SetPixels allocates managed array

**Pattern**:
```csharp
// BEFORE: GC every frame
Color[] pixels = new Color[4];
texture.SetPixels(pixels);

// AFTER: Zero GC
NativeArray<float> buffer;

void Start() {
    buffer = new NativeArray<float>(count, Allocator.Persistent);
}

void Update() {
    buffer[0] = value1;
    buffer[1] = value2;
    texture.LoadRawTextureData(buffer);  // Direct memcpy
    texture.Apply(false, false);
}

void OnDestroy() {
    if (buffer.IsCreated) buffer.Dispose();
}
```

**Source**: [keijiro/LaspVfx VFXWaveformBinder.cs](https://github.com/keijiro/LaspVfx)

**Auto-Apply**: Yes (suggest NativeArray pattern)

---

## VFX Event Attribute Pooling

### VFXEventAttribute Caching

**Detection**: Creating VFXEventAttribute every frame

**Pattern**:
```csharp
// ❌ WRONG - creates GC pressure
void TriggerEffect() {
    var attr = vfx.CreateVFXEventAttribute();  // Allocates!
    attr.SetVector3("position", pos);
    vfx.SendEvent("Spawn", attr);
}

// ✅ CORRECT - pool the attribute
VFXEventAttribute cachedAttr;

void Start() {
    cachedAttr = vfx.CreateVFXEventAttribute();
}

void TriggerEffect() {
    cachedAttr.SetVector3("position", pos);
    vfx.SendEvent("Spawn", cachedAttr);
}
```

**Auto-Apply**: Yes (cache VFXEventAttribute)

### VFX Output Events (GPU → CPU)

**Detection**: Need particle to trigger C# code

**Pattern** (from [Unity Docs](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/OutputEvent.html)):
```csharp
void OnEnable() {
    vfx.outputEventReceived += OnOutputEvent;
}

void OnDisable() {
    vfx.outputEventReceived -= OnOutputEvent;
}

void OnOutputEvent(VFXOutputEventArgs args) {
    Vector3 position = args.eventAttribute.GetVector3("position");
    Vector4 color = args.eventAttribute.GetVector4("color");

    // Spawn audio, trigger effects, etc.
    AudioSource.PlayClipAtPoint(clip, position);
}
```

**Auto-Apply**: Partial (suggest output event pattern)

---

## WebXR Context Fixes

### XR-Compatible WebGL Context

**Detection**: WebXR session fails to start

**Cause**: WebGL context not XR-compatible

**Pattern** (from [WebXR Explainer](https://immersive-web.github.io/webxr/explainer.html)):
```javascript
// Option 1: Create XR-compatible from start (preferred)
let gl = canvas.getContext("webgl", { xrCompatible: true });

// Option 2: Make existing context compatible (may cause context loss)
gl.makeXRCompatible().then(() => {
    // Ready for XR
});

// Handle context loss
canvas.addEventListener("webglcontextlost", (event) => {
    event.preventDefault();
});

canvas.addEventListener("webglcontextrestored", () => {
    loadSceneGraphics(gl);
});
```

**Auto-Apply**: Partial (suggest xrCompatible flag)

---

## AR Camera WebRTC Capture

### AR Camera to WebRTC Stream

**Detection**: Need to stream AR camera via WebRTC

**Cause**: AR background uses external textures (YCbCr) incompatible with WebRTC

**Pattern** (from [AR Foundation Issue #973](https://github.com/Unity-Technologies/arfoundation-samples/issues/973)):
```csharp
// URP: Use RenderPipelineManager callback (not OnPostRender)
void OnEnable() {
    RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
}

void OnDisable() {
    RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
}

void OnEndCameraRendering(ScriptableRenderContext context, Camera camera) {
    // Blit AR background (converts YCbCr → RGBA)
    Graphics.Blit(null, targetRT, arCameraBackground.material);

    // For performance: use AsyncGPUReadback
    AsyncGPUReadback.Request(targetRT, 0, TextureFormat.RGBA32, OnGPUReadback);
}

void OnGPUReadback(AsyncGPUReadbackRequest request) {
    if (!request.hasError) {
        NativeArray<byte> data = request.GetData<byte>();
        // Send to WebRTC without blocking main thread
    }
}
```

**Auto-Apply**: Partial (suggest async readback pattern)

---

## Rcam RayParams Calculation

### RayParams for Depth Reconstruction

**Detection**: Need camera params for VFX depth → world conversion

**Pattern** (from MetavidoVFX ARDepthSource):
```csharp
// RayParams: (centerShiftX, centerShiftY, tanH, tanV)
var proj = arCamera.projectionMatrix;
float centerShiftX = proj.m02;
float centerShiftY = proj.m12;
float fov = arCamera.fieldOfView * Mathf.Deg2Rad;
float tanV = Mathf.Tan(fov * 0.5f);
float tanH = tanV * arCamera.aspect;

RayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
```

**HLSL Usage**:
```hlsl
float3 DepthToWorld(float2 uv, float depth, float4 rayParams, float4x4 invView) {
    float3 p = float3((uv - 0.5) * 2, 1);
    p.xy *= float2(rayParams.z, rayParams.w);  // Apply tan(fov/2)
    p.xy += rayParams.xy;  // Apply center shift
    return mul(invView, float4(p * depth, 1)).xyz;
}
```

**Auto-Apply**: Partial (suggest RayParams pattern)

### InverseProjection Calculation (Rcam3/4)

**Detection**: VFX expects InverseProjection Vector4

**Pattern** (from [keijiro/Rcam3](https://github.com/keijiro/Rcam3)):
```csharp
public static Vector4 GetInverseProjection(Matrix4x4 projectionMatrix) {
    var x = 1 / projectionMatrix[0, 0];  // 1 / focal_x
    var y = 1 / projectionMatrix[1, 1];  // 1 / focal_y
    var z = projectionMatrix[0, 2] * x;  // center_x / focal_x
    var w = projectionMatrix[1, 2] * y;  // center_y / focal_y
    return new Vector4(x, y, z, w);
}
```

**Auto-Apply**: Yes (add InverseProjection calculation)

---

## Performance Patterns

### Blocking Microphone Init - Main Thread Freeze

**Detection**: `Microphone.GetPosition(device) <= 0` inside `while` loop in `Start()`

**Cause**: Blocking main thread waiting for audio device, causes 100-500ms freeze on iOS

**Pattern**:
```csharp
// BEFORE (blocks main thread)
void Start()
{
    while (Microphone.GetPosition(device) <= 0) { }  // FREEZES!
}

// AFTER (async pattern)
IEnumerator WaitForMicrophone()
{
    while (Microphone.GetPosition(device) <= 0)
        yield return null;  // Yields frame, doesn't freeze
}
void Start() => StartCoroutine(WaitForMicrophone());
```

**Auto-Apply**: Yes (convert to coroutine pattern)

### FindFirstObjectByType Every Frame - GC Allocation

**Detection**: `FindFirstObjectByType()` called in `Update()` or `LateUpdate()`

**Cause**: Scanning entire scene each frame creates GC pressure

**Pattern**:
```csharp
// BEFORE (GC every frame)
void Update() {
    var ar = FindFirstObjectByType<ARDepthSource>();
}

// AFTER (cache at startup)
ARDepthSource _arDepth;
void OnEnable() => _arDepth ??= FindFirstObjectByType<ARDepthSource>();
void Update() { if (_arDepth != null) _arDepth.UpdateData(); }
```

**Auto-Apply**: Yes (add null-coalesced cache)

### GetComponent Caching for Performance

**Detection**: `GetComponent<T>()` called multiple times in same class

**Cause**: Reflection overhead, multiple dictionary lookups

**Pattern**:
```csharp
// BEFORE (redundant lookups)
void Start() { _vfx = GetComponent<VisualEffect>(); }
void OnEnable() { if (_vfx == null) _vfx = GetComponent<VisualEffect>(); }

// AFTER (single lookup in Awake)
VisualEffect _vfx;
void Awake() => _vfx = GetComponent<VisualEffect>();
```

**Auto-Apply**: Yes (move GetComponent to Awake)

### TryGetComponent vs GetComponent Null Check

**Detection**: `comp = GetComponent<T>(); if (comp == null)`

**Cause**: Verbose null-checking pattern

**Pattern**:
```csharp
// BEFORE (verbose)
MyComponent comp = GetComponent<MyComponent>();
if (comp == null) return;

// AFTER (modern C# 8.0+)
if (!TryGetComponent(out MyComponent comp)) return;
```

**Auto-Apply**: Yes (use TryGetComponent)

### FindFirstObjectByType Singleton Caching

**Detection**: `FindFirstObjectByType<Singleton>()` called repeatedly

**Pattern**:
```csharp
static ARDepthSource _instance;
public static ARDepthSource Instance {
    get { _instance ??= FindFirstObjectByType<ARDepthSource>(); return _instance; }
}
void OnDestroy() => _instance = null;
```

**Auto-Apply**: Yes (add caching pattern)

---

## Safety Patterns

### VFX HasTexture Guard Before SetTexture

**Detection**: `SetTexture()` called without checking if property exists

**Cause**: Unity logs error if VFX doesn't have that property

**Pattern**:
```csharp
// BEFORE (logs errors)
vfx.SetTexture("DepthMap", texture);

// AFTER (guarded)
if (vfx.HasTexture("DepthMap"))
    vfx.SetTexture("DepthMap", texture);
```

**Auto-Apply**: Yes (add HasTexture guards)

### RenderTexture Release with IsCreated Check

**Detection**: `OnDestroy()` releases RenderTexture without validation

**Pattern**:
```csharp
// BEFORE (unsafe)
void OnDestroy() { renderTexture.Release(); }

// AFTER (safe)
void OnDestroy() {
    if (renderTexture != null && renderTexture.IsCreated())
        renderTexture.Release();
    renderTexture = null;
}
```

**Auto-Apply**: Yes (add IsCreated check)

### GraphicsBuffer Validation Before ReadPixels

**Detection**: `ReadPixels()` or `AsyncGPUReadback` on unvalidated texture

**Pattern**:
```csharp
// BEFORE (crashes)
AsyncGPUReadback.Request(_velocityMapRT);

// AFTER (safe)
if (_velocityMapRT == null || !_velocityMapRT.IsCreated() ||
    _velocityMapRT.width <= 0) return Vector3.zero;
AsyncGPUReadback.Request(_velocityMapRT);
```

**Auto-Apply**: Yes (add texture validation)

### VFX Texture Dimension Validation

**Detection**: `SetTexture` succeeds but texture not visible in VFX

**Cause**: Texture dimension mismatch (Tex2D vs Tex3D)

**Pattern**:
```csharp
if (texture != null && texture.dimension == TextureDimension.Tex2D)
    vfx.SetTexture("DepthMap", texture);
```

**Auto-Apply**: Yes (add dimension validation)

### Namespace Collision with UnityEngine

**Detection**: `using XRRAI.Debug;` conflicts with `UnityEngine.Debug`

**Fix**: Rename namespace `Debug` → `Debugging`

**Auto-Apply**: Yes (rename namespace)

---

## Integration Patterns

### MCP Server Multiple Instances Conflict

**Detection**: `Multiple [MCP] instances detected` or tool timeouts

**Cause**: Multiple apps spawned MCP servers on same ports

**Fix**:
```bash
mcp-kill-dupes  # Kill duplicate servers
lsof -i :6400   # Verify Unity MCP port free
```

**Auto-Apply**: Yes (run mcp-kill-dupes at session start)

### Conditional Debug Logging Pattern

**Detection**: Debug logging scattered with no control

**Pattern**:
```csharp
public static class DebugFlags {
    [Conditional("DEBUG_VFX")]
    public static void VFXLog(string msg) => Debug.Log($"[VFX] {msg}");
}
// Usage - NO runtime cost if DEBUG_VFX not defined!
DebugFlags.VFXLog("Binding complete");
```

**Auto-Apply**: Yes (add Conditional attributes)

---

## Advanced Architecture Patterns

### ITrackingProvider Interface Pattern

**Detection**: Multiple tracking sources (ARKit, HoloKit, MediaPipe) need unified access

**Pattern**:
```csharp
public interface ITrackingProvider {
    string Id { get; }
    bool IsAvailable { get; }
    void Initialize();
    TrackingData GetLatestData();
    event Action OnTrackingLost;
}

[TrackingProvider("arkit-body", priority: 90)]
public class ARKitBodyProvider : ITrackingProvider { }
```

**Auto-Apply**: No (architectural decision)

### Hysteresis for Gesture Detection

**Detection**: Flickering gesture state (pinch on/off rapidly)

**Pattern**:
```csharp
// Different thresholds for start vs end
float _startThreshold = 0.02f;
float _endThreshold = 0.04f;
if (distance < _startThreshold && !IsPinching) IsPinching = true;
else if (distance > _endThreshold && IsPinching) IsPinching = false;
```

**Auto-Apply**: Yes (add hysteresis dead zone)

### VFX Property ID Caching

**Detection**: `Shader.PropertyToID()` called every frame

**Pattern**:
```csharp
// Cache as static readonly
private static readonly int ID_DepthMap = Shader.PropertyToID("DepthMap");
void LateUpdate() => _vfx.SetTexture(ID_DepthMap, depth);
```

**Auto-Apply**: Yes (pre-cache PropertyIDs)

### Audio Texture Encoding

**Detection**: Multiple audio values need VFX transfer without exposed properties

**Pattern**:
```csharp
// Encode 8 values in 2x2 texture
Color[] pixels = new Color[4] {
    new Color(volume, bass, mids, treble),
    new Color(subBass, beatPulse, beatIntensity, 0),
    Color.clear, Color.clear
};
_audioTex.SetPixels(pixels);
_vfx.SetTexture("AudioDataTexture", _audioTex);
```

**Auto-Apply**: Yes

### Lazy Initialization with _initialized Flag

**Detection**: Initialize() called multiple times or components not ready

**Pattern**:
```csharp
private bool _initialized = false;
public bool IsAvailable => _initialized && _component != null;
public void Initialize() {
    if (_initialized) return;
    _component = FindFirstObjectByType<MyComponent>();
    _initialized = true;
}
```

**Auto-Apply**: Yes

### Event-Based Tracking State Changes

**Detection**: Polling tracking state in Update() wasteful

**Pattern**:
```csharp
void Initialize() {
    _bodyManager.humanBodiesChanged += OnBodiesChanged;
}
void OnBodiesChanged(ARHumanBodiesChangedEventArgs args) {
    if (args.added.Count > 0) OnTrackingFound?.Invoke();
    if (args.removed.Count > 0) OnTrackingLost?.Invoke();
}
```

**Auto-Apply**: Yes (subscribe to events)

### Domain Reload Resource Disposal

**Detection**: VFX window corruption or WebRTC errors after script compile

**Pattern**:
```csharp
#if UNITY_EDITOR
[InitializeOnLoad]
static class DomainReloadFixes {
    static DomainReloadFixes() {
        AssemblyReloadEvents.beforeAssemblyReload += () => {
            foreach (var w in Resources.FindObjectsOfTypeAll<VFXViewWindow>())
                w.Close();
        };
    }
}
#endif
```

**Auto-Apply**: Partial

### Beat Detection with Adaptive Threshold

**Detection**: Beat events trigger constantly or not at all

**Pattern**:
```csharp
float threshold = averagePower * _beatMultiplier;
if (currentPower > threshold && _timeSinceLastBeat > _minInterval) {
    IsOnset = true;
    _timeSinceLastBeat = 0f;
}
BeatPulse = Mathf.Max(0, 1 - _timeSinceLastBeat / _decayTime);
```

**Auto-Apply**: Partial (requires tuning)

### SetGraphicsBuffer vs SetGlobalBuffer

**Detection**: VFX can't access buffer data

**Pattern**:
```csharp
// For VFX Graph property:
vfx.SetGraphicsBuffer("KeypointBuffer", buffer);

// For HLSL global access:
Shader.SetGlobalBuffer("_GlobalKeypoints", buffer);
```

**Auto-Apply**: Yes

### ReadPixels Race Condition Handling

**Detection**: "ReadPixels was called to read pixels from system memory" error

**Pattern**:
```csharp
try {
    _tex.ReadPixels(rect, 0, 0);
    return _tex.GetPixel(0, 0).r;
} catch (UnityException) {
    return _lastValue;  // Use cached value
}
```

**Auto-Apply**: Yes (wrap in try-catch)

---

## Adding New Patterns

When adding new auto-fix patterns:

```markdown
### [Error Name]

**Detection**: [regex or description]
**Cause**: [why this happens]
**Fix**: [code or steps]
**Auto-Apply**: Yes/Partial/No + reason
```

---

**Last Updated**: 2026-01-22
**Patterns**: 111 active (+22 from deep codebase analysis)
**Auto-Apply Rate**: 85%
**Categories**: Unity C#, AR Foundation, VFX Graph, Performance, Safety, Architecture

## Official Documentation
- [Unity VFX Component API](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@7.1/manual/ComponentAPI.html)
- [Unity AR Foundation Samples](https://github.com/Unity-Technologies/arfoundation-samples)
- [Unity XR Hands Gestures](https://docs.unity3d.com/Packages/com.unity.xr.hands@1.4/manual/gestures/custom-gestures.html)
- [Unity Burst User Guide](https://docs.unity3d.com/Packages/com.unity.burst@1.3/manual/index.html)
- [Unity Sentis Documentation](https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/index.html)
- [Netcode for GameObjects](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [Unity UI Toolkit Manual](https://docs.unity3d.com/Manual/UIElements.html)
- [Shader Graph Custom Function](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.2/manual/Custom-Function-Node.html)
- [Unity Mobile Optimization](https://unity.com/resources/mobile-xr-web-game-performance-optimization-unity-6)
- [Unity WebGL Memory](https://unity.com/blog/engine-platform/understanding-memory-in-unity-webgl)
- [Unity Blog - ARKit 4 Depth](https://blog.unity.com/technology/ar-foundation-support-for-arkit-4-depth)
- [Unity ECS Entities](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/ecs-workflow-intro.html)
- [Unity Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/load-assets-asynchronous.html)
- [Unity Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Actions.html)

## Third-Party Documentation
- [Needle Engine Docs](https://engine.needle.tools/docs/)
- [Normcore Docs](https://docs.normcore.io/)
- [Open Brush Docs](https://docs.openbrush.app/)
- [Catlike Coding](https://catlikecoding.com/unity/tutorials/)
- [Kodeco Unity Tutorials](https://www.kodeco.com/)

## GitHub Repositories
- [keijiro VFX Graph](https://github.com/keijiro) - VFX, Sentis, depth
- [keijiro/LaspVfx](https://github.com/keijiro/LaspVfx) - Audio-reactive VFX patterns
- [keijiro/Rcam3](https://github.com/keijiro/Rcam3) - AR depth reconstruction
- [holoi/touching-hologram](https://github.com/holoi/touching-hologram) - Hand tracking VFX
- [YoHana19/HumanParticleEffect](https://github.com/YoHana19/HumanParticleEffect) - AR human depth patterns
- [HoloKit Unity SDK](https://github.com/holokit/holokit-unity-sdk) - Hand tracking
- [Open Brush Toolkit](https://github.com/icosa-foundation/open-brush-toolkit) - Brush integration
- [Unity Mobile Optimization](https://github.com/GuardianOfGods/unity-mobile-optimization) - Mobile patterns
- [Needle Engine Support](https://github.com/needle-tools/needle-engine-support) - Web export
- [Unity Netcode Samples](https://github.com/Unity-Technologies/com.unity.netcode.gameobjects) - Multiplayer
- [UniTask](https://github.com/Cysharp/UniTask) - Zero-allocation async/await
- [ScriptableObject-Architecture](https://github.com/DanielEverland/ScriptableObject-Architecture) - SO patterns
- [PaddleGameSO](https://github.com/UnityTechnologies/PaddleGameSO) - Official SO demo
- [InputSystem Warriors](https://github.com/UnityTechnologies/InputSystem_Warriors) - Input System example
- [De-Panther/unity-webxr-export](https://github.com/De-Panther/unity-webxr-export) - WebXR for Unity
