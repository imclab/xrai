# Master Configuration Registry - All AI Tools & IDEs

**Last Updated**: 2026-01-08
**Purpose**: Central registry of all config files, rules, and paths across entire system

---

## 🗺️ Configuration Map

### Claude Code
| Type | Path | Purpose | Auto-Update |
|------|------|---------|-------------|
| Global Rules | `~/.claude/CLAUDE.md` | All projects | ✅ Via script |
| Project Rules | `~/Documents/GitHub/portals_v4/CLAUDE.md` | Project-specific | ✅ Symlink |
| MCP Config | `~/.windsurf/mcp.json` | Shared with Windsurf | ✅ Via alias |
| MCP Profiles | `~/.claude/mcp-configs/*.json` | Context switcher | ✅ Via alias |

### Windsurf
| Type | Path | Purpose | Auto-Update |
|------|------|---------|-------------|
| Global Rules | Built into Cascade | Context awareness | N/A (native) |
| MCP Config | `~/.windsurf/mcp.json` | MCP servers | ✅ Via alias |
| Fast Context | Built-in (SWE-grep) | 20x faster search | N/A (native) |

### Cursor
| Type | Path | Purpose | Auto-Update |
|------|------|---------|-------------|
| Project Rules | `.cursorrules` (per project) | Cursor-specific | ⚠️ Manual |
| MCP Config | `~/.cursor/mcp.json` | MCP servers | ✅ Via alias |

### AntiGravity
| Type | Path | Purpose | Auto-Update |
|------|------|---------|-------------|
| Global Rules | `~/.gemini/GEMINI.md` | All conversations | ✅ Via script |
| MCP Config | `~/.gemini/antigravity/mcp_config.json` | MCP servers | ✅ Via alias |
| Global Workflows | `~/.gemini/antigravity/global_workflows/` | Automation | ⚠️ Manual |
| Project Workflows | `.agent/workflows/` | Project-specific | ⚠️ Manual |

### GitHub Copilot
| Type | Path | Purpose | Auto-Update |
|------|------|---------|-------------|
| Project Instructions | `.github/copilot-instructions.md` | Per repo | ❌ Not yet configured |
| Global Config | Managed via GitHub account | Account-level | ❌ External |

---

## 📚 Knowledge Base

| File | Path | Auto-Update | Trigger |
|------|------|-------------|---------|
| Master Index | `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md` | ✅ Git hook | On .md commit |
| Main KB Files | `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_*.md` | ⚠️ Manual | User edits |
| Search Optimization | `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/SEARCH_OPTIMIZATION_2026.md` | ⚠️ Manual research | Quarterly |
| Config Registry | `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/MASTER_CONFIG_REGISTRY.md` | ⚠️ Manual | On system changes |

---

## 🔧 Automation Scripts

### MCP Profile Switchers
```bash
# Location: ~/.zshrc (lines 110-115, 134-138)

# Individual IDE switches
mcp-unity              # Windsurf + Cursor → Unity profile
mcp-design             # Windsurf + Cursor → Design profile
mcp-devops             # Windsurf + Cursor → DevOps profile
mcp-full               # Windsurf + Cursor → All servers (emergency)

# AntiGravity-specific
mcp-antigravity-unity  # AntiGravity → Unity profile
mcp-antigravity-status # Check AntiGravity MCP servers

# Multi-IDE (all at once)
mcp-all-unity          # ALL IDEs → Unity profile
```

**File**: `~/.zshrc` (lines 107-138)

### KB Maintenance
```bash
# Location: ~/.zshrc (lines 118-124)

kb-audit               # Audit KB for issues
kb-backup              # Create KB backup
kb-research            # Research latest best practices
kb-optimize            # Optimize KB structure
kb-maintain            # Full maintenance cycle
```

**Script Location**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/`

### KB Index Generator
```bash
# Auto-generates KB index from all .md files
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/generate-kb-index.sh
```

**Git Hook**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.git/hooks/post-commit`
**Trigger**: Automatic on KB markdown file commits

---

## 🔄 Update Schedule

### Daily (Automated)
- ✅ KB index regeneration (git hook on commits)

### Weekly (Manual)
- ⚠️ Check Unity MCP for updates: https://github.com/CoplayDev/unity-mcp
- ⚠️ Review Unity console for warnings
- ⚠️ Run `mcp-status` to verify active servers

