# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MetavidoVFX is a Unity 6 AR/VFX demonstration project that visualizes volumetric videos captured with iPhone Pro LiDAR sensors. It combines AR Foundation, VFX Graph, and advanced particle systems for interactive AR experiences on iOS.

**Unity Version**: 6000.2.14f1
**Primary Target**: iOS (ARKit)
**Render Pipeline**: URP 17.2.0

## Build Commands

### iOS Build (Primary)
```bash
./build_and_deploy.sh    # Full cycle: Unity build → Xcode → device install
./build_ios.sh           # Unity build only (generates Xcode project)
```

Build output: `Builds/iOS/Unity-iPhone.xcodeproj`

### Manual Unity Build
```bash
/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "$(pwd)" \
  -buildTarget iOS \
  -executeMethod BuildScript.BuildiOS
```

### Device Debugging
```bash
./debug.sh               # Stream device logs (filters Unity/MetavidoVFX)
idevicesyslog | grep Unity  # Manual fallback
```

## Architecture

### CRITICAL: Live AR Pipeline (Our Approach)

**Key Distinction**: Our pipeline extracts ALL data from live AR Foundation (local device), NOT from:
- ❌ Rcam4: NDI network stream (remote device → PC)
- ❌ MetavidoVFX Original: Encoded .metavido video files

This gives us ~16ms latency, minimal CPU overhead, and excellent mobile performance.

**Optimal for Multi-Hologram**: VFXBinderManager uses ONE compute dispatch for ALL VFX.

| Holograms | GPU Time | Note |
|-----------|----------|------|
| 1 | ~2ms | Single dispatch |
| 10 | ~5ms | Scales well |
| 20 | ~8ms | 60fps feasible |

### Core AR → VFX Pipeline (Updated 2026-01-16)

**✅ IMPLEMENTED**: Hybrid Bridge Pattern - O(1) compute + O(N) lightweight binding

**Implementation Status**: COMPLETE (verified Jan 16, 2026)
- 73 VFX assets organized in Resources/VFX by category
- VFXLibraryManager rewritten for new pipeline (~920 LOC)
- One-click setup and legacy removal working
- Performance: 353 FPS @ 10 active VFX

```
AR Foundation → ARDepthSource (singleton) → VFXARBinder (per-VFX) → VFX
                      ↓                           ↓
           ONE compute dispatch          SetTexture() calls only
                      ↓
           PositionMap, VelocityMap (shared)
```

**Key Components**:
- `Assets/Scripts/Bridges/ARDepthSource.cs` - **PRIMARY** singleton, ONE compute dispatch (~256 LOC)
- `Assets/Scripts/Bridges/VFXARBinder.cs` - Lightweight per-VFX binding (~160 LOC)
- `Assets/Scripts/Bridges/AudioBridge.cs` - FFT audio bands to global shader props (~130 LOC)
- `Assets/Scripts/VFX/VFXLibraryManager.cs` - **NEW** VFX management with pipeline integration (~920 LOC)
- `Assets/Scripts/VFX/VFXPipelineDashboard.cs` - Real-time debug UI (~350 LOC)
- `Assets/Scripts/VFX/VFXTestHarness.cs` - Keyboard shortcuts for testing (~250 LOC)
- `Assets/Scripts/Editor/VFXPipelineMasterSetup.cs` - Editor automation (~500 LOC)
- `Assets/Scripts/Editor/InstantiateVFXFromResources.cs` - **NEW** instantiate from Resources (~90 LOC)
- `Assets/H3M/Core/HologramSource.cs` - Hologram depth (use for anchor/scale features)
- `Assets/Resources/DepthToWorld.compute` - GPU depth→world position conversion

**VFX Organization (Resources/VFX - 73 total)**:
| Category | Count | Examples |
|----------|-------|----------|
| People | 5 | bubbles, glitch, humancube_stencil, particles, trails |
| Environment | 5 | swarm, warp, worldgrid, ribbons, markers |
| NNCam2 | 9 | joints, eyes, electrify, mosaic, tentacles |
| Akvfx | 7 | point, web, spikes, voxel, particles |
| Rcam2 | 20 | HDRP→URP converted body effects |
| Rcam3 | 8 | depth people/environment effects |
| Rcam4 | 14 | NDI-style body effects |
| SdfVfx | 5 | SDF environment effects |

