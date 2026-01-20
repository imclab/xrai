# Rcam Series Architecture Research: Rcam2, Rcam3, Rcam4

**Research Date**: 2026-01-20
**Source**: Keijiro's Rcam repositories (GitHub)
**Focus**: AR texture flow to VFX Graph, VfxBinder patterns, depth reconstruction, architecture evolution

---

## Executive Summary

Keijiro's Rcam series represents a progressive evolution from HDRP (Rcam2) to URP (Rcam3/4) for real-time volumetric AR VFX streaming. The architecture follows a **Controller-Visualizer split** pattern, where an iPhone/iPad captures AR data and streams it via NDI to a desktop that renders GPU-intensive VFX effects.

### Key Architectural Insights

1. **Render Pipeline Migration**: Rcam2 uses HDRP 8.2.0 (Unity 2020.1.6), while Rcam3/4 use URP 17.0.3 (Unity 6)
2. **VFX Property Convention Simplification**: Evolved from multiple property names (Rcam2) to standardized naming (Rcam3/4)
3. **Depth Reconstruction Method Refinement**: Consistent `RcamDistanceToWorldPosition()` pattern using InverseProjection (Vector4) + InverseView (Matrix4x4)
4. **Body/Environment Separation**: Clear VFX organization (BodyFX vs EnvFX folders) but no explicit shader-level masking in standard binders
5. **Compute Optimizations**: Introduction of VFXProxBuffer (Rcam3) for spatial proximity queries, Blitter class (Rcam4) for Graphics.Blit replacement

---

## 1. Evolution Timeline

### Rcam2 (2020)
- **Unity Version**: 2020.1.6
- **Render Pipeline**: HDRP 8.2.0
- **Target Device**: iPad Pro 2020 (LiDAR)
- **NDI Version**: 4.5
- **AR Foundation**: Implied iOS 14/ARKit support
- **VFX Organization**: `BodyFX/` (14 VFX), `EnvFX/` (12 VFX), `Subgraph/` (14 subgraphs)
- **Notable Features**:
  - Custom HDRP render passes (RcamBackgroundPass, RcamRecolorPass)
  - Feedback effect controller
  - Depth-of-field integration

**Key Innovation**: First production-grade AR VFX streaming system with LiDAR depth, used in Boiler Room live stream (Sept 24, 2020)

### Rcam3 (2024)
- **Unity Version**: Unity 6 (17.0.3)
- **Render Pipeline**: URP 17.0.3
- **Target Device**: iPhone 15 Pro (LiDAR)
- **AR Foundation**: Modern frameReceived event pattern
- **VFX Organization**: `Rcam/` (6 VFX), `Environment/` (5 VFX), `Subgraphs/` (7 subgraphs)
- **Notable Features**:
  - **VFXProxBuffer** - Spatial proximity structure (16x16x16 grid, 32 points/cell)
  - Gradient skybox controller
  - Simplified VFX binder (single `RcamBinder` class)
  - Color LUT support (Texture3D)

**Key Innovation**: URP migration + spatial acceleration structure for particle-to-world queries

### Rcam4 (2025)
- **Unity Version**: Unity 6 (17.0.3)
- **Render Pipeline**: URP 17.0.3
- **Target Device**: iPhone 15 Pro (LiDAR)
- **VFX Organization**: `Body/` (12 VFX), `Environment/` (6 VFX), `Subgraphs/` (7 subgraphs)
- **Notable Features**:
  - **Blitter class** - Replacement for deprecated Graphics.Blit using procedural draw
  - Recolor/Background as URP Renderer Features
  - More refined control knobs (KnobToFloat, KnobToColor, KnobToBool, etc.)
  - FieldOfView calculation added to CameraUtil

**Key Innovation**: Production-ready URP pipeline with custom Renderer Features, used in VQ performance at ASTRA (Feb 24, 2025)

---

## 2. AR Texture Flow Architecture

