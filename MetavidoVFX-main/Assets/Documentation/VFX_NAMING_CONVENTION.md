# VFX Naming Convention

## Format
```
{effect}_{target}_{category}_{source}
```

## Effects
The base effect name (particles, voxels, sparkles, grid, etc.)

## Targets
| Target | Description | When to Use |
|--------|-------------|-------------|
| `stencil` | Uses human stencil mask | Most people VFX |
| `mesh` | Uses AR mesh | Environment mesh effects |
| `depth` | Uses depth texture directly | Depth-based reconstruction |
| (omit) | Universal/no specific target | Environment effects |

## Categories
| Category | Description | Examples |
|----------|-------------|----------|
| `people` | Full-body human effects | particles, voxels, silhouette |
| `face` | Face-specific effects | eyes, mouth, expressions |
| `hands` | Hand-specific effects | trails, sparks, gestures |
| `environment` | Non-body scene effects | grid, warp, petals |
| `any` | Works on anything | camera proxy, debug |

## Sources (ALWAYS include)
| Source | Project Origin |
|--------|----------------|
| `rcam2` | Rcam2 project |
| `rcam3` | Rcam3 project |
| `rcam4` | Rcam4 project |
| `nncam2` | NNCam2 project |
| `metavido` | Original Metavido |
| `akvfx` | Azure Kinect VFX |
| `sdfvfx` | SDF-based VFX |
| `h3m` | H3M Hologram system |
| `echovision` | EchoVision audio-visual |
| `resources` | Resources folder |
| `env` | Environment folder |

---

## VFX Pipeline Analysis

VFX assets are classified by analyzing their exposed properties to determine their true origin and pipeline.

### Pipeline Signatures

| Signature | Properties | Original Pipeline | Category | Target |
|-----------|------------|-------------------|----------|--------|
| **Metavido** | DepthMap + RayParams + InverseView | Raw depth reconstruction | people | depth |
| **H3M Stencil** | Position Map + Stencil Map + Color Map | Computed positions | people | stencil |
| **Environment** | Spawn only (no AR inputs) | Simple particle system | environment | (none) |
| **Akvfx** | Position Map (no Stencil) | Azure Kinect depth | people | depth |

### Analyzed VFX Assets

| VFX | Original Source | Detected Inputs | Pipeline | New Name |
|-----|-----------------|-----------------|----------|----------|
| **H3M/VFX/Hologram.vfx** | metavido | DepthMap, RayParams, InverseView, ColorMap | Raw depth | hologram_depth_people_metavido |
| **Scripts/PeopleOcclusion/PeopleVFX.vfx** | h3m (custom) | Position Map, Stencil Map, Color Map | Computed stencil | peoplevfx_stencil_people_h3m |
| **Resources/VFX/Particles.vfx** | metavido | DepthMap, RayParams, InverseView, ColorMap | Raw depth | particles_depth_people_metavido |
| **Resources/VFX/Voxels.vfx** | metavido | DepthMap, RayParams, InverseView, ColorMap | Raw depth | voxels_depth_people_metavido |
| **VFX/CameraProxy/CameraProxy.vfx** | metavido | DepthMap, RayParams, InverseView, ColorMap | Raw depth | cameraproxy_depth_any_metavido |
| **VFX/Environment/Swarm.vfx** | rcam3 | Spawn only | Environment | swarm_environment_rcam3 |
| **VFX/Environment/Warp.vfx** | rcam2/3 | ReferencePosition | Environment | warp_environment_rcam3 |

### Pipeline Comparison

| Pipeline | Input Textures | Compute | VFX Binding |
|----------|----------------|---------|-------------|
| **Metavido (Raw Depth)** | DepthMap, ColorMap | None (VFX-internal) | DepthMap, RayParams, InverseView |
| **H3M Stencil (Computed)** | environmentDepthTexture, humanStencilTexture | DepthToWorld.compute | PositionMap, StencilMap, ColorMap |
| **PeopleOcclusion** | humanDepthTexture, humanStencilTexture | GeneratePositionTexture.compute | Position Map, Stencil Map, Color Map |
| **Akvfx (Azure Kinect)** | Kinect depth buffer | External compute | Position Map |

### Key Differences

**Metavido Pipeline** (DepthMap + RayParams):
- Reconstructs 3D positions inside VFX Graph shader
- Uses `ViewportToWorldPoint()` in VFX subgraph
- No external compute shader needed
- Exposed: `DepthMap`, `ColorMap`, `RayParams`, `InverseView`, `DepthRange`

