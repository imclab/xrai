# XR Scene Format Comparison: .tilt vs USDZ vs glTF vs XRRAI

**Created**: 2026-01-22
**Purpose**: Comprehensive analysis of 3D scene formats for XR, hologram systems, and AI integration
**Last Updated**: 2026-01-22

---

## Executive Summary

| Format | Best For | Cross-Platform | AI/ML Ready | Streaming | Recommendation |
|--------|----------|----------------|-------------|-----------|----------------|
| **glTF/GLB** | Web, mobile, universal | ✅ Excellent | ✅ KHR_gaussian_splatting | ⚠️ Limited | **PRIMARY** |
| **USDZ** | iOS/visionOS only | ❌ Apple-only | ⚠️ Via Omniverse | ❌ No | iOS export only |
| **.tilt** | Brush strokes/art | ⚠️ VR-focused | ❌ None | ❌ No | Import compatibility |
| **XRRAI** | MetavidoVFX native | ✅ By design | ✅ Native | ✅ Chunked | **INTERNAL** |

**Recommended Strategy**:
- Internal: XRRAI JSON format (full fidelity)
- Export: glTF/GLB (universal compatibility)
- iOS: Auto-convert to USDZ for Quick Look
- Import: Support .tilt for OpenBrush compatibility

---

## 1. Format Deep Dive

### 1.1 .tilt (Tilt Brush / Open Brush)

**Structure**: 16-byte header + PKZIP archive

```
tilT (4 bytes magic) + header (12 bytes)
├── metadata.json     - Scene metadata (brushes, layers, environment)
├── data.sketch       - Binary stroke data
├── thumbnail.png     - 256x256 preview
└── hires.png         - Optional high-res thumbnail
```

**Binary data.sketch format**:
```
Header:
  int32   sentinel              // 0xc576a5cd
  int32   version               // 5 or 6
  int32   reserved              // 0
  uint32  additionalHeaderSize
  int32   num_strokes

Per Stroke:
  int32   brush_index
  float32x4 brush_color         // RGBA
  float32 brush_size
  uint32  stroke_extension_mask  // Flags, scale, group, seed, layer
  uint32  controlpoint_extension_mask
  int32   num_control_points
  [Control Points]:
    float32x3 position          // 12 bytes
    float32x4 orientation       // 16 bytes (quaternion)
    [Extensions]: pressure, timestamp
```

**Capabilities**:
| Feature | Support | Notes |
|---------|---------|-------|
| Stroke data | ✅ Full | Position, rotation, pressure, timestamp |
| Brush metadata | ✅ GUID-based | 90+ brushes |
| Layers | ✅ v22.0+ | Independent canvases |
| Models | ✅ v7.0+ | OBJ/glTF references |
| Images/Videos | ✅ v7.5+ | Embedded references |
| Environment | ✅ | Skybox, lighting, fog |
| Camera paths | ✅ v22.0+ | Animation splines |
| Audio reactive | ❌ | Runtime-only |
| AR anchors | ❌ | Not supported |
| Physics | ❌ | Not supported |
| Streaming | ❌ | Full file required |

**Platform Support**:
- Unity: TiltBrushToolkit, custom import
- Web: three-tiltloader, three-icosa
- Quest/VR: Native Open Brush
- iOS/Android: Viewer only (no creation)

**File Sizes** (typical):
- 100 strokes, 50 points each: ~200 KB
- 500 strokes: ~1 MB
- 1000 strokes, 100 points: ~4 MB
- Complex artwork: 1-20 MB

---

### 1.2 USDZ (Apple/Pixar Universal Scene Description)

**Structure**: Zero-compression ZIP archive (64-byte alignment)

```
USDZ Package (read-only archive)
├── scene.usdc         - Binary USD (default)
├── textures/          - PNG, JPEG, EXR, AVIF
└── audio/             - M4A, MP3, WAV
```

**Key Characteristics**:
- **No compression** by design (mmap/zero-copy optimization)
- **Read-only** (must unpack to edit)
- **64-byte alignment** for AVX512 optimization

