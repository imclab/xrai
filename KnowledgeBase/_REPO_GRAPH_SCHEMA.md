# Repository Knowledge Graph Schema

**Version**: 1.0
**Created**: 2026-01-10
**Purpose**: Multi-table schema for indexing GitHub repositories as a queryable knowledge graph

---

## Schema Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         REPOSITORY KNOWLEDGE GRAPH                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  LEVEL 1: REPOSITORY (Primary Table)                                    │
│  ├── Identity: name, url, owner, description                            │
│  ├── Category: domain, techniques[], tags[]                             │
│  ├── Platform: iOS, Android, WebGL, Quest, visionOS, Windows, macOS     │
│  └── Status: active, archived, workspace, stars, lastUpdate             │
│                                                                          │
│  LEVEL 2: CODEBASE (Secondary Table)                                    │
│  ├── Structure: files[], folders[], entryPoints[]                       │
│  ├── Dependencies: packages[], versions[], unityVersion                 │
│  ├── Languages: C#, Swift, JS, Python, HLSL                             │
│  └── Build: renderPipeline, targetFramework, buildSystem                │
│                                                                          │
│  LEVEL 3: SOCIAL (Tertiary Table)                                       │
│  ├── Owner: profile, followers, otherRepos[], expertise                 │
│  ├── Contributors: [name, commits, lastActive, filesOwned[]]            │
│  ├── Activity: commits30d, issues, PRs, releases, trend                 │
│  └── Network: inboundDeps[], outboundDeps[], forks[], inspirations[]    │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Level 1: Repository (Primary Table)

### Schema Definition

```typescript
interface Repository {
  // Identity
  id: string;                    // Unique ID (owner/repo)
  name: string;                  // Repository name
  url: string;                   // GitHub URL
  owner: string;                 // Owner username/org
  description: string;           // Short description

  // Classification
  category: Category;            // Primary category
  subcategory?: string;          // More specific classification
  techniques: string[];          // Technical approaches used
  tags: string[];                // Searchable tags

  // Platform Support
  platforms: PlatformMatrix;     // Detailed platform compatibility

  // Status
  status: 'active' | 'archived' | 'workspace' | 'reference';
  stars: number;
  forks: number;
  lastUpdate: Date;
  lastCommit: Date;

  // Relations
  relatedRepos: string[];        // Similar/related repos
  parentRepo?: string;           // Fork source
  childRepos?: string[];         // Forks of this repo
}

type Category =
  | 'xr-tools'           // XR development tools/frameworks
  | 'vfx-effects'        // VFX Graph, particles, shaders
  | 'body-tracking'      // Human/body/face tracking
  | 'depth-sensing'      // LiDAR, depth cameras
  | 'audio-reactive'     // Audio visualization
  | 'gaussian-splatting' // Neural rendering
  | 'multiplayer'        // Networking, multiplayer
  | 'ml-inference'       // ML/AI inference
  | 'web-ar'             // WebGL/WebXR
  | 'streaming'          // Video/data streaming
  | 'utilities'          // Tools, helpers
  | 'samples'            // Official samples
  | 'research';          // Experimental/academic
```

### Platform Compatibility Matrix

```typescript
interface PlatformMatrix {
  iOS: PlatformSupport;
  Android: PlatformSupport;
  WebGL: PlatformSupport;
  Quest: PlatformSupport;
  visionOS: PlatformSupport;
  Windows: PlatformSupport;
  macOS: PlatformSupport;
  Linux: PlatformSupport;
  HoloLens: PlatformSupport;
}

interface PlatformSupport {
  supported: boolean;
  verified: boolean;           // Actually tested
  notes?: string;              // Limitations, requirements
  minVersion?: string;         // Minimum OS/SDK version
  features?: string[];         // Platform-specific features used
}
```

### Platform Quick Reference

| Platform | Code | Common Requirements |
|----------|------|---------------------|
| iOS | `ios` | ARKit, Metal, iOS 14+ |
| Android | `android` | ARCore, OpenGL ES 3.0 |
| WebGL | `webgl` | WebGL 2.0, no native plugins |
| Quest | `quest` | OpenXR, Quest runtime |
| visionOS | `visionos` | visionOS 1.0+, RealityKit |
| Windows | `win` | DirectX 11+, OpenXR |
| macOS | `macos` | Metal, macOS 11+ |
| HoloLens | `hololens` | MRTK, OpenXR |

---

## Level 2: Codebase (Secondary Table)

### Schema Definition

