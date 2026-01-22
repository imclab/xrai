# Modular Tracking Architecture: Tracker & Platform Agnostic

**Created**: 2026-01-20
**Principle**: Auto-discovery, auto-binding, hot-swappable

---

## Core Design

```
┌────────────────────────────────────────────────────────────────────┐
│                      TRACKING SERVICE                               │
│  TrackingService.Instance (singleton orchestrator)                 │
│  ├─ Auto-discovers available providers                             │
│  ├─ Auto-binds to consumers                                        │
│  └─ Hot-swaps on capability change                                 │
└────────────────────────────────────────────────────────────────────┘
         ▲                                         │
         │ ITrackingProvider                       │ ITrackingConsumer
         │ (inputs)                                ▼ (outputs)
┌────────┴───────────┐                  ┌─────────┴──────────┐
│ PROVIDER REGISTRY  │                  │ CONSUMER REGISTRY  │
│ ├─ ARKitProvider   │                  │ ├─ VFXConsumer     │
│ ├─ MetaProvider    │                  │ ├─ AvatarConsumer  │
│ ├─ SentisProvider  │                  │ ├─ NetworkConsumer │
│ ├─ WebXRProvider   │                  │ └─ AudioConsumer   │
│ └─ (auto-register) │                  │   (auto-register)  │
└────────────────────┘                  └────────────────────┘
```

---

## Auto-Registration Pattern

### Providers (Inputs)

```csharp
// Providers self-register via attribute
[TrackingProvider(
    Priority = 100,                    // Higher = preferred
    Platforms = Platform.iOS | Platform.visionOS,
    Capabilities = TrackingCap.HandTracking21 | TrackingCap.BodySkeleton91
)]
public class ARKitBodyProvider : ITrackingProvider
{
    // Implementation discovers itself at runtime
}

// Or via ScriptableObject for inspector configuration
[CreateAssetMenu(menuName = "Tracking/Provider Config")]
public class ProviderConfig : ScriptableObject
{
    public string providerType;
    public int priority;
    public Platform platforms;
}
```

### Consumers (Outputs)

```csharp
// Consumers declare what they need via attribute
[TrackingConsumer(
    Required = TrackingCap.HandTracking21,
    Optional = TrackingCap.PoseKeypoints17
)]
public class HandVFXConsumer : MonoBehaviour, ITrackingConsumer
{
    // Auto-bound by TrackingService when capability available
}
```

---

## Core Interfaces

### ITrackingProvider (Input Side)

```csharp
public interface ITrackingProvider : IDisposable
{
    // Identity
    string Id { get; }
    int Priority { get; }
    Platform SupportedPlatforms { get; }
    TrackingCap Capabilities { get; }
    bool IsAvailable { get; }

    // Lifecycle
    void Initialize();
    void Update();
    void Shutdown();

    // Data access (pull model)
    bool TryGetData<T>(out T data) where T : struct, ITrackingData;

    // Events (push model)
    event Action<TrackingCap> OnCapabilitiesChanged;
    event Action OnTrackingLost;
    event Action OnTrackingFound;
}
```

### ITrackingConsumer (Output Side)

```csharp
public interface ITrackingConsumer
{
    // Requirements
    TrackingCap RequiredCapabilities { get; }
    TrackingCap OptionalCapabilities { get; }

    // Binding
    void OnBind(ITrackingProvider provider);
    void OnUnbind();

    // Data reception
    void OnTrackingData<T>(T data) where T : struct, ITrackingData;
}
```

### ITrackingData (Normalized Data)

```csharp
public interface ITrackingData
{
    double Timestamp { get; }
    float Confidence { get; }
}

public struct HandData : ITrackingData
{
    public double Timestamp { get; set; }
    public float Confidence { get; set; }

    public Handedness Hand;
    public NativeArray<JointPose> Joints;  // Always 21 (normalized)
    public bool IsPinching;
    public float PinchStrength;
    public Vector3 PinchPosition;
    public Vector3 Velocity;
}

public struct BodyData : ITrackingData
{
    public double Timestamp { get; set; }
    public float Confidence { get; set; }

    public NativeArray<JointPose> Skeleton;  // Normalized to 17 or full
    public int OriginalJointCount;           // 17, 70, 91
    public CoordinateSpace Space;
}

public struct SegmentationData : ITrackingData
{
    public double Timestamp { get; set; }
    public float Confidence { get; set; }

    public Texture2D Mask;
    public int PartCount;  // 1 (binary), 24 (BodyPix), N (instance)
}
```

---

## TrackingService (Orchestrator)

```csharp
public class TrackingService : MonoBehaviour
{
    public static TrackingService Instance { get; private set; }

    // Registries
    private readonly List<ITrackingProvider> _providers = new();
    private readonly List<ITrackingConsumer> _consumers = new();
    private readonly Dictionary<TrackingCap, ITrackingProvider> _capabilityMap = new();

    // Auto-discovery on Awake
    void Awake()
    {
        Instance = this;
        DiscoverProviders();
        DiscoverConsumers();
        BindAll();
    }

    // === AUTO-DISCOVERY ===

    void DiscoverProviders()
    {
        // 1. Find all types with [TrackingProvider] attribute
        var providerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<TrackingProviderAttribute>() != null)
            .Where(t => typeof(ITrackingProvider).IsAssignableFrom(t));

        foreach (var type in providerTypes)
        {
            var attr = type.GetCustomAttribute<TrackingProviderAttribute>();

            // Check platform compatibility
            if (!attr.Platforms.HasFlag(CurrentPlatform))
                continue;

            // Instantiate and check availability
            var provider = (ITrackingProvider)Activator.CreateInstance(type);
            if (provider.IsAvailable)
            {
                RegisterProvider(provider);
            }
        }

        // 2. Also check Resources for ScriptableObject configs
        var configs = Resources.LoadAll<ProviderConfig>("TrackingProviders");
        foreach (var config in configs)
        {
            // Instantiate from config...
        }
    }

    void DiscoverConsumers()
    {
        // Find all MonoBehaviours implementing ITrackingConsumer
        var consumers = FindObjectsOfType<MonoBehaviour>()
            .OfType<ITrackingConsumer>();

        foreach (var consumer in consumers)
        {
            RegisterConsumer(consumer);
        }
    }

    // === AUTO-BINDING ===

    void BindAll()
    {
        foreach (var consumer in _consumers)
        {
            TryBindConsumer(consumer);
        }
    }

    void TryBindConsumer(ITrackingConsumer consumer)
    {
        var required = consumer.RequiredCapabilities;

        // Find provider that satisfies all required capabilities
        var provider = _providers
            .Where(p => (p.Capabilities & required) == required)
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        if (provider != null)
        {
            consumer.OnBind(provider);
            Debug.Log($"Bound {consumer.GetType().Name} to {provider.Id}");
        }
        else
        {
            Debug.LogWarning($"No provider found for {consumer.GetType().Name} " +
                           $"(requires {required})");
        }
    }

    // === HOT-SWAP ===

    public void RegisterProvider(ITrackingProvider provider)
    {
        provider.Initialize();
        _providers.Add(provider);
        _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        // Update capability map
        foreach (TrackingCap cap in Enum.GetValues(typeof(TrackingCap)))
        {
            if (provider.Capabilities.HasFlag(cap))
            {
                if (!_capabilityMap.ContainsKey(cap) ||
                    provider.Priority > _capabilityMap[cap].Priority)
                {
                    _capabilityMap[cap] = provider;
                }
            }
        }

        // Re-bind consumers that might benefit
        RebindConsumers();

        provider.OnCapabilitiesChanged += OnProviderCapabilitiesChanged;
    }

    public void UnregisterProvider(ITrackingProvider provider)
    {
        provider.OnCapabilitiesChanged -= OnProviderCapabilitiesChanged;
        _providers.Remove(provider);
        provider.Shutdown();

        // Re-bind affected consumers
        RebindConsumers();
    }

    void OnProviderCapabilitiesChanged(TrackingCap newCaps)
    {
        // Provider capabilities changed (e.g., hand tracking lost)
        RebindConsumers();
    }

    // === DATA FLOW ===

    void Update()
    {
        // Update all active providers
        foreach (var provider in _providers)
        {
            if (provider.IsAvailable)
            {
                provider.Update();
            }
        }

        // Push data to consumers (could also be pull-based)
        PushDataToConsumers();
    }

    void PushDataToConsumers()
    {
        // Hand data
        if (TryGetData<HandData>(Handedness.Left, out var leftHand))
        {
            foreach (var consumer in GetConsumersFor(TrackingCap.HandTracking21))
            {
                consumer.OnTrackingData(leftHand);
            }
        }

        // Body data
        if (TryGetData<BodyData>(out var body))
        {
            foreach (var consumer in GetConsumersFor(TrackingCap.PoseKeypoints17))
            {
                consumer.OnTrackingData(body);
            }
        }

        // Segmentation data
        if (TryGetData<SegmentationData>(out var seg))
        {
            foreach (var consumer in GetConsumersFor(TrackingCap.BodySegmentation24Part))
            {
                consumer.OnTrackingData(seg);
            }
        }
    }

    // === PUBLIC API ===

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        var cap = GetCapabilityForDataType<T>();
        if (_capabilityMap.TryGetValue(cap, out var provider))
        {
            return provider.TryGetData(out data);
        }
        data = default;
        return false;
    }

    public ITrackingProvider GetProviderFor(TrackingCap capability)
    {
        _capabilityMap.TryGetValue(capability, out var provider);
        return provider;
    }
}
```

