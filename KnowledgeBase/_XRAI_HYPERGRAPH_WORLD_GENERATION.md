# XRAI World Generation from Hypergraphs

**Version**: 1.0
**Created**: 2026-01-10
**Status**: Research Specification
**Related**: `_REPO_GRAPH_SCHEMA.md`, `_JT_PRIORITIES.md`, `_WEB_INTEROPERABILITY_STANDARDS.md`

---

## Executive Summary

This specification defines how **hypergraph knowledge structures** can be automatically transformed into **immersive XRAI worlds** - creating navigable, interactive 3D environments from abstract data relationships.

**Core Principle**: "From simple rules comes infinite complexity" (Wolfram)

**Value Proposition**: Any knowledge graph (MCP Memory, Neo4j, Obsidian vault, GitHub repo network) can generate a corresponding XRAI world that users can explore in VR/AR/browser.

---

## Foundational Concepts

### 1. Hypergraph → Spatial Mapping

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    HYPERGRAPH → XRAI WORLD PIPELINE                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  HYPERGRAPH STRUCTURE          →        XRAI SPATIAL STRUCTURE               │
│  ─────────────────────                  ────────────────────────             │
│                                                                              │
│  Nodes (Entities)              →        Spatial Objects/Rooms                │
│  Edges (Relations)             →        Portals/Corridors/Connections        │
│  Hyperedges (N-ary)            →        Gathering Spaces/Hubs                │
│  Node Properties               →        Visual Appearance/Behavior           │
│  Edge Weights                  →        Distance/Visibility                  │
│  Clusters/Communities          →        Districts/Regions/Biomes             │
│  Hierarchy Depth               →        Elevation/Scale Levels               │
│  Temporal Data                 →        Day/Night/Season Cycles              │
│  Provenance/Trust              →        Material Quality/Luminosity          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2. Semantic Zoom Correlation

| Hypergraph Level | XRAI World Scale | Visual Representation |
|------------------|------------------|----------------------|
| Universe (all graphs) | Galaxy View | Floating constellation of worlds |
| Domain (graph subset) | Planet/Continent | Landmass with distinct biome |
| Cluster (community) | City/District | Architecture style group |
| Hub Node | Building/Plaza | Central gathering space |
| Regular Node | Room/Object | Interactive element |
| Leaf Node | Detail/Artifact | Inspectable item |
| Observation | Particle/Aura | Ambient information |

---

## XRAI Format Extensions for Hypergraph Data

### 3. Proposed Section Types

```typescript
interface XRAIHypergraphExtension {
  // Core XRAI sections (11 existing)
  geometry: MeshData | GaussianSplats | NeRFData;
  materials: PBRMaterial[];
  animations: AnimationClip[];
  vfxSystems: VFXGraphAsset[];
  onnxModels: ONNXNetwork[];
  audio: SpatialAudioClip[];
  interactions: InteractionDefinition[];
  behaviors: BehaviorTree[];
  physics: PhysicsData;
  streaming: StreamingManifest;
  metadata: XRAIMetadata;

  // NEW: Hypergraph-Specific Sections
  sourceGraph: {
    format: 'mcp-memory' | 'neo4j' | 'obsidian' | 'json-ld' | 'rdf';
    schema: GraphSchema;
    snapshot: GraphSnapshot;      // Embedded or URI reference
    lastSync: Date;
    provenance: ProvenanceData;
  };

  spatialMapping: {
    nodeLayout: NodeLayoutAlgorithm;
    edgeRouting: EdgeRoutingStrategy;
    clusterArrangement: ClusterAlgorithm;
    scaleFactors: ScaleMapping;
  };

  generativeRules: {
    nodeToGeometry: GenerativeRule[];    // Node type → 3D object
    edgeToConnection: GenerativeRule[];  // Edge type → corridor/portal
    propertyToAppearance: GenerativeRule[]; // Property → material/scale
    aiEnhancements: ONNXRuleSet[];       // AI-driven generation
  };

  dynamicBehaviors: {
    graphUpdates: 'live' | 'periodic' | 'static';
    syncInterval?: number;              // ms
    animateChanges: boolean;
    notifyOnChange: NotificationRule[];
  };
}
```

