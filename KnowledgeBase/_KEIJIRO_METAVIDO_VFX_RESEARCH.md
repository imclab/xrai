# Keijiro's MetavidoVFX Deep Research

**Date**: 2026-01-20
**Source**: https://github.com/keijiro/MetavidoVFX
**Related**: https://github.com/keijiro/Metavido
**Unity Version**: Unity 6 (6000.0.0f1+)
**VFX Graph Version**: 17.0.0+
**Status**: Research-only analysis (no code modifications)

---

## Executive Summary

MetavidoVFX is Keijiro's volumetric video VFX demonstration that captures iPhone Pro LiDAR depth and camera tracking data into a custom `.metavido` video format, then visualizes it using VFX Graph. The system uses a **burnt-in barcode metadata extension** to synchronize depth/stencil/color data with camera position/rotation without external files.

**Key Innovation**: UV-multiplexed video format stores color (right half), depth (top-left quarter), and human stencil (bottom-left quarter) in a single 1920x1080 frame with metadata encoded in the stencil region's blue channel.

---

## 1. Architecture Overview

### 1.1 Data Flow (AR → VFX)

```
iPhone LiDAR Capture (Metavido Encoder)
  ↓
.metavido file (1920x1080 multiplexed video)
  ├─ Color: Right half (960x1080)
  ├─ Depth: Top-left quarter (960x540) - Hue-encoded
  ├─ Stencil: Bottom-left quarter (960x540) - Human mask
  └─ Metadata: Blue channel of stencil (camera pos/rot/FOV/depth range)
  ↓
MetadataDecoder.cs (Reads burnt-in barcode from stencil blue channel)
  ├─ Metadata struct: CameraPosition, CameraRotation, CenterShift, FOV, DepthRange
  └─ RenderUtils.RayParams() → Vector4(sx, sy, h*16/9, h) where h = tan(FOV/2)
  ↓
TextureDemuxer.cs (Demux.shader)
  ├─ Pass 0: Extract Color + Stencil → ColorTexture (RGBA, 960x1080)
  └─ Pass 1: Extract + Decode Depth → DepthTexture (R16 Half, 960x540)
  ↓
VFXMetavidoBinder.cs (VFX Property Binder)
  ├─ ColorMap (Texture2D) → VFX exposed property
  ├─ DepthMap (Texture2D) → VFX exposed property
  ├─ RayParams (Vector4) → UV-to-ray conversion params
  ├─ InverseView (Matrix4x4) → Camera local-to-world transform
  └─ DepthRange (Vector2) → Near/far plane distances
  ↓
VFX Graph (Particles, Voxels, Afterimage)
  ├─ Sample DepthMap at UV
  ├─ Compute world position: MetavidoInverseProjection(UV, Depth, RayParams, InverseView)
  ├─ Sample ColorMap at UV
  └─ Apply visual effects with world-space particles
```

---

## 2. Core Components

### 2.1 VFXMetavidoBinder.cs

**File**: `/tmp/MetavidoVFX/Packages/jp.keijiro.metavido.vfxgraph/Scripts/VFXMetavidoBinder.cs`

**Purpose**: Unity VFX Property Binder that bridges Metavido decoder → VFX Graph exposed properties.

**Dependencies**:
- `MetadataDecoder _decoder` - Burnt-in metadata reader
- `TextureDemuxer _demux` - Texture splitter (color/depth)

**Exposed Properties**:
```csharp
[VFXPropertyBinding("UnityEngine.Texture2D")]
ExposedProperty _colorMapProperty = "ColorMap";

[VFXPropertyBinding("UnityEngine.Texture2D")]
ExposedProperty _depthMapProperty = "DepthMap";

[VFXPropertyBinding("UnityEngine.Vector4")]
ExposedProperty _rayParamsProperty = "RayParams";

[VFXPropertyBinding("UnityEngine.Matrix4x4")]
ExposedProperty _inverseViewProperty = "InverseView";

[VFXPropertyBinding("UnityEngine.Vector2")]
ExposedProperty _depthRangeProperty = "DepthRange";
```

**UpdateBinding Logic**:
```csharp
public override void UpdateBinding(VisualEffect component)
{
    var meta = _decoder.Metadata;
    if (!meta.IsValid) return;

    // Compute camera parameters from metadata
    var ray = RenderUtils.RayParams(meta);
    var iview = RenderUtils.InverseView(meta);

    // Bind textures and parameters to VFX
    component.SetTexture(_colorMapProperty, _demux.ColorTexture);
    component.SetTexture(_depthMapProperty, _demux.DepthTexture);
    component.SetVector4(_rayParamsProperty, ray);
    component.SetMatrix4x4(_inverseViewProperty, iview);
    component.SetVector2(_depthRangeProperty, meta.DepthRange);
}
```

