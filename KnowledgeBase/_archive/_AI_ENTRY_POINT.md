# AI Knowledgebase Entry Point

**Purpose**: Universal entry point for any AI tool (Claude, Gemini, Codex, Cursor, Windsurf, etc.)
**Location**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/`
**Symlinks**: `~/.claude/knowledgebase`, `~/.gemini/knowledgebase`, `~/.cursor/knowledgebase`, `~/.windsurf/knowledgebase`

---

## Quick Navigation

### By Task Type

| Need | File |
|------|------|
| **Global rules & memory** | `_GLOBAL_RULES_AND_MEMORY.md` |
| **IDE extensions & models** | `_IDE_EXTENSIONS_AND_MODELS.md` |
| **AI config files** | `_AI_CONFIG_FILES_REFERENCE.md` |
| **Shell config/aliases** | `_SHELL_CONFIG_REFERENCE.md` |
| **AI health checks** | `_AI_HEALTH_SCRIPTS.md` |
| **Unity debugging** | `_UNITY_DEBUGGING_REFERENCE.md` |
| **Unity + React Native** | `_UNITY_AS_A_LIBRARY_OVERVIEW.md`, `_REACT_NATIVE_UNITY_PACKAGES.md` |
| **AR/VFX development** | `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` |
| **Gaussian splatting** | `_GAUSSIAN_SPLATTING_AND_VIZ_TOOLS.md` |
| **Unity patterns (500+)** | `_UNITY_INTELLIGENCE_PATTERNS.md` - Say: **"Using Unity Intelligence patterns"** |
| **Patterns by interest** | `_UNITY_PATTERNS_BY_INTEREST.md` - Brushes, hand tracking, audio, depth |
| **WebGL patterns** | `_WEBGL_INTELLIGENCE_PATTERNS.md` - Say: **"Using WebGL Intelligence patterns"** |
| **3DVis patterns** | `_3DVIS_INTELLIGENCE_PATTERNS.md` - Say: **"Using 3DVis Intelligence patterns"** |
| **3D visualization** | `_3D_VISUALIZATION_KNOWLEDGEBASE.md` |
| **Unity MCP tools** | `_UNITY_MCP_DEV_HOOKS.md` |
| **Spec-driven dev** | `_SPEC_KIT_METHODOLOGY.md` |
| **AI memory patterns** | `_AI_MEMORY_SYSTEMS_DEEP_DIVE.md` |
| **GitHub repos** | `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` |
| **Project priorities** | `_JT_PRIORITIES.md` |
| **KB cross-links** | `_KB_CROSS_LINKS.md` - Full navigation map between all files |

### By Domain

| Domain | Files |
|--------|-------|
| **Unity** | `_UNITY_*.md`, `_ARFOUNDATION_*.md` |
| **Web/Viz** | `_WEBGPU_*.md`, `_3D_*.md`, `_ECHARTS_*.md` |
| **AI/Agents** | `_AI_*.md`, `_SPEC_KIT_*.md`, `_SELF_IMPROVING_*.md` |
| **Projects** | `_PORTALS_*.md`, `_H3M_*.md`, `_XRAI_*.md` |

---

## File Naming Convention

- `_` prefix = knowledgebase reference file
- `SCREAMING_CASE` = important/index files
- Domain prefix groups related files

---

## For AI Tools: How to Use This KB

1. **Start here** - Read this file first for navigation
2. **Task-specific** - Load only relevant files for current task
3. **Don't load all** - KB is 1.4MB/71 files, load on-demand
4. **Log discoveries** - Append to `LEARNING_LOG.md`
5. **Cross-reference** - Files link to each other

---

## Key Files Summary (~tokens)

| File | Tokens | Use Case |
|------|--------|----------|
| `_AI_ENTRY_POINT.md` | ~400 | Start here |
| `_QUICK_REFERENCE.md` | ~800 | Common patterns |
| `_UNITY_DEBUGGING_REFERENCE.md` | ~2K | Unity issues |
| `_UNITY_MCP_DEV_HOOKS.md` | ~3K | MCP tools |
| `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | ~4K | AR/VFX dev |
| `LEARNING_LOG.md` | ~2K | Discoveries |