### 4. Generative Rule System

```typescript
interface GenerativeRule {
  id: string;
  name: string;
  description: string;

  // Pattern matching
  match: {
    nodeTypes?: string[];              // e.g., ['repository', 'person']
    edgeTypes?: string[];              // e.g., ['uses', 'created_by']
    propertyConditions?: PropertyCondition[];
  };

  // Output generation
  generate: {
    type: 'mesh' | 'splats' | 'procedural' | 'prefab' | 'ai-generated';
    template?: string;                 // Prefab or model reference
    procedural?: ProceduralParameters; // Noise, L-systems, etc.
    aiModel?: string;                  // ONNX model for AI generation
    modifiers: Modifier[];             // Scale, color, effects
  };

  // Position/orientation
  placement: {
    algorithm: 'force-directed' | 'hierarchical' | 'geographic' | 'semantic';
    constraints: PlacementConstraint[];
  };
}

// Example Rules
const EXAMPLE_RULES: GenerativeRule[] = [
  {
    id: 'repo-to-building',
    name: 'Repository as Building',
    description: 'Each GitHub repository becomes a building',
    match: { nodeTypes: ['repository'] },
    generate: {
      type: 'procedural',
      procedural: {
        algorithm: 'building-generator',
        params: {
          heightFactor: 'node.stars / 1000',      // Stars → height
          widthFactor: 'node.files / 100',         // Files → footprint
          styleSeed: 'node.category',              // Category → architecture
          colorPalette: 'CATEGORY_COLORS[node.category]'
        }
      }
    },
    placement: {
      algorithm: 'force-directed',
      constraints: [
        { type: 'minDistance', value: 50 },
        { type: 'clusterByCategory', enabled: true }
      ]
    }
  },
  {
    id: 'person-to-avatar',
    name: 'Person as Avatar/Statue',
    description: 'Contributors become statue or NPC',
    match: { nodeTypes: ['person'] },
    generate: {
      type: 'ai-generated',
      aiModel: 'text-to-3d-avatar.onnx',
      modifiers: [
        { type: 'scale', value: 'log10(node.commits + 1) * 0.5' }
      ]
    },
    placement: {
      algorithm: 'semantic',
      constraints: [
        { type: 'nearRelatedNodes', radius: 20 }
      ]
    }
  },
  {
    id: 'uses-to-bridge',
    name: 'Dependency as Bridge',
    description: 'Uses relationship becomes walkable bridge',
    match: { edgeTypes: ['uses', 'depends_on'] },
    generate: {
      type: 'procedural',
      procedural: {
        algorithm: 'bridge-generator',
        params: {
          style: 'node.from.category',
          width: 'edge.strength * 5',
          material: 'glass'
        }
      }
    }
  },
  {
    id: 'trust-to-luminosity',
    name: 'Trust Score as Light',
    description: 'High trust entities glow, low trust are dim',
    match: { propertyConditions: [{ path: 'provenance.trust', exists: true }] },
    generate: {
      type: 'modifier',
      modifiers: [
        { type: 'emission', value: 'node.provenance.trust.computedScore' },
        { type: 'particleAura', enabled: 'node.provenance.trust.computedScore > 0.8' }
      ]
    }
  }
];
```

---

## World Generation Pipeline