### Monthly (Research Required)
- ⚠️ Research latest Unity XR best practices
- ⚠️ Check AR Foundation updates
- ⚠️ Review MCP ecosystem for new servers
- ⚠️ Update SEARCH_OPTIMIZATION_2026.md with new tools

### Quarterly (Major Updates)
- ⚠️ Deep audit of all IDE configs
- ⚠️ Cross-IDE compatibility check
- ⚠️ Performance benchmarking
- ⚠️ Token usage optimization review

---

## 📖 Official Documentation Links

### Unity
- **XRI 3.1**: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/
- **AR Foundation 6.2**: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/
- **URP 17**: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/
- **Release Notes**: https://unity.com/releases/editor/archive

### Meta Quest
- **Unity SDK**: https://developers.meta.com/horizon/documentation/unity/
- **Quest 3 Guidelines**: https://developers.meta.com/horizon/documentation/native/native-design-guidelines/
- **Performance**: https://developers.meta.com/horizon/documentation/unity/unity-perf/

### AI Tools
- **Claude API**: https://docs.anthropic.com/en/docs/intro-to-claude
- **Unity MCP**: https://github.com/CoplayDev/unity-mcp
- **MCP Protocol**: https://modelcontextprotocol.io/
- **Windsurf Docs**: https://docs.windsurf.com/
- **AntiGravity Docs**: https://antigravity.google/docs/

### Search Tools
- **ripgrep**: https://github.com/BurntSushi/ripgrep
- **ugrep**: https://github.com/Genivia/ugrep
- **Fast Context**: https://docs.windsurf.com/context-awareness/fast-context

---

## 🚨 Conflict Prevention

### Known Issues

#### 1. GEMINI.md Conflict (Issue #16058)
**Problem**: AntiGravity IDE and Gemini CLI both write to `~/.gemini/GEMINI.md`
**Link**: https://github.com/google-gemini/gemini-cli/issues/16058
**Status**: Open as of 2026-01-08
**Mitigation**:
- Keep AntiGravity IDE-specific rules in GEMINI.md
- Don't use Gemini CLI for project work (use AntiGravity IDE instead)
- Backup GEMINI.md before running `gemini` CLI commands

#### 2. MCP Port Conflicts
**Problem**: Unity MCP (6400) can conflict if multiple instances run
**Check**: `lsof -i :6400`
**Fix**: `killall Unity && sleep 2 && open -a "Unity Hub"`

#### 3. Stale MCP Caches
**Problem**: MCP server updates don't take effect
**Fix**: Restart IDE after MCP config changes

---

## 🔄 Auto-Update & Cross-Linking System

### Automated Cross-Linking
**Script**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/auto_cross_link_configs.sh`

**What it does**:
1. ✅ **Auto-backups** all configs before changes (timestamped)
2. ✅ **Verifies cross-links** between global rules files
3. ✅ **Updates configs** with unified cross-reference footer
4. ✅ **Checks MCP sync** across all IDEs
5. ✅ **Provides rollback** instructions

**Quick Commands**:
```bash
config-sync      # Auto cross-link and verify all configs
config-backup    # Manual backup of all configs
```

**Backup Location**: `~/Documents/GitHub/code-backups/config-backups-YYYYMMDD-HHMMSS/`

### Auto-Backup Policy
**ALWAYS backup before changes** - Built into all automation:
- ✅ `config-sync` - Creates timestamped backups
- ✅ `kb-research-full` - Logs changes
- ✅ MCP profile switches - Preserves .backup files
- ✅ Git hooks - Version controlled

**Manual Backup**:
```bash
config-backup  # One-command backup of all configs
```

---

## 🛠️ Verification Commands

### Check Active MCP Servers
```bash
# Windsurf/Cursor
mcp-status

# AntiGravity
mcp-antigravity-status

# Raw configs
cat ~/.windsurf/mcp.json | jq '.mcpServers | keys'
cat ~/.cursor/mcp.json | jq '.mcpServers | keys'
cat ~/.gemini/antigravity/mcp_config.json | jq '.mcpServers | keys'
```

### Check Config Sync
```bash
# Verify all IDEs have same MCP profile
diff ~/.windsurf/mcp.json ~/.cursor/mcp.json
diff ~/.claude/mcp-configs/mcp-unity.json ~/.windsurf/mcp.json