**Legacy (Removed)**:
- `VFXBinderManager.cs` - Replaced by ARDepthSource
- `VFXARDataBinder.cs` - Replaced by VFXARBinder (moved to _Legacy folder)

**Quick Setup**: `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)`

### H3M Hologram System

Located in `Assets/H3M/`:
- `Core/` - HologramSource, HologramRenderer, HologramAnchor
- `Editor/` - Scene setup utilities
- `Network/` - WebRTC for multiplayer holograms

### VFX Properties (Standard Names)
```
DepthMap      - AR depth texture
StencilMap    - Human body mask
PositionMap   - World positions (GPU-computed)
ColorMap      - Camera RGB texture
InverseView   - Inverse view matrix
InverseProj   - Inverse projection matrix
RayParams     - (0, 0, tan(fov/2)*aspect, tan(fov/2)) for UV+depth→3D
DepthRange    - Depth clipping (default 0.1-10m)
```

### Body Segmentation (BodyPixSentis 24-Part)

**Requires**: `BODYPIX_AVAILABLE` scripting define (auto-added if package installed)
**Setup**: `H3M > Body Segmentation > Setup BodyPix Defines`

| Property | Type | Description |
|----------|------|-------------|
| `BodyPartMask` | Texture2D | 24-part segmentation (R channel = part index 0-23, 255=background) |
| `BodyPositionMap` | Texture2D | Torso-only world positions |
| `ArmsPositionMap` | Texture2D | Arms-only world positions |
| `HandsPositionMap` | Texture2D | Hands-only world positions |
| `LegsPositionMap` | Texture2D | Legs+feet world positions |
| `FacePositionMap` | Texture2D | Face-only world positions |
| `KeypointBuffer` | GraphicsBuffer | 17 pose landmarks |

**Body Part Index Reference**:
- 0-1: Face (L/R)
- 2-9: Arms (upper/lower, front/back)
- 10-11: Hands
- 12-13: Torso (front/back)
- 14-21: Legs (upper/lower, front/back)
- 22-23: Feet
- 255: Background

**Key Files**:
- `Assets/Scripts/Segmentation/BodyPartSegmenter.cs` - ML inference wrapper
- `Assets/Resources/SegmentedDepthToWorld.compute` - GPU segmented position maps
- `Assets/Scripts/Editor/BodyPixDefineSetup.cs` - Auto-setup scripting define

## Key Files

| File | Purpose |
|------|---------|
| `Assets/Scenes/HOLOGRAM_Mirror_MVP.unity` | Main build scene |
| `Assets/Editor/BuildScript.cs` | Build automation entry point |
| `Packages/manifest.json` | Package dependencies |
| `Assets/URP/` | URP renderer configurations |

## Dependencies

### Critical Packages
- `com.unity.xr.arfoundation`: 6.2.1
- `com.unity.xr.arkit`: 6.2.1
- `com.unity.visualeffectgraph`: 17.2.0
- `jp.keijiro.metavido`: 5.1.1 (volumetric video)

### Scoped Registry (Keijiro packages)
Registry URL: `https://registry.npmjs.com`
Scopes: `jp.keijiro`

### External Dependencies
- **AR Foundation Remote 2**: Asset Store package, not in version control. Install via `Assets/Plugins/ARFoundationRemoteInstaller/`
- **Unity MCP**: `com.coplaydev.unity-mcp` (Git dependency)

## Editor Testing

Use AR Foundation Remote 2 for fast iteration:
1. Build companion app from `Assets/Plugins/ARFoundationRemoteInstaller/Installer` scene
2. Connect: `Window > AR Foundation Remote > Connection`
3. Press Play - device camera/LiDAR streams to Editor

## On-Device Debugging

InGameDebugConsole is auto-injected during builds:
- **Open**: Tap 3 fingers on screen
- **Purpose**: Catch runtime errors that don't crash the app

## Build Configuration