```typescript
interface Codebase {
  repoId: string;                // Links to Repository

  // Structure
  structure: {
    totalFiles: number;
    totalFolders: number;
    keyFiles: KeyFile[];         // Important entry points
    folderTree: FolderNode;      // Simplified structure
  };

  // Dependencies
  dependencies: {
    packages: Package[];         // npm, UPM, NuGet, pip
    unityVersion?: string;       // e.g., "6000.1.2f1"
    renderPipeline?: 'URP' | 'HDRP' | 'Built-in';
    xrPlugin?: string;           // e.g., "ARFoundation 6.2"
  };

  // Languages
  languages: {
    primary: string;             // Main language
    breakdown: Record<string, number>; // Language percentages
  };

  // Build
  build: {
    system: 'Unity' | 'Xcode' | 'Gradle' | 'npm' | 'CMake';
    entryPoint?: string;         // Main scene/file
    buildTargets: string[];      // Configured platforms
  };
}

interface KeyFile {
  path: string;
  type: 'entry' | 'config' | 'core' | 'shader' | 'asset';
  description: string;
}

interface Package {
  name: string;
  version: string;
  source: 'upm' | 'npm' | 'nuget' | 'pip' | 'git';
  required: boolean;
}
```

### Example Codebase Entry

```json
{
  "repoId": "keijiro/SplatVFX",
  "structure": {
    "totalFiles": 42,
    "totalFolders": 8,
    "keyFiles": [
      { "path": "Assets/Scripts/SplatRenderer.cs", "type": "core", "description": "Main splat rendering" },
      { "path": "Assets/Shaders/Splat.shader", "type": "shader", "description": "GPU splat shader" }
    ]
  },
  "dependencies": {
    "packages": [
      { "name": "com.unity.visualeffectgraph", "version": "14.0.8", "source": "upm", "required": true },
      { "name": "com.unity.render-pipelines.high-definition", "version": "14.0.8", "source": "upm", "required": true }
    ],
    "unityVersion": "2022.3.0f1",
    "renderPipeline": "HDRP"
  },
  "languages": {
    "primary": "C#",
    "breakdown": { "C#": 70, "HLSL": 25, "ShaderLab": 5 }
  }
}
```

---

## Level 3: Social (Tertiary Table)

### Schema Definition

```typescript
interface Social {
  repoId: string;                // Links to Repository

  // Owner Profile
  owner: {
    username: string;
    displayName?: string;
    type: 'user' | 'organization';
    followers: number;
    expertise: string[];         // Known for what
    otherRepos: string[];        // Other notable repos
    links: {
      website?: string;
      twitter?: string;
      linkedin?: string;
    };
  };

  // Contributors
  contributors: Contributor[];

  // Activity Metrics
  activity: {
    commits30d: number;
    commits90d: number;
    openIssues: number;
    closedIssues30d: number;
    openPRs: number;
    mergedPRs30d: number;
    releases: number;
    lastRelease?: Date;
    trend: 'rising' | 'stable' | 'declining' | 'archived';
  };

  // Dependency Network
  network: {
    dependsOn: string[];         // This repo uses these
    usedBy: string[];            // These repos use this
    forkCount: number;
    notableForks: string[];      // Significant forks
    inspirations: string[];      // Inspired by (manual)
    inspires: string[];          // Inspired these (manual)
  };
}

interface Contributor {
  username: string;
  commits: number;
  additions: number;
  deletions: number;
  lastActive: Date;
  filesOwned: string[];          // Primary maintainer of these
  expertise: string[];           // What they contribute
}
```

### Hub Developers (Key Profiles)

| Username | Expertise | Notable Repos | Followers |
|----------|-----------|---------------|-----------|
| **keijiro** | VFX, ML, Unity effects | SplatVFX, Minis, Lasp, Kino | 10k+ |
| **dilmerv** | XR tutorials, VisionOS | XRInteractionDemo, VisionOS samples | 5k+ |
| **hecomi** | Face tracking, OSC, audio | uLipSync, uOSC, uARKitFaceMesh | 2k+ |
| **asus4** | ARKit, ONNX, streaming | ARKitStreamer, onnxruntime-unity | 1k+ |
| **Unity-Technologies** | Official samples | EntityComponentSystemSamples, arfoundation-samples | 10k+ |

---

## Category Definitions

### Primary Categories

