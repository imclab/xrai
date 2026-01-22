# VFX25 Hologram, 3D Visualization & Portal Intelligence Patterns

**Source**: `/Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/______VFX25/`
**Extracted**: 2026-01-13
**Projects Analyzed**: 15+ Keijiro & community projects

---

## 1. Hologram Rendering Patterns

### 1.1 BodyPix Neural Segmentation (Unity Sentis)

**Source**: `BodyPixSentis-main` by Keijiro Takahashi
**Use Case**: Real-time body segmentation without LiDAR

```csharp
// Core pattern: Neural network body detection
public sealed class BodyDetector : System.IDisposable
{
    Worker _worker;  // Unity Sentis GPU worker

    public void ProcessImage(Texture sourceTexture)
    {
        // 1. Preprocess image for NN
        _preprocess.Dispatch(source, _resources.preprocess);

        // 2. Run neural network inference
        _worker.Schedule(_preprocess.Tensor);

        // 3. Postprocess - extract mask
        post1.SetBuffer(0, "Segments", _worker.PeekOutputBuffer("segments"));
        post1.SetTexture(0, "Output", _output.mask);
        post1.DispatchThreads(0, width, height, 1);

        // 4. Extract keypoints (17 body joints)
        post2.SetBuffer(0, "Keypoints", _output.keypoints);
    }

    // Outputs
    public RenderTexture MaskTexture => _output.mask;
    public GraphicsBuffer KeypointBuffer => _output.keypoints;
}
```

**Key Insight**: BodyPix runs at 512x384 input resolution for optimal performance/quality.

---

### 1.2 NNCam2 - BodyPix to VFX Pipeline

**Source**: `NNCam2-main`
**Use Case**: Camera effects with neural segmentation

```csharp
public sealed class BodyPixInput : MonoBehaviour
{
    BodyDetector _detector;

    void Start()
    {
        // Initialize detector at 512x384
        _detector = new BodyDetector(_resources, 512, 384);
    }

    void LateUpdate()
    {
        // Process frame
        _detector.ProcessImage(_source.AsTexture);

        // Output to render texture
        Graphics.SetRenderTarget(_output);
        _material.SetTexture(ShaderID.BodyPixTexture, _detector.MaskTexture);
        _material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, 1);
    }

    // Expose keypoints for VFX
    public GraphicsBuffer KeypointBuffer => _detector.KeypointBuffer;
}
```

**Filter Variants**: Delay, Thru, Feedback, Slitscan, Overlay

---

### 1.3 MetavidoVFX - VFX Graph Depth Binder

**Source**: `MetavidoVFX-main`
**Use Case**: Volumetric video playback via VFX Graph

```csharp
[VFXBinder("Metavido")]
public sealed class VFXMetavidoBinder : VFXBinderBase
{
    // Required VFX Graph properties
    ExposedProperty _colorMapProperty = "ColorMap";      // RGB texture
    ExposedProperty _depthMapProperty = "DepthMap";      // Depth texture
    ExposedProperty _rayParamsProperty = "RayParams";    // Camera ray parameters
    ExposedProperty _inverseViewProperty = "InverseView"; // Inverse view matrix
    ExposedProperty _depthRangeProperty = "DepthRange";  // Min/max depth

    public override void UpdateBinding(VisualEffect component)
    {
        var meta = _decoder.Metadata;
        if (!meta.IsValid) return;

        // Bind all properties
        component.SetTexture(_colorMapProperty, _demux.ColorTexture);
        component.SetTexture(_depthMapProperty, _demux.DepthTexture);
        component.SetVector4(_rayParamsProperty, RenderUtils.RayParams(meta));
        component.SetMatrix4x4(_inverseViewProperty, RenderUtils.InverseView(meta));
        component.SetVector2(_depthRangeProperty, meta.DepthRange);
    }
}
```

**VFX Graph Setup**:
1. Exposed Texture2D: `ColorMap`, `DepthMap`
2. Exposed Vector4: `RayParams`
3. Exposed Matrix4x4: `InverseView`
4. Exposed Vector2: `DepthRange`

---

### 1.4 Rcam4 - RGBD Streaming Metadata

**Source**: `Rcam4-main`
**Use Case**: LiDAR depth streaming between devices

```csharp
[StructLayout(LayoutKind.Sequential)]
public readonly struct Metadata
{
    // Camera pose (for world reconstruction)
    public readonly Vector3 CameraPosition;
    public readonly Quaternion CameraRotation;

    // Camera parameters (for unprojection)
    public readonly Matrix4x4 ProjectionMatrix;
    public readonly Vector2 DepthRange;  // (near, far)

    // Serialization for network transport
    public string Serialize()
    {
        ReadOnlySpan<Metadata> data = stackalloc Metadata[] { this };
        var bytes = MemoryMarshal.AsBytes(data).ToArray();
        return "<![CDATA[" + Convert.ToBase64String(bytes) + "]]>";
    }
}
```

**Depth Range**: Typically (0.1f, 10f) for indoor scenes

