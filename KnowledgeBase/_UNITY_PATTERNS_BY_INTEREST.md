# Unity Intelligence Patterns by Interest

**Activation**: Say **"Using Unity Intelligence patterns"**
**Organized by**: JT Priorities (H3M Portals, brushes, hand tracking, audio, LiDAR)

---

## 1. Particle Brush Systems (P0 Priority)

### Open Brush GeometryPool Pattern
From `icosa-foundation/open-brush` - Production-tested brush pooling system.

```csharp
// GeometryPool - Efficient mesh data storage with residency control
public class GeometryPool {
    public const int kNumTexcoords = 3;

    public struct TexcoordData {
        public List<Vector2> v2;
        public List<Vector3> v3;
        public List<Vector4> v4;

        public void SetSize(int size) {
            switch (size) {
                case 2: v2 = v2 ?? new List<Vector2>(); v3 = null; v4 = null; break;
                case 3: v2 = null; v3 = v3 ?? new List<Vector3>(); v4 = null; break;
                case 4: v2 = null; v3 = null; v4 = v4 ?? new List<Vector4>(); break;
            }
        }
    }

    // Memory management
    public void CopyToMesh(Mesh mesh);
    public void MakeGeometryNotResident(Mesh mesh);  // Free CPU memory, keep GPU
    public void EnsureGeometryResident();            // Reload from GPU/file
}
```

### GeniusParticlesBrush Pattern
Geometry-based particles (faster than Unity ParticleSystem).

```csharp
class GeniusParticlesBrush : GeometryBrush {
    private const float kSpawnInterval_PS = 0.0025f * App.METERS_TO_UNITS;
    private const int kVertsInSolid = 4;
    private const int kTrisInSolid = 2;

    private List<float> m_DecayTimers;
    private float m_DistancePointerTravelled;
    private float m_SpawnInterval;

    // Deterministic RNG for consistent particle appearance
    protected int CalculateSalt(int knotIndex, int particleIndex) {
        int pretendKnotIndex = knotIndex + m_DecayedKnots;
        return kSaltMaxSaltsPerParticle * (pretendKnotIndex * kSaltMaxParticlesPerKnot + particleIndex);
    }

    // Preview decay - particles fade out over time
    public override void DecayBrush() {
        int knotsToShift = 0;
        for (int i = 0; i < m_DecayTimers.Count; i++) {
            m_DecayTimers[i] += Time.deltaTime;
            if (m_DecayTimers[i] > kPreviewDuration) knotsToShift++;
        }
        RemoveInitialKnots(knotsToShift);
    }
}
```

### Brush Descriptor Pattern
Scriptable object for brush configuration.

```csharp
[CreateAssetMenu(fileName = "NewBrush", menuName = "Brushes/Brush Descriptor")]
public class BrushDescriptor : ScriptableObject {
    public Guid m_Guid;
    public string m_Description;
    public Material m_Material;
    public float m_ParticleRate = 1f;
    public float m_ParticleSpeed = 1f;
    public Vector2 m_BrushSizeRange = new Vector2(0.01f, 0.1f);
    public bool m_AudioReactive;
    public bool m_SupportsVR;
}
```

---

## 2. Hand Tracking + Gesture Recognition (P1 Priority)

### HandPoseBarracuda Pattern
MediaPipe + Barracuda for gesture recognition.

```csharp
// Hand landmark indices (MediaPipe standard)
public enum HandLandmark {
    Wrist = 0,
    ThumbCMC = 1, ThumbMCP = 2, ThumbIP = 3, ThumbTip = 4,
    IndexMCP = 5, IndexPIP = 6, IndexDIP = 7, IndexTip = 8,
    MiddleMCP = 9, MiddlePIP = 10, MiddleDIP = 11, MiddleTip = 12,
    RingMCP = 13, RingPIP = 14, RingDIP = 15, RingTip = 16,
    PinkyMCP = 17, PinkyPIP = 18, PinkyDIP = 19, PinkyTip = 20
}

public class HandGestureRecognizer : MonoBehaviour {
    [SerializeField] NNModel handModel;
    private IWorker worker;

    void Start() {
        var model = ModelLoader.Load(handModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
    }

    public Vector3[] GetLandmarks(Texture2D input) {
        var inputTensor = new Tensor(input);
        worker.Execute(inputTensor);
        var output = worker.PeekOutput();
        // Parse 21 landmarks x 3 coords
        return ParseLandmarks(output);
    }

    public bool IsPinching(Vector3[] landmarks) {
        float dist = Vector3.Distance(
            landmarks[(int)HandLandmark.ThumbTip],
            landmarks[(int)HandLandmark.IndexTip]
        );
        return dist < 0.03f; // 3cm threshold
    }
}
```

