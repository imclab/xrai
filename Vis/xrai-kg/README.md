# xrai-kg

Modular, fast, platform-agnostic knowledge graph library with fuzzy search and natural language commands.

## Features

- **Modular Architecture** - Use only what you need (data, search, AI, viz)
- **Fuzzy Search** - Fuse.js integration with built-in fallback
- **Natural Language Commands** - Chat interface for graph operations
- **ECharts Visualization** - Auto-scaling from small graphs to 10K+ nodes with WebGL
- **Mermaid Export** - Flowchart, mindmap, and class diagram export
- **Platform Agnostic** - Works in Node.js, browser, VS Code, Chrome extensions
- **Zero Required Dependencies** - All dependencies are optional/peer

## Installation

```bash
npm install xrai-kg
```

Optional peer dependencies for enhanced features:
```bash
npm install fuse.js      # Better fuzzy search
npm install echarts      # Graph visualization
npm install echarts-gl   # WebGL acceleration for large graphs (10K+ nodes)
```

## Demo

Run the interactive demo:

```bash
cd xrai-kg
npx serve .
# Open http://localhost:3000/demo/
```

Features shown in demo:
- ECharts graph visualization with interactive nodes
- Fuzzy search with live results
- Type filtering with counts
- Chat command interface
- Mermaid diagram export (flowchart, mindmap, class diagram)
- Entity details panel
- PNG export

## Quick Start

```javascript
import XRAI from 'xrai-kg';

// Create instance with data
const xrai = XRAI.create({
    data: {
        entities: [
            { name: 'Unity', entityType: 'Engine' },
            { name: 'ARFoundation', entityType: 'Framework' }
        ],
        relations: [
            { from: 'Unity', to: 'ARFoundation', relationType: 'includes' }
        ]
    }
});

// Search
const results = xrai.find('unity');

// Natural language commands
xrai.run('add MyProject as Project');
xrai.run('relate MyProject to Unity as uses');
xrai.run('neighbors of Unity');
xrai.run('export mermaid');
```

## Chat Commands

| Command | Example |
|---------|---------|
| `search <query>` | `search unity patterns` |
| `add <name> as <type>` | `add ARFoundation as Technology` |
| `relate <A> to <B> [as type]` | `relate Unity to ARFoundation as uses` |
| `filter [by] <type>` | `filter by Project` |
| `neighbors [of] <name>` | `neighbors of Unity` |
| `stats` | Show graph statistics |
| `export [json\|mermaid\|csv]` | `export mermaid` |
| `help` | Show available commands |

## Modular Usage

```javascript
// Use individual layers
import { KnowledgeGraph } from 'xrai-kg/data';
import { SearchEngine } from 'xrai-kg/search';
import { ChatParser } from 'xrai-kg/ai';

const kg = new KnowledgeGraph();
kg.addEntity({ name: 'Unity', entityType: 'Engine' });

const search = new SearchEngine(kg);
const results = search.search('unity');

const parser = new ChatParser();
const cmd = parser.parse('add React as Framework');
```

## Visualization

### ECharts Renderer

```javascript
import { EChartsRenderer } from 'xrai-kg/viz/echarts';
// or
import { EChartsRenderer } from 'xrai-kg';

// Inject ECharts library
import * as echarts from 'echarts';
import 'echarts-gl';  // For GraphGL (large graphs)
EChartsRenderer.setEChartsLibrary(echarts);

// Create renderer
const renderer = new EChartsRenderer('#graph-container', {
    theme: 'dark',
    autoPerformance: true,  // Auto-select graph vs graphGL
    focusOnClick: true
});

// Render knowledge graph
renderer.render(kg);

// Interactions
renderer.focusNode('ARFoundation');
renderer.filterTypes(['Project', 'Technology']);
renderer.startLayout();  // GraphGL only
renderer.stopLayout();

// Export
const imageUrl = renderer.toImage({ type: 'png', pixelRatio: 2 });

// Cleanup
renderer.dispose();
```

### Mermaid Export

```javascript
import { MermaidExporter } from 'xrai-kg/viz/mermaid';
// or
import XRAI from 'xrai-kg';

const xrai = XRAI.create({ data: mcpData });

// Quick export
const flowchart = xrai.toMermaid();
const mindmap = xrai.toMermaid({ diagramType: 'mindmap' });
const classDiagram = xrai.toMermaid({ diagramType: 'classDiagram' });

// Or use exporter directly
const code = MermaidExporter.toMermaid(kg, {
    diagramType: 'flowchart',
    direction: 'TD',
    showRelationLabels: true
});
```

**Supported Diagram Types:**
- `flowchart` - Flow diagram with subgraphs by type
- `mindmap` - Hierarchical tree structure
- `classDiagram` - UML-style class diagram

## API Reference

### XRAI (Facade)