---

## Example Providers

### ARKit Body Provider (iOS)

```csharp
[TrackingProvider(
    Priority = 100,
    Platforms = Platform.iOS | Platform.visionOS,
    Capabilities = TrackingCap.BodySkeleton91 | TrackingCap.LiDAREnhanced
)]
public class ARKitBodyProvider : ITrackingProvider
{
    public string Id => "ARKit.Body";
    public int Priority => 100;
    public Platform SupportedPlatforms => Platform.iOS | Platform.visionOS;
    public TrackingCap Capabilities => _currentCaps;
    public bool IsAvailable => ARSession.state == ARSessionState.SessionTracking;

    private TrackingCap _currentCaps;
    private ARHumanBodyManager _bodyManager;

    public void Initialize()
    {
        #if UNITY_IOS
        _bodyManager = Object.FindObjectOfType<ARHumanBodyManager>();
        _currentCaps = TrackingCap.BodySkeleton91;

        // Check for LiDAR
        if (ARSession.descriptor.supportsDepth)
            _currentCaps |= TrackingCap.LiDAREnhanced;
        #endif
    }

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        if (typeof(T) == typeof(BodyData))
        {
            data = (T)(object)GetBodyData();
            return true;
        }
        data = default;
        return false;
    }

    private BodyData GetBodyData()
    {
        var body = _bodyManager.trackables.FirstOrDefault();
        if (body == null) return default;

        var joints = new NativeArray<JointPose>(91, Allocator.Temp);
        // Map ARKit skeleton to our format...

        return new BodyData
        {
            Timestamp = Time.timeAsDouble,
            Confidence = body.estimatedHeightScaleFactor,
            Skeleton = joints,
            OriginalJointCount = 91,
            Space = CoordinateSpace.World3D
        };
    }

    // ... rest of interface
}
```

### Sentis/BodyPix Provider (Cross-platform)

```csharp
[TrackingProvider(
    Priority = 50,  // Lower than native
    Platforms = Platform.iOS | Platform.Android | Platform.Standalone,
    Capabilities = TrackingCap.BodySegmentation24Part | TrackingCap.PoseKeypoints17
)]
public class SentisBodyPixProvider : ITrackingProvider
{
    public string Id => "Sentis.BodyPix";
    public int Priority => 50;
    public TrackingCap Capabilities =>
        TrackingCap.BodySegmentation24Part | TrackingCap.PoseKeypoints17;

    private BodyPartSegmenter _segmenter;

    public void Initialize()
    {
        _segmenter = Object.FindObjectOfType<BodyPartSegmenter>();
        _segmenter?.Initialize();
    }

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        if (typeof(T) == typeof(SegmentationData))
        {
            data = (T)(object)new SegmentationData
            {
                Timestamp = Time.timeAsDouble,
                Confidence = 0.9f,
                Mask = _segmenter.MaskTexture,
                PartCount = 24
            };
            return _segmenter.IsReady;
        }

        if (typeof(T) == typeof(BodyData))
        {
            data = (T)(object)GetBodyDataFromKeypoints();
            return _segmenter.IsReady;
        }

        data = default;
        return false;
    }

    private BodyData GetBodyDataFromKeypoints()
    {
        var joints = new NativeArray<JointPose>(17, Allocator.Temp);
        for (int i = 0; i < 17; i++)
        {
            joints[i] = new JointPose
            {
                Position = _segmenter.GetKeypointPosition(i),
                Confidence = _segmenter.GetKeypointScore(i)
            };
        }

        return new BodyData
        {
            Timestamp = Time.timeAsDouble,
            Confidence = joints.Average(j => j.Confidence),
            Skeleton = joints,
            OriginalJointCount = 17,
            Space = CoordinateSpace.Screen2D
        };
    }
}
```

### WebXR Provider (WebGL)

```csharp
[TrackingProvider(
    Priority = 80,
    Platforms = Platform.WebGL,
    Capabilities = TrackingCap.HandTracking21
)]
public class WebXRHandProvider : ITrackingProvider
{
    public string Id => "WebXR.Hands";

    [DllImport("__Internal")]
    private static extern bool WebXR_IsHandTrackingSupported();

    [DllImport("__Internal")]
    private static extern void WebXR_GetHandJoints(int hand, float[] joints);

    public bool IsAvailable => WebXR_IsHandTrackingSupported();

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        if (typeof(T) == typeof(HandData))
        {
            var floats = new float[21 * 4]; // 21 joints × (x,y,z,confidence)
            WebXR_GetHandJoints(0, floats);

            data = (T)(object)ParseHandData(floats, Handedness.Left);
            return true;
        }
        data = default;
        return false;
    }
}
```

---

## Example Consumers

### VFX Consumer (Auto-binds to any hand provider)

```csharp
[TrackingConsumer(
    Required = TrackingCap.HandTracking21,
    Optional = TrackingCap.PoseKeypoints17
)]
public class VFXTrackingConsumer : MonoBehaviour, ITrackingConsumer
{
    [SerializeField] private VisualEffect _vfx;

    public TrackingCap RequiredCapabilities => TrackingCap.HandTracking21;
    public TrackingCap OptionalCapabilities => TrackingCap.PoseKeypoints17;

    private ITrackingProvider _provider;

    public void OnBind(ITrackingProvider provider)
    {
        _provider = provider;
        Debug.Log($"VFX bound to {provider.Id}");
    }

    public void OnUnbind()
    {
        _provider = null;
    }

    public void OnTrackingData<T>(T data) where T : struct, ITrackingData
    {
        switch (data)
        {
            case HandData hand:
                BindHandToVFX(hand);
                break;
            case BodyData body:
                BindBodyToVFX(body);
                break;
        }
    }

    private void BindHandToVFX(HandData hand)
    {
        if (_vfx == null) return;

        var wrist = hand.Joints[0];
        _vfx.SetVector3("HandPosition", wrist.Position);
        _vfx.SetVector3("HandVelocity", hand.Velocity);
        _vfx.SetFloat("HandSpeed", hand.Velocity.magnitude);
        _vfx.SetBool("IsPinching", hand.IsPinching);
        _vfx.SetFloat("PinchStrength", hand.PinchStrength);

        // Bind all 21 joints if VFX supports it
        if (_vfx.HasGraphicsBuffer("HandJoints"))
        {
            // Convert to GraphicsBuffer format...
        }
    }

    private void BindBodyToVFX(BodyData body)
    {
        if (_vfx == null) return;

        // Bind keypoints to VFX
        for (int i = 0; i < Mathf.Min(body.Skeleton.Length, 17); i++)
        {
            var joint = body.Skeleton[i];
            _vfx.SetVector3($"Keypoint{i}", joint.Position);
        }
    }
}
```

### Network Consumer (Sync to remote users)

```csharp
[TrackingConsumer(
    Required = TrackingCap.PoseKeypoints17,
    Optional = TrackingCap.HandTracking21 | TrackingCap.FaceBlendshapes52
)]
public class NetworkTrackingConsumer : MonoBehaviour, ITrackingConsumer
{
    public TrackingCap RequiredCapabilities => TrackingCap.PoseKeypoints17;
    public TrackingCap OptionalCapabilities =>
        TrackingCap.HandTracking21 | TrackingCap.FaceBlendshapes52;

    private WebRTCDataChannel _channel;
    private byte[] _buffer = new byte[2048];

    public void OnTrackingData<T>(T data) where T : struct, ITrackingData
    {
        // Serialize and send
        int size = TrackingSerializer.Serialize(data, _buffer);
        _channel?.Send(_buffer, 0, size);
    }
}
```