**Key Pattern**: Uses `ExposedProperty` type instead of `const string` for VFX Graph property bindings (same pattern as ARFoundation VFXPropertyBinder).

---

### 2.2 RenderUtils.cs

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Decoder/Scripts/RenderUtils.cs`

**Purpose**: Converts Metavido metadata into shader-ready camera parameters.

**RayParams Computation**:
```csharp
public static Vector4 RayParams(in Metadata meta)
{
    var s = meta.CenterShift;        // Projection center offset (m02, m12)
    var h = Mathf.Tan(meta.FieldOfView / 2);  // Half-height of frustum
    return new Vector4(s.x, s.y, h * 16 / 9, h);
    //                 ^^^^  ^^^^ ^^^^^^^^^^  ^^^
    //                 sx    sy   width       height (aspect-corrected)
}
```

**InverseView Computation**:
```csharp
public static Matrix4x4 InverseView(in Metadata meta)
  => Matrix4x4.TRS(meta.CameraPosition, meta.CameraRotation, Vector3.one);
```

---

### 2.3 Metadata.cs

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Common/Scripts/Metadata.cs`

**Purpose**: Burnt-in metadata struct read from blue channel barcode.

**Stored Data**:
```csharp
readonly float _px, _py, _pz;           // Camera position
readonly float _rx, _ry, _rz;           // Camera rotation (quaternion xyz)
readonly float _sx, _sy;                // Projection center shift
readonly float _fov;                    // Vertical field of view
readonly float _near, _far;             // Depth range
readonly float _hash;                   // Frame hash for sync
```

**Public Accessors**:
```csharp
public Vector3 CameraPosition => new Vector3(_px, _py, _pz);
public Quaternion CameraRotation => new Quaternion(_rx, _ry, _rz, RW);
public Vector2 CenterShift => new Vector2(_sx, _sy);
public float FieldOfView => _fov;
public Vector2 DepthRange => new Vector2(_near, _far);
```

**Reconstruction Logic**: Quaternion W component is reconstructed from `sqrt(1 - x² - y² - z²)`.

---

### 2.4 TextureDemuxer.cs

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Decoder/Scripts/TextureDemuxer.cs`

**Purpose**: Splits multiplexed .metavido video frame into separate color and depth textures.

**Output Textures**:
- `ColorTexture`: RenderTexture (RGBA, 960x1080) - Contains RGB color + stencil alpha
- `DepthTexture`: RenderTexture (R16 Half, 960x540) - Linear depth in meters

**Demux Process**:
```csharp
public void Demux(Texture source, in Metadata meta)
{
    var (w, h) = (source.width, source.height);
    if (_color == null) _color = GfxUtil.RGBARenderTexture(w / 2, h);
    if (_depth == null) _depth = GfxUtil.RHalfRenderTexture(w / 2, h / 2);

    _material.SetInteger(ShaderID.Margin, _margin);
    _material.SetVector(ShaderID.DepthRange, meta.DepthRange);
    Graphics.Blit(source, _color, _material, 0);  // Pass 0: Color
    Graphics.Blit(source, _depth, _material, 1);  // Pass 1: Depth
}
```

---

### 2.5 Demux.shader

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Decoder/Shaders/Demux.shader`

**Purpose**: GPU shader for extracting color and depth from multiplexed video.

**Pass 0 - Color + Stencil**:
```hlsl
void VertexColor(float4 position : POSITION, float2 texCoord : TEXCOORD,
                 out float4 outPosition : SV_Position, out float4 outTexCoord : TEXCOORD)
{
    outPosition = UnityObjectToClipPos(position);
    // Map UV to color region (right half) and stencil region (bottom-left)
    outTexCoord = texCoord.xyxy * mtvd_FrameSize.xyxy / float4(2, 1, 2, 2);
    outTexCoord.x += mtvd_FrameSize.x / 2;  // Offset to right half
}

float4 FragmentColor(float4 position : SV_Position, float4 texCoord : TEXCOORD0) : SV_Target
{
    float3 c = _MainTex[texCoord.xy].rgb;    // Color from right half
    float  s = _MainTex[texCoord.zw].r;      // Stencil from bottom-left
    s = saturate(lerp(-0.1, 1, s));          // Compression noise filter
    return float4(c, s);
}
```

