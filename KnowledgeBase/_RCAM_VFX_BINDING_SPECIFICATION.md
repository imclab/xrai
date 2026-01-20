# Rcam VFX Binding Specification: Rcam2, Rcam3, Rcam4

**Research Date**: 2026-01-20
**Source**: MetavidoVFX VFX assets (73 total), Keijiro's Rcam repositories, VFXARBinder.cs
**Purpose**: Definitive binding reference for MetavidoVFX Rcam-based VFX integration

---

## Executive Summary

MetavidoVFX contains **73 VFX assets** across Rcam2, Rcam3, and Rcam4 categories. This specification documents the **exact property bindings** required for each category, compute shader dependencies, and integration with the **Hybrid Bridge Pipeline** (ARDepthSource + VFXARBinder).

### Key Findings

1. **Property Naming Evolution**: Rcam2 uses `RayParamsMatrix` (Matrix4x4), Rcam3/4 standardized on `InverseProjection` (Vector4) + `InverseView` (Matrix4x4)
2. **Compute Dependency**: ALL Rcam VFX require `PositionMap` (world positions from depth) - MetavidoVFX provides this via ARDepthSource
3. **Stencil Separation**: Body vs Environment VFX distinguished by `StencilMap` binding (People effects need it, Environment effects do NOT)
4. **Velocity Support**: Rcam4 VFX (lightning, trails) require optional `VelocityMap` for motion-reactive effects

---

## VFX Asset Inventory

| Category | Count | Path | Examples |
|----------|-------|------|----------|
| **Rcam2** | 20 | `Assets/Resources/VFX/Rcam2/` | brush, bubble, candy, dot, emoji, eyeball, floor, fragment, glitch, particle, petal (2x), point, ribbon, spark, spike, text, trail, voxel, warp |
| **Rcam3** | 8 | `Assets/Resources/VFX/Rcam3/` | flame, grid, particles, plexus, points, scanlines, sparkles, sweeper |
| **Rcam4** | 14 | `Assets/Resources/VFX/Rcam4/` | balls, bubbles, flame, grid, lightning, petals, pointcloud, rcam4, shards, sparkles, speedlines, spikes, trails, voxels |
| **Total** | 42 | | Rcam-based VFX |

**Note**: MetavidoVFX also includes 31 other VFX (Akvfx, NNCam2, SdfVfx, People, Environment) with different binding requirements.

---

## Property Binding Matrix

### Rcam2 (HDRP → URP Converted)

**Requires HDRP 8.2.0 → URP 17.0.3 property renaming**

| VFX Property | Type | MetavidoVFX Source | Notes |
|--------------|------|-------------------|-------|
| `ColorMap` | Texture2D | `ARDepthSource.ColorMap` | AR camera RGB (demand-driven) |
| `DepthMap` | Texture2D | `ARDepthSource.DepthMap` | Raw depth (RFloat) |
| `RayParamsMatrix` | **Matrix4x4** | **DEPRECATED** | ⚠️ Rcam2 legacy - replace with `RayParams` (Vector4) |
| `RayParams` | Vector4 | `ARDepthSource.RayParams` | `(centerShiftX, centerShiftY, tanH, tanV)` |
| `DepthThreshold` | float | VFX-specific | Environment effects use this for filtering |
| `Size` | float | VFX-specific | Particle size control |
| `Throttle` | float | `VFXARBinder.Throttle` | 0-1 intensity control |

**Compute Dependency**: Rcam2 VFX expect `RayParamsMatrix` (4x4) but MetavidoVFX provides `RayParams` (Vector4). **Solution**: Use VFXARBinder alias resolution (`RayParamsMatrix` → `RayParams`) or modify VFX to use Vector4.

**Critical Finding**: Rcam2 `RayParamsMatrix` is **NOT** the same as `InverseProjection`. It's a **full 4x4 matrix** computed from camera intrinsics, while `InverseProjection` is a **Vector4** with compressed parameters.

### Rcam3 (URP, Standard)