### Avatar Consumer (Drive avatar from any tracking source)

```csharp
[TrackingConsumer(
    Required = TrackingCap.None,  // Works with anything
    Optional = TrackingCap.BodySkeleton91 | TrackingCap.BodySkeleton70 |
               TrackingCap.PoseKeypoints17 | TrackingCap.HandTracking21 |
               TrackingCap.FaceBlendshapes52
)]
public class AvatarTrackingConsumer : MonoBehaviour, ITrackingConsumer
{
    [SerializeField] private Animator _animator;
    [SerializeField] private SkinnedMeshRenderer _faceMesh;

    public TrackingCap RequiredCapabilities => TrackingCap.None;
    public TrackingCap OptionalCapabilities =>
        TrackingCap.BodySkeleton91 | TrackingCap.PoseKeypoints17 |
        TrackingCap.HandTracking21 | TrackingCap.FaceBlendshapes52;

    public void OnTrackingData<T>(T data) where T : struct, ITrackingData
    {
        switch (data)
        {
            case BodyData body:
                ApplyBodyToAvatar(body);
                break;
            case HandData hand:
                ApplyHandToAvatar(hand);
                break;
            case FaceData face:
                ApplyFaceToAvatar(face);
                break;
        }
    }

    private void ApplyBodyToAvatar(BodyData body)
    {
        // Map any skeleton (17/70/91) to avatar bones
        var mapper = SkeletonMapper.GetMapper(body.OriginalJointCount);
        mapper.ApplyToAnimator(_animator, body.Skeleton);
    }
}
```

---

## Skeleton Normalization

```csharp
public static class SkeletonMapper
{
    // Common 17-joint format (COCO)
    public static readonly string[] Joints17 = {
        "Nose", "LeftEye", "RightEye", "LeftEar", "RightEar",
        "LeftShoulder", "RightShoulder", "LeftElbow", "RightElbow",
        "LeftWrist", "RightWrist", "LeftHip", "RightHip",
        "LeftKnee", "RightKnee", "LeftAnkle", "RightAnkle"
    };

    // Mapping from 91-joint ARKit to 17-joint common
    private static readonly int[] ARKit91To17 = {
        65, 64, 66, -1, -1,  // Head joints (nose, eyes, ears)
        11, 12, 13, 14, 15, 16,  // Arms
        1, 2, 3, 4, 5, 6  // Legs
    };

    // Mapping from 70-joint Meta to 17-joint common
    private static readonly int[] Meta70To17 = {
        // ... similar mapping
    };

    public static NativeArray<JointPose> NormalizeTo17(
        NativeArray<JointPose> source,
        int sourceJointCount)
    {
        var result = new NativeArray<JointPose>(17, Allocator.Temp);
        var map = sourceJointCount switch
        {
            91 => ARKit91To17,
            70 => Meta70To17,
            17 => Enumerable.Range(0, 17).ToArray(),
            _ => throw new NotSupportedException()
        };

        for (int i = 0; i < 17; i++)
        {
            result[i] = map[i] >= 0 ? source[map[i]] : default;
        }

        return result;
    }
}
```

---

## Hand Joint Normalization

```csharp
public static class HandJointMapper
{
    // Common 21-joint format (MediaPipe)
    public static readonly string[] Joints21 = {
        "Wrist",
        "ThumbCMC", "ThumbMCP", "ThumbIP", "ThumbTip",
        "IndexMCP", "IndexPIP", "IndexDIP", "IndexTip",
        "MiddleMCP", "MiddlePIP", "MiddleDIP", "MiddleTip",
        "RingMCP", "RingPIP", "RingDIP", "RingTip",
        "PinkyMCP", "PinkyPIP", "PinkyDIP", "PinkyTip"
    };

    // XR Hands (26 joints) to 21-joint common
    public static NativeArray<JointPose> From26To21(NativeArray<JointPose> xrHands)
    {
        var result = new NativeArray<JointPose>(21, Allocator.Temp);
        // Map XRHandJointID to our 21-joint format
        // XR Hands has: Wrist, Palm, ThumbMetacarpal, etc.
        result[0] = xrHands[(int)XRHandJointID.Wrist];
        result[1] = xrHands[(int)XRHandJointID.ThumbMetacarpal];
        result[2] = xrHands[(int)XRHandJointID.ThumbProximal];
        // ... etc
        return result;
    }
}
```

---

## Configuration (Inspector-Friendly)

```csharp
[CreateAssetMenu(menuName = "Tracking/Service Config")]
public class TrackingServiceConfig : ScriptableObject
{
    [Header("Provider Priorities")]
    public List<ProviderPriority> providerPriorities = new()
    {
        new() { providerId = "ARKit.Body", priority = 100 },
        new() { providerId = "Meta.Movement", priority = 100 },
        new() { providerId = "Sentis.BodyPix", priority = 50 },
        new() { providerId = "WebXR.Hands", priority = 80 },
        new() { providerId = "MediaPipe.Hands", priority = 30 }
    };

    [Header("Fallback Behavior")]
    public bool enableFallbackProviders = true;
    public bool hotSwapOnCapabilityChange = true;

    [Header("Data Normalization")]
    public bool normalizeSkeletonTo17 = true;
    public bool normalizeHandsTo21 = true;

    [Header("Performance")]
    public int maxProvidersActive = 3;
    public float updateInterval = 0f;  // 0 = every frame
}

[Serializable]
public struct ProviderPriority
{
    public string providerId;
    public int priority;
}
```

---

## Usage Example

```csharp
// Scene Setup - just add components, auto-binding handles the rest

// 1. TrackingService (singleton) - add to scene root
[RequireComponent(typeof(TrackingService))]
public class TrackingSetup : MonoBehaviour { }

// 2. VFX with tracking - just add component, it auto-binds
public class MyVFX : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;

    void Start()
    {
        // Add consumer component - it will auto-register and bind
        var consumer = gameObject.AddComponent<VFXTrackingConsumer>();
        consumer.SetVFX(vfx);
        // That's it! TrackingService handles the rest.
    }
}

// 3. Query tracking directly if needed
void Update()
{
    // Pull model - ask for data
    if (TrackingService.Instance.TryGetData<HandData>(out var hand))
    {
        Debug.Log($"Hand at {hand.Joints[0].Position}");
    }

    // Or check capabilities
    var caps = TrackingService.Instance.AvailableCapabilities;
    if (caps.HasFlag(TrackingCap.BodySkeleton91))
    {
        // Full body tracking available
    }
}
```

---

## File Structure

```
Assets/
├── Scripts/
│   └── Tracking/
│       ├── Core/
│       │   ├── TrackingService.cs
│       │   ├── ITrackingProvider.cs
│       │   ├── ITrackingConsumer.cs
│       │   ├── TrackingData.cs
│       │   └── TrackingCapabilities.cs
│       ├── Providers/
│       │   ├── ARKitBodyProvider.cs
│       │   ├── ARKitHandProvider.cs
│       │   ├── MetaMovementProvider.cs
│       │   ├── SentisBodyPixProvider.cs
│       │   ├── WebXRHandProvider.cs
│       │   └── MediaPipeProvider.cs
│       ├── Consumers/
│       │   ├── VFXTrackingConsumer.cs
│       │   ├── AvatarTrackingConsumer.cs
│       │   └── NetworkTrackingConsumer.cs
│       ├── Mapping/
│       │   ├── SkeletonMapper.cs
│       │   └── HandJointMapper.cs
│       └── Config/
│           └── TrackingServiceConfig.asset
└── Resources/
    └── TrackingProviders/
        └── (ScriptableObject configs)
```

---

## Summary

| Feature | Implementation |
|---------|---------------|
| **Auto-discovery** | Reflection + `[TrackingProvider]` attribute |
| **Auto-binding** | `[TrackingConsumer]` declares requirements |
| **Hot-swap** | `OnCapabilitiesChanged` event triggers rebind |
| **Platform agnostic** | Platform flags in attribute, runtime check |
| **Tracker agnostic** | `ITrackingProvider` interface, any source |
| **Normalized data** | Skeleton → 17-joint, Hands → 21-joint |
| **Push + Pull** | Events for real-time, `TryGetData` for queries |
| **Inspector config** | `TrackingServiceConfig` ScriptableObject |

