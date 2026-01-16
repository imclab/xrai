# VFX Pipeline Comparison

## Overview

Comprehensive comparison of all VFX pipelines available in MetavidoVFX project for driving particle effects from AR sensor data.

---

## Pipeline Architecture Diagrams

### 1. PeopleOcclusionVFXManager (GPU Compute - 3 Pass)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PeopleOcclusionVFXManager Pipeline                    │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │
│  │ ARKit Depth  │    │ ARKit Color  │    │ Human Stencil│              │
│  │   Texture    │    │   Texture    │    │   Texture    │              │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘              │
│         │                   │                    │                      │
│         ▼                   │                    ▼                      │
│  ┌──────────────────────────┴────────────────────────────┐              │
│  │            GPU Compute Pass 1: DepthToWorld           │              │
│  │      (Depth + Stencil → World Position Texture)       │              │
│  │           32x32 thread groups per dispatch            │              │
│  └───────────────────────────┬───────────────────────────┘              │
│                              │                                          │
│                              ▼                                          │
│  ┌───────────────────────────────────────────────────────┐              │
│  │            GPU Compute Pass 2: Velocity               │              │
│  │      (Position[t] - Position[t-1] → Velocity)         │              │
│  │           Temporal buffer (ping-pong)                 │              │
│  └───────────────────────────┬───────────────────────────┘              │
│                              │                                          │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     VFX Graph Properties                         │    │
│  │  • ColorMap (Texture2D)        • PositionMap (RenderTexture)    │    │
│  │  • StencilMap (Texture2D)      • VelocityMap (RenderTexture)    │    │
│  │  • InverseView (Matrix4x4)     • DepthRange (Vector2)           │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  Performance: ~2.5ms GPU @ 256x192 depth | ~4ms @ 512x384               │
│  Memory: 3 RenderTextures (Position, Velocity, Previous Position)       │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2. H3M HologramSource (GPU Compute - 1 Pass)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       HologramSource Pipeline                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │
│  │  Depth Tex   │    │ Color (Tex)  │    │ Stencil Tex  │              │
│  │ ARKit/LiDAR  │    │   Provider   │    │ (Optional)   │              │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘              │
│         │                   │                    │                      │
│         ▼                   │                    ▼                      │
│  ┌──────────────────────────┴────────────────────────────┐              │
│  │            GPU Compute: DepthToWorld Kernel           │              │
│  │                                                        │              │
│  │   Inputs:  _Depth, _Stencil, _InvVP, _DepthRange      │              │
│  │   Output:  _PositionRT (ARGBFloat)                    │              │
│  │                                                        │              │
│  │   Dispatch: ceil(w/32) × ceil(h/32) × 1               │              │
│  └───────────────────────────┬───────────────────────────┘              │
│                              │                                          │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     Exposed Properties                           │    │
│  │  • PositionMap (RenderTexture)  • ColorTexture (Texture)        │    │
│  │  • StencilTexture (Texture)     • DepthRange (Vector2)          │    │
│  │  • InverseViewMatrix            • RayParams (Vector4)           │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                          │
│  Performance: ~1.5ms GPU @ 256x192 | Single compute pass                │
│  Memory: 1 RenderTexture (PositionMap ARGBFloat)                        │
└─────────────────────────────────────────────────────────────────────────┘
```

### 3. H3MLiDARCapture (Direct Passthrough - 0 Compute)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    H3MLiDARCapture Pipeline (Rcam4)                      │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐    ┌──────────────┐                                   │
│  │ ARKit Depth  │    │ Camera Color │                                   │
│  │   Manager    │    │   Provider   │                                   │
│  └──────┬───────┘    └──────┬───────┘                                   │
│         │                   │                                            │
│         ▼                   ▼                                            │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                 Direct VFX Property Binding                       │   │
│  │                                                                    │   │
│  │   vfx.SetTexture("DepthMap", environmentDepthTexture);           │   │
│  │   vfx.SetTexture("ColorMap", colorProvider.Texture);             │   │
│  │   vfx.SetMatrix4x4("InverseView", camera.cameraToWorldMatrix);   │   │
│  │   vfx.SetVector4("RayParams", rayParams);                        │   │
│  │   vfx.SetVector2("DepthRange", depthRange);                      │   │
│  │   vfx.SetBool("Spawn", true);                                    │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  Performance: ~0.2ms CPU only | No GPU compute                          │
│  Memory: No additional RenderTextures                                   │
│  Note: Depth→Position conversion happens in VFX Graph shader           │
└─────────────────────────────────────────────────────────────────────────┘
```

