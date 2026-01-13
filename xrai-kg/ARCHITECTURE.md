# XRAI Knowledge Graph - Modular Architecture

## Design Principles

1. **Separation of Concerns** - Each layer is independent and replaceable
2. **Simple is Better** - Use existing libraries, don't reinvent
3. **Future-Proof** - Standards-based, no vendor lock-in
4. **Fast & Accurate** - Optimized data structures, proven algorithms

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        ADAPTER LAYER                             │
│   VS Code │ Chrome Extension │ Standalone │ Electron │ Web      │
├─────────────────────────────────────────────────────────────────┤
│                      PRESENTATION LAYER                          │
│              UI Components │ Event Handlers │ State              │
├─────────────────────────────────────────────────────────────────┤
│                     VISUALIZATION LAYER                          │
│        ECharts │ Cytoscape.js │ 3D Force Graph │ Mermaid        │
├─────────────────────────────────────────────────────────────────┤
│                          AI LAYER                                │
│           Chat Parser │ NLP │ LLM Hooks │ Suggestions           │
├─────────────────────────────────────────────────────────────────┤
│                        SEARCH LAYER                              │
│              Fuse.js │ Filtering │ Indexing │ Cache             │
├─────────────────────────────────────────────────────────────────┤
│                         DATA LAYER                               │
│         KnowledgeGraph │ Entities │ Relations │ Storage         │
└─────────────────────────────────────────────────────────────────┘
```

---

## Layer Responsibilities

### 1. DATA LAYER (`/src/data/`)
**Purpose**: Pure data management, no logic beyond CRUD

**Status**: ✅ Implemented

**Files**:
- `KnowledgeGraph.js` - Core data structure (✅ includes Entity & Relation)
- `Storage.js` - Persistence interface (localStorage, IndexedDB, file)

**Dependencies**: None (pure JavaScript)

**Interface**:
```javascript
interface DataLayer {
    addEntity(entity): Entity
    addRelation(relation): Relation
    getEntity(id): Entity
    getAllEntities(): Entity[]
    getAllRelations(): Relation[]
    toJSON(): object
    fromJSON(data): void
}
```

---

### 2. SEARCH LAYER (`/src/search/`)
**Purpose**: Fast, accurate search and filtering

**Status**: ✅ Implemented

**Files**:
- `SearchEngine.js` - Wraps Fuse.js with fallback (✅ includes filtering & cache)
- `Index.js` - Indexing strategies

**Dependencies**:
- `fuse.js` (proven fuzzy search library, 25KB)

**Interface**:
```javascript
interface SearchLayer {
    search(query, options): SearchResult[]
    filter(predicate): Entity[]
    suggest(partial): Suggestion[]
    reindex(): void
}
```

---

### 3. AI LAYER (`/src/ai/`)
**Purpose**: Natural language processing and LLM integration

**Files**:
- `ChatParser.js` - Command parsing (✅ implemented)
- `CommandRegistry.js` - Extensible commands
- `LLMAdapter.js` - Hooks for Claude/GPT/Gemini
- `ResponseFormatter.js` - Structured responses

**Dependencies**: None (LLM calls are adapter-provided)

**Interface**:
```javascript
interface AILayer {
    parse(input): ParsedCommand
    execute(command, xrai): Result
    setLLMHandler(handler): void
}
```

**Supported Commands** (ChatParser):

| Command | Pattern | Example |
|---------|---------|---------|
| Search | `search <query>` | `search unity patterns` |
| Add Entity | `add <name> as <type>` | `add ARFoundation as Technology` |
| Add Relation | `relate <A> to <B> [as type]` | `relate Unity to ARFoundation as uses` |
| Filter | `filter [by] <type>` | `filter by Project` |
| Neighbors | `neighbors [of] <name>` | `neighbors of Unity depth 2` |
| Stats | `stats` | `stats` |
| Export | `export [json\|mermaid\|csv]` | `export mermaid` |
| Clear | `clear` | `clear` |
| Help | `help` | `help` |

**Command Aliases**:
- `find`, `query`, `s`, `f` → search
- `create`, `new` → add
- `connect`, `link` → relate
- `show` → filter
- `related`, `connections` → neighbors
- `?` → help

---

### 4. VISUALIZATION LAYER (`/src/viz/`)
**Purpose**: Render graphs and charts

**Status**: ✅ Implemented

**Files**:
- `EChartsRenderer.js` - Apache ECharts/GraphGL implementation (✅)
- `MermaidExporter.js` - Mermaid code generation (✅)
- `index.js` - Re-exports (✅)

**Dependencies** (peer, not bundled):
- `echarts` (Apache ECharts, industry standard)
- `echarts-gl` (WebGL acceleration for large graphs)

**EChartsRenderer Features**:
- Auto-selects `graph` or `graphGL` based on node count
- Performance presets: small (<500), medium (<2K), large (<10K), huge (10K+)
- ForceAtlas2 GPU-accelerated layout
- Community detection (modularity)
- Interactive highlighting (click to focus neighbors)
- Theme support (light/dark)
- Export to PNG/JPEG

**MermaidExporter Features**:
- Flowchart/graph export
- Mindmap export
- Class diagram export
- Type-based shapes and colors
- Relation label arrows

**Interface**:
```javascript
// EChartsRenderer
const renderer = new EChartsRenderer(container, options);
renderer.render(kg);
renderer.focusNode(name);
renderer.filterTypes(['Project', 'Technology']);
renderer.startLayout();  // GraphGL only
renderer.stopLayout();
renderer.toImage({ type: 'png' });
renderer.dispose();

