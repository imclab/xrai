# Unity-XR-AI Project

**Comprehensive Unity XR/AR/VR Development Knowledgebase + Visualization Tools**

This repository contains production-ready code patterns, 520+ GitHub repo references, 10 visualization frontends, and the MetavidoVFX Unity project.

---

## 📂 Repository Structure

```
Unity-XR-AI/
├── KnowledgeBase/           # 75+ knowledge files, patterns, references
├── AgentBench/              # Unity research workbench (source code access)
├── Vis/                     # 10 3D visualization frontends
├── MetavidoVFX-main/        # Unity VFX project (AR Foundation + H3M)
├── mcp-server/              # MCP KB Server (TypeScript)
├── Scripts/                 # Utility scripts
├── specs/                   # Spec-Kit specifications
└── xrai-speckit/            # Specify.ai templates
```

---

## 🔑 Key Files

| File | Purpose |
|------|---------|
| `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` | 520+ repos indexed by category |
| `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | 50+ production-ready code snippets |
| `KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` | Hologram, portal, depth patterns |
| `KnowledgeBase/_COMPREHENSIVE_HOLOGRAM_PIPELINE_ARCHITECTURE.md` | 6-layer hologram architecture |
| `KnowledgeBase/_LIVE_AR_PIPELINE_ARCHITECTURE.md` | ⚠️ LEGACY - See Hybrid Bridge Pattern |
| `MetavidoVFX-main/Assets/Documentation/VFX_PIPELINE_FINAL_RECOMMENDATION.md` | **PRIMARY** - Hybrid Bridge architecture |
| `KnowledgeBase/_HAND_SENSING_CAPABILITIES.md` | 21-joint hand tracking patterns |
| `KnowledgeBase/_HOLOGRAM_RECORDING_PLAYBACK.md` | Recording/playback specs (40K) |
| `KnowledgeBase/_UNITY_SOURCE_REFERENCE.md` | Unity engine internals (AgentBench) |
| `KnowledgeBase/_PROJECT_CONFIG_REFERENCE.md` | All configs/scripts documented |
| `KnowledgeBase/LEARNING_LOG.md` | Continuous discoveries |
| `AgentBench/AGENT.md` | Unity research workbench instructions |
| `Vis/README.md` | Visualization setup documentation |
| `PLATFORM_COMPATIBILITY_MATRIX.md` | Platform support matrix |
| `MetavidoVFX-main/Assets/Documentation/README.md` | MetavidoVFX system docs |
| `MetavidoVFX-main/Assets/Documentation/SYSTEM_ARCHITECTURE.md` | 90% complete architecture docs |
| `MetavidoVFX-main/Assets/Documentation/QUICK_REFERENCE.md` | VFX properties cheat sheet |
| `MetavidoVFX-main/CLAUDE.md` | MetavidoVFX project instructions |

---

## 📊 Statistics

- **KnowledgeBase**: 1,043 markdown files (658MB after cleanup)
- **GitHub Repos**: 520+ curated (ARFoundation, VFX, DOTS, Networking, ML/AI)
- **Vis Projects**: 10 (xrai-kg, HOLOVIS, cosmos-*, WarpDashboard, chalktalk)
- **Code Snippets**: 50+ production-ready patterns
- **Platform Coverage**: iOS 15+, Android, Quest 3/Pro, WebGL, Vision Pro
- **MetavidoVFX Scripts**: 458 C# scripts (core + H3M + Echovision + NNCam)
- **VFX Assets**: 88 total (73 in Resources/VFX organized by category)
- **Unity Version**: 6000.2.14f1, AR Foundation 6.2.1, VFX Graph 17.2.0
- **Performance**: 353 FPS @ 10 VFX (verified Jan 16, 2026)

---

## 🖥️ Visualization Frontends (Vis/)

| Project | Stack | Purpose |
|---------|-------|---------|
| **xrai-kg** | ES6 + ECharts | Modular knowledge graph library |
| **HOLOVIS** | Three.js + Express | Unity codebase 3D visualizer |
| **cosmos-standalone-web** | 3d-force-graph | Force-directed graphs |
| **cosmos-needle-web** | Needle Engine | WebXR visualization |
| **WarpDashboard** | Static HTML | Jobs data dashboard |
| **chalktalk-master** | Node.js + WebGL | Ken Perlin's sketch-to-3D |

**Quick Start**: `cd Vis/xrai-kg && npm install && npm run dev`

---

## 🎮 MetavidoVFX Unity Project

AR Foundation VFX project with H3M Hologram system.

**Build**: `./build_ios.sh`
**Deploy**: `./deploy_ios.sh`

### Core Architecture (Updated 2026-01-16)

**Primary Pipeline**: Hybrid Bridge Pattern (ARDepthSource + VFXARBinder) - O(1) compute scaling
- Single compute dispatch (ARDepthSource) for all active VFX
- Lightweight per-VFX binders (VFXARBinder) for texture/data mapping
- VFXLibraryManager (~920 LOC) for pipeline-aware VFX management
- 73 VFX in Resources/VFX organized by category (People, Environment, NNCam2, Akvfx, Rcam2-4, SdfVfx)
- 353 FPS verified with 10 active VFX
- Legacy components removed: VFXBinderManager, VFXARDataBinder (moved to _Legacy folder)

**Systems**:
- **VFX Management**: ARDepthSource (PRIMARY), VFXARBinder, VFXLibraryManager, VFXToggleUI
- **Hand Tracking**: HandVFXController (velocity-driven, pinch detection), HoloKit integration
- **Audio**: AudioBridge (FFT frequency bands to global shader props), SoundWaveEmitter
- **Performance**: VFXAutoOptimizer (FPS-adaptive), VFXLODController, VFXProfiler
- **EchoVision**: MeshVFX (AR mesh → GraphicsBuffers), HumanParticleVFX
- **H3M Hologram**: HologramSource, HologramRenderer, HologramAnchor
- **NNCam**: NNCamKeypointBinder, NNCamVFXSwitcher (9 keypoint-driven VFX)
- **Body Segmentation**: BodyPartSegmenter (24-part BodyPixSentis)

**Documentation**: `MetavidoVFX-main/Assets/Documentation/README.md`

### Bug Fixes Applied (Jan 2026)

See `MetavidoVFX-main/Assets/Documentation/CODEBASE_AUDIT_2026-01-15.md` for details:
1. ✅ **Thread Dispatch Mismatch** - Fixed: uses dynamic thread group size queries
2. ✅ **Integer Division Truncation** - Fixed: HumanParticleVFX uses `CeilToInt()`
3. ✅ **Memory Leak** - Fixed: RenderTexture release in OnDestroy()

---

## 🔬 AgentBench (Unity Research)

Unity source code research workbench from keijiro/AgentBench.

**Location**: `AgentBench/`

| Directory | Content |
|-----------|---------|
| `UnityCsReference/` | Unity engine C# source (VFX, XR, iOS) |
| `BuiltinShaders/` | Shader source (UnityCG.cginc, depth functions) |

**Key Use Cases**:
- Understanding Unity internals (VFX Graph API, XR subsystems)
- Depth conversion functions (`Linear01Depth`, `LinearEyeDepth`)
- iOS/Metal-specific bindings
- Compute shader patterns

**Reference**: `KnowledgeBase/_UNITY_SOURCE_REFERENCE.md`

---

## 🔍 For AI Assistants

1. **Search KB first** before implementing new features
2. **Check `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md`** for existing solutions
3. **Reference `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md`** for hologram/portal work
4. **Use `_UNITY_SOURCE_REFERENCE.md`** for Unity internals deep dive
5. **Log discoveries** to `LEARNING_LOG.md`

---

## 📄 License

MIT License - Knowledge bases and code snippets attributed to original repos.

---

**Repository**: https://github.com/imclab/Unity-XR-AI

**Maintained by**: James Tunick

**Last Updated**: 2026-01-16