**Schema Types**:
| Schema | Purpose |
|--------|---------|
| UsdGeomMesh | Polygon geometry |
| UsdGeomXform | Transformation hierarchy |
| UsdShade | Materials (MaterialX) |
| UsdSkel | Skeletal animation |
| BlendShape | Morph targets |
| UsdPhysics* | Rigid bodies, collisions, joints |
| UsdLux | Lighting |

**AR Quick Look Constraints**:
| Constraint | Recommendation | Hard Limit |
|------------|----------------|------------|
| File size | 4-8 MB | Soft |
| Memory footprint | <200 MB | ~200 MB |
| Single 4K texture | ~130 MB (memory) | Avoid |
| Polygons (single mat) | 100k | Implementation-specific |
| Triangles (complex) | 200k | Use LODs |

**Platform Support**:
| Platform | Support | Notes |
|----------|---------|-------|
| iOS/iPadOS | ✅ Native | AR Quick Look |
| visionOS | ✅ Native | Reality Composer Pro |
| Android | ❌ None | Use glTF |
| Web | ⚠️ Export only | Babylon.js 8.0 |
| Unity | ⚠️ Experimental | com.unity.formats.usd 3.0 |
| Three.js | ❌ | "Far less practical than glTF" |

**AI/ML Integration**:
- Neural rendering via NVIDIA Omniverse only
- MaterialX for procedural materials
- No native 3DGS support on Apple platforms yet

**Future (AOUSD)**:
- Core Spec 1.0: Dec 2025
- Core Spec 1.1: 2026 (animation, scaling, compliance)
- Members: Pixar, Apple, NVIDIA, Adobe, Autodesk

---

### 1.3 glTF 2.0 (Khronos Group)

**Structure**: JSON + binary + textures or single GLB

```
Option A: Separate files
├── scene.gltf         - JSON scene description
├── geometry.bin       - Binary buffers
└── textures/          - PNG, JPEG, KTX2

Option B: Single binary (recommended)
└── scene.glb          - All-in-one binary
```

**Compression Options**:
| Method | Ratio | Speed | Best For |
|--------|-------|-------|----------|
| Draco | 80-95% | Slower (WASM) | Static geometry |
| meshopt | 60-80% | Fast | Animation, morph targets |
| KTX2/Basis | 75-90% | GPU-native | Textures |
| Quantization | 2.4x | Instant | All geometry |

**Extensions (19 ratified + 15+ in progress)**:

**Materials (11 ratified)**:
- KHR_materials_unlit, clearcoat, sheen, transmission
- KHR_materials_volume, ior, specular, iridescence
- KHR_materials_anisotropy, emissive_strength, dispersion

**Geometry & Animation**:
- KHR_draco_mesh_compression
- KHR_mesh_quantization
- KHR_animation_pointer (animate any property)
- EXT_mesh_gpu_instancing

**In Development (2025-2026)**:
| Extension | Status | Purpose |
|-----------|--------|---------|
| KHR_interactivity | Review | Behavior graphs |
| KHR_physics_rigid_bodies | Review | Physics simulation |
| KHR_audio | Initial | Spatial audio |
| **KHR_gaussian_splatting** | Proposal | 3D Gaussian Splats |
| KHR_gaussian_splatting_compression_spz | Proposal | SPZ compression (90% smaller) |

**Platform Support**:
| Platform | Support | Library |
|----------|---------|---------|
| Unity | ✅ Excellent | glTFast 6.15, UnityGLTF |
| Three.js | ✅ Excellent | GLTFLoader |
| Babylon.js | ✅ Excellent | Native + WebGPU |
| iOS | ✅ Via Unity | Or model-viewer |
| Android | ✅ Native | Scene Viewer |
| Quest | ✅ Full | Unity/Unreal |
| WebXR | ✅ Primary | Standard format |
| WebGPU | ✅ | Babylon.js 8.0, Three.js |

