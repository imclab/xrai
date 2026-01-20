# VFX Source Bindings Reference

Authoritative binding specifications for all VFX categories based on original keijiro projects.

## Binding Modes

| Mode | Description | AR Pipeline Required |
|------|-------------|---------------------|
| **AR** | Full AR depth/color pipeline | Yes |
| **Audio** | Audio-reactive (global shader) | No |
| **Keypoint** | ML body keypoints | Partial (ColorMap) |
| **Standalone** | No external data | No |

## Category: Fluo (Audio-Reactive)

**Source**: keijiro/Fluo-GHURT, keijiro/LASP

### Required Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `Throttle` | float | Input | 0-1 intensity |

### Optional Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `AudioLevel` | float | AudioBridge | Sparkles only |
| `Palette` | Gradient | BrushController | Brush.vfx only |

### Global Shader Properties (Auto-bound)
```
_Fluo_AudioLevel (Vector4) = _AudioBands
_Fluo_ThemeColor (Color) = theme color
```

### VFX List
| VFX | Mode | Required | Optional |
|-----|------|----------|----------|
| Bubbles | Audio | Throttle | - |
| Streams | Audio | Throttle | - |
| Confetti | Audio | Throttle | - |
| Sparkles | Audio | Throttle | AudioLevel |
| Brush | Audio | Throttle | Palette |
| Drops | Audio | Throttle | - |
| HUD VFX | Audio | WaveformTexture | AudioLowLevel, AudioMidLevel, AudioHighLevel |

### VFXAudioDataBinder Properties

When `VFXAudioDataBinder` is added to a VFX, it expects these Float properties in the VFX Graph:

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| `AudioVolume` | float | 0-1 | Overall audio level |
| `AudioBass` | float | 0-1 | Low frequency band (20-250Hz) |
| `AudioMid` | float | 0-1 | Mid frequency band (250-4000Hz) |
| `AudioTreble` | float | 0-1 | High frequency band (4000-20000Hz) |
| `AudioSubBass` | float | 0-1 | Sub-bass (20-60Hz) |
| `AudioPitch` | float | Hz | Detected pitch (requires EnhancedAudioProcessor) |
| `BeatPulse` | float | 0-1 | Beat detection pulse (decays after beat) |
| `BeatIntensity` | float | 0-1 | Beat strength |

**Core properties** (recommended minimum): `AudioVolume`, `AudioBass`, `AudioMid`, `AudioTreble`

**Adding properties to VFX Graph**:
1. Open VFX Graph Editor (double-click .vfx asset)
2. Open Blackboard panel (View > Blackboard)
3. Click '+' > Float
4. Name exactly as shown above
5. Check 'Exposed' checkbox

**Menu helper**: `H3M > VFX Pipeline Master > Audio > Show Audio Integration Guide`

### Custom HLSL Audio Access (Advanced)

For VFX without exposed properties, use Custom HLSL Operators to access global audio properties:

**Include File**: `Assets/Shaders/ARGlobals.hlsl`

**Available Functions**:
```hlsl
float GetAudioVolume()      // 0-1 overall volume
float GetAudioBass()        // 0-1 bass band (20-250Hz)
float GetAudioMid()         // 0-1 mid band (250-4000Hz)
float GetAudioTreble()      // 0-1 treble band (4000-20000Hz)
float GetAudioSubBass()     // 0-1 sub-bass band (20-60Hz)
float GetBeatPulse()        // 0-1 decaying beat pulse
float GetBeatIntensity()    // 0-1 beat strength
float4 GetAudioData()       // (volume, bass, mid, treble)
```

**Example Custom HLSL Operator**:
```hlsl
#include "Assets/Shaders/ARGlobals.hlsl"

// Scale particle size by bass
float scale = baseScale * (1.0 + GetAudioBass() * 2.0);

// Pulse color on beat
float3 color = baseColor * (1.0 + GetBeatPulse() * 3.0);
```

**Global Shader Properties** (set by AudioBridge.cs):
| Property | Type | Description |
|----------|------|-------------|
| `_AudioBands` | Vector4 | (bass*100, mids*100, treble*100, subBass*100) |
| `_AudioVolume` | float | 0-1 overall volume |
| `_BeatPulse` | float | 0-1 decaying pulse |
| `_BeatIntensity` | float | 0-1 beat strength |

---

## Category: Rcam2/3/4 (AR Depth)

**Source**: keijiro/Rcam2, keijiro/Rcam3, keijiro/Rcam4

### Required Bindings (People VFX)
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `DepthMap` | Texture2D | ARDepthSource | Required |
| `StencilMap` | Texture2D | ARDepthSource | Human mask |
| `ColorMap` | Texture2D | ARDepthSource | Camera RGB |
| `RayParams` | Vector4 | ARDepthSource | UV→ray |
| `InverseView` | Matrix4x4 | ARDepthSource | Camera pose |

### Required Bindings (Environment VFX)
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `DepthMap` | Texture2D | ARDepthSource | Required |
| `ColorMap` | Texture2D | ARDepthSource | Camera RGB |
| `RayParams` | Vector4 | ARDepthSource | UV→ray |
| `InverseView` | Matrix4x4 | ARDepthSource | Camera pose |

