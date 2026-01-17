# Gaussian Splatting VFX Patterns

**Source**: keijiro/SplatVFX
**Updated**: 2026-01-17

---

## Overview

3D Gaussian Splatting rendered via VFX Graph. Point cloud data (position, orientation, color) imported from `.splat` files and bound to VFX particles.

---

## Architecture

```
.splat File → SplatImporter → SplatData (ScriptableObject)
                                    ↓
                            GraphicsBuffer (GPU)
                                    ↓
                            VFXSplatDataBinder
                                    ↓
                            VFX Graph (8M particles)
```

---

## Core Components

### SplatData (ScriptableObject)

Stores point cloud data with lazy GPU buffer creation.

```csharp
public sealed class SplatData : ScriptableObject
{
    // CPU arrays (serialized)
    public Vector3[] PositionArray { get; set; }
    public Vector3[] AxisArray { get; set; }     // 3 per splat (ellipsoid axes)
    public Color[] ColorArray { get; set; }

    // GPU buffers (cached)
    public GraphicsBuffer PositionBuffer => GetCachedBuffers().position;
    public GraphicsBuffer AxisBuffer => GetCachedBuffers().axis;
    public GraphicsBuffer ColorBuffer => GetCachedBuffers().color;

    // Lazy GPU allocation
    (GraphicsBuffer, GraphicsBuffer, GraphicsBuffer) GetCachedBuffers()
    {
        if (_cachedBuffers.position == null)
        {
            _cachedBuffers.position = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                SplatCount,
                sizeof(float) * 3);
            _cachedBuffers.position.SetData(PositionArray);
            // ... axis and color buffers
        }
        return _cachedBuffers;
    }

    void OnDisable() => ReleaseGpuResources();
}
```

### VFXSplatDataBinder (Property Binder)

Standard VFX property binder pattern using `ExposedProperty`.

```csharp
[VFXBinder("Splat Data")]
public sealed class VFXSplatDataBinder : VFXBinderBase
{
    public SplatData SplatData = null;

    // ExposedProperty for VFX Graph binding resolution
    [VFXPropertyBinding("System.UInt32"), SerializeField]
    ExposedProperty _splatCountProperty = "SplatCount";

    [VFXPropertyBinding("UnityEngine.GraphicsBuffer"), SerializeField]
    ExposedProperty _positionBufferProperty = "PositionBuffer";

    [VFXPropertyBinding("UnityEngine.GraphicsBuffer"), SerializeField]
    ExposedProperty _axisBufferProperty = "AxisBuffer";

    [VFXPropertyBinding("UnityEngine.GraphicsBuffer"), SerializeField]
    ExposedProperty _colorBufferProperty = "ColorBuffer";

    public override bool IsValid(VisualEffect component)
      => SplatData != null &&
         component.HasUInt(_splatCountProperty) &&
         component.HasGraphicsBuffer(_positionBufferProperty);

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetUInt(_splatCountProperty, (uint)SplatData.SplatCount);
        component.SetGraphicsBuffer(_positionBufferProperty, SplatData.PositionBuffer);
        component.SetGraphicsBuffer(_axisBufferProperty, SplatData.AxisBuffer);
        component.SetGraphicsBuffer(_colorBufferProperty, SplatData.ColorBuffer);
    }
}
```

---

## .splat File Format

32-byte binary records:

| Offset | Size | Field | Type |
|--------|------|-------|------|
| 0 | 12 | Position (x,y,z) | float[3] |
| 12 | 12 | Scale (x,y,z) | float[3] |
| 24 | 4 | Color (RGBA) | byte[4] |
| 28 | 4 | Rotation (quaternion) | byte[4] |

```csharp
struct ReadData
{
    public float px, py, pz;  // Position
    public float sx, sy, sz;  // Scale (ellipsoid axes)
    public byte r, g, b, a;   // Color
    public byte rw, rx, ry, rz; // Rotation quaternion (normalized)
}
```

### Coordinate Conversion