### 4. ARKitMetavidoBinder (VFXBinderBase Pattern)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   ARKitMetavidoBinder Pipeline                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                     VFXBinderBase Lifecycle                         │ │
│  │                                                                      │ │
│  │   IsValid() → Check Manager/Texture availability                   │ │
│  │   UpdateBinding() → Push properties each frame                     │ │
│  │                                                                      │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐              │
│  │ ARKit Depth  │    │ Camera Color │    │   Compute    │              │
│  │   Manager    │    │   Provider   │    │   Shader     │              │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘              │
│         │                   │                    │                      │
│         ▼                   ▼                    ▼                      │
│  ┌──────────────────────────────────────────────────────────────────┐   │
│  │                   UpdateBinding() Method                          │   │
│  │                                                                    │   │
│  │   1. Dispatch DepthToWorld compute                                │   │
│  │   2. vfx.SetTexture("ColorMap", ...)                             │   │
│  │   3. vfx.SetTexture("DepthMap", ...)                             │   │
│  │   4. vfx.SetTexture("PositionMap", ...)                          │   │
│  │   5. vfx.SetMatrix4x4("InverseView", ...)                        │   │
│  │   6. vfx.SetVector4("RayParams", ...)                            │   │
│  └──────────────────────────────────────────────────────────────────┘   │
│                                                                          │
│  Performance: ~1.5ms GPU + VFXBinder overhead                           │
│  Benefit: Pluggable via VFX Property Binders UI                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5. Echovision (Audio + Mesh Driven)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Echovision Pipeline                                 │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    Audio Analysis Path                             │  │
│  │                                                                     │  │
│  │  ┌─────────────┐    ┌──────────────────────────────────────────┐  │  │
│  │  │ AudioSource │───▶│         AudioProcessor                   │  │  │
│  │  │ (Microphone)│    │  • GetOutputData() → RMS → dB            │  │  │
│  │  └─────────────┘    │  • GetSpectrumData() → FFT → Pitch       │  │  │
│  │                     │  • Outputs: AudioVolume, AudioPitch      │  │  │
│  │                     └────────────────────┬─────────────────────┘  │  │
│  └──────────────────────────────────────────┼────────────────────────┘  │
│                                              │                          │
│  ┌───────────────────────────────────────────┼───────────────────────┐  │
│  │                    Mesh Streaming Path    │                        │  │
│  │                                           │                        │  │
│  │  ┌─────────────┐    ┌─────────────────────┴────────────────────┐  │  │
│  │  │ ARMeshMgr   │───▶│              MeshVFX                     │  │  │
│  │  │  (LiDAR)    │    │  • GraphicsBuffer (65K verts, 12B/vert)  │  │  │
│  │  └─────────────┘    │  • MeshPointCache, MeshNormalCache       │  │  │
│  │                     │  • MeshPointCount                         │  │  │
│  │                     └────────────────────┬─────────────────────┘  │  │
│  └──────────────────────────────────────────┼────────────────────────┘  │
│                                              │                          │
│  ┌───────────────────────────────────────────┼───────────────────────┐  │
│  │                   Wave Emission           │                        │  │
│  │                                           ▼                        │  │
│  │  ┌─────────────────────────────────────────────────────────────┐  │  │
│  │  │              SoundWaveEmitter                                │  │  │
│  │  │  • 3-wave circular buffer (Wave0, Wave1, Wave2)             │  │  │
│  │  │  • TrackedPoseDriver for head position                      │  │  │
│  │  │  • Audio volume → Wave amplitude                            │  │  │
│  │  │  • WaveRadius expands over time                             │  │  │
│  │  └─────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                          │
│  Performance: ~1ms CPU (audio) + ~2ms GPU (mesh upload)                 │
│  Memory: 1 GraphicsBuffer (780KB for 65K vertices)                      │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Performance Comparison Table

| Pipeline | GPU Cost | CPU Cost | Memory | iOS Scale | Android Scale | Quest |
|----------|----------|----------|--------|-----------|---------------|-------|
| **PeopleOcclusion** | ~2.5ms | 0.5ms | 3 RT | Good (LiDAR) | Limited | N/A |
| **HologramSource** | ~1.5ms | 0.3ms | 1 RT | Excellent | Limited | N/A |
| **H3MLiDARCapture** | ~0ms | 0.2ms | 0 RT | Best | N/A | N/A |
| **ARKitMetavidoBinder** | ~1.5ms | 0.4ms | 1 RT | Good | Limited | N/A |
| **Echovision Audio** | ~0ms | 1.0ms | FFT buf | Excellent | Excellent | Good |
| **Echovision Mesh** | ~2ms | 0.5ms | 780KB | Good | Good | Limited |

