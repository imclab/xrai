# Claude Code Subagents Knowledge Base

> **Source**: https://code.claude.com/docs/en/sub-agents  
> **Last Updated**: 2025-01-13

---

## Overview

Subagents are specialized AI assistants that handle specific types of tasks within Claude Code. Each subagent runs in its own context window with:
- Custom system prompt
- Specific tool access
- Independent permissions
- Separate context from main conversation

## Key Benefits

- **Preserve context**: Keep exploration/implementation out of main conversation
- **Enforce constraints**: Limit which tools a subagent can use
- **Reuse configurations**: User-level subagents work across projects
- **Specialize behavior**: Focused prompts for specific domains
- **Control costs**: Route to faster/cheaper models like Haiku

---

## Built-in Subagents

| Subagent | Model | Tools | Purpose |
|----------|-------|-------|---------|
| **Explore** | Haiku | Read-only | Fast codebase search and analysis |
| **Plan** | Inherit | Read-only | Research for plan mode |
| **general-purpose** | Inherit | All tools | Complex multi-step tasks |
| **Bash** | Inherit | - | Terminal commands in separate context |
| **Claude Code Guide** | Haiku | - | Questions about Claude Code features |

### Explore Thoroughness Levels
- `quick`: Targeted lookups
- `medium`: Balanced exploration
- `very thorough`: Comprehensive analysis

---

## Creating Custom Subagents

### File Locations (by priority)

| Location | Scope | Priority |
|----------|-------|----------|
| `--agents` CLI flag | Current session | 1 (highest) |
| `.claude/agents/` | Current project | 2 |
| `~/.claude/agents/` | All projects | 3 |
| Plugin's `agents/` | Where plugin enabled | 4 (lowest) |

### Subagent File Format

```yaml
---
name: code-reviewer
description: Reviews code for quality and best practices
tools: Read, Glob, Grep
model: sonnet
---

You are a code reviewer. When invoked, analyze the code and provide
specific, actionable feedback on quality, security, and best practices.
```

### Frontmatter Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Unique identifier (lowercase, hyphens) |
| `description` | Yes | When Claude should delegate to this subagent |
| `tools` | No | Allowed tools (inherits all if omitted) |
| `disallowedTools` | No | Tools to deny |
| `model` | No | `sonnet`, `opus`, `haiku`, or `inherit` |
| `permissionMode` | No | `default`, `acceptEdits`, `dontAsk`, `bypassPermissions`, `plan` |
| `skills` | No | Skills to load at startup |
| `hooks` | No | Lifecycle hooks for this subagent |

---

## CLI Definition

```bash
claude --agents '{
  "code-reviewer": {
    "description": "Expert code reviewer. Use proactively after code changes.",
    "prompt": "You are a senior code reviewer...",
    "tools": ["Read", "Grep", "Glob", "Bash"],
    "model": "sonnet"
  }
}'
```

---

## Permission Modes

| Mode | Behavior |
|------|----------|
| `default` | Standard permission prompts |
| `acceptEdits` | Auto-accept file edits |
| `dontAsk` | Auto-deny prompts (allowed tools still work) |
| `bypassPermissions` | Skip all checks |
| `plan` | Read-only exploration mode |

---

## Hooks for Subagents

### In Frontmatter

```yaml
hooks:
  PreToolUse:
    - matcher: "Bash"
      hooks:
        - type: command
          command: "./scripts/validate-command.sh"
  PostToolUse:
    - matcher: "Edit|Write"
      hooks:
        - type: command
          command: "./scripts/run-linter.sh"
```

### In settings.json

```json
{
  "hooks": {
    "SubagentStart": [
      {
        "matcher": "db-agent",
        "hooks": [{ "type": "command", "command": "./scripts/setup.sh" }]
      }
    ],
    "SubagentStop": [
      {
        "matcher": "db-agent", 
        "hooks": [{ "type": "command", "command": "./scripts/cleanup.sh" }]
      }
    ]
  }
}
```

---

## Foreground vs Background

| Mode | Behavior |
|------|----------|
| **Foreground** | Blocks main conversation, interactive prompts work |
| **Background** | Runs concurrently, inherits permissions, auto-denies missing perms |

**Disable background tasks**: `CLAUDE_CODE_DISABLE_BACKGROUND_TASKS=1`

---

## Common Patterns

### Isolate High-Volume Operations
```
Use a subagent to run the test suite and report only the failing tests
```

### Run Parallel Research
```
Research authentication, database, and API modules in parallel using separate subagents
```

### Chain Subagents
```
Use code-reviewer to find issues, then use optimizer to fix them
```

---

## Example Subagents

### Code Reviewer (Read-Only)
```yaml
---
name: code-reviewer
description: Expert code review specialist. Use proactively after code changes.
tools: Read, Grep, Glob, Bash
model: inherit
---

You are a senior code reviewer ensuring high standards.

When invoked:
1. Run git diff to see recent changes
2. Focus on modified files
3. Begin review immediately

Review checklist:
- Code clarity and readability
- Well-named functions/variables
- No duplicated code
- Proper error handling
- No exposed secrets
- Input validation
- Test coverage
- Performance considerations
```

### Debugger (Can Edit)
```yaml
---
name: debugger
description: Debugging specialist for errors and test failures.
tools: Read, Edit, Bash, Grep, Glob
---

You are an expert debugger specializing in root cause analysis.

Debugging process:
1. Capture error and stack trace
2. Identify reproduction steps
3. Isolate failure location
4. Implement minimal fix
5. Verify solution works
```

### Database Query Validator (Hook-Based)
```yaml
---
name: db-reader
description: Execute read-only database queries.
tools: Bash
hooks:
  PreToolUse:
    - matcher: "Bash"
      hooks:
        - type: command
          command: "./scripts/validate-readonly-query.sh"
---

You are a database analyst with read-only access.
Execute SELECT queries to answer questions about data.
```

Validation script:
```bash
#!/bin/bash
INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if echo "$COMMAND" | grep -iE '\b(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)\b' > /dev/null; then
  echo "Blocked: Write operations not allowed" >&2
  exit 2
fi
exit 0
```

---

## Disabling Subagents

In settings.json:
```json
{
  "permissions": {
    "deny": ["Task(Explore)", "Task(my-custom-agent)"]
  }
}
```

Or via CLI:
```bash
claude --disallowedTools "Task(Explore)"
```

---

## Resuming Subagents

Subagents can be resumed with full context:
```
Continue that code review and now analyze the authorization logic
```

Transcripts stored at: `~/.claude/projects/{project}/{sessionId}/subagents/agent-{agentId}.jsonl`

---

## When to Use Subagents vs Main Conversation

**Use Main Conversation**:
- Frequent back-and-forth needed
- Multiple phases share context
- Quick, targeted changes
- Latency matters

**Use Subagents**:
- Task produces verbose output
- Need specific tool restrictions
- Work is self-contained
- Want isolated context

---

*Created for Unity-XR-AI Knowledge Base*
