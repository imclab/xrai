# HOLOGRAM.unity Scene Context

**Last Updated**: 2026-01-21
**Purpose**: Cross-AI-tool persistent scene knowledge

---

## Critical Scene Objects

### AR Pipeline (Hybrid Bridge Pattern)

| Object | ID | Path | Components |
|--------|-----|------|------------|
| ARDepthSource | 129478 | /ARDepthSource | Transform, ARDepthSource |
| AR Camera | 129600 | HoloKit Camera Rig/Camera Offset/AR Camera | Camera, ARCameraManager, AROcclusionManager, ARCameraTextureProvider |
| AudioBridge | 129500 | /AudioBridge | AudioBridge, AudioSource |
| VFXModeController | 129550 | /VFXModeController | VFXModeController |

**ARDepthSource** is the singleton compute source. ONE dispatch for all VFX.
- Provides: DepthMap, StencilMap, PositionMap, VelocityMap, ColorMap, RayParams

**ARCameraTextureProvider** converts iOS YCbCr to RGB for ColorMap.

**AudioBridge** provides 4-band FFT + beat detection (Spec 007).

**VFXModeController** manages runtime mode switching (People/Env/Audio/etc).

### Hologram System

| Object | ID | Path | Components |
|--------|-----|------|------------|
| Hologram | 128402 | /Hologram | HologramPlacer, HologramController, VideoPlayer, MetadataDecoder, TextureDemuxer |
| HologramVFX | 129162 | Hologram/HologramVFX | VisualEffect, VFXARBinder, VFXCategory |

**HologramVFX** scale: 0.15
**VFXARBinder** bindings enabled: ColorMap, PositionMap, DepthMap, RayParams

---

## Key Scripts

| Script | Path | Purpose |
|--------|------|---------|
| ARDepthSource.cs | Assets/Scripts/Bridges/ | Singleton, ONE compute dispatch for all VFX |
| VFXARBinder.cs | Assets/Scripts/Bridges/ | Per-VFX lightweight binding (O(N) SetTexture) |
| AudioBridge.cs | Assets/Scripts/Bridges/ | Audio analysis & beat detection |
| VFXModeController.cs | Assets/Scripts/VFX/ | Runtime mode switching logic |
| HiFiHologramVFX.hlsl | Assets/Shaders/ | HLSL sampling for PositionMap + ColorMap |
| RealisticHologramSetup.cs | Assets/Scripts/Editor/ | Menu: H3M > Hologram > Setup Realistic RGB Hologram |
| HiFiHologramVFXCreator.cs | Assets/Scripts/Editor/ | Menu: H3M > HiFi Hologram |

---

## Architecture Pattern

```
AR Foundation → ARDepthSource (singleton) → VFXARBinder (per-VFX) → VFX
                      ↓                           ↓
           ONE compute dispatch          SetTexture() calls only
                      ↓
           PositionMap, VelocityMap (shared)
```

**Key Insight**: PositionMap is precomputed by ARDepthSource GPU compute shader. VFX reads directly - no redundant depth→position computation in HLSL.

**Performance**: 353 FPS verified with 10 active VFX.

---

## VFX Property Names

| Property | Type | Description |
|----------|------|-------------|
| DepthMap | Texture2D | AR depth texture |
| StencilMap | Texture2D | Human body mask |
| PositionMap | Texture2D | World positions (GPU-computed) |
| ColorMap | Texture2D | Camera RGB texture |
| VelocityMap | Texture2D | Motion vectors |
| RayParams | Vector4 | (0, 0, tan(fov/2)*aspect, tan(fov/2)) |
| InverseView | Matrix4x4 | Inverse view matrix |
| MapWidth | float | Texture width |
| MapHeight | float | Texture height |
| Dimensions | Vector2 | (width, height) for Rcam4-style VFX |
| AudioVolume | float | 0-1 RMS volume |
| AudioBass | float | 0-1 bass band |
| BeatPulse | float | 0->1->0 beat impulse |
| HandPosition | Vector3 | Wrist position |
| HandVelocity | Vector3 | Hand velocity |

---

## Menu Commands

- `H3M > VFX Pipeline Master > Setup Complete Pipeline` - Full pipeline setup
- `H3M > Hologram > Setup Realistic RGB Hologram` - One-click RGB hologram
- `H3M > HiFi Hologram > Create Optimized HiFi Hologram VFX` - Create optimized VFX
- `H3M > HiFi Hologram > Add to Hologram Prefab` - Configure HologramVFX

---

## HiFi Hologram Testing & Debugging (Quick Run)

1. **Create optimized VFX**: `H3M > HiFi Hologram > Create Optimized HiFi Hologram VFX`
2. **Assign to Hologram**: `H3M > HiFi Hologram > Add to Hologram Prefab`
3. **Validate bindings**: `H3M > VFX Debug > Validate All VFX Bindings`
4. **Inspect runtime props**: press **F1** (VFXPropertyInspector)
5. **AR Remote test**: use AR Companion (ColorMap requires live camera)

Expected bindings on `Hologram/HologramVFX`:
- PositionMap ✅
- ColorMap ✅
- DepthMap ✅
- RayParams ✅
- InverseView ✅

If **cropped**:
- Check `Dimensions` binding (Vector2) or `MapWidth/MapHeight`
- Confirm PositionMap resolution matches depth texture

---

## For AI Assistants

1. **ARDepthSource** is the singleton - never create duplicates
2. **VFXARBinder** is per-VFX - one binder per VisualEffect
3. **PositionMap** is precomputed - don't use HLSL to compute from DepthMap (redundant)
4. **ColorMap** requires ARCameraTextureProvider for iOS
5. **AudioBridge** is the single source of truth for audio data
6. Scene path: `Assets/HOLOGRAM.unity`