### Resolution Scaling (iOS LiDAR)

| Resolution | GPU Time | Recommended Use |
|------------|----------|-----------------|
| 256×192 | ~1.5ms | Real-time VFX, high particle count |
| 512×384 | ~4.0ms | Balanced quality/performance |
| 768×576 | ~8.0ms | High quality, limited VFX |

---

## Scene Setup Instructions

### 1. PeopleOcclusionVFXManager Setup

```
Required GameObjects:
├── AR Session Origin
│   ├── AR Camera
│   │   └── AR Camera Background
│   ├── AR Occlusion Manager (required)
│   └── AR Camera Texture Provider (Metavido)
├── VFX Controller
│   └── [PeopleOcclusionVFXManager.cs]
│       ├── Compute Shader: GeneratePositionTexture
│       ├── Compute Shader: GenerateVelocityTexture
│       └── VFX Targets: [array of VisualEffect]
└── VFX Particles
    └── [VisualEffect] with required properties

Required VFX Properties:
- ColorMap (Texture2D)
- StencilMap (Texture2D)
- PositionMap (Texture2D)
- VelocityMap (Texture2D)
- InverseView (Matrix4x4)
```

### 2. HologramSource + HologramRenderer Setup

```
Required GameObjects:
├── AR Session Origin
│   ├── AR Camera
│   ├── AR Occlusion Manager
│   └── AR Camera Texture Provider
├── Hologram System
│   └── [HologramSource.cs]
│       ├── Compute Shader: DepthToWorld (Resources)
│       ├── Use Stencil: true
│       └── Depth Range: (0.1, 5.0)
└── VFX Renderer
    └── [HologramRenderer.cs]
        └── Links to HologramSource outputs

HologramRenderer reads:
- hologramSource.PositionMap
- hologramSource.ColorTexture
- hologramSource.StencilTexture
- hologramSource.GetInverseViewMatrix()
- hologramSource.GetRayParams()
```

### 3. H3MLiDARCapture Setup (Lightweight)

```
Required GameObjects:
├── AR Session Origin
│   ├── AR Camera
│   │   └── [H3MLiDARCapture.cs] ← Attach to camera
│   │       ├── Occlusion Manager: [ref]
│   │       ├── Color Provider: [ref]
│   │       ├── VFX: [VisualEffect ref]
│   │       └── Depth Range: (0.1, 5.0)
│   └── AR Occlusion Manager
└── VFX Particles
    └── [VisualEffect]

Required VFX Properties:
- DepthMap (Texture2D)
- ColorMap (Texture2D)
- InverseView (Matrix4x4)
- RayParams (Vector4)
- DepthRange (Vector2)
- Spawn (bool)
```

### 4. Echovision Audio Setup

```
Required GameObjects:
├── Audio System
│   ├── [AudioSource] (Microphone or music)
│   └── [AudioProcessor.cs]
│       └── Audio Source: [ref]
├── Head Tracking
│   └── [TrackedPoseDriver] (XR Origin camera)
└── VFX Emitter
    └── [SoundWaveEmitter.cs]
        ├── Audio Processor: [ref]
        ├── Head: [TrackedPoseDriver ref]
        └── VFX: [VisualEffect ref]

Required VFX Properties:
- Wave0_Origin, Wave0_Radius, Wave0_Amplitude
- Wave1_Origin, Wave1_Radius, Wave1_Amplitude
- Wave2_Origin, Wave2_Radius, Wave2_Amplitude
```

---

## VFX Property Standardization

### Universal Properties (All Pipelines Should Support)

