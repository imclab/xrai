# Intelligence Systems Index

**Purpose**: Central reference for all AI intelligence systems.
**Version**: 1.0 (2026-01-22)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    INTELLIGENCE LAYER                           │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Auto-Fix     │  │ Self-Healing │  │ Continuous   │          │
│  │ Patterns     │  │ Monitoring   │  │ Learning     │          │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │
│         │                 │                  │                  │
│         └────────────┬────┴──────────────────┘                  │
│                      ▼                                          │
│              ┌──────────────┐                                   │
│              │ Knowledge    │                                   │
│              │ Base (131)   │                                   │
│              └──────────────┘                                   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Active Systems

### 1. Auto-Fix Patterns
**File**: `_AUTO_FIX_PATTERNS.md`
**Purpose**: Automatically applicable fixes for common errors

| Trigger | Action |
|---------|--------|
| Compilation error | Lookup fix, apply automatically |
| Runtime exception | Pattern match, suggest fix |
| MCP failure | Run `mcp-kill-dupes` |

**Usage**: AI tools apply these without user confirmation.

### 2. Quick Fix Reference
**File**: `_QUICK_FIX.md`
**Purpose**: Fast error→fix lookup via `kbfix` alias

```bash
kbfix "CS0246"    # Returns: Add using statement
kbfix "NullRef"   # Returns: Add null check
```

### 3. Self-Healing Monitoring
**Embedded in**: `GLOBAL_RULES.md` §Performance Monitoring
**Purpose**: Detect and auto-recover from system issues

| Issue | Detection | Auto-Fix |
|-------|-----------|----------|
| MCP timeout | No response 30s | `mcp-kill-dupes` |
| Memory pressure | >90% | Suggest `purge` |
| Duplicate servers | >2 connections | Kill duplicates |
| Token overflow | >180K | Force `/compact` |

**Thresholds**:
- CPU: OK <70%, Warning 70-90%, Critical >90%
- Memory: OK <80%, Warning 80-95%, Critical >95%
- MCP Response: OK <5s, Warning 5-15s, Critical >30s

### 4. Continuous Learning
**File**: `LEARNING_LOG.md`
**Purpose**: Capture discoveries, patterns, insights

**Triggers**:
| Event | Log To | Action |
|-------|--------|--------|
| Failure (3+ attempts) | LEARNING_LOG | Create prevention |
| Success (first try) | LEARNING_LOG | Document pattern |
| New fix discovered | _AUTO_FIX_PATTERNS | Add to library |

### 5. Pattern Tags Index
**File**: `_PATTERN_TAGS.md`
**Purpose**: Topic→file mapping for `kbtag` alias

```bash
kbtag "vfx"           # Returns: 15 files with VFX patterns
kbtag "hand-tracking" # Returns: 5 files with hand tracking
```

---

## Automation Hooks (6 total)

**Location**: `~/.claude/hooks/`

| Hook | Trigger | Purpose |
|------|---------|---------|
| `auto-detect-agent.sh` | UserPromptSubmit | Auto-suggest agents from intent |
| `magic-keywords.sh` | UserPromptSubmit | Activate modes (persist, ultrawork, etc.) |
| `failure-tracker.sh` | PostToolUse | 3-failure circuit breaker + KB search |
| `session-health-check.sh` | SessionStart | Auto-fix MCP duplicates |
| `todo-enforcer.sh` | Stop | Warn on incomplete tasks |
| `filter-test-output.sh` | PreToolUse:Bash | Filter verbose test output |

### Auto-Detected Modes (No Keywords Needed)

| You Say | Auto-Activates |
|---------|----------------|
| "setup...", "implement..." | PERSIST |
| "debug...", "why...", "analyze..." | DEEP |
| "fix all...", "across project..." | PARALLEL |
| Long prompt (>50 words) | ULTRAWORK |
| "what is...", "status..." | QUICK |
| "still doesn't work" | DEEP + escalate agent |

### Manual Keywords (Optional Override)

| Keyword | Alias | Use When |
|---------|-------|----------|
| `persist` | `ralph` | Force completion mode |
| `ultrawork` | `uw` | Force all capabilities |
| `ultrathink` | `ut` | Force deep reasoning |