- **Team ID**: Z8622973EB (auto-set in scripts)
- **Device**: IMClab 15 (configured in build_and_deploy.sh)
- **Xcode**: 16.4 (pinned via DEVELOPER_DIR)

## Common Issues

### DepthToWorld Kernel Not Found
The compute shader `Assets/Shaders/DepthToWorld.compute` must contain a kernel named `DepthToWorld`. Check HologramSource.cs:90 for the FindKernel call.

### Metal Pipeline Errors (ARKitBackgroundEditor)
Depth attachment format mismatch in editor - typically safe to ignore during Editor play mode; resolves on device builds.

### AR Foundation Remote Black Screen
Ensure "Enable AR Remote" is checked in Project Settings and device/editor are on same network.

### Compute Shader Thread Group Mismatch
`DepthToWorld.compute` uses `[numthreads(32,32,1)]`. Dispatch must use `ceil(width/32)` groups, not `ceil(width/8)`. Fixed in VFXBinderManager.cs (2026-01-14).

### HologramRenderer Binding Conflict
HologramRenderer.cs must NOT bind PositionMap to DepthMap property. VFX expecting raw depth would receive computed positions, causing particles to fail. Fixed by removing fallback (2026-01-14).

## H3M Menu Commands

Editor utilities accessible via Unity menu bar:

| Menu | Purpose |
|------|---------|
| **Hologram** | |
| `H3M > Hologram > Setup Complete Hologram Rig` | Instantiate & wire full hologram rig prefab |
| `H3M > Hologram > Add HologramSource Only` | Add source component |
| `H3M > Hologram > Add HologramRenderer to Selected VFX` | Add renderer to existing VFX |
| `H3M > Hologram > Verify Hologram Setup` | Check all wiring |
| `H3M > Hologram > Re-Wire All References` | Fix broken references |
| **HoloKit** | |
| `H3M > HoloKit > Setup HoloKit Defines` | Add HOLOKIT_AVAILABLE define |
| `H3M > HoloKit > Setup Complete HoloKit Rig` | Full HoloKit + hand tracking |
| **General** | |
| `H3M > Post-Processing > Setup Post-Processing` | Create/update Global Volume |
| `H3M > EchoVision > Setup All EchoVision Components` | Full AR/Audio/VFX setup |
| `H3M > VFX Performance > Add Auto Optimizer` | Add FPS-based quality control |
| `H3M > VFX Performance > Profile All VFX` | Analyze VFX performance |
| `H3M > VFX > Auto-Setup Binders` | Auto-add binders based on VFX properties |
| `H3M > VFX > Add Binders to Selected` | Quick setup for selected VFX |
| **VFX Pipeline Master** | |
| `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)` | **One-click**: ARDepthSource + VFXARBinder + Dashboard + Test Harness |
| `H3M > VFX Pipeline Master > Pipeline Components > Create ARDepthSource` | Add singleton compute source |
| `H3M > VFX Pipeline Master > Pipeline Components > Add VFXARBinder to All VFX` | Batch add binders |
| `H3M > VFX Pipeline Master > Pipeline Components > Add VFXARBinder to Selected` | Selected VFX only |
| `H3M > VFX Pipeline Master > Legacy Management > Mark All Legacy (Disable)` | Disable VFXBinderManager, VFXARDataBinder |
| `H3M > VFX Pipeline Master > Legacy Management > Mark All Legacy (Delete)` | Remove legacy components |
| `H3M > VFX Pipeline Master > Legacy Management > Restore Legacy (Re-enable)` | Undo disable |
| `H3M > VFX Pipeline Master > Testing > Add Test Harness` | Keyboard shortcuts (1-9, Space, C, A, P) |
| `H3M > VFX Pipeline Master > Testing > Add Pipeline Dashboard` | Real-time debug UI (Tab toggle) |
| `H3M > VFX Pipeline Master > Testing > Validate All Bindings` | Health check report |
| `H3M > VFX Pipeline Master > VFX Library > Populate All VFX` | Find & categorize all VFX |
| `H3M > VFX Pipeline Master > Create Master Prefab` | Save setup as prefab |
| **Network** | |
| `H3M > Network > Setup WebRTC Receiver` | Create WebRTC receiver for conferencing |
| `H3M > Network > Add WebRTC Binder to Selected` | Add remote stream binder to VFX |
| `H3M > Network > Verify Network Setup` | Check WebRTC configuration |
| **Debug** | |
| `H3M > Debug > Re-enable iOS Components` | Force re-enable HoloKit components after Play mode |