| VFX Property | Type | MetavidoVFX Source | Notes |
|--------------|------|-------------------|-------|
| `ColorMap` | Texture2D | `ARDepthSource.ColorMap` | AR camera RGB |
| `DepthMap` | Texture2D | `ARDepthSource.DepthMap` | Raw depth |
| `RayParams` | Vector4 | `ARDepthSource.RayParams` | UV+depth → world position |
| `InverseProjection` | Vector4 | **COMPUTED** | `(1/fx, 1/fy, cx/fx, cy/fy)` |
| `InverseView` | Matrix4x4 | `ARDepthSource.InverseView` | `TRS(cameraPos, cameraRot, Vector3.one)` |
| `Dimensions` | Vector2 | VFX-specific | Effect spatial bounds |
| `FocusDistance` | float | VFX-specific | Depth-of-field effect |
| `HueShift` | float | VFX-specific | Color variation |
| `Ripple` | float | VFX-specific | Wave animation |
| `Origin` | Vector3 | VFX-specific | Effect spawn point |
| `Throttle` | float | `VFXARBinder.Throttle` | 0-1 intensity |

**Key Difference**: Rcam3 introduces `InverseProjection` (Vector4) as the standard for depth reconstruction, replacing Rcam2's `RayParamsMatrix`.

**Compute Pattern** (Rcam3/4 Standard):
```hlsl
float3 RcamDistanceToWorldPosition(float2 uv, float d, float4 inv_proj, float4x4 inv_view) {
    float3 p = float3((uv - 0.5) * 2, 1);        // UV → NDC
    p.xy = (p.xy * inv_proj.xy) + inv_proj.zw;  // NDC → View
    return mul(inv_view, float4(p * d, 1)).xyz; // View → World
}
```

### Rcam4 (URP, Production)

| VFX Property | Type | MetavidoVFX Source | Notes |
|--------------|------|-------------------|-------|
| `ColorMap` | Texture2D | `ARDepthSource.ColorMap` | AR camera RGB |
| `DepthMap` | Texture2D | `ARDepthSource.DepthMap` | Raw depth |
| `RayParams` | Vector4 | `ARDepthSource.RayParams` | UV+depth → world |
| `InverseProjection` | Vector4 | **COMPUTED** | `(1/fx, 1/fy, cx/fx, cy/fy)` |
| `InverseView` | Matrix4x4 | `ARDepthSource.InverseView` | Camera TRS matrix |
| `Particles` | int | VFX-specific | Max particle count |
| `Strip` | int | VFX-specific | Lightning strip count |
| `Throttle` | float | `VFXARBinder.Throttle` | 0-1 intensity |

**Rcam4-Specific Features**:
- **VelocityMap** (optional): Lightning/trail effects use motion data
- **NDI Metadata**: Original Rcam4 streams XML metadata for camera pose, but MetavidoVFX extracts this locally from `ARCameraManager.frameReceived`

---

## Compute Shader Requirements

### ARDepthSource Pipeline (MetavidoVFX Implementation)

**File**: `Assets/Shaders/DepthToWorld.compute`

```hlsl
#pragma kernel DepthToWorld

Texture2D<float> _Depth;       // Input: AR depth map
RWTexture2D<float4> _PositionRT; // Output: World positions

float4x4 _InvVP;                // Inverse view-projection matrix

[numthreads(32,32,1)]
void DepthToWorld(uint3 id : SV_DispatchThreadID) {
    float depth = _Depth[id.xy];
    if (depth <= 0) return;

    // UV → NDC → World
    float2 uv = (id.xy + 0.5) / float2(_Width, _Height);
    float3 ndc = float3((uv - 0.5) * 2, 1);
    float3 worldPos = mul(_InvVP, float4(ndc * depth, 1)).xyz;

    _PositionRT[id.xy] = float4(worldPos, 1);
}
```

**Dispatch Pattern** (O(1) scaling):
```csharp
// ARDepthSource.cs - SINGLE dispatch for ALL VFX
int groupsX = Mathf.CeilToInt(depth.width / 32f);
int groupsY = Mathf.CeilToInt(depth.height / 32f);
_depthToWorld.Dispatch(_kernel, groupsX, groupsY, 1);
```

