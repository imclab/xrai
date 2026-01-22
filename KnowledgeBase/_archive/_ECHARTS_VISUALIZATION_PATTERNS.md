# Apache ECharts Visualization Patterns

**Purpose**: Reference patterns for high-performance graph visualization
**Technology**: Apache ECharts + ECharts-GL (WebGL acceleration)
**Last Updated**: 2026-01-12

---

## Quick Reference

| Feature | Library | Use Case |
|---------|---------|----------|
| 2D Charts | echarts | Bar, line, pie, scatter |
| 3D Charts | echarts-gl | 3D scatter, surface, bar |
| Large Graphs | echarts-gl (graphGL) | 10K+ nodes with GPU acceleration |
| Force Layout | ForceAtlas2 | Automatic graph layout |

---

## Installation

```bash
# NPM
npm install echarts echarts-gl

# Yarn
yarn add echarts echarts-gl
```

### CDN
```html
<script src="https://cdn.jsdelivr.net/npm/echarts/dist/echarts.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/echarts-gl/dist/echarts-gl.min.js"></script>
```

### ES Module with Tree-shaking
```javascript
import * as echarts from 'echarts/core';
import { GraphGLChart } from 'echarts-gl/charts';
import { Grid3DComponent } from 'echarts-gl/components';

echarts.use([GraphGLChart, Grid3DComponent]);
```

---

## GraphGL Pattern: NPM Dependencies

