# MetavidoVFX Pipeline Architecture

> ⚠️ **SUPERSEDED**: See **[VFX_PIPELINE_FINAL_RECOMMENDATION.md](VFX_PIPELINE_FINAL_RECOMMENDATION.md)** for the latest Hybrid Bridge architecture.
> This document is kept for historical reference. The new recommendation reduces code by 84% and uses O(1) compute scaling.

> Last Updated: 2026-01-16 (Superseded by VFX_PIPELINE_FINAL_RECOMMENDATION.md)

## Overview

MetavidoVFX uses a multi-pipeline architecture for binding AR, audio, and tracking data to VFX Graph effects. This document describes all pipelines, their status, and recommended usage.

---

## CRITICAL: Live AR vs Encoded Streams

### Our Adaptation vs Original Projects

| Project | Original Data Source | Our Adaptation |
|---------|---------------------|----------------|
| **Rcam4** | NDI network stream (remote device → PC) | **Live AR Foundation** (local device) |
| **MetavidoVFX** | Encoded .metavido video files | **Live AR Foundation** (local device) |

**Key Insight**: Our pipelines extract ALL data needed to drive VFX from the **live AR Foundation camera** rather than:
- ❌ Remote encoded & decoded NDI video feed (Rcam4 approach)
- ❌ Pre-recorded Metavido encoded videos (MetavidoVFX original approach)

### Why This Matters

| Factor | Live AR | NDI Stream | Encoded Video |
|--------|---------|------------|---------------|
| **Latency** | ~16ms (1 frame) | ~50-100ms | ~30-50ms |
| **CPU Overhead** | Minimal | NDI decode | Video decode |
| **Memory** | AR textures only | +Network buffers | +Video buffers |
| **Bandwidth** | None | ~100Mbps+ | File I/O |
| **Mobile Friendly** | ⭐⭐⭐⭐⭐ | ⭐ | ⭐⭐⭐ |

### Multi-Hologram Scalability

**VFXBinderManager is optimal for multi-hologram rendering:**
- Single compute dispatch for ALL VFX (O(1) compute)
- Same PositionMap shared by all holograms
- Each additional hologram is just binding cost (~0.3ms)

| Hologram Count | VFXBinderManager | Both Pipelines Active |
|----------------|------------------|----------------------|
| 1 | ~2ms GPU | ~4ms GPU (redundant) |
| 5 | ~3.5ms GPU | ~5.5ms GPU |
| 10 | ~5ms GPU | ~7ms GPU |
| 20 | ~8ms GPU | ~10ms GPU |

### Recommendation

**Use VFXBinderManager as the single compute source**. If H3M anchor/scale features are needed, extend VFXBinderManager's PositionMap rather than running duplicate compute.

