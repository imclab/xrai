# High-Fidelity Hologram VFX Setup Guide

**Purpose**: Create lifelike hologram VFX that samples actual RGB colors from video texture
**Based on**: Record3D, Metavido, PeopleVFX patterns
**Target**: 100K+ particles with actual video colors for realistic human representation

---

## Overview

Traditional hologram VFX often use gradient colors or single-color tinting. For **lifelike telepresence**, we need to:

1. **Sample actual RGB color** from the video texture at each particle's UV position
2. Use **high particle counts** (50K-200K) for dense point clouds
3. Keep **particle sizes small** (1-5mm) for crisp detail
4. **Avoid color tinting** - pure video color for realism

---

## VFX Graph Node Setup

### Required Exposed Properties

Add these as **Exposed Properties** in VFX Graph (Blackboard):

| Property | Type | Description |
|----------|------|-------------|
| `ColorMap` | Texture2D | RGB video frame from camera |
| `DepthMap` | Texture2D | Depth texture from LiDAR |
| `StencilMap` | Texture2D | Human segmentation mask |
| `PositionMap` | Texture2D | World positions (from ARDepthSource) |
| `ParticleCount` | UInt | Number of particles (10K-200K) |
| `ParticleSize` | Float | Particle size in meters (0.001-0.01) |
| `RayParams` | Vector4 | Inverse projection for depth→world |
| `InverseView` | Matrix4x4 | Camera inverse view matrix |
| `DepthRange` | Vector2 | (near, far) depth clipping |

### Initialize Context

```
Initialize Particle [Set Capacity: ParticleCount]
├── Set Position from Map
│   └── Sample PositionMap at random UV
│       or use "Metavido Sample Random" block
├── Set Color from Map [CRITICAL for realism]
│   └── Sample ColorMap at same UV as position
│       Block: Sample Texture2D → Set Color
├── Set Size
│   └── ParticleSize (constant for uniform point cloud)
└── Set Lifetime
    └── Random(0.5, 1.5) for natural cycling
```

### Color Sampling (Critical for Realism)

The key to lifelike holograms is sampling the actual RGB color from the video:

**Option 1: Using Sample Texture2D Block**
```
1. Add "Sample Texture2D" operator
2. Connect ColorMap to Texture input
3. Connect particle's stored UV to UV input
4. Connect output Color to "Set Color" block
```

**Option 2: Using Metavido Sample UV Block**
```
1. Add "Metavido Sample UV" block (from jp.keijiro.metavido.vfxgraph)
2. It automatically samples ColorMap at particle position
3. Outputs color directly to particle
```

**Option 3: Custom HLSL (Advanced)**
```hlsl
// In Initialize/Update context
float4 SampleVideoColor(VFXSampler2D colorMap, float2 uv)
{
    return colorMap.t.Load(int3(uv * colorMap.t.GetDimensions().xy, 0));
}
```

### Update Context

```
Update Particle
├── [Optional] Conform to Sphere
│   └── Add subtle turbulence for organic feel
├── [Optional] Add Velocity
│   └── Sample VelocityMap for motion trails
└── Kill (Age > Lifetime OR outside DepthRange)
```

### Output Context

```
Output Particle Quad (or Point)
├── Set Size: ParticleSize
├── Set Color: RGB (from stored color attribute)
├── Orient: Face Camera
└── [Optional] Soft Particle: blend at depth edges
```

---

## Quality Presets

| Preset | Particles | Size | Use Case |
|--------|-----------|------|----------|
| **Low** | 10,000 | 5mm | Mobile, stress testing |
| **Medium** | 50,000 | 3mm | Balanced (iPhone) |
| **High** | 100,000 | 2mm | High quality (Quest 3) |
| **Ultra** | 200,000 | 1.5mm | Maximum fidelity (PC) |

---

## VFX Graph Structure

### Recommended VFX Graph Layout

