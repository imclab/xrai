# Periodic Maintenance & Auto-Update System ✅

**Last Updated**: 2026-01-08
**Status**: Production Ready

---

## ⚡ Quick Commands

```bash
# Config Management (ALWAYS creates backups first!)
config-sync          # Auto cross-link & verify all configs
config-backup        # Manual backup of all configs

# KB Maintenance
kb-research          # Weekly health check (5 min)
kb-research-full     # Monthly deep research (30 min)
kb-update-all        # Full system update (all IDEs + KB)

# MCP Profile Switching (includes backup)
mcp-all-unity        # Update all IDEs to Unity profile
mcp-status           # Check MCP server status
```

Run `source ~/.zshrc` to activate.

---

## 🔒 Backup Policy (ALWAYS!)

**Every automation creates backups before modifications:**

| Action | Backup Location | Rollback |
|--------|----------------|----------|
| `config-sync` | `~/code-backups/config-backups-YYYYMMDD/` | Auto-provided in output |
| `config-backup` | `~/code-backups/manual-backup-YYYYMMDD/` | Copy files back |
| `mcp-*` switches | `.backup` in same directory | `cp *.backup` to restore |
| Git commits | Git history | `git revert` or `git reset` |

**Manual Backup Anytime**:
```bash
config-backup  # One-command full backup
```

---

## 📅 Maintenance Schedule

### **Daily** (Automated ✅)
- KB index regeneration (git hook on .md commits)
- Zero manual action required

### **Weekly** (5 min)
```bash
config-sync    # Auto cross-link & backup (1 min)
kb-research    # Health check (4 min)
```
**Checks**:
- ✅ **Config cross-links** (auto-updated)
- ✅ **Auto-backups created** (timestamped)
- ✅ MCP server versions
- ✅ KB index freshness
- ✅ Config sync across IDEs
- ✅ Unity console health
- ✅ Backup verification

### **Monthly** (30 min)
```bash
kb-research-full
```
**Research Topics**:
- Unity XR Interaction Toolkit 2026 best practices
- AR Foundation 6.2 performance optimization
- Unity MCP latest features
- Meta Quest 3 guidelines 2026
- Windsurf Fast Context updates
- ugrep vs ripgrep benchmarks
- MCP ecosystem new servers

**Actions**:
1. Run research script (generates topic list)
2. Research each topic in Claude/Windsurf/AntiGravity
3. Update KB files with findings
4. Commit changes (triggers auto-index regeneration)

### **Quarterly** (2 hours)
```bash
kb-update-all  # Full system update
```
**Deep Audit**:
- Cross-IDE compatibility verification
- Performance benchmarking
- Token usage optimization review
- Breaking changes review
- Dependency updates

---

## 🔄 Automated Systems

### 1. KB Index Auto-Generation
**File**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/generate-kb-index.sh`
**Trigger**: Git post-commit hook on .md files
**Action**: Regenerates KB_MASTER_INDEX.md automatically

### 2. Research & Health Check Script
**File**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/KB_RESEARCH_AND_UPDATE.sh`
**Modes**:
- `--quick`: 5 min health check (weekly)
- `--full`: 30 min deep research (monthly)

**Checks**:
- System health (IDE processes, MCP ports)
- MCP version verification
- KB index freshness
- Config sync across IDEs
- Unity console errors/warnings
- Backup verification

### 3. MCP Profile Switcher
**File**: `~/.zshrc`
**Commands**:
```bash
mcp-all-unity        # Update all IDEs to Unity profile
mcp-status           # Check Windsurf/Cursor servers
mcp-antigravity-status  # Check AntiGravity servers
```

---

## 📖 Configuration Registry

**Master Registry**: [MASTER_CONFIG_REGISTRY.md](./MASTER_CONFIG_REGISTRY.md)