### XR Hands Integration (Unity XRI 3.x)
```csharp
using UnityEngine.XR.Hands;

public class XRHandsPainting : MonoBehaviour {
    [SerializeField] XRHandSubsystem handSubsystem;

    void Update() {
        if (handSubsystem.TryGetJoint(XRHandJointID.IndexTip, out var indexTip) &&
            handSubsystem.TryGetJoint(XRHandJointID.ThumbTip, out var thumbTip)) {

            float pinchDistance = Vector3.Distance(indexTip.position, thumbTip.position);
            if (pinchDistance < 0.02f) {
                Paint(indexTip.position);
            }
        }
    }
}
```

---

## 3. Audio Reactive VFX (P1 Priority)

### WASAPI Audio Capture Pattern
From `smaerdlatigid/VFXcubes-WASAPI`.

```csharp
public class AudioReactiveVFX : MonoBehaviour {
    [SerializeField] VisualEffect vfx;
    [SerializeField] int spectrumSize = 256;

    private float[] spectrum;
    private float[] bands = new float[8]; // 8-band EQ

    void Start() {
        spectrum = new float[spectrumSize];
    }

    void Update() {
        AudioListener.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        CalculateBands();

        vfx.SetFloat("Bass", bands[0]);
        vfx.SetFloat("LowMid", bands[1]);
        vfx.SetFloat("Mid", bands[2]);
        vfx.SetFloat("HighMid", bands[3]);
        vfx.SetFloat("High", bands[4]);
        vfx.SetFloat("Amplitude", GetAmplitude());
    }

    void CalculateBands() {
        int[] sampleCounts = { 2, 4, 8, 16, 32, 64, 128, 256 };
        int sampleIndex = 0;

        for (int i = 0; i < 8; i++) {
            float avg = 0;
            for (int j = 0; j < sampleCounts[i]; j++) {
                avg += spectrum[sampleIndex] * (sampleIndex + 1);
                sampleIndex++;
            }
            bands[i] = avg / sampleCounts[i];
        }
    }

    float GetAmplitude() {
        float sum = 0;
        foreach (var s in spectrum) sum += s;
        return sum / spectrumSize;
    }
}
```

### VFX Graph Audio Binding
```csharp
// VFX Graph exposed properties for audio
public class VFXAudioBinder : MonoBehaviour {
    [SerializeField] VisualEffect vfx;

    // Bind audio to VFX properties
    public void SetAudioData(float bass, float mid, float high, float amplitude) {
        vfx.SetFloat("AudioBass", Mathf.Lerp(vfx.GetFloat("AudioBass"), bass, 0.3f));
        vfx.SetFloat("AudioMid", Mathf.Lerp(vfx.GetFloat("AudioMid"), mid, 0.3f));
        vfx.SetFloat("AudioHigh", Mathf.Lerp(vfx.GetFloat("AudioHigh"), high, 0.3f));
        vfx.SetFloat("AudioAmplitude", amplitude);

        // Trigger burst on beat
        if (bass > 0.8f) vfx.SendEvent("OnBeat");
    }
}
```

---

## 4. LiDAR / Depth Effects (Rcam Style)

### ARFoundation Depth to VFX
```csharp
public class DepthToVFX : MonoBehaviour {
    [SerializeField] AROcclusionManager occlusionManager;
    [SerializeField] VisualEffect vfx;
    [SerializeField] ComputeShader depthProcessor;

    private RenderTexture positionMap;
    private RenderTexture colorMap;

    void Start() {
        positionMap = new RenderTexture(256, 192, 0, RenderTextureFormat.ARGBFloat);
        positionMap.enableRandomWrite = true;
        colorMap = new RenderTexture(256, 192, 0, RenderTextureFormat.ARGB32);
        colorMap.enableRandomWrite = true;
    }

    void Update() {
        var depthTex = occlusionManager.environmentDepthTexture;
        if (depthTex == null) return;

        // Process depth to world positions
        int kernel = depthProcessor.FindKernel("DepthToPosition");
        depthProcessor.SetTexture(kernel, "_DepthTex", depthTex);
        depthProcessor.SetTexture(kernel, "_PositionMap", positionMap);
        depthProcessor.SetMatrix("_InvProjection", Camera.main.projectionMatrix.inverse);
        depthProcessor.SetMatrix("_CameraToWorld", Camera.main.cameraToWorldMatrix);
        depthProcessor.Dispatch(kernel, 256/8, 192/8, 1);

        // Feed to VFX
        vfx.SetTexture("PositionMap", positionMap);
        vfx.SetTexture("ColorMap", colorMap);
    }
}
```

### Human Body Depth Pattern
```csharp
public class HumanDepthVFX : MonoBehaviour {
    [SerializeField] AROcclusionManager occlusionManager;
    [SerializeField] VisualEffect vfxGraph;

    void Update() {
        // Human depth (person segmentation)
        if (occlusionManager.humanDepthTexture != null) {
            vfxGraph.SetTexture("HumanDepth", occlusionManager.humanDepthTexture);
        }

        // Human stencil (binary mask)
        if (occlusionManager.humanStencilTexture != null) {
            vfxGraph.SetTexture("HumanStencil", occlusionManager.humanStencilTexture);
        }
    }
}
```

---

## 5. Million Particle Optimization

