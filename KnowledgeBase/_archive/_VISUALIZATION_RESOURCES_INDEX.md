# Visualization Resources Index

*Auto-generated: 2026-01-10*
*Purpose: Consolidated reference for all 3D/graph visualization tools across local projects*

---

## Quick Reference Matrix

| Tool | Type | Framework | Max Nodes | 3D | WebGL | Collaboration |
|------|------|-----------|-----------|----|----|---------------|
| XRAI-KG Dashboard | Graph + 3D | Cytoscape + Three.js | 1000+ | Yes | Yes | No |
| ultra-search-engine | 3D Search | React Three Fiber | 2,000,000 | Yes | Yes | Yes |
| immersive-search | 3D Search | R3F + Socket.IO | 500K+ | Yes | Yes | Yes |
| 5dio / Triplex | 3D Game/App | R3F + Koota | N/A | Yes | Yes | No |
| HOLOVIS | Unity Viz | Three.js | 10,000+ | Yes | Yes | No |
| 3d-force-graph | Graph | Three.js | 100,000+ | Yes | Yes | No |
| xrai-viewer.js | XRAI Format | WebGL/WebGPU | N/A | Yes | Yes | No |
| Hyper Index Explorer | Knowledge | D3.js | 50,000+ | No | No | No |
| NewApp VFX | Point Cloud | Unity VFX Graph | 100K | Yes | Metal | No |
| MetavidoVFX | LiDAR VFX | Unity + ARKit | 32K | Yes | Metal | No |
| XRAI Voice HUD | Voice AI | ECharts+Cytoscape | 100+ | No | Yes | No |
| Hyperfy | Virtual Worlds | PhysX + WebXR | N/A | Yes | Yes | Yes |

---

## 1. XRAI Knowledge Graph Dashboard (NEW)

**Location:** `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/knowledge-graph-xrai-dashboard.html`

**Features:**
- 2D Cytoscape graph visualization
- 3D Three.js force-directed graph (toggle with `3` key)
- XRAI format export (3 modes: Full World, Graph, Manifest)
- Real-time search filtering
- Entity details panel with XRAI metadata
- Keyboard shortcuts: `/` search, `2`/`3` view mode, `E` export

**Data Source:** MCP Memory entities/relations

**Export Formats:**
1. **Full XRAI World** - Complete 3D environment with rooms, portals, AI components
2. **Graph XRAI** - 3D force-directed graph as XRAI scene
3. **JSON Manifest** - XRAI-compatible JSON with spatial metadata

---

## 2. Ultra-Performance 3D AI Search Engine

**Location:** `/Users/jamestunick/ultra-search-engine/`

**Capabilities:**
- **2,000,000 nodes** at 60 FPS (Apple Silicon optimized)
- **25,000 concurrent users** via WebSocket + Redis
- GPU particle systems with Metal Performance Shaders
- Dynamic Level of Detail (LOD) rendering

**Visualization Modes:**
1. **Galaxy Layout** - Spiral arrangement with gravitational clustering
2. **Sphere Layout** - Fibonacci sphere distribution
3. **ConeTree Layout** - Hierarchical 3D tree structure
4. **Force Layout** - Physics-based node positioning

**Tech Stack:**
- React 18 + Three.js Fiber
- WebGL 2.0 + Apple Metal
- Node.js Cluster (16 workers)
- Socket.IO + Redis

**Usage:**
```bash
cd /Users/jamestunick/ultra-search-engine
./start-ultra.sh
# Open http://localhost:3000
```

---

## 3. 5dio - React Three Fiber + Koota

**Location:** `/Users/jamestunick/5dio/`

**Architecture:**
- **Koota** - Entity Component System (ECS) state management
- **React Three Fiber** - Declarative 3D rendering
- **Triplex** - Visual editor integration
- **TypeScript** - Type-safe development

**Key Patterns:**
```typescript
// Entities spawn as Koota world entities with traits
const world = createWorld();

// Systems run each frame in sequence
useFrame((_, delta) => {
  velocityTowardsTarget(world, delta);
  positionFromVelocity(world, delta);
  meshFromPosition(world, delta);
});
```

