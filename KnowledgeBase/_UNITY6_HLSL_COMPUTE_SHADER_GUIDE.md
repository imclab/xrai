# Unity 6 HLSL & Compute Shader Guide

**Date**: 2026-01-21
**Unity Version**: 6000.2.14f1
**Status**: Production Reference

---

## 1. Compute Shader Fundamentals

### Thread Group Sizing

```hlsl
// Large textures (depth maps) - 1024 threads = perfect Metal fit
[numthreads(32,32,1)]
void DepthToWorld(uint3 id : SV_DispatchThreadID) { ... }

// Mobile-friendly - 64 threads
[numthreads(8,8,1)]
void ProcessMesh(uint3 id : SV_DispatchThreadID) { ... }
```

### C# Dispatch Pattern

```csharp
int kernel = computeShader.FindKernel("DepthToWorld");
int threadGroupsX = (width + 31) / 32;  // Ceil division
int threadGroupsY = (height + 31) / 32;
computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
```

---

## 2. Depth → World Position Pattern

Standard AR depth processing compute shader:

```hlsl
#pragma kernel GeneratePositionTexture

Texture2D<float> DepthTexture;
Texture2D<float> StencilTexture;
RWTexture2D<float4> PositionTexture;

float4x4 InvVPMatrix;
float4x4 ProjectionMatrix;
float4 DepthRange;  // x=min, y=max, z=stencilThreshold

float3 ViewportToWorldPoint(float3 position)
{
    float4 projW = mul(ProjectionMatrix, float4(0, 0, position.z, 1));
    float4 pos4 = float4(1.0 - 2.0 * position.x, 1.0 - 2.0 * position.y, projW.z / projW.w, 1);
    float4 res4 = mul(InvVPMatrix, pos4);
    return res4.xyz / res4.w;
}

[numthreads(32,32,1)]
void GeneratePositionTexture(uint3 id : SV_DispatchThreadID)
{
    float outWidth, outHeight;
    PositionTexture.GetDimensions(outWidth, outHeight);

    if (id.x >= (uint)outWidth || id.y >= (uint)outHeight) return;

    float2 uv = float2(float(id.x) / outWidth, float(id.y) / outHeight);

    // Sample depth with Y-flip for ARKit
    float depthWidth, depthHeight;
    DepthTexture.GetDimensions(depthWidth, depthHeight);
    uint2 depthCoord = uint2(uv.x * depthWidth, (1.0 - uv.y) * depthHeight);
    float depth = DepthTexture[depthCoord];

    // Stencil filtering
    float stencilWidth, stencilHeight;
    StencilTexture.GetDimensions(stencilWidth, stencilHeight);
    uint2 stencilCoord = uint2(uv.x * stencilWidth, (1.0 - uv.y) * stencilHeight);
    float stencil = StencilTexture[stencilCoord];

    if (depth < DepthRange.x || depth > DepthRange.y || stencil < DepthRange.z)
    {
        PositionTexture[id.xy] = float4(0, -1000.0, 0, 0);
        return;
    }

    float3 worldPos = ViewportToWorldPoint(float3(uv, depth));
    PositionTexture[id.xy] = float4(worldPos, 1.0);
}
```

---

## 3. VFX Graph Custom HLSL

### Function Signature (Required)

```hlsl
// ✅ CORRECT - void return, inout VFXAttributes
void CustomFunction(inout VFXAttributes attributes)
{
    attributes.position += float3(0, 1, 0);
}

// ✅ CORRECT - additional parameters allowed
void CustomFunction(inout VFXAttributes attributes, in float Intensity)
{
    attributes.position *= Intensity;
}

// ❌ WRONG - no return values
float3 GetOffset() { return float3(0, 1, 0); }
```

### Texture Sampling

```hlsl
// ✅ CORRECT - SampleLevel required (no derivatives in compute)
float4 color = myTexture.SampleLevel(mySampler, uv, 0);

// ❌ WRONG - Sample() not available
float4 color = myTexture.Sample(mySampler, uv);
```

### Conditional Attributes

```hlsl
#if VFX_HAS_ATTR_VELOCITY
    attributes.velocity = float3(0, 1, 0);
#endif

#if VFX_HAS_ATTR_COLOR
    attributes.color = float4(1, 0, 0, 1);
#endif
```

### Common VFX HLSL Errors