// MermaidExporter
const code = MermaidExporter.toMermaid(kg, {
    diagramType: 'flowchart',  // or 'mindmap', 'classDiagram'
    direction: 'TD',
    showRelationLabels: true
});
```

---

### 5. ADAPTER LAYER (`/src/adapters/`)
**Purpose**: Platform-specific integration

**Files**:
- `BrowserAdapter.js` - Standalone web
- `VSCodeAdapter.js` - VS Code extension
- `ChromeAdapter.js` - Chrome extension
- `MCPAdapter.js` - MCP server integration

**Dependencies**: Platform-specific APIs

**Interface**:
```javascript
interface Adapter {
    init(config): void
    getData(): Promise<Data>
    saveData(data): Promise<void>
    sendMessage(msg): void
    onMessage(handler): void
}
```

---

## File Structure

```
xrai-kg/
├── src/
│   ├── index.js              # ✅ Main entry + XRAI facade
│   │
│   ├── data/                 # DATA LAYER
│   │   └── KnowledgeGraph.js # ✅ Entity, Relation, KnowledgeGraph
│   │
│   ├── search/               # SEARCH LAYER
│   │   └── SearchEngine.js   # ✅ Fuse.js wrapper + fallback + cache
│   │
│   ├── ai/                   # AI LAYER
│   │   └── ChatParser.js     # ✅ Natural language commands
│   │
│   ├── viz/                  # VISUALIZATION LAYER
│   │   ├── index.js          # ✅ Re-exports
│   │   ├── EChartsRenderer.js # ✅ ECharts/GraphGL renderer
│   │   └── MermaidExporter.js # ✅ Mermaid diagram export
│   │
│   └── adapters/             # ADAPTER LAYER (planned)
│       ├── BrowserAdapter.js
│       ├── VSCodeAdapter.js
│       └── ChromeAdapter.js
│
├── dist/                     # Built bundles (after npm run build)
│   ├── xrai-kg.esm.js       # ES Modules
│   ├── xrai-kg.cjs.js       # CommonJS
│   └── xrai-kg.umd.js       # UMD (browser)
│
├── package.json              # ✅ NPM package config
├── ARCHITECTURE.md           # ✅ This file
└── README.md
```

**Implementation Status**:
- ✅ Data Layer (KnowledgeGraph, Entity, Relation)
- ✅ Search Layer (SearchEngine with Fuse.js)
- ✅ AI Layer (ChatParser with commands)
- ✅ Visualization Layer (EChartsRenderer, MermaidExporter)
- ⏳ Adapter Layer (VS Code, Chrome, Browser)

---

## Usage Examples

### Basic (Separate Layers)
```javascript
import { KnowledgeGraph, SearchEngine } from 'xrai-kg';