| Category | Description | Example Repos |
|----------|-------------|---------------|
| **xr-tools** | XR development frameworks, SDKs | XRI-Examples, MRTK, HoloKit SDK |
| **vfx-effects** | VFX Graph, shaders, particles | SplatVFX, Kino, VFXGraphSandbox |
| **body-tracking** | Human body, face, hand tracking | MediaPipeUnityPlugin, uLipSync |
| **depth-sensing** | LiDAR, depth cameras, point clouds | Rcam4, arfoundation-densepointcloud |
| **audio-reactive** | Audio visualization, FFT | LaspVfx, Reaktion |
| **gaussian-splatting** | Neural radiance, 3DGS | UnityGaussianSplatting, supersplat |
| **multiplayer** | Networking, co-located AR | Normcore-Samples, netcode-transport |
| **ml-inference** | Sentis, ONNX, Barracuda | sentis-samples, onnxruntime-unity |
| **web-ar** | WebGL, WebXR, Three.js | AR.js, react-three-fiber |
| **streaming** | Video, data, WebRTC | UnityRenderStreaming, record3d |
| **utilities** | Tools, helpers, converters | UnityGLTF, unity-figma-importer |
| **samples** | Official reference implementations | arfoundation-samples, DOTS samples |
| **research** | Experimental, academic | 3dgrut, RaySplatting |

### Technique Tags

```typescript
const TECHNIQUES = [
  // Tracking
  'arkit-face', 'arkit-body', 'arkit-hand', 'mediapipe', 'openpose',
  'lidar-depth', 'stereo-depth', 'scene-mesh', 'occlusion',

  // Rendering
  'vfx-graph', 'shader-graph', 'compute-shader', 'gpu-instancing',
  'neural-rendering', 'gaussian-splatting', 'point-cloud',

  // Performance
  'dots-ecs', 'burst-compiler', 'job-system', 'object-pooling',

  // Networking
  'webrtc', 'websocket', 'multipeer', 'netcode', 'normcore',

  // ML
  'sentis', 'barracuda', 'onnx-runtime', 'tflite', 'coreml',

  // Platform
  'urp', 'hdrp', 'openxr', 'arfoundation', 'visionos', 'webxr'
];
```

---

## Graph Relationships

### Relationship Types

| Relation | From | To | Example |
|----------|------|----|---------|
| `uses` | Repo | Repo | Paint-AR uses SplatVFX |
| `extends` | Repo | Repo | Open-Brush extends Tilt-Brush |
| `forks` | Repo | Repo | needle-UnityGLTF forks KhronosGroup-UnityGLTF |
| `created_by` | Repo | Person | SplatVFX created_by keijiro |
| `contributes_to` | Person | Repo | asus4 contributes_to ARKitStreamer |
| `depends_on` | Repo | Package | Rcam4 depends_on VFX Graph |
| `supports` | Repo | Platform | HoloKit-SDK supports iOS |
| `implements` | Repo | Technique | MetavidoVFX implements lidar-depth |
| `inspired_by` | Repo | Repo | Paint-AR inspired_by Tilt-Brush |
| `same_author` | Repo | Repo | Lasp same_author LaspVfx |

---

## Query Examples

### Find iOS-compatible VFX repos
```
nodes WHERE category = 'vfx-effects' AND platforms.iOS.supported = true
```

### Find all Keijiro repos using VFX Graph
```
nodes WHERE owner = 'keijiro' AND techniques CONTAINS 'vfx-graph'
```

### Find depth sensing projects with LiDAR
```
nodes WHERE (category = 'depth-sensing' OR techniques CONTAINS 'lidar-depth')
  AND platforms.iOS.supported = true
```

### Find most active body tracking repos
```
nodes WHERE category = 'body-tracking'
  ORDER BY activity.commits30d DESC
  LIMIT 10
```

### Find repos that depend on AR Foundation
```
nodes WHERE dependencies.packages CONTAINS { name: 'com.unity.xr.arfoundation' }
```

---

## Dashboard Integration

### Node Colors by Category

```javascript
const CATEGORY_COLORS = {
  'xr-tools': '#00d4ff',        // Cyan
  'vfx-effects': '#ff6b6b',     // Red
  'body-tracking': '#fd79a8',   // Pink
  'depth-sensing': '#a29bfe',   // Purple
  'audio-reactive': '#ffe66d',  // Yellow
  'gaussian-splatting': '#00ff88', // Green
  'multiplayer': '#74b9ff',     // Light blue
  'ml-inference': '#e17055',    // Orange
  'web-ar': '#00cec9',          // Teal
  'streaming': '#6c5ce7',       // Indigo
  'utilities': '#b2bec3',       // Gray
  'samples': '#55efc4',         // Mint
  'research': '#fdcb6e'         // Gold
};
```

