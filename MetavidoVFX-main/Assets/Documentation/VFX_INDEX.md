# VFX Index

Complete index of all VFX assets in MetavidoVFX project.

**Total Production VFX**: 88
**Last Updated**: 2026-01-16

---

## Naming Convention

VFX files follow the pattern: `{effect}_{datasource}_{target}_{origin}.vfx`

| Component | Values | Description |
|-----------|--------|-------------|
| **effect** | particles, voxels, flame, etc. | Visual effect type |
| **datasource** | depth, stencil, any | Input data type |
| **target** | people, environment, any | What it affects |
| **origin** | metavido, rcam2, rcam3, rcam4, h3m, akvfx, nncam2, sdfvfx | Source project |

---

## Resources/VFX (14) - Runtime Loadable

| VFX | Data Source | Target |
|-----|-------------|--------|
| `particles_depth_people_metavido.vfx` | Depth | People |
| `voxels_depth_people_metavido.vfx` | Depth | People |
| `pointcloud_depth_people_metavido.vfx` | Depth | People |
| `pointcloud_depth_people_metavido 1.vfx` | Depth | People |
| `bodyparticles_depth_people_metavido.vfx` | Depth | People |
| `bubble_depth_people_metavido.vfx` | Depth | People |
| `bubbles_depth_people_metavido.vfx` | Depth | People |
| `trails_depth_people_metavido.vfx` | Depth | People |
| `flame_depth_people_metavido.vfx` | Depth | People |
| `glitch_depth_people_metavido.vfx` | Depth | People |
| `sparkles_depth_people_metavido.vfx` | Depth | People |
| `rcam3sparkles_depth_people_metavido.vfx` | Depth | People |
| `rcam3flame_depth_people_metavido.vfx` | Depth | People |
| `humancube_stencil_people_h3m.vfx` | Stencil | People |

---

## VFX/Metavido (4) - Core Metavido

| VFX | Data Source | Target |
|-----|-------------|--------|
| `particles_depth_people_metavido.vfx` | Depth | People |
| `voxels_depth_people_metavido.vfx` | Depth | People |
| `bodyparticles_depth_people_metavido.vfx` | Depth | People |
| `afterimage_depth_people_metavido.vfx` | Depth | People |

---

## VFX/Rcam2 (20) - HDRP→URP Converted

### BodyFX (13)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `brush_depth_people_rcam2.vfx` | Depth | People |
| `bubble_depth_people_rcam2.vfx` | Depth | People |
| `dot_depth_people_rcam2.vfx` | Depth | People |
| `fragment_depth_people_rcam2.vfx` | Depth | People |
| `glitch_depth_people_rcam2.vfx` | Depth | People |
| `petal_depth_people_rcam2.vfx` | Depth | People |
| `point_depth_people_rcam2.vfx` | Depth | People |
| `spark_depth_people_rcam2.vfx` | Depth | People |
| `spike_depth_people_rcam2.vfx` | Depth | People |
| `trail_depth_people_rcam2.vfx` | Depth | People |
| `voxel_depth_people_rcam2.vfx` | Depth | People |

### EnvFX (9)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `candy_environment_rcam2.vfx` | Any | Environment |
| `emoji_environment_rcam2.vfx` | Any | Environment |
| `eyeball_environment_rcam2.vfx` | Any | Environment |
| `floor_environment_rcam2.vfx` | Any | Environment |
| `particle_environment_rcam2.vfx` | Any | Environment |
| `petal_environment_rcam2.vfx` | Any | Environment |
| `ribbon_environment_rcam2.vfx` | Any | Environment |
| `text_environment_rcam2.vfx` | Any | Environment |
| `warp_environment_rcam2.vfx` | Any | Environment |

---

## VFX/Rcam3 (8)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `flame_depth_people_rcam3.vfx` | Depth | People |
| `grid_any_rcam3.vfx` | Any | Any |
| `particles_any_rcam3.vfx` | Any | Any |
| `plexus_depth_people_rcam3.vfx` | Depth | People |
| `points_depth_people_rcam3.vfx` | Depth | People |
| `scanlines_depth_people_rcam3.vfx` | Depth | People |
| `sparkles_depth_people_rcam3.vfx` | Depth | People |
| `sweeper_any_rcam3.vfx` | Any | Any |

