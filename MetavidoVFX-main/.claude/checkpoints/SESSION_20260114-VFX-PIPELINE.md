# Session Checkpoint: VFX Pipeline Cleanup & Binding Fixes

**Date**: 2026-01-14
**Scene**: HOLOGRAM_Mirror_MVP.unity
**Focus**: VFX data pipeline architecture, particle visibility fixes

---

## Completed Tasks

### 1. VFX Pipeline Cleanup
- **Disabled** `PeopleOcclusionVFXManager` (redundant - creates own VFX at runtime)
- **Removed** 5 `ARKitMetavidoBinder` components (redundant per-VFX binding)
- **Created** `Assets/Scripts/Editor/VFXPipelineCleanup.cs` - Editor menu for cleanup operations
- **Menu**: `H3M > Pipeline Cleanup > Run Full Cleanup`

### 2. VFXBinderManager - Critical Fixes
**File**: `Assets/Scripts/VFX/VFXBinderManager.cs`

Added missing bindings that were causing particles to be invisible:

| Property | Type | Purpose |
|----------|------|---------|
| `RayParams` | Vector4 | `(0, 0, tan(fov/2)*aspect, tan(fov/2))` - Required for UV+depth to 3D conversion |
| `ProjectionVector` | Vector4 | Alternate name for RayParams (Rcam2 VFX) |
| `InverseProjection` | Matrix4x4 | Inverse projection matrix (Rcam4 VFX) |
| `PositionMap` | Texture2D | GPU-computed world positions via DepthToWorld.compute |
| `legacyAudioProcessor` | AudioProcessor | Fallback for EchoVision compatibility |

**Key code additions**:
```csharp
// RayParams calculation (in UpdateCachedData)
float fovV = arCamera.fieldOfView * Mathf.Deg2Rad;
float tanV = Mathf.Tan(fovV * 0.5f);
float tanH = tanV * arCamera.aspect;
_rayParams = new Vector4(0f, 0f, tanH, tanV);

// Binding (in BindVFX)
if (vfx.HasVector4("RayParams"))
    vfx.SetVector4("RayParams", _rayParams);
if (vfx.HasVector4("ProjectionVector"))
    vfx.SetVector4("ProjectionVector", _rayParams);
```

### 3. Audio System Compatibility
- VFXBinderManager now supports both `EnhancedAudioProcessor` AND legacy `AudioProcessor`
- Updated `EchovisionSetup.cs` to add EnhancedAudioProcessor alongside legacy
- Validation now checks for both audio processors

### 4. VFX Categorization (Documented)
**File**: `Assets/Documentation/QUICK_REFERENCE.md`

| Category | Path | Required Inputs |
|----------|------|-----------------|
| **Body/People** | `VFX/Metavido/`, `VFX/Rcam*/Body/`, `H3M/VFX/` | DepthMap, ColorMap, InverseView, RayParams |
| **PositionMap** | `VFX/Akvfx/`, `VFX/HumanEffects/` | PositionMap, ColorMap |
| **Environment** | `VFX/Environment/`, `VFX/SdfVfx/` | Spawn, Throttle only |
| **Mesh/Audio** | `Echovision/VFX/` | MeshPointCache, Wave*, HumanStencilTexture |

---

## Current Scene State

### Data Sources (from console)
```
OcclusionMgr=True, CameraMgr=True, CameraBG=True, Camera=True, Audio=Legacy, Hand=True
```

### Components Present
- [x] ARSession, ARCameraManager, AROcclusionManager
- [x] ARMeshManager (MeshManager)
- [x] TrackedPoseDriver (AR Camera)
- [x] AudioProcessor (legacy) on AudioInput
- [x] SoundWaveEmitter
- [x] DepthImageProcessor
- [x] MeshVFX
- [x] VFXBinderManager (on HoloKit Camera Rig)
- [x] HandVFXSystem with LeftHandVFX/RightHandVFX
- [x] 15 VFX in SpawnControlVFX_Container
- [x] Global Volume (post-processing)

### Pending
- [ ] EnhancedAudioProcessor (run `H3M > EchoVision > Setup Audio Input` after recompile)

---

## Files Modified This Session

1. `Assets/Scripts/VFX/VFXBinderManager.cs` - Major fixes (RayParams, PositionMap, audio fallback)
2. `Assets/Scripts/Editor/VFXPipelineCleanup.cs` - NEW FILE
3. `Assets/Scripts/Editor/EchovisionSetup.cs` - Enhanced audio setup
4. `Assets/Documentation/QUICK_REFERENCE.md` - VFX categories added
5. `Assets/Documentation/README.md` - Pipeline architecture diagram

---

## Next Steps

1. **Wait for Unity to recompile** scripts
2. **Run**: `H3M > EchoVision > Setup Audio Input` - adds EnhancedAudioProcessor
3. **Run**: `H3M > EchoVision > Validate Setup` - verify all components
4. **Test particles** - should now be visible with RayParams binding
5. **Build to device** - verify on iOS

---

## Key Insights

### Why RayParams is Critical
Metavido VFX use HLSL pattern:
```hlsl
float3 rayDir = float3((uv + RayParams.xy) * RayParams.zw, 1);
float3 worldPos = mul(InverseView, float4(rayDir * depth, 1)).xyz;
```
Without RayParams, particles spawn at incorrect positions (effectively invisible).

### Pipeline Architecture
```
AR Session Origin → VFXBinderManager → All VFX
                          ↓
         DepthMap, ColorMap, StencilMap, RayParams, InverseView
                          ↓
              GPU Compute (DepthToWorld.compute)
                          ↓
                    PositionMap (world XYZ)
```

---

## Commands Reference

```bash
# Cleanup redundant pipelines
H3M > Pipeline Cleanup > Run Full Cleanup

# Setup EchoVision
H3M > EchoVision > Setup All EchoVision Components

# Validate
H3M > EchoVision > Validate Setup

# Build
./build_ios.sh
```
