# MetavidoVFX System Architecture

**Generated**: 2026-01-14, Updated: 2026-01-17
**Unity Version**: 6000.2.14f1
**Platform**: iOS (ARKit)
**Render Pipeline**: URP 17.2.0
**Primary Pipeline**: ARDepthSource + VFXARBinder (Hybrid Bridge Pattern)

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
│   ├── Bridges/                     # Primary AR -> VFX pipeline (Hybrid Bridge)
│   │   ├── ARDepthSource.cs          # Single compute dispatch (Depth/Stencil -> PositionMap)
│   │   ├── VFXARBinder.cs            # Per-VFX binding (SetTexture)
│   │   ├── AudioBridge.cs            # Audio bands -> global shader props
│   │   └── DirectDepthBinder.cs      # Zero-compute binder for new VFX
│   ├── VFX/                          # VFX management + tools
│   │   ├── VFXCategory.cs            # VFX categorization & binding requirements
│   │   ├── VFXLibraryManager.cs      # VFX catalog + pipeline setup
│   │   ├── VFXPipelineDashboard.cs   # Runtime pipeline dashboard
│   │   ├── VFXTestHarness.cs         # Keyboard test harness
│   │   └── VFXProxBuffer.cs          # Global buffers for proximity VFX
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
│   ├── _Legacy/                      # Legacy pipelines (do not use)
│   │   ├── VFXBinderManager.cs       # Legacy centralized binder
│   │   ├── VFXARDataBinder.cs        # Legacy per-VFX binder
│   │   └── PeopleOcclusionVFXManager.cs  # Legacy runtime spawner
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
│   ├── DepthToWorld.compute          # Depth->world position conversion (runtime)
│   └── VFX/                          # VFX assets loaded at runtime
├── Shaders/
│   └── DepthToWorld.compute          # Depth->world position conversion (editor load)
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

### 3.1 Primary Pipeline: Hybrid Bridge (ARDepthSource + VFXARBinder)

Single compute dispatch with lightweight per-VFX binding. This is the current, recommended pipeline.

**Locations**:
- `Assets/Scripts/Bridges/ARDepthSource.cs` (singleton compute source)
- `Assets/Scripts/Bridges/VFXARBinder.cs` (per-VFX binder)

**Data Flow**:
```
AR Foundation → ARDepthSource → PositionMap/DepthMap/StencilMap/ColorMap
                                      ↓
                               VFXARBinder (per VFX)
                                      ↓
                                   VFX Graph
```

**Key Responsibilities**:
- Pulls depth and stencil from `AROcclusionManager` (prefers human depth, falls back to environment)
- Optional portrait rotation via `RotateUV90CW` for iOS depth orientation
- Computes `PositionMap` with `DepthToWorld.compute` (single dispatch per frame)
- Computes `RayParams` using projection matrix center shift
- Exposes `DepthMap`, `StencilMap`, `PositionMap`, `ColorMap`, `RayParams`, `InverseView`
- Optional `VelocityMap` (disabled by default)
- Editor-only mock textures for pipeline testing without a device

**Debug Hooks**:
- `ARDepthSource` context menu: `Debug Source`, `Enable Verbose Logging`
- `VFXARBinder` context menu: `Debug Binder`, `Auto-Detect Bindings`

### 3.2 DirectDepthBinder (Zero-Compute Option)

`Assets/Scripts/Bridges/DirectDepthBinder.cs` passes raw depth + camera params directly to VFX.
Use this for new VFX graphs that do their own depth-to-world conversion.

### 3.3 VFX Categories

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

### 3.4 Legacy Pipelines (DISABLED)

| Pipeline | Status | Reason |
|----------|--------|--------|
| VFXBinderManager (`_Legacy/`) | Legacy | Replaced by ARDepthSource |
| VFXARDataBinder (`_Legacy/`) | Legacy | Replaced by VFXARBinder |
| PeopleOcclusionVFXManager (`_Legacy/`) | Legacy | Runtime spawner conflicts with VFX library |
| ARKitMetavidoBinder | Legacy | Per-VFX binding, redundant |

**Cleanup**: use `H3M > VFX Pipeline Master > Legacy Management` menu items to disable or remove legacy components.

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
│   │   ├── humanDepthTexture
│   │   ├── environmentDepthTexture
│   │   └── humanStencilTexture
│   └── TrackedPoseDriver
├── ARDepthSource
├── HologramSource (optional)
├── HandVFXController (optional)
└── VFX (each with VFXARBinder)
    └── VisualEffect + VFXARBinder
```

### 4.2 Depth Processing Pipeline

```
AROcclusionManager.humanDepthTexture (preferred) or environmentDepthTexture
        ↓ (optional RotateUV90CW for iOS portrait)
ARDepthSource.LateUpdate()
        ↓
DepthToWorld.compute (GPU kernel)
        ├── Input: _Depth, _Stencil, _InvVP
        └── Output: _PositionRT (ARGBFloat)
        ↓