---

## 2. AR Portal Patterns

### 2.1 Stencil Buffer Portal Technique

**Source**: `ar-portal-arfoundation-master` by Tongzhou Yu
**Use Case**: Walk-through AR portals

**StencilMask.shader** - Writes to stencil buffer:
```hlsl
Shader "Custom/StencilMask"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Zwrite Off      // Don't write to depth
        ColorMask 0     // Don't write to color
        Cull off        // Render both sides

        Pass
        {
            Stencil {
                Ref 1           // Stencil reference value
                Comp always     // Always pass stencil test
                Pass replace    // Write Ref to stencil buffer
            }
            // Fragment outputs nothing (invisible mask)
        }
    }
}
```

**PortalManager.cs** - Bidirectional traversal:
```csharp
public class PortalManager : MonoBehaviour
{
    public Material[] materials;  // Inner world materials
    bool inOtherWorld;

    void SetMaterials(bool fullRender)
    {
        // Toggle stencil test direction
        var stencilTest = fullRender
            ? CompareFunction.NotEqual  // Show when NOT in portal area
            : CompareFunction.Equal;    // Show when IN portal area

        foreach (var mat in materials)
            mat.SetInt("_StencilComp", (int)stencilTest);
    }

    bool GetIsInFront()
    {
        // Calculate camera position relative to portal plane
        Vector3 worldPos = camera.position + camera.forward * nearClipPlane;
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        return localPos.y >= 0;  // Y-axis = portal normal
    }

    void whileCameraColliding()
    {
        bool isInFront = GetIsInFront();
        // Detect crossing (front-to-back or back-to-front)
        if ((isInFront && !wasInFront) || (wasInFront && !isInFront))
        {
            inOtherWorld = !inOtherWorld;
            SetMaterials(inOtherWorld);
        }
    }
}
```

**Key Components**:
1. **Portal Frame**: Mesh with StencilMask shader (invisible, writes stencil)
2. **Inner World**: Objects with `_StencilComp` material property
3. **Trigger Collider**: Detects camera traversal

---

## 3. 3D Visualization Patterns

### 3.1 Gaussian Splatting VR

**Source**: `Unity-VR-Gaussian-Splatting-main`
**Use Case**: Photorealistic 3D reconstruction in VR

**Integration Points**:
- XR Interaction Toolkit 3.0.3
- URP rendering pipeline
- Quest/PCVR compatible

### 3.2 Multi-Layer Gaussian Splatting (Anatomy)

**Source**: `Multi-Layer-Gaussian-Splatting-for-Immersive-Anatomy-Visualization-main`
**Use Case**: Medical/anatomical visualization with layers

**Key Innovation**: Separate splat layers for different anatomical structures

---

## 4. Project Index

### Hologram/Depth Projects
| Project | Purpose | Key Files |
|---------|---------|-----------|
| `BodyPixSentis-main` | Neural body segmentation | `BodyDetector.cs` |
| `NNCam2-main` | BodyPix VFX effects | `BodyPixInput.cs` |
| `MetavidoVFX-main` | Volumetric VFX playback | `VFXMetavidoBinder.cs` |
| `Rcam4-main` | LiDAR depth streaming | `Metadata.cs` |
| `Rcam3-main` | Legacy depth streaming | Multiple |
| `NNCam-main` | Original neural camera | Assets/NNCam/ |

### Portal Projects
| Project | Purpose | Key Files |
|---------|---------|-----------|
| `ar-portal-arfoundation-master` | Basic AR portal | `PortalManager.cs`, `StencilMask.shader` |
| `portals-urp-main` | URP-compatible portals | Shaders/ |

### 3D Visualization Projects
| Project | Purpose | Platform |
|---------|---------|----------|
| `Unity-VR-Gaussian-Splatting-main` | VR splat rendering | Quest/PCVR |
| `Multi-Layer-Gaussian-Splatting-*` | Anatomy viz | Desktop |
| `UnityGaussianSplatting-main` | Aras-P implementation | All |

### VFX/Effects Projects
| Project | Purpose |
|---------|---------|
| `FlashGlitch-main` | Screen glitch effects |
| `DrumPadVFX-main` | Audio-reactive VFX |
| `VFXCustomCode-main` | Custom VFX blocks |

---

## 5. Unified Hologram Pipeline (2026-01-16 Breakthrough)

**Discovery**: HologramSource was duplicating ARDepthSource compute work. Now unified.

### 5.1 Architecture

```
BEFORE (Redundant):
  ARDepthSource → PositionMap (compute #1) → Regular VFX
  HologramSource → PositionMap (compute #2) → Hologram VFX  ← DUPLICATE!

AFTER (Unified):
  ARDepthSource (singleton) → PositionMap → ALL VFX
                                    ↓
                          VFXARBinder (per-VFX)
                                    ↓
                    ┌───────────────┼───────────────┐
                    │               │               │
              Regular VFX    Hologram VFX     Other VFX
                            (+AnchorPos,
                             +HologramScale)
```

