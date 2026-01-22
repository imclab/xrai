# Tool Integration Map

**Version**: 1.0 (2026-01-21)
**Purpose**: Unified visibility across all AI tools, configs, and workflows.

---

## Configuration Hierarchy

```
~/GLOBAL_RULES.md                    ← Single Source of Truth (all tools)
├── ~/.claude/CLAUDE.md              ← Claude Code specific
├── ~/.windsurf/CLAUDE.md            → symlink to ~/CLAUDE.md
├── ~/.cursor/CLAUDE.md              → symlink to ~/CLAUDE.md
└── project/CLAUDE.md                ← Project overrides
```

## Shared Resources

### Knowledgebase (Symlinked Everywhere)
```
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
├── ~/.claude/knowledgebase/         → symlink
├── ~/.windsurf/knowledgebase/       → symlink
└── ~/.cursor/knowledgebase/         → symlink
```

**Key Files**:
| File | Purpose |
|------|---------|
| `_TOKEN_EFFICIENCY_COMPLETE.md` | Token optimization strategies |
| `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` | 520+ GitHub repos |
| `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` | 50+ code patterns |
| `PLATFORM_COMPATIBILITY_MATRIX.md` | Platform support |
| `LEARNING_LOG.md` | Discoveries log |

### Memory System (claude-mem)
```
~/.claude-mem/
├── claude-mem.db                    ← SQLite database
├── chroma/                          ← Vector embeddings
├── hooks/                           ← Session hooks
│   ├── session-start.js
│   ├── stop.js
│   └── user-prompt-submit.js
└── settings.json
```

**MCP Integration**: `mcp__claude-mem__*` tools

---

## Tool Configurations

### Claude Code (`~/.claude/`)
```
├── settings.json                    ← Main config
├── settings.local.json              ← Local overrides
├── agents/                          ← 14 custom agents
│   ├── unity-error-fixer.md         (Haiku)
│   ├── code-searcher.md             (Haiku)
│   ├── research-agent.md            (Sonnet)
│   └── ...
├── hooks/
│   └── filter-test-output.sh        ← Test output filter
├── commands/                        ← Custom commands
├── plugins/                         ← Enabled plugins
└── mcp-configs/                     ← MCP server configs
```

### Windsurf (`~/.windsurf/`)
```
├── CLAUDE.md                        → symlink to ~/CLAUDE.md
├── knowledgebase/                   → symlink to KB
├── mcp.json                         ← MCP servers
└── extensions/                      ← VS Code extensions
```

### Cursor (`~/.cursor/`)
```
├── CLAUDE.md                        → symlink to ~/CLAUDE.md
├── knowledgebase/                   → symlink to KB
├── mcp.json                         ← MCP servers
└── extensions/                      ← VS Code extensions
```

---

## MCP Servers

### Essential (Keep Active)
| Server | Port | Purpose |
|--------|------|---------|
| UnityMCP | 6400 | Unity Editor integration |
| jetbrains | - | Rider indexed search |
| claude-mem | - | Persistent memory |
| github | - | Repo operations |

### Optional (Disable When Unused)
| Server | Purpose | Token Cost |
|--------|---------|------------|
| fetch | URL fetching | 5-10K |
| filesystem | File ops | 5-10K |
| memory | Legacy memory | 5-10K |
| blender | Blender MCP | 5-10K |
| TalkToFigma | Figma MCP | 5-10K |

### Config Locations
- Claude Code: `~/.claude/settings.json` + `~/.claude/mcp-configs/`
- Windsurf: `~/.windsurf/mcp.json`
- Cursor: `~/.cursor/mcp.json`

---

## CLI Tools

### AI CLIs
| Tool | Model | Context | Cost |
|------|-------|---------|------|
| `claude` | Claude 4 | 200K | $$$ |
| `gemini` | Gemini 2.0 | 1M | FREE tier |
| `codex` | GPT-4 Turbo | 128K | $$ |
| `antigravity` | Various | - | - |

### Integration Commands
```bash
# Claude Code
claude --print                       # Headless mode
claude mcp list                      # List MCP servers
claude mcp remove <server>           # Remove server

# Gemini CLI
gemini -m flash                      # Fast model
gemini -m pro                        # Full model

# Codex CLI
codex --approval-mode full-auto      # Autonomous
codex --model gpt-4-turbo           # Model selection
```

---

## IDE Integration