```csharp
// Position & Matrices
"HandPosition"      // Vector3 - Primary control point
"HandVelocity"      // Vector3 - Motion vector
"HandSpeed"         // Float   - Velocity magnitude
"InverseView"       // Matrix4x4 - Camera to world

// Audio (Enhanced)
"AudioVolume"       // Float [0-1] - Overall amplitude
"AudioPitch"        // Float [0-1] - Dominant frequency
"AudioBass"         // Float [0-1] - Low frequency (20-250Hz)
"AudioMid"          // Float [0-1] - Mid frequency (250-2000Hz)
"AudioTreble"       // Float [0-1] - High frequency (2000-20000Hz)
"AudioSpectrum"     // Texture2D - Full FFT visualization

// Gesture
"IsPinching"        // Bool - Pinch gesture active
"BrushWidth"        // Float - Pinch-controlled parameter
"ExtendedFingers"   // Int [0-5] - Number of extended fingers

// Physics
"CollisionPlanePosition" // Vector3
"CollisionPlaneNormal"   // Vector3
"CollisionBounciness"    // Float [0-1]

// Control
"Spawn"             // Bool - Enable/disable particle spawning
"EmissionRate"      // Float - Particles per second
```

---

## Audio FFT Frequency Bands

The enhanced AudioProcessor should provide these frequency bands:

| Band | Frequency Range | Musical Content | VFX Mapping |
|------|-----------------|-----------------|-------------|
| **Sub Bass** | 20-60 Hz | Rumble, sub | Ground shake, slow pulse |
| **Bass** | 60-250 Hz | Kick, bass | Large particles, pulse |
| **Low Mid** | 250-500 Hz | Warmth | Medium particles |
| **Mid** | 500-2000 Hz | Vocals, instruments | Core emission |
| **High Mid** | 2000-4000 Hz | Presence | Sparkle start |
| **Treble** | 4000-20000 Hz | Brilliance, air | Fine detail, shimmer |

### FFT Bin Mapping (1024 samples @ 44100 Hz)

```
Bin Resolution: 44100 / 1024 = 43.07 Hz per bin
Bass (60-250 Hz):    Bins 1-5
Mid (250-2000 Hz):   Bins 6-46
Treble (2000-20000 Hz): Bins 47-464
```

---

## Scaling Recommendations

### iOS (iPhone Pro with LiDAR)

**Best Pipeline**: H3MLiDARCapture or HologramSource
- Use 256×192 depth resolution for >60fps VFX
- LiDAR provides accurate depth without compute overhead
- Human stencil available for body-specific effects

### iOS (Non-LiDAR iPhones)

**Best Pipeline**: Echovision Audio
- No depth available, use audio-reactive effects
- ARKit body detection (skeleton only, no stencil)
- Limited occlusion support

### Android (ARCore)

**Best Pipeline**: Echovision Audio + Basic depth
- Depth API varies by device (monocular estimation)
- Focus on audio-reactive and mesh-based effects
- Test depth quality per device

### Quest 3/Pro

**Best Pipeline**: Custom hand-tracking + mesh
- Native hand tracking (no ARKit)
- Room mesh for collision
- Use Oculus-specific depth features

---

## Integration Checklist

### Adding Audio + Velocity to Any Pipeline

1. **Add AudioProcessor** component to scene
2. **Add HandVFXController** for velocity tracking
3. **Ensure VFX has properties**:
   - AudioVolume, AudioPitch (basic)
   - AudioBass, AudioMid, AudioTreble (enhanced)
   - HandVelocity, HandSpeed
4. **Connect references** in inspector
5. **Test with**: Pinch to spawn, velocity to trail, audio to color/scale

### Speech-to-Text Integration (Future)

```csharp
// Planned architecture
public interface IVoiceCommandHandler
{
    void OnCommand(string command, float confidence);
}

// Example commands
"change to fire"      → SetVFX("Fire")
"bigger particles"    → SetFloat("Scale", current * 1.5f)
"follow the beat"     → SetBool("BeatSync", true)
"rainbow mode"        → SetColorMode(ColorMode.Rainbow)
```

---

## References

- `Assets/Scripts/PeopleOcclusion/PeopleOcclusionVFXManager.cs`
- `Assets/Scripts/ARKitMetavidoBinder.cs`
- `Assets/H3M/Core/HologramSource.cs`
- `Assets/Scripts/Rcam4/H3MLiDARCapture.cs`
- `Assets/Echovision/Scripts/AudioProcessor.cs`
- `Assets/Echovision/Scripts/SoundWaveEmitter.cs`
- `Assets/Echovision/Scripts/MeshVFX.cs`
- `Assets/Scripts/HandTracking/HandVFXController.cs`
- `Assets/Scripts/HandTracking/PhysicsVFXCollider.cs`

---

*Last Updated: 2026-01-14*
*Part of MetavidoVFX Unity-XR-AI Knowledge Base*