### 5. Pipeline Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      HYPERGRAPH → XRAI GENERATION PIPELINE                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐     │
│  │   SOURCE    │   │   LAYOUT    │   │  GEOMETRY   │   │   OUTPUT    │     │
│  │   GRAPH     │ → │   ENGINE    │ → │  GENERATOR  │ → │   XRAI      │     │
│  │             │   │             │   │             │   │   FILE      │     │
│  └─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘     │
│        ↓                 ↓                 ↓                 ↓              │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐   ┌─────────────┐     │
│  │ MCP Memory  │   │ Force-Dir   │   │ Procedural  │   │ Binary XRAI │     │
│  │ Neo4j       │   │ Hierarchical│   │ Splats      │   │ Streaming   │     │
│  │ Obsidian    │   │ Geographic  │   │ AI-Generated│   │ Fallback    │     │
│  │ JSON-LD     │   │ Semantic    │   │ Prefab      │   │   GLTF      │     │
│  └─────────────┘   └─────────────┘   └─────────────┘   └─────────────┘     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 6. Layout Algorithms

```typescript
interface LayoutEngine {
  // Force-Directed (default for general graphs)
  forceDirected: {
    algorithm: 'fr' | 'd3-force' | 'cola' | 'ngraph';
    iterations: number;
    gravity: number;
    repulsion: number;
    linkDistance: (edge: Edge) => number;
    is3D: boolean;               // 2D layout → extrude, or native 3D
  };

  // Hierarchical (for trees, DAGs)
  hierarchical: {
    algorithm: 'sugiyama' | 'tidier' | 'radial';
    levelHeight: number;
    nodeSpacing: number;
    direction: 'TD' | 'BU' | 'LR' | 'RL';
  };

  // Geographic (for location-aware data)
  geographic: {
    projection: 'mercator' | 'orthographic' | 'custom';
    terrainGeneration: 'height-from-data' | 'flat' | 'procedural';
    scaleKm: number;
  };

  // Semantic (AI-driven placement)
  semantic: {
    embeddingModel: string;      // ONNX model for semantic embeddings
    dimensionReduction: 'pca' | 'tsne' | 'umap';
    clusterFirst: boolean;
  };
}
```

### 7. Geometry Generation

```typescript
interface GeometryGenerator {
  // Procedural Building Generation
  buildingGenerator: {
    styles: {
      'xr-tools': { base: 'modern-glass', roof: 'dome' },
      'vfx-effects': { base: 'crystalline', roof: 'spire' },
      'body-tracking': { base: 'organic', roof: 'flowing' },
      'depth-sensing': { base: 'brutalist', roof: 'antenna' },
      'gaussian-splatting': { base: 'cloud-form', roof: 'diffuse' },
      'multiplayer': { base: 'hub-radial', roof: 'beacon' },
      'ml-inference': { base: 'server-rack', roof: 'neural-net' }
    };
    heightFromProperty: 'stars' | 'commits' | 'files' | 'custom';
    lodLevels: number;
  };

  // Splat-Based Generation (for organic/natural)
  splatGenerator: {
    density: number;              // Splats per unit volume
    colorFromProperty: string;    // Property → color mapping
    sizeVariance: number;
    noiseOctaves: number;
  };

  // AI-Generated (ONNX models)
  aiGenerator: {
    textTo3D: string;            // Model for text → 3D
    imageToSplats: string;       // Model for image → Gaussian splats
    styleTransfer: string;       // Apply style to generated geometry
  };

  // Connection Generation (edges → corridors/portals)
  connectionGenerator: {
    bridgeStyles: Record<string, BridgeParams>;
    portalEffects: Record<string, VFXParams>;
    minWidth: number;
    curveSmoothing: number;
  };
}
```

---

## Use Case: Repository Knowledge Graph World

### 8. Example: GitHub Repo Explorer

**Input**: MCP Memory graph with 24 entities (repos, people, projects)