**File Structure:**
- `/src/entities/` - React components as ECS entities
- `/src/levels/` - Scene compositions
- `/src/shared/systems.ts` - Reusable behavior systems
- `/src/shared/traits.ts` - Entity traits (Position, Velocity, etc.)

---

## 4. HOLOVIS - Unity Project Visualizer

**Location:** `/Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/HOLOVIS/`

**Purpose:** 3D visualization of Unity project structure

**Visualization Modes:**
1. **Tree Graph** - Nested cube visualization of project hierarchy
2. **Node Graph** - Connected packages/systems fanning from center
3. **Flow Chart** - Timeline view of scenes and scripts

**Analysis Capabilities:**
- Unity version detection
- MonoBehaviour/ScriptableObject identification
- Package dependency mapping
- Asset relationship tracking (scenes, prefabs, materials, shaders)

**Usage:**
```bash
cd /Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/HOLOVIS
npm install && npm run serve
# Open http://localhost:3000
```

---

## 5. 3d-force-graph (vasturiano)

**Location:** `/Users/jamestunick/Documents/GitHub/3d-force-graph/`

**Features:**
- Force-directed 3D graph visualization
- WebGL rendering via Three.js
- Supports 100,000+ nodes
- VR/AR compatible

**Integration:**
```javascript
import ForceGraph3D from '3d-force-graph';

const Graph = ForceGraph3D()(elem)
  .graphData(data)
  .nodeColor(node => node.color)
  .linkDirectionalParticles(2);
```

**Used By:** HyperGraph Knowledge Explorer, XRAI-KG Dashboard

---

## 6. XRAI Viewer (xrai-viewer.js)

**Location:** `/Users/jamestunick/Documents/GitHub/xrrai/examples/web/xrai-viewer.js`

**Classes:**
- **XRAILoader** - Loads XRAI files, detects capabilities, builds scene graphs
- **XRAIDecoder** - Parses XRAI binary format (magic, TOC, sections)
- **XRAIScene** - Manages geometries, materials, VFX, AI components

**Geometry Types:**
- Mesh (traditional polygons)
- Splat (Gaussian splatting)
- NeRF (Neural Radiance Fields)
- SDF (Signed Distance Fields)

**Adaptive Quality:**
- GPU tier detection (1-3)
- Automatic LOD selection
- Fallback rendering for low-end devices

---

## 7. Hyper Index Explorer

**Location:** `/Users/jamestunick/Documents/GitHub/10-minute-sketches/`

**Features:**
- SQLite FTS5 full-text search
- MediaWiki API integration
- D3.js force-directed graph
- Wikipedia concept exploration

**Tech Stack:**
- Python/Flask backend
- D3.js visualization
- SQLite database

---

## Integration Patterns

### Knowledge Graph to 3D World Pipeline

```
MCP Memory Graph → XRAI-KG Dashboard → XRAI Export → Unity/WebXR Viewer
      ↓                    ↓                ↓              ↓
  Entities/          2D Cytoscape     JSON Manifest    Immersive
  Relations          3D Force Graph   Full World       Experience
```

### Visualization Selection Guide

| Use Case | Recommended Tool |
|----------|------------------|
| Knowledge graph exploration | XRAI-KG Dashboard |
| Massive dataset (1M+ nodes) | ultra-search-engine |
| Unity project analysis | HOLOVIS |
| 3D app development | 5dio (R3F + Koota) |
| Simple 3D graph | 3d-force-graph |
| XRAI format viewing | xrai-viewer.js |
| Wikipedia exploration | Hyper Index Explorer |

### Shared Dependencies

All Three.js-based tools share:
- WebGL 2.0 rendering
- OrbitControls for navigation
- GPU acceleration
- Responsive canvas sizing

---

## Future Integration Opportunities

1. **Unified Data Format**
   - XRAI as interchange format
   - MCP Memory as source of truth
   - Bidirectional sync