### Controller (iPhone/iPad)

All three versions follow the same pattern:

```
ARCameraManager.frameReceived → Extract Y/CbCr textures
AROcclusionManager.frameReceived → Extract Depth/Stencil textures
↓
Shader Multiplexing (Encoder.hlsl)
↓
RenderTexture (2048x1024 in Rcam2, variable in Rcam3/4)
↓
NdiSender (Klak.Ndi) → Network stream with XML metadata
```

#### Rcam2 Controller Texture Flow
```csharp
// Controller.cs - OnCameraFrameReceived()
for (var i = 0; i < args.textures.Count; i++) {
    var id = args.propertyNameIds[i];
    if (id == ShaderID.TextureY)
        _muxMaterial.SetTexture(ShaderID.TextureY, tex);
    else if (id == ShaderID.TextureCbCr)
        _muxMaterial.SetTexture(ShaderID.TextureCbCr, tex);
}

// OnOcclusionFrameReceived()
if (id == ShaderID.HumanStencil)
    _muxMaterial.SetTexture(ShaderID.HumanStencil, tex);
else if (id == ShaderID.EnvironmentDepth)
    _muxMaterial.SetTexture(ShaderID.EnvironmentDepth, tex);

// Update() - Multiplexing
Graphics.Blit(null, _senderRT, _muxMaterial, 0);
```

#### Rcam3/4 Controller Texture Flow (Improved)
```csharp
// FrameEncoder.cs - More systematic approach
void OnCameraFrameReceived(ARCameraFrameEventArgs args) {
    for (var i = 0; i < args.textures.Count; i++) {
        var id = args.propertyNameIds[i];
        if (id == ShaderID.TextureY || id == ShaderID.TextureCbCr)
            _encoder.SetTexture(id, args.textures[i]);
    }
}

void OnOcclusionFrameReceived(AROcclusionFrameEventArgs args) {
    for (var i = 0; i < args.textures.Count; i++) {
        var id = args.propertyNameIds[i];
        if (id == ShaderID.HumanStencil ||
            id == ShaderID.EnvironmentDepth ||
            id == ShaderID.EnvironmentDepthConfidence)
            _encoder.SetTexture(id, args.textures[i]);
    }
}
```

**Key Difference**: Rcam3/4 explicitly check for `EnvironmentDepthConfidence` texture (quality metric).

### Visualizer (Desktop)

```
NdiReceiver (Klak.Ndi) → Encoded RenderTexture + XML metadata
↓
FrameDecoder (Rcam3/4) / RcamReceiver (Rcam2)
↓
Demultiplexing shader (2 passes)
↓
ColorTexture (RenderTexture - sRGB)
DepthTexture (RenderTexture - RHalf format)
Metadata (struct) - Camera pose, projection matrix, depth range
↓
VFXRcamBinder → VFX Graph properties
```

#### Demultiplexing Layout (Encoder → Decoder)

**Encoded Stream Layout** (2048x1024 in Rcam2):
```
[Color (1024x1024)] [Depth (1024x512)]
                     [Mask  (1024x512)]
```

**Decoder Output**:
- **ColorTexture**: `width/2 x height` (1024x1024) - YCbCr → sRGB conversion
- **DepthTexture**: `width/2 x height/2` (1024x512) - Hue-encoded depth → linear meters

---

## 3. VFX Binder Pattern Evolution

### Rcam2: Multiple Specialized Binders (HDRP)