// Data layer
const kg = new KnowledgeGraph();
kg.bulkAdd(mcpData);  // { entities: [...], relations: [...] }

// Search layer
const search = new SearchEngine(kg);
const results = search.search('unity pattern', { limit: 10 });
// Returns: [{ entity: Entity, score: 0.95 }, ...]

// Type filtering
const projects = search.filterByTypes(['Project']);
const types = search.getTypes();  // [{ name: 'Project', count: 5 }, ...]
```

### XRAI Facade (Recommended)
```javascript
import XRAI from 'xrai-kg';

const xrai = XRAI.create({ data: mcpData });

// Search
const results = xrai.find('ARFoundation');

// Add data
xrai.addEntity({ name: 'MyProject', entityType: 'Project' });
xrai.addRelation('MyProject', 'ARFoundation', 'uses');

// Graph traversal
const neighbors = xrai.getNeighbors('ARFoundation', 2);

// Export
const json = xrai.toJSON();
```

### With AI Chat Commands
```javascript
import XRAI from 'xrai-kg';

const xrai = XRAI.create({ data: mcpData });

// Natural language commands
let result = xrai.run('search unity patterns');
console.log(result.message);  // "Found 5 result(s) for 'unity patterns'"
console.log(result.data);     // [{ entity: Entity, score: 0.9 }, ...]

result = xrai.run('add ARFoundation as Technology');
// "Added entity 'ARFoundation' (Technology)"

result = xrai.run('relate Unity to ARFoundation as uses');
// "Added relation: Unity --[uses]--> ARFoundation"

result = xrai.run('neighbors of Unity');
// "Found 3 neighbor(s) of 'Unity' (depth: 1)"

result = xrai.run('export mermaid');
// Returns Mermaid diagram code

result = xrai.run('stats');
// "Graph: 150 entities, 89 relations"
```

### Using ChatParser Directly
```javascript
import { ChatParser, CommandType } from 'xrai-kg/ai';

const parser = new ChatParser();

// Parse without executing
const cmd = parser.parse('add Unity as Engine');
// { type: 'addEntity', params: { name: 'Unity', entityType: 'Engine' } }

// With LLM fallback for complex queries
parser.setLLMHandler(async (input) => {
    // Call your LLM API here
    return { type: CommandType.SEARCH, params: { query: input } };
});
const result = await parser.parseWithLLM('show me AR-related things');
```

### VS Code Extension (Planned)
```javascript
import { VSCodeAdapter } from 'xrai-kg/adapters';

const adapter = new VSCodeAdapter(vscode.window);
adapter.init({ panel: webviewPanel });