**Critical**: Thread group size is `[numthreads(32,32,1)]`, NOT 8x8. Using `ceil(width/8)` causes **thread group mismatch errors**.

### VelocityMap Compute (Optional)

**Kernel**: `CalculateVelocity` (same compute shader)

```hlsl
#pragma kernel CalculateVelocity

RWTexture2D<float4> _PositionRT;          // Current frame
Texture2D<float4> _PreviousPositionRT;    // Previous frame
RWTexture2D<float4> _VelocityRT;          // Output velocity
float _DeltaTime;

[numthreads(32,32,1)]
void CalculateVelocity(uint3 id : SV_DispatchThreadID) {
    float3 pos = _PositionRT[id.xy].xyz;
    float3 prevPos = _PreviousPositionRT[id.xy].xyz;
    float3 velocity = (pos - prevPos) / _DeltaTime;
    _VelocityRT[id.xy] = float4(velocity, 1);
}
```

**Enable in ARDepthSource**: `_enableVelocity = true` (disabled by default - can cause tracking jitter)

---

## VFXARBinder Integration

### Property Alias Resolution (Cross-Project Compatibility)

VFXARBinder supports **automatic alias resolution** for Rcam2, Rcam3, Rcam4, Akvfx, and H3M VFX:

```csharp
// VFXARBinder.cs - Property aliases (lines 119-129)
static readonly string[] DepthMapAliases = { "DepthMap", "Depth", "DepthTexture", "_Depth" };
static readonly string[] StencilMapAliases = { "StencilMap", "Stencil", "HumanStencil", "StencilTexture" };
static readonly string[] PositionMapAliases = { "PositionMap", "Position", "WorldPosition", "WorldPos" };
static readonly string[] ColorMapAliases = { "ColorMap", "Color", "ColorTexture", "CameraColor", "MainTex" };
static readonly string[] RayParamsAliases = { "RayParams", "RayParameters", "CameraParams" };
static readonly string[] InverseViewAliases = { "InverseView", "InvView", "CameraToWorld", "ViewInverse" };
static readonly string[] InverseProjAliases = { "InverseProjection", "RayParamsMatrix", "InvProj", "ProjectionInverse" };
```

**Auto-Detection**:
```csharp
// Right-click VFXARBinder in Inspector → "Auto-Detect Bindings (with Aliases)"
// OR: H3M > VFX Pipeline Master > Testing > Validate All Bindings
```

### Binding Toggle Patterns

| VFX Type | Toggles | Notes |
|----------|---------|-------|
| **Rcam2 (People)** | DepthMap, ColorMap, StencilMap, RayParams | Requires stencil for body separation |
| **Rcam2 (Environment)** | DepthMap, ColorMap, RayParams | NO stencil |
| **Rcam3 (People)** | DepthMap, ColorMap, RayParams, InverseView | Uses InverseView for world-space particles |
| **Rcam3 (Any)** | DepthMap, ColorMap, RayParams | Grid/sweeper don't care about body/env |
| **Rcam4 (People)** | DepthMap, ColorMap, RayParams, InverseView | Production-ready URP effects |
| **Rcam4 (Environment)** | DepthMap, ColorMap, RayParams | Speedlines, grid, shards |

**Example Configuration** (Rcam3 `plexus_depth_people_rcam3.vfx`):
```csharp
_bindDepthMap = true;
_bindColorMap = true;
_bindRayParams = true;
_bindInverseView = true;
_bindStencilMap = false;  // Plexus uses depth only, not stencil
_bindPositionMap = true;  // Computed by ARDepthSource
```

---

## Body vs Environment Separation

### Stencil-Based Filtering

**People VFX** (Rcam2/3/4 `*_depth_people_*`):
- Bind `StencilMap` from `AROcclusionManager.humanStencilTexture`
- VFX samples stencil in Initialize/Update contexts to spawn particles only on humans

**Environment VFX** (Rcam2/3/4 `*_environment_*`):
- Do NOT bind `StencilMap` (or bind `Texture2D.whiteTexture` as dummy)
- VFX spawns particles on all depth data

