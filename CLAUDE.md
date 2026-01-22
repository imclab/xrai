# Unity-XR-AI Project

**Comprehensive Unity XR/AR/VR Development Knowledgebase + Visualization Tools**

This repository contains production-ready code patterns, 520+ GitHub repo references, 10 visualization frontends, and the MetavidoVFX Unity project.

---

## 📂 Repository Structure

```
Unity-XR-AI/
├── KnowledgeBase/           # 137 knowledge files, patterns, references
├── AgentBench/              # Unity research workbench (source code access)
├── Vis/                     # 10 3D visualization frontends
│
├── # UNITY PROJECTS
├── MetavidoVFX-main/        # Unity VFX project (AR Foundation + H3M)
├── Fluo-GHURT-main/         # Keijiro's Fluo controller/receiver system
├── SplatVFX/                # Gaussian Splatting for VFX Graph (keijiro)
├── TouchingHologram/        # HoloKit hand tracking + Buddha VFX (holoi)
├── TamagotchU/              # ML-Agents + Spine virtual pet (EyezLee)
├── HoloKitApp/              # Official HoloKit multi-reality app (holoi)
├── HoloKitMultiplayer/      # Colocated multiplayer boilerplate (holoi)
├── FaceTrackingVFX/         # ARKit face mesh → VFX Graph (mao-test-h)
├── LLMUnity/                # AI characters with local LLMs (undreamai)
│
├── mcp-server/              # MCP KB Server (TypeScript)
├── Scripts/                 # Utility scripts
├── specs/                   # ⚠️ DEPRECATED - Use MetavidoVFX-main/Assets/Documentation/specs/
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
| `MetavidoVFX-main/Assets/Documentation/ICOSA_INTEGRATION.md` | Voice-to-object 3D model integration |
| `MetavidoVFX-main/Assets/Documentation/specs/README.md` | Spec-Kit index (002-015) |
| `MetavidoVFX-main/Assets/Documentation/specs/MASTER_DEVELOPMENT_PLAN.md` | 17-sprint implementation roadmap |

---

## 📊 Statistics (Updated 2026-01-22)

- **KnowledgeBase**: 137 markdown files (updated 2026-01-22)
- **Auto-Fix Patterns**: 106 (80% auto-apply rate)
- **GitHub Repos**: 520+ curated (ARFoundation, VFX, DOTS, Networking, ML/AI)
- **Vis Projects**: 10 (xrai-kg, HOLOVIS, cosmos-*, WarpDashboard, chalktalk)
- **Code Snippets**: 50+ production-ready patterns
- **Platform Coverage**: iOS 15+, Android, Quest 3/Pro, WebGL, Vision Pro
- **MetavidoVFX Scripts**: 179 C# scripts (129 runtime + 50 editor)
- **VFX Assets**: 432 total (292 primary in Assets/VFX)
- **Scenes**: 25 custom (5 HOLOGRAM + 10 spec demos + 10 other)
- **Specs**: 14 total (002-015), 6 complete, 4 in progress, 4 draft
- **Unity Version**: 6000.2.14f1, AR Foundation 6.2.1, VFX Graph 17.2.0
- **Performance**: 353 FPS @ 10 VFX (verified Jan 21, 2026)

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

### Core Architecture (Updated 2026-01-20)

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
- **3D Model Integration**: WhisperIcosaController (voice-to-object), IcosaAssetLoader (glTF import)

**Documentation**: `MetavidoVFX-main/Assets/Documentation/README.md`

### Bug Fixes Applied (Jan 2026)

See `MetavidoVFX-main/Assets/Documentation/CODEBASE_AUDIT_2026-01-15.md` for details:
1. ✅ **Thread Dispatch Mismatch** - Fixed: uses dynamic thread group size queries
2. ✅ **Integer Division Truncation** - Fixed: HumanParticleVFX uses `CeilToInt()`
3. ✅ **Memory Leak** - Fixed: RenderTexture release in OnDestroy()
4. ✅ **VFXARBinder ExposedProperty** - Fixed: uses `ExposedProperty` instead of `const string` for proper VFX Graph property resolution
5. ✅ **ReadPixels Bounds Errors** - Fixed: VFXPhysicsBinder/VelocityVFXBinder validate `IsCreated()` before ReadPixels
6. ✅ **Editor Mock Textures** - Added: ARDepthSource provides mock textures for Editor testing without AR device
7. ✅ **AR Texture Access Crash** - Fixed: TryGetTexture pattern in 6 files (spec 005-ar-texture-safety)

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

## 🆕 Projects Migrated (2026-01-17)

| Project | Source | Key Technologies |
|---------|--------|------------------|
| **SplatVFX** | keijiro/SplatVFX | Gaussian Splatting, VFX Graph, URP 17 |
| **TouchingHologram** | holoi/touching-hologram | HoloKit SDK, Hand Tracking, 24 Buddha VFX |
| **TamagotchU** | EyezLee/TamagotchU_Unity | ML-Agents, Spine 4.3, Dynamic Bone, VATBaker |
| **HoloKitApp** | holoi/holokit-app | Multi-reality AR, Netcode, MPC, Apple Watch |
| **HoloKitMultiplayer** | holoi/holokit-colocated-multiplayer | Colocated AR, Image Marker Alignment |
| **FaceTrackingVFX** | mao-test-h/FaceTracking-VFX | ARKit Face Mesh, Smrvfx, VFX Graph |
| **LLMUnity** | undreamai/LLMUnity | Local LLMs, RAG, AI Characters, Mobile |

### KB Files Added
- `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` - Photon/Normcore/coherence comparison
- `_WEBXR_DEVICE_API_EXPLAINER.md` - WebXR + unity-webxr-export

---

## 📋 Next Steps (Updated 2026-01-22)

### Active Development (Spec-Driven)

**Sprint 0** (✅ complete): Debug Infrastructure
- ✅ DebugFlags.cs with conditional attributes
- ✅ DebugConfig.cs with category filtering
- ✅ WebcamMockSource for Editor testing

**Sprint 8-10** (in progress, 70%): Icosa/Sketchfab Integration (Spec 009)
- ✅ SketchfabClient.cs - Sketchfab Download API wrapper
- ✅ ModelCache.cs - LRU disk caching for models
- ✅ UnifiedModelSearch.cs - Aggregate Icosa + Sketchfab results
- ✅ ModelSearchUI.cs, ModelPlacer.cs, IcosaAssetMetadata.cs
- ✅ Voice integration (WhisperIcosaController)
- ⬜ GLTFast runtime loading (pending)

**Spec 014** (in progress, 50%): HiFi Hologram VFX
- ✅ HiFiHologramController for quality presets
- ⬜ RGB color sampling refinement
- ⬜ Quality preset profiles (Low/Medium/High/Ultra)

**Spec 003** (in progress, 60%): Hologram Conferencing
- ✅ Recording/playback
- ⬜ WebRTC signaling server
- ⬜ Multi-peer support

### Completed Specs (6 total)
- ✅ Spec 002 - H3M Hologram Foundation (Legacy, use Hologram.prefab)
- ✅ Spec 004 - MetavidoVFX Systems
- ✅ Spec 005 - AR Texture Safety
- ✅ Spec 006 - VFX Library & Pipeline (73 VFX, 353 FPS)
- ✅ **Spec 007 - VFX Multi-Mode** (all 6 phases, audio/physics)
- ✅ **Spec 012 - Hand Tracking + Brush** (5 providers, gestures, tests)
- ✅ Spec 015 - VFX Binding Architecture

### Integration Opportunities
- **Voice-to-Object** - "Put a cat here" → Icosa/Sketchfab search → AR placement
- **Gaussian Splatting + AR** - SplatVFX in AR Foundation context
- **Hand Tracking + MetavidoVFX** - Spec 012 unifies HoloKit + XRHands
- **Colocated Multiplayer** - Apply HoloKitMultiplayer patterns (Spec 010)

---

## 🔌 API Access

### REST API
```bash
node api/kb-api.js  # Start on port 3847

# Endpoints
GET /api/search?q=query  # Search KB
GET /api/files           # List files
GET /api/file/:name      # Get file
GET /api/patterns        # Auto-fix patterns
GET /api/stats           # Statistics
```

### MCP Server
```bash
cd mcp-server && npm start  # KB semantic search
# Tools: kb_search, kb_get_repo, kb_get_snippet, kb_stats
```

### GitHub Raw Access
```
https://raw.githubusercontent.com/imclab/Unity-XR-AI/main/KnowledgeBase/_AUTO_FIX_PATTERNS.md
```

### Symlinks (AI CLIs)
```
~/.claude/knowledgebase → KnowledgeBase/
~/.windsurf/knowledgebase → KnowledgeBase/
~/.cursor/knowledgebase → KnowledgeBase/
~/.codex/knowledgebase → KnowledgeBase/
```

**Full Guide**: `KnowledgeBase/_KB_ACCESS_GUIDE.md`

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

**Last Updated**: 2026-01-21