**Sources**: [AR Foundation 6.1 Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/changelog/CHANGELOG.html), [keijiro/Rcam2](https://github.com/keijiro/Rcam2), [keijiro/Metavido](https://github.com/keijiro/Metavido), [Apple Metal Docs](https://developer.apple.com/documentation/metal/compute_passes/calculating_threadgroup_and_grid_sizes)

---

## Pipeline Hierarchy

```
AR Sensors (ARKit)
       │
       ├─────────────────────────────────────────────────────────────────┐
       │                                                                 │
       ▼                                                                 ▼
VFXBinderManager (PRIMARY)                                    HologramSource (H3M)
  │ Computes: PositionMap                                       │ Computes: PositionMap
  │ Binds: DepthMap, StencilMap, ColorMap                       │ Provides: AnchorPos, Scale
  │ Binds: InverseView, RayParams, DepthRange                   │
  │                                                              ▼
  │                                                     HologramRenderer
  │                                                       │ Binds to single VFX
  │                                                       │
  ├──────────────────────────────────────────────────────┤
  │                                                       │
  ▼                                                       ▼
ALL VFX in Scene                                    Hologram.vfx
  │
  ├── Hand VFX ◄── HandVFXController (position, velocity, pinch)
  ├── Audio VFX ◄── EnhancedAudioProcessor (bass, mid, treble)
  ├── Mesh VFX ◄── MeshVFX (GraphicsBuffer vertices)
  └── Wave VFX ◄── SoundWaveEmitter (expanding waves)
```

---

## Detailed Pipeline Flow

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                         AR FOUNDATION (iOS/ARKit)                              ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║   iPhone LiDAR Sensor              iPhone Camera                               ║
║         │                               │                                      ║
║         ▼                               ▼                                      ║
║   ┌─────────────┐                ┌─────────────┐                              ║
║   │ Depth Frame │                │ Video Frame │                              ║
║   │   256x192   │                │  1920x1440  │                              ║
║   │ (landscape) │                │ (landscape) │                              ║
║   └──────┬──────┘                └──────┬──────┘                              ║
║          │                              │                                      ║
║          ▼                              ▼                                      ║
║   ┌─────────────────────────────────────────────────────────────┐             ║
║   │                    ARKit Processing                          │             ║
║   │  • Rotates frames based on device orientation                │             ║
║   │  • Downsamples depth to match AR session config              │             ║
║   │  • Generates human segmentation stencil                      │             ║
║   └─────────────────────────────────────────────────────────────┘             ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║                         AR FOUNDATION MANAGERS (Unity)                         ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  ┌────────────────────────┐      ┌────────────────────────┐                   ║
║  │   AROcclusionManager   │      │    ARCameraManager     │                   ║
║  ├────────────────────────┤      ├────────────────────────┤                   ║
║  │ environmentDepthTexture│      │ Camera.main            │                   ║
║  │   └─► 84x63 (portrait) │      │   • fieldOfView: 58.7° │                   ║
║  │       Format: RFloat   │      │   • aspect: 0.669      │                   ║
║  │                        │      │   • projectionMatrix   │                   ║
║  │ humanStencilTexture    │      │   • transform          │                   ║
║  │   └─► 84x63 (portrait) │      │                        │                   ║
║  │       Format: R8       │      └────────────────────────┘                   ║
║  └────────────┬───────────┘                                                   ║
║               │                                                                ║
╚═══════════════╪════════════════════════════════════════════════════════════════╝
                │
                ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║                        VFXBinderManager.Update()                               ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  ┌─────────────────────────────────────────────────────────────────────────┐  ║
║  │ STEP 1: Capture & Rotate Textures                                        │  ║
║  ├─────────────────────────────────────────────────────────────────────────┤  ║
║  │                                                                          │  ║
║  │  Raw Depth (84x63)  ──► RotateUV90CW.shader ──► Rotated Depth (63x84)   │  ║
║  │  Raw Stencil (84x63) ──► RotateUV90CW.shader ──► Rotated Stencil (63x84)│  ║
║  │                                                                          │  ║
║  │  Shader UV Transform: float2(1.0 - v.uv.y, v.uv.x)                       │  ║
║  │                                                                          │  ║
║  └─────────────────────────────────────────────────────────────────────────┘  ║
║                                      │                                         ║
║                                      ▼                                         ║
║  ┌─────────────────────────────────────────────────────────────────────────┐  ║
║  │ STEP 2: Compute Camera Parameters                                        │  ║
║  ├─────────────────────────────────────────────────────────────────────────┤  ║
║  │                                                                          │  ║
║  │  InverseView = TRS(camera.position, camera.rotation, Vector3.one)        │  ║
║  │                                                                          │  ║
║  │  RayParams (with rotation fix):                                          │  ║
║  │    tanV = tan(fieldOfView/2)           // ~0.577                         │  ║
║  │    tanH = tanV * aspect                // ~0.386 (portrait)              │  ║
║  │    _rayParams = (centerX, centerY, -tanH, tanV)  // Note: -tanH          │  ║
║  │                                                                          │  ║
║  └─────────────────────────────────────────────────────────────────────────┘  ║
║                                      │                                         ║
║                                      ▼                                         ║
║  ┌─────────────────────────────────────────────────────────────────────────┐  ║
║  │ STEP 3: Compute PositionMap (GPU - DepthToWorld.compute)                 │  ║
║  ├─────────────────────────────────────────────────────────────────────────┤  ║
║  │                                                                          │  ║
║  │  Input:  Rotated Depth (63x84), InvVP matrix                             │  ║
║  │  Output: PositionMap (63x84, ARGBFloat, world positions)                 │  ║
║  │                                                                          │  ║
║  │  [numthreads(32,32,1)]                                                   │  ║
║  │  void DepthToWorld(uint3 id) {                                           │  ║
║  │      float2 uv = (id.xy + 0.5) / float2(width, height);                  │  ║
║  │      float depth = _Depth.SampleLevel(sampler, uv, 0);                   │  ║
║  │      float4 clipPos = float4(uv * 2 - 1, depth, 1);                      │  ║
║  │      float4 worldPos = mul(_InvVP, clipPos);                             │  ║
║  │      _PositionRT[id.xy] = worldPos / worldPos.w;                         │  ║
║  │  }                                                                       │  ║
║  │                                                                          │  ║
║  └─────────────────────────────────────────────────────────────────────────┘  ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║                         VFXBinderManager.BindVFX()                             ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  For each VisualEffect in scene:                                              ║
║                                                                                ║
║    vfx.SetTexture("DepthMap", rotatedDepthRT)         ← 63x84 rotated         ║
║    vfx.SetTexture("StencilMap", rotatedStencilRT)     ← 63x84 rotated         ║
║    vfx.SetTexture("ColorMap", colorRT)                ← 1284x1920             ║
║    vfx.SetTexture("PositionMap", positionMapRT)       ← 63x84 world pos       ║
║    vfx.SetMatrix4x4("InverseView", inverseViewMatrix)                         ║
║    vfx.SetVector4("RayParams", rayParams)             ← (0, 0, -tanH, tanV)   ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
                                      │
                                      ▼
╔═══════════════════════════════════════════════════════════════════════════════╗
║               VFX Graph (e.g., pointcloud_depth_people_metavido.vfx)           ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                                ║
║  INITIALIZE PARTICLE:                                                         ║
║  ┌────────────────────────────────────────────────────────────────────────┐   ║
║  │                                                                         │   ║
║  │  particleIndex → UV → Metavido Sample UV Subgraph                       │   ║
║  │                              │                                          │   ║
║  │                              ▼                                          │   ║
║  │  ┌───────────────────────────────────────────────────────────────────┐ │   ║
║  │  │ Metavido Sample UV (Keijiro's subgraph)                            │ │   ║
║  │  │                                                                    │ │   ║
║  │  │  1. Sample depth = tex2D(DepthMap, uv)                             │ │   ║
║  │  │                                                                    │ │   ║
║  │  │  2. UV → Ray Direction (using RayParams):                          │ │   ║
║  │  │     rayDir.x = (uv.x - 0.5) * RayParams.z * 2   // -tanH           │ │   ║
║  │  │     rayDir.y = (uv.y - 0.5) * RayParams.w * 2   // tanV            │ │   ║
║  │  │     rayDir.z = 1.0                                                 │ │   ║
║  │  │                                                                    │ │   ║
║  │  │  3. cameraSpacePos = normalize(rayDir) * depth                     │ │   ║
║  │  │                                                                    │ │   ║
║  │  │  4. worldPos = InverseView * cameraSpacePos                        │ │   ║
║  │  │                                                                    │ │   ║
║  │  └───────────────────────────────────────────────────────────────────┘ │   ║
║  │                              │                                          │   ║
║  │                              ▼                                          │   ║
║  │  particle.position = worldPos                                           │   ║
║  │  particle.color = tex2D(ColorMap, uv)                                   │   ║
║  │                                                                         │   ║
║  └────────────────────────────────────────────────────────────────────────┘   ║
║                                                                                ║
║  OUTPUT: Render particles at computed world positions                          ║
║                                                                                ║
╚════════════════════════════════════════════════════════════════════════════════╝
```

---

## Depth Rotation Fix (ARKit Orientation)

### Problem
ARKit depth textures have a different orientation than what the Metavido VFX subgraph expects. Without correction, the point cloud appears rotated 90° (head pointing left) and horizontally mirrored.

### Solution
Two-part fix implemented in VFXBinderManager:

**1. Texture Rotation** (`RotateUV90CW.shader`)
```hlsl
// Rotate UV 90 degrees CW
o.uv = float2(1.0 - v.uv.y, v.uv.x);
```
- Depth texture: 84x63 → 63x84 (dimensions swap)
- Stencil texture: Same rotation applied

**2. RayParams Horizontal Flip**
```csharp
// Negate tanH to fix horizontal mirror
_rayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV);
```

### Configuration
In VFXBinderManager Inspector:
- `Rotate Depth Texture`: ✅ Enabled (default)

### Files Modified
| File | Change |
|------|--------|
| `Assets/Scripts/VFX/VFXBinderManager.cs` | Added rotation blit + RayParams flip |
| `Assets/Shaders/RotateUV90CW.shader` | New shader for UV rotation |

---

## Primary Pipeline: VFXBinderManager

**File**: `Assets/Scripts/VFX/VFXBinderManager.cs`
**Status**: ✅ PRIMARY - Use for all AR data binding

### What It Does
- Auto-finds all `VisualEffect` components in scene
- Computes `PositionMap` via GPU (DepthToWorld.compute)
- Binds AR textures (depth, stencil, color) to all VFX
- Binds camera matrices and ray parameters
- Delegates audio binding to `EnhancedAudioProcessor`

### Data Bound
| Property | Type | Source |
|----------|------|--------|
| `DepthMap` | Texture2D | AROcclusionManager |
| `StencilMap` | Texture2D | AROcclusionManager |
| `ColorMap` | RenderTexture | ARCameraBackground blit |
| `PositionMap` | RenderTexture | DepthToWorld.compute |
| `InverseView` | Matrix4x4 | Camera.cameraToWorldMatrix |
| `InverseProjection` | Matrix4x4 | Camera.projectionMatrix.inverse |
| `RayParams` | Vector4 | (0, 0, tan(fov/2)*aspect, tan(fov/2)) |
| `DepthRange` | Vector2 | (0.1, 10.0) |

### When to Use
- All scene VFX that need AR data
- Default for any new VFX development
- Replacement for per-VFX binders

---

## H3M Pipeline: HologramSource + HologramRenderer

**Files**:
- `Assets/H3M/Core/HologramSource.cs`
- `Assets/H3M/Core/HologramRenderer.cs`

**Status**: ✅ SPECIALIZED - H3M hologram system

### What It Does
- `HologramSource`: AR data acquisition + compute dispatch
- `HologramRenderer`: Binds to single hologram VFX
- Supports anchor positioning ("Man in the Mirror")
- Supports hologram scaling (mini-me effect)

### Additional Data Bound
| Property | Type | Source |
|----------|------|--------|
| `AnchorPos` | Vector3 | AR surface placement |
| `HologramScale` | float | Size multiplier |

### When to Use
- H3M hologram rendering
- When you need anchor-relative positioning
- When you need scale transformation

---

## Specialized Pipelines

### HandVFXController
**File**: `Assets/Scripts/HandTracking/HandVFXController.cs`
**Status**: ✅ SPECIALIZED - Only source of hand data

| Property | Type | Description |
|----------|------|-------------|
| `HandPosition` | Vector3 | Wrist world position |
| `HandVelocity` | Vector3 | Velocity vector |
| `HandSpeed` | float | Velocity magnitude |
| `BrushWidth` | float | Pinch-modulated width |
| `IsPinching` | bool | Pinch gesture active |
| `TrailLength` | float | Speed-based trail |

**Events**: `OnPinchStart`, `OnPinchEnd`

---

### EnhancedAudioProcessor
**File**: `Assets/Scripts/Audio/EnhancedAudioProcessor.cs`
**Status**: ✅ SPECIALIZED - FFT audio analysis

| Property | Type | Range |
|----------|------|-------|
| `AudioVolume` | float | 0-1 |
| `AudioPitch` | float | 0-1 |
| `AudioBass` | float | 0-1 (0-250Hz) |
| `AudioMid` | float | 0-1 (250-2000Hz) |
| `AudioTreble` | float | 0-1 (2000-20000Hz) |
| `AudioSubBass` | float | 0-1 (0-60Hz) |

---

### MeshVFX
**File**: `Assets/Echovision/Scripts/MeshVFX.cs`
**Status**: ✅ SPECIALIZED - AR mesh to VFX

| Property | Type | Description |
|----------|------|-------------|
| `MeshPointCache` | GraphicsBuffer | Vertex positions |
| `MeshNormalCache` | GraphicsBuffer | Vertex normals |
| `MeshPointCount` | int | Active vertex count |
| `MeshTransform_position` | Vector3 | Mesh position |
| `MeshTransform_angles` | Vector3 | Mesh rotation |
| `MeshTransform_scale` | Vector3 | Mesh scale |

---

### SoundWaveEmitter
**File**: `Assets/Echovision/Scripts/SoundWaveEmitter.cs`
**Status**: ✅ SPECIALIZED - Expanding sound waves

| Property | Type | Description |
|----------|------|-------------|
| `WaveOrigin` | Vector3 | Emission point |
| `WaveDirection` | Vector3 | Travel direction |
| `WaveRange` | float | Current radius |
| `WaveAngle` | float | Cone angle (90-180) |
| `WaveAge` | float | Age percentage (0-1) |

---

## Legacy Pipelines (Deprecated)

### VFXARDataBinder
**File**: `Assets/Scripts/VFX/Binders/VFXARDataBinder.cs`
**Status**: ⚠️ LEGACY - Use only for runtime-spawned VFX

Use `VFXBinderManager` instead for scene VFX. Keep this only for runtime VFX that need individual binding.

### ARKitMetavidoBinder
**File**: `Assets/Scripts/ARKitMetavidoBinder.cs`
**Status**: ⚠️ LEGACY - Superseded by VFXBinderManager

Per-VFX binding approach. Replaced by centralized VFXBinderManager.

---

## Disabled Pipelines (Remove)

### PeopleOcclusionVFXManager
**File**: `Assets/Scripts/PeopleOcclusion/PeopleOcclusionVFXManager.cs`
**Status**: ❌ DISABLED - Redundant

Creates VFX at runtime, binds per-VFX. Anti-pattern replaced by VFXBinderManager.

### VelocityVFXBinder
**File**: `Assets/Scripts/PeopleOcclusion/VelocityVFXBinder.cs`
**Status**: ❌ DEPRECATED - Performance issue

Reads velocity from RenderTexture (GPU→CPU stall every frame). Not used.

---

## Compute Shaders

### DepthToWorld.compute (PRIMARY)
**Location**: `Assets/Resources/DepthToWorld.compute`

| Kernel | Input | Output | Thread Groups |
|--------|-------|--------|---------------|
| `DepthToWorld` | Depth + Stencil + Matrices | PositionMap (ARGBFloat) | 32×32×1 |

**Parameters**:
- `_InvVP`: Inverse view-projection matrix
- `_ProjectionMatrix`: Camera projection
- `_DepthRange`: Near/far clipping
- `_Depth`: Input depth texture
- `_Stencil`: Optional human stencil
- `_UseStencil`: Enable stencil masking

---

## Usage Recommendations

### New VFX Development
```csharp
// Scene setup (once):
// 1. Add VFXBinderManager to AR Session Origin
// 2. Add HandVFXController (if hand effects needed)
// 3. Add EnhancedAudioProcessor (if audio effects needed)

// Your VFX just needs to expose properties:
// - DepthMap, PositionMap, RayParams (for AR depth)
// - HandPosition, HandVelocity (for hand tracking)
// - AudioBass, AudioMid (for audio reactivity)
```

### Runtime-Spawned VFX
```csharp
// Use VFXBinderUtility for auto-setup:
VFXBinderUtility.SetupVFXAuto(mySpawnedVFX);

// Or specify preset:
VFXBinderUtility.SetupVFX(myVFX, VFXBinderPreset.ARWithAudio);
```

### H3M Hologram
```csharp
// Use HologramSource + HologramRenderer
// Supports anchor placement and scaling
// Binds: PositionMap, ColorMap, AnchorPos, HologramScale
```

---

## Performance Budget

| Component | Time @ 60fps | Notes |
|-----------|--------------|-------|
| VFXBinderManager | 5ms | Compute + all bindings |
| HandVFXController | 0.3ms | Hand tracking |
| EnhancedAudioProcessor | 1ms | FFT analysis |
| MeshVFX | 2ms | Buffer updates |
| VFX Rendering | 6ms | Particle systems |
| **Headroom** | **2.4ms** | Safety margin |

---

## Migration Guide

### From ARKitMetavidoBinder to VFXBinderManager
1. Remove ARKitMetavidoBinder components from VFX GameObjects
2. Ensure VFXBinderManager exists in scene (auto-finds all VFX)
3. VFX properties are bound automatically

### From PeopleOcclusionVFXManager
1. Disable/remove PeopleOcclusionVFXManager
2. Place VFX in scene manually
3. VFXBinderManager handles all binding

---

## Future Development

### Planned: VFXMetavidoBinder
For Spec 003 (Hologram Recording/Playback):
- Decode Metavido video frames
- Extract ColorTexture, DepthTexture, CameraPose
- Bind to VFX Graph for recorded hologram playback
- Same properties as live AR, different source

### Planned: WebRTC Integration
For Spec 003 (Multiplayer):
- Receive Metavido video via WebRTC
- Decode → VFXMetavidoBinder → VFX
- 2-6 simultaneous remote holograms

---

## Future Architecture: Segmented Hologram Pipeline

See `KnowledgeBase/_COMPREHENSIVE_HOLOGRAM_PIPELINE_ARCHITECTURE.md` for the full architecture covering:

### Segmentation Layers
| Segment | Data Source | VFX Properties |
|---------|-------------|----------------|
| **Body** | ARKit Stencil + BodyPixSentis | BodyPositionMap, 24 part masks |
| **Hands** | XR Hands / HoloKit (21 joints) | HandPosition, Velocity, Gestures |
| **Face** | ARKit Face (52 blendshapes) | FaceVertices, BlendShapes, Gaze |
| **Environment** | ARMeshManager | MeshVertices, MeshNormals |

### Driver Sources
| Driver | Data Source | VFX Properties |
|--------|-------------|----------------|
| **Audio** | EnhancedAudioProcessor | Bass, Mid, Treble, BeatDetected |
| **Velocity** | Frame delta compute | VelocityMap, HandVelocity |
| **Proximity** | AR Anchor distances | UserProximity, AnchorProximity |
| **Voice** | Speech Recognition | VoiceCommandIndex, Confidence |
| **Classification** | ML models | GestureType, MotionType |

### Multi-User Capacity (SFU Architecture)
| Users | Decode Time | Target FPS |
|-------|-------------|------------|
| 1-2 | 0-2ms | 60fps |
| 4 | 4ms | 50fps |
| 6 | 6ms | 46fps |
| 8 | 8ms | 42fps |

**Recommendation**: 4-6 simultaneous remote holograms for optimal balance.
