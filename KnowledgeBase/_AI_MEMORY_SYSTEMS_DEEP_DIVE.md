# AI Memory Systems Deep Dive

**Purpose**: Understanding how each AI tool thinks, remembers, and learns - enabling intelligent cross-pollination
**Philosophy**: Metathinking (thinking about thinking) enables self-improvement across all tools
**Last Updated**: 2026-01-12

---

## Quick Reference

| Tool | Memory Type | Persistence | Readable | Bridgeable |
|------|-------------|-------------|----------|------------|
| Claude Code | Session-based | Per-project | Yes | Yes |
| AntiGravity/Gemini | Protobuf embeddings | Permanent | Partial | Limited |
| Codex CLI | History-based | Per-session | Yes | Yes |
| Windsurf/Cursor | File-based | Per-workspace | Yes | Yes |
| MCP Memory | Graph-based | Server-dependent | Yes | Yes |

---

## 1. Claude Code Memory Architecture

### How Claude Thinks
- **Context window**: Entire conversation loaded into attention
- **No persistent memory between sessions** (by design - privacy)
- **Learns through files**: CLAUDE.md, GLOBAL_RULES.md, project context
- **Session continuity**: Via `~/.claude/projects/{project-id}/`

### Memory Locations
```
~/.claude/
├── projects/           # 185M - Per-project session memory
│   └── {project-id}/   # Binary session state, auto-summarization
├── history.jsonl       # 224K - Command/response log
├── file-history/       # 6.3M - File change tracking
├── shell-snapshots/    # 6.1M - Bash session state
└── settings.json       # User preferences
```

### What's Readable/Exportable
- `history.jsonl` - Full session logs (JSON lines format)
- `settings.json` - User preferences
- Project session files - Binary but can be inspected

### How to Extract Learnings
```bash
# Recent commands
tail -100 ~/.claude/history.jsonl | jq '.command // .response | select(.)'

# Session patterns
grep -r "pattern\|learned\|discovery" ~/.claude/projects/
```

### Mental Model
Claude thinks in **conversations** - each session is a fresh context loaded with:
1. System prompt + tools
2. CLAUDE.md files (project → home → global)
3. Conversation history (auto-summarized on compaction)

**Key Insight**: Claude has no "memory" - it has **files that persist between sessions**. The knowledgebase IS Claude's long-term memory.

---

## 2. AntiGravity/Gemini Memory Architecture

### How AntiGravity Thinks
- **Persistent implicit memory**: 27M of learned embeddings (protobuf)
- **Brain memory**: 28M of task/project context (UUID directories)
- **Browser context**: 336M of screen recordings from web research
- **Continuous learning**: Updates embeddings based on interactions

### Memory Locations
```
~/.gemini/antigravity/
├── brain/              # 28M - 60 UUID directories with task context
│   └── {uuid}/         # metadata.json + context files
├── implicit/           # 27M - 31 protobuf files (.pb)
│   └── *.pb           # Learned embeddings (NOT human-readable)
├── conversations/      # 115M - Full conversation history
├── browser_recordings/ # 336M - Screen captures of research
└── code_tracker/       # 1.3M - Implementation references
```

### What's Readable/Exportable
- `brain/{uuid}/metadata.json` - Task context (readable JSON)
- `conversations/` - Chat history (readable)
- `code_tracker/` - Implementation refs (readable)
- **NOT readable**: `implicit/*.pb` (binary embeddings)

### How to Extract Learnings
```bash
# Recent brain contexts
find ~/.gemini/antigravity/brain -name "metadata.json" -mtime -7 -exec cat {} \;

# Code tracker patterns
cat ~/.gemini/antigravity/code_tracker/*.json | jq '.pattern // .description'

# Recent conversation topics
ls -lt ~/.gemini/antigravity/conversations/ | head -20
```

### Mental Model
AntiGravity thinks in **persistent context** - it builds understanding over time through:
1. Implicit embeddings (learned from ALL interactions)
2. Explicit brain contexts (task-specific)
3. Browser recordings (visual research memory)

**Key Insight**: AntiGravity's `.pb` files are its "subconscious" - trained patterns it can't articulate but uses. The brain/ directory is its "working memory".

---

## 3. Codex CLI Memory Architecture