**Pass 1 - Depth Decoding**:
```hlsl
void VertexDepth(float4 position : POSITION, float2 texCoord : TEXCOORD,
                 out float4 outPosition : SV_Position, out float2 outTexCoord : TEXCOORD)
{
    outPosition = float4(position.x * 2 - 1, 1 - position.y * 2, 1, 1);
    outTexCoord = texCoord * mtvd_FrameSize / 2;
    outTexCoord.y += mtvd_FrameSize.y / 2;  // Map to top-left quarter
}

float4 FragmentDepth(float4 position : SV_Position, float2 texCoord : TEXCOORD) : SV_Target
{
    uint2 tc = texCoord;
    tc.x = min(tc.x, mtvd_FrameSize.x / 2 - 1 - _Margin);
    tc.y = max(tc.y, mtvd_FrameSize.y / 2 + _Margin);
    float3 rgb = _MainTex[tc].rgb;
    return mtvd_DecodeDepth(rgb, _DepthRange);  // Hue → linear depth
}
```

---

### 2.6 Common.hlsl

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Common/Shaders/Common.hlsl`

**Purpose**: Shared HLSL utilities for depth encoding/decoding and UV remapping.

**Depth Hue Encoding**:
```hlsl
float3 mtvd_EncodeDepth(float depth, float2 range)
{
    depth = (depth - range.x) / (range.y - range.x);  // Normalize to [0,1]
    depth = depth * (1 - mtvd_DepthHuePadding * 2) + mtvd_DepthHuePadding;
    depth = saturate(depth) * (1 - mtvd_DepthHueMargin * 2) + mtvd_DepthHueMargin;
    return mtvd_Hue2RGB(depth);  // Encode as hue (rainbow gradient)
}

float mtvd_DecodeDepth(float3 rgb, float2 range)
{
    float depth = mtvd_RGB2Hue(rgb);  // Decode hue from RGB
    depth = (depth - mtvd_DepthHueMargin ) / (1 - mtvd_DepthHueMargin  * 2);
    depth = (depth - mtvd_DepthHuePadding) / (1 - mtvd_DepthHuePadding * 2);
    return lerp(range.x, range.y, depth);  // Scale to meters
}
```

**UV Remapping Functions**:
```hlsl
// Full frame → Color region (right half)
float2 mtvd_UV_FullToColor(float2 uv)
{
    uv.x = uv.x * 2 - 1;  // Map [0,1] to [0.5,1] then to [0,1]
    return uv;
}

// Full frame → Depth region (top-left quarter)
float2 mtvd_UV_FullToDepth(float2 uv)
{
    uv *= 2;
    uv.y -= 1;  // Map to top-left
    return uv;
}

// Full frame → Stencil region (bottom-left quarter)
float2 mtvd_UV_FullToStencil(float2 uv)
{
    return uv * 2;
}
```

**Frame Layout**:
```
+-----+-----+  1920x1080 multiplexed frame
|  Z  |     |  Z: Hue-encoded depth (960x540)
+-----+  C  |  C: Color RGB (960x1080)
| S/M |     |  S: Human stencil (960x540)
+-----+-----+  M: Metadata in stencil blue channel
```

---

### 2.7 Utils.hlsl (Decoder)

**File**: `/tmp/Metavido/Packages/jp.keijiro.metavido/Decoder/Shaders/Utils.hlsl`

**Purpose**: World position reconstruction from depth.

**Distance → World Position**:
```hlsl
float3 mtvd_DistanceToWorldPosition(float2 uv, float d,
                                     in float4 rayParams,
                                     in float4x4 inverseView)
{
    // Convert UV to NDC space [-1, 1]
    float3 ray = float3((uv - 0.5) * 2, 1);

    // Apply center shift and FOV scaling
    ray.xy = (ray.xy + rayParams.xy) * rayParams.zw;
    //                   ^^^^^^^^^^^    ^^^^^^^^^^^
    //                   Center shift   FOV scale (width, height)

    // Scale ray by depth distance
    ray *= d;

    // Transform from camera space to world space
    return mul(inverseView, float4(ray, 1)).xyz;
}
```

**Key Insight**: This is the **canonical UV → world position** algorithm for depth-based VFX.

---

## 3. VFX Graph Integration

### 3.1 Metavido Inverse Projection Operator

**File**: `/tmp/MetavidoVFX/Packages/jp.keijiro.metavido.vfxgraph/VFX/Metavido Inverse Projection.vfxoperator`

**Inputs**:
- `UV` (Vector2) - Texture coordinates [0,1]
- `Depth` (Float) - Linear depth in meters
- `RayParams` (Vector4) - Camera projection parameters
- `InverseView` (Matrix4x4) - Camera local-to-world matrix

**Output**:
- `Position` (Vector3) - World-space position

**HLSL Code**:
```hlsl
float3 MetavidoInverseProjection(float2 UV, float Depth, float4 RayParams, float4x4 InverseView)
{
    float3 p = float3(UV * 2 - 1, 1);              // UV → NDC
    p.xy = (p.xy + RayParams.xy) * RayParams.zw;   // Apply center shift + FOV
    p *= Depth;                                    // Scale by depth
    return mul(InverseView, float4(p, 1)).xyz;     // Camera → world space
}
```

**Usage in VFX Graph**:
```yaml
Initialize Particle
  ├─ Sample DepthMap at random UV → Depth
  ├─ Metavido Inverse Projection(UV, Depth, RayParams, InverseView) → position
  └─ Sample ColorMap at UV → color