### Node Size by Importance

```javascript
function getNodeSize(repo) {
  let score = 0;
  score += Math.log10(repo.stars + 1) * 5;        // Stars (log scale)
  score += repo.status === 'workspace' ? 10 : 0;  // Workspace bonus
  score += repo.activity.trend === 'rising' ? 5 : 0;
  return Math.min(50, Math.max(15, score));
}
```

---

## Implementation Notes

1. **MCP Memory Integration**: Each hub repo becomes an entity with observations for key metadata
2. **Dashboard Extension**: Add repo nodes to existing knowledge graph with category colors
3. **Search Enhancement**: Enable filtering by platform, category, technique, owner
4. **Lazy Loading**: Load codebase/social data on-demand when node is selected

---

## Level 4: Provenance (Universal Tracking Layer)

### Schema Definition

```typescript
interface Provenance {
  entityId: string;                // Links to any entity (Repo, Person, Concept)
  entityType: EntityType;          // Type discrimination

  // Temporal Tracking (File System Inspired)
  timestamps: {
    created: Date;                 // First addition to knowledge base
    modified: Date;                // Last update to any field
    discovered: Date;              // When crawler/agent found it
    accessed: Date;                // Last read/query
    verified: Date;                // Last human verification
  };

  // Authorship Chain
  authorship: {
    originalCreator: string;       // Who created the original (e.g., keijiro)
    addedBy: AuthorRecord;         // Who added to knowledge base
    contributors: AuthorRecord[];  // Who modified knowledge base entry
    verifiedBy?: AuthorRecord;     // Human verification
  };

  // Source Provenance
  sources: {
    primary: SourceRecord;         // Main source (e.g., GitHub URL)
    secondary: SourceRecord[];     // Supporting sources
    citations: CitationRecord[];   // Academic/formal citations
  };

  // Link Graph (Forward/Back Links)
  links: {
    forward: LinkRecord[];         // What this entity references
    backward: LinkRecord[];        // What references this entity
    bidirectional: LinkRecord[];   // Mutual references
  };

  // Version History
  history: VersionRecord[];

  // Trust Score
  trust: TrustMetrics;
}

// Supporting Types
interface AuthorRecord {
  id: string;                      // Agent ID or username
  type: 'human' | 'ai-agent' | 'crawler' | 'import';
  name: string;
  timestamp: Date;
  context?: string;                // Why they made this change
}

interface SourceRecord {
  url: string;                     // Primary URL
  type: 'github' | 'web' | 'file' | 'api' | 'manual' | 'ai-generated';
  fetchedAt: Date;
  lastVerified?: Date;
  status: 'active' | 'dead' | 'moved' | 'unknown';
  archive?: string;                // Archive.org backup URL
}

interface LinkRecord {
  targetId: string;                // Entity being linked to
  targetType: EntityType;
  relation: RelationType;          // Type of relationship
  strength: number;                // 0-1 confidence/relevance
  createdAt: Date;
  createdBy: string;
  context?: string;                // Why this link exists
}

interface VersionRecord {
  version: number;
  timestamp: Date;
  author: AuthorRecord;
  changes: string[];               // Summary of changes
  snapshot?: object;               // Full state at this version
  diff?: object;                   // Delta from previous
}

interface TrustMetrics {
  sourceReliability: number;       // 0-1 based on source type
  verificationLevel: 'none' | 'auto' | 'human' | 'expert';
  citationCount: number;           // How many other entities cite this
  lastVerifiedDaysAgo: number;
  decayFactor: number;             // Trust decay over time
  computedScore: number;           // 0-1 overall trust
}

interface CitationRecord {
  format: 'bibtex' | 'apa' | 'url' | 'doi';
  value: string;
  accessedAt: Date;
}

type EntityType =
  | 'repository'
  | 'person'
  | 'concept'
  | 'technique'
  | 'platform'
  | 'package'
  | 'file'
  | 'snippet';

type RelationType =
  | 'uses'           // A uses B as dependency
  | 'extends'        // A extends/forks B
  | 'created_by'     // A created by B (person)
  | 'references'     // A mentions/links to B
  | 'implements'     // A implements B (technique)
  | 'related_to'     // Semantic similarity
  | 'derived_from'   // A derived knowledge from B
  | 'supersedes'     // A replaces B (newer version)
  | 'conflicts_with' // A conflicts with B
  | 'cites';         // Academic citation
```