**"Any" VFX** (Rcam3 `*_any_*`):
- Grid, sweeper effects work on full scene
- Stencil binding optional (ignored by VFX logic)

### Stencil Access Pattern (AR Foundation)

```csharp
// ARDepthSource.cs - Safe texture access
Texture TryGetTexture(System.Func<Texture> getter) {
    try { return getter?.Invoke(); }
    catch { return null; }
}

Texture stencil = TryGetTexture(() => _occlusion.humanStencilTexture);
StencilMap = stencil ?? Texture2D.whiteTexture;  // Fallback to white (all 1s)
```

**Stencil Format**: R8 (single-channel), 0 = environment, 1 = human

---

## InverseProjection Calculation (Rcam3/4 Standard)

### C# Implementation

```csharp
// Source: Keijiro's Rcam3/4 CameraUtil.cs
public static Vector4 GetInverseProjection(Matrix4x4 projectionMatrix) {
    var x = 1 / projectionMatrix[0, 0];  // 1 / focal_x
    var y = 1 / projectionMatrix[1, 1];  // 1 / focal_y
    var z = projectionMatrix[0, 2] * x;  // center_x / focal_x
    var w = projectionMatrix[1, 2] * y;  // center_y / focal_y
    return new Vector4(x, y, z, w);
}
```

**Usage** (if VFX requires InverseProjection explicitly):
```csharp
// In VFXARBinder.cs LateUpdate()
if (_bindInverseProj && _vfx.HasMatrix4x4(inverseProjProperty)) {
    var cam = Camera.main;
    var invProj = GetInverseProjection(cam.projectionMatrix);
    _vfx.SetVector4(inverseProjProperty, invProj);
}
```

**Note**: MetavidoVFX currently uses `RayParams` (similar concept) instead of `InverseProjection`. For true Rcam3/4 compatibility, add this calculation.

---

## RayParams vs InverseProjection

### RayParams (MetavidoVFX Standard)

```csharp
// ARDepthSource.cs - Compute RayParams
var proj = _arCamera.projectionMatrix;
float centerShiftX = proj.m02;
float centerShiftY = proj.m12;
float fov = _arCamera.fieldOfView * Mathf.Deg2Rad;
float tanV = Mathf.Tan(fov * 0.5f);
float tanH = tanV * _arCamera.aspect;

RayParams = new Vector4(centerShiftX, centerShiftY, tanH, tanV);
```

**Components**:
- `.xy`: Principal point offset (lens center shift)
- `.z`: `tan(fov_horizontal / 2)`
- `.w`: `tan(fov_vertical / 2)`

### InverseProjection (Rcam3/4 Standard)

```csharp
InverseProjection = new Vector4(
    1 / proj[0, 0],          // 1 / focal_x
    1 / proj[1, 1],          // 1 / focal_y
    proj[0, 2] / proj[0, 0], // center_x / focal_x
    proj[1, 2] / proj[1, 1]  // center_y / focal_y
);
```

**Equivalence**: Both encode the same camera intrinsics, but with different parameterizations:
- `RayParams.zw` = `tan(FOV/2)` (angle-based)
- `InverseProjection.xy` = `1/focal_length` (focal-length-based)

**Conversion**: `1 / focal_x ≈ tan(fov_x / 2) / (width / 2)`

---

## Depth Rotation (iOS Portrait Mode)

### Problem: ARKit Depth is Landscape

ARKit depth textures are always **landscape orientation** (wider than tall), even in portrait mode. MetavidoVFX VFX expect **portrait depth** (taller than wide).

**Solution**: 90° CW rotation using shader + RT swap

```csharp
// ARDepthSource.cs - Rotate depth 90° CW
[SerializeField] bool _rotateDepthTexture = true;  // Default: enabled

Texture RotateTexture(Texture source, ref RenderTexture rotatedRT) {
    // Swap width/height
    int rotW = source.height;
    int rotH = source.width;

    // Create RT with swapped dimensions
    rotatedRT = new RenderTexture(rotW, rotH, 0, RenderTextureFormat.RFloat);

    // Blit with RotateUV90CW shader
    Graphics.Blit(source, rotatedRT, _rotateMaterial);
    return rotatedRT;
}
```

