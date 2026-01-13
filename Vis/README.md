# 3D Visualization Frontends

**Last Updated**: 2026-01-13
**Purpose**: Centralized collection of all 3D visualization and dashboard tools

---

## Quick Overview (Sorted by Date Created)

| Project | Created | Stack | Port | Status |
|---------|---------|-------|------|--------|
| [chalktalk-master](#1-chalktalk-master) | 2018-05-20 | Node.js + WebGL | 3000 | Legacy |
| [HOLOVIS](#2-holovis) | 2025-06-29 | Three.js + Express | 3000 | Active |
| [cosmos-visualizer](#3-cosmos-visualizer) | 2025-09-23 | Vite + Three.js | 5173 | Active |
| [cosmos-standalone-web](#4-cosmos-standalone-web) | 2025-09-24 | Vite + 3d-force-graph | 5173 | Active |
| [cosmos-needle-web](#5-cosmos-needle-web) | 2025-09-24 | Needle Engine + Vite | 5173 | Active |
| [WarpDashboard](#6-warpdashboard) | 2025-12-06 | Static HTML | N/A | Active |
| [dashboard.html](#7-dashboardhtml) | 2026-01-08 | Standalone HTML | N/A | Active |
| [knowledge-graph-dashboard.html](#8-knowledge-graph-dashboardhtml) | 2026-01-10 | ECharts HTML | N/A | Active |
| [xrai-kg](#9-xrai-kg) | 2026-01-12 | Modular ES6 + ECharts | N/A | Active |
| [knowledge-graph-xrai-dashboard.html](#10-knowledge-graph-xrai-dashboardhtml) | 2026-01-13 | ECharts + xrai-kg | N/A | Active |

---

## 1. chalktalk-master

**Ken Perlin's Chalktalk** - Pioneering WebGL sketch-to-3D visualization

**Created**: 2018-05-20 | **Stack**: Node.js, Express, WebSockets, WebGL

### Setup
```bash
cd Vis/chalktalk-master/server
npm install
cd ..
node server/server.js
# Open http://localhost:3000
```

### Features
- Real-time collaborative sketching
- WebGL 3D rendering from 2D sketches
- WebSocket-based multi-user support

### Notes
- Legacy project - may need Node.js v14 or earlier
- Original by Ken Perlin (NYU Future Reality Lab)

---

## 2. HOLOVIS

**3D Unity Codebase Visualizer** - Analyze and visualize Unity project structures

**Created**: 2025-06-29 | **Stack**: Three.js, Express, WebSockets

### Setup
```bash
cd Vis/HOLOVIS
npm install
npm run serve     # Start server on port 3000
# OR
npm run demo      # Run demo server
```

### Available Commands
| Command | Description |
|---------|-------------|
| `npm start` | Start main app |
| `npm run dev` | Development with hot reload |
| `npm run analyze` | Analyze Unity codebase |
| `npm run serve` | Start web server |
| `npm run demo` | Demo server |
| `npm run batch` | Batch analysis |

### Features
- Unity codebase structure analysis
- 3D force-directed graph visualization
- Real-time WebSocket updates

---

## 3. cosmos-visualizer

**D3 + Three.js Cosmos Visualization**

**Created**: 2025-09-23 | **Stack**: Vite, Three.js, D3.js, Tween.js

### Setup
```bash
cd Vis/cosmos-visualizer
npm install
npm run dev       # Start dev server on http://localhost:5173
npm run build     # Build for production
npm run preview   # Preview production build
```

### Dependencies
- Three.js ^0.165.0 - 3D rendering
- D3 ^7.9.0 - Data visualization
- dat.GUI ^0.7.9 - Interactive controls
- Tween.js - Smooth animations

---

## 4. cosmos-standalone-web

**3D Force Graph Visualization**

**Created**: 2025-09-24 | **Stack**: Vite, 3d-force-graph, D3-force-3d, GSAP

### Setup
```bash
cd Vis/cosmos-standalone-web
npm install
npm run dev       # Start dev server on http://localhost:5173
npm run build     # Build for production
npm run preview   # Preview production build
```

### Key Dependencies
- 3d-force-graph ^1.71.4 - Force-directed 3D graphs
- D3-force-3d ^3.0.5 - 3D force simulation
- GSAP ^3.12.4 - Animation library
- pako ^2.1.0 - Compression

---

## 5. cosmos-needle-web

**Needle Engine WebXR Visualization**

**Created**: 2025-09-24 | **Stack**: Needle Engine, Vite, Three.js

### Setup
```bash
cd Vis/cosmos-needle-web
npm install
npm run dev       # Start dev server on http://localhost:5173
npm run build     # Build for production
npm run preview   # Preview production build
```

### Key Dependencies
- @needle-tools/engine ^3.39.0 - Needle Engine for WebXR
- Three.js ^0.169.0 - 3D rendering
- D3-force-3d ^3.0.5 - Force simulation

### Notes
- WebXR ready - works with VR/AR headsets
- Requires Needle Engine runtime

---

## 6. WarpDashboard

**Jobs Data Dashboard** - Static HTML dashboard for jobs data visualization

**Created**: 2025-12-06 | **Stack**: Static HTML/CSS/JS

### Setup
```bash
cd Vis/WarpDashboard
# Open in browser (no server needed):
open jobs_dashboard.html
# OR
open jobs_data_explorer.html
```

### Files
| File | Description |
|------|-------------|
| `jobs_dashboard.html` | Main dashboard |
| `jobs_data.json` | Jobs data source |
| `jobs_data_explorer.html` | Data explorer |

---

## 7. dashboard.html

**Standalone Dashboard** - Single-file visualization dashboard

**Created**: 2026-01-08 | **Stack**: Standalone HTML

### Setup
```bash
cd Vis
open dashboard.html
```

---

## 8. knowledge-graph-dashboard.html

**ECharts Knowledge Graph** - Interactive knowledge graph visualization

**Created**: 2026-01-10 | **Stack**: ECharts, Standalone HTML

### Setup
```bash
cd Vis
open knowledge-graph-dashboard.html
```

### Features
- ECharts-based graph rendering
- Interactive node exploration
- Force-directed layout

---

## 9. xrai-kg

**XRAI Knowledge Graph Library** - Modular, platform-agnostic knowledge graph

**Created**: 2026-01-12 | **Stack**: ES6 Modules, Rollup, ECharts (optional)

### Setup
```bash
cd Vis/xrai-kg
npm install
npm run dev       # Vite dev server
npm run demo      # Serve demo via npx serve
npm run build     # Build library (CJS, ESM, UMD)
npm test          # Run tests
```

### Module Exports
```javascript
import { KnowledgeGraph } from 'xrai-kg/data';
import { SearchEngine } from 'xrai-kg/search';
import { ChatParser } from 'xrai-kg/ai';
import { EChartsRenderer } from 'xrai-kg/viz/echarts';
import { MermaidExporter } from 'xrai-kg/viz/mermaid';
```

### Features
- Zero dependencies core
- Fuzzy search (Fuse.js optional)
- ECharts/ECharts-GL visualization (optional)
- Mermaid diagram export
- VSCode & Chrome extension adapters

### Use as NPM Package
```bash
npm install xrai-kg
# Or use directly:
<script type="module">
  import { KnowledgeGraph } from './xrai-kg/dist/xrai-kg.esm.js';
</script>
```

---

## 10. knowledge-graph-xrai-dashboard.html

**XRAI Dashboard Integration** - Full-featured dashboard using xrai-kg library

**Created**: 2026-01-13 | **Stack**: ECharts + xrai-kg

### Setup
```bash
cd Vis
open knowledge-graph-xrai-dashboard.html
# OR with local server:
npx serve .
```

---

## Development Tips

### Common Commands
```bash
# Start any Vite project
npm run dev

# Build for production
npm run build

# Quick serve any HTML file
npx serve .
```

### Port Conflicts
Most Vite projects default to port 5173. To run multiple:
```bash
# cosmos-visualizer on 5173
cd cosmos-visualizer && npm run dev

# cosmos-standalone-web on 5174
cd cosmos-standalone-web && npm run dev -- --port 5174
```

### Browser Testing
For WebXR features (cosmos-needle-web), use:
- Chrome with WebXR enabled
- Quest browser for VR testing
- Safari for AR Quick Look

---

## Architecture Overview

```
Vis/
├── chalktalk-master/     # Legacy WebGL sketching
├── HOLOVIS/              # Unity codebase analyzer (Three.js)
├── cosmos-visualizer/    # D3 + Three.js graphs
├── cosmos-standalone-web/ # 3d-force-graph
├── cosmos-needle-web/    # Needle Engine WebXR
├── WarpDashboard/        # Static jobs dashboard
├── xrai-kg/              # Modular KG library
├── dashboard.html        # Standalone dashboard
├── knowledge-graph-dashboard.html      # ECharts KG
└── knowledge-graph-xrai-dashboard.html # xrai-kg integrated
```

---

**Maintainer**: James Tunick
