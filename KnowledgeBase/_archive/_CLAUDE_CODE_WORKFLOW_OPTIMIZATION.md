# Claude Code Workflow Optimization

## Permission Configuration

### Global Settings Location
**File**: `~/.claude/settings.json`

This file persists across:
- All sessions
- All terminal windows
- All projects
- All Claude Code instances

### Permission Syntax (Correct)

```json
{
  "permissions": {
    "allow": [
      "Bash",                              // ALL bash commands
      "Read",                              // ALL file reads
      "Write",                             // ALL file writes
      "Edit",                              // ALL edits
      "Glob",                              // ALL glob searches
      "Grep",                              // ALL grep searches
      "WebSearch",                         // Web search
      "WebFetch(domain:github.com)",       // Specific domain
      "WebFetch(domain:docs.unity3d.com)", // Specific domain
      "mcp__UnityMCP__*",                  // All Unity MCP tools
      "mcp__github__*",                    // All GitHub MCP tools
      "mcp__filesystem__*"                 // All filesystem MCP tools
    ],
    "deny": [
      "**/.env*",                          // Block env files
      "**/*secret*"                        // Block secrets
    ]
  }
}
```

### Common Mistakes

| Wrong | Right | Why |
|-------|-------|-----|
| `Bash(*)` | `Bash` | No parentheses for "allow all" |
| `WebFetch(*)` | `WebFetch(domain:example.com)` | Must use domain: prefix |
| `Bash(ls:*)` | `Bash(ls *)` | Space not colon for patterns |

### Project-Level Settings
**File**: `<project>/.claude/settings.local.json`

Same syntax, but only applies to that project. Global settings take precedence for deny rules.

---

## Todo List Persistence

### Session-Scoped (Default)
- `TodoWrite` tool creates per-session todos
- Stored in `~/.claude/todos/` as JSON files
- **Lost when session ends**

### Persistent Todo
Create `TODO.md` in project root for cross-session task tracking.

---

## Build Optimization Tools

### ccache (C++ Compilation Cache)
```bash
brew install ccache
```

Enable in `ios/Podfile.properties.json`:
```json
{"apple.ccacheEnabled": "true"}
```

**Important**: After enabling, must run fresh `pod install` for config to take effect.

**RN 0.81+ Limitation**: React Native core is prebuilt (`React-Core-prebuilt`), so ccache provides minimal benefit. Most local C++ compilation is eliminated. Keep enabled (zero cost) but expect speedups from Unity Append mode and Xcode DerivedData instead.

### Build Lock (Prevent Concurrent Builds)
```bash
LOCK_FILE="/tmp/build.lock"
if [ -f "$LOCK_FILE" ]; then
    OTHER_PID=$(cat "$LOCK_FILE")
    kill -0 "$OTHER_PID" 2>/dev/null && die "Build running (PID $OTHER_PID)"
fi
echo $$ > "$LOCK_FILE"
trap "rm -f '$LOCK_FILE'" EXIT
```

---

## Useful Settings

```json
{
  "env": {
    "BASH_DEFAULT_TIMEOUT_MS": "30000",
    "CLAUDE_CODE_MAX_OUTPUT_TOKENS": "8192"
  }
}
```

---

## Automation Strategies (From Official Anthropic Best Practices)

### 1. Custom Slash Commands
Store reusable prompts in `.claude/commands/`:

```markdown
# .claude/commands/debug-loop.md
Debug the following error: $ARGUMENTS

1. Analyze the error message
2. Search codebase for relevant files
3. Implement fix
4. Run tests to verify
5. If tests fail, iterate
6. Commit when green
```

Usage: `/project:debug-loop "NullReferenceException in BridgeTarget"`

### 2. Hooks for Automation

**PreToolUse** - Block/modify before execution:
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{
        "type": "command",
        "command": "echo 'Running: $TOOL_INPUT'"
      }]
    }]
  }
}
```

**PostToolUse** - Log/notify after:
```json
{
  "hooks": {
    "PostToolUse": [{
      "matcher": "Write",
      "hooks": [{
        "type": "command",
        "command": "echo 'File written: $TOOL_INPUT' >> /tmp/claude.log"
      }]
    }]
  }
}
```

### 3. Headless Mode for CI/CD

```bash
# Non-interactive automation
claude -p "Run tests and fix failures" --output-format stream-json

# In GitHub Actions
- run: claude -p "Review PR and add comments" --allowedTools "Bash,Read,mcp__github__*"
```

### 4. Block-at-Submit Pattern

Create hook that blocks commits until tests pass:
```bash
# Hook checks for /tmp/tests-passed file
# Only created by successful test run
# Forces "test → fix → test" loop
```

### 5. Thinking Levels for Complex Tasks

| Phrase | Thinking Budget |
|--------|-----------------|
| "think" | Low |
| "think hard" | Medium |
| "think harder" | High |
| "ultrathink" | Maximum |

### 6. TDD Workflow (Anthropic Favorite)

1. Write tests first (tell Claude not to implement yet)
2. Run tests, confirm they fail
3. Commit tests
4. Implement code to pass tests
5. Iterate until green
6. Commit implementation

### 7. Multi-Agent Pattern

Use subagents for complex tasks:
```
"Use subagents to:
1. Investigate the error in parallel
2. Check related files
3. Verify assumptions"
```

---

## Multi-Agent Coordination

**Full Guide**: See `_MULTI_AGENT_COORDINATION.md`

### Quick Reference
- **Before killing processes**: Verify ownership with `ps aux`
- **Before parallel agents**: Confirm no shared resources
- **Build locks**: Check `/tmp/*.lock` before building
- **One at a time**: Git ops, builds, device deploys, Unity MCP writes

### Cross-Tool Shared Resources
| Resource | Path | Conflict Risk |
|----------|------|---------------|
| Xcode builds | `~/Library/Developer/Xcode/DerivedData` | High |
| Node modules | `<project>/node_modules` | Medium |
| CocoaPods | `<project>/ios/Pods` | High |
| Unity cache | `<project>/unity/Library` | High |
| Build locks | `/tmp/*.lock` | Respect always |

---

## Sources

- [Claude Code Best Practices (Anthropic)](https://www.anthropic.com/engineering/claude-code-best-practices)
- [Claude Code Docs - Common Workflows](https://code.claude.com/docs/en/common-workflows)
- [Awesome Claude Code](https://github.com/hesreallyhim/awesome-claude-code)

---

*Last updated: 2026-01-09*