**All Paths Documented**:
- Claude Code: `~/.claude/CLAUDE.md`, `~/CLAUDE.md`
- Windsurf: `~/.windsurf/mcp.json` (Cascade has built-in rules)
- Cursor: `~/.cursor/mcp.json`, `.cursorrules`
- AntiGravity: `~/.gemini/GEMINI.md`, `~/.gemini/antigravity/mcp_config.json`
- KB: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/`

**Links to Official Docs**:
- Unity XRI 3.1: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/
- AR Foundation 6.2: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/
- Unity MCP: https://github.com/CoplayDev/unity-mcp
- MCP Protocol: https://modelcontextprotocol.io/
- Windsurf Docs: https://docs.windsurf.com/
- AntiGravity Docs: https://antigravity.google/docs/

---

## 🎯 Optimization Targets

**Always Optimizing For**:
1. **Speed**: <50ms search (ripgrep/ugrep), <500ms Unity MCP
2. **Accuracy**: Official 2026 docs, triple-check against code
3. **Token Reduction**: <25K MCP overhead, <100K session total

**Current Performance**:
- ✅ MCP: 20-33K tokens (was 55-83K) - **35-50K savings**
- ✅ KB: Static files (was 10-15K MCP overhead) - **10-15K savings**
- ✅ Search: 10-50ms ripgrep (can upgrade to 5-40ms ugrep)

---

## 🚨 Conflict Prevention

### Known Issues Documented

**1. GEMINI.md Conflict**
- **Issue**: https://github.com/google-gemini/gemini-cli/issues/16058
- **Mitigation**: Don't use `gemini` CLI while using AntiGravity IDE

**2. MCP Port 6400**
- **Check**: `lsof -i :6400`
- **Fix**: Restart Unity if conflicts occur

**3. Stale Caches**
- **Fix**: Restart IDE after MCP config changes

---

## 📊 Success Tracking

**Token Savings Achieved**:
- Per IDE: 35-60K tokens saved
- Total (3 IDEs): ~90-140K tokens per session

**Research Frequency**:
- Weekly quick checks: Prevent drift
- Monthly deep research: Stay current
- Quarterly audits: Catch breaking changes

**Zero-Dependency Design**:
- KB: Just markdown + git (works forever)
- Search: CLI tools (no external APIs)
- Automation: Bash scripts (no npm/pip/docker)

---

## 🔗 Related Documentation

**Setup Guides**:
- [MASTER_CONFIG_REGISTRY.md](./MASTER_CONFIG_REGISTRY.md) - All paths & links
- [ANTIGRAVITY_SETUP_COMPLETE.md](./ANTIGRAVITY_SETUP_COMPLETE.md) - AntiGravity config
- [CROSS_IDE_SETUP_COMPLETE.md](./CROSS_IDE_SETUP_COMPLETE.md) - Multi-IDE overview

**Optimization Docs**:
- [SEARCH_OPTIMIZATION_2026.md](./SEARCH_OPTIMIZATION_2026.md) - Search benchmarks
- [ROBUSTNESS.md](./ROBUSTNESS.md) - Zero-MCP philosophy
- `~/.claude/docs/TOKEN_OPTIMIZATION.md` - Token deep dive

---

## ✅ System Status

**Automation Level**: 90% automated
- ✅ Daily: 100% automated (git hooks)
- ✅ Weekly: 1-click (kb-research)
- ⚠️ Monthly: Semi-automated (research + update)
- ⚠️ Quarterly: Manual audit required

**Coverage**: All IDEs & tools
- ✅ Claude Code
- ✅ Windsurf
- ✅ Cursor
- ✅ AntiGravity
- ⚠️ GitHub Copilot (not yet configured)

**Documentation**: Complete
- ✅ All paths registered
- ✅ All commands aliased
- ✅ All schedules defined
- ✅ All conflicts documented
- ✅ All links provided

---

**Next Action**: Run `source ~/.zshrc && kb-research` to verify setup

**Version**: 1.0
**Last Updated**: 2026-01-08