# Check for Unity MCP version
grep "unity-mcp" ~/.windsurf/mcp.json ~/.cursor/mcp.json ~/.gemini/antigravity/mcp_config.json
```

### Check KB Health
```bash
# Count KB files
find ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase -name "*.md" | wc -l

# Check index freshness
ls -lh ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md

# Regenerate index manually
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/generate-kb-index.sh
```

### Check Token Usage (Claude Code)
```bash
# In Claude Code session
/context   # Shows token usage and MCP overhead
/cost      # Shows session costs
```

---

## 📂 Backup Locations

All backups stored in timestamped directories:

```bash
# Code backups (before deletions)
~/Documents/GitHub/code-backups/YYYYMMDD-HHMMSS/

# Config backups
~/.windsurf/mcp.json.backup
~/.cursor/mcp.json.backup
~/.gemini/GEMINI.md.backup-*
~/.gemini/antigravity/mcp_config.json.backup-*
```

---

## 🔗 Related Documentation

**Setup Guides**:
- [ANTIGRAVITY_SETUP_COMPLETE.md](./ANTIGRAVITY_SETUP_COMPLETE.md) - AntiGravity configuration
- [CROSS_IDE_SETUP_COMPLETE.md](./CROSS_IDE_SETUP_COMPLETE.md) - Multi-IDE overview
- [SEARCH_OPTIMIZATION_2026.md](./SEARCH_OPTIMIZATION_2026.md) - Search tools benchmarks
- [ROBUSTNESS.md](./ROBUSTNESS.md) - Zero-MCP philosophy

**Workflow Guides** (Claude Code specific):
- `~/.claude/docs/TOKEN_OPTIMIZATION.md` - Token management
- `~/.claude/docs/AGENT_ORCHESTRATION.md` - Agent usage
- `~/.claude/docs/UNITY_RN_INTEGRATION_WORKFLOW.md` - Unity + RN workflow

**Project Docs** (portals_v4):
- `~/Documents/GitHub/portals_v4/UNITY_SCENE_ANALYSIS.md` - Scene architecture
- `~/Documents/GitHub/portals_v4/DEVICE_TESTING_CHECKLIST.md` - Testing guide

---

## ⚡ Quick Actions

### Switch All IDEs to Unity Profile
```bash
mcp-all-unity
```

### Research Latest Best Practices
```bash
kb-research
# Or manually:
# 1. Search "Unity XR Interaction Toolkit 2026 best practices"
# 2. Search "AR Foundation 6.2 performance optimization"
# 3. Search "Unity MCP latest release"
# 4. Update KB files with findings
```

### Verify System Health
```bash
# Check MCP servers
mcp-status
mcp-antigravity-status

# Check Unity console
# In Claude Code: read_console(action="get", types=["error", "warning"])

# Check KB index freshness
ls -lh ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md
```

### Full System Update
```bash
# 1. Update MCP configs
mcp-all-unity

# 2. Regenerate KB index
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/generate-kb-index.sh

# 3. Research latest docs (manual)
kb-research

# 4. Restart IDEs
# Windsurf, Cursor, AntiGravity, Unity

# 5. Verify
mcp-status && mcp-antigravity-status
```

---

## 🎯 Optimization Targets

### Token Usage
- **Startup**: <50K tokens (system + MCP)
- **Session**: <100K tokens (50% of 200K context)
- **MCP overhead**: <25K tokens per IDE

### Performance
- **Search**: <50ms (ripgrep/ugrep)
- **KB index**: <10ms (read static file)
- **Unity MCP**: <500ms (batch operations)

### Accuracy
- **Research before changes**: Official 2026 docs + GitHub
- **Triple-check**: Verify against code
- **Update frequency**: Monthly research, quarterly deep audit

---

## 📊 Success Metrics

**Token Savings Achieved**:
- MCP optimization: 35-60K tokens per IDE (was 55-83K, now 20-33K)
- KB static index: 10-15K tokens saved (vs MCP search servers)
- Total per session: ~90-140K tokens saved across all IDEs

**Speed Improvements**:
- KB search: Instant (was 1-2s with MCP)
- MCP batch ops: 10-100x faster (Unity MCP v9.0.1)
- Fast Context: 20x faster (Windsurf built-in)

**Reliability**:
- Zero-dependency KB: Works forever (just markdown + git)
- Automatic backups: All configs timestamped
- Conflict prevention: Known issues documented

---

**Version**: 1.0
**Last Updated**: 2026-01-08
**Next Review**: 2026-02-08 (monthly check)
