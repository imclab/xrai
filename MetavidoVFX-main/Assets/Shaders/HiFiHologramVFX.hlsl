// ===========================================================================
// HiFi Hologram VFX - Custom HLSL for Optimized Point Cloud
// ===========================================================================
// Uses PositionMap (precomputed by ARDepthSource) + ColorMap for RGB color.
// No redundant computation - positions are read directly from texture.
//
// Usage in VFX Graph:
// 1. Add exposed properties: PositionMap (Texture2D), ColorMap (Texture2D)
// 2. In Initialize Particle, use Custom HLSL with these functions
// 3. VFXARBinder will bind the textures automatically
// ===========================================================================

#ifndef _HIFI_HOLOGRAM_VFX_H_
#define _HIFI_HOLOGRAM_VFX_H_

// ---------------------------------------------------------------------------
// Sample Position from PositionMap
// ---------------------------------------------------------------------------
// Input: PositionMap texture, UV coordinates
// Output: World position (xyz)
//
// VFX Graph Custom HLSL setup:
// - Input slots: PositionMap (Texture2D), UV (Vector2)
// - Output slot: Position (Vector3)
float3 SamplePositionMap(Texture2D positionMap, float2 uv)
{
    // Use Load for exact pixel sampling (no filtering)
    uint2 dims;
    positionMap.GetDimensions(dims.x, dims.y);
    int2 pixel = int2(uv * dims);
    return positionMap.Load(int3(pixel, 0)).xyz;
}

// ---------------------------------------------------------------------------
// Sample Color from ColorMap
// ---------------------------------------------------------------------------
// Input: ColorMap texture, UV coordinates
// Output: RGB color
//
// VFX Graph Custom HLSL setup:
// - Input slots: ColorMap (Texture2D), UV (Vector2)
// - Output slot: Color (Vector3)
float3 SampleColorMap(Texture2D colorMap, float2 uv)
{
    uint2 dims;
    colorMap.GetDimensions(dims.x, dims.y);
    int2 pixel = int2(uv * dims);
    return colorMap.Load(int3(pixel, 0)).rgb;
}

// ---------------------------------------------------------------------------
// Sample Position and Color together (single function)
// ---------------------------------------------------------------------------
// Input: Both textures, UV coordinates
// Output: Position (xyz) in first 3 components, Color (rgb) returned separately
//
// Use this when you need both position and color from the same UV
void SamplePositionAndColor(
    Texture2D positionMap,
    Texture2D colorMap,
    float2 uv,
    out float3 position,
    out float3 color)
{
    uint2 posDims, colorDims;
    positionMap.GetDimensions(posDims.x, posDims.y);
    colorMap.GetDimensions(colorDims.x, colorDims.y);

    int2 posPixel = int2(uv * posDims);
    int2 colorPixel = int2(uv * colorDims);

    position = positionMap.Load(int3(posPixel, 0)).xyz;
    color = colorMap.Load(int3(colorPixel, 0)).rgb;
}

// ---------------------------------------------------------------------------
// Check if position is valid (not at origin or very far)
// ---------------------------------------------------------------------------
bool IsValidPosition(float3 position, float maxDistance)
{
    float dist = length(position);
    return dist > 0.01 && dist < maxDistance;
}

// ---------------------------------------------------------------------------
// Generate UV from particle index (for GPU spawning)
// ---------------------------------------------------------------------------
// Input: Particle index, texture dimensions
// Output: UV coordinates for this particle
float2 IndexToUV(uint particleIndex, uint2 dimensions)
{
    uint x = particleIndex % dimensions.x;
    uint y = particleIndex / dimensions.x;
    return (float2(x, y) + 0.5) / float2(dimensions);
}

// ---------------------------------------------------------------------------
// Complete HiFi Hologram sampling (all-in-one)
// ---------------------------------------------------------------------------
// This is the main function for the HiFi Hologram VFX.
// Call this in Initialize Particle to set position and color.
//
// VFX Graph Custom HLSL setup:
// - Input slots: PositionMap, ColorMap, ParticleIndex, MapWidth, MapHeight
// - Output slots: Position (Vector3), Color (Vector3), IsValid (bool)
void HiFiHologramSample(
    Texture2D positionMap,
    Texture2D colorMap,
    uint particleIndex,
    float mapWidth,
    float mapHeight,
    float maxDistance,
    out float3 position,
    out float3 color,
    out bool isValid)
{
    // Convert index to UV
    uint2 dims = uint2(mapWidth, mapHeight);
    float2 uv = IndexToUV(particleIndex, dims);

    // Sample position and color
    SamplePositionAndColor(positionMap, colorMap, uv, position, color);

    // Validate position
    isValid = IsValidPosition(position, maxDistance);
}

// ---------------------------------------------------------------------------
// Simplified version for VFX Graph (returns float4 with position.xyz, alpha)
// ---------------------------------------------------------------------------
float4 HiFiSamplePosition(Texture2D positionMap, float2 uv)
{
    float3 pos = SamplePositionMap(positionMap, uv);
    float alpha = IsValidPosition(pos, 10.0) ? 1.0 : 0.0;
    return float4(pos, alpha);
}

float4 HiFiSampleColor(Texture2D colorMap, float2 uv)
{
    float3 col = SampleColorMap(colorMap, uv);
    return float4(col, 1.0);
}

#endif