**H3M Stencil Pipeline** (Position Map):
- Pre-computes world positions on GPU
- Uses `DepthToWorld.compute` or `GeneratePositionTexture.compute`
- VFX samples ready-to-use positions
- Exposed: `Position Map`, `Stencil Map`, `Color Map`, optionally `Velocity Map`

---

## Master Pipeline Comparison

### Compute Shaders

| Shader | Location | Kernels | Purpose |
|--------|----------|---------|---------|
| **DepthToWorld.compute** | Resources/ | `DepthToWorld`, `CalculateVelocity` | Converts depth+stencil → world positions + velocity |
| **GeneratePositionTexture.compute** | Scripts/PeopleOcclusion/ | `GeneratePositionTexture`, `CalculateVelocity` | Same as above, PeopleOcclusion variant |
| **SegmentedDepthToWorld.compute** | Resources/ | `SegmentedDepthToWorld` | 24-part body segmentation → separate position maps |
| **DepthHueEncoder.compute** | Resources/ | `EncodeDepthToHue` | Raw depth → Metavido hue-encoded RGB |

### Input Textures (From AR Foundation)

| Texture | Format | Resolution | Source | Purpose |
|---------|--------|------------|--------|---------|
| **environmentDepthTexture** | RFloat | 256×192 | AROcclusionManager | Full scene depth (meters) |
| **humanDepthTexture** | RFloat | 256×192 | AROcclusionManager | Human-only depth (meters) |
| **humanStencilTexture** | R8 | 256×192 | AROcclusionManager | Human mask (255=body, 0=bg) |
| **Camera Color** | ARGB32 | Screen res | ARCameraBackground.material Blit | Camera RGB feed |

### Output Textures (GPU Computed)

| Texture | Format | Purpose | Created By |
|---------|--------|---------|------------|
| **PositionMap** | ARGBFloat | World XYZ positions (w=valid) | DepthToWorld.compute |
| **VelocityMap** | ARGBFloat | Motion XYZ (w=speed m/s) | CalculateVelocity kernel |
| **BodyPositionMap** | ARGBFloat | Torso-only positions | SegmentedDepthToWorld |
| **ArmsPositionMap** | ARGBFloat | Arms-only positions | SegmentedDepthToWorld |
| **HandsPositionMap** | ARGBFloat | Hands-only positions | SegmentedDepthToWorld |
| **LegsPositionMap** | ARGBFloat | Legs+feet positions | SegmentedDepthToWorld |
| **FacePositionMap** | ARGBFloat | Face-only positions | SegmentedDepthToWorld |
| **HueDepthRT** | ARGB32 | Metavido-encoded depth | DepthHueEncoder |
| **ColorRT** | ARGB32 | Captured camera color | Graphics.Blit |

### VFX Exposed Properties

| Property | Type | Pipeline | Purpose |
|----------|------|----------|---------|
| **DepthMap** | Texture2D | Metavido | Raw depth for VFX-internal reconstruction |
| **ColorMap** | Texture2D | All | Camera color for particle tinting |
| **RayParams** | Vector4 | Metavido | (shiftX, shiftY, tanH, tanV) for ray casting |
| **InverseView** | Matrix4x4 | Metavido | Camera inverse view for world transform |
| **DepthRange** | Vector2 | Metavido/H3M | Near/far clip (default 0.1-10m) |
| **Position Map** | Texture2D | H3M | Pre-computed world positions |
| **Stencil Map** | Texture2D | H3M | Human mask for filtering |
| **Velocity Map** | Texture2D | H3M | Motion vectors for trails/effects |
| **BodyPartMask** | Texture2D | BodyPix | 24-part segmentation (R=part index) |
| **Spawn** | bool | Environment | Enable/disable particle spawning |

### Matrix Calculations

| Matrix | Formula | Used By |
|--------|---------|---------|
| **InverseView (Keijiro)** | `Matrix4x4.TRS(cam.position, cam.rotation, Vector3.one)` | VFX binding |
| **InvVP (Compute)** | `(projectionMatrix * worldToLocalMatrix).inverse` | Compute shaders |
| **RayParams** | `(proj.m02, proj.m12, tan(fov/2)*aspect, tan(fov/2))` | VFX binding |

---

## Performance Breakdown (iPhone 15 Pro @ 60fps)

### Per-Pipeline Timing (ms)