## H3M Hologram Prefabs

Located in `Assets/H3M/Prefabs/`:

| Prefab | Components | Purpose |
|--------|------------|---------|
| `H3M_HologramSource` | HologramSource | Depth→PositionMap compute |
| `H3M_HologramRenderer` | VisualEffect, HologramRenderer | Binds source to VFX |
| `H3M_HologramAnchor` | ARRaycastManager, HologramAnchor | AR plane placement |
| `H3M_HologramRig` | All above + HologramDebugUI | Complete hologram setup |

**H3M_HologramRig Hierarchy:**
```
H3M_HologramRig
├── Source          (HologramSource)
├── Renderer        (VisualEffect + HologramRenderer)
├── Anchor          (ARRaycastManager + HologramAnchor)
└── DebugUI         (UIDocument + HologramDebugUI)
```

**Quick Setup**: `H3M > Hologram > Setup Complete Hologram Rig`

## Data Pipeline Architecture (Updated 2026-01-16)

**Primary Pipeline**: Hybrid Bridge (ARDepthSource + VFXARBinder)

```
┌─────────────────────────────────────────────────────────────────┐
│                   AR Foundation                                  │
│      AROcclusionManager → DepthMap, StencilMap                  │
└─────────────────────┬───────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────────────┐
│              ARDepthSource (singleton)                          │
│    ONE compute dispatch → PositionMap, VelocityMap              │
│    Public properties: DepthMap, StencilMap, PositionMap,        │
│                       VelocityMap, RayParams, InverseView       │
└─────────────────────┬───────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────────────┐
│            VFXARBinder (per-VFX, lightweight)                   │
│    Just SetTexture() calls - NO compute                         │
│    Auto-detects which properties VFX needs                      │
└─────────────────────┬───────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────────────────┐
│                    VFX Graph                                     │
│    DepthMap, StencilMap, PositionMap, VelocityMap, ColorMap    │
│    RayParams, InverseView (via HLSL global access)             │
└─────────────────────────────────────────────────────────────────┘
```

| Pipeline | Status | Data Bound |
|----------|--------|------------|
| **ARDepthSource + VFXARBinder** | ✅ **PRIMARY** | DepthMap, StencilMap, PositionMap, VelocityMap, RayParams |
| **AudioBridge** | ✅ Audio | _AudioBands (global Vector4), _AudioVolume (global float) |
| **HologramSource/Renderer** | ✅ H3M | Use for anchor/scale features |
| **HandVFXController** | ✅ Hands | HandPosition, HandVelocity, BrushWidth |
| **NNCamKeypointBinder** | ✅ Keypoints | KeypointBuffer (17 pose landmarks) |
| **VFXBinderManager** | ❌ LEGACY | Replaced by ARDepthSource |
| **VFXARDataBinder** | ❌ LEGACY | Replaced by VFXARBinder |
| **EnhancedAudioProcessor** | ❌ LEGACY | Replaced by AudioBridge |

**Setup**: `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)`
**Verify**: `H3M > VFX Pipeline Master > Testing > Validate All Bindings`

## New Systems

### VFX Pipeline Tools (2026-01-16)

**One-click automation** for VFX pipeline setup, testing, and debugging.

**Components**:
- `Assets/Scripts/Bridges/ARDepthSource.cs` - Singleton compute source (O(1) dispatches)
- `Assets/Scripts/Bridges/VFXARBinder.cs` - Lightweight per-VFX binding (O(N) SetTexture)
- `Assets/Scripts/Bridges/AudioBridge.cs` - FFT audio → global shader vectors
- `Assets/Scripts/VFX/VFXPipelineDashboard.cs` - Real-time IMGUI debug overlay
- `Assets/Scripts/VFX/VFXTestHarness.cs` - Keyboard shortcuts for rapid testing
- `Assets/Scripts/Editor/VFXPipelineMasterSetup.cs` - All editor automation

