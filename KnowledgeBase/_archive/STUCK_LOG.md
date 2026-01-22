# Stuck Log

**Purpose**: Track when we get stuck to learn prevention patterns.
**Integration**: Connected to FAILURE_LOG, ANTI_PATTERNS, insight-extractor

---

## Template

```markdown
## [Date] - [Task] - Stuck Event

**Signal**: What triggered detection
**Duration**: How long stuck
**Root Cause**: Why we got stuck
**Resolution**: How we got unstuck
**Prevention**: How to avoid next time
**Pattern Created**: ✅/⬜ Added to prevention
```

---

## Detection Signals

| Signal | Response |
|--------|----------|
| Same error 3+ times | Try different approach |
| Task >30 min, no progress | Reassess, break down |
| Circular conversation | Summarize, redirect |
| User frustration | Step back, fresh approach |
| Spec drift | Return to original spec |

---

## Categories

| Category | Count | Top Cause |
|----------|-------|-----------|
| Wrong Approach | 0 | - |
| Missing Info | 0 | - |
| Tool Issue | 0 | - |
| Scope Creep | 0 | - |
| Environment | 0 | - |

---

**Last Updated**: 2026-01-21