```

---

### 3.2 VFX Property Bindings

**Particles.vfx / Voxels.vfx / Afterimage.vfx**:

All VFX assets expect these exposed properties:

| Property Name | Type | Purpose |
|--------------|------|---------|
| `ColorMap` | Texture2D | RGB color from right half of frame |
| `DepthMap` | Texture2D | Linear depth in meters (R16 Half) |
| `RayParams` | Vector4 | `(centerShiftX, centerShiftY, widthScale, heightScale)` |
| `InverseView` | Matrix4x4 | Camera local-to-world transform |
| `DepthRange` | Vector2 | `(near, far)` depth clamp values |

**Example VFX YAML**:
```yaml
m_ExposedName: DepthMap
m_Type: UnityEngine.Texture2D

m_ExposedName: RayParams
m_Type: UnityEngine.Vector4

m_ExposedName: InverseView
m_Type: UnityEngine.Matrix4x4
```

---

### 3.3 DisplacedMeshBuilder.cs

**File**: `/tmp/MetavidoVFX/Packages/jp.keijiro.metavido.vfxgraph/Scripts/DisplacedMeshBuilder.cs`

**Purpose**: Creates a displaced mesh from depth texture for non-particle VFX.

**MeshBuilder.compute Integration**:
```csharp
void LateUpdate()
{
    var meta = _decoder.Metadata;
    var ray = RenderUtils.RayParams(meta);
    var iview = RenderUtils.InverseView(meta);

    _compute.SetVector(ShaderID.RayParams, ray);
    _compute.SetMatrix(ShaderID.InverseView, iview);
    _compute.SetTexture(0, ShaderID.DepthTexture, _demuxer.DepthTexture);

    // Vertex reconstruction
    _compute.SetInts("Dims", ColumnCount, RowCount);
    _compute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
    _compute.DispatchThreads(0, ColumnCount, RowCount);  // Uses extension method

    // Index array generation
    _compute.SetBuffer(1, "IndexBuffer", _indexBuffer);
    _compute.DispatchThreads(1, ColumnCount - 1, RowCount - 1);
}
```

**DispatchThreads Extension**:
```csharp
public static void DispatchThreads(this ComputeShader compute, int kernel, int x, int y = 1, int z = 1)
{
    uint xc, yc, zc;
    compute.GetKernelThreadGroupSizes(kernel, out xc, out yc, out zc);
    x = (x + (int)xc - 1) / (int)xc;  // Ceiling division
    y = (y + (int)yc - 1) / (int)yc;
    z = (z + (int)zc - 1) / (int)zc;
    compute.Dispatch(kernel, x, y, z);
}
```

**Same pattern used in our ARDepthSource** for dynamic thread group queries.

---

### 3.4 MeshBuilder.compute

**File**: `/tmp/MetavidoVFX/Packages/jp.keijiro.metavido.vfxgraph/Shaders/MeshBuilder.compute`

**Purpose**: GPU-based mesh generation from depth texture.

**Vertex Kernel**:
```hlsl
float3 GetVertexPosition(float2 uv)
{
    float depth = tex2Dlod(_DepthTexture, float4(uv, 0, 0)).x;
    return mtvd_DistanceToWorldPosition(uv, depth, _RayParams, _InverseView);
}