| Error | Cause | Fix |
|-------|-------|-----|
| `undeclared identifier` | Missing function/include | Inline the function or add include |
| `cannot convert VFXAttributes to Texture2D` | External file function mismatch | Set m_ShaderFile to {fileID: 0} |
| `Sample() not found` | Wrong texture sample method | Use SampleLevel(..., 0) |

---

## 4. External HLSL File Reference

### VFX Graph m_ShaderFile

```yaml
# References external file - VFX sees ALL functions
m_ShaderFile: {fileID: 10900000, guid: abc123..., type: 3}

# No external file - uses inline code only
m_ShaderFile: {fileID: 0}
```

**Warning**: When referencing external files, VFX Graph may try to use functions with wrong parameter types. If your inline code doesn't need external functions, set `m_ShaderFile: {fileID: 0}`.

### Include Paths

```hlsl
// Relative to VFX asset
#include "MyShaders/Utils.hlsl"

// Absolute from Assets
#include "Assets/Shaders/Common.hlsl"

// Absolute from Packages
#include "Packages/com.mypackage/Shaders/Utils.hlsl"
```

---

## 5. Metal (iOS) Considerations

### Thread Group Limits

```hlsl
// Metal limit: X * Y * Z <= 1024
[numthreads(32,32,1)]  // 1024 threads - max
[numthreads(8,8,1)]    // 64 threads - conservative
```

### ARKit Texture Rotation

```hlsl
// ARKit depth is landscape, app may be portrait
// Rotate 90° CW: (u,v) → (1-v, u)
float2 rotatedUV = float2(1.0 - uv.y, uv.x);
```

### Precision

```hlsl
half4 color;      // 16-bit - faster on mobile
float4 position;  // 32-bit - needed for positions/matrices
```

---

## 6. GraphicsBuffer for VFX

### C# Setup

```csharp
int pointCount = width * height;
var posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, pointCount, sizeof(float) * 3);

// Set in compute shader
computeShader.SetBuffer(kernel, "PositionBuffer", posBuffer);

// Set in VFX Graph
visualEffect.SetGraphicsBuffer("PositionBuffer", posBuffer);

// Cleanup
void OnDestroy() => posBuffer?.Dispose();
```

### Compute Shader

```hlsl
RWStructuredBuffer<float3> PositionBuffer;

[numthreads(8,8,1)]
void FillBuffer(uint3 id : SV_DispatchThreadID)
{
    uint index = id.y * Width + id.x;
    PositionBuffer[index] = float3(id.x, id.y, 0);
}
```

---

## 7. Safe AR Texture Access

```csharp
// ❌ WRONG - crashes when AR subsystem not ready
var depth = occlusionManager?.humanDepthTexture;

// ✅ CORRECT - TryGetTexture pattern
Texture TryGetTexture(System.Func<Texture> getter)
{
    try { return getter?.Invoke(); }
    catch { return null; }
}
var depth = TryGetTexture(() => occlusionManager.humanDepthTexture);
```

---

## 8. Platform Compatibility

| Feature | iOS Metal | Android Vulkan | Quest | WebGL |
|---------|-----------|----------------|-------|-------|
| Compute Shaders | ✅ | ✅ | ✅ | ⚠️ WebGPU only |
| VFX Custom HLSL | ✅ | ✅ | ✅ | ❌ |
| GraphicsBuffer | ✅ | ✅ | ✅ | ⚠️ Limited |
| RWTexture2D | ✅ | ✅ | ✅ | ⚠️ Limited |

---

## 9. Performance

From MetavidoVFX benchmarks (Jan 2026):

| Operation | GPU Time | Notes |
|-----------|----------|-------|
| DepthToWorld (256x192) | ~0.5ms | Single dispatch |
| Position + Velocity | ~1.2ms | Two-pass |
| 10 VFX (Hybrid Bridge) | ~2.8ms | O(1) compute |

**Target**: <3ms compute budget for 60fps

---

## References

- [Unity 6 Compute Shaders](https://docs.unity3d.com/6000.2/Documentation/Manual/class-ComputeShader-hlsl-shaderlab.html)
- [VFX Graph Custom HLSL](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/Block-CustomHLSL.html)
- [Metal Thread Group Limits](https://developer.apple.com/documentation/metal/compute_passes/calculating_threadgroup_and_grid_sizes)

---

*Last Updated: 2026-01-21*