```csharp
[BurstCompile]
void ParseReadData(in ReadData src, out Vector3 position,
    out Vector3 axis1, out Vector3 axis2, out Vector3 axis3, out Color color)
{
    // Convert rotation bytes to quaternion
    var rv = (math.float4(src.rx, src.ry, src.rz, src.rw) - 128) / 128;
    var q = math.quaternion(-rv.x, -rv.y, rv.z, rv.w);

    // Convert to Unity coordinate system (flip Z)
    position = math.float3(src.px, src.py, -src.pz);

    // Compute ellipsoid axes via quaternion rotation
    axis1 = math.mul(q, math.float3(src.sx, 0, 0));
    axis2 = math.mul(q, math.float3(0, src.sy, 0));
    axis3 = math.mul(q, math.float3(0, 0, src.sz));

    color = (Vector4)math.float4(src.r, src.g, src.b, src.a) / 255;
}
```

---

## VFX Graph Properties

| Property | Type | Description |
|----------|------|-------------|
| SplatCount | uint | Number of splats |
| PositionBuffer | GraphicsBuffer | World positions |
| AxisBuffer | GraphicsBuffer | 3 axes per splat (orientation) |
| ColorBuffer | GraphicsBuffer | RGBA colors |

---

## VFX Graph Custom Blocks

| Block | Purpose |
|-------|---------|
| `InitializeSplat.vfxblock` | Initialize particles from buffer |
| `ProjectSplat.vfxblock` | Project 3D Gaussians to screen |
| `SampleSplatAxes.vfxoperator` | Sample ellipsoid axes |
| `SelectMajorAxes.vfxoperator` | Sort axes by size |

---

## Shader (Gaussian.shadergraph)

Renders ellipsoid splats with Gaussian falloff:

```hlsl
// Screen-space Gaussian projection
float2 screenPos = TransformWorldToScreen(worldPos);
float3 majorAxis = GetMajorAxis(axes);
float gaussianWeight = exp(-dot(offset, offset) / (2 * sigma * sigma));
```

---

## Integration Pattern

### Adding to Project

```json
// manifest.json
{
  "scopedRegistries": [{
    "name": "Keijiro",
    "url": "https://registry.npmjs.com",
    "scopes": ["jp.keijiro"]
  }],
  "dependencies": {
    "jp.keijiro.splat-vfx": "file:../../SplatVFX/jp.keijiro.splat-vfx"
  }
}
```

### Runtime Usage

```csharp
// Load SplatData asset
var splatData = Resources.Load<SplatData>("MySplatData");

// Create VFX with binder
var go = new GameObject("Splat");
var vfx = go.AddComponent<VisualEffect>();
vfx.visualEffectAsset = splatVfxAsset;

var binderBase = go.AddComponent<VFXPropertyBinder>();
var binder = binderBase.AddPropertyBinder<VFXSplatDataBinder>();
binder.SplatData = splatData;
```

---

## Capacity Limits

| Configuration | Max Splats | Memory |
|---------------|------------|--------|
| Default | 8,000,000 | ~512MB GPU |
| Low-end | 1,000,000 | ~64MB GPU |
| High-end | 16,000,000+ | ~1GB+ GPU |

To change: Edit VFX Graph → Initialize Particle → Capacity

---

## Performance Notes

- **Projection artifacts**: VFX Graph algorithm causes sudden pops during camera motion
- **Color space**: .splat files trained in sRGB may have artifacts in Linear rendering
- **For production**: Consider [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) (compute-based)

---

## Creating .splat Files

1. Train Gaussian Splatting model → get `.ply` file
2. Use [WebGL Gaussian Splat Viewer](https://github.com/antimatter15/splat)
3. Drag & drop `.ply` → downloads `.splat`

---

## Related Files

- `SplatVFX/jp.keijiro.splat-vfx/Runtime/SplatData.cs` - Data container
- `SplatVFX/jp.keijiro.splat-vfx/Runtime/SplatDataBinder.cs` - VFX binder
- `SplatVFX/jp.keijiro.splat-vfx/Editor/SplatImporter.cs` - Asset importer
- `SplatVFX/jp.keijiro.splat-vfx/VFX/Splat.vfx` - VFX Graph
- `KnowledgeBase/_HAND_VFX_PATTERNS.md` - Hand tracking VFX
- `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR VFX patterns
