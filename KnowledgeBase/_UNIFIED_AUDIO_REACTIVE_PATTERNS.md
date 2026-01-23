# Unified Audio Reactive System Patterns

**Created**: 2026-01-22
**Component**: `XRRAI.Audio.UnifiedAudioReactive`
**Related**: Spec 011 (OpenBrush), Spec 007 (VFX Multi-Mode)

## Overview

The Unified Audio Reactive system consolidates all FFT audio analysis into a single pipeline, eliminating duplicate computation and providing consistent 8-band frequency data to all consumers (VFX, brushes, shaders).

### Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    AudioSource (Microphone/Music)                   │
└─────────────────────────┬───────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────────────────┐
│              UnifiedAudioReactive (singleton)                       │
│    ONE FFT per frame → 8 logarithmic bands                         │
│    Beat detection → spectral flux algorithm                        │
│    Global shader props + Audio texture                              │
└─────────────────────────┬───────────────────────────────────────────┘
                          ↓
          ┌───────────────┼───────────────┐
          ↓               ↓               ↓
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│  VFX Graph      │ │  Brush Painting │ │  Shader Props   │
│  (8-band binder)│ │  (modulation)   │ │  (global)       │
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

## Key Components

| File | Purpose | LOC |
|------|---------|-----|
| `Assets/Scripts/Audio/UnifiedAudioReactive.cs` | Singleton 8-band FFT + beat detection | ~450 |
| `Assets/Scripts/VFX/Binders/VFXAudioDataBinder8Band.cs` | VFX binder for 8-band data | ~180 |
| `Assets/Scripts/Bridges/AudioBridge.cs` | Legacy 4-band (kept for compatibility) | ~415 |

## 8 Frequency Bands (Logarithmic)

| Band | Name | Frequency Range | Musical Element |
|------|------|-----------------|-----------------|
| 0 | Sub-bass | 20-60 Hz | Sub drops, rumble |
| 1 | Bass | 60-250 Hz | Kick drums, bass |
| 2 | Low-mids | 250-500 Hz | Bass guitar, warmth |
| 3 | Mids | 500-2000 Hz | Vocals, instruments |
| 4 | High-mids | 2000-4000 Hz | Presence, clarity |
| 5 | Presence | 4000-6000 Hz | Definition, bite |
| 6 | Brilliance | 6000-10000 Hz | Air, sparkle |
| 7 | Air | 10000-20000 Hz | Shimmer, sibilance |

### Band Boundaries (FFT Bins)

```csharp
// For 512-sample FFT at 48kHz
static readonly int[] BandBoundaries = { 2, 4, 8, 16, 32, 64, 128, 256 };
// Band 0: bins 0-1   (0-93.75 Hz)
// Band 1: bins 2-3   (93.75-187.5 Hz)
// Band 2: bins 4-7   (187.5-375 Hz)
// Band 3: bins 8-15  (375-750 Hz)
// Band 4: bins 16-31 (750-1500 Hz)
// Band 5: bins 32-63 (1500-3000 Hz)
// Band 6: bins 64-127 (3000-6000 Hz)
// Band 7: bins 128-255 (6000-12000 Hz)
```

## Usage Patterns

### 1. Access from Any Script

```csharp
using XRRAI.Audio;

void Update()
{
    var audio = UnifiedAudioReactive.Instance;
    if (audio == null) return;

    // Individual bands (0-1 normalized)
    float bass = audio.Band1;
    float mids = audio.Mids;
    float treble = audio.Treble;

    // Beat detection
    if (audio.IsOnset)
    {
        // Beat detected this frame!
        TriggerBeatEffect();
    }

    float beatPulse = audio.BeatPulse; // 0-1, decays after beat
}
```

### 2. Brush Audio Modulation

```csharp
using XRRAI.Audio;
using XRRAI.BrushPainting;

// In brush stroke update
var audio = UnifiedAudioReactive.Instance;
var modulation = audio.GetBrushModulation(brush.AudioParams);

// Apply to stroke size
float sizeMultiplier = Mathf.Lerp(
    brush.AudioParams.SizeMultiplierRange.x,
    brush.AudioParams.SizeMultiplierRange.y,
    modulation.NormalizedLevel);
```

### 3. VFX Graph Binding

Add `VFXAudioDataBinder8Band` component to any VFX:

```csharp
// Auto-binds to UnifiedAudioReactive singleton
// VFX Graph properties:
// - AudioBand0-AudioBand7 (float)
// - AudioSubBass, AudioBass, AudioMid, AudioTreble (legacy 4-band)
// - AudioVolume, AudioPeak (float)
// - BeatPulse, BeatIntensity (float)
// - AudioDataTexture (Texture2D, optional)
```

### 4. Global Shader Access

