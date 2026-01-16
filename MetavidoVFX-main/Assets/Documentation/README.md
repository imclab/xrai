# MetavidoVFX - System Documentation

This document explains all systems and components in the MetavidoVFX project.

## Table of Contents

1. [Quick Start](#quick-start)
2. [VFX System](#vfx-system)
3. [Hand Tracking](#hand-tracking)
4. [Audio System](#audio-system)
5. [Performance Optimization](#performance-optimization)
6. [Post-Processing](#post-processing)
7. [EchoVision Components](#echovision-components)
8. [Editor Menus](#editor-menus)

---

## Quick Start

### In Unity Editor:

1. **Setup VFX Pipeline** (REQUIRED - one-click setup):
   ```
   H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)
   ```
   This creates ARDepthSource, adds VFXARBinder to all VFX, adds Dashboard and Test Harness.

2. **Debug/Test VFX** (Keyboard shortcuts in Play mode):
   - `Tab` - Toggle Pipeline Dashboard
   - `1-9` - Select VFX by index
   - `Space` - Cycle to next VFX
   - `C` - Cycle categories
   - `A` - Toggle all VFX on/off
   - `P` - Auto-cycle profiling mode

3. **Setup HoloKit** (if using hand tracking):
   ```
   H3M > HoloKit > Setup HoloKit Defines
   H3M > HoloKit > Setup Complete HoloKit Rig
   ```

4. **Setup Post-Processing**:
   ```
   H3M > Post-Processing > Setup Post-Processing
   ```

5. **Validate**:
   ```
   H3M > VFX Pipeline Master > Testing > Validate All Bindings
   H3M > Post-Processing > Validate Setup
   ```

### Key Documentation

| Document | Purpose |
|----------|---------|
| **[VFX_PIPELINE_FINAL_RECOMMENDATION.md](VFX_PIPELINE_FINAL_RECOMMENDATION.md)** | **Primary** - Hybrid Bridge architecture |
| [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) | Triple-verified testing workflow |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | VFX properties cheat sheet |
| [VFX_NAMING_CONVENTION.md](VFX_NAMING_CONVENTION.md) | Asset naming standards |
| [VFX_INDEX.md](VFX_INDEX.md) | All 88 VFX assets indexed |

---

## Data Pipeline Architecture (Updated 2026-01-16)

### Recommended Architecture: Hybrid Bridge Pattern

**O(1) compute + O(N) lightweight binding** - Optimal for multi-VFX scenes

```
┌─────────────────────────────────────────────────────────────────┐
│                     AR Foundation                                │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐  │
│  │ ARCameraManager │  │ AROcclusionMgr   │  │ARCameraBackground│ │
│  └────────┬────────┘  └────────┬─────────┘  └───────┬────────┘  │
│           │                    │                     │           │
└───────────┼────────────────────┼─────────────────────┼───────────┘
            │                    │                     │
            v                    v                     v
┌───────────────────────────────────────────────────────────────────┐
│              ARDepthSource (SINGLETON - one compute dispatch)     │
│  ONE GPU compute → PositionMap, VelocityMap                       │
│  Public: DepthMap, StencilMap, PositionMap, VelocityMap, RayParams│
└──────────────────────────────┬────────────────────────────────────┘
                               │
            ┌──────────────────┼──────────────────┐
            v                  v                  v
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│VFXARBinder      │  │VFXARBinder      │  │VFXARBinder      │
│+ VisualEffect   │  │+ VisualEffect   │  │+ VisualEffect   │
│ SetTexture only │  │ SetTexture only │  │ SetTexture only │
│    (VFX 1)      │  │    (VFX 2)      │  │    (VFX N)      │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

**Scaling**: 1 VFX = ~2ms, 10 VFX = ~5ms, 20 VFX = ~8ms (60fps feasible)

### Pipeline Status

| Pipeline | Status | Properties Bound |
|----------|--------|------------------|
| **ARDepthSource + VFXARBinder** | ✅ **PRIMARY** | DepthMap, StencilMap, PositionMap, VelocityMap, RayParams |
| **AudioBridge** | ✅ Audio | _AudioBands (global Vector4), _AudioVolume (global float) |
| **HologramSource/Renderer** | ✅ H3M | Use for anchor/scale features |
| **HandVFXController** | ✅ Hands | HandPosition, HandVelocity, BrushWidth, IsPinching |
| **NNCamKeypointBinder** | ✅ Keypoints | KeypointBuffer (17 pose landmarks) |
| **BodyPartSegmenter** | ✅ Segmentation | BodyPartMask, segmented PositionMaps |
| **VFXBinderManager** | ❌ LEGACY | Replaced by ARDepthSource |
| **VFXARDataBinder** | ❌ LEGACY | Replaced by VFXARBinder |
| **EnhancedAudioProcessor** | ❌ LEGACY | Replaced by AudioBridge |

### Deprecated/Redundant (DO NOT USE)

| Component | Reason | Alternative |
|-----------|--------|-------------|
| **VFXBinderManager** | Heavy centralized compute | ARDepthSource |
| **VFXARDataBinder** | Redundant per-VFX binder | VFXARBinder |
| **PeopleOcclusionVFXManager** | Creates own VFX at runtime | VFXARBinder |
| **EnhancedAudioProcessor** | Complex audio binding | AudioBridge |

### Pipeline Setup

**One-click**: `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)`

**Individual options**:
- `H3M > VFX Pipeline Master > Pipeline Components > Create ARDepthSource`
- `H3M > VFX Pipeline Master > Pipeline Components > Add VFXARBinder to All VFX`
- `H3M > VFX Pipeline Master > Legacy Management > Mark All Legacy (Disable)`

---

## VFX System

### VFX Gallery UI (`VFXGalleryUI.cs`)

Floating world-space UI for selecting VFX effects.

**Features:**
- Auto-populates from `Resources/VFX/` folder
- Gaze-and-dwell selection (HoloKit)
- Touch fallback
- Spawn control mode (recommended) or asset swapping

**Properties (VFX Graph):**
```
Spawn (bool)        - Controls particle emission
HandPosition        - World position of hand
HandVelocity        - Velocity vector
AudioVolume         - Microphone volume 0-1
AudioBass           - Low frequency amplitude
```

### VFX Selector UI (`VFXSelectorUI.cs`)

UI Toolkit-based VFX selector matching original MetavidoVFX style.

**Location:** `Assets/UI/VFXSelector.uxml`

**Style:** Inconsolata font, dark translucent background, minimal padding.

### VFX Category System (`VFXCategory.cs`)

Categorizes VFX by type for organized management:

| Category | Use Case |
|----------|----------|
| People | Human segmentation/occlusion VFX |
| Face | Face tracking effects |
| Hands | Hand-driven particle effects |
| Environment | AR mesh, depth-based effects |
| Audio | Sound-reactive particles |
| Hybrid | Multi-source effects |

**Binding Requirements (flags):**
- DepthMap, ColorMap, StencilMap
- HandTracking, FaceTracking, BodyTracking
- Audio

### VFX Binder Manager (`VFXBinderManager.cs`)

Unified data binding for all VFX in scene.

**Auto-binds:**
- AR depth/stencil textures
- Camera matrices (InverseView)
- Audio data from EnhancedAudioProcessor
- Hand tracking from HandVFXController

---

## Hand Tracking

### HoloKit Integration (`HoloKitHandTrackingSetup.cs`)

**Menu:** `H3M > HoloKit > Setup Complete HoloKit Rig`

Creates:
- HoloKit Camera Rig with XROrigin
- HandGestureRecognitionManager
- GazeRaycastInteractor + GazeGestureInteractor

**Required Define:** `HOLOKIT_AVAILABLE`

### Hand VFX Controller (`HandVFXController.cs`)

Attaches VFX to hands with velocity, audio, and gesture control.

**VFX Properties pushed:**
```csharp
HandPosition    // Vector3 - wrist world position
HandVelocity    // Vector3 - velocity vector
HandSpeed       // float   - velocity magnitude
TrailLength     // float   - velocity * multiplier
BrushWidth      // float   - based on pinch distance
IsPinching      // bool    - pinch gesture active
```

**Events sent:**
- `OnPinchStart` - when pinch begins
- `OnPinchEnd` - when pinch ends

### ARKit Hand Tracking (`ARKitHandTracking.cs`)

Fallback when HoloKit is not available. Uses XR Hands subsystem.

**Required Define:** `UNITY_XR_HANDS`

---

## Audio System

### Enhanced Audio Processor (`EnhancedAudioProcessor.cs`)

Advanced audio analysis with frequency bands.

**Properties:**
```csharp
AudioVolume     // Overall volume 0-1
AudioBass       // Low frequency (20-250Hz)
AudioMid        // Mid frequency (250-2000Hz)
AudioTreble     // High frequency (2000-20000Hz)
AudioPitch      // Estimated pitch
BeatDetected    // True on beat transients
```

**VFX Properties pushed:**
```
AudioVolume, AudioBass, AudioMid, AudioTreble
```

### Sound Wave Emitter (`SoundWaveEmitter.cs`)

Emits expanding sound waves for VFX based on audio volume.

**VFX Properties:**
```
WaveOrigin      // Vector3 - emission point
WaveDirection   // Vector3 - wave direction
WaveRange       // float   - current radius
WaveAngle       // float   - cone angle
WaveAge         // float   - normalized age 0-1
```

---

## Performance Optimization

### VFX Auto Optimizer (`VFXAutoOptimizer.cs`)

Automatically adjusts VFX quality to maintain 60fps.

**States:**
- `Optimal` - FPS >= 58, can increase quality
- `Degrading` - FPS 45-58, reduce quality gradually
- `Critical` - FPS < 45, aggressive reduction
- `Recovering` - FPS improving, slowly restore quality

**Menu:** `H3M > VFX Performance > Add Auto Optimizer to Scene`

### VFX LOD Controller (`VFXLODController.cs`)

Distance-based quality control per VFX.

**LOD Levels:**
| Distance | Quality Multiplier |
|----------|-------------------|
| 0 - 2m   | 1.0 (full) |
| 2 - 5m   | 0.7 |
| 5 - 10m  | 0.4 |
| 10 - 15m | 0.2 |
| > 15m    | Culled |

### VFX Profiler (`VFXProfiler.cs`)

Analyzes VFX performance and generates recommendations.

**Menu:** `H3M > VFX Performance > Profile All VFX`

**Checks:**
- Particle count per VFX
- Expensive operations (3D noise, SDF)
- Texture usage
- Collision detection

---

## Post-Processing

### PostProcessing_EchoVision.asset

**Effects:**
- Bloom (threshold=0.82, intensity=2)
- Tonemapping (ACES)
- ColorAdjustments (contrast=7)

### Postprocess.asset (Full)

**Additional Effects:**
- Vignette (intensity=0.25)
- ChromaticAberration (intensity=0.2)
- LensDistortion (intensity=-0.2)
- ScreenSpaceLensFlare (intensity=0.1)
- Saturation (40)

**Menu:** `H3M > Post-Processing > Setup Post-Processing`

---

## EchoVision Components

### MeshVFX (`MeshVFX.cs`)

Collects AR mesh vertices/normals and pushes to VFX via GraphicsBuffers.

**VFX Properties:**
```
MeshPointCache      // GraphicsBuffer - vertex positions
MeshNormalCache     // GraphicsBuffer - vertex normals
MeshPointCount      // int - number of vertices
MeshTransform_*     // Vector3 - mesh transform
```

### Human Particle VFX (`HumanParticleVFX.cs`)

Maps AR human depth to world-space positions for VFX.

**Requires:**
- HumanDepthMapper.compute shader
- PositionMap/ColorMap render textures

**VFX Properties:**
```
PositionMap     // Texture2D - world positions
ColorMap        // Texture2D - camera colors
```

---

## Editor Menus

### H3M > HoloKit
| Menu | Description |
|------|-------------|
| Setup HoloKit Defines | Adds HOLOKIT_AVAILABLE define |
| Setup Complete HoloKit Rig | Full HoloKit + hand tracking |
| Validate Hand Tracking Setup | Check components |
| Force Enable HoloKit | Force add defines |

### H3M > Post-Processing
| Menu | Description |
|------|-------------|
| Setup Post-Processing | Create/update Global Volume |
| Enable Camera Post-Processing | Enable on all cameras |
| Validate Setup | Check all effects |
| Copy Effects to DefaultVolume | Sync from Postprocess.asset |

### H3M > EchoVision
| Menu | Description |
|------|-------------|
| Setup All EchoVision Components | Full AR/Audio/VFX setup |
| Setup AR Mesh Manager | Add ARMeshManager |
| Setup Audio Input | Add AudioProcessor + SoundWaveEmitter |
| Setup MeshVFX | Add MeshVFX component |
| Validate Setup | Check all components |

### H3M > VFX Performance
| Menu | Description |
|------|-------------|
| Add Auto Optimizer to Scene | Add VFXAutoOptimizer |
| Profile All VFX | Analyze performance |
| Add LOD Controller to All VFX | Add distance-based LOD |

### Metavido
| Menu | Description |
|------|-------------|
| Copy More VFX to Resources | Copy VFX to Resources/VFX |
| Cleanup Duplicate VFX Containers | Remove duplicate containers |

---

## File Structure

```
Assets/
├── Scripts/
│   ├── Audio/
│   │   └── EnhancedAudioProcessor.cs
│   ├── Editor/
│   │   ├── EchovisionSetup.cs
│   │   ├── HoloKitDefineSetup.cs
│   │   ├── HoloKitHandTrackingSetup.cs
│   │   └── PostProcessingSetup.cs
│   ├── HandTracking/
│   │   ├── ARKitHandTracking.cs
│   │   ├── HandVFXController.cs
│   │   └── PhysicsVFXCollider.cs
│   ├── Performance/
│   │   ├── VFXAutoOptimizer.cs
│   │   ├── VFXLODController.cs
│   │   └── VFXProfiler.cs
│   ├── UI/
│   │   ├── VFXGalleryUI.cs
│   │   ├── VFXSelectorUI.cs
│   │   └── VFXCardInteractable.cs
│   └── VFX/
│       ├── VFXCategory.cs
│       ├── VFXBinderManager.cs
│       └── HumanParticleVFX.cs
├── UI/
│   ├── Monitor.uss
│   ├── Monitor.uxml
│   ├── VFXSelector.uss
│   ├── VFXSelector.uxml
│   └── Inconsolata/
├── VFX/
│   └── HumanEffects/
│       ├── HumanCube.vfx
│       └── *.renderTexture
├── Shaders/
│   └── HumanEffects/
│       ├── HumanDepthMapper.compute
│       └── HumanSegmentation_*.shader
└── Resources/
    ├── VFX/
    │   └── *.vfx
    └── HumanDepthMapper.compute
```

---

## Troubleshooting

### Post-Processing Not Working
1. Run `H3M > Post-Processing > Setup Post-Processing`
2. Check camera has `Render Post Processing` enabled
3. Verify Global Volume exists with profile assigned

### Hand Tracking Not Working
1. Run `H3M > HoloKit > Setup HoloKit Defines`
2. Check HOLOKIT_AVAILABLE is in Player Settings > Scripting Define Symbols
3. Install HoloKit samples via Package Manager

### VFX Freeze / Low FPS
1. Run `H3M > VFX Performance > Add Auto Optimizer to Scene`
2. Reduce VFX particle counts
3. Enable spawn control mode in VFXGalleryUI
4. Run `H3M > VFX Performance > Profile All VFX` for recommendations

### Missing Components
1. Run `H3M > EchoVision > Validate Setup`
2. Run missing component setup menus
3. Check console for error messages