### Optional Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `PositionMap` | Texture2D | ARDepthSource | Pre-computed world pos |
| `VelocityMap` | Texture2D | ARDepthSource | Motion vectors |
| `Throttle` | float | Input | 0-1 intensity |
| `DepthRange` | Vector2 | Config | Near/far clip |

### VFX Naming Convention
- `*_depth_people_*` → Requires StencilMap
- `*_environment_*` → No StencilMap
- `*_any_*` → StencilMap optional

---

## Category: NNCam2 (Keypoint)

**Source**: keijiro/NNCam2

### Required Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `KeypointBuffer` | GraphicsBuffer | BodyPartSegmenter | 17 COCO keypoints |
| `Threshold` | float | Config | Confidence threshold (0.3-0.5) |

### Optional Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `MaskTexture` | Texture2D | BodyPartSegmenter | Body mask |
| `ColorMap` | Texture2D | ARDepthSource | Camera RGB |
| `Throttle` | float | Input | 0-1 intensity |

### VFX List
| VFX | Keypoints Used | Threshold |
|-----|----------------|-----------|
| eyes | 1, 2 | 0.3 |
| joints | All 17 | 0.5 |
| electrify | Limb pairs | 0.4 |
| mosaic | 0-4 (face) | 0.5 |
| tentacles | 9,10,15,16 | 0.4 |
| petals | 0,5,6,11,12 | 0.5 |
| spikes | All joints | 0.4 |
| symbols | All joints | 0.5 |
| particle | All | 0.4 |

---

## Category: Akvfx (Point Cloud)

**Source**: keijiro/Akvfx

### Required Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `PositionMap` | Texture2D | ARDepthSource | World positions (RGBAFloat) |
| `ColorMap` | Texture2D | ARDepthSource | Camera RGB |

### Optional Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `MapWidth` | int | ARDepthSource | Texture width |
| `MapHeight` | int | ARDepthSource | Texture height |
| `Throttle` | float | Input | 0-1 intensity |

### VFX List
- point, web, spikes, voxel, particles, ribbon, trail

---

## Category: SdfVfx (Environment SDF)

**Source**: keijiro/SdfVfxSamples

### Required Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `SDF` | Texture3D | SDF Bake Tool | Pre-baked or runtime |

### Optional Bindings
| Property | Type | Source | Notes |
|----------|------|--------|-------|
| `ColorMap` | Texture2D | ARDepthSource | For coloring |
| `Throttle` | float | Input | 0-1 intensity |

---

## Alias Resolution

VFXARBinder auto-resolves these aliases:

```
DepthMap: Depth, DepthTexture, _Depth
StencilMap: Stencil, HumanStencil, _Stencil
PositionMap: Position, WorldPosition, _Position
ColorMap: Color, ColorTexture, _Color
RayParams: RayParamsMatrix, InverseProjection
InverseView: InverseViewMatrix, CameraToWorld
Throttle: Intensity, Scale
```

---

## Mode Detection Rules

```csharp
// Auto-detect mode from VFX properties
if (HasProperty("KeypointBuffer")) return Mode.Keypoint;
if (HasProperty("DepthMap") || HasProperty("StencilMap")) return Mode.AR;
if (HasProperty("Throttle") && !HasProperty("DepthMap")) return Mode.Audio;
return Mode.Standalone;
```

---

## Specialized Binders (Auto-Added)

VFXPipelineAuditor automatically adds specialized binders when needed:

| Binding | Binder | Added When |
|---------|--------|------------|
| `KeypointBuffer` | NNCamKeypointBinder | VFX has GraphicsBuffer("KeypointBuffer") |
| `Audio` | VFXAudioDataBinder | Mode == Audio or has audio properties |
| `VelocityMap` | VFXPhysicsBinder | VFX uses velocity-based physics |
| Hand tracking | HandVFXController | VFX name contains "hand", "pinch", "brush" |
| Fluo canvas | FluoCanvas | Mode == Audio and name contains "fluo" |

### KeypointBuffer (Specialized Binding)

`KeypointBuffer` is NOT a VFXARBinder binding - it requires `NNCamKeypointBinder`:

```csharp
// KeypointBuffer structure (17 COCO keypoints)
struct Keypoint {
    float3 position;  // UV or world position
    float score;      // Confidence 0-1
};

// Keypoint indices
0: nose, 1-2: eyes, 3-4: ears, 5-6: shoulders,
7-8: elbows, 9-10: wrists, 11-12: hips,
13-14: knees, 15-16: ankles
```

---

## Auto-Fix Behavior

`H3M > VFX Pipeline Master > Audit & Fix` automatically:

1. **Detects binding mode** from VFX properties
2. **Adds VFXARBinder** with required AR bindings (AR/Keypoint modes)
3. **Adds specialized binders** (NNCamKeypointBinder, VFXAudioDataBinder, etc.)
4. **Adds VFXNoCull** for screen-space VFX (NNCam, screen-space effects)
5. **Adds VFXCategory** for mode-based management
6. **Does NOT edit VFX Graph files** - all binding via runtime components

### Override with custom-bindings.md

User can override auto-detection in `Assets/Documentation/custom-bindings.md`:

```yaml
eyes_any_nncam2:
  mode: Keypoint
  bindings:
    ColorMap: true
    Throttle: false
```

---

*Generated from keijiro source projects research 2026-01-20*
*Updated with specialized binders and auto-fix behavior 2026-01-20*
