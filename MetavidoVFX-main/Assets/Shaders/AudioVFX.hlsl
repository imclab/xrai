// ===========================================================================
// AudioVFX.hlsl - Custom HLSL Library for Audio-Reactive VFX Graph
// ===========================================================================
// Usage: #include "Assets/Shaders/AudioVFX.hlsl" in VFX Graph Custom HLSL
// Requires: AudioDataTexture (Texture2D) exposed property bound by VFXAudioDataBinder
// ===========================================================================

#ifndef _AUDIO_VFX_H_
#define _AUDIO_VFX_H_

// ---------------------------------------------------------------------------
// AUDIO DATA SAMPLING
// AudioDataTexture is a 2x2 RGBAFloat texture from AudioBridge:
//   Pixel (0,0) at UV (0.25, 0.25): (Volume, Bass, Mids, Treble)
//   Pixel (1,0) at UV (0.75, 0.25): (SubBass, BeatPulse, BeatIntensity, 0)
// ---------------------------------------------------------------------------

// Sample primary audio data: (Volume, Bass, Mids, Treble)
float4 SampleAudioPrimary(Texture2D audioTex, SamplerState samp)
{
    return audioTex.SampleLevel(samp, float2(0.25, 0.25), 0);
}

// Sample extended audio data: (SubBass, BeatPulse, BeatIntensity, 0)
float4 SampleAudioExtended(Texture2D audioTex, SamplerState samp)
{
    return audioTex.SampleLevel(samp, float2(0.75, 0.25), 0);
}

// Convenience samplers for individual values
float SampleVolume(Texture2D audioTex, SamplerState samp) { return SampleAudioPrimary(audioTex, samp).r; }
float SampleBass(Texture2D audioTex, SamplerState samp) { return SampleAudioPrimary(audioTex, samp).g; }
float SampleMids(Texture2D audioTex, SamplerState samp) { return SampleAudioPrimary(audioTex, samp).b; }
float SampleTreble(Texture2D audioTex, SamplerState samp) { return SampleAudioPrimary(audioTex, samp).a; }
float SampleSubBass(Texture2D audioTex, SamplerState samp) { return SampleAudioExtended(audioTex, samp).r; }
float SampleBeatPulse(Texture2D audioTex, SamplerState samp) { return SampleAudioExtended(audioTex, samp).g; }
float SampleBeatIntensity(Texture2D audioTex, SamplerState samp) { return SampleAudioExtended(audioTex, samp).b; }

// ---------------------------------------------------------------------------
// AUDIO-REACTIVE POSITION MODIFIERS
// ---------------------------------------------------------------------------

// Pulse position outward on beat (radial burst)
// Usage: position = AudioPulsePosition(position, center, audioTex, samp, strength);
float3 AudioPulsePosition(float3 position, float3 center, Texture2D audioTex, SamplerState samp, float strength)
{
    float beat = SampleBeatPulse(audioTex, samp);
    float3 dir = normalize(position - center + 0.0001);
    return position + dir * beat * strength;
}

// Oscillate position on Y-axis by bass
// Usage: position = AudioBassLift(position, audioTex, samp, height);
float3 AudioBassLift(float3 position, Texture2D audioTex, SamplerState samp, float height)
{
    float bass = SampleBass(audioTex, samp);
    position.y += bass * height;
    return position;
}

// Wave motion driven by frequency bands
// Usage: position = AudioWavePosition(position, audioTex, samp, amplitude, frequency);
float3 AudioWavePosition(float3 position, Texture2D audioTex, SamplerState samp, float amplitude, float frequency)
{
    float4 audio = SampleAudioPrimary(audioTex, samp);
    float wave = sin(position.x * frequency + audio.g * 10.0) * audio.r;
    position.y += wave * amplitude;
    return position;
}

// Scatter position randomly scaled by volume
// Usage: position = AudioScatter(position, audioTex, samp, seed, maxScatter);
float3 AudioScatter(float3 position, Texture2D audioTex, SamplerState samp, float seed, float maxScatter)
{
    float volume = SampleVolume(audioTex, samp);
    float3 noise = frac(sin(float3(seed, seed + 1, seed + 2) * 12.9898) * 43758.5453) * 2.0 - 1.0;
    return position + noise * volume * maxScatter;
}

// ---------------------------------------------------------------------------
// AUDIO-REACTIVE VELOCITY MODIFIERS
// ---------------------------------------------------------------------------

