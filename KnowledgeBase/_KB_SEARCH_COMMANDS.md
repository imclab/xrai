# KnowledgeBase Search Commands

**Tags**: `search` `grep` `knowledgebase` `all-tools` `holistic`
**Updated**: 2026-01-21
**Purpose**: Universal search commands for all AI tools and IDEs

---

## Decision Flow (Holistic - Always Check KB First)

```
Error/Question arrives
        │
        ▼
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│ 1. kbfix/kbtag  │────▶│ 2. Found?       │────▶│ 3. Apply direct │
│    (0 tokens)   │ No  │    grep KB      │ Yes │    (done)       │
└─────────────────┘     └────────┬────────┘     └─────────────────┘
                                 │ No
                                 ▼
                        ┌─────────────────┐
                        │ 4. MCP/Agent?   │
                        │ JetBrains/Unity │
                        └────────┬────────┘
                                 │
              ┌──────────────────┼──────────────────┐
              ▼                  ▼                  ▼
     ┌────────────────┐ ┌────────────────┐ ┌────────────────┐
     │ JetBrains MCP  │ │ Unity MCP      │ │ Explore Agent  │
     │ C# code search │ │ Editor state   │ │ Multi-file     │
     │ (~100 tokens)  │ │ (~50 tokens)   │ │ (independent)  │
     └────────────────┘ └────────────────┘ └────────────────┘
```

---

## Quick Lookup (Zero Tokens)

```bash
# Error → Fix (instant lookup)
grep -i "CS0246\|NullRef\|MCP timeout" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md

# Find pattern file by topic
grep -i "vfx\|hologram\|hand" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md

# Search all pattern files
grep -ri "ExposedProperty\|TryGetTexture" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_*.md
```

---

## By Use Case

### Fix an Error
```bash
# Step 1: Check quick fix table
grep -i "error message" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md

# Step 2: If not found, search auto-fix patterns
grep -ri "error message" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_AUTO_FIX_PATTERNS.md
```

### Find a Pattern
```bash
# Step 1: Check pattern tags index
grep -i "topic" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md

# Step 2: Search specific pattern file
grep -i "topic" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md
```

### Find GitHub Repo
```bash
# Search 520+ curated repos
grep -i "hand tracking\|gaussian" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
```

### Find Code Snippet
```bash
# AR Foundation + VFX patterns
grep -i "depth\|segmentation" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md

# Unity compute/shader patterns
grep -i "dispatch\|buffer" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_UNITY6_HLSL_COMPUTE_SHADER_GUIDE.md
```

---

## Topic-Specific Searches

### VFX / Hologram
```bash
grep -ri "vfx\|particle\|hologram" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_VFX*.md
grep -i "portal\|depth" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md
```

### AR / Hand Tracking
```bash
grep -ri "ARFoundation\|hand\|tracking" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_AR*.md
grep -i "gesture\|pinch" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_HAND_SENSING_CAPABILITIES.md
```

### MCP / Unity Automation
```bash
grep -i "batch_execute\|read_console" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_UNITY_MCP_QUICK_REFERENCE.md
grep -i "mcp\|tool" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md
```

### Performance / Token Optimization
```bash
grep -i "token\|optimization" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_TOKEN_EFFICIENCY_COMPLETE.md
grep -i "fps\|memory\|performance" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PERFORMANCE_PATTERNS_REFERENCE.md
```

### WebGL / Three.js
```bash
grep -ri "webgl\|three\|webgpu" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_WEBGL*.md
```

---

## Multi-Pattern Search (Regex)

```bash
# Search multiple terms at once
grep -Ei "ExposedProperty|TryGetTexture|CeilToInt" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/*.md

# Search with context (3 lines before/after)
grep -B3 -A3 -i "pattern" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md

# Count matches per file
grep -c "vfx" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_*.md | grep -v ":0$"

# List files containing term
grep -l "hologram" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/*.md
```

---

## Aliases (Add to ~/.zshrc or ~/.bashrc)

```bash
# Quick KB search
alias kb='grep -ri --include="*.md" "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/'
alias kbfix='grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md'
alias kbtag='grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md'
alias kbrepo='grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md'

# Functions for more complex searches
kb() { grep -ri --include="*.md" "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/; }
kbfix() { grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md; }
kbtag() { grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md; }
kbrepo() { grep -i "$1" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md; }
```

---

## IDE-Specific

### VS Code / Cursor / Windsurf
```
Ctrl+Shift+F (Cmd+Shift+F on Mac)
Search in: KnowledgeBase/**/*.md
```

### Rider / IntelliJ
```
Ctrl+Shift+F → Scope: KnowledgeBase
File mask: *.md
```

### Terminal (Any Tool)
```bash
# From any directory
grep -ri "search term" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
```

---

## AI Tool Integration

### Claude Code
```
"Search KB for [topic]"
→ Uses grep internally (0 tokens)
```

### Gemini CLI
```
# Paste this context:
"Search ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/ for [topic]"
```

### Codex CLI
```
# Same as Gemini - shared filesystem
grep -ri "topic" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
```

---

## Key Files Reference

| Purpose | Command |
|---------|---------|
| Error fixes | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md` |
| Pattern index | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md` |
| MCP reference | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_UNITY_MCP_QUICK_REFERENCE.md` |
| GitHub repos | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` |
| Token tips | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_TOKEN_EFFICIENCY_COMPLETE.md` |
| Tool rollover | `cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_CROSS_TOOL_ROLLOVER_GUIDE.md` |

---

---

## Screenshot Commands (Visual Context)

```bash
# Automated screenshot capture (then Ctrl+V to paste in Claude Code)
ss                    # Interactive screenshot → clipboard
ssu                   # Unity window screenshot → clipboard
ai-screenshot         # Full version with notification
ai-screenshot-unity   # Unity-specific capture
```

**Saves**: ~500-2000 tokens vs describing visually

**Best for**: Errors, UI issues, visual bugs, design references

---

## Priority Order (Fastest First)

| Priority | Command | Tokens | Speed | Use Case |
|----------|---------|--------|-------|----------|
| 1 | `kbfix "error"` | **0** | Instant | Known errors |
| 2 | `kbtag "topic"` | **0** | Instant | Find pattern files |
| 3 | `kb "search"` | **0** | <1s | Full KB search |
| 4 | `kbrepo "topic"` | **0** | <1s | GitHub repos |
| 5 | `ss` / `ssu` | **0** | 2s | Visual context |
| 6 | JetBrains MCP | ~100 | 1s | C# code search |
| 7 | Unity MCP | ~50 | 1s | Editor state |
| 8 | Explore agent | Independent | 5s | Multi-file search |
| 9 | claude-mem | ~500 | 2s | Semantic recall |
| 10 | WebSearch | ~1000 | 3s | External info |

---

## See Also

- `_QUICK_FIX.md` - Error → Fix lookup table
- `_PATTERN_TAGS.md` - Pattern file index by tag
- `_CROSS_TOOL_ROLLOVER_GUIDE.md` - Tool switching guide
- `GLOBAL_RULES.md` - Universal rules (~/GLOBAL_RULES.md)
