# System Architecture (Simplified)

**Philosophy**: Speed + Accuracy + Auto-Improvement. Nothing more.

---

## Core Loop

```
Error/Task → Search KB → Apply Fix → Log if new → Done
```

That's it. Everything else supports this loop.

---

## Architecture (3 Layers)

```
┌─────────────────────────────────────────────────────────┐
│                      YOU (James)                         │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                   LAYER 1: AI TOOLS                      │
│                                                          │
│   Claude Code ←→ Gemini ←→ Cursor ←→ Codex              │
│        │                                                 │
│        └── All read: GLOBAL_RULES.md + KnowledgeBase/   │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                   LAYER 2: KNOWLEDGE                     │
│                                                          │
│   _AUTO_FIX_PATTERNS.md    ← 101 patterns (PRIMARY)     │
│   _QUICK_FIX.md            ← Error→Fix table            │
│   _MASTER_GITHUB_REPO.md   ← 520+ repos                 │
│   LEARNING_LOG.md          ← New discoveries            │
│                                                          │
│   [That's the 80% you need. The rest is reference.]     │
└─────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────┐
│                   LAYER 3: AUTOMATION                    │
│                                                          │
│   Hooks (6):                                            │
│   - auto-detect-agent.sh  → Auto-suggest agents         │
│   - failure-tracker.sh    → 3-strike escalation         │
│   - session-health.sh     → MCP duplicate cleanup       │
│                                                          │
│   Agents (USE 5, HAVE 25):                              │
│   - unity-error-fixer     → Compilation errors          │
│   - vfx-tuner             → VFX property adjustments    │
│   - parallel-researcher   → Fast multi-search           │
│   - tech-lead             → Architecture decisions      │
│   - mcp-tools-specialist  → Unity Editor automation     │
└─────────────────────────────────────────────────────────┘
```

---

## Speed Optimizations

| Technique | Speedup | How |
|-----------|---------|-----|
| `kbfix "error"` | 10x | Zero-token lookup |
| Parallel agents | 5x | Independent context |
| Auto-detect mode | 2x | No keywords needed |
| Failure tracker | 3x | Auto-escalates stuck |

---

## Auto-Improvement (Simple)

**When**: Task completes successfully on first try
**Action**: Add pattern to `_AUTO_FIX_PATTERNS.md`

**When**: Same error 3+ times
**Action**: Research fix, add to KB, log to `LEARNING_LOG.md`

**That's it.** No complex "intelligence systems" needed.

---

## What We Removed (Over-Engineering)

| Removed | Why |
|---------|-----|
| 14 "intelligence" files | Replaced by this single file |
| 459MB node_modules in KB | Build artifacts, not knowledge |
| _archive/ duplicates | Redundant copies |
| Agent variants (-quick/-deep) | Use model selection instead |
| Multiple CLAUDE.md versions | Symlink to one source |

---

## File Count Reality

**Before**: 137 KB files, 14 intelligence docs, 25 agents
**After**: ~100 KB files, 1 architecture doc, 5-10 active agents

**Rule**: If you haven't used it in 2 weeks, delete it.

---

## Key Files (Memorize These)

| File | Purpose | When |
|------|---------|------|
| `_AUTO_FIX_PATTERNS.md` | Fix errors | Every error |
| `_QUICK_FIX.md` | Fast lookup | Quick reference |
| `LEARNING_LOG.md` | Log discoveries | After solving new problem |
| `GLOBAL_RULES.md` | Universal rules | Session start |

Everything else is reference material you search when needed.

---

## Two Questions (Before Any Change)

1. **"Do we really need this?"**
2. **"Could this cause other issues?"**

If unsure on either → Don't do it, or ask first.

---

**Last Updated**: 2026-01-22
**Complexity Target**: Minimal. If it needs explaining, simplify it.