**Source**: [ecomfe/echarts-gl/test/graphGL-npm.html](https://github.com/ecomfe/echarts-gl/blob/master/test/graphGL-npm.html)

Best for: Dependency graphs, network visualization, 1K-50K nodes

```javascript
// Initialize with reduced pixel ratio for performance
var chart = echarts.init(document.getElementById('main'), null, {
    devicePixelRatio: 1
});

// Transform data
var nodes = data.nodes.map(function (nodeName, idx) {
    return {
        name: nodeName,
        value: data.dependentsCount[idx]
    }
});

var edges = [];
for (var i = 0; i < data.edges.length;) {
    edges.push({
        source: data.edges[i++],
        target: data.edges[i++]
    });
}

// Conditional labels based on importance
nodes.forEach(function (node) {
    if (node.value > 100) {
        node.emphasis = { label: { show: true } };
    }
    if (node.value > 5000) {
        node.label = { show: true };
    }
});

chart.setOption({
    series: [{
        type: 'graphGL',
        nodes: nodes,
        edges: edges,

        // Community detection
        modularity: {
            resolution: 2,
            sort: true
        },

        // GPU-accelerated force layout
        forceAtlas2: {
            steps: 5,               // Iterations per frame
            maxSteps: 3000,         // Total iterations
            jitterTolerence: 10,    // Convergence threshold
            edgeWeight: [0.2, 1],   // Weight range
            gravity: 5,             // Center attraction
            edgeWeightInfluence: 0
        },

        // Dynamic node sizing
        symbolSize: function (value) {
            return Math.sqrt(value / 10);
        },

        // Click to highlight connections
        focusNodeAdjacencyOn: 'click',

        // Styling
        lineStyle: {
            color: 'rgba(255,255,255,1)',
            opacity: 0.05,
            width: 1
        },
        label: {
            textStyle: { color: '#fff' }
        },
        emphasis: {
            label: { show: false },
            lineStyle: { opacity: 0.5, width: 1 }
        }
    }]
});

// Programmatic control
chart.dispatchAction({ type: 'graphGLStartLayout' });
chart.dispatchAction({ type: 'graphGLStopLayout' });
```

---

## GraphGL Pattern: Large Internet Graph

**Source**: [ecomfe/echarts-gl/test/graphGL-large.html](https://github.com/ecomfe/echarts-gl/blob/master/test/graphGL-large.html)

Best for: Very large graphs (50K+ nodes), category-based coloring

```javascript
// Random initial positions (faster than calculated)
var nodes = graph.nodes.map(function (node) {
    return {
        x: Math.random() * window.innerWidth,
        y: Math.random() * window.innerHeight,
        symbolSize: node[2],
        category: node[3],
        value: 1
    }
});

chart.setOption({
    // 30+ distinct colors for categories
    color: [
        "rgb(203,239,15)", "rgb(73,15,239)", "rgb(239,231,15)",
        "rgb(15,217,239)", "rgb(30,15,239)", "rgb(15,174,239)",
        // ... more colors
    ],
    series: [{
        type: 'graphGL',
        nodes: nodes,
        edges: edges,
        categories: categories,

        // Optimized for large graphs
        forceAtlas2: {
            steps: 1,               // Minimal per-frame work
            jitterTolerence: 10,
            edgeWeight: [0.2, 1],
            gravity: 1,
            edgeWeightInfluence: 1,
            scaling: 0.2            // Compact layout
        },

        lineStyle: {
            color: 'rgba(255,255,255,0.2)'
        },
        itemStyle: {
            opacity: 1
        }
    }]
});
```

---

## Performance Optimization Guide

### For Small Graphs (<1K nodes)
```javascript
{
    forceAtlas2: {
        steps: 10,
        maxSteps: 5000,
        gravity: 2
    },
    label: { show: true }  // Show all labels
}
```

### For Medium Graphs (1K-10K nodes)
```javascript
{
    devicePixelRatio: 1,
    forceAtlas2: {
        steps: 5,
        maxSteps: 3000,
        gravity: 5
    },
    lineStyle: { opacity: 0.1 },
    label: { show: false }  // Labels on hover only
}
```

### For Large Graphs (10K+ nodes)
```javascript
{
    devicePixelRatio: 1,
    forceAtlas2: {
        steps: 1,
        maxSteps: 1000,
        gravity: 1,
        scaling: 0.2
    },
    lineStyle: { opacity: 0.05 },
    symbolSize: function(v) { return Math.max(2, Math.sqrt(v/100)); }
}
```

---

## ForceAtlas2 Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `steps` | 1 | Iterations per animation frame |
| `maxSteps` | - | Stop layout after N iterations |
| `jitterTolerence` | 0.01 | Convergence threshold |
| `gravity` | 1 | Attraction to center |
| `scaling` | 1 | Overall layout scale |
| `edgeWeight` | [1,1] | Min/max edge weight |
| `edgeWeightInfluence` | 1 | How much weight affects layout |
| `preventOverlap` | false | Prevent node overlap |
| `GPU` | true | Use WebGL acceleration |

---

## Integration with XRAI Knowledge Graph

```javascript
// Convert MCP data to ECharts format
function mcpToECharts(mcpData) {
    const nodes = mcpData.entities.map((e, i) => ({
        id: i,
        name: e.name,
        value: e.observations?.length || 1,
        category: e.entityType
    }));

    const nodeIndex = {};
    nodes.forEach((n, i) => nodeIndex[n.name] = i);

    const edges = mcpData.relations.map(r => ({
        source: nodeIndex[r.from],
        target: nodeIndex[r.to]
    })).filter(e => e.source !== undefined && e.target !== undefined);

    const categories = [...new Set(mcpData.entities.map(e => e.entityType))]
        .map(name => ({ name }));

    return { nodes, edges, categories };
}
```

---

## Resources

- [Apache ECharts Official](https://echarts.apache.org/)
- [ECharts-GL GitHub](https://github.com/ecomfe/echarts-gl)
- [GraphGL Examples](https://echarts.apache.org/examples/en/index.html#chart-type-graphGL)
- [ForceAtlas2 Paper](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0098679)

---

## ECharts Ecosystem (from awesome-echarts)

Source: [ecomfe/awesome-echarts](https://github.com/ecomfe/awesome-echarts)

### Framework Bindings

| Framework | Package | Stars |
|-----------|---------|-------|
| React | [echarts-for-react](https://github.com/hustcc/echarts-for-react) | Most popular |
| Vue | [vue-echarts](https://github.com/Justineo/vue-echarts) | Official-ish |
| Angular | [ngx-echarts](https://github.com/xieziyu/ngx-echarts) | |
| Svelte | [svelte-echarts](https://github.com/bherbruck/svelte-echarts) | |
| React Native | [wrn-echarts](https://github.com/wuba/wrn-echarts) | SVG/Skia based |
| Flutter | [flutter_echarts](https://github.com/entronad/flutter_echarts) | |
| Web Components | [ECharts-JSX](https://github.com/idea2app/ECharts-JSX) | TypeScript |

### Language Wrappers

| Language | Package |
|----------|---------|
| Python | [pyecharts](https://github.com/pyecharts/pyecharts) |
| Go | [go-echarts](https://github.com/chenjiandongx/go-echarts) |
| Java | [ECharts-Java](https://github.com/ECharts-Java/ECharts-Java) |
| .NET | [TagEChartsBlazor](https://github.com/draculakkk/TagEChartsBlazor) |
| R | [echarts4r](https://echarts4r.john-coene.com/) |
| Ruby | [rails_charts](https://github.com/railsjazz/rails_charts) |
| Julia | [ECharts.jl](https://github.com/randyzwitch/ECharts.jl) |

### Key Extensions

| Extension | Purpose |
|-----------|---------|
| [echarts-gl](https://github.com/ecomfe/echarts-gl) | 3D/WebGL charts |
| [echarts-wordcloud](https://github.com/ecomfe/echarts-wordcloud) | Word clouds |
| [echarts-liquidfill](https://github.com/ecomfe/echarts-liquidfill) | Liquid fill gauges |
| [echarts-graph-modularity](https://github.com/ecomfe/echarts-graph-modularity) | Community detection |
| [echarts-leaflet](https://github.com/gnijuohz/echarts-leaflet) | Leaflet maps |
| [echarts-extension-gmap](https://github.com/plainheart/echarts-extension-gmap) | Google Maps |

### Tools

| Tool | Purpose |
|------|---------|
| [echarts-vscode-extension](https://github.com/susiwen8/echarts-vscode-extension) | VS Code autocomplete |
| [Grafana ECharts Panel](https://github.com/volkovlabs/volkovlabs-echarts-panel) | Dashboard integration |
| [pyecharts-snapshot](https://github.com/pyecharts/pyecharts-snapshot) | Export to PNG/PDF |
| [node-echarts](https://github.com/suxiaoxin/node-echarts) | Server-side rendering |

### Design Pattern: How Extensions Work

ECharts uses a **registration pattern** for extensions:

```javascript
// 1. Register custom chart type
echarts.registerChart('myChart', MyChartClass);

// 2. Register coordinate system
echarts.registerCoordinateSystem('myCoord', MyCoordSystem);

// 3. Register extension
echarts.registerExtension(extensionInstaller);

// 4. Use in options
chart.setOption({
    series: [{
        type: 'myChart',      // Custom chart
        coordinateSystem: 'myCoord'
    }]
});
```

### XRAI Integration Pattern

For our knowledge graph, follow similar patterns:

```javascript
// 1. Core library (like echarts)
import { KnowledgeGraph } from 'xrai-kg/data';
import { SearchEngine } from 'xrai-kg/search';

// 2. Visualization plugin (like echarts-gl)
import { EChartsRenderer } from 'xrai-kg/viz/echarts';

// 3. Platform adapter (like echarts-for-react)
import { VSCodeAdapter } from 'xrai-kg/adapters/vscode';

// 4. Compose
const kg = new KnowledgeGraph();
const search = new SearchEngine(kg);
const viz = new EChartsRenderer(container, kg);
const adapter = new VSCodeAdapter(vscode, { kg, search, viz });
```

---

## XRAI-KG Library

The `xrai-kg` library implements these patterns. See:
- `/xrai-kg/README.md` - Quick start guide
- `/xrai-kg/ARCHITECTURE.md` - Detailed architecture

```javascript
// Usage with XRAI
import XRAI from 'xrai-kg';

const xrai = XRAI.create({ data: mcpData });

// Natural language commands
xrai.run('search ARFoundation');
xrai.run('add MyProject as Project');
xrai.run('export mermaid');

// Direct API
const results = xrai.find('unity', { limit: 10 });
const neighbors = xrai.getNeighbors('ARFoundation', 2);
```

---

*Related: ARCHITECTURE.md, _VISUALIZATION_RESOURCES_INDEX.md*