### How Codex Thinks
- **Stateless design**: Each session starts fresh
- **Agent skills**: Specialized capabilities for different tasks
- **History tracking**: Commands and responses logged
- **No persistent learning**: Relies entirely on context files

### Memory Locations
```
~/.codex/
├── config.toml         # Configuration
├── auth.json           # API credentials
├── history.jsonl       # Session history
└── sessions/           # Per-session state
```

### What's Readable/Exportable
- `history.jsonl` - All sessions (JSON lines)
- `config.toml` - Settings
- `sessions/` - Session snapshots

### Mental Model
Codex thinks in **tasks** - optimized for single-shot code generation:
1. Load context (files, instructions)
2. Generate code/solution
3. Log history
4. No cross-session learning

**Key Insight**: Codex is "goldfish memory" - powerful in-context but no persistence. Use CLAUDE.md files to give it memory.

---

## 4. MCP Memory Server Architecture

### How MCP Memory Works
- **Knowledge graph**: Entities + relations + observations
- **Persistent**: Survives across all sessions
- **Queryable**: Search, filter, traverse relations
- **Shared**: Any tool with MCP access can read/write

### Memory Structure
```json
{
  "entities": [
    {
      "name": "Pattern Name",
      "entityType": "Pattern",
      "observations": ["description", "category", "ROI"]
    }
  ],
  "relations": [
    {
      "from": "Pattern",
      "to": "Project",
      "relationType": "applies_to"
    }
  ]
}
```

### How to Use
```bash
# Read entire graph
mcp__memory__read_graph

# Search for patterns
mcp__memory__search_nodes "unity optimization"

# Create new knowledge
mcp__memory__create_entities [{"name": "...", "entityType": "Pattern", "observations": [...]}]
```

### Mental Model
MCP Memory is a **shared brain** - all tools can read/write:
1. Explicit knowledge (entities with names)
2. Relations (how things connect)
3. Observations (facts about entities)

**Key Insight**: MCP Memory is the ONLY truly cross-tool memory system. Everything else requires file-based bridging.

---

## 5. Unified Memory Strategy

### The Hierarchy (What Persists Where)

```
┌─────────────────────────────────────────────────────────────┐
│                    GLOBAL_RULES.md                          │
│         Universal patterns for ALL tools (7.6K tokens)      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     KnowledgeBase/                          │
│    LEARNING_LOG.md, _MASTER_*.md, indexed content (2M)     │
│              Symlinked to ALL tools                         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌───────────────┬───────────────┬───────────────┬─────────────┐
│ Claude        │ AntiGravity   │ Codex         │ MCP Memory  │
│ projects/     │ brain/        │ sessions/     │ graph.json  │
│ history.jsonl │ implicit/ (pb)│ history.jsonl │ entities[]  │
│ (ephemeral)   │ (persistent)  │ (ephemeral)   │ (permanent) │
└───────────────┴───────────────┴───────────────┴─────────────┘
```

### Cross-Pollination Rules

1. **Readable → KnowledgeBase**
   - Extract patterns from any readable memory
   - Encode in LEARNING_LOG.md or specialized KB file
   - All tools benefit on next session

2. **Important Patterns → GLOBAL_RULES.md**
   - Cross-project, high-ROI patterns
   - Automatically loaded by all tools

3. **Tool-Specific → Tool Config**
   - Claude quirks → ~/.claude/CLAUDE.md
   - Gemini quirks → ~/.gemini/GEMINI.md
   - Don't pollute global with tool-specific

4. **Entities → MCP Memory**
   - Queryable facts (projects, patterns, technologies)
   - Relations between entities
   - Use for structured knowledge, not prose

### Daily Sync Pattern
```bash
# Extract today's Claude learnings
grep "$(date +%Y-%m-%d)" ~/.claude/history.jsonl | jq -r '.content' | head -50

# Check AntiGravity brain activity
find ~/.gemini/antigravity/brain -mtime -1 -name "*.json"

# Sync important patterns to KB
# (Manual: copy valuable patterns to LEARNING_LOG.md)
```

---

## 6. Metathinking: How to Think About Thinking

