# Pattern Architecture

**Purpose**: Unified format for fast lookup, auto-improvement, and rapid debugging.

## Design Principles (From Research)

Based on [Claude Code Architecture](https://www.anthropic.com/engineering/claude-code-best-practices), [KB Best Practices 2026](https://www.bolddesk.com/blogs/knowledge-base-architecture), and [RAG Architecture](https://levelup.gitconnected.com/designing-a-production-grade-rag-architecture-bee5a4e4d9aa):

1. **Simple Loop** - Claude uses `think → act → observe → repeat`. Our KB: `search → apply → log → repeat`
2. **Lazy Loading** - Don't load everything upfront. Quick Fix table → Tags → Full search
3. **Standard Templates** - All patterns use same format (searchable, consistent)
4. **Hub-and-Spoke** - Index files point to detail files
5. **Auto-Categorization** - Scripts tag and index, not AI (zero tokens)
6. **Duplicate Detection** - Indexer finds similar patterns
7. **Usage Tracking** - Promote frequently-used patterns to Quick Fix

---

## Data Architecture

```
KnowledgeBase/
├── _QUICK_FIX.md          ← Error → Fix (instant lookup)
├── _PATTERN_TAGS.md       ← Tag index (find by topic)
├── LEARNING_LOG.md        ← Append-only discoveries
├── _AUTO_FIX_PATTERNS.md  ← Detailed fixes
│
├── Patterns by Domain/
│   ├── _UNITY_INTELLIGENCE_PATTERNS.md
│   ├── _WEBGL_INTELLIGENCE_PATTERNS.md
│   ├── _3DVIS_INTELLIGENCE_PATTERNS.md
│   └── _*_PATTERNS.md
│
└── Scripts/automation/
    ├── pattern-extractor.sh    ← Extract from code
    ├── pattern-indexer.sh      ← Rebuild indexes
    └── pattern-search.sh       ← Fast CLI search
```

---

## Unified Pattern Format

Every pattern in every file uses this structure:

```markdown
### [Pattern Name]
**Tags**: `tag1` `tag2` `tag3`
**Use When**: [One-line trigger condition]
**Code**:
```[language]
// Pattern code here
```
**Why**: [One-line explanation]
**See Also**: [Related patterns]
```

### Example

```markdown
### ExposedProperty VFX Binding
**Tags**: `vfx` `unity` `property` `binding`
**Use When**: Setting VFX Graph properties from C#
**Code**:
```csharp
static readonly ExposedProperty PropName = "PropertyName";
vfx.SetVector3(PropName, value);
```
**Why**: Compile-time validation, faster than string lookup
**See Also**: VFX Buffer Binding, VFX Texture Binding
```

---

## Quick Fix Format

`_QUICK_FIX.md` - Instant error-to-solution lookup:

```markdown
| Error | Fix | File:Line |
|-------|-----|-----------|
| CS0246 type not found | Add `using X;` | - |
| NullRef in AR texture | Use TryGetTexture | ARDepthSource:45 |
| VFX not updating | Use ExposedProperty | VFXARBinder:23 |
| MCP timeout | Run mcp-kill-dupes | - |
```

No explanation, just answer. Explanations in `_AUTO_FIX_PATTERNS.md`.

---

## Tag Index Format

`_PATTERN_TAGS.md` - Find patterns by topic:

```markdown
## vfx
- ExposedProperty VFX Binding → _UNITY_INTELLIGENCE_PATTERNS.md
- VFX Buffer Binding → _VFX25_HOLOGRAM_PORTAL_PATTERNS.md
- Audio VFX Binding → _LASPVFX_AUDIO_BINDING_PATTERNS.md

## ar
- TryGetTexture Pattern → _AUTO_FIX_PATTERNS.md
- AR Session Ready Check → _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md

## performance
- Object Pooling → _PERFORMANCE_PATTERNS_REFERENCE.md
- Burst Compilation → _UNITY_INTELLIGENCE_PATTERNS.md
```

---

## Auto-Improvement Flow

```
Code Change
    ↓
Git commit triggers pattern-extractor.sh
    ↓
New patterns appended to LEARNING_LOG.md
    ↓
Weekly: pattern-indexer.sh rebuilds _PATTERN_TAGS.md
    ↓
Patterns become searchable
    ↓
Next error → instant lookup
```

**Zero AI tokens** for the automation loop.

---

## Search Priority (Fastest First)

| Priority | Tool | Tokens | Use When |
|----------|------|--------|----------|
| 1 | **_QUICK_FIX.md** | 0 | Error codes, known fixes |
| 2 | **_PATTERN_TAGS.md** | 0 | Topic → file lookup |
| 3 | **grep KB** | 0 | Keyword search |
| 4 | **JetBrains MCP** | ~200 | C# code search (Rider open) |
| 5 | **ChromaDB** | ~500 | Semantic/fuzzy queries |
| 6 | **LEARNING_LOG.md** | 0 | Recent discoveries |
| 7 | **Web search** | ~1000+ | Last resort |

**Rule**: Exhaust zero-token options before using MCP/API calls.

---

## CLI Search Script

`Scripts/automation/pattern-search.sh`:
```bash
#!/bin/bash
# Usage: ./pattern-search.sh "vfx property"
QUERY="$1"
KB_DIR="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase"

# 1. Check quick fix first
grep -i "$QUERY" "$KB_DIR/_QUICK_FIX.md" 2>/dev/null && exit 0

# 2. Check tag index
grep -i "$QUERY" "$KB_DIR/_PATTERN_TAGS.md" 2>/dev/null

# 3. Full search
grep -rli "$QUERY" "$KB_DIR"/_*PATTERNS*.md | head -5
```

---

## Frequency Tracking

Add to patterns when used:
```markdown
**Used**: 5x (2026-01-21)
```

Weekly script counts usage, promotes frequent patterns to _QUICK_FIX.md.

---

## Best Practices

1. **One pattern per problem** - No mega-patterns
2. **Tags are cheap** - Add many, search is fast
3. **Code over prose** - Show, don't explain
4. **Link related** - See Also saves searching
5. **Track usage** - Promote what works

---

## Migration Checklist

- [ ] Create `_QUICK_FIX.md` from `_AUTO_FIX_PATTERNS.md`
- [ ] Create `_PATTERN_TAGS.md` by scanning all pattern files
- [ ] Add Tags to existing patterns
- [ ] Create `pattern-search.sh`
- [ ] Create `pattern-indexer.sh`
- [ ] Add git hook for auto-extraction

---

**Speed**: Table lookup < Tag search < Grep < AI search
**Goal**: Most lookups should be table lookup (O(1))
