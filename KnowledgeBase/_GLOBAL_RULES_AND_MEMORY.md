# Global Rules & Memory Locations

## Memory Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    AI MEMORY SYSTEM                         │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  RULES (What to do)              MEMORY (What was learned)  │
│  ─────────────────               ────────────────────────   │
│  ~/GLOBAL_RULES.md               MCP Memory Graph           │
│  ~/.claude/CLAUDE.md             ~/Applications/            │
│  project/CLAUDE.md                 claude_memory.json       │
│                                                             │
│  LEARNINGS (Discoveries)         KNOWLEDGEBASE (Reference)  │
│  ───────────────────────         ─────────────────────────  │
│  LEARNING_LOG.md                 ~/Documents/GitHub/        │
│  (append-only journal)             Unity-XR-AI/KnowledgeBase│
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## File Locations

### Rules Files

| File | Location | Purpose | Tokens |
|------|----------|---------|--------|
| GLOBAL_RULES.md | `~/GLOBAL_RULES.md` | Universal rules for all AI tools | ~3,650 |
| Claude CLAUDE.md | `~/.claude/CLAUDE.md` | Claude Code specific config | ~900 |
| AI Agent V3 | `~/.claude/AI_AGENT_CORE_DIRECTIVE_V3.md` | Intelligence amplification | ~875 |
| Project CLAUDE.md | `project/CLAUDE.md` | Project-specific overrides | ~variable |

### Memory Files

| File | Location | Purpose |
|------|----------|---------|
| MCP Memory | `~/Applications/claude_memory.json` | Structured entity/relation graph |
| Learning Log | `KnowledgeBase/LEARNING_LOG.md` | Append-only discoveries journal |
| KB Files | `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/*.md` | Reference documentation |

---

## GLOBAL_RULES.md Contents

**Location**: `~/GLOBAL_RULES.md`
**Tokens**: ~3,650 (optimized from ~8,800)

### Sections

1. **Debugging & Iteration Protocols**
   - State-of-the-art debugging (binary search isolation)
   - Multi-layer verbose logging (4 channels)
   - MCP-first verification loop
   - Iteration speed optimization
   - Nuclear option for persistent failures

2. **Spec-Driven Development**
   - Always use todo lists
   - Spec-Kit workflow: `Constitution → Specify → Clarify → Plan → Tasks → Implement`

3. **Metacognitive Intelligence**
   - Persistent long-term memory encoding
   - Self-improvement loop
   - Resource-aware thinking
   - Self-audit triggers

4. **Automatic Pattern Extraction**
   - Session-end protocol
   - Log to LEARNING_LOG.md
   - Create MCP memory entities

5. **Process & Agent Coordination**
   - Process awareness (never kill without verification)
   - Agent parallelism rules
   - Cross-tool awareness
   - Device availability checks

6. **Cross-Tool Memory Architecture**
   - Files are memory principle
   - Storage hierarchy

7. **Unity MCP Server**
   - Quick fixes for common issues

---

## MCP Memory System

### Config Location
```json
// In ~/.claude.json
"memory": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-memory"],
  "env": {
    "MEMORY_FILE_PATH": "/Users/jamestunick/Applications/claude_memory.json"
  }
}
```

### Memory File
**Location**: `~/Applications/claude_memory.json`
**Size**: ~38KB (grows over time)

### Entity Types Stored
- Projects (H3M Portals, Paint AR, WarpJobs, etc.)
- Workflows (Daily Priority System, Pattern Extraction)
- Preferences (JamesBuildPreference, TokenUsagePreference)
- Repositories (keijiro repos, Unity samples)
- Technical Concepts (XRAI-Format, VNMF, MCP)
- Solutions (VFX-iOS-Shader-Stripping-Fix)
- People (James Tunick, keijiro)

### Memory Operations
```bash
# MCP memory tools available in Claude Code
mcp__memory__create_entities    # Add new entities
mcp__memory__add_observations   # Add facts to entities
mcp__memory__create_relations   # Link entities
mcp__memory__search_nodes       # Query memory
mcp__memory__read_graph         # Get full graph
```

---

## LEARNING_LOG.md

**Location**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md`
**Size**: ~50KB
**Format**: Append-only markdown journal

### Entry Format
```markdown
## YYYY-MM-DD HH:MM - [Tool] - [Topic]

**Discovery**: [One sentence summary]
**Context**: [Why this matters]
**Impact**: [Benefits]
**Pattern**: [Code/steps if applicable]
**Category**: [debugging|architecture|optimization|workflow|mental-model|anti-pattern]
**ROI**: [Low|Medium|High] - [Brief justification]
**Related**: [Links to knowledgebase files or MCP entities]
```

### Auto-Extraction Triggers
- Solved non-obvious problem
- Found reusable pattern (2+ projects)
- Discovered anti-pattern
- Saved significant time
- Learned new mental model

---

## Memory Hierarchy

| Pattern Type | Store In | Persistence |
|--------------|----------|-------------|
| Universal rules | `GLOBAL_RULES.md` | Permanent |
| Discoveries | `LEARNING_LOG.md` | Append-only |
| Structured facts | MCP Memory entities | Queryable graph |
| Tool-specific | `~/.{tool}/*.md` | Per-tool |
| Project-specific | `project/CLAUDE.md` | Per-project |

---

## Self-Improvement Loop

```
Execute Task
    ↓
Observe Results
    ↓
Extract Pattern ──→ LEARNING_LOG.md
    ↓
Encode to Memory ──→ MCP Memory Graph
    ↓
Monthly Promotion ──→ GLOBAL_RULES.md (if high-ROI)
    ↓
Verify Persistence
```

---

## Quick Commands

```bash
# View memory file
cat ~/Applications/claude_memory.json | jq '.entities[] | .name'

# View learning log
cat ~/.claude/knowledgebase/LEARNING_LOG.md | tail -100

# Check memory size
ls -lh ~/Applications/claude_memory.json

# Search memory (in Claude Code)
# Use: mcp__memory__search_nodes with query
```

---

## Cross-References

- `_AI_CONFIG_FILES_REFERENCE.md` - All config file locations
- `_AI_HEALTH_SCRIPTS.md` - Health check scripts
- `_AI_MEMORY_SYSTEMS_DEEP_DIVE.md` - Detailed memory architecture
- `_SELF_IMPROVING_MEMORY_ARCHITECTURE.md` - Auto-improvement system

---

*Updated: 2026-01-13*
