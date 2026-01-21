# Feature Specification: High-Fidelity Hologram VFX

**Feature Branch**: `014-hifi-hologram-vfx`
**Created**: 2026-01-21
**Status**: Planning
**Priority**: P0 (Critical for lifelike telepresence)

---

## Overview

Create high-fidelity hologram VFX that render humans as realistic point clouds by sampling actual RGB colors from video textures. This spec defines the VFX Graph setup, quality presets, and controller components for lifelike hologram rendering.

**Design Philosophy**: Inspired by Record3D, Metavido, and PeopleVFX - prioritizing visual fidelity through actual color sampling rather than gradient tints.

---

## Research: Existing Point Cloud Implementations

### Record3D Unity Demo

Based on [record3d_unity_demo](https://github.com/marek-simonik/record3d_unity_demo):

| Technique | Description | Our Implementation |
|-----------|-------------|-------------------|
| **RGB Sampling** | Sample video texture at particle UV | Sample ColorMap in VFX Initialize |
| **Dense Points** | 100K-200K particles | Quality presets (10K-200K) |
| **Small Particles** | 1-5mm for crisp detail | 1.5mm-5mm based on quality |
| **LiDAR Depth** | Direct depth from iOS sensor | ARDepthSource → PositionMap |

### Metavido VFX Approach

Based on [jp.keijiro.metavido.vfxgraph](https://github.com/keijiro/Metavido):

| Block | Purpose | Our Usage |
|-------|---------|-----------|
| `Metavido Sample UV` | Random position + color from encoded frame | Use for video playback |
| `Metavido Sample Random` | Sample random pixel for new particles | Use in Initialize |
| Burnt-in Barcode | Camera pose metadata in frame | For recorded playback |

### Point Cloud Best Practices

| Principle | Rationale | Implementation |
|-----------|-----------|----------------|
| **No Color Tinting** | Preserves skin tones | Use pure RGB from ColorMap |
| **High Particle Density** | Fills gaps in point cloud | 50K-200K particles |
| **Small Particle Size** | Crisp edges, no blur | 1.5mm-5mm diameter |
| **UV-Position Correlation** | Color matches 3D location | Store UV, sample at same point |

---

## Quality Presets

| Preset | Particles | Size | GPU Time | Use Case |
|--------|-----------|------|----------|----------|
| **Low** | 10,000 | 5mm | ~1ms | Mobile stress testing |
| **Medium** | 50,000 | 3mm | ~2ms | Balanced (iPhone) |
| **High** | 100,000 | 2mm | ~3ms | Quality (Quest 3) |
| **Ultra** | 200,000 | 1.5mm | ~5ms | Maximum (PC/Vision Pro) |

### Auto Quality Adjustment

```csharp
// When FPS drops below 80% of target, reduce quality
if (currentFPS < targetFPS * 0.8f && quality > Low)
    Quality = quality - 1;

// When FPS exceeds 120% of target, increase quality
if (currentFPS > targetFPS * 1.2f && quality < Ultra)
    Quality = quality + 1;
```

---

## VFX Graph Architecture

### Required Exposed Properties

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
| `ColorSaturation` | Float | Saturation multiplier (default 1.0) |
| `ColorBrightness` | Float | Brightness multiplier (default 1.0) |

### VFX Graph Structure

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
│ ├── Sample Random UV (0-1, 0-1)                             │
│ ├── Store UV as attribute                                   │
│ ├── Sample PositionMap at UV → Set Position                 │
│ ├── Sample ColorMap at UV → Set Color  [CRITICAL]           │
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
│ └── Blend Mode: Alpha or Additive                           │
└─────────────────────────────────────────────────────────────┘
```

### Color Sampling (Critical for Realism)

The key differentiator for lifelike holograms:

**Option 1: Sample Texture2D Block**
```
1. Add "Sample Texture2D" operator
2. Connect ColorMap to Texture input
3. Connect particle's stored UV to UV input
4. Connect output Color to "Set Color" block
```

**Option 2: Custom HLSL**
```hlsl
float4 SampleVideoColor(VFXSampler2D colorMap, float2 uv)
{
    int2 dims;
    colorMap.t.GetDimensions(dims.x, dims.y);
    return colorMap.t.Load(int3(uv * dims, 0));
}
```

**Option 3: Metavido Sample UV Block**
```
1. Add "Metavido Sample UV" block (from jp.keijiro.metavido.vfxgraph)
2. It automatically samples ColorMap at particle position
3. Outputs color directly to particle
```

---

## User Stories & Testing

### User Story 1 - Lifelike Self-Hologram (Priority: P0)

As a user, I want to see myself as a realistic hologram with accurate skin tones and clothing colors.

**Independent Test**:
1. Open HOLOGRAM.unity scene
2. Add HiFiHologramController to VFX object
3. Set Quality to High
4. Point camera at person
5. Verify hologram colors match actual person
6. Verify no gradient tinting visible

**Acceptance Scenarios**:
1. **Given** ColorMap is video feed, **When** VFX renders, **Then** particles show actual RGB colors.
2. **Given** Quality is High, **When** checking visually, **Then** 100K particles fill human shape.
3. **Given** person is wearing red shirt, **When** viewing hologram, **Then** hologram shirt is red.

### User Story 2 - Quality Presets (Priority: P1)

As a developer, I want to quickly switch between quality presets for different devices.

**Independent Test**:
1. Add HiFiHologramController to VFX
2. Set Quality to Low, observe FPS
3. Set Quality to Ultra, observe FPS
4. Verify particle counts match presets

**Acceptance Scenarios**:
1. **Given** Quality is Low, **When** checking VFX, **Then** ParticleCount is 10,000.
2. **Given** Quality is Ultra, **When** checking VFX, **Then** ParticleCount is 200,000.
3. **Given** auto-adjust enabled, **When** FPS drops, **Then** Quality reduces automatically.

### User Story 3 - Multi-Hologram Performance (Priority: P1)

As a user in a conference, I want multiple high-fidelity holograms to render at 30+ FPS.

**Independent Test**:
1. Simulate 4 holograms using EditorConferenceSimulator
2. Set all to Medium quality (50K each)
3. Verify FPS stays above 30
4. Monitor memory usage (<500MB total)

**Acceptance Scenarios**:
1. **Given** 4 Medium holograms, **When** rendering, **Then** FPS > 30.
2. **Given** 4 High holograms, **When** on Quest 3, **Then** FPS > 45.
3. **Given** memory pressure, **When** monitored, **Then** <500MB for 4 holograms.

---

## Requirements

### Functional Requirements

- **FR-001**: VFX MUST sample actual RGB color from ColorMap at particle UV position.
- **FR-002**: VFX MUST NOT apply gradient or single-color tinting to particles.
- **FR-003**: Quality presets MUST control both particle count and size.
- **FR-004**: Auto quality adjustment MUST reduce quality when FPS drops.
- **FR-005**: HiFiHologramController MUST expose public API for texture binding.
- **FR-006**: VFX MUST support both live AR and Metavido video sources.

### Non-Functional Requirements

- **NFR-001**: Single hologram at High quality < 4ms GPU time.
- **NFR-002**: Memory per hologram < 100MB (textures + particles).
- **NFR-003**: Color accuracy > 95% match to source video.
- **NFR-004**: Particle visibility distance > 5 meters.

---

## Implementation

### HiFiHologramController.cs

**Location**: `Assets/H3M/VFX/HiFiHologramController.cs`
**Status**: ✅ Implemented

```csharp
public class HiFiHologramController : MonoBehaviour
{
    // Quality presets
    public HologramQuality Quality { get; set; }

    // Texture inputs
    public void SetColorMap(Texture2D colorMap);
    public void SetDepthMap(Texture2D depthMap);
    public void SetStencilMap(Texture2D stencilMap);

    // Camera matrices
    public void SetCameraMatrices(Matrix4x4 inverseView, Vector4 rayParams, Vector2 depthRange);

    // Quality control
    public void ForceQuality(HologramQuality quality);
    public void EnableAutoQuality(int targetFPS = 60);
}
```

### VFX Asset

**Location**: `Assets/VFX/People/hifi_hologram_people.vfx` (to be created)

Required blocks:
1. Initialize: Sample PositionMap, Sample ColorMap, Set Size
2. Update: Optional turbulence, depth culling
3. Output: Billboard quads with RGB color

---

## File Structure

```
Assets/
├── H3M/
│   └── VFX/
│       └── HiFiHologramController.cs     # ✅ Implemented
├── VFX/
│   └── People/
│       ├── hifi_hologram_people.vfx      # ⬜ To create
│       └── hifi_hologram_gsplat.vfx      # ⬜ Future: Gaussian splatting
├── Documentation/
│   └── HIFI_HOLOGRAM_VFX_SETUP.md        # ✅ Implemented
```

---

## Integration Points

### With ARDepthSource (Spec 006)

```csharp
// ARDepthSource provides all required textures
var source = ARDepthSource.Instance;
controller.SetColorMap(source.ColorMap);
controller.SetDepthMap(source.DepthMap);
controller.SetCameraMatrices(source.InverseView, source.RayParams, source.DepthRange);
```

### With ConferenceLayoutManager (Spec 003)

```csharp
// Each remote hologram gets a HiFiHologramController
public void OnRemoteHologramCreated(string peerId, GameObject hologram)
{
    var controller = hologram.GetComponent<HiFiHologramController>();
    controller.Quality = DetermineQualityForPeerCount(_connectedPeers.Count);
}
```

### With VFXARBinder (Spec 006)

```csharp
// VFXARBinder auto-detects and binds all required properties
var binder = vfx.GetComponent<VFXARBinder>();
binder.Refresh(); // Binds ColorMap, DepthMap, PositionMap, etc.
```

---

## Implementation Phases

### Phase 1: Core VFX (Sprint 1)

- [ ] Create hifi_hologram_people.vfx with color sampling
- [ ] Integrate with HiFiHologramController
- [ ] Test quality presets on device
- [ ] Verify color accuracy

### Phase 2: Optimization (Sprint 2)

- [ ] Implement auto quality adjustment
- [ ] Add GPU instancing for multi-hologram
- [ ] Profile memory usage
- [ ] Add LOD system for distant holograms

### Phase 3: Advanced Rendering (Sprint 3)

- [ ] Gaussian splatting variant
- [ ] Temporal stability (reduce particle jitter)
- [ ] Edge refinement for crisp silhouettes
- [ ] SSAO for depth perception

---

## Success Criteria

- [ ] SC-001: Hologram skin tones match video source
- [ ] SC-002: High quality renders at 100K particles
- [ ] SC-003: Single hologram < 4ms GPU time
- [ ] SC-004: Auto quality keeps FPS above target
- [ ] SC-005: 4 holograms render at 30+ FPS
- [ ] SC-006: Memory < 100MB per hologram

---

## References

- [Record3D Unity Demo](https://github.com/marek-simonik/record3d_unity_demo)
- [Metavido Package](https://github.com/keijiro/Metavido)
- [Point Cloud Renderer](https://github.com/pablothedolphin/Point-Cloud-Renderer)
- KB: `_COMPREHENSIVE_HOLOGRAM_PIPELINE_ARCHITECTURE.md`
- KB: `_VISION_PRO_SPATIAL_PERSONAS_PATTERNS.md`
- Spec 003: Hologram Conferencing
- Spec 006: VFX Library Pipeline

---

*Created: 2026-01-21*
*Author: Claude Code*
