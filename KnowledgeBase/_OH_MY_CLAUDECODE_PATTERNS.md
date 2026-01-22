# Oh-My-ClaudeCode & OpenCode Patterns Reference

**Sources**:
- https://github.com/Yeachan-Heo/oh-my-claudecode
- https://github.com/code-yeongyu/oh-my-opencode

**Extracted**: 2026-01-22
**Purpose**: Best practices for multi-agent orchestration without external dependencies

---

## Core Philosophy

> "Zero learning curve. Maximum power."
> "Don't make users learn commands—detect intent and activate automatically."

**Key Insight**: Auto-detect intent from conversation, don't require explicit commands.

### Auto-Detection in Action

The `auto-detect-agent.sh` hook analyzes every prompt and automatically:

| Pattern Detected | Mode Activated | Agent Suggested |
|------------------|----------------|-----------------|
| "setup", "implement", "create" | PERSIST | — |
| "debug", "analyze", "why" | DEEP | — |
| "fix all", "across project" | PARALLEL | — |
| >50 words | ULTRAWORK | — |
| "status", "what is" (<15 words) | QUICK | health-monitor |
| CS0246, "error", "broken" | — | unity-error-fixer |
| "still doesn't work" | DEEP | unity-error-fixer-deep |
| "architect", "design system" | DEEP | tech-lead-advisor |

**Users just talk naturally.** The system adapts.

---

## Agent Architecture Patterns

### 1. Tiered Model Routing

| Tier | Model | Use Case | Cost |
|------|-------|----------|------|
| `-low` | Haiku | Quick checks, simple tasks | 0.3x |
| (default) | Sonnet | Standard work | 1x |
| `-high` | Opus | Complex reasoning, architecture | 3-5x |

**Example**: `scientist-low`, `scientist`, `scientist-high`

### 2. Role Separation

| Role | Can Do | Cannot Do |
|------|--------|-----------|
| **Architect/Oracle** | Analyze, recommend | Modify files |
| **Researcher/Librarian** | Search, synthesize | Make changes |
| **Executor/Sisyphus** | Implement | Delegate, spawn agents |
| **Critic** | Review, find issues | Fix issues |

**Key Insight**: Architects should be READ-ONLY to force analysis before action.

### 3. Todo Discipline (Iron Law)

From Executor pattern:
```
1. Multi-step work → TodoWrite breakdown FIRST
2. Mark in_progress BEFORE starting each task
3. Complete and mark IMMEDIATELY after finishing
4. NEVER batch completions
```

### 4. Verification Protocol

Before claiming completion:
```
1. What command PROVES the claim?
2. Execute verification (test, build, lint)
3. Read and validate ACTUAL output
4. Only then claim completion WITH EVIDENCE
```

**Red Flags** (require reverification):
- Hedging language ("should work", "probably fixed")
- Satisfaction expressed before running tests
- Claims without evidence

---

## Magic Keywords

| Keyword | Behavior |
|---------|----------|
| `ralph` | Persistence mode - continue until VERIFIED complete |
| `ulw` | Maximum parallel execution |
| `plan` | Structured planning interview |
| `autopilot` | Full autonomous workflow |
| `ultrawork` | All capabilities combined |

**Composable**: `ralph ulw: migrate database`

---

## Structured Output Markers

### Scientist/Research Markers

```
[OBJECTIVE] Task goal statement
[DATA] Dataset loaded, N records
[FINDING] Discovery with evidence
[STAT:name] Statistical evidence (p-value, CI, effect size)
[LIMITATION] Acknowledged constraint
[STAGE:begin/end] Phase tracking
```

### Architect/Analysis Markers

```
[ANALYSIS] Current state summary
[FINDING] Issue with file:line reference
[RECOMMENDATION] Proposed approach
[TRADE-OFF] Pros/cons evaluation
[RISK] Potential problems
[NEXT] Handoff instructions
```

### Quality Gates

Every [FINDING] requires evidence within 5-10 lines:
- Source URL or file:line
- Sample size or scope
- Confidence qualifier
- Statistical backing where applicable

---

## 3-Failure Circuit Breaker

```
Failure 1: (silent tracking)
Failure 2: [WARNING] - 1 more triggers escalation
Failure 3: [CIRCUIT BREAKER] - Stop, escalate, re-evaluate
```

**Actions on Circuit Break**:
1. Stop current approach
2. Search KB for existing solutions
3. Consider architectural issues
4. Escalate to higher model or user

---

## Research Workflow (5 Steps)

From Researcher/Librarian pattern:

```
1. CLARIFY - Specific research question
2. IDENTIFY - Relevant external sources
3. FORMULATE - Targeted search queries
4. GATHER - Information from multiple sources
5. SYNTHESIZE - Findings with attribution
```

**External Sources Priority**:
1. Official documentation
2. GitHub repos with working code
3. Package repos (npm, PyPI)
4. Stack Overflow (verified answers)
5. Technical blogs

