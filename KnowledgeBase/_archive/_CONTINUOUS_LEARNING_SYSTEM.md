# Continuous Learning System

**Purpose**: Auto-improve AI coding capabilities through pattern extraction and KB updates.
**Version**: 1.0 (2026-01-22)

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    LEARNING PIPELINE                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Task Completion                                                 │
│       ↓                                                          │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐     │
│  │ Success?     │ YES │ Extract      │ → → │ Add to       │     │
│  │ First Try?   │ ──→ │ Pattern      │     │ AUTO_FIX     │     │
│  └──────────────┘     └──────────────┘     └──────────────┘     │
│       │ NO                                                       │
│       ↓                                                          │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐     │
│  │ 3+ Failures? │ YES │ Analyze      │ → → │ Create       │     │
│  │              │ ──→ │ Root Cause   │     │ Prevention   │     │
│  └──────────────┘     └──────────────┘     └──────────────┘     │
│       │                                                          │
│       ↓                                                          │
│  ┌──────────────┐                                               │
│  │ Log to       │                                               │
│  │ LEARNING_LOG │                                               │
│  └──────────────┘                                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## Learning Triggers

| Event | Detection | Action |
|-------|-----------|--------|
| Success (first try) | Task completed without errors | Extract pattern → `_AUTO_FIX_PATTERNS.md` |
| Failure (3+ attempts) | Same error repeated | Analyze root cause → Create prevention |
| New fix discovered | Manual fix applied | Add to fix library |
| Pattern from research | WebSearch/KB review | Extract and add to KB |
| Error not in KB | Error code not found | Log gap → Research later |

---

## Pattern Extraction Process

### 1. From Task Completion

```markdown
## Pattern Template

### [Pattern Name]

**Detection**: [When this pattern applies]
**Cause**: [Why this happens]
**Pattern**:
\`\`\`csharp
// Code solution
\`\`\`
**Source**: [URL or reference]
**Auto-Apply**: Yes/Partial/No
```

### 2. From Research (Parallel)

Use `parallel-researcher` agent:
1. Identify 5+ subtopics
2. Execute parallel WebSearch
3. Extract code patterns from results
4. Add to appropriate KB file
5. Update `_AUTO_FIX_PATTERNS.md`

### 3. From KB Review

Use `insight-extractor` agent:
1. Read existing KB files
2. Identify patterns not yet in `_AUTO_FIX_PATTERNS.md`
3. Extract and format
4. Add with source attribution

---

## Pattern Categories (106 total)

| Category | Count | Primary Source |
|----------|-------|----------------|
| Unity C# Errors | 8 | Unity docs, Stack Overflow |
| AR Foundation | 12 | Unity AR samples, GitHub issues |
| VFX Graph | 18 | keijiro repos, Unity VFX docs |
| Compute/HLSL | 14 | Catlike Coding, Unity docs |
| Hand Tracking | 8 | XR Hands docs, HoloKit |
| Jobs/Burst | 6 | Unity Burst manual |
| Networking | 10 | Photon, Normcore, Netcode docs |
| Mobile/WebGL | 8 | Unity optimization guides |
| UI Toolkit | 4 | Unity 6 UI docs |
| Shader Graph | 4 | Shader Graph manual |
| Audio VFX | 2 | keijiro/LaspVfx |
| WebXR | 2 | WebXR explainer |
| Other | 8 | Various |

---

## Knowledge Flow

```
Online Docs / GitHub
        ↓
    WebSearch
        ↓
┌───────────────────┐
│ parallel-researcher│
│ pattern-researcher │
└─────────┬─────────┘
          ↓
    Extract Patterns
          ↓
┌───────────────────┐
│ _AUTO_FIX_PATTERNS │
│ LEARNING_LOG       │
│ Category KB files  │
└───────────────────┘
          ↓
    AI Tool Usage
          ↓
    Better Code
```

---

## Automation Hooks

### failure-tracker.sh (PostToolUse)

```bash
# 1st failure: Silent tracking
# 2nd failure: Auto-search KB for fix
# 3rd failure: Circuit breaker + escalate
```

### auto-detect-agent.sh (UserPromptSubmit)

```bash
# Detects patterns like:
# "auto learn from docs" → pattern-researcher
# "research multiple topics" → parallel-researcher
```

---

## Quality Gates

### Before Adding Pattern

- [ ] Code compiles (tested in Unity if possible)
- [ ] Source URL provided
- [ ] Not duplicate of existing pattern
- [ ] Follows template format
- [ ] Auto-Apply field specified

### Pattern Maintenance

- Weekly: Review error frequency
- Monthly: Archive unused patterns
- Quarterly: Research new Unity versions for API changes

---

## Agents for Learning

| Agent | Purpose | Trigger |
|-------|---------|---------|
| `pattern-researcher` | Research from docs/GitHub | Manual or "auto learn" |
| `parallel-researcher` | Fast multi-topic research | "research multiple" |
| `insight-extractor` | Extract patterns from code | After task completion |
| `system-improver` | Auto-improve configs/KB | Discoveries found |

---

## Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Auto-fix patterns | 106 | 150+ |
| Auto-apply rate | 80% | 85%+ |
| First-try success | ~60% | 75%+ |
| Pattern coverage | Good | Comprehensive |

---

## Related Files

- `_AUTO_FIX_PATTERNS.md` - Pattern library (106 patterns)
- `LEARNING_LOG.md` - Discovery journal
- `_INTELLIGENCE_SYSTEMS_INDEX.md` - Central reference
- `_SELF_HEALING_SYSTEM.md` - Error recovery
- `~/.claude/agents/pattern-researcher.md` - Research agent
- `~/.claude/agents/parallel-researcher.md` - Parallel research
- `~/.claude/hooks/failure-tracker.sh` - Failure tracking

---

**Last Updated**: 2026-01-22
**Maintained By**: system-improver agent
