# Shell Configuration Reference

**Location**: `~/.zshrc`
**Shell**: Zsh with Oh My Zsh
**Theme**: gentoo

---

## Alias Quick Reference

### MCP Profile Switching

| Alias | Action |
|-------|--------|
| `mcp-unity` | Switch to Unity dev profile (saves 35-50K tokens) |
| `mcp-design` | Switch to Design mode (Figma + Blender) |
| `mcp-devops` | Switch to DevOps mode |
| `mcp-full` | Full MCP (warning: 55-83K tokens!) |
| `mcp-status` | Show active MCP servers |
| `mcp-list` | List available MCP configs |
| `mcp-all-unity` | Apply Unity profile to ALL IDEs |
| `mcp-antigravity-unity` | AntiGravity Unity profile |
| `mcp-antigravity-status` | AntiGravity MCP status |

### Knowledgebase

| Alias | Action |
|-------|--------|
| `kb-audit` | Run KB audit script |
| `kb-backup` | Backup knowledgebase |
| `kb-research` | Quick KB research update |
| `kb-research-full` | Full KB research update |
| `kb-optimize` | Optimize KB |
| `kb-maintain` | KB maintenance |
| `kb-update-all` | Full system update |

### KB Tool Aliases (from kb-aliases.sh)

| Alias | Action |
|-------|--------|
| `kb-p "text"` | Add pattern to LEARNING_LOG |
| `kb-i "text"` | Add insight to LEARNING_LOG |
| `kb-a "text"` | Add anti-pattern |
| `kb-auto` | Auto-extract from git |
| `kb-quick "text"` | Quick note |
| `kb-check` | Quick audit |
| `kb-full` | Full audit |
| `kb-sec` | Security audit |
| `kb-cd` | cd to knowledgebase |
| `kb-log` | Open LEARNING_LOG |
| `kb-commit` | Git commit KB changes |
| `kb-sync` | Git pull + commit + push |

### iOS Build (Portals)

| Alias | Action |
|-------|--------|
| `ios-fast` | Fast iOS build (skip Unity export) |
| `ios-full` | Full iOS build |
| `portals-cd` | cd to portals_v4 |
| `unity-test` | Run Unity test automation |

### Config Management

| Alias | Action |
|-------|--------|
| `config-sync` | Cross-link all configs |
| `config-backup` | Backup all AI configs |

---

## PATH Configuration

```bash
/opt/homebrew/bin
/usr/local/bin
$HOME/.local/bin
$HOME/bin
```

---

## Key Settings

| Setting | Value |
|---------|-------|
| `ENABLE_CORRECTION` | true |
| `DISABLE_UNTRACKED_FILES_DIRTY` | true |
| `HIST_STAMPS` | mm/dd/yyyy |
| `LANG` | en_US.UTF-8 |
| `plugins` | git |

---

## Sourced Files

| File | Purpose |
|------|---------|
| `~/.oh-my-zsh/oh-my-zsh.sh` | Oh My Zsh |
| `~/.env` | Environment variables (not committed) |
| `~/.local/bin/kb-aliases.sh` | KB tool aliases |

---

## MCP Config Locations

| Tool | Config Path |
|------|-------------|
| Claude Code | `~/.claude.json` |
| Windsurf | `~/.windsurf/mcp.json` |
| Cursor | `~/.cursor/mcp.json` |
| AntiGravity | `~/.gemini/antigravity/mcp_config.json` |
| Profiles | `~/.claude/mcp-configs/` |

---

## Full .zshrc Contents