This architecture allows:
- Adding new tracking sources without modifying existing code
- VFX/Avatar/Network consumers work with any provider
- Graceful degradation when capabilities unavailable
- Hot-swapping when better provider becomes available
- Zero configuration for common cases, full control when needed

---

# Voice Interface Architecture (Same Pattern)

## Core Design

```
┌────────────────────────────────────────────────────────────────────┐
│                       VOICE SERVICE                                 │
│  VoiceService.Instance (singleton orchestrator)                    │
│  ├─ Auto-discovers STT/TTS/LLM providers                          │
│  ├─ Auto-binds to consumers                                        │
│  └─ Hot-swaps on availability change                               │
└────────────────────────────────────────────────────────────────────┘
         ▲                                         │
         │ IVoiceProvider                          │ IVoiceConsumer
         │ (inputs)                                ▼ (outputs)
┌────────┴───────────┐                  ┌─────────┴──────────┐
│ PROVIDER REGISTRY  │                  │ CONSUMER REGISTRY  │
│ ├─ WhisperProvider │                  │ ├─ VFXVoiceConsumer│
│ ├─ AzureSTTProvider│                  │ ├─ CommandConsumer │
│ ├─ WebSpeechProvider│                 │ ├─ AgentConsumer   │
│ ├─ ElevenLabsTTS   │                  │ └─ SubtitleConsumer│
│ └─ (auto-register) │                  │   (auto-register)  │
└────────────────────┘                  └────────────────────┘
```

---

## Voice Capabilities

```csharp
[Flags]
public enum VoiceCap
{
    None = 0,

    // Speech-to-Text
    STT = 1 << 0,              // Basic transcription
    STTStreaming = 1 << 1,     // Real-time streaming
    STTMultilingual = 1 << 2,  // 90+ languages
    STTLocalInference = 1 << 3,// On-device (no cloud)

    // Text-to-Speech
    TTS = 1 << 4,              // Basic synthesis
    TTSStreaming = 1 << 5,     // Real-time streaming
    TTSVoiceCloning = 1 << 6,  // Custom voices
    TTSEmotional = 1 << 7,     // Emotion control
    TTSLocalInference = 1 << 8,// On-device

    // Language Models
    LLM = 1 << 9,              // Text completion
    LLMConversation = 1 << 10, // Multi-turn context
    LLMFunctionCalling = 1 << 11, // Tool use
    LLMLocalInference = 1 << 12,  // On-device

    // Combined
    VoiceAgent = STT | TTS | LLM | LLMConversation,
    FullDuplex = STTStreaming | TTSStreaming,  // Simultaneous listen/speak
}
```

---

## Core Interfaces

### IVoiceProvider

```csharp
public interface IVoiceProvider : IDisposable
{
    string Id { get; }
    int Priority { get; }
    Platform SupportedPlatforms { get; }
    VoiceCap Capabilities { get; }
    bool IsAvailable { get; }

    // Lifecycle
    void Initialize();
    void Shutdown();

    // STT
    Task<TranscriptionResult> TranscribeAsync(AudioClip audio);
    IAsyncEnumerable<TranscriptionChunk> TranscribeStreamAsync(IAudioStream stream);

    // TTS
    Task<AudioClip> SynthesizeAsync(string text, VoiceSettings settings = default);
    IAsyncEnumerable<AudioChunk> SynthesizeStreamAsync(string text, VoiceSettings settings = default);

    // LLM
    Task<string> CompleteAsync(string prompt, LLMSettings settings = default);
    IAsyncEnumerable<string> CompleteStreamAsync(string prompt, LLMSettings settings = default);
    Task<string> ChatAsync(List<ChatMessage> messages, LLMSettings settings = default);

    // Events
    event Action<VoiceCap> OnCapabilitiesChanged;
    event Action<string> OnError;
}
```

### IVoiceConsumer

```csharp
public interface IVoiceConsumer
{
    VoiceCap RequiredCapabilities { get; }
    VoiceCap OptionalCapabilities { get; }

    void OnBind(IVoiceProvider provider);
    void OnUnbind();

    // Callbacks
    void OnTranscription(TranscriptionResult result);
    void OnSynthesisReady(AudioClip clip);
    void OnLLMResponse(string response);
}
```

---

## Voice Data Structures

```csharp
public struct TranscriptionResult
{
    public string Text;
    public string Language;
    public float Confidence;
    public double Timestamp;
    public TranscriptionSegment[] Segments;  // Word-level timing
}

public struct TranscriptionChunk
{
    public string Text;
    public bool IsFinal;
    public float Confidence;
}

public struct AudioChunk
{
    public float[] Samples;
    public int SampleRate;
    public bool IsFinal;
}

public struct VoiceSettings
{
    public string VoiceId;
    public float Speed;       // 0.5 - 2.0
    public float Pitch;       // 0.5 - 2.0
    public string Emotion;    // "neutral", "happy", "sad"
    public string Language;   // "en-US", "ja-JP"
}

public struct LLMSettings
{
    public float Temperature;
    public int MaxTokens;
    public string SystemPrompt;
    public List<FunctionDefinition> Functions;  // For function calling
}

public struct ChatMessage
{
    public string Role;  // "system", "user", "assistant"
    public string Content;
}
```

---

## Example Providers

### Whisper Provider (Local/Cloud)

```csharp
[VoiceProvider(
    Priority = 80,
    Platforms = Platform.All,
    Capabilities = VoiceCap.STT | VoiceCap.STTStreaming | VoiceCap.STTMultilingual
)]
public class WhisperProvider : IVoiceProvider
{
    public string Id => "OpenAI.Whisper";
    public VoiceCap Capabilities => _caps;

    private VoiceCap _caps;
    private bool _useLocal;

    public void Initialize()
    {
        // Check if local Whisper model is available
        _useLocal = File.Exists(Application.streamingAssetsPath + "/whisper-base.onnx");

        _caps = VoiceCap.STT | VoiceCap.STTMultilingual;
        if (_useLocal)
            _caps |= VoiceCap.STTLocalInference;
        else
            _caps |= VoiceCap.STTStreaming;  // Cloud supports streaming
    }

    public async Task<TranscriptionResult> TranscribeAsync(AudioClip audio)
    {
        if (_useLocal)
            return await TranscribeLocal(audio);
        else
            return await TranscribeCloud(audio);
    }

    private async Task<TranscriptionResult> TranscribeLocal(AudioClip audio)
    {
        // Use ONNX Runtime for local inference
        var samples = new float[audio.samples];
        audio.GetData(samples, 0);

        var tensor = new DenseTensor<float>(samples, new[] { 1, samples.Length });
        var inputs = new List<NamedOnnxValue> {
            NamedOnnxValue.CreateFromTensor("audio", tensor)
        };

        using var results = _session.Run(inputs);
        var text = results.First().AsEnumerable<string>().First();

        return new TranscriptionResult { Text = text, Confidence = 0.95f };
    }

    private async Task<TranscriptionResult> TranscribeCloud(AudioClip audio)
    {
        // Call OpenAI API
        var wavBytes = AudioClipToWav(audio);
        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions",
            new MultipartFormDataContent {
                { new ByteArrayContent(wavBytes), "file", "audio.wav" },
                { new StringContent("whisper-1"), "model" }
            }
        );
        var json = await response.Content.ReadAsStringAsync();
        return JsonUtility.FromJson<TranscriptionResult>(json);
    }
}
```

### Web Speech Provider (Browser)

```csharp
[VoiceProvider(
    Priority = 60,
    Platforms = Platform.WebGL,
    Capabilities = VoiceCap.STT | VoiceCap.STTStreaming | VoiceCap.TTS
)]
public class WebSpeechProvider : IVoiceProvider
{
    public string Id => "Web.SpeechAPI";

    [DllImport("__Internal")]
    private static extern void WebSpeech_StartListening(string lang, Action<string> callback);

    [DllImport("__Internal")]
    private static extern void WebSpeech_StopListening();

    [DllImport("__Internal")]
    private static extern void WebSpeech_Speak(string text, string lang, float rate);

    public async IAsyncEnumerable<TranscriptionChunk> TranscribeStreamAsync(IAudioStream stream)
    {
        var tcs = new TaskCompletionSource<string>();
        WebSpeech_StartListening("en-US", text => tcs.SetResult(text));

        while (!stream.IsComplete)
        {
            if (tcs.Task.IsCompleted)
            {
                yield return new TranscriptionChunk {
                    Text = tcs.Task.Result,
                    IsFinal = false
                };
                tcs = new TaskCompletionSource<string>();
            }
            await Task.Yield();
        }

        WebSpeech_StopListening();
    }

    public Task<AudioClip> SynthesizeAsync(string text, VoiceSettings settings)
    {
        WebSpeech_Speak(text, settings.Language ?? "en-US", settings.Speed);
        return Task.FromResult<AudioClip>(null);  // Browser handles playback
    }
}
```

