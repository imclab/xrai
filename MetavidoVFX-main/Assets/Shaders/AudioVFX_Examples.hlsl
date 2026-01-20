// ===========================================================================
// AudioVFX_Examples.hlsl - VFX Graph Custom HLSL Snippets (URP/Mobile)
// ===========================================================================
// Copy entire code block into VFX Graph Custom HLSL
// Uses VFXAttributes struct pattern for full compatibility
// ===========================================================================

// ---------------------------------------------------------------------------
// EXAMPLE 1: Beat Pulse Position
// Context: Update Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float PulseStrength)
{
    float beat = SampleBeatPulse(AudioDataTexture, samplerAudioDataTexture);
    float3 center = float3(0, 0, 0);
    float3 dir = normalize(attributes.position - center + 0.0001);
    attributes.position = attributes.position + dir * beat * PulseStrength;
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 2: Bass Lift + Treble Turbulence
// Context: Update Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture)
{
    float4 audio = SampleAudioPrimary(AudioDataTexture, samplerAudioDataTexture);
    float bass = audio.g;
    float treble = audio.a;

    attributes.position.y = attributes.position.y + bass * 2.0;
    attributes.velocity = attributes.velocity + sin(attributes.position * 5.0 + treble * 20.0) * treble * 0.5;
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 3: Beat Flash Color
// Context: Output Particle Quad
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float4 FlashColor)
{
    float beat = SampleBeatPulse(AudioDataTexture, samplerAudioDataTexture);
    attributes.color = lerp(attributes.color, FlashColor, beat);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 4: Frequency-Mapped RGB Color
// Context: Output
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float Intensity)
{
    float4 audio = SampleAudioPrimary(AudioDataTexture, samplerAudioDataTexture);
    attributes.color = float4(audio.g * Intensity, audio.b * Intensity, audio.a * Intensity, 1.0);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 5: Volume-Driven Size
// Context: Initialize or Update
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float MinMult, in float MaxMult)
{
    float volume = SampleVolume(AudioDataTexture, samplerAudioDataTexture);
    attributes.size = attributes.size * lerp(MinMult, MaxMult, volume);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 6: Beat Burst Velocity
// Context: Initialize Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float BurstForce)
{
    float beat = SampleBeatPulse(AudioDataTexture, samplerAudioDataTexture);
    float3 burstDir = normalize(float3(0, 1, 0) + attributes.position * 0.1);
    attributes.velocity = attributes.velocity + burstDir * beat * BurstForce;
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 7: Orbit Motion by Mids
// Context: Update Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float OrbitSpeed)
{
    float mids = SampleMids(AudioDataTexture, samplerAudioDataTexture);
    float3 center = float3(0, 0, 0);
    float3 toCenter = attributes.position - center;
    float3 tangent = normalize(float3(-toCenter.z, 0, toCenter.x));
    attributes.velocity = attributes.velocity + tangent * mids * OrbitSpeed;
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 8: Complete Audio-Reactive Update
// Context: Update Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float BeatStrength, in float TurbStrength)
{
    float4 audioPrimary = SampleAudioPrimary(AudioDataTexture, samplerAudioDataTexture);
    float4 audioExtended = SampleAudioExtended(AudioDataTexture, samplerAudioDataTexture);

    float bass = audioPrimary.g;
    float treble = audioPrimary.a;
    float beat = audioExtended.g;

    attributes.position.y = attributes.position.y + bass * 1.5;
    float3 pulseDir = normalize(attributes.position + 0.001);
    attributes.position = attributes.position + pulseDir * beat * BeatStrength;

    attributes.velocity = attributes.velocity + sin(attributes.position * 5.0) * treble * TurbStrength;

    attributes.size = attributes.size * (1.0 + beat * 0.5);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 9: Audio-Reactive Color + Brightness
// Context: Output
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float4 FlashColor)
{
    float4 audio = SampleAudioPrimary(AudioDataTexture, samplerAudioDataTexture);
    float beat = SampleBeatPulse(AudioDataTexture, samplerAudioDataTexture);
    float volume = audio.r;

    attributes.color = lerp(attributes.color, FlashColor, beat);
    attributes.color.rgb = attributes.color.rgb * (0.5 + volume * 1.5);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 10: Rainbow Hue Shift
// Context: Output
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float ShiftAmount)
{
    float mids = SampleMids(AudioDataTexture, samplerAudioDataTexture);
    float hueShift = mids * ShiftAmount * 6.28318;

    float cosA = cos(hueShift);
    float sinA = sin(hueShift);

    float3x3 hueMatrix = float3x3(
        0.299 + 0.701 * cosA + 0.168 * sinA,
        0.587 - 0.587 * cosA + 0.330 * sinA,
        0.114 - 0.114 * cosA - 0.497 * sinA,
        0.299 - 0.299 * cosA - 0.328 * sinA,
        0.587 + 0.413 * cosA + 0.035 * sinA,
        0.114 - 0.114 * cosA + 0.292 * sinA,
        0.299 - 0.300 * cosA + 1.250 * sinA,
        0.587 - 0.588 * cosA - 1.050 * sinA,
        0.114 + 0.886 * cosA - 0.203 * sinA
    );

    attributes.color.rgb = mul(hueMatrix, attributes.color.rgb);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 11: Global Shader Properties (No Texture Needed)
// Context: Update Particle
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/ARGlobals.hlsl"

void CustomHLSL(inout VFXAttributes attributes, in float BeatStrength)
{
    float volume = GetAudioVolume();
    float bass = GetAudioBass();
    float treble = GetAudioTreble();
    float beat = GetBeatPulse();

    float3 dir = normalize(attributes.position + 0.001);
    attributes.position = attributes.position + dir * beat * BeatStrength;

    attributes.velocity = attributes.velocity + sin(attributes.position * 5.0) * treble * 0.3;

    attributes.color.rgb = attributes.color.rgb * (1.0 + beat * 2.0);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 12: Simple Beat Size Pulse
// Context: Any
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture)
{
    float beat = SampleBeatPulse(AudioDataTexture, samplerAudioDataTexture);
    attributes.size = attributes.size * (1.0 + beat);
}
*/

// ---------------------------------------------------------------------------
// EXAMPLE 13: Bass-Driven Gravity
// Context: Update
// ---------------------------------------------------------------------------
/*
#include "Assets/Shaders/AudioVFX.hlsl"

void CustomHLSL(inout VFXAttributes attributes, Texture2D AudioDataTexture, SamplerState samplerAudioDataTexture, in float GravityScale)
{
    float bass = SampleBass(AudioDataTexture, samplerAudioDataTexture);
    float gravityMod = 1.0 - bass * 0.8;
    attributes.velocity.y = attributes.velocity.y - 9.81 * gravityMod * GravityScale * unity_DeltaTime.x;
}
*/

// ===========================================================================
// VFXAttributes STRUCT REFERENCE
// ===========================================================================
/*
attributes.position     float3   World position
attributes.velocity     float3   Velocity vector
attributes.color        float4   RGBA color
attributes.size         float    Particle size
attributes.age          float    Time since spawn
attributes.lifetime     float    Total lifetime
attributes.alive        bool     Is particle alive
attributes.axisX        float3   Local X axis
attributes.axisY        float3   Local Y axis
attributes.axisZ        float3   Local Z axis
attributes.seed         uint     Random seed per particle
*/

// ===========================================================================
// SETUP INSTRUCTIONS
// ===========================================================================
/*
1. BLACKBOARD: Add "AudioDataTexture" (Texture2D, Exposed)
2. GAMEOBJECT: Add VFXAudioDataBinder component
3. VFX GRAPH: Right-click context > Create Node > HLSL > Custom HLSL
4. INSPECTOR: Paste code block, click Compile
5. CONNECT: Wire AudioDataTexture from Blackboard to input slot

TEXTURE INPUT NOTE:
- VFX Graph auto-creates "samplerAudioDataTexture" for "AudioDataTexture"
- Both must be passed to sample functions

MOBILE PERFORMANCE:
- Sample texture ONCE at start of function
- Store in local float4 variables
- Keep particle count under 5000
*/