**Generated World**:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         "UNITY-XR-AI KNOWLEDGE CITY"                         │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│    🏛️ VFX District          🏗️ Body Tracking Quarter                        │
│    ├── SplatVFX Tower        ├── MediaPipe Hub                              │
│    ├── LaspVFX Building      ├── uLipSync Studio                            │
│    └── Kino Research Lab     └── ARKitFaceTracking Lab                      │
│                                                                              │
│    🔮 Neural Rendering Zone   📡 Streaming Sector                            │
│    ├── 3DGS Cloud Complex     ├── Rcam4 Broadcast Tower                     │
│    └── NeRF Research Tower    └── UnityRenderStreaming Hub                  │
│                                                                              │
│    👤 keijiro Plaza (Central Hub)                                           │
│    ├── 10+ building connections (bridges)                                   │
│    └── Avatar statue with contribution aura                                 │
│                                                                              │
│    🌐 Portals Project (User's Workspace)                                    │
│    ├── Elevated position (workspace bonus)                                  │
│    ├── Bright glow (high activity)                                          │
│    └── Multiple incoming bridges (dependencies)                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Generative Rules Applied**:

| Rule | Input | Output |
|------|-------|--------|
| repo-to-building | SplatVFX node | Tower (12 stars → height) |
| category-to-district | 'vfx-effects' | Red-tinted district |
| person-to-avatar | keijiro entity | Central plaza statue |
| uses-to-bridge | SplatVFX→VFXGraph | Glass bridge |
| trust-to-luminosity | provenance.trust=0.85 | Soft glow effect |

---

## Cross-Platform Runtime

### 9. Platform Adapters

```typescript
interface XRAIPlatformAdapter {
  // Unity (primary development)
  unity: {
    version: '>= 6000.1';
    renderPipeline: 'URP' | 'HDRP';
    xrPlatforms: ['Quest', 'visionOS', 'PC VR'];
    vfxGraphSupport: boolean;
    gaussianSplatSupport: 'SplatVFX' | 'UnityGaussianSplatting';
  };

  // WebXR (browser)
  webxr: {
    engine: 'three.js' | 'babylon.js' | 'aframe';
    splatRenderer: 'gsplat.js' | 'splat' | 'fallback-mesh';
    progressiveLoading: boolean;
    maxSplats: number;         // Browser memory limit
  };

  // visionOS (Apple native)
  visionos: {
    framework: 'RealityKit' | 'Unity';
    sharedSpaceSupport: boolean;
    immersiveSpaceSupport: boolean;
    usdzFallback: boolean;     // Convert to USDZ if needed
  };

  // Fallback (all platforms)
  fallback: {
    meshLOD: number;           // LOD level for mesh fallback
    textureResolution: number;
    animationSupport: boolean;
    format: 'gltf' | 'glb';
  };
}
```

### 10. Streaming Strategy

```typescript
interface StreamingManifest {
  // Level-of-Detail streaming
  lodLevels: {
    level: number;
    distance: number;          // Load when camera within this distance
    geometry: 'splats' | 'mesh' | 'billboard';
    resolution: number;
  }[];

  // Spatial partitioning
  chunks: {
    id: string;
    bounds: BoundingBox;
    nodeIds: string[];         // Which graph nodes in this chunk
    fileUrl: string;           // Chunk file URL
    priority: number;
  }[];

  // Predictive loading
  prediction: {
    algorithm: 'viewport-cone' | 'navigation-path' | 'popularity';
    preloadDistance: number;
    cacheSize: number;
  };
}
```

---

## DNA-Like Properties

### 11. Sparse Instructions, Rich Output

Following the XRAI philosophy of "DNA-like sparse instructions":

```typescript
// A complete world definition in ~500 bytes
interface XRAIWorldSeed {
  schema: string;              // URI to graph schema
  graph: string;               // URI to graph data (or inline)
  rules: string;               // URI to generative rules
  style: string;               // URI to style preset

  // Override defaults
  overrides?: {
    layoutAlgorithm?: string;
    colorPalette?: string;
    scale?: number;
  };
}

// Example: Entire GitHub repo world in a tweet
const seed: XRAIWorldSeed = {
  schema: 'xrai://schemas/repo-graph/1.0',
  graph: 'mcp://memory/entities?type=repository',
  rules: 'xrai://rules/repo-city/default',
  style: 'xrai://styles/cyberpunk-neon'
};

// This seed expands to:
// - Layout calculated from graph structure
// - Geometry generated from node properties
// - Materials from category colors
// - Behaviors from relationship types
// - Full XRAI world file (potentially gigabytes)
```

---

## Integration with Existing Systems

### 12. MCP Memory → XRAI World

```typescript
async function generateWorldFromMCPMemory(): Promise<XRAIWorld> {
  // 1. Read current graph state
  const graph = await mcp__memory__read_graph();

  // 2. Apply layout algorithm
  const layout = calculateLayout(graph.entities, graph.relations, {
    algorithm: 'force-directed',
    dimensions: 3
  });

  // 3. Generate geometry for each node
  const geometries = await Promise.all(
    graph.entities.map(entity =>
      generateGeometry(entity, layout.positions[entity.name])
    )
  );

  // 4. Generate connections for each edge
  const connections = await Promise.all(
    graph.relations.map(relation =>
      generateConnection(relation, layout)
    )
  );

  // 5. Package into XRAI format
  return packXRAI({
    geometry: mergeGeometries(geometries),
    connections,
    sourceGraph: graph,
    spatialMapping: layout,
    metadata: {
      generated: new Date(),
      generator: 'hypergraph-to-xrai/1.0',
      provenance: extractProvenance(graph)
    }
  });
}
```

### 13. Live Updates

```typescript
interface LiveWorldSync {
  // Watch for graph changes
  watch: {
    source: 'mcp-memory' | 'neo4j' | 'obsidian';
    pollInterval: number;      // ms (0 for websocket)
    diffDetection: 'hash' | 'version' | 'timestamp';
  };

  // Animation on change
  animations: {
    nodeAdded: 'grow-in' | 'teleport' | 'fade';
    nodeRemoved: 'shrink-out' | 'explode' | 'fade';
    edgeAdded: 'bridge-construct' | 'portal-open';
    edgeRemoved: 'bridge-collapse' | 'portal-close';
    propertyChanged: 'pulse' | 'morph' | 'recolor';
  };

  // Notification system
  notifications: {
    visual: 'particle-burst' | 'beacon' | 'overlay';
    audio: 'chime' | 'voice' | 'none';
    haptic: boolean;           // For VR controllers
  };
}
```

---

## Next Steps

### Implementation Roadmap

**Phase 1 (2026 Q1)**: Proof of Concept
- [ ] MCP Memory → basic layout (2D force-directed)
- [ ] Simple procedural buildings from node properties
- [ ] Three.js web viewer
- [ ] Export to GLTF fallback

**Phase 2 (2026 Q2)**: Full Pipeline
- [ ] 3D layout algorithms
- [ ] Gaussian splat generation
- [ ] Unity runtime loader
- [ ] Quest 3 testing

**Phase 3 (2026 Q3)**: AI Enhancement
- [ ] ONNX text-to-3D integration
- [ ] Semantic layout using embeddings
- [ ] Style transfer for visual coherence

**Phase 4 (2026 Q4)**: Production
- [ ] Live sync with graph updates
- [ ] Multi-user exploration (Normcore)
- [ ] visionOS shared space
- [ ] XRAI file format finalization

---

## References

- `_JT_PRIORITIES.md`: XRAI/VNMF research tracks
- `_WEB_INTEROPERABILITY_STANDARDS.md`: Format comparison
- `_REPO_GRAPH_SCHEMA.md`: Repository knowledge graph schema
- Wolfram Physics Project: Hypergraph universe model
- Tim Berners-Lee: Linked Data principles
- Ted Nelson: Project Xanadu (transclusion, provenance)

---

**This specification enables:**
- Automatic world generation from any knowledge graph
- Cross-platform XRAI worlds (Unity, WebXR, visionOS)
- DNA-like sparse representation (seed → full world)
- Live synchronization with graph updates
- Provenance-aware visualization (trust = luminosity)