---

## Logs & State

- **LEARNING_LOG.md** - Persistent discoveries across sessions
- **metrics.json** - KB health metrics
- **daily-notes-*.md** - Quick notes (auto-generated)

---

## Tool-Specific Notes

| Tool | Config Location | Notes |
|------|-----------------|-------|
| Claude Code | `~/.claude/CLAUDE.md` | References `~/GLOBAL_RULES.md` |
| Gemini | `~/.gemini/GEMINI.md` | Same KB access |
| Cursor | `~/.cursor/rules` | Uses `.cursorrules` |
| Windsurf | `~/.windsurf/` | Cascade rules |
| Codex | Project context | Load `_AI_ENTRY_POINT.md` |

---

---

## Cloud Access (GitHub)

**Repository**: `imclab/xrai`
**Branch**: `main`
**Path**: `KnowledgeBase/`

### Raw URL Pattern
```
https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/{filename}
```

### Quick Fetch Examples

```bash
# Fetch entry point
curl -s https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_AI_ENTRY_POINT.md

# Fetch quick reference
curl -s https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_QUICK_REFERENCE.md

# Fetch any file
curl -s "https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_UNITY_MCP_DEV_HOOKS.md"
```

### API Access (GitHub API)

```bash
# List KB files
curl -s "https://api.github.com/repos/imclab/xrai/contents/KnowledgeBase" | jq '.[].name'

# Get file content (base64)
curl -s "https://api.github.com/repos/imclab/xrai/contents/KnowledgeBase/_AI_ENTRY_POINT.md" | jq -r '.content' | base64 -d
```

### For AI Tools (Cloud Mode)

```python
# Python fetch
import requests
KB_BASE = "https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase"
def fetch_kb(filename):
    return requests.get(f"{KB_BASE}/{filename}").text

# Usage
entry = fetch_kb("_AI_ENTRY_POINT.md")
quick_ref = fetch_kb("_QUICK_REFERENCE.md")
```

```javascript
// JavaScript fetch
const KB_BASE = "https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase";
const fetchKB = async (filename) => (await fetch(`${KB_BASE}/${filename}`)).text();

// Usage
const entry = await fetchKB("_AI_ENTRY_POINT.md");
```

### Key Cloud URLs

| File | Raw URL |
|------|---------|
| Entry Point | [_AI_ENTRY_POINT.md](https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_AI_ENTRY_POINT.md) |
| Quick Ref | [_QUICK_REFERENCE.md](https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_QUICK_REFERENCE.md) |
| Unity MCP | [_UNITY_MCP_DEV_HOOKS.md](https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_UNITY_MCP_DEV_HOOKS.md) |
| Learning Log | [LEARNING_LOG.md](https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/LEARNING_LOG.md) |
| Cross-Links | [_KB_CROSS_LINKS.md](https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_KB_CROSS_LINKS.md) |

---

## Cross-Domain Navigation

### Intelligence Pattern Libraries
| Domain | Entry File | Cross-Links |
|--------|------------|-------------|
| **Unity/XR** | `_UNITY_INTELLIGENCE_PATTERNS.md` | `_ARFOUNDATION_VFX_*`, `_VFX25_*`, `_UNITY_MCP_*` |
| **WebGL/Web** | `_WEBGL_INTELLIGENCE_PATTERNS.md` | `_WEBGPU_*`, `_WEBXR_*`, `_WEB_*` |
| **3D Vis** | `_3DVIS_INTELLIGENCE_PATTERNS.md` | `_ECHARTS_*`, `_COSMOS_*`, `_VISUALIZATION_*` |
| **AI/Agents** | `_AI_AGENT_PHILOSOPHY.md` | `_CLAUDE_CODE_*`, `_MCP_*`, `_MULTI_AGENT_*` |

### Full Navigation Map
See `_KB_CROSS_LINKS.md` for complete file-to-file relationships.

---

*Updated: 2026-01-13*
