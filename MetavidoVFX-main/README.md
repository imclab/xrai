# Metavido VFX

![gif](https://github.com/user-attachments/assets/124a2b96-76d0-4e2a-8761-d2cc4ee1df72)
![gif](https://github.com/user-attachments/assets/078d9368-25ff-4fa8-99ed-0dbfadfc02b9)

**Metavido VFX** is a demonstration project that visualizes volumetric videos captured
with an iPhone Pro using its LiDAR sensor. It utilizes Unity’s VFX Graph and WebGPU to
create visually striking effects.

## Related Project

**Metavido** is an experimental project that captures volumetric videos with camera
tracking data using a burnt-in barcode extension. Please refer to the
[Metavido repository] for further details.

[Metavido repository]: https://github.com/keijiro/Metavido

## System Requirements

- Unity 6
- VFX Graph with URP or HDRP

## Quick Start (Project)

- Open `Assets/Player.unity` or generate a fresh scene via menu: **Metavido/Setup ARKit Scene**.
- Ensure AR Foundation packages are present (see `Packages/manifest.json`).
- For Editor testing with device feed, use AR Foundation Remote 2 (see `ARFoundationRemoteSetup.md`).

### Optional: Optimized AR → VFX bridge
- Component: `OptimizedARVFXBridge` (attach to AR camera). Forwards AR occlusion depth/stencil + camera matrices to VFX; adaptive RT resolution.
- Sample compute shader: `Assets/Shaders/DepthToWorld.compute` (produces `PositionMap` world positions; `_UseStencil` flag gates stencil).
- Default VFX property names (configurable in inspector): `DepthMap`, `StencilMap`, `PositionMap`, `InverseView`, `InverseProj`, `DepthRange`.

## Web Browser Demo

A WebGPU build is available on [Unity Play].

[Unity Play]: https://play.unity.com/games/f4e0ea34-bd6d-4b2d-b24d-69ffa6e88795/metavido

To run it in your web browser, your environment must support WebGPU. It works on most
desktop browsers except Safari. It also runs on Chrome for Android.

For Safari on macOS or iOS, you must enable WebGPU manually using feature flags.
Follow the steps below to enable it.

###  Steps to enable WebGPU in Safari for iPhone

1. Open **Settings** on your iPhone.
2. Enable **Developer Mode** under *Privacy & Security > Developer Mode*.
3. Go to *Apps > Safari > Advanced > Feature Flags*, then enable **WebGPU**.

---

## Depth-to-VFX Pipeline Analysis (2026-01-14)

This section documents the analysis of the depth-to-hologram pipeline, comparing our implementation with the canonical patterns from Keijiro Takahashi's original `Metavido`, `Rcam4`, and `Echovision` projects.

### Correct Pattern (Keijiro's Metavido)

The original Metavido uses a **ray-based unprojection** approach (simpler and more direct):

```hlsl
// Utils.hlsl from jp.keijiro.metavido
float3 mtvd_DistanceToWorldPosition(float2 uv, float d, float4 rayParams, float4x4 inverseView)
{
    // 1. UV [0,1] → NDC ray [-1,1] for XY, Z=1
    float3 ray = float3((uv - 0.5) * 2, 1);

    // 2. Apply camera intrinsics (center shift + FOV tangent)
    ray.xy = (ray.xy + rayParams.xy) * rayParams.zw;

    // 3. Scale by distance and transform to world space
    return mul(inverseView, float4(ray * d, 1)).xyz;
}
```

**Key Parameters**:
- `RayParams` = `(centerShiftX, centerShiftY, tanHalfFov * aspectRatio, tanHalfFov)`
- `InverseView` = `Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one)` (NOT inverse VP!)
- `d` = raw depth value (meters from ARKit)

### Issues Found in Our Implementation

| Issue | File | Severity | Status |
|-------|------|----------|--------|
| Thread group mismatch (8×8 dispatch for 32×32 kernel) | `OptimalARVFXBridge.cs:197` | **CRITICAL** | 🔧 Needs fix |
| Double Y-flip (UV flipped then clip space flipped) | `DepthProcessor.compute:37,40` | **HIGH** | 🔧 Needs fix |
| Using inverse VP matrix instead of ray-based approach | Multiple files | **MEDIUM** | Consider refactor |
| Missing filterMode on position RT | `PeopleOcclusionVFXManager.cs:151` | **MEDIUM** | 🔧 Needs fix |
| Missing wrapMode (Clamp) on position RT | `HologramSource.cs:150` | **MEDIUM** | 🔧 Needs fix |
| Square RT (512×512) vs rectangular screen | `OptimalARVFXBridge.cs:24` | **MEDIUM** | Consider fix |
| DepthStencilMask.shader masking logic disabled | `DepthStencilMask.shader:53-54` | **LOW** | Enable or remove |

### Recommended Fixes

#### 1. Thread Group Alignment (Critical)

```csharp
// OptimalARVFXBridge.cs - BEFORE (WRONG)
int threadGroupsX = Mathf.CeilToInt(textureResolution.x / 8.0f);
int threadGroupsY = Mathf.CeilToInt(textureResolution.y / 8.0f);

// AFTER (matches 32×32 numthreads in DepthToWorld.compute)
int threadGroupsX = Mathf.CeilToInt(textureResolution.x / 32.0f);
int threadGroupsY = Mathf.CeilToInt(textureResolution.y / 32.0f);
```

#### 2. RenderTexture Configuration

```csharp
// Add to all position map creation
positionRT.filterMode = FilterMode.Bilinear;
positionRT.wrapMode = TextureWrapMode.Clamp;  // Prevents edge artifacts
```

#### 3. Ray-Based Unprojection (Recommended Refactor)

Instead of:
```csharp
// Current approach: Full inverse VP matrix
var vpMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
var invVPMatrix = vpMatrix.inverse;
```

Use:
```csharp
// Keijiro's approach: Separate intrinsics/extrinsics
float fovV = camera.fieldOfView * Mathf.Deg2Rad;
float h = Mathf.Tan(fovV * 0.5f);
float w = h * camera.aspect;
Vector4 rayParams = new Vector4(0, 0, w, h);  // centerShift=0 for AR

Matrix4x4 inverseView = Matrix4x4.TRS(
    camera.transform.position,
    camera.transform.rotation,
    Vector3.one
);
```

### VFX Graph Property Bindings

Standard property names (match Metavido package):

| Property | Type | Description |
|----------|------|-------------|
| `ColorMap` | Texture2D | RGB camera feed |
| `DepthMap` | Texture2D | Depth buffer (RFloat) |
| `RayParams` | Vector4 | `(centerShiftX, centerShiftY, tanHalfFov*aspect, tanHalfFov)` |
| `InverseView` | Matrix4x4 | Camera transform (TRS) |
| `DepthRange` | Vector2 | `(nearClip, farClip)` typically `(0.1, 10)` |

### Reference Projects

| Project | Pattern | Source |
|---------|---------|--------|
| **Metavido** | Ray-based VFX binding | `jp.keijiro.metavido` package |
| **Rcam4** | RGBD streaming + position texture | `keijiro/Rcam4` |
| **Echovision** | ARMesh → GraphicsBuffer → VFX | `keijiro/Echovision` |
| **HumanParticleEffect** | Human segmentation → particles | `YoHana19/HumanParticleEffect` |

### Testing Checklist

- [ ] Verify thread groups match `numthreads` in compute shaders
- [ ] Check Y-axis orientation (should NOT be double-flipped)
- [ ] Confirm RenderTexture formats are consistent (ARGBFloat for positions)
- [ ] Test depth range (0.1m-10m typical for indoor AR)
- [ ] Profile on device (target 60 FPS with <5ms compute time)

---

## Knowledgebase References

See parent repository for extensive documentation:

| Document | Contents |
|----------|----------|
| `../KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | 50+ production code patterns |
| `../KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` | Hologram, portal, depth patterns |
| `../KnowledgeBase/_H3M_HOLOGRAM_ROADMAP.md` | H3M system architecture |
| `../KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` | 520+ curated GitHub repos |
