# WebGPU, Three.js & Web 3D Visualization (2025-2026)

## Browser Support Status (November 2025)

WebGPU now ships by default in ALL major browsers:
- **Chrome**: v113+ (since 2023)
- **Safari**: v26.0 (September 2025)
- **Firefox**: v141 (July 2025, Windows)
- **Edge**: Full support

**Gap**: Linux support still rolling out with driver-specific requirements.

---

## Category: LIBRARIES & FRAMEWORKS

### Three.js WebGPU
**Type**: Library
**Latest**: r170+ with full WebGPURenderer support

```javascript
// Migration: WebGL to WebGPU
import * as THREE from 'three/webgpu';
import { WebGPURenderer } from 'three/webgpu';

const renderer = new WebGPURenderer({ antialias: true });
// Automatically falls back to WebGL 2 if WebGPU unavailable
```

**Key Features (r170)**:
- Device Lost Event handling
- GLTFLoader WebGPU support
- Bloom-based lens flare FX
- Selective outlines FX
- SMAA (Subpixel Morphological Anti-Aliasing)
- UASTC HDR transcoding to BC6H

**TSL (Three Shading Language)**:
- Renderer-agnostic shader language
- Transpiles to GLSL (WebGL) or WGSL (WebGPU)
- JavaScript-like syntax
- Node materials: `MeshBasicNodeMaterial`, `MeshStandardNodeMaterial`

