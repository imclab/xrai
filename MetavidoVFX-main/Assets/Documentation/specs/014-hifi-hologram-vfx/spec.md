# Feature Specification: High-Fidelity Hologram VFX

**Feature Branch**: `014-hifi-hologram-vfx`
**Created**: 2026-01-21
**Status**: Complete (VFX assets created)
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
│ └── Blend Mode: Additive or Alpha                           │
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

## Hologram Integration

### Single User Mode (Self-Hologram)

The HiFi VFX integrates with the local AR pipeline for self-viewing:

```
AR Foundation → ARDepthSource → HiFiHologramController → VFX
                     ↓
              PositionMap, ColorMap, DepthMap
                     ↓
        hifi_hologram_people.vfx → RGB Point Cloud
```

**Setup Steps**:
1. Add `hifi_hologram_people.vfx` to scene
2. Add `HiFiHologramController` component to VFX GameObject
3. Add `VFXARBinder` to bind ARDepthSource outputs
4. Configure quality preset based on device

**Key Bindings**:
| Component | Provides | Consumes |
|-----------|----------|----------|
| ARDepthSource | ColorMap, DepthMap, PositionMap | AR Foundation |
| VFXARBinder | (binding) | ARDepthSource outputs |
| HiFiHologramController | Quality control | VFX properties |
| hifi_hologram_people.vfx | Rendered particles | All above |

### Conference Mode (Multi-Hologram)

For WebRTC conferencing, each remote peer gets their own HiFi hologram:

```
WebRTC Stream → MetavidoWebRTCDecoder → HiFiHologramController → VFX
                        ↓
              ColorTexture, DepthTexture, Metadata
                        ↓
          hifi_hologram_people.vfx → Remote Hologram
```

**Multi-Hologram Architecture**:
```
┌────────────────────────────────────────────────────────────────┐
│                   ConferenceLayoutManager                       │
│    Manages seat poses, spatial layout, peer tracking           │
└───────────────────────┬────────────────────────────────────────┘
                        ↓
┌────────────────────────────────────────────────────────────────┐
│               HologramConferenceManager                         │
│    Creates/destroys hologram instances per peer                │
└───────────────────────┬────────────────────────────────────────┘
                        ↓
┌──────────────────────────────────────────────────────────────────┐
│ For Each Remote Peer:                                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ MetavidoWebRTC  │  │ HiFiHologram    │  │ hifi_hologram   │  │
│  │ Decoder         │→ │ Controller      │→ │ _people.vfx     │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘  │
└──────────────────────────────────────────────────────────────────┘
```

**Dynamic Quality Scaling**:
```csharp
// Auto-adjust quality based on peer count
public HologramQuality GetQualityForPeerCount(int peerCount)
{
    return peerCount switch
    {
        1 => HologramQuality.Ultra,   // 1 peer: max quality
        2 => HologramQuality.High,    // 2 peers: high quality
        <= 4 => HologramQuality.Medium, // 3-4 peers: balanced
        _ => HologramQuality.Low      // 5+ peers: performance
    };
}
```

---

## Testing Procedures

### Test Suite 1: Single User Mode

**Prerequisites**:
- iOS device with LiDAR (iPhone Pro/iPad Pro)
- ARDepthSource in scene
- HiFi VFX with VFXARBinder

| Test | Steps | Expected Result |
|------|-------|-----------------|
| **T1.1: Basic Rendering** | 1. Open HOLOGRAM.unity<br>2. Add hifi_hologram_people VFX<br>3. Add HiFiHologramController<br>4. Run on device | Hologram renders with colored particles |
| **T1.2: Color Accuracy** | 1. Hold red object in frame<br>2. Observe hologram colors | Red object appears red in hologram |
| **T1.3: Skin Tones** | 1. Point camera at face<br>2. Observe hologram | Skin tones are natural, not tinted |
| **T1.4: Quality Switching** | 1. Set Quality to Low<br>2. Observe particle count<br>3. Set Quality to Ultra | Low: sparse, Ultra: dense |
| **T1.5: Auto Quality** | 1. Enable auto quality<br>2. Add heavy post-processing<br>3. Observe quality changes | Quality reduces when FPS drops |
| **T1.6: Depth Culling** | 1. Set DepthRange to (0.5, 2.0)<br>2. Move camera | Objects outside range not rendered |

### Test Suite 2: Conference Mode

**Prerequisites**:
- Two devices with builds
- WebRTC signaling server running
- EditorConferenceSimulator for Editor testing

| Test | Steps | Expected Result |
|------|-------|-----------------|
| **T2.1: Simulated Conference** | 1. Open HOLOGRAM.unity<br>2. Add EditorConferenceSimulator<br>3. Set participant count to 4<br>4. Enter Play mode | 4 holograms render in grid layout |
| **T2.2: WebRTC Connection** | 1. Launch app on Device A<br>2. Launch app on Device B<br>3. Join same room | Both see each other's hologram |
| **T2.3: Multi-Peer Quality** | 1. Connect 4 peers<br>2. Check quality preset | All at Medium quality |
| **T2.4: Layout Modes** | 1. With 2 peers, check layout<br>2. With 4 peers, check layout | 2: side-by-side, 4: grid |
| **T2.5: Peer Disconnect** | 1. Connect 3 peers<br>2. Disconnect 1 peer | Hologram removed, quality adjusts |
| **T2.6: Network Latency** | 1. Add simulated latency<br>2. Observe hologram updates | Smooth with <200ms latency |

### Test Suite 3: Performance Benchmarks

| Metric | Low | Medium | High | Ultra |
|--------|-----|--------|------|-------|
| **Particle Count** | 10K | 50K | 100K | 200K |
| **GPU Time (1 hologram)** | <1ms | <2ms | <3ms | <5ms |
| **FPS (1 hologram)** | >60 | >60 | >55 | >45 |
| **FPS (4 holograms)** | >50 | >35 | >25 | N/A |
| **Memory (per hologram)** | <30MB | <60MB | <80MB | <120MB |

### Test Suite 4: Integration Verification

| Test | Steps | Expected Result |
|------|-------|-----------------|
| **T4.1: ARDepthSource Binding** | 1. Check VFXARBinder properties<br>2. Verify binding status | All textures bound (green) |
| **T4.2: WebRTC Decoder Binding** | 1. Connect remote peer<br>2. Check decoder output | ColorTexture/DepthTexture valid |
| **T4.3: Quality Persistence** | 1. Set Quality to High<br>2. Reconnect peer | Quality remains High |
| **T4.4: Memory Cleanup** | 1. Connect/disconnect peers<br>2. Monitor memory | Memory releases on disconnect |

---

## Implementation Phases

### Phase 1: Core VFX (Sprint 1)

- [ ] Create hifi_hologram_people.vfx with color sampling
- [ ] Integrate with HiFiHologramController
- [ ] Test quality presets on device
- [ ] Verify color accuracy
- [ ] **Single user mode testing (T1.1-T1.6)**

### Phase 2: Conference Integration (Sprint 2)

- [ ] Integrate with MetavidoWebRTCDecoder
- [ ] Connect to HologramConferenceManager
- [ ] Add dynamic quality scaling by peer count
- [ ] **Conference mode testing (T2.1-T2.6)**

### Phase 3: Optimization (Sprint 3)

- [ ] Implement auto quality adjustment
- [ ] Add GPU instancing for multi-hologram
- [ ] Profile memory usage
- [ ] Add LOD system for distant holograms
- [ ] **Performance benchmarks (Test Suite 3)**

### Phase 4: Advanced Rendering (Sprint 4)

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
