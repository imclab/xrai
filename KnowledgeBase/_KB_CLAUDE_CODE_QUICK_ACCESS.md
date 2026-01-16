# KnowledgeBase Quick Access for Claude Code

**Version**: 2.0 | **Last Updated**: 2026-01-15 | **Files**: 73+ | **Size**: 1.74MB

---

## Instant Navigation by Task

### Unity XR/AR/VFX Development
| Task | Primary File | Backup |
|------|--------------|--------|
| Find existing solutions | `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` (530+ repos) | Search first! |
| AR Foundation patterns | `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` (50+ snippets) | `CodeSnippets/` |
| VFX Graph + AR | `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` | Keijiro repos |
| Hologram systems | `_HOLOGRAM_RECORDING_PLAYBACK.md` | `_H3M_HOLOGRAM_ROADMAP.md` |
| Hand tracking | `_HAND_SENSING_CAPABILITIES.md` | Session checkpoints |
| Platform support | `PLATFORM_COMPATIBILITY_MATRIX.md` | iOS/Android/Quest/WebGL |
| Performance | `_PERFORMANCE_PATTERNS_REFERENCE.md` | `_UNITY_URP_OPTIMIZATION.md` |

### MetavidoVFX Project Specifics
| Need | Location |
|------|----------|
| Project docs | `MetavidoVFX-main/Assets/Documentation/README.md` |
| VFX properties | `MetavidoVFX-main/Assets/Documentation/QUICK_REFERENCE.md` |
| Architecture | `MetavidoVFX-main/Assets/Documentation/SYSTEM_ARCHITECTURE.md` |
| Project rules | `MetavidoVFX-main/CLAUDE.md` |

### Web/3D Visualization
| Task | File |
|------|------|
| Three.js/WebGL | `_WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md` |
| Web standards | `_WEB_INTEROPERABILITY_STANDARDS.md` (230K - load selectively!) |
| 3D graphs | `_3DVIS_INTELLIGENCE_PATTERNS.md` |
| ECharts | `_ECHARTS_VISUALIZATION_PATTERNS.md` |
| Vis projects | `_VISUALIZATION_RESOURCES_INDEX.md` (12 tools) |

### Project Planning & Priorities
| Task | File |
|------|------|
| Current priorities | `_JT_PRIORITIES.md` (P0-P4 roadmap) |
| Feature specs | `_ADVANCED_AR_FEATURES_IMPLEMENTATION_PLAN.md` (12 features) |
| Hologram roadmap | `_H3M_HOLOGRAM_ROADMAP.md` (5 phases) |
| Portal architecture | `_PORTALS_V4_ARCHITECTURE.md` |

### AI Tool Configuration
| Task | File |
|------|------|
| Tool registry | `_MASTER_AI_TOOLS_REGISTRY.md` |
| Config hierarchy | `_AI_CONFIG_FILES_REFERENCE.md` |
| Memory systems | `_SELF_IMPROVING_MEMORY_ARCHITECTURE.md` |
| Claude workflows | `_CLAUDE_CODE_WORKFLOW_OPTIMIZATION.md` |

---

## Critical Files (Load These First)

```
ALWAYS search before implementing:
├── _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md  → 530+ curated repos by category
├── _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md   → 50+ production code patterns
└── LEARNING_LOG.md                        → Recent discoveries (append-only)

Session continuity:
├── SESSION_CHECKPOINT_*.md                → Last session state
└── _JT_PRIORITIES.md                      → Current focus areas
```

---

## Code Snippets Directory

Production-ready implementations in `CodeSnippets/`:

```
ARFoundationDepthProcessor.compute     - GPU depth processing
ARFoundationVFXBridge2.cs              - AR → VFX binding
AudioReactiveVFXiOS.cs                 - Audio → VFX (iOS)
DepthToPointCloud.compute              - LiDAR → point cloud
DepthToPointCloudManager.cs            - Point cloud management
HumanDepthToVFX.cs                     - Human segmentation → VFX
OptimalARVFXBridge.cs                  - Optimized AR bridge
PeopleOcclusionVFXManager.cs           - Body occlusion VFX
```

---

## Search Patterns

### Finding Repos by Technology
```bash
# In _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md:
- Hand tracking → search "hand" or "gesture"
- Gaussian splat → search "splat" or "3DGS"
- Audio reactive → search "audio" or "sound"
- Networking → search "normcore" or "netcode"
```

### Finding Code Patterns
```bash
# In _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md:
- Depth textures → search "depth" or "LiDAR"
- Body tracking → search "body" or "skeleton"
- Point clouds → search "point cloud" or "GraphicsBuffer"
```

---

## Knowledge Categories (73 Files)

| Category | Files | Key Content |
|----------|-------|-------------|
| **Master Indices** | 5 | Navigation, cross-links, repo database |
| **AR/VFX/Hologram** | 8 | Production patterns, H3M roadmaps |
| **Unity Patterns** | 8 | Debugging, optimization, URP |
| **Web Development** | 8 | Three.js, WebGL, WebXR |
| **Project Config** | 4 | Setup, scripts, configuration |
| **AI Development** | 7 | Memory, agents, Claude workflows |
| **Platform Guides** | 7 | iOS, Android, React Native |
| **MCP/Protocol** | 4 | MCP servers, SDK patterns |
| **Planning** | 3 | Priorities, roadmaps, specs |
| **Quick References** | 5 | Cheat sheets, summaries |
| **Advanced Systems** | 5 | Cloud, multi-agent, hypergraph |
| **Tool References** | 6 | Hooks, subagents, IDE tools |
| **Code Snippets** | 10 | Production C# + compute shaders |

---

## Token Budget Guidelines

| Session Type | Budget | Load |
|--------------|--------|------|
| Quick fix | 5-10K | Just the target file |
| New feature | 15-20K | GitHub KB + AR patterns + 1 specific guide |
| Research | 30-40K | Multiple guides + web search |
| Architecture | 40-60K | Full KB sections |

---

## File Paths (Absolute)

```bash
KB="/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase"

# Master references
$KB/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
$KB/_MASTER_KNOWLEDGEBASE_INDEX.md
$KB/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md

# Project-specific
$KB/_JT_PRIORITIES.md
$KB/_H3M_HOLOGRAM_ROADMAP.md
$KB/_PORTALS_V4_ARCHITECTURE.md

# Logs
$KB/LEARNING_LOG.md
$KB/SESSION_CHECKPOINT_*.md
```

---

## Quick Commands

### Discover existing solutions
1. Search `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` for technology
2. Check `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` for patterns
3. Look in `CodeSnippets/` for implementations

### Start new session
1. Read latest `SESSION_CHECKPOINT_*.md`
2. Check `_JT_PRIORITIES.md` for current focus
3. Scan recent `LEARNING_LOG.md` entries

### Log discovery
Append to `LEARNING_LOG.md`:
```markdown
## YYYY-MM-DD HH:MM - Tool - Title

**Discovery**: What was learned
**Context**: Why it matters
**Impact**: How it helps
**Related**: Cross-references
```

---

## Maintenance Scripts

```bash
# In KnowledgeBase/scripts/
KB_RESEARCH_AND_UPDATE.sh      # Update knowledge files
auto_cross_link_configs.sh     # Generate cross-references
generate-kb-index.sh           # Regenerate navigation index
KB_AUDIT.sh                    # Audit KB health
KB_OPTIMIZE.sh                 # Optimize token usage
```

---

**Remember**: Search the KB before implementing. The answer is probably already here.