**Sources**:
- [Three.js Releases](https://github.com/mrdoob/three.js/releases)
- [WebGPURenderer Docs](https://threejs.org/docs/pages/WebGPURenderer.html)
- [Field Guide to TSL](https://blog.maximeheckel.com/posts/field-guide-to-tsl-and-webgpu/)

---

### React Three Fiber (R3F) v9
**Type**: Library (React wrapper)
**Status**: Full WebGPU support

```javascript
import * as THREE from 'three/webgpu';
import { extend, Canvas } from '@react-three/fiber';

// Extend Three.js elements for R3F
extend(THREE);

// Async WebGPURenderer setup
<Canvas
  gl={async (canvas) => {
    const renderer = new THREE.WebGPURenderer({ canvas, antialias: true });
    await renderer.init();
    return renderer;
  }}
>
```

**Resources**:
- [v9 Migration Guide](https://r3f.docs.pmnd.rs/tutorials/v9-migration-guide)
- [r3f-webgpu-starter](https://github.com/ektogamat/r3f-webgpu-starter)
- [Wawa Sensei TSL Course](https://wawasensei.dev/courses/react-three-fiber/lessons/webgpu-tsl)

---

### 3d-force-graph (vasturiano)
**Type**: Library
**Platform**: WebGL (WebGPU coming)

```javascript
import ForceGraph3D from '3d-force-graph';

const Graph = ForceGraph3D()(container)
  .graphData({ nodes, links })
  .nodeColor(node => colorByType(node.type))
  .linkCurvature(0.25)
  .linkDirectionalParticles(2);
```

**Source**: [GitHub - 3d-force-graph](https://github.com/vasturiano/3d-force-graph)

---

### GraphPU
**Type**: Library (Rust/WGPU)
**Purpose**: Large-scale GPU graph visualization (millions of nodes)

**Key Optimizations**:
- 18 GPGPU compute kernels
- Barnes-Hut algorithm (O(n²) → O(n log n))
- Custom atomic float operations in WGSL
- Bitonic merge sort for particle sorting
- 60x speedup on spring force calculations

**Source**: [Building GraphPU](https://latentcat.com/en/blog/building-graphpu)

---

## Category: VIEWERS & RENDERERS

### WebGPU Gaussian Splatting Renderers

| Project | Platform | Performance |
|---------|----------|-------------|
| [Visionary](https://github.com/Visionary-Laboratory/visionary) | WebGPU + ONNX | 2.1ms/frame, 0.6ms for 6M points |
| [web-splat](https://github.com/KeKsBoTer/web-splat) | Rust/WGPU | >200 FPS (RTX 3090), ~130 FPS (RX 380) |
| [Scthe/gaussian-splatting-webgpu](https://github.com/Scthe/gaussian-splatting-webgpu) | WebGPU | Pure compute shader implementation |
| [GSWT Renderer](https://github.com/zengyf131/gswt_renderer) | WGPU | SIGGRAPH Asia 2025 paper |

**Visionary Features**:
- Real-time 3DGS, 4DGS, MLP-based 3DGS, Neural Avatars
- Per-frame ONNX inference
- Click-to-run browser experience
- Eliminates CPU bottleneck of WebGL approaches

---

### LLM Visualization Viewers

| Project | Description | URL |
|---------|-------------|-----|
| LLM-Viz | 3D GPT-style network visualization | [bbycroft.net/llm](https://bbycroft.net/llm) |
| Transformer Explainer | Interactive GPT-2 walkthrough | [poloclub.github.io](https://poloclub.github.io/transformer-explainer/) |

**LLM-Viz**:
- Visualizes GPT-2, nano-GPT, GPT-3 architecture
- Real-time inference visualization
- Every layer, attention head, matrix operation visible

**Source**: [GitHub - llm-viz](https://github.com/bbycroft/llm-viz)

---

### Code City Viewers

| Tool | Platform | Description |
|------|----------|-------------|
| [CodeCharta](https://codecharta.com/) | Web | Files as buildings, folders as districts |
| [ExplorViz](https://explorviz.net/) | Web | Live trace visualization, semantic zoom |
| [SoftwareCity](https://github.com/jonaslanzlinger/software-city-project) | Web | 3D repo structure visualization |

**CodeCharta Features**:
- Area, height, color represent different metrics
- Hotspot detection
- Fully local (no data upload)

**2025 Research**: "Semantic Zoom and Mini-Maps for Software Cities" - VISSOFT 2025

---

## Category: DATASETS & SEARCH

### 3D Model Search APIs

| Service | Type | Notes |
|---------|------|-------|
| Objaverse | Dataset/API | Massive 3D model library |
| Icosa/Casa Gallery | Search | 3D model search |
| Sketchfab | Search/Dataset | Commercial 3D library |
| Polycam | Tools | Gaussian splatting creation |

### Knowledge Graph APIs

| Platform | Type | Features |
|----------|------|----------|
| [NVIDIA DGX Spark](https://build.nvidia.com/spark/txt2kg) | Search + Viz | Text-to-knowledge-graph, WebGPU rendering |
| [graphrag-workbench](https://github.com/ChristopherLyon/graphrag-workbench) | Viewer | Microsoft GraphRAG visualization |
| Neo4j | Database | Graph database with 3D viz plugins |

---

## Category: AI-POWERED VISUALIZATION

### Neural Network Visualization

| Tool | Purpose | Technology |
|------|---------|------------|
| LLM-Viz | Transformer architecture | WebGL |
| Transformer Explainer | Attention mechanisms | Interactive web |
| Transformers.js v3 | In-browser inference | WebGPU, >60 tokens/sec |

### AI-Generated Visualizations

- LLMs generating visualization code from natural language
- Real-time dashboard creation from prompts
- Automated data-driven reports

### Embedding Visualization

- **t-SNE / UMAP**: Project high-dimensional embeddings to 2D/3D
- Token clusters visualization
- Semantic similarity mapping

---

## Category: SIMULATION & DIGITAL TWINS

### Command & Control Platforms

| Platform | Domain | Features |
|----------|--------|----------|
| NVIDIA Omniverse | Universal | Collaborative 3D, real-time physics |
| Unity 6 | Gaming/Industrial | WebGPU backend, digital twin support |
| Unreal Engine | Real-time | Photorealistic rendering |
| FCReality | Control Rooms | Mission-critical environment simulation |

### Digital Twin Visualization Layers

1. **Data Ingestion**: Real-time sensor feeds
2. **3D Model Layer**: Photorealistic/schematic views
3. **KPI Overlay**: Dynamic metrics display
4. **Simulation Layer**: Physics, thermal, electrical
5. **AR/VR Interface**: Immersive exploration

### Data Center Digital Twins

- 3D rack elevations with real-time power/environmental data
- Dynamic power single-line diagrams
- Capacity visualization and optimization
- Live network topology mapping

---

## Category: CUTTING-EDGE PROJECTS (2025)

### Expo 2025 Fluid Simulation
- Material Point Method (MPM) for particles
- Three.js WebGPU renderer
- Smooth pressure-based reactions
- By Renaud Rohlinger (Utsubo)

### Galaxy Simulation
- 1 million particles with physics
- WebGPU compute shaders
- Procedural generation
- Real-time interactivity

**Source**: [Galaxy Simulation Tutorial](https://threejsroadmap.com/blog/galaxy-simulation-webgpu-compute-shaders)

### Interactive Text Destruction (Codrops)
- Three.js + WebGPU + TSL
- Dynamic letter explosion
- Procedural shape generation

**Source**: [Codrops Tutorial](https://tympanus.net/codrops/2025/07/22/interactive-text-destruction-with-three-js-webgpu-and-tsl/)

### Visual Shader Editor (Shade)
- Graph-based shader design
- Patch interface
- Direct GPU compilation
- By Mustafa Ali

**Source**: [Three.js Forum - Shade](https://discourse.threejs.org/t/shade-webgpu-graphics/66969)

---

## WebGPU Performance Comparison

| Feature | WebGL | WebGPU |
|---------|-------|--------|
| Compute Shaders | No | Yes |
| Multi-threaded | No | Yes |
| Memory Management | Automatic | Explicit |
| Shader Language | GLSL | WGSL |
| Pipeline Control | Limited | Full |
| Typical Speedup | 1x | 2-10x |

### Gaussian Splatting Benchmarks

| Implementation | Platform | 6M Points |
|---------------|----------|-----------|
| WebGL + CPU | CPU sort | ~15-30ms/frame |
| WebGPU (Visionary) | GPU sort | ~0.6ms |
| Native (CUDA) | GPU | ~0.2ms |

---

## Integration with Your Projects

### HyperGraph Knowledge Explorer
- Use GraphPU patterns for large-scale rendering
- Implement city layout for semantic zoom
- WebGPU compute for force simulation

### XRAI Format
- WebGPU renderer for VFX Graph playback
- Gaussian splat integration for neural fields
- TSL for custom shader nodes

### VNMF
- WebGPU for lightfield rendering
- Compute shaders for neural inference
- Real-time audiofield processing

### GlobeGraph / sociographer
- Three.js r170+ WebGPU upgrade path
- 3d-force-graph for relationship visualization
- City layout for hierarchical data

---

## Migration Checklist: WebGL → WebGPU

1. Update imports: `three` → `three/webgpu`
2. Replace `WebGLRenderer` → `WebGPURenderer`
3. Convert GLSL shaders → TSL node materials
4. Add async initialization (`await renderer.init()`)
5. Update geometry to use `GPUBufferUsage` flags
6. Test fallback to WebGL2

---

*Generated: 2026-01-13*
*Sources: Web research, Three.js docs, GitHub repositories*