```
[System: HiFi Hologram]

┌─────────────────────────────────────────────────────────────┐
│ Spawn                                                        │
│ ├── Spawn Rate: Constant (ParticleCount / Lifetime)        │
│ └── or Single Burst: ParticleCount                          │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│ Initialize                                                   │
│ ├── [Metavido Sample Random] or [Sample PositionMap]        │
│ │   └── Sets position from depth-to-world texture          │
│ ├── [Sample ColorMap]  ← CRITICAL FOR REALISM               │
│ │   └── Store UV, sample color at particle position        │
│ ├── Set Size: ParticleSize                                  │
│ └── Set Lifetime: Random(0.5, 1.5)                          │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│ Update                                                       │
│ ├── [Optional] Turbulence Noise (strength: 0.01)            │
│ └── Kill: Age > Lifetime OR Depth out of range             │
└─────────────────────────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│ Output Particle Quad                                         │
│ ├── Orient: Face Camera Billboard                           │
│ ├── Set Size: ParticleSize                                  │
│ ├── Set Color: RGB (from color attribute)                   │
│ └── Blend Mode: Additive or Alpha                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Creating the VFX Asset

### Step 1: Create VFX Graph

1. Right-click in Project → Create → Visual Effects → Visual Effect Graph
2. Name: `hifi_hologram_people_h3m.vfx`
3. Save in `Assets/VFX/People/`

### Step 2: Add Exposed Properties

1. Open VFX Graph
2. Add to Blackboard:
   - ColorMap (Texture2D)
   - DepthMap (Texture2D)
   - StencilMap (Texture2D)
   - PositionMap (Texture2D)
   - ParticleCount (UInt, default: 100000)
   - ParticleSize (Float, default: 0.002)
   - RayParams (Vector4)
   - InverseView (Matrix4x4)
   - DepthRange (Vector2, default: 0.1, 10)

### Step 3: Setup Spawn

1. Add Spawn context
2. Add "Constant Spawn Rate" block
3. Set rate to `ParticleCount / 1.0` (particles per second)

### Step 4: Setup Initialize

1. Add Initialize context
2. Add **"Metavido Sample UV"** block (or custom sampling)
   - This samples position from DepthMap + color from ColorMap
3. Set Size: `ParticleSize`
4. Set Lifetime: `Random(0.5, 1.5)`

### Step 5: Setup Update

1. Add Update context
2. [Optional] Add "Turbulence (Perlin)" with low intensity (0.01)
3. Add kill condition for depth range

### Step 6: Setup Output

1. Add Output Particle Quad
2. Set Orient: Face Camera Billboard
3. Set Size: `ParticleSize`
4. Set Color: RGB (from color attribute)
5. Blend Mode: Alpha or Additive

### Step 7: Add Controller

1. Add `HiFiHologramController` component to VFX GameObject
2. Set quality preset
3. Connect to ARDepthSource or video player

---

## Integration with HiFiHologramController

The `HiFiHologramController.cs` script manages:
- Quality presets (10K → 200K particles)
- Auto quality adjustment based on FPS
- Color/brightness tweaking
- Depth fade settings

```csharp
// Usage
var controller = vfxGameObject.GetComponent<HiFiHologramController>();
controller.Quality = HologramQuality.High;
controller.SetColorMap(videoColorTexture);
controller.SetDepthMap(lidarDepthTexture);
```

---

## Performance Tips

1. **Use GPU Events** for particle recycling (not CPU spawn)
2. **Batch texture updates** - don't set textures every frame
3. **Use RenderTexture** not Texture2D for dynamic updates
4. **Reduce ParticleCount** first when optimizing (before size)
5. **Kill particles** outside depth range to reduce overdraw

---

## References

- [Record3D Unity Demo](https://github.com/marek-simonik/record3d_unity_demo)
- [Point Cloud Renderer](https://github.com/pablothedolphin/Point-Cloud-Renderer)
- [Metavido Package](https://github.com/keijiro/Metavido)
- Metavido VFX blocks: `Packages/jp.keijiro.metavido.vfxgraph/VFX/`

---

## Next Steps

1. **Create VFX Asset**: Run `H3M/HiFi Hologram/Create Optimized HiFi Hologram VFX` to generate `hifi_hologram_optimized.vfx`.
2. **Scene Setup**: Open `HOLOGRAM.unity` and run `H3M/HiFi Hologram/Add to Hologram Prefab`.
3. **Testing**: Verify hologram rendering in Editor (mock data) and on device.

---

*Created: 2026-01-21*
*Location: Assets/Documentation/HIFI_HOLOGRAM_VFX_SETUP.md*