**Performance**:
- GLB load: ~9ms (100 iterations avg)
- OBJ load: ~133ms (14x slower)
- Best compression: Draco + KTX2 = 90%+ reduction

---

### 1.4 AI/ML 3D Formats

**3D Gaussian Splatting Formats**:

| Format | Creator | Size vs PLY | glTF Integration |
|--------|---------|-------------|------------------|
| .ply | Original | 100% baseline | Via extension |
| .splat | Antimatter15 | ~40-50% | Not standardized |
| .ksplat | mkkellogg | ~30% | Not standardized |
| **.spz** | Niantic | **~10%** | **KHR_gaussian_splatting_compression_spz** |
| .sog | PlayCanvas | ~25% | Not standardized |

**SPZ Format (Recommended)**:
- "JPG for splats" - 90% compression
- MIT License - commercially viable
- Adopted by Khronos for glTF extension
- Partners: OGC, Khronos, Niantic, Cesium, Esri

**Neural Radiance Fields (NeRF)**:
- Checkpoint formats: .pt, .ckpt, .safetensors
- Export targets: Mesh (OBJ, GLB), Point cloud (PLY)
- SNeRG: 12ms per frame (3 orders magnitude faster)
- Trend: NeRF → 3DGS migration for real-time

**Volumetric Video**:
| Company | Output Format | Unity Support |
|---------|---------------|---------------|
| Microsoft MRCS | HoloStream | Via Arcturus |
| Depthkit | DK format | Dkvfx |
| 8i | OMS, WebAR | Yes |
| Metavido | H.264+metadata | Native (existing) |

**AI-Generated 3D**:
| Platform | Output | Speed |
|----------|--------|-------|
| Meshy AI | GLB, OBJ, USDZ | Fast |
| Tripo AI | GLB, FBX, OBJ | Fast |
| Stable Fast 3D | GLB | 0.5 sec |
| SPAR3D | Mesh | <1 sec |

---

## 2. Comparative Analysis Matrix

### 2.1 Core Capabilities

| Feature | .tilt | USDZ | glTF | XRRAI (proposed) |
|---------|-------|------|------|------------------|
| Brush strokes | ✅ Native | ❌ | ⚠️ Custom ext | ✅ Native |
| AR anchors | ❌ | ⚠️ Apple only | ⚠️ Proposed | ✅ Native |
| Physics | ❌ | ✅ UsdPhysics | ⚠️ KHR_physics | ✅ Native |
| Audio reactive | ❌ | ❌ | ❌ | ✅ Native |
| Gaussian splats | ❌ | ⚠️ Omniverse | ✅ KHR_gaussian | ✅ Native |
| Streaming | ❌ | ❌ | ⚠️ Progressive | ✅ Chunked |
| Layers | ✅ | ✅ | ⚠️ Via nodes | ✅ Native |
| Hologram data | ❌ | ❌ | ❌ | ✅ Native |

### 2.2 Platform Compatibility

| Platform | .tilt | USDZ | glTF | XRRAI |
|----------|-------|------|------|-------|
| iOS AR | ⚠️ View | ✅ Native | ✅ model-viewer | ✅ Via export |
| Android AR | ⚠️ View | ❌ None | ✅ Scene Viewer | ✅ Via export |
| Quest 3 | ✅ VR | ❌ | ✅ Full | ✅ Via export |
| Vision Pro | ❌ | ✅ Native | ⚠️ Web | ✅ Via USDZ |
| WebGL | ✅ three-tilt | ❌ | ✅ Primary | ✅ Via glTF |
| WebGPU | ⚠️ | ❌ | ✅ | ✅ Via glTF |
| Unity | ✅ Toolkit | ⚠️ Exp | ✅ glTFast | ✅ Native |

### 2.3 Future-Proofing