```javascript
const xrai = XRAI.create({ data, search, chat });

// Data operations
xrai.load(data)              // Bulk load entities/relations
xrai.addEntity(data)         // Add single entity
xrai.addRelation(from, to, type)  // Add relation

// Search operations
xrai.find(query, options)    // Fuzzy search
xrai.suggest(partial, limit) // Autocomplete
xrai.filterByTypes(types)    // Filter by type
xrai.getTypes()              // Get type counts

// Graph operations
xrai.getNeighbors(name, depth)  // Connected entities
xrai.getRelations(name)         // Relations for entity

// Chat commands
xrai.run(command)            // Execute natural language command
xrai.parseCommand(input)     // Parse without executing

// Visualization
xrai.createRenderer(container, options)  // Create ECharts renderer
xrai.toMermaid(options)                  // Export as Mermaid diagram

// Export
xrai.toJSON()
xrai.getStats()
```

### KnowledgeGraph

```javascript
const kg = new KnowledgeGraph();

kg.addEntity({ name, entityType, observations })
kg.getEntity(id)
kg.getEntityByName(name)
kg.removeEntity(id)
kg.getAllEntities()

kg.addRelation({ from, to, relationType, weight })
kg.getRelationsFor(entityName)
kg.getAllRelations()

kg.bulkAdd({ entities, relations })
kg.toJSON()
kg.getStats()
kg.getNeighbors(entityName, depth)

// Events
kg.on(callback)  // Returns unsubscribe function
```

### SearchEngine

```javascript
const search = new SearchEngine(kg, {
    threshold: 0.3,      // Fuzzy threshold (0-1)
    cacheEnabled: true,
    cacheTTL: 60000
});

search.search(query, { limit, threshold, types, minScore })
search.suggest(partial, limit)
search.filter(predicate)
search.filterByTypes(types)
search.getTypes()

// Inject Fuse.js
SearchEngine.setFuseLibrary(Fuse);
```

### ChatParser

```javascript
const parser = new ChatParser();

parser.parse(input)          // Returns { type, params, raw }
parser.execute(input, xrai)  // Execute and return result

// Optional LLM integration
parser.setLLMHandler(async (input) => {
    // Your LLM API call
    return { type, params };
});
await parser.parseWithLLM(input);
```

### EChartsRenderer

```javascript
const renderer = new EChartsRenderer(container, {
    echarts: echartsLib,     // Injected ECharts library
    theme: 'dark',           // 'light' or 'dark'
    autoPerformance: true,   // Auto-select graph type by size
    forceType: null,         // Force 'graph' or 'graphGL'
    symbolSize: [5, 30],     // Node size range
    modularity: true,        // Enable community detection
    focusOnClick: true       // Click to highlight neighbors
});

// Static injection
EChartsRenderer.setEChartsLibrary(echarts);

// Methods
renderer.init()                    // Initialize chart
renderer.render(kg, options)       // Render knowledge graph
renderer.update(kg)                // Update with new data
renderer.focusNode(name)           // Highlight node
renderer.clearFocus()              // Clear highlighting
renderer.filterTypes(types)        // Filter by entity types
renderer.showAllTypes()            // Show all types
renderer.startLayout()             // Start GraphGL layout
renderer.stopLayout()              // Stop GraphGL layout
renderer.setTheme(theme)           // Change theme
renderer.toImage(options)          // Export as image
renderer.resize()                  // Resize chart
renderer.dispose()                 // Cleanup
renderer.on(event, handler)        // Subscribe to events
renderer.off(event, handler)       // Unsubscribe
```

### MermaidExporter

```javascript
const exporter = new MermaidExporter({
    diagramType: 'flowchart',      // 'flowchart', 'mindmap', 'classDiagram'
    direction: 'TD',               // 'TD', 'LR', 'BT', 'RL'
    showRelationLabels: true,      // Show relation type on arrows
    useStyles: true                // Generate CSS styles
});

exporter.export(kg, options)       // Export to Mermaid code

// Static methods
MermaidExporter.toMermaid(kg, options)  // Quick export
MermaidExporter.validate(code)          // Validate syntax
```

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed design documentation.

```
┌─────────────────────────────────────────────────────────────────┐
│                        ADAPTER LAYER                            │
│   VS Code │ Chrome Extension │ Standalone │ Electron │ Web      │
├─────────────────────────────────────────────────────────────────┤
│                     VISUALIZATION LAYER                         │
│        ECharts │ Cytoscape.js │ 3D Force Graph │ Mermaid        │
├─────────────────────────────────────────────────────────────────┤
│                          AI LAYER                               │
│           Chat Parser │ NLP │ LLM Hooks │ Suggestions           │
├─────────────────────────────────────────────────────────────────┤
│                        SEARCH LAYER                             │
│              Fuse.js │ Filtering │ Indexing │ Cache             │
├─────────────────────────────────────────────────────────────────┤
│                         DATA LAYER                              │
│         KnowledgeGraph │ Entities │ Relations │ Storage         │
└─────────────────────────────────────────────────────────────────┘
```

## Integration with XRAI Dashboard

The main XRAI Dashboard (`knowledge-graph-xrai-dashboard.html`) now includes ECharts as a visualization option alongside Cytoscape 2D, 3D Force Graph, and AI Architecture views.

To use:
1. Open `knowledge-graph-xrai-dashboard.html`
2. Click the **ECharts** button in the view toggle
3. Load data via the Ingest panel
4. All views stay synchronized

## License

MIT
