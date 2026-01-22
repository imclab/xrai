# Failure Log

**Purpose**: Track failures to prevent recurrence and build prevention patterns.
**Pattern**: Each failure → root cause → prevention → auto-fix

---

## Template

```markdown
## [Date] - [Category] - [Failure Type]

**What Happened**: Brief description
**Expected**: What should have occurred
**Actual**: What actually occurred
**Root Cause**: Why it failed (be specific)
**Time Lost**: Estimated debugging time
**Prevention**: How to avoid in future
**Auto-Fix Created**: ✅/⬜ Added to _AUTO_FIX_PATTERNS.md
**Pattern Updated**: ✅/⬜ Which file updated
```

---

## 2026-01-21 - Tool - MCP Server Timeout

**What Happened**: Unity MCP calls failed silently
**Expected**: Unity operations should execute
**Actual**: Timeout, no response
**Root Cause**: Duplicate MCP server processes blocking port
**Time Lost**: 15 min debugging
**Prevention**: Run `mcp-kill-dupes` at session start (added to GLOBAL_RULES)
**Auto-Fix Created**: ✅ Added to _AUTO_FIX_PATTERNS.md
**Pattern Updated**: ✅ GLOBAL_RULES.md startup tasks

---

## Categories

| Category | Count | Top Issue |
|----------|-------|-----------|
| Tool | 1 | MCP timeouts |
| Unity | 0 | - |
| Agent | 0 | - |
| Token | 0 | - |
| Integration | 0 | - |

---

**Last Updated**: 2026-01-21
