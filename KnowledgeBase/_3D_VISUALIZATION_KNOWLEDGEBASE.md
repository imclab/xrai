# 3D Visualization Knowledge Base

## Overview

Comprehensive reference for 3D knowledge graph visualization, force-directed layouts, and spatial data representation. This knowledgebase connects to the following key projects:

- **GlobeGraph** - Globe-based graph visualization (imclab/GlobeGraph)
- **sociographer** - Social graph visualizer (imclab/sociographer)
- **XRAI-Format** - eXtended Reality AI file format with VFX Graph system
- **VNMF** - Volumetric Neural Media Format (perception-first philosophy)
- **HyperGraph Knowledge Explorer** - Universal 3D visualization for knowledge, code, and AI systems
- **3d-force-graph** (vasturiano) - Three.js + D3 force-directed graphs

---

## Core Visualization Implementations

### 1. CosmosVisualizer (XRAI Project)

Full-featured 3D visualization system with multiple layout modes and data source integration.

**Location**: `XRAI/cosmos-standalone-web/src/main.js`

**Key Features**:
- Multiple layout modes: `force`, `cosmos`, `city`, `tree`
- WebGPU support with WebGL fallback
- GSAP-powered camera animations
- Streaming search with progressive rendering
- Stress testing for 1M+ nodes
- Multi-format model loading (GLTF, OBJ, FBX, USDZ)

**Data Sources**:
| Source | Color | Description |
|--------|-------|-------------|
| icosa | #FF6B6B | Casa Gallery - 3D models |
| objaverse | #4ECDC4 | Objaverse API - massive 3D library |
| github | #95E1D3 | GitHub repositories |
| local | #F38181 | Local filesystem |
| web | #AA96DA | Web search results |
| sketchfab | #1CAAD9 | Sketchfab models |

**Scene Setup Pattern**:
```javascript
// Three.js scene with WebGPU detection
if (navigator.gpu) {
    this.renderer = new THREE.WebGLRenderer({
        canvas, antialias: true, powerPreference: 'high-performance'
    });
} else {
    this.renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
}
this.renderer.toneMapping = THREE.ACESFilmicToneMapping;
this.scene.fog = new THREE.Fog(0x0a0a0a, 100, 1000);
```

**Node Interaction Pattern**:
```javascript
zoomToNode(object, clickPoint, nodeData) {
    const box = new THREE.Box3().setFromObject(object);
    const size = box.getSize(new THREE.Vector3());
    const distance = Math.max(Math.max(size.x, size.y, size.z) * 3, 10);

    gsap.to(this.camera.position, {
        ...targetCameraPos, duration: 1, ease: 'power2.inOut',
        onUpdate: () => this.camera.lookAt(targetPoint)
    });
}
```

---

### 2. HypergraphRenderer (TypeScript)

Clean TypeScript implementation with force and city layouts.

**Location**: `XRAI/standalone-web/src/hypergraph.ts`

**Data Types**:
```typescript
type LayoutMode = "force" | "city";

interface HypergraphNode {
  id: string;
  label: string;
  type: "star" | "actor" | "repo" | "artifact" | "model" | "search";
  position?: [number, number, number];
  color?: number;
  magnitude?: number;
}

interface HypergraphLink {
  source: string;
  target: string;
  kind: "orbit" | "activity" | "artifact" | "search";
}
```

**Force Layout Algorithm**:
```typescript
private applyForceLayout(nodes, links): Map<string, [number, number, number]> {
    const iterations = 150;
    const repulsion = 80;
    const spring = 0.12;

    for (let i = 0; i < iterations; i++) {
        // Repulsion: nodes push each other apart
        nodes.forEach((a, idxA) => {
            nodes.slice(idxA + 1).forEach((b) => {
                const dir = new THREE.Vector3(...posA).sub(new THREE.Vector3(...posB));
                const distanceSquared = Math.max(dir.lengthSq(), 0.01);
                const force = repulsion / distanceSquared;
                dir.normalize().multiplyScalar(force);
                // Apply force to both nodes
            });
        });

        // Attraction: connected nodes pull together
        links.forEach((link) => {
            const dir = targetVec.clone().sub(sourceVec);
            const displacement = dir.normalize().multiplyScalar((distance - 35) * spring);
        });
    }
}
```

**City Layout Algorithm** (for hierarchical data):
```typescript
private applyCityLayout(nodes, links): Map<string, [number, number, number]> {
    // Build adjacency and degree maps
    // BFS traversal to compute depth levels
    // Sort by degree within each depth level
    // Position: x = lane position, y = height (log of degree), z = depth

    const laneSpacing = 16;
    const depthSpacing = 36;

    depthGroups.forEach((ids, depth) => {
        ids.forEach((id, idx) => {
            const height = Math.max(4, Math.log2(degree + 1) * 8);
            positions.set(id, [idx * laneSpacing - offset, height, depth * depthSpacing]);
        });
    });
}
```

---

### 3. ECharts Force Layout

Apache ECharts implementation for 2D force-directed graphs with curvature support.

**Location**: `echarts-6.0.0/src/chart/graph/forceLayout.ts`

**Key Parameters**:
```typescript
const forceInstance = forceLayout(nodes, edges, {
    rect: boundingRect,
    gravity: forceModel.get('gravity'),
    friction: forceModel.get('friction')
});

// Node properties
{ w: repulsion, rep: repulsion, fixed: boolean, p: [x, y] | null }

// Edge properties
{ n1: node1, n2: node2, d: distance, curveness, ignoreForceLayout }
```

---

## Layout Algorithms Reference

### Force-Directed Layout

