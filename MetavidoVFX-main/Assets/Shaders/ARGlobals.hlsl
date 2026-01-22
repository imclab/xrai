#ifndef _AR_GLOBALS_H_
#define _AR_GLOBALS_H_

// ===========================================================================
// AR Globals - Global shader properties for VFX Graph HLSL
// ===========================================================================
// These properties are set by C# scripts and accessible in Custom HLSL blocks.
// NOTE: Textures do NOT work globally for VFX Graph - use SetTexture() instead.
// ===========================================================================

// ---------------------------------------------------------------------------
// AR Camera Properties (set by ARDepthSource.cs)
// ---------------------------------------------------------------------------
float4 _ARRayParams;      // (0, 0, tan(fov/2)*aspect, tan(fov/2)) for UV→ray
float4x4 _ARInverseView;  // Camera inverse view matrix for ray→world

// ---------------------------------------------------------------------------
// Audio Properties (set by AudioBridge.cs)
// ---------------------------------------------------------------------------
float4 _AudioBands;       // (bass * 100, mids * 100, treble * 100, subBass * 100)
float _AudioVolume;       // 0-1 overall volume
float _BeatPulse;         // 0-1 decaying beat pulse (resets to 1 on beat, decays to 0)
float _BeatIntensity;     // 0-1 strength of last detected beat

// ---------------------------------------------------------------------------
// AUDIO HELPER FUNCTIONS
// Use these in VFX Graph Custom HLSL Operators
// ---------------------------------------------------------------------------

// Get normalized audio volume (0-1)
float GetAudioVolume()
{
    return saturate(_AudioVolume);
}

// Get bass frequency band (0-1, normalized from *100 scaling)
float GetAudioBass()
{
    return saturate(_AudioBands.x * 0.01);
}

// Get mid frequency band (0-1)
float GetAudioMid()
{
    return saturate(_AudioBands.y * 0.01);
}

// Get treble frequency band (0-1)
float GetAudioTreble()
{
    return saturate(_AudioBands.z * 0.01);
}

// Get sub-bass frequency band (0-1)
float GetAudioSubBass()
{
    return saturate(_AudioBands.w * 0.01);
}

// Get beat pulse (0-1, decays after each beat)
float GetBeatPulse()
{
    return saturate(_BeatPulse);
}

// Get beat intensity (0-1, strength of last beat)
float GetBeatIntensity()
{
    return saturate(_BeatIntensity);
}

// Get all audio as float4 for efficient sampling
// Returns: (volume, bass, mid, treble)
float4 GetAudioData()
{
    return float4(
        saturate(_AudioVolume),
        saturate(_AudioBands.x * 0.01),
        saturate(_AudioBands.y * 0.01),
        saturate(_AudioBands.z * 0.01)
    );
}

// Get audio-reactive scale factor based on volume/bass
// Useful for scaling particle size, emission rate, etc.
float GetAudioReactiveScale(float baseScale, float sensitivity)
{
    float audio = saturate(_AudioVolume + _AudioBands.x * 0.02); // bias toward bass
    return baseScale * (1.0 + audio * sensitivity);
}

// Get beat-reactive pulse for one-shot effects
// Returns 1.0 on beat onset, smoothly decays to 0
float GetBeatReactivePulse()
{
    return _BeatPulse * _BeatPulse; // squared for snappier decay
}

// ---------------------------------------------------------------------------
// AUDIO DATA TEXTURE SAMPLING (for VFX without exposed properties)
// ---------------------------------------------------------------------------
// AudioDataTexture is a 2x2 RGBAFloat texture set by VFXAudioDataBinder:
//   Pixel (0,0) at UV (0.25, 0.25): (Volume, Bass, Mids, Treble)
//   Pixel (1,0) at UV (0.75, 0.25): (SubBass, BeatPulse, BeatIntensity, 0)
//
// To use in VFX Graph Custom HLSL:
// 1. Add "AudioDataTexture" as a Texture2D exposed property in VFX Graph
// 2. Pass the texture to your Custom HLSL operator
// 3. Use SampleAudioDataTexture() functions below

// Sample primary audio data from AudioDataTexture
// Returns: (Volume, Bass, Mids, Treble)
float4 SampleAudioDataTexture(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.25, 0.25), 0);
}

// Sample extended audio data from AudioDataTexture
// Returns: (SubBass, BeatPulse, BeatIntensity, 0)
float4 SampleAudioDataTextureExtended(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.75, 0.25), 0);
}