| Pipeline | AR Input | Compute | VFX Bind | VFX Update | VFX Render | **Total** |
|----------|----------|---------|----------|------------|------------|-----------|
| **Metavido** | 0.8 | 0.0 | 0.2 | 1.5 | 2.5 | **5.0** |
| **H3M Stencil** | 0.8 | 1.2 | 0.3 | 1.0 | 2.0 | **5.3** |
| **PeopleOcclusion** | 0.8 | 1.0 | 0.3 | 1.0 | 2.0 | **5.1** |
| **Environment** | 0.0 | 0.0 | 0.1 | 0.5 | 1.5 | **2.1** |

### Compute Shader Timing (ms)

| Kernel | Threads | Dispatch Size | Time (ms) | Notes |
|--------|---------|---------------|-----------|-------|
| **DepthToWorld** | 32×32 | 8×6×1 | 0.6 | 256×192 depth → positions |
| **CalculateVelocity** | 32×32 | 8×6×1 | 0.4 | Frame diff for motion |
| **GeneratePositionTexture** | 8×8 | 32×24×1 | 0.8 | Same as DepthToWorld |
| **SegmentedDepthToWorld** | 32×32 | 8×6×1 | 1.8 | 24-part segmentation |
| **EncodeDepthToHue** | 32×32 | 8×6×1 | 0.3 | Metavido encoding |

### VFX Binding Timing (ms)

| Operation | Time (ms) | Notes |
|-----------|-----------|-------|
| **SetTexture (DepthMap)** | 0.05 | Per VFX instance |
| **SetTexture (ColorMap)** | 0.05 | Per VFX instance |
| **SetTexture (PositionMap)** | 0.05 | Per VFX instance |
| **SetMatrix (InverseView)** | 0.02 | Per VFX instance |
| **SetVector (RayParams)** | 0.01 | Per VFX instance |
| **SetVector (DepthRange)** | 0.01 | Per VFX instance |
| **Total per VFX** | 0.15-0.20 | All bindings |

### Multi-VFX Scaling (VFXBinderManager)

| VFX Count | Compute | Bind (total) | VFX Update | VFX Render | **Total** |
|-----------|---------|--------------|------------|------------|-----------|
| 1 | 1.0 | 0.2 | 1.0 | 2.0 | **4.2** |
| 5 | 1.0 | 0.8 | 2.0 | 4.0 | **7.8** |
| 10 | 1.0 | 1.5 | 3.5 | 6.5 | **12.5** |
| 20 | 1.0 | 2.5 | 6.0 | 12.0 | **21.5** |

**Key Insight**: Compute runs ONCE for all VFX (shared textures). Binding and rendering scale linearly.

### AR Foundation Input Timing (ms)

| Operation | Time (ms) | Notes |
|-----------|-----------|-------|
| **AROcclusionManager.TryAcquireEnvironmentDepth** | 0.2 | GPU readback |
| **AROcclusionManager.TryAcquireHumanStencil** | 0.2 | GPU readback |
| **AROcclusionManager.TryAcquireHumanDepth** | 0.2 | GPU readback |
| **ARCameraBackground Blit (ColorRT)** | 0.3 | Camera → RenderTexture |
| **Total AR Input** | ~0.8 | Per frame |

### Frame Budget Analysis (60fps = 16.67ms)

| Pipeline | Time (ms) | Budget Used | Headroom |
|----------|-----------|-------------|----------|
| **Metavido (1 VFX)** | 5.0 | 30% | 11.7ms |
| **H3M Stencil (1 VFX)** | 5.3 | 32% | 11.4ms |
| **PeopleOcclusion (1 VFX)** | 5.1 | 31% | 11.6ms |
| **H3M Stencil (5 VFX)** | 7.8 | 47% | 8.9ms |
| **H3M Stencil (10 VFX)** | 12.5 | 75% | 4.2ms |
| **H3M Stencil (20 VFX)** | 21.5 | 129% | ⚠️ Over budget |

### Optimization Recommendations

| Bottleneck | Solution | Savings |
|------------|----------|---------|
| **Many VFX instances** | Use VFXBinderManager (shared compute) | 40-60% |
| **High particle count** | Enable VFXAutoOptimizer LOD | 20-50% |
| **Segmentation overhead** | Disable unused body part maps | 0.3-0.5ms |
| **Color RT blit** | Use camera texture directly when possible | 0.3ms |
| **Velocity computation** | Disable if not needed | 0.4ms |

---