| Parameter | Typical Value | Description |
|-----------|---------------|-------------|
| repulsion | 80-200 | Node push-apart force |
| spring | 0.1-0.2 | Edge attraction strength |
| friction | 0.5-0.9 | Velocity dampening |
| gravity | 0.1-0.3 | Center-pulling force |
| iterations | 100-300 | Simulation steps |

### City/Block Layout

- **X-axis**: Lane position (sorted by degree within depth)
- **Y-axis**: Height = log2(connections + 1) * scale
- **Z-axis**: BFS depth from root nodes

### Cosmos/Sphere Layout

- Distribute nodes on sphere surface
- Cluster by category/type
- Use spherical coordinates (theta, phi, radius)

---

## Integration Patterns

### With 3d-force-graph (vasturiano)

```javascript
import ForceGraph3D from '3d-force-graph';

const Graph = ForceGraph3D()(document.getElementById('3d-graph'))
    .graphData({ nodes, links })
    .nodeColor(node => getColorByType(node.type))
    .nodeVal(node => node.weight)
    .linkCurvature(0.25)
    .linkDirectionalParticles(2)
    .onNodeClick(node => handleNodeClick(node));
```

### With Globe Visualization (GlobeGraph pattern)

```javascript
// Convert lat/lng to 3D sphere position
function latLngToVector3(lat, lng, radius) {
    const phi = (90 - lat) * (Math.PI / 180);
    const theta = (lng + 180) * (Math.PI / 180);
    return new THREE.Vector3(
        -radius * Math.sin(phi) * Math.cos(theta),
        radius * Math.cos(phi),
        radius * Math.sin(phi) * Math.sin(theta)
    );
}
```

### With HyperGraph Knowledge Explorer

- **Semantic Zoom Levels**: Universe → System → Module → Component → Member → Statement
- **Adapter Pattern**: MCP Memory, FileSystem, TypeScript, Python, Git
- **Read-only Guarantee**: Never modifies source data

---

## Related GitHub Projects (from arfoundation-vfx-master-list)

### 3D Data Visualization

| Project | Description | Stars |
|---------|-------------|-------|
| [vasturiano/3d-force-graph](https://github.com/vasturiano/3d-force-graph) | 3D force-directed graph | 4k+ |
| [vasturiano/three-globe](https://github.com/vasturiano/three-globe) | WebGL globe visualization | 1k+ |
| [Niekes/d3-3d](https://github.com/Niekes/d3-3d) | 3D visualizations with D3 | 500+ |
| [keijiro/GeoVfx](https://github.com/keijiro/GeoVfx) | Geographic data viz in Unity | 300+ |
| [Dandarawy/Unity3D-Globe](https://github.com/Dandarawy/Unity3D-Globe) | Chrome experiment port | 200+ |

### Knowledge Graph Visualization

| Project | Description | Platform |
|---------|-------------|----------|
| [pennmem/brain_viz_unity](https://github.com/pennmem/brain_viz_unity) | Brain network visualization | Unity WebGL |
| [Call-for-Code/UnityStarterKit](https://github.com/Call-for-Code/UnityStarterKit) | Data visualization in AR/VR | Unity |
| stardustjs.github.io | GPU-based visualizations | WebGL |

---

## Performance Optimization

### Node Count Thresholds

| Count | Technique |
|-------|-----------|
| < 1,000 | Full DOM labels, rich interactions |
| 1,000 - 10,000 | Instanced meshes, LOD labels |
| 10,000 - 100,000 | GPU particles, no labels |
| 100,000+ | Compute shaders, spatial indexing |

### GPU Optimization Pattern

```javascript
// Instanced rendering for many nodes
const geometry = new THREE.BufferGeometry();
geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));

const material = new THREE.PointsMaterial({
    size: 2.1,
    vertexColors: true,
    transparent: true,
    blending: THREE.AdditiveBlending
});

const points = new THREE.Points(geometry, material);
```

### Streaming/Progressive Rendering

```javascript
// Process results as they stream in
searchHandle.onProgress(({ newResults, totalResults, count }) => {
    if (count % 50 === 0 || count < 50) {
        requestAnimationFrame(() => {
            const graphData = convertToGraphData(currentResults);
            updateVisualizationFast(graphData);
        });
    }
});
```

---

## File Format Integration

### XRAI Format VFX Graph System

XRAI supports embedded VFX Graph definitions for dynamic particle effects:
- Node-based effect definitions
- AI-driven parameter adaptation
- Real-time streaming updates

### VNMF 6-Layer Architecture

1. **Lightfield Layer** - Appearance at any view angle
2. **Audiofield Layer** - Spatial sound
3. **Interaction Layer** - User input responses
4. **Semantic Layer** - Meaning and relationships
5. **Environment Layer** - Context integration
6. **Fallback Layer** - mesh.gltf for compatibility

---

## Cross-References

- `_COSMOS_VISUALIZATION_RESOURCES.md` - Illustris, CosmosVR, 3D-TSNE
- `arfoundation-vfx-master-list.md` - 500+ related projects
- `HumanDepthToVFX.cs` - AR depth to particle effects
- `XRAI/CLAUDE.md` - Full XRAI project context

---

## Key Contributors

- **Keijiro Takahashi** - VFX Graph, depth processing
- **Vasturiano** - 3d-force-graph, three-globe
- **HECOMI** - Face/depth visualization
- **James Tunick (imclab)** - GlobeGraph, sociographer

---

*Generated: 2026-01-13*
*Sources: XRAI cosmos-standalone-web, hypergraph.ts, echarts-6.0.0, arfoundation-vfx-master-list*