**Shader**: `Assets/Resources/RotateUV90CW.shader`
```hlsl
Shader "Hidden/RotateUV90CW" {
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            sampler2D _MainTex;

            float4 frag(v2f_img i) : SV_Target {
                float2 uv = i.uv;
                uv = float2(1.0 - uv.y, uv.x); // 90° CW rotation
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
```

**RayParams Adjustment** (negated tanH):
```csharp
if (_rotateDepthTexture) {
    float depthAspect = (float)depth.width / depth.height;
    tanH = tanV * depthAspect;
    RayParams = new Vector4(centerShiftX, centerShiftY, -tanH, tanV); // Negate tanH
}
```

---

## VFXProxBuffer (Rcam3 Spatial Acceleration)

### When Needed

**Plexus/Metaball Effects**: VFX that connect particles with lines or form mesh surfaces require **nearest-neighbor queries**. Without acceleration, this is O(N²) brute force.

**Rcam3 Solution**: VFXProxBuffer - 16×16×16 spatial hash grid (4096 cells, 32 points/cell max)

### Implementation

**C# Setup**:
```csharp
// VFXProxBuffer.cs (from Rcam3)
const int CellsPerAxis = 16;
const int CellCapacity = 32;

GraphicsBuffer pointBuffer;  // 4096 * 32 * float3 = 1.5MB
GraphicsBuffer countBuffer;  // 4096 * uint = 16KB

// Compute shader dispatch
_compute.SetBuffer(0, "VFXProx_PointBuffer", pointBuffer);
_compute.SetBuffer(0, "VFXProx_CountBuffer", countBuffer);
_compute.DispatchThreads(0, CellsPerAxis, CellsPerAxis, CellsPerAxis);
```

**HLSL API** (VFXProxCommon.hlsl):
```hlsl
// Add particle to grid (VFX Initialize context)
void VFXProx_AddPoint(float3 pos) {
    uint index = VFXProx_GetFlatIndexAt(pos);
    uint count = 0;
    InterlockedAdd(VFXProx_CountBuffer[index], 1, count);
    if (count < VFXProx_CellCapacity)
        VFXProx_PointBuffer[index * VFXProx_CellCapacity + count] = pos;
}

// Query nearest 2 points (VFX Update context)
void VFXProx_LookUpNearestPair(float3 pos, out float3 first, out float3 second) {
    // Searches 3x3x3 = 27 cells around query position
    // Returns 2 closest points for line connections
}
```

**Performance**: O(1) insertion, O(864) queries per particle (27 cells × 32 max points)

**MetavidoVFX Status**: NOT currently implemented. Plexus VFX (`plexus_depth_people_rcam3.vfx`) would benefit from this.

---

## Blitter Class (Unity 6 / URP)

### Problem: Graphics.Blit Deprecated

Unity 6 deprecated `Graphics.Blit` for URP. Keijiro's Rcam4 introduced **Blitter class** using `DrawProceduralNow`.

**Implementation** (Rcam4 Utils.cs):
```csharp
public class Blitter : System.IDisposable {
    Material _material;

    public Blitter(Shader shader) => _material = new Material(shader);

    public void Run(Texture source, RenderTexture dest, int pass) {
        RenderTexture.active = dest;
        _material.mainTexture = source;
        _material.SetPass(pass);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, 1); // Fullscreen triangle
    }

    public void Dispose() { Object.Destroy(_material); }
}
```

**Usage**:
```csharp
var blitter = new Blitter(Shader.Find("Hidden/MyFullscreenShader"));
blitter.Run(sourceTexture, destRT, 0);
blitter.Dispose();
```

**MetavidoVFX Status**: Currently using `Graphics.Blit` for depth rotation. Should migrate to Blitter for Unity 6+ compliance.

---

## MetavidoVFX Integration Checklist

### ✅ Implemented