**Dashboard Features** (Toggle: Tab key):
- FPS graph (60-frame history, min/avg/max)
- Pipeline flow visualization (ARDepthSource → VFXARBinder → VFX)
- Binding status (green/red indicators)
- Memory usage (RenderTexture allocations)
- Active VFX list with particle counts

**Test Harness Keyboard Shortcuts**:
- `1-9`: Select VFX by index (or favorites)
- `Space`: Cycle to next VFX
- `C`: Cycle categories (People, Hands, Audio, Environment)
- `A`: Toggle all VFX on/off
- `P`: Toggle auto-cycle (profiling mode)
- `R`: Refresh VFX list
- `Tab`: Toggle Dashboard

**Setup**: `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)`

### VFX Library System (Updated 2026-01-16)

**✅ REWRITTEN** for Hybrid Bridge Pipeline integration (~920 LOC):
- `Assets/Scripts/VFX/VFXLibraryManager.cs` - **NEW** Pipeline-aware VFX management
  - One-click `SetupCompletePipeline()` - creates ARDepthSource, adds VFXARBinder, removes legacy
  - Auto-loads 73 VFX from Resources/VFX organized by category
  - `RemoveAllLegacyComponents()` - removes VFXBinderManager, VFXARDataBinder
  - `AutoDetectAllBindings()` - refreshes VFXARBinder bindings
- `Assets/Scripts/UI/VFXToggleUI.cs` - UI Toolkit panel with 4 modes (Auto/Standalone/Embedded/Programmatic)
- `Assets/Scripts/Editor/VFXLibrarySetup.cs` - Editor setup utilities (`H3M > VFX Library`)
- `Assets/Scripts/Editor/VFXLibraryManagerEditor.cs` - Custom Inspector with pipeline controls
- `Assets/Scripts/Editor/InstantiateVFXFromResources.cs` - **NEW** Batch VFX instantiation

**Quick Setup**:
- `H3M > VFX Pipeline Master > Setup Complete Pipeline (Recommended)` - Full pipeline + library
- `H3M > VFX Library > Setup Complete System` - Library only
- Context Menu: Right-click VFXLibraryManager → "Setup Complete Pipeline"

**VFX Count**: 73 VFX in Resources/VFX (People, Environment, NNCam2, Akvfx, Rcam2-4, SdfVfx)

### VFX Management
- `Assets/Scripts/VFX/VFXCategory.cs` - Categorizes VFX by binding requirements (with SetCategory method)
- `Assets/Scripts/VFX/VFXBinderManager.cs` - Unified data binding for all VFX
- `Assets/Scripts/UI/VFXGalleryUI.cs` - World-space gaze-and-dwell VFX selector
- `Assets/Scripts/UI/VFXSelectorUI.cs` - UI Toolkit VFX selector (screen-space)

### Hand Tracking
- `Assets/Scripts/HandTracking/HandVFXController.cs` - Velocity-driven VFX with pinch detection
- `Assets/Scripts/HandTracking/ARKitHandTracking.cs` - XR Hands fallback
- `Assets/Scripts/Editor/HoloKitHandTrackingSetup.cs` - HoloKit rig setup

### Audio System
- `Assets/Scripts/Audio/EnhancedAudioProcessor.cs` - Frequency band analysis
- `Assets/Echovision/Scripts/SoundWaveEmitter.cs` - Expanding sound waves for VFX

### Performance
- `Assets/Scripts/Performance/VFXAutoOptimizer.cs` - FPS tracking + auto quality
- `Assets/Scripts/Performance/VFXLODController.cs` - Distance-based quality
- `Assets/Scripts/Performance/VFXProfiler.cs` - VFX analysis and recommendations

### EchoVision
- `Assets/Echovision/Scripts/MeshVFX.cs` - AR mesh → VFX GraphicsBuffers
- `Assets/Scripts/VFX/HumanParticleVFX.cs` - AR depth → world positions via compute

