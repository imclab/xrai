# Multi-Agent & Process Coordination Guide

**Applies to**: Claude Code, Cursor, Windsurf, GitHub Copilot, and any AI-assisted development tools.

---

## Core Principles

### 1. Process Sovereignty
Every running process has an owner. Before interacting with any process:
- **Identify owner**: Was it started by current session, another terminal, or system service?
- **Verify intent**: Is it supposed to be running?
- **Ask before killing**: Unless clearly from current session, confirm with user

### 2. Resource Exclusivity
Some resources can only be safely accessed by one agent/tool at a time:

| Resource | Conflict Risk | Resolution |
|----------|---------------|------------|
| Git index (.git/index) | High | Never parallel git operations |
| Build artifacts (DerivedData) | High | Use build locks |
| Device connections (USB) | High | One deploy at a time |
| Unity MCP server | High | One connection per Unity instance |
| File being edited | Medium | Coordinate via locks or turn-taking |
| node_modules | Low | Parallel reads OK, sequential writes |

### 3. Shared State Awareness
Multiple tools share the same filesystem and system state:

```
~/.claude/              # Claude Code config
~/.cursor/              # Cursor config
~/.windsurf/            # Windsurf config
~/Library/Developer/    # Xcode DerivedData (shared!)
/tmp/*.lock             # Build locks (respect across all tools)
```

---

## Agent Parallelism Decision Matrix

### Safe to Parallelize
- Reading different files
- Web searches and research
- Independent API calls
- Searching different directories
- Running read-only MCP queries

### Requires Coordination
- Editing files in same project (check for overlap)
- Running tests (may conflict with builds)
- Database operations (transaction isolation)
- Cache operations (invalidation timing)

### Never Parallelize
- Git operations (add, commit, rebase, merge)
- Build processes (xcodebuild, gradle, Unity)
- Device deployments (iOS/Android)
- Unity MCP write operations
- Pod install / npm install
- Schema migrations

---

## Implementation Patterns

### Build Lock Pattern
```bash
LOCK_FILE="/tmp/build.lock"
if [ -f "$LOCK_FILE" ]; then
    OTHER_PID=$(cat "$LOCK_FILE")
    if kill -0 "$OTHER_PID" 2>/dev/null; then
        echo "Build running (PID $OTHER_PID). Wait or: kill $OTHER_PID"
        exit 1
    fi
fi
echo $$ > "$LOCK_FILE"
trap "rm -f '$LOCK_FILE'" EXIT
```

### Process Check Before Kill
```bash
# ALWAYS verify before killing
ps aux | grep <process> | grep -v grep
# Look for: user, start time, command to identify owner
# If ambiguous: ASK USER
```

### Port Conflict Check
```bash
# Before starting a server
lsof -i :8081  # Metro
lsof -i :6400  # Unity MCP
lsof -i :3000  # Common dev server
```

---

## Cross-Tool Communication

### Signaling via Files
Tools can coordinate via well-known paths:
- `/tmp/unity-building` - Unity export in progress
- `/tmp/xcode-building` - Xcode build in progress
- `/tmp/metro-running` - Metro bundler active

### Checking Other Tools
```bash
# Is another AI tool active?
ps aux | grep -E "cursor|windsurf|copilot" | grep -v grep

# Is Unity Editor open?
pgrep -x Unity

# Is Xcode building?
pgrep xcodebuild
```

---

## Conflict Recovery

### Git Conflicts
```bash
# If parallel git ops caused issues
git status
git stash  # Save work
git reset --hard HEAD  # If needed
git stash pop  # Restore work
```

### Build Conflicts
```bash
# Clear stale locks
rm -f /tmp/*.lock

# Clear DerivedData if corrupted
rm -rf ~/Library/Developer/Xcode/DerivedData/<project>*

# Force clean build
UNITY_CLEAN_BUILD=1 ./scripts/build_minimal.sh
```

### Device Conflicts
```bash
# If device connection is stuck
killall -9 MobileDeviceUpdater devicectl
# Reconnect device physically
```

---

## Best Practices Summary

1. **Check before acting** - `ps aux`, `lsof`, lock files
2. **One build at a time** - Respect all lock mechanisms
3. **One device deploy at a time** - Queue if needed
4. **Ask about unknown processes** - Don't assume ownership
5. **Document coordination** - If you create a lock pattern, document it
6. **Clean up after yourself** - Remove locks, temp files on exit
7. **Fail gracefully** - If locked out, wait or inform user

---

## Tool-Specific Notes

### Claude Code
- Uses subagents for parallel work
- Subagents share filesystem context
- Coordinate via TodoWrite for visibility

### Cursor
- Multi-file edits may overlap with Claude Code
- Shares DerivedData, node_modules
- Check for .cursor-server processes

### Windsurf
- Similar architecture to Cursor
- Shares same resources
- Check for windsurf processes

### Unity MCP
- Single connection per Unity instance
- After builds, server needs restart
- Port 6400 by default

---

*Last updated: 2026-01-09*
