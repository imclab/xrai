# MetavidoVFX Quick Reference

## VFX Properties Cheat Sheet

### Hand Tracking Properties
```
HandPosition     Vector3    World position of wrist
HandVelocity     Vector3    Velocity vector
HandSpeed        float      Velocity magnitude
TrailLength      float      For trail effects
BrushWidth       float      Pinch-controlled width
IsPinching       bool       True during pinch
```

### Audio Properties
```
AudioVolume      float      0-1 overall volume
AudioBass        float      0-1 low frequency
AudioMid         float      0-1 mid frequency
AudioTreble      float      0-1 high frequency
AudioPitch       float      Estimated pitch
```

### AR Depth Properties
```
DepthMap         Texture2D  Environment depth
StencilMap       Texture2D  Human segmentation
PositionMap      Texture2D  World positions (GPU computed)
VelocityMap      Texture2D  Frame-to-frame motion (xyz=velocity, w=speed)
ColorMap         Texture2D  Camera color
InverseView      Matrix4x4  Inverse view matrix
RayParams        Vector4    (centerX, centerY, tan(fov/2)*aspect, tan(fov/2))
DepthRange       Vector2    Near/far clip (0.1, 10)
```

### Body Segmentation Properties (BodyPixSentis 24-Part)
```
BodyPartMask     Texture2D  24-part segmentation (R=part index 0-23)
SegmentationMask Texture2D  Alias for BodyPartMask
KeypointBuffer   Buffer     17 pose keypoints (x,y,score)

# Segmented Position Maps (world-space XYZ per body region)
BodyPositionMap  Texture2D  Torso only (parts 12-13)
TorsoPositionMap Texture2D  Alias for BodyPositionMap
ArmsPositionMap  Texture2D  Arms only (parts 2-9)
HandsPositionMap Texture2D  Hands only (parts 10-11)
LegsPositionMap  Texture2D  Legs + feet (parts 14-23)
FacePositionMap  Texture2D  Face only (parts 0-1)

# Individual Keypoints (17 pose landmarks)
NosePosition          Vector3   Keypoint 0
LeftShoulderPosition  Vector3   Keypoint 5
RightShoulderPosition Vector3   Keypoint 6
LeftWristPosition     Vector3   Keypoint 9
RightWristPosition    Vector3   Keypoint 10
# ... (17 total - see BodyPartSegmenter.cs)
```

**Body Part Index Reference** (BodyPixSentis):
| Index | Body Part | Index | Body Part |
|-------|-----------|-------|-----------|
| 0-1 | Face (L/R) | 12-13 | Torso (F/B) |
| 2-5 | Upper Arms | 14-17 | Upper Legs |
| 6-9 | Lower Arms | 18-21 | Lower Legs |
| 10-11 | Hands | 22-23 | Feet |
| 255 | Background | | |

### Mesh VFX Properties
```
MeshPointCache   Buffer     Vertex positions
MeshNormalCache  Buffer     Vertex normals
MeshPointCount   int        Active vertex count
```

### Sound Wave Properties
```
WaveOrigin       Vector3    Emission point
WaveDirection    Vector3    Wave direction
WaveRange        float      Current radius
WaveAngle        float      Cone angle degrees
WaveAge          float      Normalized age 0-1
```

---

## VFX Categories by Input Requirements

### Category 1: Body/People VFX (Depth-Based)
**Path**: `VFX/Metavido/`, `VFX/Rcam*/Body/`, `H3M/VFX/`
**Inputs**: DepthMap, ColorMap, InverseView, RayParams, DepthRange
**Examples**: Particles.vfx, Voxels.vfx, Hologram.vfx, Sparkles.vfx

```
Required Properties:
- DepthMap (raw AR depth)
- ColorMap (camera color)
- InverseView / InverseViewMatrix
- RayParams / ProjectionVector
- DepthRange (optional)
- Spawn (control)
```

### Category 2: PositionMap VFX (Pre-Computed)
**Path**: `VFX/Akvfx/`, `VFX/HumanEffects/`
**Inputs**: PositionMap, VelocityMap, ColorMap, MapWidth, MapHeight
**Examples**: Spikes.vfx, HumanCube.vfx, Point.vfx

```
Required Properties:
- PositionMap (world-space XYZ from GPU compute)
- VelocityMap (optional, xyz=velocity, w=speed magnitude)
- ColorMap
- MapWidth, MapHeight (optional)
```

### Category 3: Environment VFX (No AR Input)
**Path**: `VFX/Environment/`, `VFX/Rcam*/EnvFX/`, `VFX/SdfVfx/`
**Inputs**: Spawn, Throttle, Yaw (minimal)
**Examples**: Swarm.vfx, Warp.vfx, Blocks.vfx

