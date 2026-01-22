# Persistent Issues Registry

**Purpose**: Track recurring issues that need dedicated attention.
**Escalation**: 3 occurrences → entry, 5 → priority, 10 → critical

---

## Template

```markdown
## [Issue ID] - [Short Description]

**First Seen**: [Date]
**Occurrences**: [Count]
**Platforms**: iOS/Android/Editor/etc.
**Status**: Open/Workaround/Investigating/Fixed
**Symptoms**: How it manifests
**Root Cause**: If known
**Workaround**: Current mitigation
**Fix Attempts**: What's been tried
**Blockers**: Why not fixed yet
**Related**: Links to other issues
```

---

## Active Issues

### PI-001 - MCP Server Timeouts

**First Seen**: 2026-01-21
**Occurrences**: 3+
**Platforms**: macOS (all IDEs)
**Status**: Workaround
**Symptoms**: Unity MCP calls timeout, no response
**Root Cause**: Duplicate server processes blocking ports
**Workaround**: Run `mcp-kill-dupes` at session start
**Fix Attempts**: Added to GLOBAL_RULES startup tasks
**Blockers**: MCP servers spawn per-app by design
**Related**: _SELF_HEALING_SYSTEM.md

---

## Status Legend

| Status | Meaning |
|--------|---------|
| Open | No fix or workaround yet |
| Workaround | Mitigation exists, not root cause fixed |
| Investigating | Actively researching |
| Fixed | Resolved, kept for reference |

---

## Issue Categories

| Category | Count | Priority |
|----------|-------|----------|
| MCP/Tools | 1 | Medium |
| Unity/Build | 0 | - |
| AR/Device | 0 | - |
| Performance | 0 | - |
| Token/Context | 0 | - |

---

**Last Updated**: 2026-01-21