- [x] ARDepthSource - Single compute dispatch for ALL VFX
- [x] VFXARBinder - Lightweight per-VFX binding with alias resolution
- [x] RayParams calculation (centerShift + tan(FOV))
- [x] InverseView matrix (TRS pattern)
- [x] Depth rotation for iOS portrait mode
- [x] StencilMap binding (human segmentation)
- [x] Demand-driven ColorMap allocation (spec-007)
- [x] Throttle/Intensity binding
- [x] Audio binding (global shader vectors)

### ⚠️ Partially Implemented

- [ ] InverseProjection (Vector4) - Currently using RayParams instead
- [ ] VelocityMap - Compute kernel exists but disabled by default
- [ ] Normal map - Compute kernel exists, rarely used

### ❌ Not Implemented (Rcam3/4 Features)

- [ ] VFXProxBuffer - Spatial hash grid for plexus/metaball effects
- [ ] Blitter class - Using deprecated Graphics.Blit instead
- [ ] Color LUT (Texture3D) - Rcam3/4 support color grading LUTs
- [ ] URP Renderer Features - Rcam4 integrates background/recolor as Renderer Features

### 🔧 Migration Tasks

1. **Rcam2 VFX Property Renaming**:
   - `RayParamsMatrix` (Matrix4x4) → `RayParams` (Vector4)
   - OR: Modify VFXARBinder to compute Matrix4x4 from RayParams

2. **InverseProjection Support**:
   - Add `GetInverseProjection()` method to ARDepthSource
   - Bind as Vector4 to VFX (currently using RayParams)

3. **VFXProxBuffer Integration**:
   - Port Rcam3's VFXProxBuffer.cs + VFXProxCommon.hlsl
   - Enable for `plexus_depth_people_rcam3.vfx` and similar effects

4. **Blitter Migration**:
   - Replace `Graphics.Blit` in depth rotation code
   - Use Blitter for all fullscreen shader passes

---

## Critical Notes (Always Remember)

### Property Naming Standards

| Rcam2 (Legacy) | Rcam3/4 (Standard) | MetavidoVFX |
|----------------|-------------------|-------------|
| `ProjectionVector` | `InverseProjection` | `RayParams` |
| `InverseViewMatrix` | `InverseView` | `InverseView` |
| `RayParamsMatrix` (4x4) | `RayParams` (Vector4) | `RayParams` (Vector4) |

**Migration Path**: VFXARBinder's alias resolution handles these differences automatically.

### Compute Shader Thread Groups

❌ **WRONG**: `[numthreads(8,8,1)]` + `ceil(width/8)` dispatch
✅ **CORRECT**: `[numthreads(32,32,1)]` + `ceil(width/32)` dispatch

**Reason**: Thread group size must match dispatch groups. Mismatch causes "Thread group count exceeds maximum" errors.

### Depth Formats

Always use **RenderTextureFormat.RHalf** (16-bit float) for depth:
```csharp
var depthRT = new RenderTexture(width, height, 0, RenderTextureFormat.RHalf) {
    wrapMode = TextureWrapMode.Clamp
};
```

### VFX Global Texture Access

❌ **DOES NOT WORK**: `Shader.SetGlobalTexture("_DepthMap", depth);`
✅ **WORKS**: `vfx.SetTexture("DepthMap", depth);`

**Reason**: VFX Graph cannot read global textures set via `Shader.SetGlobal*`. Must use per-VFX `SetTexture()`.

**Exception**: Vectors/Matrices CAN be global:
```csharp
Shader.SetGlobalVector("_ARRayParams", rayParams);  // ✅ Works
Shader.SetGlobalMatrix("_ARInverseView", invView);  // ✅ Works
```

---

## Testing Procedure

### 1. Auto-Detection (Recommended)

```csharp
// In Unity Editor:
// 1. Select VFX GameObject with VFXARBinder component
// 2. Right-click component → "Auto-Detect Bindings (with Aliases)"
// 3. Verify bindings in Inspector
```

**Expected Output** (Console):
```
[VFXARBinder] Auto-detected bindings for plexus_depth_people_rcam3 (with alias resolution):
  DepthMap=True (DepthMap)
  StencilMap=False (none)
  PositionMap=True (PositionMap)
  ColorMap=True (ColorMap)
  RayParams=True (RayParams)
  InverseView=True (InverseView)
  InverseProj=False (none)
  Throttle=True (Throttle)
```