```
Required Properties:
- Spawn (control)
- Throttle (intensity)
- No AR textures needed
```

### Category 4: Mesh/Audio VFX (EchoVision)
**Path**: `Echovision/VFX/`
**Inputs**: MeshPointCache, MeshNormalCache, Wave*, HumanStencilTexture
**Examples**: BufferedMesh.vfx

```
Required Properties:
- MeshPointCache (GraphicsBuffer)
- MeshNormalCache (GraphicsBuffer)
- MeshPointCount
- WaveOrigin, WaveRange, WaveAngle, WaveAge
- HumanStencilTexture (optional)
```

### Category 5: Segmented Body VFX (BodyPixSentis)
**Path**: `VFX/Segmented/`, `VFX/BodyParts/`
**Inputs**: BodyPartMask, *PositionMap (segmented), KeypointBuffer
**Examples**: SegmentedHologram.vfx, BodyGlow.vfx
**Requires**: BODYPIX_AVAILABLE scripting define

```
Required Properties:
- BodyPartMask (24-part segmentation)
- BodyPositionMap / ArmsPositionMap / HandsPositionMap / LegsPositionMap / FacePositionMap
- KeypointBuffer (optional, 17 pose landmarks)
- ColorMap (camera color)
```

**Use Case**: Apply different VFX effects to different body regions:
- Fire on hands, ice on torso
- Face-only glow effects
- Limb-specific particle trails

### Property Name Variants
| Standard | Alternate | Notes |
|----------|-----------|-------|
| InverseView | InverseViewMatrix | Camera inverse view |
| InverseProjection | InvProjMatrix | Camera inverse projection |
| RayParams | ProjectionVector | UV→ray conversion Vector4 |
| StencilMap | HumanStencil, Stencil Map | Human segmentation |
| PositionMap | Position Map | World positions |
| VelocityMap | Velocity Map | Frame motion |
| ColorMap | Color Map | Camera RGB |
| DepthMap | DepthTexture | AR depth |

### Depth Projection Methods (All Supported)
| Method | Property | VFX Source | Description |
|--------|----------|------------|-------------|
| **RayParams** | `RayParams` (Vector4) | Metavido, Rcam4 | `(0, 0, tan(fov/2)*aspect, tan(fov/2))` |
| **InverseProjection** | `InverseProjection` (Matrix4x4) | Rcam3, Rcam4 | Full inverse projection matrix |
| **ProjectionVector** | `ProjectionVector` (Vector4) | Rcam2 | Same format as RayParams |
| **PositionMap** | `PositionMap` (Texture2D) | Akvfx, H3M | Pre-computed world positions |

**VFX Graph Audit Results (2026-01-16):**
- 44 Depth VFX ✓ (all have valid projection)
- 9 Stencil VFX ✓ (use PositionMap)
- 23 Environment VFX ✓ (no AR inputs needed)
- 9 NNCam VFX ✓ (use KeypointBuffer)

---

## Menu Commands

| Command | Shortcut |
|---------|----------|
| Setup Post-Processing | `H3M > Post-Processing > Setup Post-Processing` |
| Setup HoloKit | `H3M > HoloKit > Setup Complete HoloKit Rig` |
| Setup EchoVision | `H3M > EchoVision > Setup All EchoVision Components` |
| Add VFX Optimizer | `H3M > VFX Performance > Add Auto Optimizer to Scene` |
| Profile VFX | `H3M > VFX Performance > Profile All VFX` |
| Auto-Setup ALL VFX | `H3M > VFX > Auto-Setup ALL VFX (One-Click)` |
| Auto-Setup Binders | `H3M > VFX > Auto-Setup Binders` |
| Add Binders to Selected | `H3M > VFX > Add Binders to Selected` |

---

## Script Locations

| Script | Path |
|--------|------|
| VFXGalleryUI | `Scripts/UI/VFXGalleryUI.cs` |
| HandVFXController | `Scripts/HandTracking/HandVFXController.cs` |
| EnhancedAudioProcessor | `Scripts/Audio/EnhancedAudioProcessor.cs` |
| VFXAutoOptimizer | `Scripts/Performance/VFXAutoOptimizer.cs` |
| VFXBinderManager | `Scripts/VFX/VFXBinderManager.cs` |
| BodyPartSegmenter | `Scripts/Segmentation/BodyPartSegmenter.cs` |
| MeshVFX | `Echovision/Scripts/MeshVFX.cs` |
| SoundWaveEmitter | `Echovision/Scripts/SoundWaveEmitter.cs` |
| VFXARDataBinder | `Scripts/VFX/Binders/VFXARDataBinder.cs` |
| VFXAudioDataBinder | `Scripts/VFX/Binders/VFXAudioDataBinder.cs` |
| VFXHandDataBinder | `Scripts/VFX/Binders/VFXHandDataBinder.cs` |
| VFXBinderUtility | `Scripts/VFX/Binders/VFXBinderUtility.cs` |
| SegmentedDepthToWorld | `Resources/SegmentedDepthToWorld.compute` |