2. **Cross-Tool Navigation**
   - Deep links between dashboards
   - Shared entity identifiers
   - Synchronized selection

3. **VR/AR Extensions**
   - WebXR support in all viewers
   - visionOS compatibility
   - Hand tracking integration

4. **AI Enhancement**
   - Neural layout algorithms
   - Semantic clustering
   - Automatic annotation

---

## Quick Start Commands

```bash
# XRAI-KG Dashboard (static HTML)
open /Users/jamestunick/Documents/GitHub/Unity-XR-AI/knowledge-graph-xrai-dashboard.html

# ultra-search-engine
cd /Users/jamestunick/ultra-search-engine && ./start-ultra.sh

# 5dio
cd /Users/jamestunick/5dio && npm run dev

# HOLOVIS
cd /Users/jamestunick/Downloads/AI_Knowledge_Base_Setup/HOLOVIS && npm run serve

# 3d-force-graph examples
cd /Users/jamestunick/Documents/GitHub/3d-force-graph && npm run examples
```

---

## 8. Immersive 3D AI Search Engine

**Location:** `/Users/jamestunick/immersive-search-engine/`

**Features:**
- Interactive landscapes (cities, forests, galaxies) representing search results
- AI-powered clustering and query processing
- Multi-user collaboration with user avatars
- Adaptive 3D layouts based on data relationships
- Cross-platform (desktop, mobile, VR/AR)

**Tech Stack:**
- React 18 + Three.js Fiber
- Node.js + Express + Socket.IO
- PostgreSQL + Redis + Elasticsearch
- OpenAI/Anthropic APIs integration
- Brave Search, Google Custom Search

**Layout Modes:**
- Galaxy, City, Forest, Cluster

---

## 9. Triplex Projects (Koota ECS)

**Locations:**
- `/Users/jamestunick/5dio/`
- `/Users/jamestunick/my-triplex-project/`

**Architecture Pattern:**
```
React Components → Koota Entities → Traits → Systems → Behaviors
```

**Key Concepts:**
- **Entities**: Lightweight React components that spawn into Koota world
- **Traits**: Composable attributes (Position, Velocity, Controllable)
- **Systems**: Frame-by-frame behavior functions
- **Triplex**: Visual editor for scene development

---

## 10. NewApp - Unity AR VFX System

**Location:** `/Users/jamestunick/UnityProjects/NewApp/`

### VFX Point Cloud Processing Pipeline

**Components:**
| Script | Type | Use Case |
|--------|------|----------|
| VFXPointCloudProcessor.cs | CPU | < 10K points, mobile |
| GPUVFXPointCloudProcessor.cs | GPU | > 10K points, desktop |
| VFXGraphIntegration.cs | Bridge | Connects processors to VFX Graph |

**Data Structure:**
```csharp
struct VFXPointData {
    Vector3 position;  // Camera-relative
    Vector3 velocity;  // Motion data
    float life;        // Normalized [0-1]
}
```

### Performance Optimization Stack

**Buffer Management:**
- ComputeBufferType.Default for optimal GPU performance
- Double buffering to avoid GPU stalls
- Persistent allocation to avoid GC
- Max 100K points with adaptive quality scaling

**URP Optimizations:**
- Forward+ rendering
- SRP Batching (Vulkan/Metal)
- Platform-specific optimizations

**Profiling Integration:**
- Unity Profiler (CPU/GPU/Memory)
- Xcode Instruments (Metal signposts)
- Custom samplers for point cloud operations

### MetavidoVFX iOS Real-time

**Location:** `/Users/jamestunick/UnityProjects/NewApp/References/MetavidoVFX-main/`

**Pipeline:**
```
iPhone LiDAR → ARKit Depth → VFX Graph → Real-time Particles
```

**Key Scripts:**
- ARKitCameraCapture.cs
- RealtimeMetadataProvider.cs
- VFXRealtimeMetavidoBinder.cs
- PerformanceOptimizer.cs

**VFX Properties Exposed:**
- ColorMap (Texture2D)
- DepthMap (Texture2D)
- RayParams (Vector4)
- InverseView (Matrix4x4)
- DepthRange (Vector2)