### 2. Manual Verification

**Menu**: `H3M > VFX Pipeline Master > Testing > Validate All Bindings`

**Checks**:
- ARDepthSource exists and is ready
- All VFX have VFXARBinder
- No VFXBinderManager/VFXARDataBinder (legacy) present
- Binding toggles match VFX properties
- Textures are valid (not null, width/height > 0)

### 3. Runtime Dashboard

**Keyboard**: Press `Tab` in Play mode (requires VFXPipelineDashboard)

**Metrics**:
- FPS graph (60-frame history, min/avg/max)
- Pipeline flow: ARDepthSource → VFXARBinder → VFX
- Binding status (green/red per VFX)
- Texture allocations (DepthMap, StencilMap, PositionMap, ColorMap)
- Particle counts

### 4. Build Testing (iOS)

**Commands**:
```bash
./build_ios.sh           # Unity build → Xcode project
./build_and_deploy.sh    # Full cycle → device install
./debug.sh               # Stream device logs
```

**Expected Console Output** (iOS device):
```
[ARDepthSource] Depth available: 192x256 (rotated=True)
[VFXARBinder] plexus_depth_people_rcam3: Bound 6 properties. IsBound=True
[VFXPipelineDashboard] FPS: 60.2 (avg), Binders: 10/10 active, Memory: 48.3 MB
```

---

## Reference Files

### MetavidoVFX

| File | Purpose |
|------|---------|
| `Assets/Scripts/Bridges/ARDepthSource.cs` | Singleton compute source, O(1) dispatch |
| `Assets/Scripts/Bridges/VFXARBinder.cs` | Lightweight per-VFX binding with aliases |
| `Assets/Scripts/VFX/VFXLibraryManager.cs` | VFX library with pipeline integration |
| `Assets/Scripts/VFX/VFXPipelineDashboard.cs` | Real-time debug UI (Tab toggle) |
| `Assets/Scripts/VFX/VFXTestHarness.cs` | Keyboard shortcuts (1-9, Space, C, A, P) |
| `Assets/Scripts/Editor/VFXPipelineMasterSetup.cs` | Editor automation (`H3M > VFX Pipeline Master`) |
| `Assets/Shaders/DepthToWorld.compute` | GPU depth→world position conversion |
| `Assets/Resources/RotateUV90CW.shader` | 90° CW rotation for iOS portrait |
| `Assets/Resources/VFX/Rcam2/` | 20 Rcam2 VFX (HDRP→URP converted) |
| `Assets/Resources/VFX/Rcam3/` | 8 Rcam3 VFX (URP standard) |
| `Assets/Resources/VFX/Rcam4/` | 14 Rcam4 VFX (URP production) |

### Keijiro's Rcam Repositories

| Repo | URL | Key Files |
|------|-----|-----------|
| **Rcam2** | https://github.com/keijiro/Rcam2 | `VFXRcamMetadataBinder.cs`, `RcamReceiver.cs` |
| **Rcam3** | https://github.com/keijiro/Rcam3 | `VFXRcamBinder.cs`, `VFXProxBuffer.cs`, `RcamCommon.hlsl` |
| **Rcam4** | https://github.com/keijiro/Rcam4 | `VFXRcamBinder.cs`, `Utils.cs` (Blitter), `RcamCommon.hlsl` |

### Knowledge Base

| File | Purpose |
|------|---------|
| `KnowledgeBase/_RCAM_QUICK_REFERENCE.md` | Property naming, binder pattern, depth reconstruction |
| `KnowledgeBase/_RCAM_SERIES_ARCHITECTURE_RESEARCH.md` | Full architecture comparison (47 files analyzed) |
| `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | 50+ production code snippets |
| `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` | 520+ curated repos (keijiro section) |

---

**End of Specification**

Generated: 2026-01-20
Researcher: Unity XR-AI Research Agent
Total VFX Analyzed: 73 (42 Rcam-based, 31 other)
Total Bindings Documented: 25+ properties across 3 Rcam versions
Key Findings: Property evolution, compute requirements, binding patterns