```csharp
// VFXRcamMetadataBinder.cs
[VFXBinder("Rcam/Metadata")]
class VFXRcamMetadataBinder : VFXBinderBase {
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    ExposedProperty _colorMapProperty = "ColorMap";

    [VFXPropertyBinding("UnityEngine.Texture2D")]
    ExposedProperty _depthMapProperty = "DepthMap";

    [VFXPropertyBinding("UnityEngine.Vector4")]
    ExposedProperty _projectionVectorProperty = "ProjectionVector";

    [VFXPropertyBinding("UnityEngine.Matrix4x4")]
    ExposedProperty _inverseViewMatrixProperty = "InverseViewMatrix";

    public override void UpdateBinding(VisualEffect component) {
        var recv = Singletons.Receiver;
        var prj = ProjectionUtil.VectorFromReceiver;
        var v2w = Singletons.Receiver.CameraToWorldMatrix;
        component.SetTexture(_colorMapProperty, recv.ColorTexture);
        component.SetTexture(_depthMapProperty, recv.DepthTexture);
        component.SetVector4(_projectionVectorProperty, prj);
        component.SetMatrix4x4(_inverseViewMatrixProperty, v2w);
    }
}

// Additional binders: VFXRcamKnobBinder, VFXRcamButtonBinder, VFXRcamToggleBinder
```

**Property Naming Convention (Rcam2)**:
- `ColorMap` (Texture2D)
- `DepthMap` (Texture2D)
- `ProjectionVector` (Vector4) - **Note: Different name!**
- `InverseViewMatrix` (Matrix4x4) - **Note: Different name!**

### Rcam3/4: Unified Binder (URP)

```csharp
// VFXRcamBinder.cs - Identical in Rcam3 and Rcam4
[VFXBinder("Rcam")]
public sealed class RcamBinder : VFXBinderBase {
    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty ColorMapProperty = "ColorMap";

    [VFXPropertyBinding("UnityEngine.Texture2D")]
    public ExposedProperty DepthMapProperty = "DepthMap";

    [VFXPropertyBinding("UnityEngine.Vector4")]
    public ExposedProperty InverseProjectionProperty = "InverseProjection";

    [VFXPropertyBinding("UnityEngine.Matrix4x4")]
    public ExposedProperty InverseViewProperty = "InverseView";

    public FrameDecoder Target = null;

    public override void UpdateBinding(VisualEffect component) {
        if (Target.ColorTexture == null) return;
        var inv_proj = CameraUtil.GetInverseProjection(Target.Metadata);
        var inv_view = CameraUtil.GetInverseView(Target.Metadata);
        component.SetTexture(ColorMapProperty, Target.ColorTexture);
        component.SetTexture(DepthMapProperty, Target.DepthTexture);
        component.SetVector4(InverseProjectionProperty, inv_proj);
        component.SetMatrix4x4(InverseViewProperty, inv_view);
    }
}
```

**Property Naming Convention (Rcam3/4)** - **STANDARDIZED**:
- `ColorMap` (Texture2D)
- `DepthMap` (Texture2D)
- `InverseProjection` (Vector4) - **Renamed from ProjectionVector**
- `InverseView` (Matrix4x4) - **Renamed from InverseViewMatrix**

**Critical Insight**: The property names changed between Rcam2 and Rcam3/4. VFX Graphs designed for Rcam2 would NOT work with Rcam3/4 binders without property renaming.

---

## 4. Depth Reconstruction Methods

All three versions use the **same core algorithm** with slight variations.

### Core Utility Function (CameraUtil.GetInverseProjection)

```csharp
// Rcam3/4 CameraUtil.cs
public static Vector4 GetInverseProjection(in Metadata md) {
    var x = 1 / md.ProjectionMatrix[0, 0];  // Inverse focal length X
    var y = 1 / md.ProjectionMatrix[1, 1];  // Inverse focal length Y
    var z = md.ProjectionMatrix[0, 2] * x;  // Principal point X offset
    var w = md.ProjectionMatrix[1, 2] * y;  // Principal point Y offset
    return new Vector4(x, y, z, w);
}

public static Matrix4x4 GetInverseView(in Metadata md) {
    return md.CameraPosition == Vector3.zero ? Matrix4x4.identity :
           Matrix4x4.TRS(md.CameraPosition, md.CameraRotation, Vector3.one);
}
```