### JetBrains Rider
```
MCP Tools (when Rider open):
├── search_in_files_by_text          ← 10x faster than Grep
├── find_files_by_name_keyword       ← 5x faster than Glob
├── get_file_text_by_path            ← With truncation
├── get_symbol_info                  ← Native LSP
└── rename_refactoring               ← Project-wide
```

### Unity Editor
```
MCP Tools:
├── read_console                     ← Error checking
├── manage_editor                    ← Play/pause/stop
├── manage_scene                     ← Scene operations
├── find_gameobjects                 ← Object search
├── manage_components                ← Component ops
├── run_tests                        ← Test execution
└── batch_execute                    ← Batch operations
```

---

## Agents (Cross-Tool)

### Token-Efficient Agents (`~/.claude/agents/`)
| Agent | Model | Use Case |
|-------|-------|----------|
| `unity-error-fixer` | Haiku | Fix compilation errors |
| `unity-console-checker` | Haiku | Check console |
| `code-searcher` | Haiku | JetBrains indexed search |
| `vfx-tuner` | Haiku | VFX property changes |
| `test-runner` | Haiku | Run Unity tests |
| `monitor-agent` | Haiku | Health checks |
| `mcp-tools-specialist` | Haiku | Unity MCP ops |
| `logger-agent` | Haiku | Add logging |
| `osx-helper` | Haiku | macOS tasks |
| `research-agent` | Sonnet | KB research |
| `tech-lead` | Sonnet | Architecture |
| `code-tester` | Sonnet | Verify implementations |
| `orchestrator-agent` | Sonnet | Coordinate tasks |

---

## Workflows

### Primary: Rider + Claude Code + Unity

```
1. Error Fix (3 calls):
   Unity: read_console → JetBrains: get_file → Claude: Edit

2. Feature (4 calls):
   JetBrains: search → JetBrains: read → Claude: Edit → Unity: console

3. Debug (4 calls):
   Unity: console → Unity: find_gameobjects → Unity: components → JetBrains: read
```

### Multi-AI Orchestration

```
Gemini (FREE, 1M context):     Research, large docs
Claude ($$, 200K context):     Implementation, code
Codex ($$, 128K context):      Quick fixes, scripting
```

---

## Token Efficiency

### Session Management
| Command | When | Savings |
|---------|------|---------|
| `/cost` | Check usage | - |
| `/clear` | New task | 10-50K |
| `/compact <focus>` | Context >100K | 20-80% |
| `/context` | See MCP overhead | - |
| `/mcp` | Disable servers | 5-25K each |

### Environment Variables
```json
{
  "ENABLE_TOOL_SEARCH": "auto:5",
  "MAX_THINKING_TOKENS": "10000",
  "CLAUDE_CODE_MAX_OUTPUT_TOKENS": "16384"
}
```

---

## Quick Commands

### Session Start
```bash
# Kill duplicate MCP processes
mcp-kill-dupes

# Check MCP status
lsof -i :6400              # Unity MCP
claude mcp list            # All servers

# Remove redundant MCPs
claude mcp remove memory filesystem fetch
```

### Health Check
```bash
# Unity console
read_console(types=["error"], count=5)

# MCP status
manage_editor(action="get_state")
```

---

## File Locations Reference

| Type | Location |
|------|----------|
| Global Rules | `~/GLOBAL_RULES.md` |
| Claude Config | `~/.claude/CLAUDE.md` |
| Token Efficiency | `KnowledgeBase/_TOKEN_EFFICIENCY_COMPLETE.md` |
| Integration Map | `KnowledgeBase/_TOOL_INTEGRATION_MAP.md` |
| Agents | `~/.claude/agents/` |
| MCP Configs | `~/.claude/mcp-configs/`, `~/.windsurf/mcp.json` |
| Memory DB | `~/.claude-mem/claude-mem.db` |
| Hooks | `~/.claude/hooks/`, `~/.claude-mem/hooks/` |

---

## Maintenance

### Weekly
- [ ] Check `/cost` or `/stats`
- [ ] Review `LEARNING_LOG.md`
- [ ] Clean old todo files
- [ ] Verify symlinks work

### Monthly
- [ ] Review and trim CLAUDE.md (<500 lines)
- [ ] Archive old memories
- [ ] Update MCP servers
- [ ] Review agent effectiveness

---

**Last Updated**: 2026-01-21
