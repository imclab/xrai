# Cosmos Hypergraph Visualizer

A comprehensive 3D visualization system for exploring interconnected data from multiple sources including Icosa Gallery, Objaverse, GitHub, and local files. Features both web-only and Unity versions with multiplayer support.

## Projects Overview

### 1. Needle Engine Web (Multiplayer) - `/cosmos-needle-web`
- **Default viewer** with MetavidoVFX effects and Icosa gallery integration
- Real-time multiplayer synchronization via Needle Engine
- WebXR support for VR/AR experiences
- Supports all data sources and 3D model formats

### 2. Standalone Web Version - `/cosmos-standalone-web`
- Pure Three.js implementation without Unity dependencies
- All visualization modes: Force Graph, City Blocks, Cosmos, Tree
- Universal 3D model loader (GLTF, OBJ, FBX, USDZ)
- Lightweight and fast, runs anywhere

### 3. Unity Project (Coming Soon)
- Full Unity project with Needle Engine integration
- Native VR/AR support via ARFoundation
- Advanced particle effects and physics
- Build for Quest, iOS, Android, Windows, macOS

## Features

### Data Sources
- **Icosa Gallery**: 3D artwork from Icosa Foundation
- **Objaverse**: Large-scale 3D object database
- **GitHub Archive**: Real-time repository activity
- **Local Files**: JSON data and 3D models
- **Web Search**: General web content integration

### Visualization Layouts
- **Force Graph**: Physics-based node layout
- **City Blocks**: Hierarchical city visualization
- **Cosmos**: Spherical star-field layout
- **Tree**: Hierarchical tree structure

### 3D Model Support
- GLTF/GLB (recommended)
- OBJ with MTL
- FBX
- USDZ (iOS)
- Drag & drop support
- Automatic format detection

## Quick Start

### Needle Engine Web (Multiplayer)
```bash
cd cosmos-needle-web
npm install
npm run dev
```
Open http://localhost:3000

### Standalone Web Version
```bash
cd cosmos-standalone-web
npm install
npm run dev
```
Open http://localhost:3001

## Usage

### Search
1. Enter search terms in the search box
2. Select data sources (or use "All Sources")
3. Press Enter or click Search
4. Results appear as interconnected nodes

### Navigation
- **Left Click + Drag**: Rotate view
- **Right Click + Drag**: Pan view
- **Scroll**: Zoom in/out
- **Click Node**: Load 3D model or open link

### Import Data
- Drag & drop 3D models onto the viewport
- Upload JSON data via file picker
- Supports batch imports

### Multiplayer (Needle Engine only)
- Automatic room creation
- Synchronized search results
- Shared 3D model viewing
- User avatars and positions

## Development

### Architecture
```
cosmos-visualizer/
├── cosmos-needle-web/       # Needle Engine multiplayer
│   ├── src/
│   │   ├── main.js         # Entry point
│   │   ├── layouts/        # Visualization layouts
│   │   ├── components/     # VFX and UI components
│   │   ├── data/           # Data source managers
│   │   └── multiplayer/    # Networking code
│   └── package.json
│
├── cosmos-standalone-web/   # Standalone Three.js
│   ├── src/
│   │   ├── main.js         # Application core
│   │   ├── visualization/  # Layout algorithms
│   │   ├── data/          # Search managers
│   │   ├── loaders/       # 3D model loaders
│   │   └── ui/            # UI controllers
│   └── package.json
│
└── cosmos-unity/           # Unity project (TBD)
    ├── Assets/
    ├── Packages/
    └── ProjectSettings/
```

### Adding Data Sources
1. Create new search method in `SearchManager.js`
2. Add source button in UI
3. Implement data transformation to graph format
4. Test with sample queries

### Custom Layouts
1. Extend base layout class
2. Implement `generate()` method
3. Add to `VisualizationManager`
4. Create UI controls

## Performance

### Optimization Tips
- Use `.claudeignore` for Unity projects
- Enable GPU acceleration in browser
- Limit search results to 100 nodes
- Use LOD for complex models
- Enable instancing for repeated geometry

### System Requirements
- **Minimum**: 4GB RAM, WebGL 2.0
- **Recommended**: 8GB RAM, dedicated GPU
- **VR/AR**: Quest 2/3, ARCore/ARKit device

## API Integration

### Icosa Gallery API
```javascript
GET https://api.icosa.foundation/artworks/search?q={query}
GET https://api.icosa.foundation/artworks/random
```

### Objaverse API
```javascript
// Local index recommended for performance
const index = await fetch('/objaverse_gltf_index_lite.json');
```

### GitHub Archive
```javascript
GET https://data.gharchive.org/{date}-{hour}.json.gz
```

## Deployment

### Build for Production
```bash
# Needle Engine version
cd cosmos-needle-web
npm run build

# Standalone version
cd cosmos-standalone-web
npm run build
```

### Docker Support
```bash
docker build -t cosmos-visualizer .
docker run -p 3000:3000 cosmos-visualizer
```

## Troubleshooting

### Common Issues
1. **CORS errors**: Use proxy server or enable CORS headers
2. **Large models**: Pre-process with Draco compression
3. **Memory issues**: Clear scene between searches
4. **WebGPU not available**: Falls back to WebGL automatically

## Contributing

1. Fork the repository
2. Create feature branch
3. Add tests for new features
4. Submit pull request

## License

MIT License - See LICENSE file for details

## Credits

- Needle Engine for multiplayer framework
- Three.js for 3D rendering
- Icosa Foundation for gallery API
- ObjaVerse for 3D model database
- GitHub Archive for activity data