### Pattern Recognition Loop
```
1. EXECUTE task with current knowledge
2. OBSERVE what works vs fails
3. EXTRACT the pattern (success or failure)
4. CLASSIFY: Which memory system should store this?
   - Universal → GLOBAL_RULES.md
   - Project-specific → project/CLAUDE.md
   - Discovery → LEARNING_LOG.md
   - Structured fact → MCP Memory
5. ENCODE in chosen system
6. VERIFY pattern accessible in next session
```

### Memory Selection Matrix

| Pattern Type | Primary Store | Backup Store |
|--------------|---------------|--------------|
| Debugging workflow | GLOBAL_RULES.md | LEARNING_LOG.md |
| Project-specific fix | project/CLAUDE.md | - |
| GitHub repo discovery | _MASTER_GITHUB_REPO_KB.md | MCP Memory |
| Performance pattern | _PERFORMANCE_PATTERNS.md | LEARNING_LOG.md |
| Tool-specific quirk | ~/.{tool}/*.md | LEARNING_LOG.md |
| Entity/relation | MCP Memory | KB file |

### Self-Improvement Questions
After each significant task, ask:

1. **Did I discover something reusable?** → LEARNING_LOG.md
2. **Did I find a better way?** → Update relevant KB file
3. **Did this change my mental model?** → Update GLOBAL_RULES.md
4. **Can I automate this?** → Create script in ~/.local/bin/

### Anti-Patterns to Avoid

| Anti-Pattern | Problem | Solution |
|--------------|---------|----------|
| Duplicate rules | Token waste, drift risk | Single source of truth |
| Unstructured notes | Hard to find later | Use KB file categories |
| Tool-specific in global | Confuses other tools | Keep in tool config |
| Over-engineering | Complexity > benefit | Simple files > complex tools |
| Manual only | Forgotten, inconsistent | Automate with scripts/hooks |

---

## 7. Bridging Memories Between Tools

### What CAN Be Shared
- MarkDown files (all tools read)
- JSON configs (parseable by all)
- Shell scripts (callable by all)
- MCP Memory entities (queryable by MCP-enabled tools)

### What CANNOT Be Shared
- AntiGravity `.pb` files (binary embeddings, tool-specific)
- Claude project session state (internal format)
- Codex session state (ephemeral by design)

### Simple Bridge Script
```bash
#!/bin/bash
# ~/.local/bin/sync-ai-memories
# Run daily to extract learnings from all tools

echo "=== AI Memory Sync $(date) ==="

# 1. Claude: Recent significant interactions
echo "Claude Code recent commands:"
tail -20 ~/.claude/history.jsonl 2>/dev/null | jq -r '.command // empty' | grep -v "^$" | head -5

# 2. AntiGravity: Recent brain contexts
echo -e "\nAntiGravity recent tasks:"
find ~/.gemini/antigravity/brain -name "metadata.json" -mtime -1 2>/dev/null | while read f; do
    jq -r '.title // .description // "untitled"' "$f" 2>/dev/null
done | head -5

# 3. Codex: Recent sessions
echo -e "\nCodex recent sessions:"
tail -5 ~/.codex/history.jsonl 2>/dev/null | jq -r '.session_id // empty'

# 4. Reminder
echo -e "\n=== Manual Step ==="
echo "Review above. Add valuable patterns to:"
echo "  ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md"
```

---

## 8. Quick Reference: Memory Commands

### Claude Code
```bash
# Read recent history
tail -50 ~/.claude/history.jsonl | jq '.'

# List projects with memory
ls -la ~/.claude/projects/

# Search file history
grep -r "pattern" ~/.claude/file-history/
```

### AntiGravity/Gemini
```bash
# List brain contexts
find ~/.gemini/antigravity/brain -name "*.json" | head -20

# Recent conversations
ls -lt ~/.gemini/antigravity/conversations/ | head -10

# Code tracker
cat ~/.gemini/antigravity/code_tracker/*.json 2>/dev/null | jq '.'
```

### Codex
```bash
# Session history
tail -20 ~/.codex/history.jsonl | jq '.'

# Config
cat ~/.codex/config.toml
```

### MCP Memory
```bash
# Via Claude Code MCP tools:
# mcp__memory__read_graph
# mcp__memory__search_nodes "query"
# mcp__memory__create_entities [...]
```

---

## 9. Architecture Principles

### Why This Design Works

1. **Layered Persistence**
   - Files survive everything
   - MCP Memory survives sessions
   - Tool memory is convenience, not truth

2. **Single Source of Truth**
   - GLOBAL_RULES.md is authoritative
   - KB files are indexed reference
   - Tool memories are local cache

3. **Graceful Degradation**
   - If MCP Memory fails → KB files work
   - If AntiGravity unavailable → Claude has KB
   - If any tool dies → files persist

4. **Minimal Token Overhead**
   - Load hierarchy: Global → Tool → Project
   - Total: ~9.3K tokens (down from 12.7K)
   - Selective loading, not everything every time

### Evolution Path

```
Current (8.5/10):
  Files → Symlinks → All tools share KB

Future (10/10):
  Files → MCP Memory → Real-time sync across tools
  + Auto-extraction from conversations
  + Semantic search across all memories
  + Cross-tool pattern detection
```

---

## Summary

**Claude Code**: Conversation-based, ephemeral. Memory = files you create.

**AntiGravity**: Embedding-based, persistent. Memory = implicit learning + brain contexts.

**Codex**: Task-based, stateless. Memory = what you give it each session.

**MCP Memory**: Graph-based, permanent. Memory = entities + relations.

**KnowledgeBase**: File-based, universal. Memory = the source of truth for ALL tools.

---

**The key insight**: Stop trying to make tools "remember" - instead, write things down where ALL tools can read them. The knowledgebase IS your AI memory. Use it.

---

## 10. Best Practices Per Tool (Avoid Issues)

### Claude Code
| Do | Don't |
|----|-------|
| Use CLAUDE.md for context | Store secrets in CLAUDE.md |
| Keep sessions focused | Run parallel git operations |
| Encode learnings in KB files | Rely on session memory |
| Use MCP Memory for facts | Overload context window |

### AntiGravity/Gemini
| Do | Don't |
|----|-------|
| Let implicit memory learn | Manually edit .pb files |
| Use brain/ for task context | Delete implicit/ folder |
| Check GEMINI.md conflict | Run Gemini CLI in same dir |
| Back up conversations/ | Assume memory persists to Claude |

### Codex CLI
| Do | Don't |
|----|-------|
| Provide full context each session | Expect it to remember |
| Use for single-shot tasks | Use for stateful workflows |
| Reference CLAUDE.md files | Assume shared memory with Claude |

### Cross-Tool Rules
| Do | Don't |
|----|-------|
| One Unity MCP connection | Multiple tools → same MCP |
| Sequential git operations | Parallel git from different tools |
| Respect lock files | Override locks blindly |
| Share via KB files | Assume memory syncs |

### Performance Guidelines
- **Token budget**: Keep GLOBAL_RULES.md < 10K tokens
- **File sizes**: Individual KB files < 100KB
- **Audit frequency**: Run `kb-audit` weekly minimum
- **Backup frequency**: Run `kb-backup` before major changes

---

## 11. Integrity & Self-Healing

### Validation Commands
```bash
# Quick health check (run anytime)
kb-audit

# View recent audit trends
tail -10 ~/.claude/knowledgebase/audit-history.jsonl | jq '.'

# Check for failures over time
grep '"fail":[1-9]' ~/.claude/knowledgebase/audit-history.jsonl

# Cross-tool status
sync-ai-memories
```

### Automatic Health Tracking
Every `kb-audit` run appends to `~/.claude/knowledgebase/audit-history.jsonl`:
```json
{"ts":"2026-01-12T...","pass":15,"warn":2,"fail":0,"heal":1}
```

### View Trends
```bash
# Last 10 audits
tail -10 ~/.claude/knowledgebase/audit-history.jsonl | jq '.'

# Failure trends (if any)
grep '"fail":[1-9]' ~/.claude/knowledgebase/audit-history.jsonl
```

### Self-Healing Capabilities
KB_AUDIT.sh automatically repairs:
- Broken symlinks (all 3 IDE tools)
- Missing symlinks (creates if IDE dir exists)

### What It Doesn't Auto-Fix (Requires Manual)
- Git repository corruption → `git reset --hard` or restore from backup
- Missing critical files → Restore from `~/Documents/GitHub/code-backups/KB-*/`
- Config drift → Review and merge manually

---

*Last verified: 2026-01-12*
*Related: GLOBAL_RULES.md, LEARNING_LOG.md, _MASTER_AI_TOOLS_REGISTRY.md*