### Trust Score Calculation

```javascript
function calculateTrustScore(provenance) {
  const weights = {
    sourceReliability: 0.3,
    verificationLevel: 0.25,
    citationPopularity: 0.2,
    freshness: 0.15,
    linkDensity: 0.1
  };

  const sourceScores = {
    'github': 0.9,
    'web': 0.6,
    'api': 0.8,
    'manual': 0.7,
    'ai-generated': 0.5,
    'file': 0.4
  };

  const verificationScores = {
    'expert': 1.0,
    'human': 0.8,
    'auto': 0.5,
    'none': 0.2
  };

  const daysSinceVerified = provenance.trust.lastVerifiedDaysAgo;
  const freshnessScore = Math.max(0, 1 - (daysSinceVerified / 365));

  const citationScore = Math.min(1, Math.log10(provenance.trust.citationCount + 1) / 2);

  const linkCount = provenance.links.forward.length + provenance.links.backward.length;
  const linkScore = Math.min(1, linkCount / 20);

  return (
    weights.sourceReliability * sourceScores[provenance.sources.primary.type] +
    weights.verificationLevel * verificationScores[provenance.trust.verificationLevel] +
    weights.citationPopularity * citationScore +
    weights.freshness * freshnessScore +
    weights.linkDensity * linkScore
  );
}
```

### Provenance Query Examples

```
// Find entities added in last 7 days
nodes WHERE provenance.timestamps.created > NOW() - 7d

// Find stale entries (not verified in 90 days)
nodes WHERE provenance.timestamps.verified < NOW() - 90d
  ORDER BY provenance.trust.computedScore ASC

// Find entities with dead links
nodes WHERE provenance.sources.primary.status = 'dead'

// Find AI-generated content needing verification
nodes WHERE provenance.authorship.addedBy.type = 'ai-agent'
  AND provenance.trust.verificationLevel = 'none'

// Find most-cited entities (hub nodes)
nodes ORDER BY provenance.links.backward.length DESC LIMIT 20

// Find orphan nodes (no incoming links)
nodes WHERE provenance.links.backward.length = 0
  AND provenance.timestamps.created < NOW() - 30d

// Trace knowledge lineage
TRAVERSE FROM 'SplatVFX'
  FOLLOW links.forward WHERE relation IN ['derived_from', 'uses', 'extends']
  DEPTH 5
```

### Provenance Dashboard Indicators

```javascript
const TRUST_INDICATORS = {
  high: { color: '#00ff88', icon: '✓', threshold: 0.8 },
  medium: { color: '#ffe66d', icon: '◐', threshold: 0.5 },
  low: { color: '#ff6b6b', icon: '!', threshold: 0.2 },
  unverified: { color: '#b2bec3', icon: '?', threshold: 0 }
};

function getProvenanceBadge(provenance) {
  const score = provenance.trust.computedScore;
  const daysSinceUpdate = (Date.now() - provenance.timestamps.modified) / 86400000;

  return {
    trustLevel: Object.keys(TRUST_INDICATORS).find(
      k => score >= TRUST_INDICATORS[k].threshold
    ),
    stale: daysSinceUpdate > 90,
    hasDeadLinks: provenance.sources.primary.status === 'dead',
    needsVerification: provenance.trust.verificationLevel === 'none',
    citationCount: provenance.links.backward.length
  };
}
```

### MCP Memory Provenance Observations

When storing provenance in MCP Memory, use structured observations:

```
Entity: SplatVFX
Observations:
- "PROVENANCE:created:2026-01-10T12:00:00Z"
- "PROVENANCE:source:github:https://github.com/keijiro/SplatVFX"
- "PROVENANCE:addedBy:ai-agent:claude-code"
- "PROVENANCE:trust:0.85"
- "PROVENANCE:backlinks:5"
- "PROVENANCE:verified:none"
```

---

**This schema enables:**
- Platform compatibility queries (find iOS-ready repos)
- Technique discovery (find all VFX Graph implementations)
- Contributor analysis (find keijiro's most active projects)
- Dependency mapping (what uses AR Foundation?)
- Trend tracking (rising vs declining repos)
- **Provenance tracking** (who added, when, from where)
- **Trust scoring** (reliability of knowledge)
- **Link analysis** (forward/backward references)
- **Staleness detection** (entries needing updates)
- **Lineage tracing** (knowledge derivation chains)
