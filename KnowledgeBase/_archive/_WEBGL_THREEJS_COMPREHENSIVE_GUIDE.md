# WebGL/Three.js Comprehensive Development Guide

**Version**: 1.0
**Last Updated**: 2025-01-07
**Purpose**: Complete reference for WebGL, Three.js, React Three Fiber, and spatial web development

---

## Philosophy: CDN > NPM, Minimal > Complex

**Priority Order**:
1. Single file → 2. CDN only → 3. Minimal deps → 4. Standard libs → 5. Proven frameworks

**Speed Mandate**:
- Zero cost, always free/open-source
- Instant setup: <1 minute deployment
- Fast execution: sub-second response times
- 30-second web deploy possible

---

## Core Three.js & React Three Fiber

### Three.js Essentials
```yaml
Main Resources:
- Website: https://threejs.org/
- Examples: https://threejs.org/examples/
- Documentation: https://threejs.org/docs/
- CDN (latest): https://unpkg.com/three@0.160.0/build/three.min.js
```

### React Three Fiber (R3F)
```yaml
Core:
- GitHub: https://github.com/pmndrs/react-three-fiber
- Documentation: https://r3f.docs.pmnd.rs/getting-started/introduction
- Examples: https://r3f.docs.pmnd.rs/getting-started/examples

Drei (R3F Helpers):
- GitHub: https://github.com/pmndrs/drei
- Documentation: https://drei.docs.pmnd.rs/getting-started/introduction
```

### A-Frame (Declarative WebXR)
```yaml
Main:
- Website: https://aframe.io/
- GitHub: https://github.com/aframevr/aframe
- Registry: https://aframe.io/aframe-registry/
```

---

## Critical CodeSandbox Examples (Study These)

### Essential Templates
```yaml
Fundamentals:
- Basic R3F Setup: https://codesandbox.io/p/sandbox/bst0cy?file=%2Fsrc%2FApp.js
- Dynamic Envmaps: https://codesandbox.io/p/sandbox/building-dynamic-envmaps-forked-5c74vy
- Shadertoy Template: https://codesandbox.io/p/sandbox/shadertoy-three-js-template-forked-6ptdgg?file=%2Fsrc%2Findex.ts

Advanced:
- GPGPU Curl Noise: https://codesandbox.io/p/sandbox/gpgpu-curl-noise-dof-forked-vluse3?file=%2Fsrc%2FApp.js
- Advanced Effects: https://codesandbox.io/p/sandbox/pbwi6i?file=%2Fsrc%2FApp.js
- Complex Scenes: https://codesandbox.io/p/sandbox/f79ucc?file=%2Fsrc%2FApp.js
- Purple Sound: https://codesandbox.io/p/devbox/purple-sound-s27fdj?file=%2Fsrc%2FApp.js
- Splats Demo: https://codesandbox.io/p/devbox/splats-forked-zlh3pm?file=%2Fsrc%2FApp.js

All Examples:
- R3F Example Collection: https://codesandbox.io/examples/package/@react-three/fiber
```

---

## Performance Patterns

### Three.js Instancing (1M Objects @ 144fps)
```javascript
// Instance rendering for massive object counts
const mesh = new THREE.InstancedMesh(geometry, material, 1000000);
const matrix = new THREE.Matrix4();
const color = new THREE.Color();

// Batch update for performance
for (let i = 0; i < 1000000; i++) {
    matrix.setPosition(positions[i]);
    mesh.setMatrixAt(i, matrix);
    mesh.setColorAt(i, color.setHex(colors[i]));
}
mesh.instanceMatrix.needsUpdate = true;
mesh.instanceColor.needsUpdate = true;
```

### WebGL Optimization Checklist
```yaml
Critical Optimizations:
- Use InstancedMesh for repeated objects
- Implement frustum culling
- Enable geometry merging
- Use texture atlases
- Implement LOD (Level of Detail)
- Dispose unused resources
- Use BufferGeometry (not Geometry)
- Minimize draw calls
- Enable hardware acceleration
```

---

## 3D Viewers & Spatial Computing

### Gaussian Splatting
```yaml
Unity Implementations:
- UnityGaussianSplatting: https://github.com/aras-p/UnityGaussianSplatting
- VR Viewer: https://github.com/clarte53/GaussianSplattingVRViewerUnity
- SplatVFX (Keijiro): https://github.com/keijiro/SplatVFX
- Original Paper: https://github.com/graphdeco-inria/gaussian-splatting

Web Implementations:
- GaussianSplats3D: https://github.com/mkkellogg/GaussianSplats3D
- Gaussian Editor: https://github.com/buaacyw/GaussianEditor
```

### Spatial Viewers
```yaml
Rerun (Data Viz):
- Viewer: https://rerun.io/viewer
- GitHub: https://github.com/rerun-io/rerun

PlayCanvas:
- Workflow: https://developer.playcanvas.com/user-manual/getting-started/workflow/
- SuperSplat: https://github.com/playcanvas/supersplat
- Gaussian Docs: https://developer.playcanvas.com/user-manual/graphics/gaussian-splatting/
- Engine: https://github.com/playcanvas/engine/tree/main
- Examples: https://playcanvas.vercel.app/#/misc/hello-world

Other Viewers:
- Rufus 3D Viewer: https://rufus31415.github.io/sandbox/3d-viewer/
- Simple WebXR Unity: https://github.com/Rufus31415/Simple-WebXR-Unity
- Viser (NeRF): https://github.com/nerfstudio-project/viser
```

### AR Libraries
```yaml
JavaScript AR:
- AR.js: https://github.com/AR-js-org/AR.js
- AlvaAR: https://github.com/alanross/AlvaAR
- PlayCanvas AR: https://github.com/playcanvas/playcanvas-ar
```

