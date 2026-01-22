# Directive Conflicts & Resolution

**Version**: 1.0 (2026-01-21)
**Purpose**: Identify and resolve conflicts between configuration files.

---

## Identified Conflicts

### 1. JetBrains vs Raw Tools in Agents

**Conflict**: GLOBAL_RULES says "NEVER use raw Grep/Glob/Read when Rider open" but agents have these tools.

**Affected Agents**:
- `research-agent` (tools: Read, Grep, Glob)
- `orchestrator-agent` (tools: Read, Grep, Glob, Bash)
- `tech-lead` (tools: Read, Grep, Glob, Bash)
- `code-tester` (tools: Read, Bash, Grep)

**Resolution**: Agents run in **subagent context** and don't have access to JetBrains MCP. This is acceptable because:
- Subagents can't use MCP tools in background mode
- The main conversation should use JetBrains when available
- Agents should note "use JetBrains MCP if Rider open" in their prompts

**Status**: ✅ Acceptable (agents are fallback when JetBrains unavailable)

---

### 2. AI_AGENT_CORE_DIRECTIVE_V3 May Not Be Read

**Conflict**: File is referenced in hierarchy but not auto-loaded.

**Location**: `~/.claude/AI_AGENT_CORE_DIRECTIVE_V3.md`

**Resolution**: Add explicit reference in ~/.claude/CLAUDE.md startup section.

**Status**: ⚠️ Needs attention (documented but not enforced)

---

### 3. Project CLAUDE.md Doesn't Reference GLOBAL_RULES

**Conflict**: `Unity-XR-AI/CLAUDE.md` is a README, not a rules file.

**Resolution**: This is correct behavior - project CLAUDE.md is project documentation, not rules. GLOBAL_RULES is loaded from ~ directory.

**Status**: ✅ No conflict (different purposes)

---

### 4. Token Limit Not Enforced

**Conflict**: "Stay below 95% weekly limits" is a rule with no enforcement.

**Resolution**:
- Added `/cost` check to startup tasks
- Added reminder in GLOBAL_RULES header
- Consider adding hook for automatic warning

**Status**: ⚠️ Manual enforcement only

---

### 5. Thinking Mode Conflicts

**Conflict**: Token optimization says "MAX_THINKING_TOKENS=10000" but some tasks need more.

**Resolution**: Use graduated language:
- Default: 10K tokens
- "think hard": 15K
- "ultrathink": 31,999

**Status**: ✅ Resolved with triggers

---

## Priority Order (When Rules Conflict)

```
1. Safety (never delete without explicit request)
2. Token efficiency (stay under limits)
3. Speed (JetBrains over raw tools)
4. Accuracy (verify before acting)
5. Project-specific (CLAUDE.md overrides)
```

---

## Gaps Identified

### Gap 1: No Automatic Directive Loading
**Issue**: AI_AGENT_CORE_DIRECTIVE_V3.md is not read automatically.
**Impact**: Core philosophy may be ignored.
**Mitigation**: Reference in CLAUDE.md, manual invocation with "Apply Directive".

### Gap 2: Agent Context Isolation
**Issue**: Subagents don't see Rider status.
**Impact**: May use Grep when JetBrains available.
**Mitigation**: Agents note JetBrains preference in prompts.

### Gap 3: Cross-Tool State Sharing
**Issue**: Windsurf/Cursor don't share agent definitions.
**Impact**: Only Claude Code has optimized agents.
**Mitigation**: Acceptable - agents are Claude Code specific.

### Gap 4: Memory Queries Without Project Name
**Issue**: claude-mem searches may cross-contaminate.
**Impact**: Wrong context retrieved.
**Mitigation**: Added reminder: "ALWAYS include project name in query".

---

## Enforcement Mechanisms

### Automatic
- `ENABLE_TOOL_SEARCH=auto:5` - Dynamic MCP loading
- `MAX_THINKING_TOKENS=10000` - Limits reasoning cost
- PreToolUse hook - Filters test output

### Manual (User Must Remember)
- `/cost` check at session start
- JetBrains preference when Rider open
- Token limit monitoring
- Memory query with project name

### Recommended Additions
1. **Session start hook**: Auto-check `/cost`
2. **Context warning hook**: Alert at 100K tokens
3. **Rider detection**: Auto-switch to JetBrains MCP

---

## Rule Precedence Table

| Source | Priority | Scope | Override |
|--------|----------|-------|----------|
| Safety rules | 1 (highest) | All | Never |
| GLOBAL_RULES.md | 2 | All tools | By project only |
| ~/.claude/CLAUDE.md | 3 | Claude Code | By project |
| project/CLAUDE.md | 4 | Project | Final |
| Agent prompts | 5 | Agent context | By task |

---

## Verification Checklist

Run periodically to ensure directives are followed:

- [ ] `/cost` shows <95% usage
- [ ] JetBrains MCP used when Rider open
- [ ] Subagents use appropriate model (Haiku for simple)
- [ ] Edit used instead of Write
- [ ] Console checked after code changes
- [ ] Batch operations used (not sequential)
- [ ] Memory queries include project name

---

**Last Updated**: 2026-01-21
