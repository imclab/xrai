# MetavidoVFX System Architecture

**Generated**: 2026-01-14
**Unity Version**: 6000.2.14f1
**Platform**: iOS (ARKit)
**Render Pipeline**: URP 17.2.0

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Directory Structure](#2-directory-structure)
3. [VFX System Architecture](#3-vfx-system-architecture)
4. [AR/XR System Architecture](#4-arxr-system-architecture)
5. [Audio System Architecture](#5-audio-system-architecture)
6. [UI System Architecture](#6-ui-system-architecture)
7. [Performance System Architecture](#7-performance-system-architecture)
8. [Editor Tools](#8-editor-tools)
9. [Build System](#9-build-system)
10. [Data Flow Diagrams](#10-data-flow-diagrams)
11. [VFX Property Reference](#11-vfx-property-reference)
12. [Troubleshooting](#12-troubleshooting)

---

## 1. Project Overview

MetavidoVFX is an AR Foundation VFX demonstration project that visualizes volumetric videos captured with iPhone Pro LiDAR sensors. It combines AR Foundation, VFX Graph, and advanced particle systems for interactive AR experiences.

### Key Features
- **Depth-based VFX**: Particles spawned from LiDAR depth data
- **Hand Tracking**: HoloKit + XR Hands integration for gesture-driven effects
- **Audio Reactivity**: FFT frequency band analysis driving VFX parameters
- **Performance Optimization**: Auto-adaptive quality based on FPS
- **H3M Hologram System**: Volumetric "Man in the Mirror" rendering

### Package Dependencies
```json
{
  "com.unity.xr.arfoundation": "6.2.1",
  "com.unity.xr.arkit": "6.2.1",
  "com.unity.xr.hands": "1.4.3",
  "com.unity.visualeffectgraph": "17.2.0",
  "jp.keijiro.metavido": "5.1.1"
}
```

---

## 2. Directory Structure

```
Assets/
├── Scripts/
│   ├── VFX/                          # Core VFX management
│   │   ├── VFXCategory.cs            # VFX categorization & binding requirements
│   │   ├── VFXBinderManager.cs       # Unified data binding system (PRIMARY)
│   │   └── HumanParticleVFX.cs       # Depth-to-world position mapping
│   ├── HandTracking/
│   │   ├── HandVFXController.cs      # HoloKit hand-driven VFX
│   │   └── ARKitHandTracking.cs      # XR Hands fallback
│   ├── Audio/
│   │   └── EnhancedAudioProcessor.cs # FFT frequency band analysis
│   ├── Performance/
│   │   ├── VFXAutoOptimizer.cs       # FPS-based adaptive quality
│   │   ├── VFXLODController.cs       # Distance-based LOD culling
│   │   └── VFXProfiler.cs            # VFX analysis & recommendations
│   ├── UI/
│   │   ├── VFXGalleryUI.cs           # World-space gaze & dwell selector
│   │   ├── VFXSelectorUI.cs          # Screen-space UI Toolkit selector
│   │   └── VFXCardInteractable.cs    # HoloKit gaze+gesture support
│   ├── PeopleOcclusion/
│   │   └── PeopleOcclusionVFXManager.cs  # (Legacy - DISABLED)
│   └── Editor/                       # Editor setup utilities
├── H3M/
│   └── Core/
│       ├── HologramSource.cs         # Compute shader depth processing
│       ├── HologramRenderer.cs       # Volumetric rendering
│       └── HologramAnchor.cs         # Placement & scaling
├── Echovision/
│   └── Scripts/
│       ├── MeshVFX.cs                # AR mesh → VFX GraphicsBuffers
│       ├── SoundWaveEmitter.cs       # Audio-driven wave VFX
│       └── AudioProcessor.cs         # Legacy audio (2 properties)
├── Resources/
│   ├── DepthToWorld.compute          # Depth→world position conversion
│   └── VFX/                          # VFX assets loaded at runtime
├── VFX/                              # 65+ VFX assets
│   ├── HumanEffects/                 # People/body VFX
│   ├── Environment/                  # World/environment VFX
│   ├── Rcam4/                        # Volumetric video VFX
│   └── Metavido/                     # Metavido project VFX
└── Scenes/
    └── HOLOGRAM_Mirror_MVP.unity     # Main build scene
```

---

## 3. VFX System Architecture

### 3.1 VFXBinderManager (PRIMARY Pipeline)

Central data routing system that binds AR data to all VFX in the scene.

**Location**: `Assets/Scripts/VFX/VFXBinderManager.cs` (422 lines)

**Data Flow**:
```
AR Session Origin → VFXBinderManager → All VFX
                          ↓
         DepthMap, ColorMap, StencilMap, RayParams, InverseView
                          ↓
              GPU Compute (DepthToWorld.compute)
                          ↓
                    PositionMap (world XYZ)
```

**Key Responsibilities**:
- Fetches AR textures from AROcclusionManager
- Computes PositionMap via GPU compute shader
- Calculates RayParams for UV+depth→3D conversion
- Pushes properties to ALL registered VFX per frame
- Supports both EnhancedAudioProcessor and legacy AudioProcessor

**Critical Binding - RayParams**:
```csharp
// Required for Metavido/Rcam VFX to convert UV+depth to 3D positions
float fovV = arCamera.fieldOfView * Mathf.Deg2Rad;
float tanV = Mathf.Tan(fovV * 0.5f);
float tanH = tanV * arCamera.aspect;
_rayParams = new Vector4(0f, 0f, tanH, tanV);

vfx.SetVector4("RayParams", _rayParams);
vfx.SetVector4("ProjectionVector", _rayParams);  // Alias
```

### 3.2 VFX Categories

**Location**: `Assets/Scripts/VFX/VFXCategory.cs`

```csharp
enum VFXCategoryType { People, Face, Hands, Environment, Audio, Hybrid }

[Flags]
enum VFXBindingRequirements {
    None = 0, DepthMap = 1, ColorMap = 2, StencilMap = 4,
    HandTracking = 8, FaceTracking = 16, Audio = 32, ARMesh = 64
}
```

| Category | Path | Required Inputs |
|----------|------|-----------------|
| **Body/People** | `VFX/Metavido/`, `VFX/Rcam*/Body/` | DepthMap, ColorMap, InverseView, RayParams |
| **PositionMap** | `VFX/Akvfx/`, `VFX/HumanEffects/` | PositionMap, ColorMap |
| **Environment** | `VFX/Environment/`, `VFX/SdfVfx/` | Spawn, Throttle only |
| **Mesh/Audio** | `Echovision/VFX/` | MeshPointCache, Wave*, HumanStencilTexture |

### 3.3 Legacy Pipelines (DISABLED)

| Pipeline | Status | Reason |
|----------|--------|--------|
| PeopleOcclusionVFXManager | DISABLED | Creates own VFX at runtime, conflicts |
| ARKitMetavidoBinder | REMOVED | Per-VFX binding, redundant |

**Cleanup**: `H3M > Pipeline Cleanup > Run Full Cleanup`

---

## 4. AR/XR System Architecture

### 4.1 AR Session Hierarchy

```
AR Session Origin
├── AR Camera
│   ├── ARCameraManager
│   ├── ARCameraBackground
│   ├── Camera (Main)
│   ├── AROcclusionManager
│   │   ├── environmentDepthTexture
│   │   └── humanStencilTexture
│   └── TrackedPoseDriver
├── VFXBinderManager
├── HologramSource
│   └── DepthToWorld.compute
└── HandVFXController
    ├── LeftHandVFX
    └── RightHandVFX
```

### 4.2 Depth Processing Pipeline

```
AROcclusionManager.environmentDepthTexture (256×192, 16-bit float)
        ↓
VFXBinderManager.UpdateCachedData()
        ↓
DepthToWorld.compute (GPU kernel)
        ├── Input: _Depth, _Stencil, _InvVP, _ProjectionMatrix
        └── Output: _PositionRT (512×512 ARGBFloat)
        ↓
VFX.SetTexture("PositionMap", _positionMapRT)
```

### 4.3 Hand Tracking

**Primary**: HoloKit SDK (if HOLOKIT_AVAILABLE defined)
**Fallback**: XR Hands (com.unity.xr.hands)

**Location**: `Assets/Scripts/HandTracking/HandVFXController.cs` (365 lines)

**Data Output**:
- `HandPosition` (Vector3) - Wrist world position
- `HandVelocity` (Vector3) - Position delta / deltaTime
- `HandSpeed` (float) - Velocity magnitude
- `BrushWidth` (float) - Pinch distance mapped 0.01-0.5m
- `IsPinching` (bool) - Hysteresis-detected pinch state

**Events**:
- `OnPinchStart` - VFX event when pinch begins
- `OnPinchEnd` - VFX event when pinch ends

### 4.4 H3M Hologram System

**Location**: `Assets/H3M/Core/`

| Component | Purpose |
|-----------|---------|
| HologramSource | Compute shader wrapper, exports PositionMap |
| HologramRenderer | Volumetric rendering, VFX binding |
| HologramAnchor | AR plane-based placement |

---

## 5. Audio System Architecture

### 5.1 EnhancedAudioProcessor (Modern)

**Location**: `Assets/Scripts/Audio/EnhancedAudioProcessor.cs` (287 lines)

**Features**:
- FFT frequency analysis (1024 bins, BlackmanHarris window)
- 6 frequency bands: SubBass, Bass, Mid, Treble, Volume, Pitch
- Attack/decay smoothing (attack 20x, decay 5x)
- Automatic bin calculation from Hz ranges

**Frequency Bands**:
| Band | Range | Use Case |
|------|-------|----------|
| SubBass | 0-60 Hz | Kick detection |
| Bass | 0-250 Hz | Low frequency response |
| Mid | 250-2000 Hz | Voice, instruments |
| Treble | 2000-20000 Hz | High frequency sparkle |

**Properties Exposed**:
```csharp
AudioVolume   // 0-1 overall RMS amplitude
AudioBass     // 0-1 low frequency average + peak
AudioMid      // 0-1 mid frequency average + peak
AudioTreble   // 0-1 high frequency average + peak
AudioSubBass  // 0-1 sub-bass for kick detection
AudioPitch    // 0-1 dominant frequency (20-2000 Hz mapped)
```

### 5.2 AudioProcessor (Legacy)

**Location**: `Assets/Echovision/Scripts/AudioProcessor.cs` (105 lines)

Simple 2-property system:
- `AudioVolume` - RMS-based volume
- `AudioPitch` - Dominant frequency

### 5.3 SoundWaveEmitter

**Location**: `Assets/Echovision/Scripts/SoundWaveEmitter.cs`

Converts audio data to expanding spherical waves:
- 3 concurrent waves (ring buffer)
- Wave speed: 3-4.5 m/s
- Cone angle driven by volume (90-180°)
- Life driven by pitch multiplier

**Properties**:
- `WaveOrigin`, `WaveDirection`, `WaveRange`, `WaveAngle`, `WaveAge`

---

## 6. UI System Architecture

### 6.1 VFXGalleryUI (World-Space)

**Location**: `Assets/Scripts/UI/VFXGalleryUI.cs` (659 lines)

**Features**:
- Gaze-and-dwell selection (1.0s default)
- HoloKit pinch gesture support
- Auto-population from Resources/VFX/
- Spawn control mode (prevents scene freeze)
- Multi-select support

**Layout**:
- Grid: 6 cards per row
- Distance: 1.2m from camera
- Card size: 6cm × 5cm

### 6.2 VFXSelectorUI (Screen-Space)

**Location**: `Assets/Scripts/UI/VFXSelectorUI.cs` (272 lines)

- UI Toolkit radio button interface
- FPS and particle count stats
- Auto-cycle toggle

### 6.3 SimpleVFXUI (IMGUI)

**Location**: `Assets/Scripts/UI/SimpleVFXUI.cs` (477 lines)

- Tap-to-cycle
- Double-tap for auto-cycle
- Keyboard shortcuts (Editor)

---

## 7. Performance System Architecture

### 7.1 VFXAutoOptimizer

**Location**: `Assets/Scripts/Performance/VFXAutoOptimizer.cs` (424 lines)

**Thresholds**:
```
Target FPS:          60 fps
Critical FPS:        30 fps
Recovery FPS:        55 fps
Max Particles:       500,000
```

**States**:
| State | Condition | Action |
|-------|-----------|--------|
| Optimal | FPS >= 58, Quality < max | Ready to improve |
| Degrading | FPS 45-58 | Reduce quality (-0.1/step) |
| Critical | FPS < 30 | Emergency reduction (-0.2/step) |
| Recovering | FPS >= 55, Quality < max | Slow recovery (+0.02/step) |

### 7.2 VFXLODController

**Location**: `Assets/Scripts/Performance/VFXLODController.cs` (185 lines)

**Distance Tiers**:
| LOD | Distance | Quality |
|-----|----------|---------|
| 0 | 0-2m | 100% |
| 1 | 2-5m | 70% |
| 2 | 5-10m | 40% |
| 3 | 10-15m | 20% |
| Culled | >15m | Disabled |

### 7.3 VFXProfiler

**Location**: `Assets/Scripts/Performance/VFXProfiler.cs` (320 lines)

**Cost Scoring** (0-100):
- Base: particleCount / 50,000 × 30
- +20: Expensive noise (Turbulence/Voronoi)
- +15: 3D textures
- +15: Strips/trails
- +20: Collision

---

## 8. Editor Tools

### 8.1 Menu Commands

| Menu | Purpose |
|------|---------|
| `H3M > HoloKit > Setup Complete HoloKit Rig` | Full HoloKit + hand tracking |
| `H3M > EchoVision > Setup All EchoVision Components` | AR/Audio/VFX setup |
| `H3M > Pipeline Cleanup > Run Full Cleanup` | Remove legacy pipelines |
| `H3M > Post-Processing > Setup Post-Processing` | Global Volume + URP |
| `H3M > VFX UI > Add Simple VFX UI` | IMGUI overlay |
| `Metavido > Build iOS` | Build Xcode project |

### 8.2 Auto-Setup

**[InitializeOnLoad] Scripts**:
- `AutoSetupOnLoad.cs` - One-time full setup
- `HoloKitDefineSetup.cs` - Package detection + defines
- `HandVFXInitialSetup.cs` - HandVFXController wiring

---

## 9. Build System

### 9.1 Build Commands

```bash
./build_ios.sh           # Unity build → Xcode project
./deploy_ios.sh          # Install to device
./debug.sh               # Stream device logs
```

### 9.2 Build Configuration

- **Scene**: `Assets/Scenes/HOLOGRAM_Mirror_MVP.unity`
- **Output**: `Builds/iOS/`
- **Team ID**: Z8622973EB (auto-set in post-processor)
- **Compression**: LZ4 (faster builds)
- **Debug Console**: Auto-injected IngameDebugConsole

---

## 10. Data Flow Diagrams

### 10.1 Complete System Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    AR SESSION ORIGIN                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ARKit Sensors         ARCameraManager      AROcclusionMgr  │
│  ──────────────────→  (Camera texture) →  (Depth + Stencil) │
│                                                     │        │
│                                                     ▼        │
│                              ┌──────────────────────────┐   │
│                              │ VFXBinderManager.cs      │   │
│                              │ (PRIMARY PIPELINE)       │   │
│                              │ Outputs:                 │   │
│                              │  • DepthMap              │   │
│                              │  • ColorMap              │   │
│                              │  • StencilMap            │   │
│                              │  • PositionMap (GPU)     │   │
│                              │  • InverseView           │   │
│                              │  • RayParams             │   │
│                              └────────────┬─────────────┘   │
│                                           │                  │
│    ┌──────────────────────────────────────┼──────────────┐  │
│    │                                      │              │  │
│    ▼                                      ▼              ▼  │
│ ┌─────────────────┐  ┌─────────────────────┐  ┌──────────┐ │
│ │HandVFXController│  │EnhancedAudioProcessor│  │MeshVFX  │ │
│ │ HandPosition    │  │ AudioVolume          │  │MeshCache│ │
│ │ HandVelocity    │  │ AudioBass/Mid/Treble │  │Normals  │ │
│ │ BrushWidth      │  │ AudioPitch           │  │Count    │ │
│ └────────┬────────┘  └──────────┬───────────┘  └────┬────┘ │
│          │                      │                   │      │
│          └──────────────────────┼───────────────────┘      │
│                                 ▼                          │
│                    ┌────────────────────────┐              │
│                    │  All VFX (65+ assets)  │              │
│                    │  Categorized + Raw     │              │
│                    └────────────────────────┘              │
│                                 │                          │
│                                 ▼                          │
│                    ┌────────────────────────┐              │
│                    │  Performance Systems   │              │
│                    │  • VFXAutoOptimizer    │              │
│                    │  • VFXLODController    │              │
│                    └────────────────────────┘              │
└─────────────────────────────────────────────────────────────┘
```

---

## 11. VFX Property Reference

### 11.1 AR Depth Properties

| Property | Type | Description |
|----------|------|-------------|
| DepthMap | Texture2D | Environment depth from ARKit |
| StencilMap | Texture2D | Human segmentation mask |
| ColorMap | Texture2D | Camera color texture |
| PositionMap | Texture2D | GPU-computed world positions |
| InverseView | Matrix4x4 | Camera cameraToWorldMatrix |
| InverseProjection | Matrix4x4 | projectionMatrix.inverse |
| RayParams | Vector4 | (0, 0, tan(fov/2)*aspect, tan(fov/2)) |
| DepthRange | Vector2 | (minDepth=0.1, maxDepth=10.0) |

### 11.2 Hand Tracking Properties

| Property | Type | Description |
|----------|------|-------------|
| HandPosition | Vector3 | Wrist world position |
| HandVelocity | Vector3 | Velocity vector |
| HandSpeed | float | Velocity magnitude |
| BrushWidth | float | Pinch distance (0.01-0.5m) |
| IsPinching | bool | Active pinch state |
| TrailLength | float | Speed × multiplier |

### 11.3 Audio Properties

| Property | Type | Description |
|----------|------|-------------|
| AudioVolume | float | 0-1 overall amplitude |
| AudioBass | float | 0-1 low frequency (0-250Hz) |
| AudioMid | float | 0-1 mid frequency (250-2kHz) |
| AudioTreble | float | 0-1 high frequency (2k-20kHz) |
| AudioSubBass | float | 0-1 sub-bass (0-60Hz) |
| AudioPitch | float | 0-1 dominant frequency |

### 11.4 Property Name Variants

| Standard | Alternates |
|----------|------------|
| InverseView | InverseViewMatrix |
| RayParams | ProjectionVector |
| StencilMap | HumanStencil, Stencil Map |
| PositionMap | Position Map |
| DepthMap | DepthTexture |
| ColorMap | ColorTexture |

---

## 12. Troubleshooting

### 12.1 Particles Not Visible

**Cause**: Missing RayParams binding

**Fix**: Ensure VFXBinderManager is active and binding RayParams:
```csharp
if (vfx.HasVector4("RayParams"))
    vfx.SetVector4("RayParams", _rayParams);
```

### 12.2 Audio Not Working

**Cause**: Missing EnhancedAudioProcessor

**Fix**: Run `H3M > EchoVision > Setup Audio Input`

### 12.3 Build Fails with CS0234

**Cause**: UnityEditor namespace in runtime code

**Fix**: Wrap Editor-only code:
```csharp
#if UNITY_EDITOR
UnityEditor.AssetDatabase.LoadAssetAtPath(...)
#endif
```

### 12.4 DepthToWorld Kernel Not Found

**Cause**: Compute shader doesn't compile in Editor with Metal

**Solution**: Works on device; gracefully disabled in Editor

### 12.5 Scene Freezes When Switching VFX

**Cause**: Asset swapping causes reinitialization

**Fix**: Use Spawn Control mode in VFXGalleryUI:
```csharp
gallery.useSpawnControlMode = true;
```

---

## Appendix: File Inventory

| Category | Files | Total Lines |
|----------|-------|-------------|
| VFX Scripts | 3 | ~600 |
| Hand Tracking | 2 | ~700 |
| Audio | 2 | ~400 |
| Performance | 3 | ~930 |
| UI | 4 | ~1,650 |
| Editor | 10 | ~2,000 |
| H3M Core | 3 | ~350 |
| **Total** | **27** | **~6,630** |

---

*Document generated by Claude Code deep review - 2026-01-14*