// Adapter handles all VS Code specifics
adapter.onCommand('xrai.search', (query) => {
    const results = xrai.find(query);
    adapter.showResults(results);
});
```

---

## Dependencies (Minimal)

| Library | Purpose | Size | Why |
|---------|---------|------|-----|
| Fuse.js | Fuzzy search | 25KB | Proven, fast, configurable |
| ECharts | Charts/graphs | 1MB (tree-shakeable) | Industry standard, huge feature set |
| Cytoscape | Graph viz | 400KB | Best-in-class graph library |
| Mermaid | Diagram export | 2MB | Standard for docs |

**Total core**: ~50KB (without viz)
**With viz**: ~500KB-1MB (tree-shaken)

---

## Key Design Decisions

1. **Fuse.js over custom search**
   - Battle-tested fuzzy search
   - Configurable scoring
   - Supports Chinese, Japanese, special chars

2. **ECharts over D3/Chart.js**
   - More chart types
   - Better performance for large datasets
   - Built-in themes and animations
   - Declarative config

3. **Layer isolation**
   - Each layer can be replaced independently
   - Easy to test in isolation
   - Different bundles for different platforms

4. **No framework lock-in**
   - Pure JavaScript/TypeScript
   - Works with React, Vue, Svelte, or vanilla
   - Adapters handle framework specifics

---

---

## ECharts GraphGL Reference Patterns

Based on official ECharts-GL examples for large-scale graph visualization.

### NPM Dependency Graph Pattern
Source: [echarts-gl/test/graphGL-npm.html](https://github.com/ecomfe/echarts-gl/blob/master/test/graphGL-npm.html)

```javascript
chart.setOption({
    series: [{
        type: 'graphGL',                    // WebGL-accelerated graph
        nodes: nodes,
        edges: edges,

        // Community detection
        modularity: {
            resolution: 2,
            sort: true
        },

        // Force-directed layout (GPU-accelerated)
        forceAtlas2: {
            steps: 5,                       // Iterations per frame
            maxSteps: 3000,                 // Stop after N iterations
            jitterTolerence: 10,            // Convergence threshold
            edgeWeight: [0.2, 1],           // Edge weight range
            gravity: 5,                     // Center attraction
            edgeWeightInfluence: 0,         // 0 = ignore weights
        },

        // Dynamic symbol size based on value
        symbolSize: function (value) {
            return Math.sqrt(value / 10);
        },

        // Interactive highlighting
        focusNodeAdjacencyOn: 'click',

        // Conditional labels (performance optimization)
        // Show labels only for high-value nodes
        label: {
            show: false,                    // Default hidden
            textStyle: { color: '#fff' }
        },
        emphasis: {
            label: { show: true },
            lineStyle: { opacity: 0.5, width: 1 }
        },

        // Edge styling for large graphs
        lineStyle: {
            color: 'rgba(255,255,255,1)',
            opacity: 0.05,                  // Low opacity for many edges
            width: 1
        }
    }]
});

// Programmatic layout control
chart.dispatchAction({ type: 'graphGLStartLayout' });
chart.dispatchAction({ type: 'graphGLStopLayout' });
```

### Large Internet Graph Pattern
Source: [echarts-gl/test/graphGL-large.html](https://github.com/ecomfe/echarts-gl/blob/master/test/graphGL-large.html)

```javascript
// Data transformation for large datasets
var nodes = graph.nodes.map(function (node) {
    return {
        x: Math.random() * window.innerWidth,  // Random initial position
        y: Math.random() * window.innerHeight,
        symbolSize: node[2],                    // Size from data
        category: node[3],                      // For coloring
        value: 1
    }
});

chart.setOption({
    color: [/* 30+ distinct colors for categories */],
    series: [{
        type: 'graphGL',
        nodes: nodes,
        edges: edges,
        categories: categories,                 // Category definitions

        forceAtlas2: {
            steps: 1,                           // Fewer steps for huge graphs
            jitterTolerence: 10,
            edgeWeight: [0.2, 1],
            gravity: 1,
            edgeWeightInfluence: 1,
            scaling: 0.2                        // Compact layout
        },

        lineStyle: {
            color: 'rgba(255,255,255,0.2)'      // Semi-transparent edges
        }
    }]
});
```

### Key Performance Optimizations

| Technique | Purpose |
|-----------|---------|
| `devicePixelRatio: 1` | Reduce rendering resolution |
| `symbolSize: fn(value)` | Smaller nodes for large graphs |
| `lineStyle.opacity: 0.05` | Reduce visual clutter |
| `forceAtlas2.steps: 1` | Fewer iterations for huge datasets |
| `label.show: false` | Hide labels by default |
| Conditional labels | Show only for high-value nodes |
| `maxSteps` limit | Prevent infinite layout |

---

## Future Considerations

- **WebGPU** for 3D rendering (when stable)
- **SharedArrayBuffer** for large datasets
- **Web Workers** for search on large graphs
- **WASM** for critical paths if needed
