# Self-Healing System

**Purpose**: Automatic detection and recovery from system issues.
**Version**: 1.0 (2026-01-22)

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SELF-HEALING PIPELINE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐     ┌──────────────┐     ┌──────────────┐     │
│  │ Monitor      │ → → │ Detect       │ → → │ Auto-Fix     │     │
│  │ Health       │     │ Issue        │     │ or Alert     │     │
│  └──────────────┘     └──────────────┘     └──────────────┘     │
│                                                                  │
│  Inputs:                                                         │
│  - MCP server status                                            │
│  - Tool response times                                          │
│  - Error patterns                                               │
│  - Memory/CPU usage                                             │
│  - Token consumption                                            │
└─────────────────────────────────────────────────────────────────┘
```

---

## Health Thresholds

### System Resources

| Resource | OK | Warning | Critical | Auto-Fix |
|----------|:--:|:-------:|:--------:|----------|
| CPU | <70% | 70-90% | >90% | Suggest wait |
| Memory | <80% | 80-95% | >95% | `purge` or `/compact` |
| Disk | <80% | 80-95% | >95% | KB cleanup |

### MCP Servers

| Metric | OK | Warning | Critical | Auto-Fix |
|--------|:--:|:-------:|:--------:|----------|
| Response Time | <5s | 5-15s | >30s | `mcp-kill-dupes` |
| Connections | 1 | 2 | >2 | Kill duplicates |
| Errors/min | 0 | 1-5 | >5 | Restart server |

### Token Usage

| Metric | OK | Warning | Critical | Auto-Fix |
|--------|:--:|:-------:|:--------:|----------|
| Session | <100K | 100-150K | >150K | `/compact` |
| Weekly | <95% | 95-98% | >98% | Switch to Haiku |

---

## Auto-Fix Actions

### 1. MCP Duplicate Processes

**Detection**: Multiple Unity/JetBrains MCP processes
**Auto-Fix**:
```bash
mcp-kill-dupes
# Implemented in ~/.local/bin/mcp-kill-dupes
```

**Hook**: `session-health-check.sh` (SessionStart)

### 2. MCP Timeout

**Detection**: No response after 30s
**Auto-Fix**:
```bash
# Kill and restart affected server
pkill -f "unity-mcp"
# Server auto-restarts on next MCP call
```

### 3. Token Overflow

**Detection**: Session >150K tokens
**Auto-Fix**:
```
/compact <current focus>
# Preserves context, reduces tokens
```

### 4. Repeated Tool Failures

**Detection**: Same error 3+ times
**Auto-Fix**:
1. Search `_AUTO_FIX_PATTERNS.md` for fix
2. If found → Apply fix
3. If not found → Escalate to deep analysis
4. Log to `FAILURE_LOG.md`

**Hook**: `failure-tracker.sh` (PostToolUse)

### 5. Unity Compilation Errors

**Detection**: CS* error codes in console
**Auto-Fix**:
1. Lookup in `_AUTO_FIX_PATTERNS.md`
2. Apply automatic fixes (add using, null checks)
3. If complex → Use `unity-error-fixer` agent

---

## Circuit Breaker Pattern

```
Failure Count:
  1st: Silent (track)
  2nd: Search KB for fix
  3rd: STOP + Escalate

State Transitions:
  CLOSED (normal) → OPEN (blocked) → HALF-OPEN (testing)

Reset Conditions:
  - Success after fix
  - Manual reset
  - 5 minute timeout
```

**Implementation**: `~/.claude/hooks/failure-tracker.sh`

---

## Health Check Commands

### Quick Checks (Zero Tokens)

```bash
mcp-ps            # List MCP processes
mcp-mem           # Memory usage
mcp-kill-dupes    # Kill duplicates
```

### In-Session Checks

```
/cost             # Token usage (API)
/stats            # Usage (Max/Pro)
/context          # MCP server overhead
```

### Agent-Based Checks

```
# Use health-monitor agent
"Run health-monitor"

# Checks:
# - Unity console errors
# - MCP server status
# - Compilation state
```

---

## Error Recovery Patterns

### Pattern 1: VFX Property Not Updating

**Detection**: VFX property set but unchanged
**Self-Heal**: Use `ExposedProperty` instead of string
```csharp
// Auto-fix pattern
static readonly ExposedProperty k_Prop = "PropertyName";
vfx.SetFloat(k_Prop, value);
```

### Pattern 2: AR Texture Crash

**Detection**: NullReferenceException + AR texture
**Self-Heal**: Use TryGetTexture pattern
```csharp
// Auto-fix pattern
Texture TryGetTexture(Func<Texture> getter) {
    try { return getter?.Invoke(); }
    catch { return null; }
}
```

### Pattern 3: Compute Thread Mismatch

**Detection**: Visual artifacts or crash in compute
**Self-Heal**: Query thread group size
```csharp
// Auto-fix pattern
shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out _);
int groupsX = Mathf.CeilToInt(width / (float)x);
```

### Pattern 4: Memory Leak

**Detection**: Memory grows over time
**Self-Heal**: Add disposal in OnDestroy
```csharp
// Auto-fix pattern
void OnDestroy() {
    renderTexture?.Release();
    graphicsBuffer?.Dispose();
    if (nativeArray.IsCreated) nativeArray.Dispose();
}
```

---

## Escalation Matrix

| Issue | First Response | Escalation | Final |
|-------|---------------|------------|-------|
| Compilation Error | Auto-fix lookup | unity-error-fixer | tech-lead |
| MCP Timeout | Kill duplicates | Restart server | Manual check |
| Persistent Bug | KB search | Deep analysis | Research agent |
| Performance | LOD/pooling | Profiler | Architecture review |

---

## Monitoring Hooks

| Hook | Trigger | Checks |
|------|---------|--------|
| `session-health-check.sh` | SessionStart | MCP duplicates, Unity state |
| `failure-tracker.sh` | PostToolUse | Error patterns, failure count |
| `auto-detect-agent.sh` | UserPromptSubmit | Persistent issues → escalate |

---

## Self-Improvement Triggers

| Trigger | Action |
|---------|--------|
| New error pattern found | Add to `_AUTO_FIX_PATTERNS.md` |
| Fix effectiveness <50% | Review and update pattern |
| 3+ users hit same issue | Promote to auto-apply |
| API deprecated | Update patterns for new API |

---

## Recovery Logs

### Success Log

**File**: `SUCCESS_LOG.md` (if needed)
**Format**:
```markdown
## [Date] - [Issue] - RESOLVED
- **Detection**: How issue was found
- **Fix**: What resolved it
- **Prevention**: Pattern added to KB
```

### Failure Log

**File**: `FAILURE_LOG.md`
**Format**:
```markdown
## [Date] - [Issue] - UNRESOLVED
- **Attempts**: What was tried
- **Blocked By**: Why it failed
- **Next Steps**: Recommended action
```

---

## Related Files

- `_AUTO_FIX_PATTERNS.md` - 106 auto-fix patterns
- `_CONTINUOUS_LEARNING_SYSTEM.md` - Learning from errors
- `_INTELLIGENCE_SYSTEMS_INDEX.md` - Central reference
- `LEARNING_LOG.md` - Discovery journal
- `~/.claude/hooks/failure-tracker.sh` - Circuit breaker
- `~/.claude/hooks/session-health-check.sh` - Startup checks
- `~/GLOBAL_RULES.md` §Performance Monitoring

---

**Last Updated**: 2026-01-22
**Maintained By**: system-improver agent
