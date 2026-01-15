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
ColorMap         Texture2D  Camera color
InverseView      Matrix4x4  Inverse view matrix
RayParams        Vector4    (0, 0, tan(fov/2)*aspect, tan(fov/2))
DepthRange       Vector2    Near/far clip (0.1, 10)
```

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
**Inputs**: PositionMap, ColorMap, MapWidth, MapHeight
**Examples**: Spikes.vfx, HumanCube.vfx, Point.vfx

```
Required Properties:
- PositionMap (world-space XYZ from GPU compute)
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

### Property Name Variants
| Standard | Alternate |
|----------|-----------|
| InverseView | InverseViewMatrix |
| RayParams | ProjectionVector |
| StencilMap | HumanStencil, Stencil Map |
| PositionMap | Position Map |

---

## Menu Commands

| Command | Shortcut |
|---------|----------|
| Setup Post-Processing | `H3M > Post-Processing > Setup Post-Processing` |
| Setup HoloKit | `H3M > HoloKit > Setup Complete HoloKit Rig` |
| Setup EchoVision | `H3M > EchoVision > Setup All EchoVision Components` |
| Add VFX Optimizer | `H3M > VFX Performance > Add Auto Optimizer to Scene` |
| Profile VFX | `H3M > VFX Performance > Profile All VFX` |

---

## Script Locations

| Script | Path |
|--------|------|
| VFXGalleryUI | `Scripts/UI/VFXGalleryUI.cs` |
| HandVFXController | `Scripts/HandTracking/HandVFXController.cs` |
| EnhancedAudioProcessor | `Scripts/Audio/EnhancedAudioProcessor.cs` |
| VFXAutoOptimizer | `Scripts/Performance/VFXAutoOptimizer.cs` |
| VFXBinderManager | `Scripts/VFX/VFXBinderManager.cs` |
| MeshVFX | `Echovision/Scripts/MeshVFX.cs` |
| SoundWaveEmitter | `Echovision/Scripts/SoundWaveEmitter.cs` |

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