### ElevenLabs TTS Provider

```csharp
[VoiceProvider(
    Priority = 90,
    Platforms = Platform.All,
    Capabilities = VoiceCap.TTS | VoiceCap.TTSStreaming | VoiceCap.TTSVoiceCloning | VoiceCap.TTSEmotional
)]
public class ElevenLabsProvider : IVoiceProvider
{
    public string Id => "ElevenLabs.TTS";

    private string _apiKey;
    private string _defaultVoiceId = "21m00Tcm4TlvDq8ikWAM";  // Rachel

    public async IAsyncEnumerable<AudioChunk> SynthesizeStreamAsync(
        string text, VoiceSettings settings)
    {
        var voiceId = settings.VoiceId ?? _defaultVoiceId;
        var url = $"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}/stream";

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("xi-api-key", _apiKey);
        request.Content = JsonContent.Create(new {
            text = text,
            model_id = "eleven_turbo_v2_5",
            voice_settings = new {
                stability = 0.5f,
                similarity_boost = 0.75f
            }
        });

        var response = await _httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
        {
            yield return new AudioChunk {
                Samples = ConvertMp3ToSamples(buffer, bytesRead),
                SampleRate = 44100,
                IsFinal = false
            };
        }

        yield return new AudioChunk { IsFinal = true };
    }
}
```

### GPT-4o Voice Provider (Full Agent)

```csharp
[VoiceProvider(
    Priority = 100,
    Platforms = Platform.All,
    Capabilities = VoiceCap.VoiceAgent | VoiceCap.LLMFunctionCalling
)]
public class GPT4oVoiceProvider : IVoiceProvider
{
    public string Id => "OpenAI.GPT4o";

    private List<ChatMessage> _conversationHistory = new();

    public async Task<string> ChatAsync(List<ChatMessage> messages, LLMSettings settings)
    {
        var request = new {
            model = "gpt-4o",
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = settings.Temperature,
            max_tokens = settings.MaxTokens,
            tools = settings.Functions?.Select(f => new {
                type = "function",
                function = new { name = f.Name, description = f.Description, parameters = f.Parameters }
            })
        };

        var response = await _httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions",
            request);

        var json = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();
        return json.Choices[0].Message.Content;
    }

    // Convenience method for voice agents
    public async Task<(string text, AudioClip speech)> ProcessVoiceAsync(
        AudioClip userSpeech,
        IVoiceProvider sttProvider,
        IVoiceProvider ttsProvider)
    {
        // STT
        var transcription = await sttProvider.TranscribeAsync(userSpeech);

        // Add to conversation
        _conversationHistory.Add(new ChatMessage {
            Role = "user",
            Content = transcription.Text
        });

        // LLM
        var response = await ChatAsync(_conversationHistory, new LLMSettings {
            Temperature = 0.7f,
            MaxTokens = 256
        });

        _conversationHistory.Add(new ChatMessage {
            Role = "assistant",
            Content = response
        });

        // TTS
        var speech = await ttsProvider.SynthesizeAsync(response, default);

        return (response, speech);
    }
}
```

---

## Example Consumers

### VFX Voice Consumer (Voice-reactive effects)

```csharp
[VoiceConsumer(
    Required = VoiceCap.STT,
    Optional = VoiceCap.STTStreaming
)]
public class VFXVoiceConsumer : MonoBehaviour, IVoiceConsumer
{
    [SerializeField] private VisualEffect _vfx;
    [SerializeField] private AudioSource _audioSource;

    public VoiceCap RequiredCapabilities => VoiceCap.STT;
    public VoiceCap OptionalCapabilities => VoiceCap.STTStreaming;

    private IVoiceProvider _provider;

    public void OnBind(IVoiceProvider provider)
    {
        _provider = provider;

        // Start listening if streaming available
        if (provider.Capabilities.HasFlag(VoiceCap.STTStreaming))
        {
            StartCoroutine(StreamTranscription());
        }
    }

    private IEnumerator StreamTranscription()
    {
        var stream = new MicrophoneAudioStream();

        var task = _provider.TranscribeStreamAsync(stream).GetAsyncEnumerator();

        while (true)
        {
            var moveNext = task.MoveNextAsync();
            yield return new WaitUntil(() => moveNext.IsCompleted);

            if (!moveNext.Result) break;

            var chunk = task.Current;
            OnTranscriptionChunk(chunk);
        }
    }

    private void OnTranscriptionChunk(TranscriptionChunk chunk)
    {
        // Trigger VFX based on speech
        if (!string.IsNullOrEmpty(chunk.Text))
        {
            _vfx.SetFloat("SpeechIntensity", 1f);
            _vfx.SendEvent("OnSpeech");

            // Word-based effects
            var words = chunk.Text.Split(' ');
            _vfx.SetInt("WordCount", words.Length);
        }
    }

    public void OnTranscription(TranscriptionResult result)
    {
        // Process final transcription
        Debug.Log($"User said: {result.Text}");

        // Trigger VFX based on keywords
        if (result.Text.Contains("fire"))
            _vfx.SendEvent("FireEffect");
        else if (result.Text.Contains("water"))
            _vfx.SendEvent("WaterEffect");
    }
}
```

### Command Consumer (Voice commands)

```csharp
[VoiceConsumer(
    Required = VoiceCap.STT,
    Optional = VoiceCap.LLMFunctionCalling
)]
public class VoiceCommandConsumer : MonoBehaviour, IVoiceConsumer
{
    public VoiceCap RequiredCapabilities => VoiceCap.STT;
    public VoiceCap OptionalCapabilities => VoiceCap.LLMFunctionCalling;

    // Command registry
    private readonly Dictionary<string, Action<string[]>> _commands = new();

    void Awake()
    {
        // Register voice commands
        RegisterCommand("next effect", _ => VFXService.NextEffect());
        RegisterCommand("previous effect", _ => VFXService.PreviousEffect());
        RegisterCommand("change color to", args => VFXService.SetColor(args[0]));
        RegisterCommand("set intensity", args => VFXService.SetIntensity(float.Parse(args[0])));
        RegisterCommand("take screenshot", _ => ScreenCapture.CaptureScreenshot("screenshot.png"));
    }

    public void RegisterCommand(string trigger, Action<string[]> action)
    {
        _commands[trigger.ToLower()] = action;
    }

    public void OnTranscription(TranscriptionResult result)
    {
        var text = result.Text.ToLower();

        // Simple keyword matching
        foreach (var (trigger, action) in _commands)
        {
            if (text.Contains(trigger))
            {
                var args = ExtractArgs(text, trigger);
                action(args);
                return;
            }
        }

        // Fallback to LLM for complex commands
        if (_provider?.Capabilities.HasFlag(VoiceCap.LLMFunctionCalling) == true)
        {
            ProcessWithLLM(text);
        }
    }

    private async void ProcessWithLLM(string text)
    {
        var response = await _provider.ChatAsync(new List<ChatMessage> {
            new() { Role = "system", Content = GetSystemPrompt() },
            new() { Role = "user", Content = text }
        }, new LLMSettings {
            Functions = GetFunctionDefinitions()
        });

        // Parse and execute function call from response
    }
}
```

### Agent Consumer (Conversational AI)