### NNCam (Keypoint VFX)
- `Assets/NNCam/Scripts/NNCamKeypointBinder.cs` - Binds KeypointBuffer from BodyPartSegmenter
- `Assets/NNCam/Scripts/NNCamVFXSwitcher.cs` - Keyboard/InputAction VFX switching
- `Assets/NNCam/Scripts/Editor/NNCamSetup.cs` - Scene setup utilities
- `Assets/VFX/NNCam2/*.vfx` - 9 ported NNCam2 VFX (Eyes, Joints, Electrify, etc.)

**Setup**: `H3M > NNCam > Setup NNCam VFX Scene`

### H3M Network (WebRTC Video Conferencing)
- `Assets/H3M/Network/H3MSignalingClient.cs` - WebSocket signaling for peer discovery
- `Assets/H3M/Network/H3MWebRTCReceiver.cs` - Receives hologram video streams
- `Assets/H3M/Network/H3MWebRTCVFXBinder.cs` - Binds remote streams to VFX
- `Assets/H3M/Network/H3MStreamMetadata.cs` - Camera position/projection metadata
- `Assets/H3M/Network/Editor/H3MNetworkSetup.cs` - Editor setup utilities

**Requires**: `com.unity.webrtc` package + `UNITY_WEBRTC_AVAILABLE` scripting define
**Setup**: `H3M > Network > Setup WebRTC Receiver`

**WebRTC Properties bound:**
```
ColorMap         Texture2D  Remote camera color
DepthMap         Texture2D  Remote depth
RayParams        Vector4    Inverse projection for rays
InverseView      Matrix4x4  Remote camera transform
DepthRange       Vector2    Near/far clipping
```

### VFX Binders (Runtime VFX Support)
- `Assets/Scripts/VFX/Binders/VFXARDataBinder.cs` - AR data for spawned VFX
- `Assets/Scripts/VFX/Binders/VFXAudioDataBinder.cs` - Audio bands for spawned VFX
- `Assets/Scripts/VFX/Binders/VFXHandDataBinder.cs` - Hand tracking for spawned VFX
- `Assets/Scripts/VFX/Binders/VFXPhysicsBinder.cs` - Optional velocity & gravity for spawned VFX
- `Assets/Scripts/VFX/Binders/VFXBinderUtility.cs` - Auto-detect and setup helper
- `Assets/Scripts/Editor/VFXAutoBinderSetup.cs` - Editor window for batch setup

## VFX Properties Reference

### Hand Tracking Properties
```
HandPosition     Vector3    World position of wrist
HandVelocity     Vector3    Velocity vector
HandSpeed        float      Velocity magnitude
BrushWidth       float      Pinch-controlled width
IsPinching       bool       True during pinch
```

### Audio Properties
```
AudioVolume      float      0-1 overall volume
AudioBass        float      0-1 low frequency
AudioMid         float      0-1 mid frequency
AudioTreble      float      0-1 high frequency
```

### AR Depth Properties
```
DepthMap         Texture2D  Environment depth
StencilMap       Texture2D  Human segmentation
PositionMap      Texture2D  World positions (GPU-computed)
ColorMap         Texture2D  Camera color
InverseView      Matrix4x4  Inverse view matrix
InverseProjection Matrix4x4 Inverse projection matrix
RayParams        Vector4    (0, 0, tan(fov/2)*aspect, tan(fov/2))
DepthRange       Vector2    Near/far clipping (default 0.1-10m)
```

### Physics Properties (Optional)
```
Velocity         Vector3    Camera velocity (smoothed)
ReferenceVelocity Vector3   Alias for Velocity (warp VFX)
Speed            float      Velocity magnitude
CameraSpeed      float      Alias for Speed
Gravity          Vector3    Gravity vector (default 0,-9.81,0)
Gravity Vector   Vector3    Alias for Gravity (Sparks VFX style)
GravityStrength  float      Gravity Y-axis (-20 to 20)
GravityY         float      Alias for GravityStrength
```

### Throttle/Intensity Properties (Optional)
```
Throttle         float      0-1 overall VFX intensity
Intensity        float      Alias for Throttle
Scale            float      Alias for Throttle
```

