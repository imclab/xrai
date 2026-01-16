// RcamCommon.hlsl - Local shim for Rcam3/Rcam4 package compatibility
// Provides Rcam-compatible functions using Metavido equivalents
// Created 2026-01-15 to resolve missing package dependencies

#ifndef __INCLUDE_RCAM_COMMON_LOCAL_HLSL__
#define __INCLUDE_RCAM_COMMON_LOCAL_HLSL__

#include "Packages/jp.keijiro.metavido/Common/Shaders/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// ============================================================================
// UV/Texture Coordinate Functions
// ============================================================================

// RcamUV2TC - Convert UV coordinates to texture coordinates for Texture.Load()
// Parameters:
//   tex: Source texture
//   uv: UV coordinates (0-1)
//   scale: UV scale factor
//   offset: UV offset
// Returns: int3 texture coordinates for Load()
int3 RcamUV2TC(UnityTexture2D tex, float2 uv, float2 scale, float2 offset)
{
    float2 size = float2(tex.texelSize.zw); // texelSize.zw = texture dimensions
    float2 scaledUV = uv * scale + offset;
    int2 tc = int2(scaledUV * size);
    return int3(tc, 0);
}

// Overloads for mixed scalar/vector parameters
int3 RcamUV2TC(UnityTexture2D tex, float2 uv, float scale, float2 offset)
{
    return RcamUV2TC(tex, uv, float2(scale, scale), offset);
}

int3 RcamUV2TC(UnityTexture2D tex, float2 uv, float2 scale, float offset)
{
    return RcamUV2TC(tex, uv, scale, float2(offset, offset));
}

int3 RcamUV2TC(UnityTexture2D tex, float2 uv, float scale, float offset)
{
    return RcamUV2TC(tex, uv, float2(scale, scale), float2(offset, offset));
}

// ============================================================================
// Depth Functions
// ============================================================================

// RcamDecodeDepth - Decode depth from hue-encoded RGB
// Wraps Metavido's mtvd_DecodeDepth
float RcamDecodeDepth(float3 rgb, float2 range)
{
    return mtvd_DecodeDepth(rgb, range);
}

// RcamDistanceToDepth - Convert linear distance to Z-buffer depth
// Uses Unity's _ZBufferParams (must be available in shader)
float RcamDistanceToDepth(float d)
{
    // For URP/HDRP, depth is typically reversed-Z
    // d = linear distance, output = Z-buffer value
    return (1.0 / d - _ZBufferParams.w) / _ZBufferParams.z;
}

// ============================================================================
// World Position Functions
// ============================================================================

// RcamDistanceToWorldPosition - Convert UV + distance to world position
// Parameters:
//   uv: Screen UV (0-1)
//   distance: Linear depth/distance
//   invProjection: Vector4 (offsetX, offsetY, tan(fov/2)*aspect, tan(fov/2))
//   invView: Inverse view matrix
float3 RcamDistanceToWorldPosition(float2 uv, float distance, float4 invProjection, float4x4 invView)
{
    // Convert UV to normalized device coords (-1 to 1)
    float3 ray = float3((uv - 0.5) * 2, 1);

    // Apply projection parameters (similar to RayParams in VFX)
    ray.xy = (ray.xy + invProjection.xy) * invProjection.zw;

    // Transform to world space
    return mul(invView, float4(ray * distance, 1)).xyz;
}

#endif // __INCLUDE_RCAM_COMMON_LOCAL_HLSL__