```csharp
[VoiceConsumer(
    Required = VoiceCap.VoiceAgent,
    Optional = VoiceCap.FullDuplex
)]
public class ConversationalAgentConsumer : MonoBehaviour, IVoiceConsumer
{
    [SerializeField] private AudioSource _speakerSource;
    [SerializeField] private string _systemPrompt = "You are a helpful AR assistant.";

    public VoiceCap RequiredCapabilities => VoiceCap.VoiceAgent;
    public VoiceCap OptionalCapabilities => VoiceCap.FullDuplex;

    private IVoiceProvider _sttProvider;
    private IVoiceProvider _ttsProvider;
    private IVoiceProvider _llmProvider;
    private List<ChatMessage> _history = new();
    private bool _isListening;

    public void OnBind(IVoiceProvider provider)
    {
        // May receive multiple providers for different capabilities
        if (provider.Capabilities.HasFlag(VoiceCap.STT))
            _sttProvider = provider;
        if (provider.Capabilities.HasFlag(VoiceCap.TTS))
            _ttsProvider = provider;
        if (provider.Capabilities.HasFlag(VoiceCap.LLM))
            _llmProvider = provider;
    }

    public async void StartConversation()
    {
        _history.Clear();
        _history.Add(new ChatMessage { Role = "system", Content = _systemPrompt });
        _isListening = true;

        while (_isListening)
        {
            // Listen
            var audio = await RecordUserSpeech();
            if (audio == null) continue;

            // Transcribe
            var transcription = await _sttProvider.TranscribeAsync(audio);
            if (string.IsNullOrEmpty(transcription.Text)) continue;

            _history.Add(new ChatMessage { Role = "user", Content = transcription.Text });

            // Think
            var response = await _llmProvider.ChatAsync(_history, new LLMSettings {
                Temperature = 0.7f,
                MaxTokens = 256
            });

            _history.Add(new ChatMessage { Role = "assistant", Content = response });

            // Speak
            if (_ttsProvider.Capabilities.HasFlag(VoiceCap.TTSStreaming))
            {
                // Stream audio for lower latency
                await foreach (var chunk in _ttsProvider.SynthesizeStreamAsync(response, default))
                {
                    PlayAudioChunk(chunk);
                }
            }
            else
            {
                var clip = await _ttsProvider.SynthesizeAsync(response, default);
                _speakerSource.PlayOneShot(clip);
            }
        }
    }

    public void StopConversation() => _isListening = false;
}
```

---

## VoiceService (Orchestrator)

```csharp
public class VoiceService : MonoBehaviour
{
    public static VoiceService Instance { get; private set; }

    private readonly List<IVoiceProvider> _providers = new();
    private readonly List<IVoiceConsumer> _consumers = new();
    private readonly Dictionary<VoiceCap, IVoiceProvider> _capabilityMap = new();

    void Awake()
    {
        Instance = this;
        DiscoverProviders();
        DiscoverConsumers();
        BindAll();
    }

    void DiscoverProviders()
    {
        // Same pattern as TrackingService
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.GetCustomAttribute<VoiceProviderAttribute>() != null);

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<VoiceProviderAttribute>();
            if (!attr.Platforms.HasFlag(CurrentPlatform)) continue;

            var provider = (IVoiceProvider)Activator.CreateInstance(type);
            if (provider.IsAvailable)
                RegisterProvider(provider);
        }
    }

    // === PUBLIC API ===

    public async Task<TranscriptionResult> TranscribeAsync(AudioClip audio)
    {
        var provider = GetProviderFor(VoiceCap.STT);
        return provider != null
            ? await provider.TranscribeAsync(audio)
            : default;
    }

    public async Task<AudioClip> SpeakAsync(string text, VoiceSettings settings = default)
    {
        var provider = GetProviderFor(VoiceCap.TTS);
        return provider != null
            ? await provider.SynthesizeAsync(text, settings)
            : null;
    }

    public async Task<string> AskAsync(string prompt, LLMSettings settings = default)
    {
        var provider = GetProviderFor(VoiceCap.LLM);
        return provider != null
            ? await provider.CompleteAsync(prompt, settings)
            : null;
    }

    public IVoiceProvider GetProviderFor(VoiceCap capability)
    {
        _capabilityMap.TryGetValue(capability, out var provider);
        return provider;
    }
}
```

---

## Usage Example

```csharp
// Just add components - auto-binding handles the rest

// 1. Voice-controlled VFX
public class VoiceVFX : MonoBehaviour
{
    void Start()
    {
        gameObject.AddComponent<VFXVoiceConsumer>();
        // Automatically binds to best available STT provider
    }
}

// 2. Direct API usage
async void ProcessVoice()
{
    // Record from microphone
    var clip = Microphone.Start(null, false, 5, 44100);
    await Task.Delay(5000);
    Microphone.End(null);

    // Transcribe (uses best available STT)
    var result = await VoiceService.Instance.TranscribeAsync(clip);
    Debug.Log($"You said: {result.Text}");

    // Ask LLM
    var response = await VoiceService.Instance.AskAsync(
        $"Respond briefly to: {result.Text}"
    );

    // Speak response (uses best available TTS)
    var speech = await VoiceService.Instance.SpeakAsync(response);
    GetComponent<AudioSource>().PlayOneShot(speech);
}

// 3. Check capabilities
void CheckVoice()
{
    var caps = VoiceService.Instance.AvailableCapabilities;

    if (caps.HasFlag(VoiceCap.STTLocalInference))
        Debug.Log("Local Whisper available - no cloud needed!");

    if (caps.HasFlag(VoiceCap.FullDuplex))
        Debug.Log("Can listen and speak simultaneously");
}
```

---

## Combined: Tracking + Voice

```csharp
// XR Assistant that responds to gestures AND voice
[TrackingConsumer(Required = TrackingCap.HandTracking21)]
[VoiceConsumer(Required = VoiceCap.VoiceAgent)]
public class XRAssistant : MonoBehaviour, ITrackingConsumer, IVoiceConsumer
{
    private ITrackingProvider _trackingProvider;
    private IVoiceProvider _voiceProvider;

    public void OnTrackingData<T>(T data) where T : struct, ITrackingData
    {
        if (data is HandData hand && hand.IsPinching)
        {
            // Pinch triggers voice listening
            StartListening();
        }
    }

    private async void StartListening()
    {
        var clip = await RecordForSeconds(3);
        var transcription = await _voiceProvider.TranscribeAsync(clip);

        // Process voice command
        await ProcessCommand(transcription.Text);
    }
}
```

---

## File Structure (Combined)

```
Assets/Scripts/
├── Tracking/           # (as before)
├── Voice/
│   ├── Core/
│   │   ├── VoiceService.cs
│   │   ├── IVoiceProvider.cs
│   │   ├── IVoiceConsumer.cs
│   │   └── VoiceData.cs
│   ├── Providers/
│   │   ├── WhisperProvider.cs
│   │   ├── WebSpeechProvider.cs
│   │   ├── ElevenLabsProvider.cs
│   │   ├── GPT4oVoiceProvider.cs
│   │   └── LocalLLMProvider.cs
│   └── Consumers/
│       ├── VFXVoiceConsumer.cs
│       ├── VoiceCommandConsumer.cs
│       └── ConversationalAgentConsumer.cs
└── XR/
    └── XRService.cs    # Unified access to Tracking + Voice
```

---

# Part 3: LLMR & Meta Prompting Insights

## LLMR Architecture (MIT Media Lab)

LLMR (Large Language Model for Mixed Reality) provides key insights for XR+AI system design.

### Multi-GPT Orchestration Pattern

```
┌─────────────────────────────────────────────────────────────────┐
│                     LLMR ARCHITECTURE                           │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   User Prompt ───────────────────────────────────────────┐     │
│                                                           │     │
│   ┌─────────────┐    ┌─────────────┐    ┌─────────────┐ │     │
│   │ Scene       │    │ Skill       │    │ Inspector   │ │     │
│   │ Analyzer    │    │ Library     │    │ GPT         │ │     │
│   │ GPT         │    │ GPT         │    │ (Validator) │ │     │
│   └──────┬──────┘    └──────┬──────┘    └──────┬──────┘ │     │
│          │                  │                   │        │     │
│          └──────────────────┼───────────────────┘        │     │
│                             ▼                            │     │
│                    ┌─────────────┐                       │     │
│                    │  Builder    │◄──────────────────────┘     │
│                    │  GPT        │                             │
│                    │  (Coder)    │                             │
│                    └──────┬──────┘                             │
│                           │                                    │
│                           ▼                                    │
│                    ┌─────────────┐                             │
│                    │   Unity     │                             │
│                    │  Compiler   │                             │
│                    └──────┬──────┘                             │
│                           │                                    │
│                           ▼                                    │
│                    ┌─────────────┐                             │
│                    │  XR Scene   │                             │
│                    └─────────────┘                             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Key LLMR Components

| Component | Role | Our Equivalent |
|-----------|------|----------------|
| SceneAnalyzerGPT | Scene understanding, object inventory | TrackingService context |
| SkillLibraryGPT | Determines needed skills/tools | CapabilityRouter |
| BuilderGPT | Code generation for Unity | (optional LLM consumer) |
| InspectorGPT | Validates code against rules | Self-debugging pattern |

### Applying LLMR Patterns to Our Architecture

```csharp
// IContextProvider - like LLMR's SceneAnalyzerGPT
public interface IContextProvider
{
    string GetContextSummary();
    Dictionary<string, object> GetContextDetails();
}