### Normal Map Properties (Optional)
```
NormalMap        Texture2D  Surface normals (computed from depth)
Normal Map       Texture2D  Alias for NormalMap
```

### Keypoint Buffer (BodyPix)
```
KeypointBuffer   GraphicsBuffer  17 pose landmarks from BodyPartSegmenter
```

**Enable via VFXBinderManager Inspector:**
- `Enable Velocity Binding` - toggles camera velocity input
- `Velocity Scale` - multiplier (0.1-10)
- `Enable Gravity Binding` - toggles gravity input
- `Gravity Strength` - Y-axis value (-20 to 20)

**Enable via VFXARDataBinder Inspector (per-VFX):**
- `Bind Throttle` - toggles throttle/intensity input
- `Bind Normal Map` - toggles surface normal computation
- `Bind Velocity` - toggles camera velocity input
- `Bind Gravity` - toggles gravity input
- `Bind Audio` - toggles audio frequency band input

**Runtime API:**
```csharp
// VFXBinderManager (global)
VFXBinderManager.SetVelocityBindingEnabled(bool)
VFXBinderManager.SetGravityBindingEnabled(bool)
VFXBinderManager.SetGravityStrength(float)
VFXBinderManager.GetCameraVelocity()  // Vector3
VFXBinderManager.GetCameraSpeed()     // float

// VFXARDataBinder (per-VFX)
vfxARDataBinder.SetThrottle(float)          // 0-1
vfxARDataBinder.SetVelocityEnabled(bool)
vfxARDataBinder.SetGravityEnabled(bool)
vfxARDataBinder.SetGravityStrength(float)   // -20 to 20
```

## Project Statistics (2026-01-16)

| Metric | Count |
|--------|-------|
| C# Scripts | 458 |
| VFX Assets | 88 |
| Unity Version | 6000.2.14f1 |
| iOS Minimum | 15.0 |
| Performance | 353 FPS @ 10 VFX |

**VFX by Category**:
- Rcam2: 20 (HDRP→URP converted Jan 14)
- Rcam4: 14
- NNCam2: 9 (keypoint-driven, added Jan 16)
- Rcam3: 8
- Akvfx: 7
- Resources/VFX: 14 (core Metavido)
- Environment/SDF/Other: 16

**Conditional Compilation**:
- `HOLOKIT_AVAILABLE` - HoloKit hand tracking (15 uses)
- `BODYPIX_AVAILABLE` - BodyPix 24-part segmentation (14 uses)
- `UNITY_XR_HANDS` - XR Hands fallback (5 uses)

## Documentation

In-project documentation:
- `Assets/Documentation/README.md` - Complete system documentation
- `Assets/Documentation/QUICK_REFERENCE.md` - Properties cheat sheet
- `Assets/Documentation/PIPELINE_ARCHITECTURE.md` - All pipelines deep dive
- `Assets/Documentation/SYSTEM_ARCHITECTURE.md` - 90% complete architecture docs
- `Assets/Documentation/CODEBASE_AUDIT_2026-01-15.md` - Bug fixes and known issues
- `Assets/Documentation/VFX_NAMING_CONVENTION.md` - VFX naming standards
- `Assets/Documentation/VFX_PIPELINE_FINAL_RECOMMENDATION.md` - Hybrid Bridge architecture

## Knowledgebase

Extended documentation in parent repo:
- `../KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR/VFX code patterns
- `../KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - 520+ reference repos
- `../KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` - Hologram/portal patterns
- `../KnowledgeBase/_HOLOGRAM_RECORDING_PLAYBACK.md` - Recording/playback specs
- `../KnowledgeBase/_CLAUDE_CODE_UNITY_WORKFLOW.md` - Claude Code + Unity MCP workflow patterns

## Specifications

Project specifications in parent repo:
- `../specs/002-h3m-foundation/` - H3M MVP (Man in the Mirror)
- `../specs/003-hologram-conferencing/` - Recording/playback/multiplayer
- `../specs/004-metavidovfx-systems/` - VFX systems implementation status