**Philosophy**: "Don't make users learn commands—detect intent automatically."
**Reference**: `_OH_MY_CLAUDECODE_PATTERNS.md`

---

## Custom Agents (23 total)

**Location**: `~/.claude/agents/`

### Lightweight (model: haiku)
| Agent | Purpose | Cost |
|-------|---------|------|
| health-monitor | System health checks | 0.3x |
| unity-console-checker | Console status | 0.3x |
| monitor-agent | Quick project health | 0.3x |
| test-runner | Run Unity tests | 0.3x |
| code-searcher | JetBrains indexed search | 0.3x |
| vfx-tuner | VFX property tuning | 0.3x |
| logger-agent | Add structured logging | 0.3x |

### Standard (model: inherit → sonnet)
| Agent | Purpose |
|-------|---------|
| mcp-tools-specialist | Unity MCP operations |
| unity-error-fixer | Fix compilation errors |
| research-agent | KB research |
| insight-extractor | Extract patterns |
| claude-session-logger | Session documentation |
| code-tester | Automated testing |
| orchestrator-agent | Multi-task coordination |

### Advanced (model: inherit → opus for complex)
| Agent | Purpose |
|-------|---------|
| tech-lead | Architecture decisions |
| system-improver | Config/KB improvements |
| osx-helper | macOS system admin |

---

## Shell Aliases

```bash
# Quick lookups (zero tokens)
kb "topic"        # Search all KB files
kbfix "error"     # Error→fix lookup
kbtag "tag"       # Topic→files lookup
kbrepo "topic"    # GitHub repo search

# Maintenance
kb-audit          # Audit KB health
kb-backup         # Backup KB
kb-research       # Quick research update
kb-research-full  # Full research update
```

---

## MCP Integration

### Server Priorities
| Server | Purpose | When |
|--------|---------|------|
| UnityMCP | Editor automation | Always for Unity |
| JetBrains | Code intelligence | When Rider open |
| claude-mem | Semantic memory | Fuzzy recall |
| github | Repo operations | PRs, issues |

### Health Commands
```bash
mcp-kill-dupes    # Kill duplicate processes
mcp-ps            # List running servers
mcp-mem           # Check memory usage
```

---

## Quick Activation

```
"Run health-monitor"              # System health check
"Use insight-extractor"           # Extract patterns
"Use system-improver"             # Apply improvements
"Log failure: [description]"      # Track failure
"Log success: [description]"      # Track success
"Search KB for [topic]"           # Find existing solutions
```

---

## Pattern Categories (106 total)

| Category | Count | Source |
|----------|-------|--------|
| Unity C# Errors | 8 | CS0246, CS0103, CS1061, etc. |
| AR Foundation | 12 | Texture access, occlusion, depth |
| VFX Graph | 18 | Properties, buffers, events |
| Compute/HLSL | 14 | Thread groups, custom HLSL |
| Hand Tracking | 8 | XR Hands, HoloKit, gestures |
| Jobs/Burst | 6 | NativeArray, scheduling |
| Networking | 10 | Netcode, Normcore, WebRTC |
| Mobile/WebGL | 8 | Pooling, memory, asset bundles |
| UI Toolkit | 4 | Queries, binding, pooling |
| Shader Graph | 4 | Custom functions, preview |
| Other (ECS, Addressables, etc.) | 14 | Various platforms |

---

## Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Auto-fix patterns | **106** | 100+ ✅ |
| KB files | 118 | <100 (consolidate) |
| Custom agents | 23 | 25 (tiered variants) |
| Haiku agents | 9 | 12 (more lightweight) |
| Automation hooks | 6 | 6 (optimal) |
| Magic keywords | 7 | 7 (optional) |

---

## Missing (To Create)

| System | Priority | Notes |
|--------|----------|-------|
| Predictive suggestions | P2 | Suggest tools before asked |
| Usage analytics | P3 | Track fix effectiveness |
| Auto-archive | P2 | Move old LEARNING_LOG entries |

---

**Last Updated**: 2026-01-22
**Maintained By**: system-improver agent