[ContextProvider]
public class TrackingContextProvider : IContextProvider
{
    public string GetContextSummary()
    {
        var sb = new StringBuilder();

        // Tracking context
        var tracking = TrackingService.Instance;
        sb.AppendLine($"Tracking Capabilities: {tracking.AvailableCapabilities}");

        if (tracking.TryGetData<BodyData>(out var body))
            sb.AppendLine($"Body: {body.JointCount} joints tracked");

        if (tracking.TryGetData<HandData>(out var hand))
            sb.AppendLine($"Hand: {(hand.IsPinching ? "Pinching" : "Open")}");

        // Voice context
        var voice = VoiceService.Instance;
        sb.AppendLine($"Voice Capabilities: {voice.AvailableCapabilities}");

        return sb.ToString();
    }
}

// ISkillRouter - like LLMR's SkillLibraryGPT
public interface ISkillRouter
{
    IEnumerable<ISkill> GetSkillsForIntent(string intent);
    ISkill SelectBestSkill(string intent, TrackingCap available, VoiceCap voiceAvailable);
}

// ISelfValidator - like LLMR's InspectorGPT
public interface ISelfValidator
{
    ValidationResult Validate(object output);
    object FixIfPossible(object output, ValidationResult errors);
}
```

---

## Meta Prompting Architecture

Meta prompting lets LLMs generate and optimize their own prompts.

### Prompt Chaining for XR

```
┌──────────────────────────────────────────────────────────────────┐
│                 PROMPT CHAIN ARCHITECTURE                        │
├──────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐     │
│   │ Context │───▶│ Intent  │───▶│ Execute │───▶│ Validate│     │
│   │ Gather  │    │ Classify│    │ Action  │    │ Result  │     │
│   └─────────┘    └─────────┘    └─────────┘    └─────────┘     │
│        │              │              │              │            │
│        ▼              ▼              ▼              ▼            │
│   "What do I     "User wants    "Call VFX     "VFX now        │
│    see/hear?"     to change      SetColor()"    shows red"     │
│                   VFX color"                                    │
│                                                                  │
│   ◄──────────────── FEEDBACK LOOP ────────────────────────────▶ │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘
```

### Self-Refinement Pattern

```csharp
public class SelfRefiningAgent
{
    private readonly ILLMProvider _llm;
    private readonly int _maxIterations = 3;

    public async Task<string> ProcessWithRefinement(string input, string context)
    {
        string currentOutput = null;
        string feedback = null;

        for (int i = 0; i < _maxIterations; i++)
        {
            // Generate (or refine) response
            var prompt = feedback == null
                ? $"Context: {context}\nUser: {input}"
                : $"Context: {context}\nUser: {input}\nPrevious: {currentOutput}\nFeedback: {feedback}\nImprove:";

            currentOutput = await _llm.GenerateAsync(prompt);

            // Self-critique
            var critiquePrompt = $"Evaluate this response. Is it correct and complete? If not, explain issues.\nResponse: {currentOutput}";
            feedback = await _llm.GenerateAsync(critiquePrompt);

            // Check if satisfied
            if (feedback.Contains("correct") && feedback.Contains("complete"))
                break;
        }

        return currentOutput;
    }
}
```

---

## Testing Without Hardware Dependencies

### Mock Provider Pattern

```csharp
// MockTrackingProvider - Zero hardware dependency
[TrackingProvider(Priority = -100)] // Lowest priority, only used when nothing else available
public class MockTrackingProvider : ITrackingProvider
{
    public Platform SupportedPlatforms => Platform.Editor | Platform.All;
    public TrackingCap Capabilities => TrackingCap.All; // Simulates everything
    public bool IsAvailable => Application.isEditor || Debug.isDebugBuild;

    private MockDataGenerator _generator = new();

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        if (typeof(T) == typeof(BodyData))
        {
            data = (T)(object)_generator.GenerateBodyData();
            return true;
        }
        if (typeof(T) == typeof(HandData))
        {
            data = (T)(object)_generator.GenerateHandData();
            return true;
        }
        data = default;
        return false;
    }
}

// MockDataGenerator - Produces realistic test data
public class MockDataGenerator
{
    private float _time;

    public BodyData GenerateBodyData()
    {
        _time += Time.deltaTime;

        // Simulate breathing/idle motion
        var offset = Mathf.Sin(_time * 2f) * 0.02f;

        return new BodyData
        {
            JointCount = 17,
            Joints = GenerateIdleJoints(offset),
            Confidence = 0.95f
        };
    }

    public HandData GenerateHandData()
    {
        // Simulate hand with occasional pinch
        var isPinching = Mathf.Sin(_time * 0.5f) > 0.8f;

        return new HandData
        {
            IsTracked = true,
            IsPinching = isPinching,
            PinchStrength = isPinching ? 1f : 0f,
            Joints = GenerateHandJoints()
        };
    }
}
```

### Simulation Test Framework

```csharp
// TrackingSimulator - Record and replay tracking data
public class TrackingSimulator : MonoBehaviour
{
    [SerializeField] private TextAsset _recordedSession;
    [SerializeField] private bool _playOnStart = true;

    private TrackingRecording _recording;
    private float _playbackTime;
    private bool _isPlaying;

    void Start()
    {
        if (_recordedSession != null && _playOnStart)
        {
            _recording = JsonUtility.FromJson<TrackingRecording>(_recordedSession.text);
            _isPlaying = true;
        }
    }

    void Update()
    {
        if (!_isPlaying) return;

        _playbackTime += Time.deltaTime;
        var frame = _recording.GetFrameAtTime(_playbackTime);

        // Inject into mock provider
        MockTrackingProvider.InjectFrame(frame);

        if (_playbackTime >= _recording.Duration)
            _isPlaying = false;
    }

    // Record current session for later playback
    public static void StartRecording() => _isRecording = true;
    public static void StopRecording() => SaveRecording();
}

// Unit test without any hardware
[Test]
public void TestPinchDetection()
{
    // Load recorded pinch gesture
    var recording = LoadRecording("pinch_gesture.json");
    var simulator = new TrackingSimulator(recording);
    var consumer = new PinchDetector();

    int pinchCount = 0;
    consumer.OnPinch += () => pinchCount++;

    // Play through recording
    simulator.PlayAll(consumer);

    Assert.AreEqual(3, pinchCount); // Expected 3 pinches in recording
}
```

### Editor-Only Debug Visualization

```csharp
#if UNITY_EDITOR
[ExecuteAlways]
public class TrackingDebugVisualizer : MonoBehaviour
{
    [SerializeField] private bool _showSkeleton = true;
    [SerializeField] private bool _showHandJoints = true;
    [SerializeField] private bool _showConfidence = true;

    private void OnDrawGizmos()
    {
        if (!_showSkeleton) return;

        if (TrackingService.Instance?.TryGetData<BodyData>(out var body) == true)
        {
            DrawSkeleton(body);
        }

        if (_showHandJoints && TrackingService.Instance?.TryGetData<HandData>(out var hand) == true)
        {
            DrawHand(hand);
        }
    }

    private void DrawSkeleton(BodyData body)
    {
        Gizmos.color = Color.green;
        foreach (var joint in body.Joints)
        {
            // Size based on confidence
            float size = _showConfidence ? joint.Confidence * 0.05f : 0.03f;
            Gizmos.DrawSphere(joint.Position, size);
        }

        // Draw bones
        Gizmos.color = Color.cyan;
        foreach (var bone in SkeletonDefinition.Bones)
        {
            var start = body.Joints[bone.StartJoint].Position;
            var end = body.Joints[bone.EndJoint].Position;
            Gizmos.DrawLine(start, end);
        }
    }
}
#endif
```

---

## Platform-Agnostic Design Principles

### 1. Interface Segregation

```csharp
// Minimal interfaces that work everywhere
public interface ITrackingData { }
public interface IVoiceData { }