```bash
# ---- PATH setup ----
path=(
  /opt/homebrew/bin
  /usr/local/bin
  $HOME/.local/bin
  $path
)
export PATH="/usr/local/bin:/opt/homebrew/bin:$HOME/.local/bin:$PATH"

# ---- Oh My Zsh ----
export ZSH="$HOME/.oh-my-zsh"
ZSH_THEME="gentoo"
ENABLE_CORRECTION="true"
DISABLE_UNTRACKED_FILES_DIRTY="true"
HIST_STAMPS="mm/dd/yyyy"
plugins=(git)
source $ZSH/oh-my-zsh.sh

# ---- Environment ----
export LANG=en_US.UTF-8
export ARCHFLAGS="-arch $(uname -m)"
[ -f ~/.env ] && source ~/.env

# ---- MCP Profile Switcher ----
alias mcp-unity='cp ~/.claude/mcp-configs/mcp-unity.json ~/.windsurf/mcp.json && cp ~/.claude/mcp-configs/mcp-unity.json ~/.cursor/mcp.json && echo "✓ MCP: Unity Dev"'
alias mcp-design='cp ~/.claude/mcp-configs/mcp-design.json ~/.windsurf/mcp.json && cp ~/.claude/mcp-configs/mcp-design.json ~/.cursor/mcp.json && echo "✓ MCP: Design Mode"'
alias mcp-devops='cp ~/.claude/mcp-configs/mcp-devops.json ~/.windsurf/mcp.json && cp ~/.claude/mcp-configs/mcp-devops.json ~/.cursor/mcp.json && echo "✓ MCP: DevOps Mode"'
alias mcp-full='cp ~/.claude/mcp-configs/mcp-full.json ~/.windsurf/mcp.json && cp ~/.claude/mcp-configs/mcp-full.json ~/.cursor/mcp.json && echo "⚠ MCP: Full"'
alias mcp-status='echo "Active MCP servers:" && cat ~/.windsurf/mcp.json 2>/dev/null | jq -r ".mcpServers | keys[]"'
alias mcp-list='ls -1 ~/.claude/mcp-configs/'
alias mcp-all-unity='mcp-unity && mcp-antigravity-unity && echo "✅ All IDEs updated"'
alias mcp-antigravity-unity='cp ~/.claude/mcp-configs/mcp-antigravity.json ~/.gemini/antigravity/mcp_config.json && echo "✓ AntiGravity: Unity"'
alias mcp-antigravity-status='cat ~/.gemini/antigravity/mcp_config.json 2>/dev/null | jq -r ".mcpServers | keys[]"'

# ---- Knowledgebase ----
alias kb-audit='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_AUDIT.sh'
alias kb-backup='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_BACKUP.sh'
alias kb-research='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/KB_RESEARCH_AND_UPDATE.sh --quick'
alias kb-research-full='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/KB_RESEARCH_AND_UPDATE.sh --full'
alias kb-optimize='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_OPTIMIZE.sh'
alias kb-maintain='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_MAINTENANCE.sh'
alias kb-update-all='kb-research-full && mcp-all-unity && echo "✅ Full system update complete"'

# ---- iOS Build (Portals) ----
alias ios-fast='cd ~/Documents/GitHub/portals_v4 && ./scripts/build_and_run_ios.sh --skip-unity-export --keep-unity-open'
alias ios-full='cd ~/Documents/GitHub/portals_v4 && ./scripts/build_and_run_ios.sh'
alias portals-cd='cd ~/Documents/GitHub/portals_v4'
alias unity-test='osascript ~/Documents/GitHub/portals_v4/scripts/test_unity_editor.scpt'

# ---- Config Management ----
alias config-sync='~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/auto_cross_link_configs.sh'
alias config-backup='BACKUP_DIR=~/Documents/GitHub/code-backups/manual-backup-$(date +%Y%m%d-%H%M%S) && mkdir -p "$BACKUP_DIR" && cp ~/.claude/CLAUDE.md ~/.gemini/GEMINI.md ~/.windsurf/mcp.json ~/.cursor/mcp.json ~/.gemini/antigravity/mcp_config.json "$BACKUP_DIR/" && echo "✅ Configs backed up"'

# ---- KB Tool Aliases ----
source ~/.local/bin/kb-aliases.sh
```

---

*Updated: 2026-01-13*
