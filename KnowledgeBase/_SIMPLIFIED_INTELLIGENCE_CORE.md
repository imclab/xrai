# Simplified Intelligence Core

**Version**: 1.0 (2026-01-21)
**Philosophy**: Simple beats complex. One loop, not twenty systems.

---

## The One Pattern

```
Search KB → Act → Log discovery → Repeat
```

That's it. Everything else is overhead.

---

## What Matters (Keep)

| File | Purpose | When |
|------|---------|------|
| `LEARNING_LOG.md` | Append-only discoveries | Any insight |
| `_AUTO_FIX_PATTERNS.md` | Known fixes | On error |
| `GLOBAL_RULES.md` | Tool selection | Always |

## What's Overhead (Simplify)

| Before | After | Why |
|--------|-------|-----|
| 6 log files | 1 (LEARNING_LOG) | One place to write |
| 20 sections in CLS | 3 core patterns | Cognitive load |
| 11 agents | 3 essential | Most unused |
| Multiple indexes | 1 master | Single source |

---

## Three Essential Agents

1. **explore** - Research before coding (built-in)
2. **health-check** - Quick system status
3. **insight-log** - Append to LEARNING_LOG

Everything else: use main Claude Code session.

---

## User Patterns (James)

**Primary Goals**:
- Unity XR/AR/VFX development
- MetavidoVFX hologram systems
- Speed over perfection
- Leverage existing patterns

**Work Style**:
- Rapid iteration
- Rider + Claude Code + Unity MCP
- Spec-driven for large features
- Skip specs for quick fixes

**What Slows You Down**:
- Over-research before action
- Multiple files when one works
- Complex systems to maintain
- Token waste on process

**What Accelerates**:
- KB pattern reuse
- First-attempt success
- Auto-fixes for known errors
- Parallel tool calls

---

## Research-Backed AI Usage (RCT Evidence)

### When AI Helps vs Hurts

| Scenario | AI Impact | Strategy |
|----------|-----------|----------|
| New domain/codebase | +20-55% | Use AI extensively |
| Familiar codebase (5+ yrs) | **-19%** | Manual for familiar, AI for new |
| Boilerplate/repetitive | +35% | Always use AI |
| Complex logic you know | -10-20% | Type directly, skip AI |
| Debugging with MCP | +30-60% | Use rapid debug loop |

**Sources**: METR (arXiv:2507.09089), Microsoft RCT (SSRN:4945566)

### Key Insight: Acceptance Rate

METR study found **<44% acceptance rate** for AI suggestions in mature codebases.

**Implication**: Don't wait for AI to generate perfect code. Use AI for:
1. Exploration/scaffolding
2. Unfamiliar APIs
3. Boilerplate generation
4. Test generation

Type directly for:
1. Core logic you understand
2. Performance-critical code
3. Complex state management
4. Code you'll maintain long-term

### Unity MCP Rapid Debug Loop

```
Error → read_console → find_in_file → Edit → refresh_unity → verify
```

**30-60% faster** than manual debugging (batch_execute for multiple fixes)

---

## Tool Selection by Domain (Token-Optimized)

| Domain | Tool | Tokens | Why |
|--------|------|--------|-----|
| **KB search** | grep + _QUICK_FIX.md | **0** | Bash, no AI |
| **C# code** | JetBrains MCP | ~200 | Rider's index, 10x faster |
| **Unity Editor** | Unity MCP | ~300 | batch_execute saves 10-100x |
| **Semantic fallback** | ChromaDB (claude-mem) | ~500 | Only when grep fails |
| **Web research** | WebSearch | ~1000+ | Last resort |

### Search Priority (Fastest First)
```
1. _QUICK_FIX.md        → 0 tokens (table lookup)
2. _PATTERN_TAGS.md     → 0 tokens (tag index)
3. grep KnowledgeBase/  → 0 tokens (bash)
4. JetBrains MCP        → ~200 tokens (code only)
5. ChromaDB query       → ~500 tokens (semantic)
6. Web search           → ~1000+ tokens (last resort)
```

### Why This Order
- Steps 1-3: Zero tokens, pure file operations
- Step 4: JetBrains uses Rider's pre-built index (fast)
- Step 5: ChromaDB for fuzzy/conceptual queries
- Step 6: Only when KB has no answer

---

## Token-Free Self-Improvement

### 1. Git Hooks (Automatic)

**Post-commit hook** (`~/Documents/GitHub/Unity-XR-AI/.git/hooks/post-commit`):
```bash
#!/bin/bash
# Auto-timestamp LEARNING_LOG.md if modified
if git diff --name-only HEAD~1 | grep -q "LEARNING_LOG.md"; then
  echo "## $(date +%Y-%m-%d) - Commit $(git rev-parse --short HEAD)" >> KnowledgeBase/_COMMIT_LOG.md
fi
```

### 2. LaunchAgent (Daily)

**KB Health Check** (`~/Library/LaunchAgents/com.kb.health.plist`):
```xml
<!-- Runs daily at 6am, checks KB integrity -->
<!-- No tokens needed - pure bash -->
```

### 3. File Watchers

**Auto-dedupe patterns** when files change - no Claude needed.

---

## Simplified Activation

**Start**: Search KB first
**Error**: Check `_AUTO_FIX_PATTERNS.md`
**Discovery**: Append to `LEARNING_LOG.md`
**Done**: No extra logging

---

## Migration

### Files to Archive (Move to `_archive/`)
- FAILURE_LOG.md → merge into LEARNING_LOG.md
- SUCCESS_LOG.md → merge into LEARNING_LOG.md
- STUCK_LOG.md → merge into LEARNING_LOG.md
- ANTI_PATTERNS.md → merge into LEARNING_LOG.md
- PERSISTENT_ISSUES.md → merge into LEARNING_LOG.md
- _CONTINUOUS_LEARNING_SYSTEM.md → keep this simplified version

### Keep Active
- LEARNING_LOG.md (one log to rule them all)
- _AUTO_FIX_PATTERNS.md (actionable fixes)
- _SIMPLIFIED_INTELLIGENCE_CORE.md (this file)
- GLOBAL_RULES.md (tool selection)

---

## Future Growth Pattern

Don't add complexity. Add to existing files:
- New pattern → LEARNING_LOG.md
- New fix → _AUTO_FIX_PATTERNS.md
- New tool rule → GLOBAL_RULES.md

When LEARNING_LOG.md gets big → extract patterns to KB files.

---

**Complexity Metric**: If you can't explain the system in 30 seconds, it's too complex.

Current system explanation:
> "Search KB before coding. Log discoveries. Use known fixes for errors."

That's it.