---

## VFX/Rcam4 (14)

### Body (10)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `balls_depth_people_rcam4.vfx` | Depth | People |
| `bubbles_depth_people_rcam4.vfx` | Depth | People |
| `flame_depth_people_rcam4.vfx` | Depth | People |
| `lightning_depth_people_rcam4.vfx` | Depth | People |
| `pointcloud_depth_people_rcam4.vfx` | Depth | People |
| `rcam4_depth_people_rcam4.vfx` | Depth | People |
| `sparkles_depth_people_rcam4.vfx` | Depth | People |
| `spikes_depth_people_rcam4.vfx` | Depth | People |
| `trails_depth_people_rcam4.vfx` | Depth | People |
| `voxels_depth_people_rcam4.vfx` | Depth | People |

### Environment (4)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `grid_environment_rcam4.vfx` | Any | Environment |
| `petals_environment_rcam4.vfx` | Any | Environment |
| `shards_environment_rcam4.vfx` | Any | Environment |
| `speedlines_environment_rcam4.vfx` | Any | Environment |

---

## VFX/NNCam2 (9) - Keypoint-Driven

| VFX | Data Source | Target | Description |
|-----|-------------|--------|-------------|
| `electrify_any_nncam2.vfx` | Keypoints | Any | Electric arcs between joints |
| `eyes_any_nncam2.vfx` | Keypoints | Any | Eye tracking effects |
| `joints_any_nncam2.vfx` | Keypoints | Any | Joint visualization |
| `mosaic_any_nncam2.vfx` | Keypoints | Any | Mosaic patterns |
| `particle_any_nncam2.vfx` | Keypoints | Any | Particle trails |
| `petals_any_nncam2.vfx` | Keypoints | Any | Petal particles |
| `spikes_any_nncam2.vfx` | Keypoints | Any | Spike emanations |
| `symbols_any_nncam2.vfx` | Keypoints | Any | Symbol particles |
| `tentacles_any_nncam2.vfx` | Keypoints | Any | Tentacle trails |

**Required**: `KeypointBuffer` from BodyPartSegmenter (17 pose landmarks)

---

## VFX/Akvfx (7) - Azure Kinect Style

| VFX | Data Source | Target |
|-----|-------------|--------|
| `leaves_stencil_people_akvfx.vfx` | Stencil | People |
| `lines_stencil_people_akvfx.vfx` | Stencil | People |
| `particles_stencil_people_akvfx.vfx` | Stencil | People |
| `point_stencil_people_akvfx.vfx` | Stencil | People |
| `spikes_stencil_people_akvfx.vfx` | Stencil | People |
| `voxel_stencil_people_akvfx.vfx` | Stencil | People |
| `web_stencil_people_akvfx.vfx` | Stencil | People |

---

## VFX/SdfVfx (5) - SDF-Based

| VFX | Data Source | Target |
|-----|-------------|--------|
| `blocks_environment_sdfvfx.vfx` | SDF | Environment |
| `circuits_environment_sdfvfx.vfx` | SDF | Environment |
| `grape_environment_sdfvfx.vfx` | SDF | Environment |
| `stickies_environment_sdfvfx.vfx` | SDF | Environment |
| `trails_environment_sdfvfx.vfx` | SDF | Environment |

---

## VFX/Environment (5) - H3M Environment

| VFX | Data Source | Target |
|-----|-------------|--------|
| `markers_depth_environment_h3m.vfx` | Depth | Environment |
| `ribbons_depth_environment_h3m.vfx` | Depth | Environment |
| `swarm_environment_rcam3.vfx` | Any | Environment |
| `warp_environment_h3m.vfx` | Any | Environment |
| `worldgrid_environment_h3m.vfx` | Any | Environment |

---

## VFX/HumanEffects (1)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `humancube_stencil_people_h3m.vfx` | Stencil | People |

---

## VFX/CameraProxy (1)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `cameraproxy_depth_any_metavido.vfx` | Depth | Any |

---

## H3M/VFX (1) - Hologram System

| VFX | Data Source | Target |
|-----|-------------|--------|
| `hologram_depth_people_metavido.vfx` | Depth | People |

---

## Scripts/PeopleOcclusion (1)