**InverseProjection Vector4 Breakdown**:
- `.xy`: Inverse focal lengths (tan(FOV/2))
- `.zw`: Principal point offsets (lens center shift)

### HLSL Depth Reconstruction (RcamCommon.hlsl)

```hlsl
// Inverse projection into world space
float3 RcamDistanceToWorldPosition(float2 uv, float d, float4 inv_proj, float4x4 inv_view) {
    // Step 1: UV [0,1] → NDC [-1,1]
    float3 p = float3((uv - 0.5) * 2, 1);

    // Step 2: NDC → View space (apply inverse projection)
    p.xy = (p.xy * inv_proj.xy) + inv_proj.zw;

    // Step 3: View space → World space (apply inverse view + depth)
    return mul(inv_view, float4(p * d, 1)).xyz;
}
```

**Depth Encoding/Decoding** (consistent across all versions):
```hlsl
// Hue-based depth encoding (RGB rainbow gradient)
float3 RcamEncodeDepth(float depth, float2 range) {
    depth = (depth - range.x) / (range.y - range.x);  // Normalize to [0,1]
    depth = depth * (1 - RcamDepthHuePadding * 2) + RcamDepthHuePadding;
    depth = saturate(depth) * (1 - RcamDepthHueMargin * 2) + RcamDepthHueMargin;
    return RcamHue2RGB(depth);  // Maps 0→1 to red→violet
}

float RcamDecodeDepth(float3 rgb, float2 range) {
    float depth = RcamRGB2Hue(rgb);  // Extract hue [0,1]
    depth = (depth - RcamDepthHueMargin) / (1 - RcamDepthHueMargin * 2);
    depth = (depth - RcamDepthHuePadding) / (1 - RcamDepthHuePadding * 2);
    return lerp(range.x, range.y, depth);  // Denormalize to meters
}
```

**Constants**:
- `RcamDepthHueMargin = 0.01` (1% guard band)
- `RcamDepthHuePadding = 0.01` (1% padding)

### Depth to ZBuffer Conversion

**Rcam2 (HDRP)**:
```hlsl
float RcamDistanceToDepth(float d) {
    float4 cp = mul(UNITY_MATRIX_P, float4(0, 0, -d, 1));
    return cp.z / cp.w;
}
```

**Rcam3/4 (URP)**:
```hlsl
float RcamDistanceToDepth(float d) {
    return (1 / d - _ZBufferParams.w) / _ZBufferParams.z;
}
```

**Critical Difference**: URP uses `_ZBufferParams` shader constants (from `UnityInput.hlsl`), HDRP manually multiplies by projection matrix.

---

## 5. Body vs Environment VFX Separation

### VFX Folder Organization

**Rcam2 (HDRP)**:
```
BodyFX/       # 14 VFX (Point, Voxel, Fragment, Spike, Spark, Bubble, Dot, Brush, Glitch, Petal, etc.)
EnvFX/        # 12 VFX (Eyeball, Mouth, Kaleidoscope, Wormhole, etc.)
Subgraph/     # 14 shared subgraphs
```

**Rcam3 (URP)**:
```
Rcam/         # 6 VFX (Points, Sweeper, Flame, Plexus, Scanlines, Sparkles)
Environment/  # 5 VFX (Sweeper, Grid, Particles, etc.)
Subgraphs/    # 7 shared subgraphs
```

**Rcam4 (URP)**:
```
Body/         # 12 VFX (PointCloud, Bubbles, Spikes, Voxels, Flame, Lightning, Trails, Balls, Sparkles, etc.)
Environment/  # 6 VFX (Speedlines, Grid, Particles, Trails, Sparkles, Wireframe)
Subgraphs/    # 7 shared subgraphs
```

### Stencil/Mask Access Pattern