### VFX Graph Million Particles (dilmerv pattern)
From `dilmerv/UnityVFXMillionsOfParticles`.

```
Effects demonstrated:
- Sun Effect: 2M+ particles with turbulence
- Space Effect: Star field with parallax
- Fire Effect: Realistic flames with collisions
- Sun Movement: 5M+ particles with animation
```

**VFX Graph Settings for Millions**:
```
Capacity: 2000000-5000000
Spawn Rate: 500000-1000000
Sort: False (performance)
Strip: False (unless trails needed)
Indirect Draw: True
```

### DOTS Million Particles Pattern
```csharp
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public partial struct ParticleUpdateJob : IJobEntity {
    public float deltaTime;
    public float3 gravity;

    void Execute(ref ParticleData particle, in ParticleConfig config) {
        particle.velocity += gravity * deltaTime;
        particle.position += particle.velocity * deltaTime;
        particle.lifetime -= deltaTime;
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ParticleSystem : ISystem {
    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        new ParticleUpdateJob {
            deltaTime = SystemAPI.Time.DeltaTime,
            gravity = new float3(0, -9.81f, 0)
        }.ScheduleParallel();
    }
}
```

### Platform-Specific Limits
```csharp
public static int GetMaxParticles() {
    #if UNITY_IOS
        return SystemInfo.systemMemorySize > 6000 ? 750000 : 500000;
    #elif UNITY_ANDROID
        // Quest 2/3
        return 500000;
    #elif UNITY_STANDALONE_OSX
        // M3 Max optimized
        return 2000000;
    #elif UNITY_VISIONOS
        return 750000;
    #else
        return 1000000;
    #endif
}
```

---

## 6. Keijiro VFX Patterns

### VfxPyro (Fireworks)
Interactive particle effect with user input.

```csharp
// Spawn firework at position
public class PyroLauncher : MonoBehaviour {
    [SerializeField] VisualEffect pyroVfx;

    public void LaunchAt(Vector3 position, Color color) {
        pyroVfx.SetVector3("SpawnPosition", position);
        pyroVfx.SetVector4("ParticleColor", color);
        pyroVfx.SendEvent("Launch");
    }
}
```

### Rcam Depth Streaming Pattern
```csharp
// Real-time depth streaming from iOS LiDAR
public class RcamDepthReceiver : MonoBehaviour {
    [SerializeField] VisualEffect vfx;
    private Texture2D depthTexture;
    private Texture2D colorTexture;

    public void ReceiveFrame(byte[] depthData, byte[] colorData) {
        depthTexture.LoadRawTextureData(depthData);
        depthTexture.Apply();
        colorTexture.LoadRawTextureData(colorData);
        colorTexture.Apply();

        vfx.SetTexture("DepthMap", depthTexture);
        vfx.SetTexture("ColorMap", colorTexture);
    }
}
```

---

## 7. Networking Patterns (Multiplayer Painting)

### Normcore Brush Sync
```csharp
using Normal.Realtime;

public class RealtimeBrushStroke : RealtimeComponent<RealtimeBrushStrokeModel> {
    [SerializeField] LineRenderer lineRenderer;

    protected override void OnRealtimeModelReplaced(RealtimeBrushStrokeModel prev, RealtimeBrushStrokeModel current) {
        if (prev != null) prev.pointsDidChange -= OnPointsChanged;
        if (current != null) {
            current.pointsDidChange += OnPointsChanged;
            UpdateLine();
        }
    }

    public void AddPoint(Vector3 worldPos) {
        model.points.Add(new RealtimeVector3(worldPos));
    }

    void OnPointsChanged(RealtimeBrushStrokeModel model) => UpdateLine();

    void UpdateLine() {
        lineRenderer.positionCount = model.points.Count;
        for (int i = 0; i < model.points.Count; i++) {
            lineRenderer.SetPosition(i, model.points[i].ToVector3());
        }
    }
}
```

---

## Quick Reference

| Interest | Pattern Files | Key Classes |
|----------|--------------|-------------|
| Brushes | Open Brush repo | GeometryPool, GeniusParticlesBrush |
| Hand Tracking | HandPoseBarracuda | HandGestureRecognizer |
| Audio | VFXcubes-WASAPI | AudioReactiveVFX |
| Depth | Rcam series | DepthToVFX, HumanDepthVFX |
| Optimization | DOTS samples | ParticleSystem (ECS) |
| Networking | Normcore | RealtimeBrushStroke |

---

## GitHub Repos Referenced

- `icosa-foundation/open-brush` - Tilt Brush successor (brushes, pooling)
- `dilmerv/UnityVFXMillionsOfParticles` - Million particle examples
- `keijiro/VfxPyro` - Fireworks VFX
- `keijiro/HandPoseBarracuda` - Hand pose detection
- `mimisukeMaster/HandPoseBarracuda_GestureRecognition` - Gesture recognition
- `smaerdlatigid/VFXcubes-WASAPI` - Audio reactive VFX
- `keijiro/Rcam2` - LiDAR depth streaming

---

*Updated: 2026-01-13*