| VFX | Data Source | Target |
|-----|-------------|--------|
| `peoplevfx_stencil_people_h3m.vfx` | Stencil | People |

---

## Procedural VFX Library (3) - Experimental

| VFX | Description |
|-----|-------------|
| `LissajousPlex 2.vfx` | Lissajous curve plexus |
| `Plexus 1.vfx` | Plexus ribbons sketch |
| `Ribbon 1.vfx` | Ribbon test |

---

## Samples/Visual Effect Graph (23) - Learning Templates

Unity's official learning templates (not production):

| Category | VFX |
|----------|-----|
| **Basics** | Context&Flow, SpawnContext, Capacity, MultipleOutputs |
| **Orientation** | OrientFaceCamera, OrientFixedAxis, OrientAdvanced |
| **Rotation** | RotationAngle, AngularVelocity |
| **Textures** | FlipbookBlending, FlipbookMode, BasicTexIndex, TexIndexAdvanced, SampleTexture2D |
| **Mesh** | SampleMesh, SampleSkinnedMesh |
| **Collision** | CollisionSimple, CollisionAdvanced, CollisionBasicProperties, TriggerEventCollide |
| **Strips** | StripProperties, StripSpawnRate, StripGPUEvent, MultiStripSingleBurst, MultiStripsPeriodicBurst, MultiStripsSpawnRate, MultiStripGPUEvents |
| **SDF** | SampleSDF |
| **Pivot** | PivotAttribute, PivotAdvanced |
| **Decals** | DecalParticles |
| **Bounds** | BoundsGizmo |

### VisualEffectGraph Additions (5)

| VFX | Description |
|-----|-------------|
| `Bonfire.vfx` | Complete fire effect |
| `Flames.vfx` | Flame particles |
| `Lightning.vfx` | Lightning bolts |
| `Smoke.vfx` | Smoke effect |
| `Sparks.vfx` | Spark particles |

---

## Compatibility Audit Distribution

**Total VFX in Resources/VFX**: 73

| Mode | Count | Focus |
|------|-------|-------|
| **People** | 41 | Human segmentation/occlusion |
| **Environment** | 23 | World/AR mesh interaction |
| **Hybrid** | 9 | Multi-source (e.g., NNCam + Depth) |

---

## Data Source Requirements

| Data Source | Required Component | Properties |
|-------------|-------------------|------------|
| **Depth** | VFXARBinder (primary) / VFXARDataBinder (legacy) | DepthMap, PositionMap, RayParams |
| **Stencil** | VFXARBinder (primary) / VFXARDataBinder (legacy) | StencilMap, PositionMap |
| **Keypoints** | NNCamKeypointBinder | KeypointBuffer (GraphicsBuffer) |
| **SDF** | Custom SDF binder | SDF texture |
| **Any** | None required | Works standalone |

---

## Quick Reference by Effect Type

### People Effects (Depth-based)
- particles, voxels, pointcloud, bodyparticles
- flame, sparkles, trails, lightning
- bubble/bubbles, balls, spikes
- glitch, afterimage, brush, fragment
- dot, petal, point, spark, spike, trail

### People Effects (Stencil-based)
- humancube, peoplevfx
- leaves, lines, particles, point, spikes, voxel, web

### Environment Effects
- warp, worldgrid, markers, ribbons, swarm
- grid, petals, shards, speedlines
- candy, emoji, eyeball, floor, text
- blocks, circuits, grape, stickies, trails

### Keypoint Effects (NNCam2)
- joints, eyes, electrify
- mosaic, particle, petals
- spikes, symbols, tentacles

---

## Statistics by Origin

| Origin | Count | Pipeline |
|--------|-------|----------|
| **Metavido** | 4 | Live AR Foundation |
| **Rcam2** | 20 | HDRP→URP converted |
| **Rcam3** | 8 | NDI stream (adapted) |
| **Rcam4** | 14 | NDI stream (adapted) |
| **Akvfx** | 7 | Azure Kinect style |
| **NNCam2** | 9 | Keypoint-driven |
| **SdfVfx** | 5 | SDF-based |
| **H3M** | 8 | Custom H3M system |
| **Resources** | 14 | Runtime loadable |
| **Samples** | 28 | Learning templates |

**Total**: 88 production + 28 samples = 116 VFX assets