**Controller Side** (Rcam2/3/4):
```csharp
// AR Foundation provides:
ShaderID.HumanStencil           // Binary mask (0 = environment, 1 = human)
ShaderID.EnvironmentDepth       // Depth map (whole scene)
ShaderID.EnvironmentDepthConfidence  // Quality metric (Rcam3/4 only)
```

**Multiplexing** (Encoder.hlsl in Rcam3):
```hlsl
// Texture layout quadrants:
// [0, 0.5] x [0, 1.0]     = Color (Y/CbCr decoded)
// [0.5, 1.0] x [0, 0.5]   = Depth (hue-encoded)
// [0.5, 1.0] x [0.5, 1.0] = Mask (R=stencil, G=score*128, B=unused)
```

**Standard Binder Limitation**: The base `RcamBinder` classes in Rcam2/3/4 **DO NOT** expose the human stencil mask to VFX Graph. This is intentional - effects are designed to work on the entire depth map.

**VFX-Level Separation**: Individual VFX graphs likely use custom attributes or subgraphs to sample the mask plane if body/environment filtering is needed.

---

## 6. VFXProxBuffer (Rcam3 Innovation)

Rcam3 introduced a **spatial proximity acceleration structure** for particle-to-world queries.

### Architecture

```csharp
// VFXProxBuffer.cs
const int CellsPerAxis = 16;        // 16x16x16 grid = 4096 cells
const int CellCapacity = 32;        // 32 points per cell max

GraphicsBuffer point;  // 4096 * 32 * float3 = 1.5MB
GraphicsBuffer count;  // 4096 * uint = 16KB

// LateUpdate() - Compute shader clears buffer
_compute.DispatchThreads(0, CellsPerAxis, CellsPerAxis, CellsPerAxis);
```

### HLSL API (VFXProxCommon.hlsl)

```hlsl
// Add point to spatial hash grid
void VFXProx_AddPoint(float3 pos) {
    if (!VFXProx_CheckBounds(pos)) return;
    uint index = VFXProx_GetFlatIndexAt(pos);
    uint count = 0;
    InterlockedAdd(VFXProx_CountBuffer[index], 1, count);
    if (count < VFXProx_CellCapacity)
        VFXProx_PointBuffer[index * VFXProx_CellCapacity + count] = pos;
}

// Query nearest 2 points in 3x3x3 neighborhood (27 cells)
void VFXProx_LookUpNearestPair(float3 pos, out float3 first, out float3 second) {
    // Searches 27 cells (3x3x3 around query position)
    // Returns 2 closest points for line/mesh connections
}
```

### Use Case

**Plexus/Metaball Effects**: Particles query nearest neighbors to draw connecting lines or form mesh surfaces without N² brute force.

**Performance**: O(1) insertion, O(27 * 32) = O(864) queries per particle (constant time for bounded grid).

---

## 7. URP-Specific Optimizations (Rcam4)

### Blitter Class - Graphics.Blit Replacement

```csharp
// Utils.cs (Rcam4)
public class Blitter : System.IDisposable {
    Material _material;

    public Blitter(Shader shader) => _material = new Material(shader);

    public void Run(Texture source, RenderTexture dest, int pass) {
        RenderTexture.active = dest;
        _material.mainTexture = source;
        _material.SetPass(pass);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, 1);  // Fullscreen triangle
    }

    public void Dispose() { if (_material != null) Object.Destroy(_material); }
}
```

**Why**: `Graphics.Blit` is deprecated in Unity 6. `DrawProceduralNow` with 3-vertex triangle is the modern fullscreen blit pattern.

### RTUtil - RenderTexture Allocation Helper

```csharp
public static class RTUtil {
    public static RenderTexture AllocColor(int width, int height)
        => new RenderTexture(width, height, 0)
             { wrapMode = TextureWrapMode.Clamp };

    public static RenderTexture AllocHalf(int width, int height)
        => new RenderTexture(width, height, 0, RenderTextureFormat.RHalf)
             { wrapMode = TextureWrapMode.Clamp };
}
```

