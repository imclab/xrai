# Claude Code Hooks Reference

> **Sources**:  
> - https://code.claude.com/docs/en/hooks-guide  
> - https://code.claude.com/docs/en/hooks  
> **Last Updated**: 2025-01-13

---

## Overview

Hooks are user-defined shell commands that execute at various points in Claude Code's lifecycle. They provide **deterministic control** over behavior - ensuring actions always happen rather than relying on LLM choice.

### Use Cases
- **Notifications**: Custom alerts when Claude awaits input
- **Auto-formatting**: Run prettier/gofmt after file edits
- **Logging**: Track all executed commands for compliance
- **Feedback**: Automated feedback on code conventions
- **Custom permissions**: Block modifications to sensitive files

---

## Hook Events

| Event | When it Fires | Matcher Support |
|-------|---------------|-----------------|
| `PreToolUse` | Before tool calls (can block) | Yes |
| `PermissionRequest` | When permission dialog shown | Yes |
| `PostToolUse` | After tool completes | Yes |
| `UserPromptSubmit` | When user submits prompt | No |
| `Notification` | When Claude sends notifications | Yes |
| `Stop` | When Claude finishes responding | No |
| `SubagentStop` | When subagent completes | No |
| `PreCompact` | Before compact operation | Yes (`manual`/`auto`) |
| `SessionStart` | Session starts/resumes | Yes (`startup`/`resume`/`clear`/`compact`) |
| `SessionEnd` | Session ends | No |

---

## Configuration

### Locations (by priority)
1. `.claude/settings.local.json` - Local project (not committed)
2. `.claude/settings.json` - Project settings
3. `~/.claude/settings.json` - User settings

### Structure

```json
{
  "hooks": {
    "EventName": [
      {
        "matcher": "ToolPattern",
        "hooks": [
          {
            "type": "command",
            "command": "your-command-here",
            "timeout": 30
          }
        ]
      }
    ]
  }
}
```

### Matcher Patterns
- Simple: `Write` matches only Write tool
- Regex: `Edit|Write` matches both
- `*` or `""` matches all tools

---

## Quick Examples

### Log All Bash Commands

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [{
          "type": "command",
          "command": "jq -r '.tool_input.command' >> ~/.claude/bash-log.txt"
        }]
      }
    ]
  }
}
```

### Auto-Format TypeScript

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [{
          "type": "command",
          "command": "jq -r '.tool_input.file_path' | { read f; [[ $f == *.ts ]] && npx prettier --write \"$f\"; }"
        }]
      }
    ]
  }
}
```

### Desktop Notifications

```json
{
  "hooks": {
    "Notification": [
      {
        "matcher": "",
        "hooks": [{
          "type": "command",
          "command": "notify-send 'Claude Code' 'Awaiting your input'"
        }]
      }
    ]
  }
}
```