**Performance Tuning:**
- Particle reduction: 262K → 32K (Afterimage)
- Mesh decimation: Level 4
- Dynamic quality adjustment
- Target: 60 FPS on iPhone Pro

---

## Integration Architecture

### Knowledge Graph → 3D World → VFX Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    DATA SOURCES                                  │
├─────────────────────────────────────────────────────────────────┤
│  MCP Memory    │  Search APIs   │  LiDAR Depth   │  Point Cloud │
│  (Entities)    │  (Results)     │  (ARKit)       │  (Sensor)    │
└───────┬────────┴───────┬────────┴───────┬────────┴───────┬──────┘
        │                │                │                │
        ▼                ▼                ▼                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    VISUALIZATION LAYER                           │
├─────────────────────────────────────────────────────────────────┤
│  Cytoscape 2D  │  3d-force-graph │  React Three  │  Unity VFX   │
│  (Knowledge)   │  (3D Graph)     │  Fiber (Web)  │  Graph       │
└───────┬────────┴───────┬─────────┴───────┬───────┴───────┬──────┘
        │                │                 │               │
        ▼                ▼                 ▼               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OUTPUT FORMATS                                │
├─────────────────────────────────────────────────────────────────┤
│  XRAI World    │  WebGL Canvas  │  iOS Metal    │  VR/AR Scene  │
│  (Export)      │  (Browser)     │  (Device)     │  (Immersive)  │
└─────────────────────────────────────────────────────────────────┘
```

### Tool Selection by Use Case

| Use Case | Web Tool | Unity Tool |
|----------|----------|------------|
| Knowledge exploration | XRAI-KG Dashboard | - |
| Massive search results | ultra/immersive-search | - |
| 3D app prototyping | 5dio / Triplex | Unity Editor |
| Point cloud VFX | - | NewApp VFX Pipeline |
| LiDAR real-time | - | MetavidoVFX |
| Project analysis | HOLOVIS | - |

---

## 11. XRAI Voice Assistant System

**Location:** `/Users/jamestunick/xrai/`

**Core Components:**
- **xrai.py** - Main voice assistant with continuous listening + Ollama LLM
- **master-agent-orchestrator.py** - Coordinates specialized agents
- **dashboard.html** - HUD visualization (ECharts + D3 + Cytoscape)
- **desktop-overlay.py** - Visual feedback overlay

**Dashboard (dashboard.html):**
```
ECharts (Timeline) + D3 (Sparkline) + Cytoscape (Minimap)
```
- Birdseye view with hierarchy/time/zoom controls
- Semi-transparent dock behavior
- Real-time conversation log visualization

**Agent System:**
| Agent | Purpose |
|-------|---------|
| github-knowledge-agent.py | GitHub analysis |
| deep-code-analyzer.py | Code analysis |
| predictive-agent.py | Predictive assistance |
| viral-innovation-agent.py | Innovation tracking |

**Integration:**
- LaunchAgent auto-start
- Raycast extension (start-xrai/)
- Whisper API transcription
- macOS TTS output

---

## 12. Hyperfy - 3D Virtual Worlds

**Location:** `/Users/jamestunick/xrai/eliza-3d-hyperfy-starter/my-world/`

**Features:**
- Open-source 3D virtual world framework
- Standalone persistent worlds (self-hosted)
- Real-time content creation in-world
- WebXR VR support
- Physics engine (PhysX)
- Networked collaboration

**Tech Stack:**
- Node.js 22.11+
- PhysX physics
- WebXR
- Component-based apps (JavaScript)

**Use Cases:**
- Virtual events & conferences
- Interactive showrooms
- Social spaces
- Gaming environments
- Educational experiences
- Creative showcases

**Commands:**
```bash
cd /Users/jamestunick/xrai/eliza-3d-hyperfy-starter/my-world
npm install && npm run dev
# Open browser to localhost
```

---

## Performance Reference

### Web (Three.js / R3F)
| Metric | Standard | Optimized |
|--------|----------|-----------|
| Nodes | 10K | 2M+ |
| FPS | 30 | 60 |
| Memory | 512MB | 128GB |
| Collaboration | None | 25K users |

### Unity (VFX Graph)
| Metric | Mobile | Desktop |
|--------|--------|---------|
| Particles | 32K | 262K |
| Point Cloud | 50K | 100K |
| Target FPS | 60 | 120 |
| Buffer Type | Default | Default |
| Batching | SRP | SRP + Instancing |

---

## 13. Google Research ML/AI Projects

**Location:** `/Users/jamestunick/google-research/` (738 projects)

### Knowledge Graph + Forecasting

| Project | Description |
|---------|-------------|
| **graph_temporal_ai** | Integrates metadata + user feedback as knowledge graphs into deep learning for domain-specific forecasting |
| **business_metric_aware_forecasting** | End-to-end differentiable inventory forecasting with business metrics |
| **neural_additive_models** | Interpretable ML with neural nets (NAM) |
| **graphqa** | "Talk like a Graph" - encoding graphs for LLMs |
| **spectral_graphormer** | Spectral graph-based transformer |

### Graph Temporal AI (Key Project)

**Purpose:** Integrate domain knowledge into AI forecasting

**Architecture:**
```
Time Series Data [T x N x F] + Graph Data [N x N]
         ↓                          ↓
    Deep Learning    ←→    Knowledge Graph
         ↓
  Domain-Specific Forecasts