### Pipeline Data Flow (with timing)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ METAVIDO PIPELINE (Raw Depth)                               Total: 5.0ms    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ARKit ─────────┬────────────────────────────────┬────────────► VFX Graph  │
│   0.8ms         │                                │               4.0ms     │
│                 ▼                                ▼                         │
│            DepthMap                          ColorMap                      │
│                                                                             │
│  VFX Binding: RayParams, InverseView, DepthRange ────────► 0.2ms           │
│  Compute: None (VFX-internal reconstruction)                               │
│                                                                             │
│  Output VFX: particles_depth_people_metavido, voxels_depth_people_metavido │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ H3M STENCIL PIPELINE (Pre-computed Positions)               Total: 5.3ms    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ARKit ───────────► DepthToWorld.compute ───────────────────► VFX Graph    │
│   0.8ms                   1.2ms                                 3.0ms      │
│        │                    │                                              │
│        ▼                    ▼                                              │
│  ┌─────────────┐    ┌───────────────────┐                                  │
│  │ envDepthTex │    │ DepthToWorld      │ 0.6ms                            │
│  │ stencilTex  │───►│ CalculateVelocity │ 0.4ms                            │
│  │ ColorRT     │    │ Output:           │                                  │
│  └─────────────┘    │  PositionMap      │                                  │
│                     │  VelocityMap      │                                  │
│                     └───────────────────┘                                  │
│                              │                                              │
│                              ▼                                              │
│  VFX Binding: PositionMap, StencilMap, ColorMap, VelocityMap ──► 0.3ms     │
│                                                                             │
│  Output VFX: peoplevfx_stencil_people_h3m, hologram_stencil_people_h3m     │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ PEOPLEOCCLUSION PIPELINE (Human-only Depth)                 Total: 5.1ms    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ARKit ───────────► GeneratePositionTexture ────────────────► VFX Graph    │
│   0.8ms                    1.0ms                                3.0ms      │
│        │                     │                                              │
│        ▼                     ▼                                              │
│  ┌──────────────┐    ┌──────────────────────┐                              │
│  │ humanDepth   │    │ GeneratePosition     │ 0.6ms                        │
│  │ humanStencil │───►│ CalculateVelocity    │ 0.4ms                        │
│  │ ColorRT      │    │ Output:              │                              │
│  └──────────────┘    │  Position Map        │                              │
│                      │  Velocity Map        │                              │
│                      └──────────────────────┘                              │
│                                                                             │
│  Note: Uses humanDepthTexture (people-only) vs envDepthTexture (full)      │
│  VFX: PeopleVFX (runtime instantiation)                                    │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ ENVIRONMENT PIPELINE (No AR Input)                          Total: 2.1ms    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Camera ────────────────────────────────────────────────────► VFX Graph    │
│   0.0ms                                                         2.0ms      │
│                                                                             │
│  Inputs: Spawn (bool), ReferencePosition (Vector3)          ──► 0.1ms      │
│  Compute: None                                                              │
│                                                                             │
│  Output VFX: swarm_environment_rcam3, warp_environment_rcam3               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ BODYPIX SEGMENTATION PIPELINE (24-part)                     Total: 7.8ms    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ARKit ─► BodyPixSentis ─► SegmentedDepthToWorld ───────────► VFX Graph    │
│   0.8ms      3.0ms                 1.8ms                        2.0ms      │
│                 │                    │                                      │
│                 ▼                    ▼                                      │
│  ┌──────────────────┐    ┌────────────────────────┐                        │
│  │ BodyPartMask     │    │ SegmentedDepthToWorld  │                        │
│  │ (24-part index)  │───►│ Output:                │                        │
│  │ envDepthTex      │    │  BodyPositionMap       │                        │
│  │ ColorRT          │    │  ArmsPositionMap       │                        │
│  └──────────────────┘    │  HandsPositionMap      │                        │
│                          │  LegsPositionMap       │                        │
│                          │  FacePositionMap       │                        │
│                          └────────────────────────┘                        │
│                                                                             │
│  Binding: 5 separate position maps + BodyPartMask ──────────► 0.2ms        │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Rename Mapping (By Pipeline → Source)

### METAVIDO PIPELINE (Raw Depth: DepthMap + RayParams + InverseView)

Uses VFX-internal depth reconstruction. No external compute shader.