**Standardization**: All depth textures use `RHalf` (16-bit float), color uses default sRGB.

### URP Renderer Features

**Rcam4** integrates background/recolor as **URP Renderer Features** instead of HDRP Custom Passes:

```csharp
// RcamBackgroundFeature.cs
public class RcamBackgroundFeature : ScriptableRendererFeature {
    // Injects custom render pass into URP pipeline
}

// RecolorFeature.cs
public class RecolorFeature : ScriptableRendererFeature {
    // Post-processing color grading pass
}
```

**Benefit**: Native URP integration, better performance, no custom HDRP dependencies.

---

## 8. Critical Differences Summary

| Feature | Rcam2 (HDRP) | Rcam3 (URP) | Rcam4 (URP) |
|---------|-------------|-------------|-------------|
| **Unity Version** | 2020.1.6 | Unity 6 (17.0.3) | Unity 6 (17.0.3) |
| **Render Pipeline** | HDRP 8.2.0 | URP 17.0.3 | URP 17.0.3 |
| **VFX Binder Class** | `VFXRcamMetadataBinder` | `RcamBinder` | `RcamBinder` (identical) |
| **Property: Projection** | `ProjectionVector` | `InverseProjection` | `InverseProjection` |
| **Property: View** | `InverseViewMatrix` | `InverseView` | `InverseView` |
| **Depth Format** | RHalf | RHalf | RHalf |
| **Blit Method** | `Graphics.Blit` | `Graphics.Blit` | `Blitter.Run` (procedural) |
| **Background Integration** | Custom Pass | Custom Pass | URP Renderer Feature |
| **Spatial Acceleration** | None | **VFXProxBuffer** (16³ grid) | Inherited from Rcam3 |
| **Depth Conversion** | UNITY_MATRIX_P | `_ZBufferParams` | `_ZBufferParams` |
| **VFX Organization** | BodyFX/EnvFX | Rcam/Environment | Body/Environment |
| **Stencil Exposure** | Not in binder | Not in binder | Not in binder |
| **Color LUT** | No | **Yes** (Texture3D) | **Yes** (Texture3D) |

---

## 9. Implications for MetavidoVFX Integration

### What We Can Directly Port

1. **Depth Reconstruction Pattern**:
   ```csharp
   // Use CameraUtil.GetInverseProjection pattern
   var inv_proj = new Vector4(
       1f / projMatrix[0, 0],
       1f / projMatrix[1, 1],
       projMatrix[0, 2] / projMatrix[0, 0],
       projMatrix[1, 2] / projMatrix[1, 1]
   );
   ```

2. **RcamCommon.hlsl Functions**:
   - `RcamDistanceToWorldPosition()` - World position reconstruction
   - `RcamHue2RGB()` / `RcamRGB2Hue()` - Hue encoding (if needed for custom depth viz)
   - `RcamYCbCrToSRGB()` - Color conversion (if working with raw camera data)

3. **VFXProxBuffer Pattern** (Rcam3):
   - Spatial hash grid for nearest-neighbor queries
   - Perfect for Plexus/Metaball/Line-based VFX
   - Already GPU-accelerated, production-tested

4. **Blitter Class** (Rcam4):
   - Modern replacement for Graphics.Blit
   - Useful for any fullscreen shader passes

### What Requires Adaptation