**Always Include**:
- Version compatibility notes
- Code examples when available
- Flagged outdated information

---

## Parallel Execution Pattern

From oh-my-opencode:

```
Main Agent (Opus) - Focused execution
  ├── Background Agent 1 (Haiku) - Codebase mapping
  ├── Background Agent 2 (Haiku) - Documentation lookup
  └── Background Agent 3 (Haiku) - Test discovery
```

**Benefits**:
- Main agent stays focused
- Cheap models do parallel exploration
- Reduces context bloat

---

## Anti-Patterns to Avoid

| Anti-Pattern | Better Approach |
|--------------|-----------------|
| Generic advice | Specific file:line references |
| "Should work" claims | Verified with evidence |
| Batch completions | Complete one at a time |
| Architects that edit | Read-only analysis first |
| Single model for all | Tiered model routing |
| Manual keyword invocation | Auto-detection from context |

---

## Integration with Your System

### Already Implemented (via hooks)

- ✅ Auto-detection (auto-detect-agent.sh)
- ✅ Magic keywords (magic-keywords.sh)
- ✅ Circuit breaker (failure-tracker.sh)
- ✅ Read-only architect (tech-lead-advisor.md)

### To Add

- [ ] Tiered agent variants (-low, -high)
- [ ] Structured output markers in all agents
- [ ] Quality gates enforcement
- [ ] Parallel background exploration
- [ ] Verification protocol in executor agents

---

## Quick Reference

### When to Use Each Tier

| Task | Tier | Agent |
|------|------|-------|
| Console check | low | unity-console-checker |
| Quick search | low | code-searcher |
| Standard fix | default | unity-error-fixer |
| Research | default | research-agent |
| Architecture | high | tech-lead-advisor |
| Complex debug | high | unity-error-fixer-deep |

### When to Use Keywords

| Situation | Keyword |
|-----------|---------|
| Task keeps stopping early | `persist` |
| Multiple independent subtasks | `parallel` |
| Need thorough analysis | `deep` |
| Want hands-off execution | `ultrawork` |
| Quick simple task | `quick` |

---

## Oh-My-OpenCode Specific Patterns

**Source**: https://github.com/code-yeongyu/oh-my-opencode

### Multi-Model Orchestration

oh-my-opencode uses PURPOSE-BASED model routing:

| Agent | Model | Purpose |
|-------|-------|---------|
| **Sisyphus** | Opus 4.5 | Primary orchestrator |
| **Oracle** | GPT 5.2 | Architecture & debugging consultation |
| **Frontend Engineer** | Gemini 3 Pro | UI/UX development |
| **Librarian** | Sonnet 4.5 | Docs & codebase exploration |
| **Explore** | Grok | Fast grep/search |

**Key Insight**: Different models excel at different tasks. Route by purpose, not just complexity.

### Todo Continuation Enforcer

Prevents sessions from ending with incomplete work:

```bash
# Stop hook checks for incomplete todos
# Warns user if tasks remain pending
# Suggests using "persist" keyword next time
```

**Implementation**: ~/.claude/hooks/todo-enforcer.sh

### Ultrathink Keyword

More than just "deep" - maximum reasoning depth:

```
[ULTRATHINK MODE]
- Think through ALL implications before acting
- Consider 3+ alternative approaches
- Identify hidden assumptions and edge cases
- Question your first instinct
- Verify reasoning chain is sound
- Only proceed when confident in approach
```

**Trigger**: `ultrathink` or `ut`

### Comment Quality (Human-Indistinguishable Code)

oh-my-opencode enforces comment quality:
- Auto-remove unnecessary comments
- Require justification for remaining comments
- Code should look like human wrote it

### Temperature Control

For code agents: **Temperature 0.1** (max 0.3)
- Consistent output
- Deterministic behavior
- Reliable refactoring

### LSP/AST Integration

- Full Language Server Protocol support
- AstGrep for deterministic transformations
- Surgical modifications vs token-based rewrites

### Aggressive Context Management

**Delegation Pattern**:
```
Main Agent (focused) ──► Spawns cheap models for:
  ├── Background exploration
  ├── Documentation lookup
  └── Test discovery
```

**Benefits**: Main context stays lean, parallel work happens in background.

---

### Already Implemented (via hooks)

- ✅ Auto-detection (auto-detect-agent.sh)
- ✅ Magic keywords including ultrathink (magic-keywords.sh)
- ✅ Circuit breaker (failure-tracker.sh)
- ✅ Read-only architect (tech-lead-advisor.md)
- ✅ Todo Continuation Enforcer (todo-enforcer.sh)
- ✅ Tiered agent variants (-quick, -deep)
- ✅ Structured output markers (_AGENT_SHARED_RULES.md)

---

**Last Updated**: 2026-01-22
**Source Repos**:
- https://github.com/Yeachan-Heo/oh-my-claudecode
- https://github.com/code-yeongyu/oh-my-opencode