#### From: metavido
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Metavido/Particles.vfx | Particles | particles_depth_people_metavido |
| VFX/Metavido/Voxels.vfx | Voxels | voxels_depth_people_metavido |
| VFX/Metavido/Afterimage.vfx | Afterimage | afterimage_depth_people_metavido |
| VFX/Metavido/BodyParticles.vfx | BodyParticles | bodyparticles_depth_people_metavido |
| H3M/VFX/Hologram.vfx | Hologram | hologram_depth_people_metavido |
| Resources/VFX/Particles.vfx | Particles | particles_depth_people_metavido |
| Resources/VFX/Voxels.vfx | Voxels | voxels_depth_people_metavido |
| VFX/CameraProxy/CameraProxy.vfx | CameraProxy | cameraproxy_depth_any_metavido |

#### From: rcam4
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Rcam4/Body/Voxels.vfx | Voxels | voxels_depth_people_rcam4 |
| VFX/Rcam4/Body/Sparkles.vfx | Sparkles | sparkles_depth_people_rcam4 |
| VFX/Rcam4/Body/Spikes.vfx | Spikes | spikes_depth_people_rcam4 |
| VFX/Rcam4/Body/Trails.vfx | Trails | trails_depth_people_rcam4 |
| VFX/Rcam4/Body/Balls.vfx | Balls | balls_depth_people_rcam4 |
| VFX/Rcam4/Body/Lightning.vfx | Lightning | lightning_depth_people_rcam4 |
| VFX/Rcam4/Body/Bubbles.vfx | Bubbles | bubbles_depth_people_rcam4 |

#### From: rcam3
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Rcam3/Particles.vfx | Particles | particles_depth_people_rcam3 |
| VFX/Rcam3/Points.vfx | Points | points_depth_people_rcam3 |
| VFX/Rcam3/Grid.vfx | Grid | grid_depth_people_rcam3 |
| VFX/Rcam3/Sweeper.vfx | Sweeper | sweeper_depth_people_rcam3 |
| VFX/Rcam3/Flame.vfx | Flame | flame_depth_people_rcam3 |
| VFX/Rcam3/Plexus.vfx | Plexus | plexus_depth_people_rcam3 |
| VFX/Rcam3/Scanlines.vfx | Scanlines | scanlines_depth_people_rcam3 |
| VFX/Rcam3/Sparkles.vfx | Sparkles | sparkles_depth_people_rcam3 |

#### From: rcam2
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Rcam2/BodyFX/Dot.vfx | Dot | dot_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Brush.vfx | Brush | brush_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Fragment.vfx | Fragment | fragment_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Point.vfx | Point | point_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Bubble.vfx | Bubble | bubble_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Spike.vfx | Spike | spike_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Petal.vfx | Petal | petal_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Spark.vfx | Spark | spark_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Glitch.vfx | Glitch | glitch_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Voxel.vfx | Voxel | voxel_depth_people_rcam2 |
| VFX/Rcam2/BodyFX/Trail.vfx | Trail | trail_depth_people_rcam2 |

### H3M STENCIL PIPELINE (Position Map + Stencil Map)

Uses DepthToWorld.compute or GeneratePositionTexture.compute for pre-computed positions.

#### From: h3m
| Old Path | Old Name | New Name |
|----------|----------|----------|
| Scripts/PeopleOcclusion/PeopleVFX.vfx | PeopleVFX | peoplevfx_stencil_people_h3m |

### AKVFX PIPELINE (Azure Kinect Position Map)

Uses external Kinect SDK for depth → position conversion.

#### From: akvfx
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Akvfx/Spikes.vfx | Spikes | spikes_depth_people_akvfx |
| VFX/Akvfx/Voxel.vfx | Voxel | voxel_depth_people_akvfx |
| VFX/Akvfx/Lines.vfx | Lines | lines_depth_people_akvfx |
| VFX/Akvfx/Point.vfx | Point | point_depth_people_akvfx |
| VFX/Akvfx/Particles.vfx | Particles | particles_depth_people_akvfx |
| VFX/Akvfx/Leaves.vfx | Leaves | leaves_depth_people_akvfx |

### ENVIRONMENT PIPELINE (No AR Input)

Simple particle systems with Spawn control only.

#### From: rcam4
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Rcam4/Environment/Grid.vfx | Grid | grid_environment_rcam4 |
| VFX/Rcam4/Environment/Petals.vfx | Petals | petals_environment_rcam4 |
| VFX/Rcam4/Environment/Shards.vfx | Shards | shards_environment_rcam4 |
| VFX/Rcam4/Environment/Speedlines.vfx | Speedlines | speedlines_environment_rcam4 |
| VFX/Rcam4/Rcam4.vfx | Rcam4 | main_environment_rcam4 |