| Aspect | .tilt | USDZ | glTF | XRRAI |
|--------|-------|------|------|-------|
| Standards body | Community | AOUSD | Khronos | Internal |
| AI/ML roadmap | ❌ | ⚠️ Omniverse | ✅ 3DGS, physics | ✅ Native |
| Metaverse ready | ⚠️ | ✅ AOUSD focus | ✅ MSF member | ✅ By design |
| WebXR alignment | ⚠️ | ❌ | ✅ Primary | ✅ Via export |
| Extensibility | Bitmask | Schema | JSON ext | JSON ext |

---

## 3. XRRAI Format Specification (Proposed)

### 3.1 Design Goals

1. **Full MetavidoVFX fidelity** - All brush, hologram, VFX data
2. **Export-friendly** - Clean conversion to glTF/USDZ
3. **Streaming-ready** - Chunked loading for large scenes
4. **AI-native** - Gaussian splats, AI model refs, procedural data
5. **Cross-platform** - JSON core, binary attachments

### 3.2 File Structure

```
scene.xrrai (JSON) or scene.xrraib (binary container)
├── manifest.json         - Version, generator, dependencies
├── scene/
│   ├── hierarchy.json    - Node graph
│   ├── transforms.bin    - Position/rotation/scale (binary)
│   └── metadata.json     - Tags, descriptions
├── strokes/
│   ├── strokes.json      - Stroke metadata
│   └── points.bin        - Control points (binary)
├── holograms/
│   ├── sources.json      - Hologram configurations
│   └── recordings/       - HologramRecording data
├── vfx/
│   ├── instances.json    - VFX instance data
│   └── parameters.json   - Runtime parameters
├── anchors/
│   └── ar_anchors.json   - AR Foundation anchors
├── assets/
│   ├── meshes/           - GLB files
│   ├── textures/         - KTX2/PNG/JPEG
│   ├── audio/            - M4A/MP3
│   └── splats/           - SPZ files
└── export/
    └── preview.glb       - Pre-generated glTF export
```

### 3.3 Core Schema

```json
{
  "xrrai": "1.0",
  "generator": "MetavidoVFX/2026.1",
  "created": "2026-01-22T12:00:00Z",
  "scene": {
    "name": "My AR Scene",
    "description": "Created in MetavidoVFX",
    "tags": ["ar", "hologram", "brush"],
    "bounds": {"min": [-10,-10,-10], "max": [10,10,10]}
  },
  "extensions": ["XRRAI_hologram", "XRRAI_brush", "XRRAI_vfx"]
}
```

### 3.4 Stroke Schema (XRRAI_brush)

```json
{
  "brushes": [
    {
      "id": "brush_001",
      "name": "Glow",
      "material": "BrushStrokeGlow",
      "geometry": "flat|tube|particles",
      "audioReactive": {
        "enabled": true,
        "sizeModulation": 0.5,
        "colorModulation": 0.3,
        "frequencyBand": 1
      }
    }
  ],
  "strokes": [
    {
      "id": "stroke_001",
      "brushId": "brush_001",
      "color": [1.0, 0.5, 0.2, 1.0],
      "size": 0.02,
      "layerId": "layer_001",
      "mirrorId": "mirror_001",
      "pointsRef": "strokes/points.bin#0-1024"
    }
  ]
}
```

### 3.5 Hologram Schema (XRRAI_hologram)

```json
{
  "holograms": [
    {
      "id": "holo_001",
      "type": "live|recorded|remote",
      "source": {
        "type": "ARDepthSource|MetavidoStream|GaussianSplat",
        "config": {...}
      },
      "quality": "low|medium|high|ultra",
      "anchor": "anchor_001"
    }
  ],
  "recordings": [
    {
      "id": "rec_001",
      "duration": 30.5,
      "fps": 30,
      "dataRef": "holograms/recordings/rec_001.h264"
    }
  ]
}
```

### 3.6 AR Anchor Schema

```json
{
  "anchors": [
    {
      "id": "anchor_001",
      "type": "plane|image|face|body|world",
      "classification": "floor|wall|ceiling|table",
      "position": [0, 0, 0],
      "rotation": [0, 0, 0, 1],
      "confidence": 0.95,
      "persistent": true
    }
  ]
}
```

### 3.7 Export Pipeline