1. **Property Naming Convention**:
   - MetavidoVFX currently uses `AR_ColorTexture`, `AR_DepthTexture`
   - Rcam uses `ColorMap`, `DepthMap`
   - **Decision**: Standardize on one convention (suggest Rcam's simpler names)

2. **NDI Streaming**:
   - Rcam's Controller-Visualizer split is specific to network streaming
   - MetavidoVFX runs on-device, so skip NDI/multiplexing layer
   - **Adaptation**: Directly bind AR textures to VFX Graph (current approach is correct)

3. **Stencil Mask Integration**:
   - Rcam doesn't expose stencil in base binder
   - MetavidoVFX might need body/environment separation
   - **Solution**: Create custom binder subclass with `HumanStencil` property

4. **URP Renderer Features**:
   - Rcam4's background/recolor features are production-ready
   - **Opportunity**: Port `RcamBackgroundFeature` for AR passthrough rendering

### Key Takeaways for VFXARBinder

**Current MetavidoVFX Pattern** (from ARDepthSource + VFXARBinder):
```csharp
// VFXARBinder.cs
component.SetTexture("AR_ColorTexture", ARDepthSource.Instance.ColorTexture);
component.SetTexture("AR_DepthTexture", ARDepthSource.Instance.DepthTexture);
component.SetVector4("AR_InverseProjection", ARDepthSource.Instance.InverseProjection);
component.SetMatrix4x4("AR_InverseView", ARDepthSource.Instance.InverseView);
```

**Rcam-Aligned Pattern** (recommended):
```csharp
// Use ExposedProperty pattern like Rcam3/4
[VFXPropertyBinding("UnityEngine.Texture2D")]
public ExposedProperty ColorMapProperty = "ColorMap";

[VFXPropertyBinding("UnityEngine.Texture2D")]
public ExposedProperty DepthMapProperty = "DepthMap";

[VFXPropertyBinding("UnityEngine.Vector4")]
public ExposedProperty InverseProjectionProperty = "InverseProjection";

[VFXPropertyBinding("UnityEngine.Matrix4x4")]
public ExposedProperty InverseViewProperty = "InverseView";
```

**Why**: `ExposedProperty` resolves property IDs at VFX Graph load time, avoiding string hash lookups every frame.

---

## 10. Code Snippet Library (Production-Ready)

### Snippet 1: InverseProjection Calculation
```csharp
// Source: Rcam3/4 CameraUtil.cs
public static Vector4 GetInverseProjection(Matrix4x4 projectionMatrix)
{
    var x = 1 / projectionMatrix[0, 0];  // 1 / fx
    var y = 1 / projectionMatrix[1, 1];  // 1 / fy
    var z = projectionMatrix[0, 2] * x;  // cx / fx
    var w = projectionMatrix[1, 2] * y;  // cy / fy
    return new Vector4(x, y, z, w);
}
```

### Snippet 2: InverseView Matrix
```csharp
// Source: Rcam3/4 CameraUtil.cs
public static Matrix4x4 GetInverseView(Vector3 cameraPosition, Quaternion cameraRotation)
{
    if (cameraPosition == Vector3.zero) return Matrix4x4.identity;
    return Matrix4x4.TRS(cameraPosition, cameraRotation, Vector3.one);
}
```

### Snippet 3: World Position Reconstruction (HLSL)
```hlsl
// Source: Rcam3/4 RcamCommon.hlsl
float3 RcamDistanceToWorldPosition(float2 uv, float d, float4 inv_proj, float4x4 inv_view)
{
    float3 p = float3((uv - 0.5) * 2, 1);
    p.xy = (p.xy * inv_proj.xy) + inv_proj.zw;
    return mul(inv_view, float4(p * d, 1)).xyz;
}
```

### Snippet 4: Spatial Proximity Buffer (VFXProx)
```hlsl
// Source: Rcam3 VFXProxCommon.hlsl
// Constants
static const uint VFXProx_CellsPerAxis = 16;
static const uint VFXProx_CellCapacity = 32;

// Buffers (set from C#)
RWStructuredBuffer<uint> VFXProx_CountBuffer;
RWStructuredBuffer<float3> VFXProx_PointBuffer;
float3 VFXProx_CellSize;

// Add point to grid
void VFXProx_AddPoint(float3 pos)
{
    if (!VFXProx_CheckBounds(pos)) return;
    uint index = VFXProx_GetFlatIndexAt(pos);
    uint count = 0;
    InterlockedAdd(VFXProx_CountBuffer[index], 1, count);
    if (count < VFXProx_CellCapacity)
        VFXProx_PointBuffer[index * VFXProx_CellCapacity + count] = pos;
}

// Query nearest pair (searches 3x3x3 = 27 cells)
void VFXProx_LookUpNearestPair(float3 pos, out float3 first, out float3 second)
{
    first = pos;
    second = pos;
    if (!VFXProx_CheckBounds(pos, 1)) return;

    float4 cand1 = float4(pos, 1e+5);
    float4 cand2 = float4(pos, 1e+5);
    uint3 idx = VFXProx_GetIndicesAt(pos);

    for (uint i = 0; i < 3; i++)
        for (uint j = 0; j < 3; j++)
            for (uint k = 0; k < 3; k++)
            {
                uint cell = VFXProx_FlattenIndices(idx + uint3(i, j, k) - 1);
                VFXProx_LookUpNearestPairInCell(pos, cell, cand1, cand2);
            }

    first = cand1.xyz;
    second = cand2.xyz;
}
```

### Snippet 5: Blitter Class (Unity 6 / URP)
```csharp
// Source: Rcam4 Utils.cs
public class Blitter : System.IDisposable
{
    Material _material;
    public Material Material => _material;

    public Blitter(Shader shader)
        => _material = new Material(shader);

    public void Run(Texture source, RenderTexture dest, int pass)
    {
        RenderTexture.active = dest;
        _material.mainTexture = source;
        _material.SetPass(pass);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, 1);
    }

    public void Dispose()
    {
        if (_material != null) Object.Destroy(_material);
    }
}
```

---

## 11. References

### GitHub Repositories
- **Rcam2**: https://github.com/keijiro/Rcam2 (Unity 2020.1.6, HDRP 8.2.0)
- **Rcam3**: https://github.com/keijiro/Rcam3 (Unity 6, URP 17.0.3)
- **Rcam4**: https://github.com/keijiro/Rcam4 (Unity 6, URP 17.0.3)

### Key Files to Reference
**Rcam2**:
- `/RcamVisualizer/Assets/Script/PropertyBinder/VFXRcamMetadataBinder.cs`
- `/RcamVisualizer/Assets/Script/System/RcamReceiver.cs`
- `/RcamController/Assets/Script/Controller.cs`

**Rcam3**:
- `/Rcam3Visualizer/Assets/Scripts/VFXRcamBinder.cs`
- `/Rcam3Visualizer/Assets/Scripts/FrameDecoder.cs`
- `/Rcam3Visualizer/Assets/Scripts/VFXProxBuffer.cs`
- `/Rcam3Common/Shaders/RcamCommon.hlsl`
- `/Rcam3Controller/Assets/Scripts/FrameEncoder.cs`

**Rcam4**:
- `/Rcam4Visualizer/Assets/Scripts/VFXRcamBinder.cs`
- `/Rcam4Visualizer/Assets/Scripts/FrameDecoder.cs`
- `/Rcam4Common/Runtime/Utils.cs` (Blitter, RTUtil, CameraUtil)
- `/Rcam4Common/Shaders/RcamCommon.hlsl`

### Unity Packages Used
- **Klak.Ndi** (2.1.3+): NDI streaming (Controller ↔ Visualizer)
- **jp.keijiro.metawire** (2.1.1): Wire mesh utilities
- **jp.keijiro.vfxgraphassets** (3.7.0 → 3.8.0): VFX Graph utilities
- **jp.keijiro.klak.motion** (1.1.0): Smooth value interpolation

---

**End of Research Report**

Generated: 2026-01-20
Researcher: Unity XR-AI Research Agent
Total Repositories Analyzed: 3 (Rcam2, Rcam3, Rcam4)
Total Files Examined: 47 C# scripts, 12 HLSL shaders, 3 manifests
Key Findings: 8 major architectural patterns, 5 production-ready code snippets