VFXARBinder.SetTexture("PositionMap", PositionMap)
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
│  ARKit Sensors → ARCameraManager → Color texture            │
│               → AROcclusionManager → Depth + Stencil        │
│                              │                              │
│                              ▼                              │
│                       ARDepthSource                         │
│      (single compute + optional rotation + mock data)       │
│   Outputs: DepthMap, StencilMap, PositionMap, ColorMap,      │
│            RayParams, InverseView                            │
│                              │                              │
│                              ▼                              │
│                   VFXARBinder (per VFX)                     │
│                              │                              │
│                              ▼                              │
│                           VFX Graph                         │
│                              ▲                              │
│  HandVFXController   AudioBridge   MeshVFX (GraphicsBuffer)  │
│  (hands/gestures)    (audio)       (AR mesh)                 │
│                              │                              │
│                              ▼                              │
│                     Performance Systems                      │
│                 (VFXAutoOptimizer, VFXLOD)                  │
└─────────────────────────────────────────────────────────────┘
```

---

## 11. VFX Property Reference

### 11.1 AR Depth Properties

| Property | Type | Description |
|----------|------|-------------|
| DepthMap | Texture2D | Human depth preferred, environment depth fallback |
| StencilMap | Texture2D | Human segmentation mask (or white) |
| ColorMap | Texture2D | Camera color capture from ARDepthSource |
| PositionMap | Texture2D | GPU-computed world positions (ARGBFloat) |
| VelocityMap | Texture2D | Optional GPU velocity texture (if enabled) |
| InverseView | Matrix4x4 | TRS(camera position, rotation, Vector3.one) |
| InverseProjection | Matrix4x4 | projectionMatrix.inverse (optional bind) |
| RayParams | Vector4 | (centerShiftX, centerShiftY, tanH, tanV) with rotation fix |
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
| InverseProjection | InverseProj |
| RayParams | ProjectionVector |
| StencilMap | HumanStencil, Stencil Map |
| PositionMap | Position Map |
| DepthMap | DepthTexture |
| ColorMap | ColorTexture |

---

## 12. Troubleshooting

### 12.1 VFX Not Visible / No Particles

**Cause**: ARDepthSource not ready, VFXARBinder missing, or property names mismatch.

**Fix**:
1. Ensure `ARDepthSource` exists in the scene and `IsReady` is true.
2. Use `ARDepthSource` context menu `Debug Source` to verify `DepthMap`, `PositionMap`, and `RayParams`.
3. Ensure each VFX has `VFXARBinder` and run `Auto-Detect Bindings`.
4. Confirm VFX properties use expected names (DepthMap, PositionMap, RayParams, InverseView).

### 12.2 PositionMap Is Black or Static

**Cause**: DepthToWorld compute shader missing, kernel name mismatch, or no depth input.

**Fix**:
- Assign `Assets/Shaders/DepthToWorld.compute` to ARDepthSource if missing.
- Confirm kernels `DepthToWorld` and `CalculateVelocity` exist.
- Verify depth textures are non-null in `Debug Source`.
- Ensure dispatch uses `[numthreads(32,32,1)]` sizing (ARDepthSource already does this).

### 12.3 Depth or Stencil Appears Rotated/Mirrored

**Cause**: Portrait rotation mismatch on iOS.

**Fix**:
- Toggle `Rotate Depth Texture` on ARDepthSource.
- Ensure `Hidden/RotateUV90CW` shader exists.
- Re-check `RayParams` sign (ARDepthSource negates tanH when rotated).

### 12.4 ColorMap Is Black or Grayscale

**Cause**: Color capture not happening or ARCameraTextureProvider missing.

**Fix**:
- Ensure ARDepthSource has a valid `ARCameraTextureProvider` in scene.
- Confirm AR camera is set on ARDepthSource.
- Use `Debug Source` to verify `ColorMap` dimensions update each frame.

### 12.5 Editor Has No AR Data

**Cause**: No live device feed.

**Fix**:
- Use AR Foundation Remote (see `ARFoundationRemoteSetup.md`), or
- Enable `Use Mock Data In Editor` on ARDepthSource for offline testing.

### 12.6 Audio Not Working

**Cause**: AudioBridge missing or VFX property names mismatch.

**Fix**:
- Add `AudioBridge` with a valid `AudioSource`.
- Enable `_bindAudio` on VFXARBinder and verify property names.
- For legacy audio-driven VFX, use `EnhancedAudioProcessor`.

### 12.7 Hologram Placement or Scale Wrong

**Cause**: Anchor transform or scale not bound.

**Fix**:
- Set `AnchorTransform` and `HologramScale` on VFXARBinder.
- Use `Enable Hologram Mode` context menu to auto-configure.

### 12.8 Build Fails with CS0234

**Cause**: UnityEditor namespace used in runtime code.

**Fix**:
```csharp
#if UNITY_EDITOR
UnityEditor.AssetDatabase.LoadAssetAtPath(...)
#endif
```

---

*Document updated for Hybrid Bridge architecture - 2026-01-17*