// Add velocity burst on beat detection
// Usage: velocity = AudioBeatBurst(velocity, direction, audioTex, samp, force);
float3 AudioBeatBurst(float3 velocity, float3 direction, Texture2D audioTex, SamplerState samp, float force)
{
    float beat = SampleBeatPulse(audioTex, samp);
    return velocity + normalize(direction) * beat * force;
}

// Modulate velocity magnitude by volume
// Usage: velocity = AudioVelocityScale(velocity, audioTex, samp, minScale, maxScale);
float3 AudioVelocityScale(float3 velocity, Texture2D audioTex, SamplerState samp, float minScale, float maxScale)
{
    float volume = SampleVolume(audioTex, samp);
    float scale = lerp(minScale, maxScale, volume);
    return velocity * scale;
}

// Add turbulence scaled by treble (high frequency = chaotic motion)
// Usage: velocity = AudioTrebleTurbulence(velocity, position, audioTex, samp, strength);
float3 AudioTrebleTurbulence(float3 velocity, float3 position, Texture2D audioTex, SamplerState samp, float strength)
{
    float treble = SampleTreble(audioTex, samp);
    float3 turb = sin(position * 5.0 + treble * 20.0) * treble * strength;
    return velocity + turb;
}

// Orbit velocity (circular motion around Y-axis) driven by mids
// Usage: velocity = AudioOrbitVelocity(position, center, audioTex, samp, speed);
float3 AudioOrbitVelocity(float3 position, float3 center, Texture2D audioTex, SamplerState samp, float speed)
{
    float mids = SampleMids(audioTex, samp);
    float3 toCenter = position - center;
    float3 tangent = float3(-toCenter.z, 0, toCenter.x);
    return normalize(tangent) * mids * speed;
}

// ---------------------------------------------------------------------------
// AUDIO-REACTIVE SIZE MODIFIERS
// ---------------------------------------------------------------------------

// Scale size by volume
// Usage: size = AudioSizeByVolume(baseSize, audioTex, samp, minMult, maxMult);
float AudioSizeByVolume(float baseSize, Texture2D audioTex, SamplerState samp, float minMult, float maxMult)
{
    float volume = SampleVolume(audioTex, samp);
    return baseSize * lerp(minMult, maxMult, volume);
}

// Pulse size on beat
// Usage: size = AudioSizePulse(baseSize, audioTex, samp, pulseMult);
float AudioSizePulse(float baseSize, Texture2D audioTex, SamplerState samp, float pulseMult)
{
    float beat = SampleBeatPulse(audioTex, samp);
    return baseSize * (1.0 + beat * pulseMult);
}

// Size driven by frequency band (bass = bigger, treble = smaller)
// Usage: size = AudioSizeByFrequency(baseSize, audioTex, samp, bassScale, trebleScale);
float AudioSizeByFrequency(float baseSize, Texture2D audioTex, SamplerState samp, float bassScale, float trebleScale)
{
    float4 audio = SampleAudioPrimary(audioTex, samp);
    float freqFactor = audio.g * bassScale - audio.a * trebleScale;
    return baseSize * (1.0 + freqFactor);
}

// ---------------------------------------------------------------------------
// AUDIO-REACTIVE COLOR MODIFIERS
// ---------------------------------------------------------------------------

// Lerp between two colors based on volume
// Usage: color = AudioColorLerp(colorA, colorB, audioTex, samp);
float4 AudioColorLerp(float4 colorA, float4 colorB, Texture2D audioTex, SamplerState samp)
{
    float volume = SampleVolume(audioTex, samp);
    return lerp(colorA, colorB, volume);
}

// Color hue shift by mids (rainbow effect)
// Usage: color = AudioHueShift(baseColor, audioTex, samp, shiftAmount);
float4 AudioHueShift(float4 baseColor, Texture2D audioTex, SamplerState samp, float shiftAmount)
{
    float mids = SampleMids(audioTex, samp);
    float hueShift = mids * shiftAmount;

    // Simple hue rotation in RGB space
    float cosA = cos(hueShift * 6.28318);
    float sinA = sin(hueShift * 6.28318);

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

    baseColor.rgb = mul(hueMatrix, baseColor.rgb);
    return baseColor;
}

// Flash color on beat
// Usage: color = AudioBeatFlash(baseColor, flashColor, audioTex, samp);
float4 AudioBeatFlash(float4 baseColor, float4 flashColor, Texture2D audioTex, SamplerState samp)
{
    float beat = SampleBeatPulse(audioTex, samp);
    return lerp(baseColor, flashColor, beat);
}

