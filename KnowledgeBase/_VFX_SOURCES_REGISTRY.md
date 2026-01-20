# VFX Sources Registry

**Total VFX**: 223 assets in Resources/VFX
**Categories**: 26 folders
**Last Updated**: 2026-01-20

## VFX Pipeline Types

| Pipeline | Input | Processing | VFX Count |
|----------|-------|------------|-----------|
| **Metavido (Raw Depth)** | DepthMap + RayParams + InverseView | VFX-internal reconstruction | ~44 |
| **H3M Stencil** | PositionMap + StencilMap | DepthToWorld.compute | ~9 |
| **NNCam2 Keypoints** | KeypointBuffer (17 landmarks) | BodyPartSegmenter | 9 |
| **Environment** | Spawn control only | None | ~23 |
| **Audio Reactive** | AudioVolume + AudioBands | AudioBridge FFT | ~12 |
| **SDF-based** | SDF textures | Particle shaping | ~10 |

## VFX Sources by Origin

### Core MetavidoVFX (Original)
- **Location**: `Resources/VFX/People`, `Environment`
- **Count**: ~14 VFX
- **Pipeline**: Metavido Raw Depth

### Keijiro Projects
| Source | Repo | VFX Count | Category |
|--------|------|-----------|----------|
| **Rcam2** | keijiro/Rcam2 | 20 | HDRP→URP converted |
| **Rcam3** | keijiro/Rcam3 | 8 | Depth people/env |
| **Rcam4** | keijiro/Rcam4 | 14 | NDI-style body |
| **Akvfx** | keijiro/Akvfx | 7 | Azure Kinect |
| **Khoreo** | keijiro/Khoreo | 7 | Stage performance |
| **Fluo** | keijiro/Fluo | 8 | Brush/painting |
| **Smrvfx** | keijiro/Smrvfx | 2 | Skinned mesh |
| **SdfVfx** | keijiro/SdfVfx | 5 | SDF generation |
| **VfxGraphTestbed** | keijiro/VfxGraphTestbed | 16 | Experimental |
| **SplatVFX** | keijiro/SplatVFX | 3 | Gaussian splatting |

### HoloKit/Holoi Projects
| Source | Repo | VFX Count | Category |
|--------|------|-----------|----------|
| **Buddha** | holoi/touching-hologram | 21 | Hand-tracked mesh |
| **NNCam2** | jp.keijiro.nncam2 | 9 | Keypoint-driven |

### Unity Official
| Source | Origin | VFX Count | Category |
|--------|--------|-----------|----------|
| **UnitySamples** | Procedural VFX Library | 20 | Learning templates |
| **Portals6** | Unity Portals Demo | 22 | Portal effects |

### Third-Party Projects
| Source | Repo | VFX Count | Category |
|--------|------|-----------|----------|
| **FaceTracking** | mao-test-h/FaceTrackingVFX | 2 | ARKit face mesh |
| **MinimalCompute** | cinight/MinimalCompute | 2 | Compute examples |
| **MyakuMyakuAR** | plantblobs | 1 | AR character |
| **TamagotchU** | EyezLee/TamagotchU | 4 | Virtual pet |
| **WebRTC** | URP-WebRTC-Convai | 7 | Trails + SDF |
| **Essentials** | VFX-Essentials-main | 22 | Boids, noise, waveform, etc. |
| **Dcam2** | keijiro/Dcam2 | 13 | Depth camera visualizer |

## Key VFX Property Bindings

### AR Pipeline (ARDepthSource)
```
DepthMap      Texture2D  Raw AR depth (RFloat)
StencilMap    Texture2D  Human segmentation mask
PositionMap   Texture2D  GPU-computed world XYZ
ColorMap      Texture2D  Camera RGB
VelocityMap   Texture2D  Motion vectors
RayParams     Vector4    (0, 0, tan(fov/2)*aspect, tan(fov/2))
InverseView   Matrix4x4  Camera inverse view
DepthRange    Vector2    Near/far clip (0.1-10m)
```

### Audio Pipeline (AudioBridge)
```
AudioVolume   float      0-1 overall volume
AudioBands    Vector4    (bass, mid, treble, sub)
```

### Keypoint Pipeline (NNCamKeypointBinder)
```
KeypointBuffer  GraphicsBuffer  17 pose landmarks
```

## Folder Structure

```
Resources/VFX/
├── People/          (5)   Core body effects
├── Environment/     (5)   World-space effects
├── NNCam2/          (9)   Keypoint-driven
├── Akvfx/           (7)   Azure Kinect style
├── Rcam2/          (20)   HDRP→URP body
├── Rcam3/           (8)   Depth people/env
├── Rcam4/          (14)   NDI-style body
├── SdfVfx/          (5)   SDF generation
├── Buddha/         (21)   Hand-tracked mesh
├── Fluo/            (8)   Brush/painting
├── Khoreo/          (7)   Stage performance
├── Smrvfx/          (2)   Skinned mesh
├── Keijiro/        (16)   Experimental
├── UnitySamples/   (20)   Learning templates
├── Portals6/       (22)   Portal effects
├── FaceTracking/    (2)   Face mesh
├── Compute/         (2)   Compute examples
├── Myaku/           (1)   AR character
├── Splat/           (3)   Gaussian splatting
├── Tamagotchu/      (4)   Virtual pet
├── WebRTC/          (7)   Trails + SDF
└── (legacy)         (4)   Uncategorized
```

## GitHub Repos Referenced

- keijiro/Rcam2, Rcam3, Rcam4
- keijiro/Akvfx, Smrvfx, SdfVfx
- keijiro/Khoreo, Fluo
- keijiro/VfxGraphTestbed
- keijiro/SplatVFX
- holoi/touching-hologram
- mao-test-h/FaceTrackingVFX
- cinight/MinimalCompute
- EyezLee/TamagotchU
- Unity VFX Graph samples

## Migration History

| Date | Source | VFX Count | Commit |
|------|--------|-----------|--------|
| 2026-01-14 | Rcam2-4, Akvfx, SdfVfx | 54 | Initial setup |
| 2026-01-16 | NNCam2 | 9 | Keypoint VFX |
| 2026-01-20 | Portals6 | 22 | Portal effects |
| 2026-01-20 | Buddha, Fluo, Khoreo, etc | 76 | Reference migration |
| 2026-01-20 | _ref projects | 17 | Final migration |
