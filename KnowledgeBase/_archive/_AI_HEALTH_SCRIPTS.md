# AI Health Check Scripts

## Overview

Two complementary scripts for validating AI tool configuration and system health.

| Script | Location | Purpose |
|--------|----------|---------|
| `validate-ai-config.sh` | `~/.local/bin/` | Config validation + auto-healing |
| `ai-system-monitor.sh` | `~/.local/bin/` | System health + process monitoring |

---

## validate-ai-config.sh

**Purpose**: Validates configuration hierarchy, symlinks, token usage. Auto-heals broken symlinks.

**When to use**: After config changes, new tool setup, troubleshooting

```bash
validate-ai-config.sh
```

**Checks**:
- GLOBAL_RULES.md exists
- ~/CLAUDE.md is minimal pointer (<30 lines)
- ~/.claude/CLAUDE.md exists
- Knowledgebase symlinks valid (auto-heals if broken)
- Knowledgebase accessibility
- Token usage estimation
- Configuration redundancy
- Directory structure

**Auto-Healing**:
- Repairs broken symlinks to knowledgebase
- Creates missing symlinks

---

## ai-system-monitor.sh

**Purpose**: Comprehensive system health monitoring with multiple modes.

**Modes**:
```bash
ai-system-monitor.sh --quick    # Fast check (5 sec)
ai-system-monitor.sh --full     # Deep audit (15 sec)
ai-system-monitor.sh --fix      # Auto-fix issues
```

**Checks**:
- Token usage (GLOBAL_RULES, AI_AGENT, Claude config)
- Symlink integrity
- Critical files existence
- Knowledgebase stats
- Rogue processes (idevicesyslog, monitor_unity, etc.)
- Large log files
- Spec-kit compliance

---

## When to Use Which

| Situation | Script |
|-----------|--------|
| Daily quick check | `ai-system-monitor.sh --quick` |
| After config changes | `validate-ai-config.sh` |
| Weekly deep audit | `ai-system-monitor.sh --full` |
| Broken symlinks | `validate-ai-config.sh` (auto-heals) |
| Process issues | `ai-system-monitor.sh --full` |
| New tool setup | `validate-ai-config.sh` |

---

## Health Thresholds

### Token Usage
| Level | Tokens | Status |
|-------|--------|--------|
| Optimal | <5K | ✅ Green |
| Moderate | 5-10K | ⚠️ Yellow |
| Critical | >10K | ❌ Red |

### Current Optimized Values (2026-01-13)
```
GLOBAL_RULES.md: ~3,650 tokens
AI_AGENT_V2:     ~875 tokens
Claude config:   ~900 tokens
Total:           ~5,425 tokens ✅
```

---

## Quick Reference

```bash
# Daily check
ai-system-monitor.sh --quick

# Full audit
ai-system-monitor.sh --full

# Config validation + auto-heal
validate-ai-config.sh

# Check token usage only
ai-system-monitor.sh --quick 2>&1 | grep -A5 "token usage"
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Symlink broken | Run `validate-ai-config.sh` (auto-heals) |
| Token usage high | Optimize GLOBAL_RULES.md, move content to KB references |
| Rogue processes | `killall -9 <process>` or `ai-system-monitor.sh --fix` |
| Large logs | `truncate -s 0 ~/.claude/logs/*.log` |

---

*Updated: 2026-01-13*