// Frequency-mapped color (bass=red, mids=green, treble=blue)
// Usage: color = AudioFrequencyColor(audioTex, samp, intensity);
float4 AudioFrequencyColor(Texture2D audioTex, SamplerState samp, float intensity)
{
    float4 audio = SampleAudioPrimary(audioTex, samp);
    return float4(audio.g * intensity, audio.b * intensity, audio.a * intensity, 1.0);
}

// Brightness pulse by volume
// Usage: color = AudioBrightnessPulse(baseColor, audioTex, samp, minBright, maxBright);
float4 AudioBrightnessPulse(float4 baseColor, Texture2D audioTex, SamplerState samp, float minBright, float maxBright)
{
    float volume = SampleVolume(audioTex, samp);
    float brightness = lerp(minBright, maxBright, volume);
    return float4(baseColor.rgb * brightness, baseColor.a);
}

// ---------------------------------------------------------------------------
// AUDIO-REACTIVE LIFETIME MODIFIERS
// ---------------------------------------------------------------------------

// Extend lifetime on beat (particles live longer during beats)
// Usage: lifetime = AudioLifetimeExtend(baseLifetime, audioTex, samp, extension);
float AudioLifetimeExtend(float baseLifetime, Texture2D audioTex, SamplerState samp, float extension)
{
    float beat = SampleBeatIntensity(audioTex, samp);
    return baseLifetime * (1.0 + beat * extension);
}

// Lifetime scaled by volume (louder = shorter life, more turnover)
// Usage: lifetime = AudioLifetimeByVolume(baseLifetime, audioTex, samp, minMult, maxMult);
float AudioLifetimeByVolume(float baseLifetime, Texture2D audioTex, SamplerState samp, float minMult, float maxMult)
{
    float volume = SampleVolume(audioTex, samp);
    return baseLifetime * lerp(maxMult, minMult, volume); // Inverted: louder = shorter
}

// ---------------------------------------------------------------------------
// COMBINED ATTRIBUTE MODIFIERS (for Update context blocks)
// ---------------------------------------------------------------------------

// Apply full audio-reactive transformation to position and velocity
// Usage in Custom HLSL Update block:
//   AudioTransformParticle(position, velocity, center, audioTex, samp, settings);
struct AudioSettings
{
    float positionPulseStrength;
    float velocityBurstForce;
    float turbulenceStrength;
    float3 burstDirection;
};

void AudioTransformParticle(
    inout float3 position,
    inout float3 velocity,
    float3 center,
    Texture2D audioTex,
    SamplerState samp,
    AudioSettings settings)
{
    float4 audioPrimary = SampleAudioPrimary(audioTex, samp);
    float4 audioExtended = SampleAudioExtended(audioTex, samp);

    float volume = audioPrimary.r;
    float bass = audioPrimary.g;
    float treble = audioPrimary.a;
    float beat = audioExtended.g;

    // Position: pulse outward on beat
    float3 dir = normalize(position - center + 0.0001);
    position += dir * beat * settings.positionPulseStrength;

    // Velocity: burst on beat + treble turbulence
    velocity += normalize(settings.burstDirection) * beat * settings.velocityBurstForce;
    velocity += sin(position * 5.0 + treble * 20.0) * treble * settings.turbulenceStrength;
}

// ---------------------------------------------------------------------------
// SPAWN RATE HELPERS (for Output context - use with Set SpawnRate)
// ---------------------------------------------------------------------------

// Calculate spawn rate based on volume (0-1 → minRate-maxRate)
// Usage: spawnRate = AudioSpawnRate(audioTex, samp, minRate, maxRate);
float AudioSpawnRate(Texture2D audioTex, SamplerState samp, float minRate, float maxRate)
{
    float volume = SampleVolume(audioTex, samp);
    return lerp(minRate, maxRate, volume);
}

// Burst spawn count on beat (returns 0 normally, burst count on beat)
// Usage: burstCount = AudioBurstCount(audioTex, samp, maxBurst, threshold);
float AudioBurstCount(Texture2D audioTex, SamplerState samp, float maxBurst, float threshold)
{
    float beat = SampleBeatPulse(audioTex, samp);
    return beat > threshold ? maxBurst * beat : 0;
}

#endif // _AUDIO_VFX_H_