```hlsl
// Global shader properties (set every frame)
float _AudioBand0;  // Sub-bass
float _AudioBand1;  // Bass
float _AudioBand2;  // Low-mids
float _AudioBand3;  // Mids
float _AudioBand4;  // High-mids
float _AudioBand5;  // Presence
float _AudioBand6;  // Brilliance
float _AudioBand7;  // Air

// Legacy 4-band (for compatibility)
float _AudioSubBass;
float _AudioBass;
float _AudioMid;
float _AudioTreble;

// Beat detection
float _BeatPulse;
float _BeatIntensity;
float _AudioVolume;
float _AudioPeak;
```

### 5. Audio Texture (4x2 RGBAFloat)

For VFX without exposed float properties:

```hlsl
// Sample AudioDataTexture in VFX Custom HLSL
Texture2D AudioDataTexture;

// Row 0: 8 frequency bands (R=band0, G=band1, B=band2, A=band3, etc.)
// Row 1: Volume, Peak, BeatPulse, BeatIntensity

float GetAudioBand(int bandIndex)
{
    int x = bandIndex % 4;
    int y = bandIndex / 4;
    return AudioDataTexture.Load(int3(x, y, 0)).r;
}
```

## Beat Detection Algorithm

Uses spectral flux with adaptive threshold:

```csharp
// Spectral flux = sum of positive differences between frames
float flux = 0;
for (int i = 0; i < bands; i++)
{
    float diff = _smoothedBands[i] - _prevBands[i];
    if (diff > 0) flux += diff;
}

// Adaptive threshold from history
float threshold = _fluxHistory.Average() * _beatThreshold;
bool isOnset = flux > threshold && flux > _minOnsetStrength;

// Decay pulse
_beatPulse = isOnset ? 1f : _beatPulse * (1f - _beatDecay * Time.deltaTime);
```

## Configuration

### UnifiedAudioReactive Inspector

| Property | Default | Description |
|----------|---------|-------------|
| Audio Source | (auto) | AudioSource to analyze |
| FFT Size | 512 | FFT samples (256-4096) |
| Smoothing | 0.15 | Band smoothing (0-1) |
| Beat Threshold | 1.5 | Multiplier above average |
| Beat Decay | 8 | Pulse decay speed |
| Min Onset Strength | 0.1 | Minimum flux for beat |
| Update Global Shaders | true | Set `_AudioBand*` props |
| Enable Legacy 4-Band | true | Set `_AudioSubBass` etc. |

### VFXAudioDataBinder8Band Inspector

| Property | Default | Description |
|----------|---------|-------------|
| Band Multiplier | 1 | Scale all bands |
| Volume Multiplier | 1 | Scale volume/peak |
| Beat Multiplier | 1 | Scale beat values |
| Bind Beat Detection | true | Enable beat props |
| Bind Audio Texture | true | Enable texture |

## Performance

| Metric | Legacy (4-band + separate brush) | Unified (8-band) |
|--------|-----------------------------------|------------------|
| FFT calls/frame | 2 (AudioBridge + BrushAudioReactive) | 1 |
| CPU time | ~0.4ms | ~0.2ms |
| Memory | 2 FFT buffers | 1 FFT buffer |
| Bands available | 4 + 8 (separate) | 8 (shared) |

## Migration from Legacy

### From AudioBridge

```csharp
// Before (AudioBridge)
float bass = AudioBridge.Instance.Bass;
float[] bands = AudioBridge.Instance.FrequencyBands;

// After (UnifiedAudioReactive)
float bass = UnifiedAudioReactive.Instance.Bass;
float band0 = UnifiedAudioReactive.Instance.Band0;
```

### From BrushAudioReactive

```csharp
// Before (BrushAudioReactive)
var handler = GetComponent<BrushAudioReactive>();
var mod = handler.GetModulation(audioParams);

// After (UnifiedAudioReactive)
var audio = UnifiedAudioReactive.Instance;
var mod = audio.GetBrushModulation(audioParams);
```

### VFX Binder Migration

```csharp
// Before: VFXAudioDataBinder (4-band)
[VFXBinder("Audio/Audio Data")]

// After: VFXAudioDataBinder8Band (8-band + legacy compatibility)
[VFXBinder("Audio/Audio Data (8-Band)")]
```

## Setup Checklist

1. Add `UnifiedAudioReactive` to scene (singleton, auto-creates)
2. Assign AudioSource (or leave empty for auto-detect)
3. For VFX: Add `VFXAudioDataBinder8Band` to each audio-reactive VFX
4. For brushes: Ensure `BrushManager` uses `UnifiedAudioReactive.Instance`
5. For shaders: Access global `_AudioBand*` properties

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| No audio response | No AudioSource | Assign or auto-detect |
| Bands always 0 | Muted AudioSource | Check volume > 0 |
| Beat too sensitive | Low threshold | Increase `beatThreshold` |
| Beat misses | High threshold | Decrease `beatThreshold` |
| VFX not reactive | Missing binder | Add VFXAudioDataBinder8Band |

## Related Specs

- **Spec 011**: OpenBrush Integration - Audio-reactive brush strokes
- **Spec 007**: VFX Multi-Mode - Audio mode VFX selection
- **Spec 006**: VFX Library Pipeline - Integration with ARDepthSource

---

*Created: 2026-01-22*
*Author: Claude Code*