---

## Shader Resources

### Shader Playgrounds
```yaml
Online Editors:
- Shadertoy: https://www.shadertoy.com/
- GLSL App: https://glsl.app/
- Shadered: https://shadered.org/shaders

Unity:
- Shader Graph: Built into URP/HDRP
- VFX Graph: Visual shader editor for particles
```

---

## Creative Coding & Prototyping

### P5.js (Rapid Prototyping)
```yaml
Main:
- Examples: https://p5js.org/examples/
- Use for: Quick sketches before Three.js implementation
```

---

## Icosa Gallery (Open Brush)

### Resources
```yaml
Main:
- Gallery: https://icosa.gallery/
- API Docs: https://api.icosa.gallery/v1/docs
- Example: https://icosa.gallery/view/3UL8Bz_Id6I

Open Brush:
- GitHub: https://github.com/icosa-foundation/open-brush
- Unity Tools: https://github.com/icosa-foundation/open-brush-unity-tools
- Documentation: https://docs.openbrush.app/
```

---

## Deployment Structure (WebGL Projects)

### Minimal Project Template
```
webapp/
├── index.html            # Single entry point
├── js/
│   ├── three.min.js     # CDN fallback
│   ├── app.js           # Main logic
│   └── workers/         # Web Workers for threading
├── assets/              # Optimized 3D assets
├── install.sh           # One-click setup
└── deploy/
    ├── vercel.json
    ├── netlify.toml
    └── docker-compose.yml
```

### Lightning Deploy Script (30 seconds)
```bash
#!/bin/bash
# 30-second web deploy

# Use Bun for speed
curl -fsSL https://bun.sh/install | bash

# Project setup
bunx create-vite app --template vanilla
cd app

# Add Three.js via CDN (no build step needed)
cat > index.html << 'EOF'
<!DOCTYPE html>
<html>
<head>
    <title>3D App</title>
    <script src="https://unpkg.com/three@0.160.0/build/three.min.js"></script>
</head>
<body>
    <script src="main.js"></script>
</body>
</html>
EOF

# Deploy to multiple platforms
bunx vercel --yes
bunx netlify deploy --prod
bunx surge .

echo "✅ Live in 30 seconds!"
```

---

## AI/ML Integration for Web

### Browser-Based ML
```yaml
Tools:
- Transformers.js: CDN-based ML models (no server needed)
- ONNX Runtime Web: Browser inference engine
- MediaPipe: Real-time vision processing
- TensorFlow.js: Complete ML framework for web

Use Cases:
- Hand/pose tracking in WebXR
- Object detection in AR
- Real-time style transfer
- Voice commands (Whisper.js)
```

---

## Standards & Future Tech

### Emerging Standards
```yaml
Monitor These:
- W3C Immersive Web: https://www.w3.org/immersive-web/
- Khronos Standards: https://www.khronos.org/
- glTF Extensions: https://www.khronos.org/gltf/
- OpenUSD: https://openusd.org/release/index.html
- Metaverse Standards: https://metaverse-standards.org/

Adopt Now:
- WebGPU compute shaders
- Gaussian Splats for neural rendering
- WebTransport for low-latency networking
- React 19 with Suspense
```

### Active Research
```yaml
Stay Current:
- GitHub Trending: https://github.com/trending?since=monthly
- 3D Topics: https://github.com/topics/3d
- Data Viz: https://github.com/topics/data-visualization
- Deep Learning: https://github.com/topics/deep-learning
```

---

## Unity → WebGL Interoperability

### Asset Pipeline
```yaml
Export Formats:
- glTF/GLB: Standard 3D format (preferred)
- FBX: Legacy support
- USD: Emerging standard
- VRM: Humanoid avatars

Tools:
- Unity glTF Exporter: Built-in
- Needle Engine: Unity → Three.js
- UniGLTF: VRM support
```

### WebRTC/Multiplayer
```yaml
Unity:
- Normcore: https://docs.normcore.io/
- Netcode for GameObjects
- Photon/Fusion

Web:
- Simple-Peer: WebRTC wrapper
- Socket.io: WebSocket fallback
- PeerJS: Easy P2P
```

---

## Best Practices & Critical Rules

### Performance Targets
```yaml
Target Performance:
- Desktop: 144fps on high-end, 60fps on integrated GPU
- Mobile: 60fps on flagship, 30fps on mid-range
- XR: 90fps Quest 2, 120fps Quest 3

Optimization:
- Test on worst device first ("potato phone test")
- Profile early and often
- Use Chrome DevTools Performance tab
- Enable Stats.js for realtime FPS monitoring
```

### Implementation Philosophy
```yaml
Critical Rules:
1. Never abstract before needed - Ship working code first
2. CDN > NPM - Faster, simpler, no build step
3. Test on worst device - If it works on 2015 phone, ship it
4. Documentation = Code - Self-documenting with examples
5. Future-proof = Boring tech - Proven > Cutting edge
```

---

## Quick Reference Commands

### Development Server
```bash
# Python (built-in)
python3 -m http.server 8000

# Node.js
npx serve

# Bun (fastest)
bunx serve
```

### Testing
```bash
# Test on mobile via ngrok
npx ngrok http 8000

# Local network testing
python3 -m http.server 8000 --bind 0.0.0.0
```

---

**Remember**: Building for 2030, shipping for today.
- Every project: <1 minute setup, zero cost, instant deploy
- Code quality: Genius-level, but readable by juniors
- Performance: 144fps on high-end, 30fps on potato

**Fast or kicked hard. 🚀**