// Sample specific audio values from AudioDataTexture
float SampleAudioVolume(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.25, 0.25), 0).r;
}

float SampleAudioBass(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.25, 0.25), 0).g;
}

float SampleAudioMid(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.25, 0.25), 0).b;
}

float SampleAudioTreble(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.25, 0.25), 0).a;
}

float SampleBeatPulse(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.75, 0.25), 0).g;
}

float SampleBeatIntensity(Texture2D tex, SamplerState samp)
{
    return tex.SampleLevel(samp, float2(0.75, 0.25), 0).b;
}

// ---------------------------------------------------------------------------
// AR TEXTURE GLOBALS (set by ARDepthSource.cs)
// ---------------------------------------------------------------------------
// ⚠️ CRITICAL: VFX Graph CANNOT read global textures (architectural limitation).
// These declarations are for MATERIAL SHADERS ONLY (MeshRenderer, etc.).
// For VFX Graph, use VFXARBinder which calls SetTexture() per-VFX.
//
// What DOES work globally for VFX Graph:
//   - Vectors: _ARRayParams, _ARDepthRange, _ARMapSize
//   - Matrices: _ARInverseView
//   - GraphicsBuffers (VFXProxBuffer pattern)
//
// What does NOT work for VFX Graph:
//   - Shader.SetGlobalTexture() - VFX property system doesn't expose these

TEXTURE2D(_ARDepthMap);      // Depth texture (RFloat, meters)
TEXTURE2D(_ARStencilMap);    // Human stencil (R8, 0=bg, 1=human)
TEXTURE2D(_ARPositionMap);   // World positions (ARGBFloat, xyz)
TEXTURE2D(_ARColorMap);      // Camera color (ARGB32, rgb)
TEXTURE2D(_ARVelocityMap);   // Velocity (ARGBFloat, xyz) - optional

float4 _ARDepthRange;        // (near, far, 0, 0)
float4 _ARMapSize;           // (width, height, 1/width, 1/height)

// Standard point sampler for AR textures
SamplerState ar_point_clamp_sampler;

// ---------------------------------------------------------------------------
// AR TEXTURE SAMPLING FUNCTIONS
// ---------------------------------------------------------------------------

// Sample depth value at UV (returns meters)
float SampleARDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ARDepthMap, ar_point_clamp_sampler, uv, 0).r;
}

// Sample human stencil (1 = human, 0 = background)
float SampleARStencil(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ARStencilMap, ar_point_clamp_sampler, uv, 0).r;
}

// Sample world position from pre-computed position map
float3 SampleARPosition(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ARPositionMap, ar_point_clamp_sampler, uv, 0).xyz;
}

// Sample AR camera color
float3 SampleARColor(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ARColorMap, ar_point_clamp_sampler, uv, 0).rgb;
}

// Sample velocity (if enabled in ARDepthSource)
float3 SampleARVelocity(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_ARVelocityMap, ar_point_clamp_sampler, uv, 0).xyz;
}

// Check if UV is within valid stencil region
bool IsHuman(float2 uv)
{
    return SampleARStencil(uv) > 0.5;
}

// Get AR map dimensions
float2 GetARMapSize()
{
    return _ARMapSize.xy;
}

// Convert pixel coords to UV
float2 PixelToUV(int2 pixel)
{
    return (float2(pixel) + 0.5) * _ARMapSize.zw;
}

// ---------------------------------------------------------------------------
// AR CAMERA HELPER FUNCTIONS
// ---------------------------------------------------------------------------

// Convert UV + depth to world position ray direction
float3 GetRayDirection(float2 uv)
{
    float2 p = (uv - 0.5) * 2.0;
    float3 ray = float3(p.x * _ARRayParams.z, p.y * _ARRayParams.w, 1.0);
    return normalize(mul((float3x3)_ARInverseView, ray));
}

// Get camera world position from inverse view matrix
float3 GetCameraPosition()
{
    return _ARInverseView._m03_m13_m23;
}

// Convert UV + depth to world position (alternative to position map)
float3 UVDepthToWorld(float2 uv, float depth)
{
    float2 ndc = uv * 2.0 - 1.0;
    ndc.x += _ARRayParams.x;
    ndc.y += _ARRayParams.y;

    float3 viewPos;
    viewPos.x = ndc.x * _ARRayParams.z * depth;
    viewPos.y = ndc.y * _ARRayParams.w * depth;
    viewPos.z = depth;

    return mul(_ARInverseView, float4(viewPos, 1.0)).xyz;
}

#endif
