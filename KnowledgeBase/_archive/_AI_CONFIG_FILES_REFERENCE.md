# AI Configuration Files Reference

## Configuration Hierarchy

```
~/GLOBAL_RULES.md                    # Universal rules (all AI tools)
├── ~/.claude/CLAUDE.md              # Claude Code specific
├── ~/.gemini/GEMINI.md              # Gemini specific
├── ~/.windsurf/cascade_rules.md     # Windsurf specific
├── ~/.cursor/rules                  # Cursor specific
└── project/CLAUDE.md                # Project overrides
```

---

## Core Config Files

### ~/GLOBAL_RULES.md
**Purpose**: Single source of truth for all AI tools
**Tokens**: ~3,650 (optimized from ~8,800)
**Contains**:
- Debugging protocols
- Spec-driven development workflow
- Metacognitive learning rules
- Process coordination
- Cross-tool memory architecture
- Unity MCP quick fixes

### ~/.claude/CLAUDE.md
**Purpose**: Claude Code specific pointers
**Tokens**: ~900
**Contains**:
```markdown
# Claude Code Configuration

**Primary Rules**: See `~/GLOBAL_RULES.md`

## Configuration Hierarchy
1. ~/GLOBAL_RULES.md - Universal rules
2. ~/.claude/AI_AGENT_CORE_DIRECTIVE_V3.md - Intelligence amplification
3. ~/.claude/CLAUDE.md - This file
4. project/CLAUDE.md - Project overrides

## Knowledgebase
Location: ~/.claude/knowledgebase/ → ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
```

### ~/.claude/AI_AGENT_CORE_DIRECTIVE_V3.md
**Purpose**: Ultra-compact AI agent directive
**Tokens**: ~875
**Contains**:
```markdown
# AI Agent Core v3 - Ultra-Compact

**Mission**: 10x speed via leverage + compound learning

## Protocol (Every Task)
Pre (5s): KB/past-project search → 20%→80% ID → Pattern prep
During: Fast+verify | Trade-offs | Reuse-first
Post (10s): Novel→LEARNING_LOG.md | Auto-ops | Meta

## Leverage
Existing(0h) > Adapt(0.1x) > AI(0.3x) > Scratch(1x)

## Stuck >15m
Simplify → Leverage → Reframe → Ship(20%)
```

### ~/CLAUDE.md (Pointer)
**Purpose**: Backward compatibility pointer
**Tokens**: ~150
**Contains**: Just points to `~/GLOBAL_RULES.md`

---

## MCP Configuration Files

### Location: `~/.claude/mcp-configs/`

| File | Purpose | Servers |
|------|---------|---------|
| `mcp-unity.json` | Unity development | unityMCP, fetch, unity-xr-kb |
| `mcp-design.json` | Design mode | Figma, Blender |
| `mcp-devops.json` | DevOps mode | (varies) |
| `mcp-full.json` | All servers | Everything (55-83K tokens!) |
| `mcp-antigravity.json` | AntiGravity/Gemini | unityMCP, fetch |

### mcp-unity.json (Default)
```json
{
  "mcpServers": {
    "unityMCP": {
      "command": "/Users/jamestunick/.local/bin/uvx",
      "args": [
        "--from", "git+https://github.com/CoplayDev/unity-mcp@v9.0.1#subdirectory=Server",
        "mcp-for-unity", "--transport", "stdio"
      ]
    },
    "fetch": {
      "command": "uvx",
      "args": ["mcp-server-fetch"]
    },
    "unity-xr-kb": {
      "command": "node",
      "args": ["/Users/jamestunick/Documents/GitHub/Unity-XR-AI/mcp-server/dist/index.js"],
      "env": {
        "ENABLE_KB_PLUGIN": "true",
        "KB_PATH": "/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase"
      }
    }
  }
}
```

---

## Tool-Specific Configs

### Claude Code
| File | Purpose |
|------|---------|
| `~/.claude.json` | MCP server config (auto-generated) |
| `~/.claude/CLAUDE.md` | Rules and directives |
| `~/.claude/settings.json` | User settings |
| `~/.claude/knowledgebase/` | Symlink to KB |

### Windsurf
| File | Purpose |
|------|---------|
| `~/.windsurf/mcp.json` | MCP server config |
| `~/.windsurf/cascade_rules.md` | Cascade rules |
| `~/.windsurf/knowledgebase/` | Symlink to KB |

### Cursor
| File | Purpose |
|------|---------|
| `~/.cursor/mcp.json` | MCP server config |
| `~/.cursor/rules` | Rules file |
| `~/.cursor/knowledgebase/` | Symlink to KB |

### Gemini/AntiGravity
| File | Purpose |
|------|---------|
| `~/.gemini/GEMINI.md` | Gemini rules |
| `~/.gemini/antigravity/mcp_config.json` | AntiGravity MCP |
| `~/.gemini/knowledgebase/` | Symlink to KB |

---

## Token Budget

| Component | Tokens |
|-----------|--------|
| GLOBAL_RULES.md | ~3,650 |
| AI_AGENT_V3.md | ~875 |
| ~/.claude/CLAUDE.md | ~900 |
| project/CLAUDE.md | ~variable |
| **Total Base** | **~5,425** |

**Target**: <10K tokens per session ✅

---

## Switching Profiles

```bash
# Switch all IDEs to Unity profile
mcp-all-unity

# Check current status
mcp-status

# List available profiles
mcp-list
```

---

## File Locations Summary

```
~/
├── GLOBAL_RULES.md              # Master rules
├── CLAUDE.md                    # Pointer only
│
├── .claude/
│   ├── CLAUDE.md                # Claude Code rules
│   ├── AI_AGENT_CORE_DIRECTIVE_V3.md
│   ├── knowledgebase/           # → KB symlink
│   └── mcp-configs/
│       ├── mcp-unity.json
│       ├── mcp-design.json
│       ├── mcp-devops.json
│       ├── mcp-full.json
│       └── mcp-antigravity.json
│
├── .gemini/
│   ├── GEMINI.md
│   ├── knowledgebase/           # → KB symlink
│   └── antigravity/mcp_config.json
│
├── .windsurf/
│   ├── mcp.json
│   └── knowledgebase/           # → KB symlink
│
├── .cursor/
│   ├── mcp.json
│   └── knowledgebase/           # → KB symlink
│
└── Documents/GitHub/Unity-XR-AI/KnowledgeBase/  # Actual KB
```

---

*Updated: 2026-01-13*