### Block Sensitive Files

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [{
          "type": "command",
          "command": "python3 -c \"import json,sys; d=json.load(sys.stdin); p=d.get('tool_input',{}).get('file_path',''); sys.exit(2 if any(x in p for x in ['.env','.git/']) else 0)\""
        }]
      }
    ]
  }
}
```

---

## Exit Codes

| Code | Behavior |
|------|----------|
| 0 | Success - stdout shown in verbose mode |
| 2 | **Blocking error** - stderr fed back to Claude |
| Other | Non-blocking error - stderr shown to user |

### Exit Code 2 per Event

| Event | Exit 2 Behavior |
|-------|-----------------|
| `PreToolUse` | Blocks tool call, shows stderr to Claude |
| `PermissionRequest` | Denies permission |
| `PostToolUse` | Shows stderr to Claude (tool already ran) |
| `Stop` | Blocks stoppage, shows stderr to Claude |
| `UserPromptSubmit` | Blocks prompt, erases it |

---

## Hook Input (JSON via stdin)

### Common Fields

```json
{
  "session_id": "abc123",
  "transcript_path": "/path/to/transcript.jsonl",
  "cwd": "/current/working/directory",
  "permission_mode": "default",
  "hook_event_name": "PreToolUse"
}
```

### PreToolUse - Bash

```json
{
  "tool_name": "Bash",
  "tool_input": {
    "command": "npm test",
    "description": "Run tests",
    "timeout": 120000
  },
  "tool_use_id": "toolu_01ABC..."
}
```

### PreToolUse - Write/Edit

```json
{
  "tool_name": "Write",
  "tool_input": {
    "file_path": "/path/to/file.txt",
    "content": "file content"
  }
}
```

### PostToolUse

```json
{
  "tool_name": "Write",
  "tool_input": { "file_path": "...", "content": "..." },
  "tool_response": { "filePath": "...", "success": true }
}
```

---

## JSON Output (Advanced)

### Common Fields

```json
{
  "continue": true,
  "stopReason": "string",
  "suppressOutput": true,
  "systemMessage": "string"
}
```

### PreToolUse Decision Control

```json
{
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "allow|deny|ask",
    "permissionDecisionReason": "Reason here",
    "updatedInput": { "field": "new value" }
  }
}
```

### PostToolUse Feedback

```json
{
  "decision": "block",
  "reason": "Explanation",
  "hookSpecificOutput": {
    "hookEventName": "PostToolUse",
    "additionalContext": "Info for Claude"
  }
}
```

### Stop/SubagentStop Control

```json
{
  "decision": "block",
  "reason": "Must continue because..."
}
```

### UserPromptSubmit

```json
{
  "decision": "block",
  "reason": "Cannot process this prompt",
  "hookSpecificOutput": {
    "hookEventName": "UserPromptSubmit",
    "additionalContext": "Additional context"
  }
}
```

---

## Prompt-Based Hooks

Use LLM (Haiku) for intelligent decisions instead of bash:

```json
{
  "hooks": {
    "Stop": [{
      "hooks": [{
        "type": "prompt",
        "prompt": "Evaluate if Claude should stop: $ARGUMENTS. Check if all tasks are complete.",
        "timeout": 30
      }]
    }]
  }
}
```

LLM must respond with:
```json
{ "ok": true | false, "reason": "explanation" }
```

---

## MCP Tool Hooks

MCP tools follow pattern `mcp__<server>__<tool>`:

```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "mcp__memory__.*",
      "hooks": [{
        "type": "command",
        "command": "echo 'Memory operation' >> ~/mcp.log"
      }]
    }]
  }
}
```

---

## SessionStart Environment Variables

Persist env vars for session:

```bash
#!/bin/bash
if [ -n "$CLAUDE_ENV_FILE" ]; then
  echo 'export NODE_ENV=production' >> "$CLAUDE_ENV_FILE"
  echo 'export API_KEY=secret' >> "$CLAUDE_ENV_FILE"
fi
exit 0
```

---

## Environment Variables

| Variable | Description |
|----------|-------------|
| `CLAUDE_PROJECT_DIR` | Absolute path to project root |
| `CLAUDE_ENV_FILE` | File for persisting env vars (SessionStart) |
| `CLAUDE_CODE_REMOTE` | `"true"` if remote/web environment |
| `CLAUDE_PLUGIN_ROOT` | Plugin directory (for plugin hooks) |

---

## Hook Execution Details

- **Timeout**: 60 seconds default, configurable per command
- **Parallelization**: All matching hooks run in parallel
- **Deduplication**: Identical commands deduplicated
- **Environment**: Runs in cwd with Claude Code's environment

---

## Security Best Practices

1. **Validate inputs** - Never trust blindly
2. **Quote shell variables** - Use `"$VAR"` not `$VAR`
3. **Block path traversal** - Check for `..`
4. **Use absolute paths** - Specify full paths
5. **Skip sensitive files** - Avoid `.env`, `.git/`, keys

---

## Debugging

1. Run `/hooks` to check registration
2. Test commands manually first
3. Use `claude --debug` for execution details
4. Ensure scripts are executable (`chmod +x`)
5. Check JSON syntax in settings

---

*Created for Unity-XR-AI Knowledge Base*