// Platform-specific extensions via composition, not inheritance
public interface ITrackingProviderExtensions
{
    // iOS-specific
    bool SupportsLiDAR { get; }

    // Quest-specific
    bool SupportsPassthrough { get; }

    // All - but optional
    bool TryGetExtension<T>(out T extension);
}
```

### 2. Capability-Based Feature Detection

```csharp
// Never assume capabilities - always check
public class FeatureGatedVFX : MonoBehaviour
{
    [SerializeField] private TrackingCap _requiredCaps = TrackingCap.BodySegmentation24Part;
    [SerializeField] private TrackingCap _optionalCaps = TrackingCap.HandTracking21;
    [SerializeField] private GameObject _fallbackVFX;

    void Start()
    {
        var available = TrackingService.Instance.AvailableCapabilities;

        if (!available.HasFlag(_requiredCaps))
        {
            // Fallback or disable
            gameObject.SetActive(false);
            _fallbackVFX?.SetActive(true);
            return;
        }

        // Optional enhancements
        if (available.HasFlag(_optionalCaps))
        {
            EnableHandInteraction();
        }
    }
}
```

### 3. Zero External Dependencies Core

```csharp
// Core tracking types - zero dependencies
namespace Tracking.Core
{
    // Pure C# structs - no Unity, no external libs
    public struct Vector3f
    {
        public float X, Y, Z;

        // Implicit conversion to/from Unity
        public static implicit operator UnityEngine.Vector3(Vector3f v)
            => new(v.X, v.Y, v.Z);
        public static implicit operator Vector3f(UnityEngine.Vector3 v)
            => new() { X = v.x, Y = v.y, Z = v.z };
    }

    public struct JointData
    {
        public int Id;
        public Vector3f Position;
        public Vector3f Rotation;
        public float Confidence;
    }

    // Can be used in:
    // - Unity (via implicit conversion)
    // - Native C++ (via P/Invoke)
    // - WebGL (via jslib)
    // - Server-side (pure .NET)
}
```

### 4. Abstraction Boundaries

```
┌─────────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                            │
│   VFX, Avatars, Games, UI (uses interfaces only)               │
├─────────────────────────────────────────────────────────────────┤
│                    SERVICE LAYER                                │
│   TrackingService, VoiceService (orchestration)                │
├─────────────────────────────────────────────────────────────────┤
│                    ABSTRACTION LAYER                            │
│   ITrackingProvider, IVoiceProvider (contracts)                │
├─────────────────────────────────────────────────────────────────┤
│                    ADAPTER LAYER                                │
│   Platform-specific implementations                             │
│   (ARKit, Meta, Sentis, WebXR, etc.)                           │
├─────────────────────────────────────────────────────────────────┤
│                    PLATFORM LAYER                               │
│   Native SDKs, Hardware, OS APIs                               │
└─────────────────────────────────────────────────────────────────┘

Dependencies flow DOWN only.
Application layer has ZERO knowledge of platform specifics.
```

---

## Debugging & Extension Patterns

### 1. Hot-Swappable Providers (Runtime)

```csharp
// Switch providers at runtime for debugging
public class ProviderSwitcher : MonoBehaviour
{
    void Update()
    {
        // Debug key to switch to mock
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TrackingService.Instance.ForceProvider<MockTrackingProvider>();
            Debug.Log("Switched to Mock provider");
        }

        // Debug key to switch to recording playback
        if (Input.GetKeyDown(KeyCode.F2))
        {
            TrackingService.Instance.ForceProvider<RecordingPlaybackProvider>();
            Debug.Log("Playing back recorded session");
        }

        // Return to auto-select
        if (Input.GetKeyDown(KeyCode.F3))
        {
            TrackingService.Instance.AutoSelectProvider();
            Debug.Log($"Auto-selected: {TrackingService.Instance.ActiveProvider.Id}");
        }
    }
}
```

### 2. Provider Extension via Composition

```csharp
// Add new capability to existing provider without modifying it
public class EnhancedTrackingProvider : ITrackingProvider
{
    private readonly ITrackingProvider _base;
    private readonly IGestureRecognizer _gestures;

    public EnhancedTrackingProvider(ITrackingProvider baseProvider)
    {
        _base = baseProvider;
        _gestures = new GestureRecognizer();
    }

    public TrackingCap Capabilities => _base.Capabilities | TrackingCap.GestureRecognition;

    public bool TryGetData<T>(out T data) where T : struct, ITrackingData
    {
        if (typeof(T) == typeof(GestureData))
        {
            // Process base hand data through gesture recognizer
            if (_base.TryGetData<HandData>(out var hand))
            {
                data = (T)(object)_gestures.Process(hand);
                return true;
            }
        }

        // Delegate to base for everything else
        return _base.TryGetData(out data);
    }
}
```

### 3. Plugin-Based Provider Loading

```csharp
// Load providers from separate assemblies
public class PluginProviderLoader
{
    public static IEnumerable<ITrackingProvider> LoadFromAssemblies(string pluginPath)
    {
        var providers = new List<ITrackingProvider>();

        foreach (var dll in Directory.GetFiles(pluginPath, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                var providerTypes = assembly.GetTypes()
                    .Where(t => typeof(ITrackingProvider).IsAssignableFrom(t))
                    .Where(t => !t.IsInterface && !t.IsAbstract);

                foreach (var type in providerTypes)
                {
                    var provider = (ITrackingProvider)Activator.CreateInstance(type);
                    if (provider.IsAvailable)
                    {
                        providers.Add(provider);
                        Debug.Log($"Loaded provider: {provider.Id} from {dll}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load plugin {dll}: {e.Message}");
            }
        }

        return providers;
    }
}
```

---

## XR Blocks Pattern (Google Research)

XR Blocks enables prototyping without hardware via web simulation.

### Key Takeaways for Our Architecture

1. **Simulation-First Development**: Build and test in desktop browser, deploy to XR
2. **Same Code, Different Contexts**: Physics-based interactions work in sim and real
3. **Web Reproducibility**: All demos should work independently on web

```csharp
// Simulation-aware provider selection
public static class SimulationMode
{
    public static bool IsSimulation =>
        Application.platform == RuntimePlatform.WebGLPlayer ||
        (Application.isEditor && !XRSettings.isDeviceActive);

    public static void ConfigureForSimulation()
    {
        if (IsSimulation)
        {
            // Use mouse for hand simulation
            TrackingService.Instance.ForceProvider<MouseHandSimulator>();

            // Use keyboard for body pose presets
            TrackingService.Instance.AddProvider(new KeyboardPoseProvider());

            // Use webcam for face (if available)
            if (WebCamTexture.devices.Length > 0)
            {
                TrackingService.Instance.AddProvider(new WebcamFaceProvider());
            }
        }
    }
}
```

---

## Summary: Design Principles

| Principle | Implementation |
|-----------|----------------|
| **Zero external deps in core** | Pure C# structs, implicit Unity conversion |
| **Capability-based routing** | Flags enum, runtime detection |
| **Hot-swappable providers** | Interface-based, runtime switching |
| **Mock-first testing** | MockProvider, recorded sessions |
| **Self-documenting context** | IContextProvider for LLM integration |
| **Self-validating output** | ISelfValidator pattern |
| **Prompt chaining ready** | Modular services, clear data flow |
| **Simulation-first dev** | Desktop/web simulation, same code |

---

## References

- [LLMR: MIT Media Lab](https://www.media.mit.edu/projects/large-language-model-for-mixed-reality/overview/)
- [LLMR Paper (CHI 2024)](https://dl.acm.org/doi/10.1145/3613904.3642579)
- [LLMR GitHub (Microsoft)](https://github.com/microsoft/llmr)
- [LLM Integration in XR (CHI 2025)](https://dl.acm.org/doi/10.1145/3706598.3714224)
- [XR Blocks (Google Research)](https://research.google/blog/xr-blocks-accelerating-ai-xr-innovation/)
- [Meta Prompting Guide](https://www.promptingguide.ai/techniques/meta-prompting)
- [Prompt Chaining Guide](https://www.promptingguide.ai/techniques/prompt_chaining)
- [iv4XR: AI Test Agents for XR](https://www.openaccessgovernment.org/article/artificial-intelligence-test-agents-for-automated-testing-of-extended-reality-xr-ai/149203/)
- [SpatialLM](https://manycore-research.github.io/SpatialLM/)

---

*Updated: 2026-01-20*