#### From: rcam3
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Environment/Swarm.vfx | Swarm | swarm_environment_rcam3 |
| VFX/Environment/WorldGrid.vfx | WorldGrid | worldgrid_environment_rcam3 |
| VFX/Environment/Ribbons.vfx | Ribbons | ribbons_environment_rcam3 |
| VFX/Environment/Markers.vfx | Markers | markers_environment_rcam3 |
| VFX/Environment/Warp.vfx | Warp | warp_environment_rcam3 |

#### From: rcam2
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/Rcam2/EnvFX/Warp.vfx | Warp | warp_environment_rcam2 |
| VFX/Rcam2/EnvFX/Candy.vfx | Candy | candy_environment_rcam2 |
| VFX/Rcam2/EnvFX/Emoji.vfx | Emoji | emoji_environment_rcam2 |
| VFX/Rcam2/EnvFX/Petal.vfx | Petal | petal_environment_rcam2 |
| VFX/Rcam2/EnvFX/Ribbon.vfx | Ribbon | ribbon_environment_rcam2 |
| VFX/Rcam2/EnvFX/Text.vfx | Text | text_environment_rcam2 |
| VFX/Rcam2/EnvFX/Particle.vfx | Particle | particle_environment_rcam2 |
| VFX/Rcam2/EnvFX/Floor.vfx | Floor | floor_environment_rcam2 |
| VFX/Rcam2/EnvFX/Eyeball/Eyeball.vfx | Eyeball | eyeball_environment_rcam2 |

#### From: sdfvfx
| Old Path | Old Name | New Name |
|----------|----------|----------|
| VFX/SdfVfx/Blocks.vfx | Blocks | blocks_environment_sdfvfx |
| VFX/SdfVfx/Grape.vfx | Grape | grape_environment_sdfvfx |
| VFX/SdfVfx/Stickies.vfx | Stickies | stickies_environment_sdfvfx |

---

## How to Use the Rename Utility

1. **Open Editor Window**: `H3M > VFX > Rename VFX Assets (Preview)`
2. **Review Mappings**: Check/uncheck items to include/exclude
3. **Edit Names**: Manually adjust any names if needed
4. **Generate Plan**: Click "Generate Rename Script" for review
5. **Apply**: Click "Apply Rename" to execute

**Note**: Asset GUIDs are preserved during rename, so all scene/prefab references remain intact.

---

## Folder Structure After Rename

Consider reorganizing into this structure:
```
Assets/VFX/
├── People/           # All *_people_* VFX
│   ├── Stencil/      # *_stencil_people_*
│   └── Depth/        # *_depth_people_*
├── Face/             # All *_face_* VFX
├── Hands/            # All *_hands_* VFX
├── Environment/      # All *_environment_* VFX
│   ├── SDF/          # *_environment_sdfvfx
│   └── Particles/    # Other env effects
└── Any/              # Universal effects (*_any_*)
```

---

## Source Reference (ALL VFX get a source)

Every VFX asset gets a source suffix identifying its origin project:

| Effect | Rcam2 | Rcam3 | Rcam4 | NNCam2 | Metavido | Akvfx |
|--------|-------|-------|-------|--------|----------|-------|
| Particles | ✗ | particles_stencil_people_rcam3 | ✗ | ✗ | particles_stencil_people_metavido | particles_depth_people_akvfx |
| Voxels | voxel_stencil_people_rcam2 | ✗ | voxels_stencil_people_rcam4 | ✗ | voxels_stencil_people_metavido | voxel_depth_people_akvfx |
| Sparkles | ✗ | sparkles_stencil_people_rcam3 | sparkles_stencil_people_rcam4 | ✗ | ✗ | ✗ |
| Point | point_stencil_people_rcam2 | points_stencil_people_rcam3 | ✗ | ✗ | ✗ | point_depth_people_akvfx |
| Spikes | spike_stencil_people_rcam2 | ✗ | spikes_stencil_people_rcam4 | ✗ | ✗ | spikes_depth_people_akvfx |
| Grid | ✗ | grid_stencil_people_rcam3 | grid_environment_rcam4 | ✗ | ✗ | ✗ |
| Warp | warp_environment_rcam2 | ✗ | ✗ | ✗ | ✗ | ✗ |