---

## Runtime VFX Setup

For VFX spawned at runtime, use `VFXBinderUtility`:

```csharp
// Auto-detect needed binders from VFX properties
VFXBinderUtility.SetupVFXAuto(myVFX);

// Or specify preset explicitly
VFXBinderUtility.SetupVFX(myVFX, VFXBinderPreset.ARWithAudio);

// Available presets:
// - None, AROnly, AudioOnly, HandOnly
// - ARWithAudio, ARWithHand, Full
```

---

## Pipeline Selection Guide

| Scenario | Use This Pipeline |
|----------|-------------------|
| Scene VFX (any) | VFXBinderManager (auto-finds all) |
| Runtime-spawned VFX | VFXBinderUtility.SetupVFXAuto() |
| H3M Holograms | HologramSource + HologramRenderer |
| Hand-driven effects | HandVFXController |
| Audio-reactive effects | EnhancedAudioProcessor |
| AR mesh particles | MeshVFX |

---

## Common Fixes

### Scene Freezing
```csharp
// Use spawn control mode
gallery.useSpawnControlMode = true;

// Or disable VFX component
vfx.enabled = false;
```

### Post-Processing Not Visible
```csharp
// Enable on camera
camera.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
```

### Hand Tracking Missing
```
// Add scripting define
HOLOKIT_AVAILABLE
UNITY_XR_HANDS
```

### Body Segmentation Not Working
```
// 1. Add scripting define
BODYPIX_AVAILABLE

// 2. Assign ResourceSet to BodyPartSegmenter
// Get from: Packages/jp.keijiro.bodypix/Resources/

// 3. Check VFXBinderManager settings
VFXBinderManager:
  - useBodySegmentation = true
  - computeSegmentedPositionMaps = true
```

---

## VFX Library System (2026-01)

### Overview
Runtime VFX management system with UI Toolkit integration and Editor persistence.

**Key Components:**
| Component | Purpose |
|-----------|---------|
| `VFXLibraryManager` | Loads/manages VFX from Resources, supports Editor persistence |
| `VFXToggleUI` | UI Toolkit panel for VFX selection (4 UI modes) |
| `VFXCategory` | Categorizes VFX by binding requirements |
| `VFXLibrarySetup` | Editor setup utilities (`H3M > VFX Library`) |

### VFXToggleUI Modes
```csharp
public enum UIMode
{
    Auto,           // Use UXML if available, else create programmatically
    Standalone,     // Own UIDocument with VFXLibrary.uxml
    Embedded,       // Inject into external UIDocument container
    Programmatic    // Create all UI in code (no UXML needed)
}
```

### Editor Persistence
VFX created in Editor persist across play/stop:
```csharp
// Right-click VFXLibraryManager → "Populate Library (Editor - Persistent)"
// Uses Undo.RegisterCreatedObjectUndo for persistence
```

### Setup Commands
| Menu | Purpose |
|------|---------|
| `H3M > VFX Library > Setup Complete System` | ALL_VFX + HUD-UI-VFX |
| `H3M > VFX Library > Setup ALL_VFX` | Create ALL_VFX parent |
| `H3M > VFX Library > Setup HUD-UI` | Create UI panel |
| `H3M > VFX Library > Populate Library` | Load VFX from Resources |
| `H3M > VFX Library > List VFX in Resources` | Debug: show available VFX |

### VFX Library File Locations
| File | Path |
|------|------|
| VFXLibraryManager | `Scripts/VFX/VFXLibraryManager.cs` |
| VFXToggleUI | `Scripts/UI/VFXToggleUI.cs` |
| VFXCategory | `Scripts/VFX/VFXCategory.cs` |
| VFXLibrarySetup | `Scripts/Editor/VFXLibrarySetup.cs` |
| VFXLibrary.uxml | `UI/VFXLibrary.uxml` |
| VFXLibrary.uss | `UI/VFXLibrary.uss` |

---

## Build Commands

```bash
# iOS build
./build_ios.sh

# Full deploy cycle
./build_and_deploy.sh

# Debug device logs
./debug.sh
```