```

**Key Innovation:**
- Users provide domain knowledge (association rules, market principles, logistics)
- Knowledge graphs encode metadata and user feedback
- Adaptive, intelligent forecasting vs passive prediction

**Tech Stack:** PyTorch, NumPy, Ray (hyperparameter tuning)

### Other Relevant Projects

| Project | Use Case |
|---------|----------|
| epi_forecasts | Epidemiological forecasting (flu, COVID) |
| flood_forecasting | Inundation modeling with ML |
| graph_embedding | Graph representation learning |
| graph_compression | Efficient graph storage |
| graph_sampler | Graph sampling techniques |

---

## Integration Concept: Knowledge Graph + Forecasting + Visualization

The user's project vision connects these systems:

```
┌─────────────────────────────────────────────────────────────────┐
│                    KNOWLEDGE LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│  MCP Memory     │  Domain Knowledge  │  User Feedback           │
│  (Entities)     │  (Rules/Patterns)  │  (Annotations)           │
└───────┬─────────┴─────────┬──────────┴─────────┬────────────────┘
        │                   │                    │
        ▼                   ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    GRAPH LAYER                                   │
├─────────────────────────────────────────────────────────────────┤
│  Knowledge Graph │  Temporal Graph    │  Spatial Graph          │
│  (Semantic)      │  (Time Series)     │  (Geographic)           │
└───────┬──────────┴─────────┬──────────┴─────────┬───────────────┘
        │                    │                    │
        ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    DEEP LEARNING LAYER                           │
├─────────────────────────────────────────────────────────────────┤
│  Neural Additive │  Graph Neural      │  Transformer            │
│  Models (NAM)    │  Networks (GNN)    │  (Attention)            │
└───────┬──────────┴─────────┬──────────┴─────────┬───────────────┘
        │                    │                    │
        ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OUTPUT LAYER                                  │
├─────────────────────────────────────────────────────────────────┤
│  Forecasts       │  Visualizations    │  XRAI Worlds            │
│  (Predictions)   │  (3D Graphs)       │  (Immersive)            │
└─────────────────────────────────────────────────────────────────┘
```

### Potential Pipeline

1. **Ingest** → MCP Memory entities from sessions
2. **Encode** → Graph Temporal AI for temporal patterns
3. **Enhance** → NAM for interpretable feature importance
4. **Visualize** → XRAI-KG Dashboard (2D/3D)
5. **Export** → XRAI World for immersive exploration
6. **Forecast** → Predict future knowledge graph evolution

---

*Last updated: 2026-01-10*
*Maintainer: Claude Code session*