[numthreads(8, 8, 1)]
void VertexKernel(uint2 id : SV_DispatchThreadID)
{
    if (any(id >= Dims)) return;

    float2 uv = (float2)id / (Dims - 1);
    float3 d = float3(2.0 / (Dims - 1), 0);

    // Central position
    float3 p = GetVertexPosition(uv);

    // Neighbors for normal calculation
    float3 p_mdx = GetVertexPosition(uv - d.xz);
    float3 p_pdx = GetVertexPosition(uv + d.xz);
    float3 p_mdy = GetVertexPosition(uv - d.zy);
    float3 p_pdy = GetVertexPosition(uv + d.zy);

    // Compute normal via cross product
    float3 n = normalize(cross(p_pdy - p_mdy, p_pdx - p_mdx));

    WriteVertex(Dims.x * id.y + id.x, p, n, uv);
}
```

**Index Kernel**:
```hlsl
[numthreads(8, 8, 1)]
void IndexKernel(uint2 id : SV_DispatchThreadID)
{
    if (any(id >= Dims - 1)) return;

    uint quad_i = id.y * (Dims.x - 1) + id.x;
    uint i = quad_i * 3 * 2;  // 2 triangles per quad
    uint vi = Dims.x * id.y + id.x;

    // Triangle 1
    WriteIndices(i + 0, uint3(vi, vi + Dims.x, vi + 1));
    // Triangle 2
    WriteIndices(i + 3, uint3(vi + Dims.x, vi + Dims.x + 1, vi + 1));
}
```

---

## 4. Key Patterns & Best Practices

### 4.1 Property Naming Conventions

| Property Type | Metavido Name | ARFoundation Equivalent | Type |
|--------------|---------------|-------------------------|------|
| RGB Texture | `ColorMap` | `ColorTexture` | Texture2D |
| Depth Texture | `DepthMap` | `DepthTexture` | Texture2D |
| Camera Params | `RayParams` | `DisplayTransform` (different) | Vector4 |
| View Matrix | `InverseView` | `InverseView` | Matrix4x4 |
| Depth Range | `DepthRange` | N/A | Vector2 |

### 4.2 ExposedProperty Pattern

**Correct** (Keijiro's approach):
```csharp
[VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
ExposedProperty _depthMapProperty = "DepthMap";

component.SetTexture(_depthMapProperty, texture);
```

**Incorrect** (string literals):
```csharp
const string DepthMap = "DepthMap";
component.SetTexture(DepthMap, texture);  // ❌ May fail in VFX Graph
```

### 4.3 RayParams Computation

**Metavido Formula**:
```csharp
var s = meta.CenterShift;  // From projection matrix m02, m12
var h = Mathf.Tan(meta.FieldOfView / 2);
return new Vector4(s.x, s.y, h * 16 / 9, h);
```

**ARFoundation Equivalent** (for reference):
```csharp
// ARFoundation uses DisplayTransform instead
var displayTransform = screenRotation * imageAspectRatioCorrectionMatrix;
// RayParams would need to be computed from XRCameraParams.screenWidth/Height
```

### 4.4 UV → World Position Algorithm

**Standard Pipeline**:
```hlsl
// 1. UV [0,1] → NDC [-1,1]
float3 ray = float3((UV - 0.5) * 2, 1);

// 2. Apply camera intrinsics
ray.xy = (ray.xy + RayParams.xy) * RayParams.zw;
//        ^^^^^^^^^^^^^^^^^^^^^^   ^^^^^^^^^^^^^^
//        Center shift (optical axis)  FOV scale

// 3. Scale by depth
ray *= Depth;

// 4. Transform to world space
float3 worldPos = mul(InverseView, float4(ray, 1)).xyz;
```

### 4.5 Compute Shader Thread Dispatch

**Dynamic Thread Group Query** (preferred):
```csharp
public static void DispatchThreads(this ComputeShader compute, int kernel, int x, int y = 1, int z = 1)
{
    uint xc, yc, zc;
    compute.GetKernelThreadGroupSizes(kernel, out xc, out yc, out zc);
    x = (x + (int)xc - 1) / (int)xc;  // Ceiling division avoids truncation
    y = (y + (int)yc - 1) / (int)yc;
    z = (z + (int)zc - 1) / (int)zc;
    compute.Dispatch(kernel, x, y, z);
}
```

**Used in**:
- `DisplacedMeshBuilder.cs` (MetavidoVFX)
- `ARDepthSource.cs` (our implementation)

---

## 5. URP Compatibility

### 5.1 VFX Graph Requirements

- **Unity Version**: Unity 6 (6000.0.0f1+)
- **VFX Graph**: 17.0.0+
- **Render Pipeline**: URP 17.0.0+ or HDRP

### 5.2 Shader Compatibility

**Demux.shader** uses `UnityCG.cginc` (Built-in RP):
```hlsl
#include "UnityCG.cginc"  // Contains UnityObjectToClipPos()
```

**For URP Migration**, replace with:
```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// UnityObjectToClipPos() → TransformObjectToHClip()
```

### 5.3 Graphics.Blit Alternative

`TextureDemuxer.cs` uses `Graphics.Blit()` which is deprecated in URP:
```csharp
Graphics.Blit(source, _color, _material, 0);  // Works but legacy
```

**URP Alternative**:
```csharp
// Use CommandBuffer + Blitter API
var cmd = CommandBufferPool.Get("Demux");
Blitter.BlitCameraTexture(cmd, source, _color, _material, 0);
Graphics.ExecuteCommandBuffer(cmd);
CommandBufferPool.Release(cmd);
```

---

## 6. Comparison with Our Implementation

### 6.1 Similarities

| Feature | Keijiro (Metavido) | Our (MetavidoVFX-main) |
|---------|-------------------|----------------------|
| **Binder Pattern** | `VFXMetavidoBinder` | `VFXARBinder` |
| **Property Names** | `ColorMap`, `DepthMap` | `ColorTexture`, `DepthTexture` |
| **ExposedProperty** | ✅ Uses `ExposedProperty` | ✅ Uses `ExposedProperty` |
| **Compute Dispatch** | `DispatchThreads()` extension | `DispatchThreads()` in `ARDepthSource` |
| **UV → Position** | `MetavidoInverseProjection()` | Custom operators in VFX |
| **Performance** | Single-pass demux | Hybrid Bridge (O(1) scaling) |

### 6.2 Key Differences

| Aspect | Keijiro (Metavido) | Our (ARFoundation) |
|--------|-------------------|-------------------|
| **Data Source** | `.metavido` video files | Live AR camera (ARFoundation) |
| **Metadata** | Burnt-in barcode | `XRCameraFrame`, `AROcclusionManager` |
| **Depth Format** | Hue-encoded (rainbow) | Native ARFoundation depth (R16/RFloat) |
| **Stencil** | Human segmentation | ARFoundation segmentation |
| **Camera Params** | `RayParams` (center shift + FOV) | `DisplayTransform` (screen rotation + aspect) |
| **Platform** | Unity 6, WebGPU | Unity 6, iOS/Android/Quest |
| **Update Model** | Per-frame video decode | Per-frame AR texture access |

### 6.3 Architecture Mapping

| Metavido | ARFoundation Equivalent |
|----------|------------------------|
| `MetadataDecoder` | `XRCameraFrame.TryGetTimestamp()` |
| `TextureDemuxer` | `AROcclusionManager.environmentDepthTexture` |
| `RenderUtils.RayParams()` | Custom from `XRCameraParams` |
| `RenderUtils.InverseView()` | `Camera.main.cameraToWorldMatrix` |
| `Demux.shader` | Not needed (native depth access) |
| `VFXMetavidoBinder` | `VFXARBinder` |

---

## 7. Actionable Insights

### 7.1 Property Naming Strategy

**Recommendation**: Align with Keijiro's conventions for VFX Graph compatibility:

**Current (AR-specific)**:
```csharp
ColorTexture, DepthTexture, StencilTexture
```

**Alternative (Metavido-compatible)**:
```csharp
ColorMap, DepthMap, StencilMap
```

**Hybrid Approach** (support both):
```csharp
// VFXARBinder.cs
[VFXPropertyBinding("UnityEngine.Texture2D")]
ExposedProperty _colorProperty = "ColorTexture";  // Or "ColorMap"

public string ColorPropertyName
{
    get => (string)_colorProperty;
    set => _colorProperty = value;
}
```

### 7.2 RayParams for AR

**Compute RayParams from ARFoundation**:
```csharp
public static Vector4 ComputeRayParams(XRCameraParams cameraParams)
{
    var fov = cameraParams.zNear != 0
        ? Mathf.Atan(cameraParams.screenHeight / (2f * cameraParams.zNear)) * 2f
        : 60f * Mathf.Deg2Rad;

    var h = Mathf.Tan(fov / 2);
    var aspect = cameraParams.screenWidth / cameraParams.screenHeight;

    // Center shift (typically 0,0 for ARFoundation)
    var sx = 0f;
    var sy = 0f;

    return new Vector4(sx, sy, h * aspect, h);
}
```

### 7.3 Depth → World Position Operator

**Create VFX Operator** (`AR Inverse Projection.vfxoperator`):
```hlsl
float3 ARInverseProjection(float2 UV, float Depth, float4 RayParams, float4x4 InverseView)
{
    float3 p = float3(UV * 2 - 1, 1);
    p.xy = (p.xy + RayParams.xy) * RayParams.zw;
    p *= Depth;
    return mul(InverseView, float4(p, 1)).xyz;
}
```

**Use in VFX Graph**:
```yaml
Initialize Particle
  ├─ Sample Texture (DepthTexture) at UV → Depth
  ├─ AR Inverse Projection(UV, Depth, RayParams, InverseView) → position
  └─ Sample Texture (ColorTexture) at UV → color
```

### 7.4 Mesh Builder Pattern

**For non-particle VFX**, adapt `DisplacedMeshBuilder.cs`:

**Our Version** (`ARMeshBuilder.cs`):
```csharp
public class ARMeshBuilder : MonoBehaviour
{
    [SerializeField] ARDepthSource _depthSource;
    [SerializeField] ComputeShader _meshCompute;
    [SerializeField, Range(0, 31)] int _decimation = 7;

    void LateUpdate()
    {
        if (!_depthSource.IsTextureReady) return;

        var depthTex = _depthSource.DepthTexture;
        var (w, h) = (depthTex.width / (_decimation + 1),
                     depthTex.height / (_decimation + 1));

        // Compute RayParams from AR camera
        var rayParams = ComputeRayParams();
        var iview = Camera.main.cameraToWorldMatrix;

        _meshCompute.SetVector("_RayParams", rayParams);
        _meshCompute.SetMatrix("_InverseView", iview);
        _meshCompute.SetTexture(0, "_DepthTexture", depthTex);

        // Dispatch vertex reconstruction
        _meshCompute.SetInts("Dims", w, h);
        _meshCompute.SetBuffer(0, "VertexBuffer", _vertexBuffer);
        _meshCompute.DispatchThreads(0, w, h);

        // Dispatch index generation
        _meshCompute.SetBuffer(1, "IndexBuffer", _indexBuffer);
        _meshCompute.DispatchThreads(1, w - 1, h - 1);
    }
}
```

---

## 8. Critical Findings

### 8.1 Burnt-In Metadata Innovation

Keijiro's **barcode metadata encoding** is genius for synchronizing camera tracking with video frames **without external files**. This eliminates drift and sync issues.

**For Live AR**: We don't need this - we have direct access to `XRCameraFrame` metadata.

### 8.2 Hue Depth Encoding

**Why Hue?** Compression-resistant encoding:
- Depth → Hue (0-360°) → RGB rainbow gradient
- JPEG/H.264 compression preserves hue better than intensity
- Padding/margin prevent wraparound artifacts

**For Live AR**: We use native R16/RFloat depth textures (no encoding needed).

### 8.3 UV Multiplexing

**Frame Layout** saves bandwidth:
- Single 1920x1080 video contains color + depth + stencil + metadata
- No separate files, perfect sync guaranteed
- Trade-off: Half resolution for color (960x1080 vs 1920x1080)

**For Live AR**: We get separate textures from ARFoundation (no multiplexing).

### 8.4 ExposedProperty Pattern

**Critical for VFX Graph**:
```csharp
// ✅ Correct - Type-safe property binding
[VFXPropertyBinding("UnityEngine.Texture2D")]
ExposedProperty _depthMapProperty = "DepthMap";

// ❌ Incorrect - May fail in VFX Graph
const string DepthMap = "DepthMap";
```

**Reason**: VFX Graph internally uses `ExposedProperty` struct for property resolution. String literals may work but are not guaranteed.

---

## 9. Performance Notes

### 9.1 Demux Cost

**Per-frame operations**:
- 2x `Graphics.Blit()` calls (color + depth)
- Hue decoding on GPU (minimal cost)
- RenderTexture allocation (lazy, one-time)

**Optimization**: Could combine into single pass with MRT (Multiple Render Targets).

### 9.2 Mesh Builder Cost

**Compute shader dispatch**:
- Vertex kernel: `(w/8) * (h/8)` thread groups
- Index kernel: `((w-1)/8) * ((h-1)/8)` thread groups
- Decimation = 7 → 240x135 vertices (good balance)

**Optimization**: Already optimal with dynamic thread groups.

### 9.3 VFX Graph Cost

**Particle initialization**:
- Sample DepthMap (1 texture read)
- Sample ColorMap (1 texture read)
- Inverse projection (5 MAD ops + 1 matrix multiply)

**Per VFX**: ~3-5 texture samples, 10-20 ALU ops.

**Our Hybrid Bridge**: Optimizes this by centralizing compute dispatch in `ARDepthSource`.

---

## 10. Integration Recommendations

### 10.1 For MetavidoVFX-main Project

**Adopt from Keijiro**:
1. ✅ **ExposedProperty Pattern** - Already using in `VFXARBinder`
2. ✅ **DispatchThreads Extension** - Already using in `ARDepthSource`
3. ⬜ **RayParams Computation** - Add for AR camera parameters
4. ⬜ **Inverse Projection Operator** - Create VFX subgraph operator
5. ⬜ **Mesh Builder Pattern** - For non-particle VFX use cases

**Don't Adopt**:
- ❌ Demux shader (we have native textures)
- ❌ Hue encoding (we have native depth)
- ❌ UV multiplexing (ARFoundation provides separate textures)

### 10.2 New VFX Operators to Create

**File**: `Assets/VFX/Operators/AR Inverse Projection.vfxoperator`
```hlsl
float3 ARInverseProjection(float2 UV, float Depth, float4 RayParams, float4x4 InverseView)
{
    float3 p = float3(UV * 2 - 1, 1);
    p.xy = (p.xy + RayParams.xy) * RayParams.zw;
    p *= Depth;
    return mul(InverseView, float4(p, 1)).xyz;
}
```

**File**: `Assets/VFX/Operators/Compute RayParams.vfxoperator`
```hlsl
float4 ComputeRayParams(float FOV, float Aspect)
{
    float h = tan(FOV / 2);
    return float4(0, 0, h * Aspect, h);  // Assume center shift = 0
}
```

### 10.3 Documentation Updates

**Add to** `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`:

**Section**: "Depth to World Position Conversion"
```markdown
### UV → World Position (Keijiro Pattern)

float3 ARInverseProjection(float2 UV, float Depth, float4 RayParams, float4x4 InverseView)
{
    float3 p = float3(UV * 2 - 1, 1);              // UV [0,1] → NDC [-1,1]
    p.xy = (p.xy + RayParams.xy) * RayParams.zw;   // Apply camera intrinsics
    p *= Depth;                                    // Scale by depth
    return mul(InverseView, float4(p, 1)).xyz;     // Camera → world space
}

RayParams = (centerShiftX, centerShiftY, tanFOV * aspect, tanFOV)
InverseView = Camera.cameraToWorldMatrix
```

---

## 11. References

### 11.1 GitHub Repositories

- **MetavidoVFX**: https://github.com/keijiro/MetavidoVFX
- **Metavido**: https://github.com/keijiro/Metavido

### 11.2 Key Files Analyzed

**C# Scripts**:
- `VFXMetavidoBinder.cs` - VFX property binder
- `RenderUtils.cs` - RayParams/InverseView computation
- `Metadata.cs` - Burnt-in metadata struct
- `TextureDemuxer.cs` - Texture splitting
- `DisplacedMeshBuilder.cs` - Mesh generation
- `Util.cs` - Extension methods

**Shaders**:
- `Common.hlsl` - Depth encoding, UV remapping
- `Utils.hlsl` - World position reconstruction
- `Demux.shader` - Texture demultiplexing
- `MeshBuilder.compute` - Mesh generation

**VFX Operators**:
- `Metavido Inverse Projection.vfxoperator` - UV → world position
- `Metavido Sample UV.vfxblock` - Texture sampling
- `Metavido Filter.vfxblock` - Depth filtering

**VFX Assets**:
- `Particles.vfx` - Particle-based visualization
- `Voxels.vfx` - Voxel-based visualization
- `Afterimage.vfx` - Motion trail effect

### 11.3 Unity Documentation

- VFX Graph Property Binders: https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest
- ExposedProperty API: Unity Scripting Reference
- ComputeShader.GetKernelThreadGroupSizes(): https://docs.unity3d.com/ScriptReference/ComputeShader.GetKernelThreadGroupSizes.html

---

## 12. Conclusion

Keijiro's MetavidoVFX demonstrates **production-grade VFX Graph architecture** for volumetric video. The key innovations are:

1. **Burnt-in barcode metadata** - Eliminates sync issues
2. **UV multiplexing** - Single video contains all data
3. **Hue depth encoding** - Compression-resistant depth
4. **ExposedProperty pattern** - Type-safe VFX bindings
5. **RayParams formula** - Canonical UV → world position
6. **Compute shader mesh builder** - GPU-accelerated geometry

**For our ARFoundation project**, we should **adopt the patterns** but not the video-specific features. The `ARInverseProjection` operator and `RayParams` computation are immediately actionable improvements.

**Performance**: Keijiro's approach is O(n) per VFX (each VFX samples textures independently). Our Hybrid Bridge is O(1) across all VFX (single compute dispatch). Both are valid - Keijiro prioritizes flexibility, we prioritize scaling.

**Next Steps**:
1. Create `AR Inverse Projection.vfxoperator` based on Keijiro's HLSL
2. Add `RayParams` property to `VFXARBinder`
3. Update VFX assets to use new operators
4. Document patterns in `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`

---

**Research Completed**: 2026-01-20
**Files Created**: `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_KEIJIRO_METAVIDO_VFX_RESEARCH.md`
**Status**: ✅ Research-only (no code modifications)