### 5.2 VFXARBinder Hologram Extensions

```csharp
// MetavidoVFX-main/Assets/Scripts/Bridges/VFXARBinder.cs
[Header("Hologram (Mini-Me)")]
[SerializeField] bool _bindAnchorPos = false;
[SerializeField] bool _bindHologramScale = false;
[SerializeField] Transform _anchorTransform;
[SerializeField] float _hologramScale = 0.15f;  // 15% = mini-me
[SerializeField] bool _useTransformMode = true; // Transform VFX directly

// Two modes:
// 1. Transform Mode: Moves/scales VFX GameObject directly
// 2. Property Mode: Binds AnchorPos/HologramScale to VFX properties
```

### 5.3 HologramPlacer Touch Gestures

```csharp
// MetavidoVFX-main/Assets/Scripts/Hologram/HologramPlacer.cs
// Simple AR placement - tap to place, gestures to manipulate

| Gesture | Action |
|---------|--------|
| Tap | Place on AR plane |
| 1-finger drag | Translate X/Z |
| 2-finger drag | Translate Y (height) |
| Pinch | Scale (0.05x - 2x) |

// Key pattern: Track touchCount changes
void HandleManipulation() {
    if (Input.touchCount == 1) HandleDrag();      // XZ
    else if (Input.touchCount == 2) HandleTwoFingerGesture();  // Y + Scale
}
```

### 5.4 HologramController (Live AR / Metavido)

```csharp
// MetavidoVFX-main/Assets/Scripts/Hologram/HologramController.cs
public enum SourceMode {
    LiveAR,         // ARDepthSource (real-time)
    MetavidoVideo   // Metavido .mp4 file playback
}

// Metavido mode uses:
// - VideoPlayer → video frames
// - TextureDemuxer → ColorTexture, DepthTexture (from side-by-side encoding)
// - MetadataDecoder → RayParams, InverseView, DepthRange
```

### 5.5 Prefab Structure

```
Assets/Prefabs/Hologram/Hologram.prefab
├── HologramPlacer       (touch gestures)
├── HologramController   (mode switching)
└── HologramVFX
    ├── VisualEffect     (hologram_depth_people_metavido.vfx)
    ├── VFXARBinder      (binds AR data + hologram transform)
    └── Scale: 0.15      (mini-me default)
```

### 5.6 Key Learnings

1. **Singleton Compute**: ONE ARDepthSource serves ALL VFX - never duplicate GPU work
2. **Transform Mode**: Simpler than VFX properties for placement/scale
3. **Touch Pattern**: `touchCount` changes distinguish gesture types
4. **Metavido Integration**: TextureDemuxer extracts Color/Depth from encoded video

---

## 6. Quick Reference: VFX Graph Depth Setup

```
VFX Graph Properties Required:
├── ColorMap (Texture2D) - RGB camera feed
├── DepthMap (Texture2D) - Depth buffer (R32F or R16)
├── RayParams (Vector4) - (tanHalfFovX, tanHalfFovY, 0, 0)
├── InverseView (Matrix4x4) - Camera.cameraToWorldMatrix
└── DepthRange (Vector2) - (nearClip, farClip)

Unprojection Formula (in VFX):
worldPos = InverseView * (rayDir * depth)
where rayDir = normalize(uv * RayParams.xy - 0.5, 1)
```

---

## 6. Location Reference

All projects located in:
```
/Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/______VFX25/
├── new keijiro/           # Keijiro's projects
│   ├── HOLO.vfx.Demos/   # Demo collection
│   ├── BodyPixSentis-main/
│   ├── MetavidoVFX-main/
│   └── ...
├── RCAMS/                 # Rcam series
├── ar-portal-arfoundation-master/
├── portals-urp-main/
└── NNCam-main/
```

---

## 🔗 CROSS-REFERENCES

### Auto-Fix Patterns (106 total)
See `_AUTO_FIX_PATTERNS.md` for auto-applicable VFX fixes:
- **VFX Custom HLSL**: `void Func(inout VFXAttributes)` signature
- **SampleLevel vs Sample**: Use `SampleLevel(tex, uv, 0)` in compute
- **Global Texture Limitation**: VFX can't read Shader.SetGlobalTexture
- **VFXEventAttribute Pooling**: Cache in Start(), reuse
- **RayParams Calculation**: From projection matrix

### Quick Fixes
```bash
kbfix "VFX HLSL"       # Custom function fixes
kbfix "SampleLevel"    # Texture sampling in compute
kbfix "VFX event"      # Event attribute issues
```

### Related KB Files
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR + VFX integration
- `_RCAM_VFX_BINDING_SPECIFICATION.md` - Rcam depth binding
- `_LASPVFX_AUDIO_BINDING_PATTERNS.md` - Audio-reactive VFX
- `_INTELLIGENCE_SYSTEMS_INDEX.md` - Central reference

---

**Maintainer**: James Tunick
**Last Updated**: 2026-01-22
**Confidence**: 100% (extracted from actual source code)