```
XRRAI Scene
    ↓
┌───────────────────────────────────────────────────────┐
│ XRRAIExporter                                         │
│   ├── ExportGLTF() → Universal distribution           │
│   │     ├── Strokes → Meshes                          │
│   │     ├── Holograms → Point clouds / meshes         │
│   │     ├── VFX → Particle systems (where possible)   │
│   │     └── Anchors → Custom extension                │
│   │                                                   │
│   ├── ExportUSDZ() → iOS/visionOS                     │
│   │     ├── Convert meshes to UsdGeomMesh             │
│   │     ├── Materials to MaterialX                    │
│   │     └── Strip unsupported features                │
│   │                                                   │
│   └── ExportTilt() → Open Brush compatibility         │
│         ├── Strokes → data.sketch                     │
│         ├── Brushes → BrushIndex GUIDs                │
│         └── Layers → metadata.json                    │
└───────────────────────────────────────────────────────┘
```

---

## 4. Recommendations

### 4.1 MetavidoVFX Implementation Strategy

**Phase 1: Internal Format (XRRAI)**
- Implement XRRAI JSON schema for scene save/load
- Binary attachments for large data (points, textures)
- Chunked streaming support

**Phase 2: Export Pipeline**
- glTF/GLB export via glTFast
- USDZ export for iOS Quick Look
- .tilt export for Open Brush community

**Phase 3: Import Support**
- .tilt import for brush strokes
- glTF import for 3D models (Icosa integration)
- SPZ import for Gaussian splats

**Phase 4: Cloud Integration**
- Icosa Gallery upload (glTF)
- WebGPU viewer with XRRAI → glTF conversion
- Web AR via model-viewer pattern

### 4.2 Platform-Specific Guidance

| Platform | Primary Format | Fallback |
|----------|----------------|----------|
| iOS Quick Look | USDZ | - |
| Android Scene Viewer | glTF | - |
| Quest Browser | glTF | - |
| Vision Pro | USDZ | glTF (web) |
| WebXR | glTF | - |
| Open Brush VR | .tilt | glTF |
| Icosa Gallery | glTF | .tilt |

### 4.3 Future Considerations

1. **KHR_gaussian_splatting** - Monitor Khronos progress, prepare adapter
2. **KHR_interactivity** - Portable behaviors for cross-platform
3. **AOUSD Core Spec** - Track for visionOS requirements
4. **AI model integration** - Text-to-3D outputs (Meshy, Tripo) already GLB

---

## 5. References

### Specifications
- [OpenUSD USDZ Specification](https://openusd.org/dev/spec_usdz.html)
- [Khronos glTF 2.0 Specification](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html)
- [glTF Extensions Registry](https://github.com/KhronosGroup/glTF/blob/main/extensions/README.md)
- [KHR_gaussian_splatting Proposal](https://github.com/KhronosGroup/glTF/pull/2490)

### Tools & Libraries
- [glTFast (Unity)](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@5.2/manual/index.html)
- [three-tiltloader](https://github.com/icosa-gallery/three-tiltloader)
- [three-icosa](https://github.com/icosa-foundation/three-icosa)
- [Niantic SPZ Format](https://github.com/nianticlabs/spz)
- [model-viewer](https://modelviewer.dev/)

### Research
- [AOUSD Roadmap](https://www.linuxfoundation.org/press/alliance-for-openusd-unveils-roadmap-for-core-usd-specification)
- [Khronos 3D Gaussian Splats Blog](https://www.khronos.org/blog/khronos-ogc-and-geospatial-leaders-add-3d-gaussian-splats-to-the-gltf-asset-standard)
- [Babylon.js 8.0 WebGPU/glTF](https://blogs.windows.com/windowsdeveloper/2025/04/03/part-3-babylon-js-8-0-gltf-usdz-and-webxr-advancements/)

---

*Created by deep research analysis of .tilt, USDZ, glTF, and AI/ML 3D formats*
*For MetavidoVFX/Unity-XR-AI project*
