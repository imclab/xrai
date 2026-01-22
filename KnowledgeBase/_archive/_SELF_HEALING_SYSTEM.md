# Self-Healing & Performance Monitoring System

**Version**: 1.0 (2026-01-21)
**Purpose**: Proactive monitoring, bottleneck detection, and automatic optimization.
**Core Principle**: Never slow down Mac, Rider, Unity, Claude Code, or other tools.

---

## Research-Backed Best Practices

Based on [Self-Evolving Agents research](https://arxiv.org/abs/2508.07407) and [Agentic AI Maturity Models](https://dextralabs.com/blog/agentic-ai-maturity-model-2025/):

- **Continuous Learning**: Agents must automatically enhance based on interaction data
- **Human-in-the-Loop**: Learn from approvals and modifications
- **Multi-Agent Orchestration**: Specialized agents coordinated by orchestrator
- **Atomic Operations**: Implement undo stacks and checkpointing
- **Governance**: Audit trails across all agent actions

---

## 1. Performance Monitoring

### System Health Thresholds

| Resource | Warning | Critical | Action |
|----------|---------|----------|--------|
| **CPU** | >70% sustained | >90% | Kill background processes |
| **Memory** | >80% | >95% | Close unused apps, purge |
| **Disk I/O** | High wait | Blocking | Defer large ops |
| **Network** | >5s latency | Timeout | Retry with backoff |
| **MCP Response** | >10s | >30s | Restart server |
| **Unity Editor** | <30 FPS | <15 FPS | Reduce scene complexity |
| **Token Usage** | >80K session | >150K | Trigger compact |

### Monitoring Commands

```bash
# Quick system health check
top -l 1 | head -15              # CPU/Memory snapshot
vm_stat | head -10               # Memory pressure
lsof -i :6400 | wc -l            # Unity MCP connections
lsof -i :63342 | wc -l           # JetBrains MCP connections

# Process-specific
ps aux | grep -E "(Unity|Rider|claude)" | grep -v grep
```

### Automated Health Check Agent

**File**: `~/.claude/agents/health-monitor.md`

```yaml
---
name: health-monitor
description: Proactive system health monitoring. Run periodically.
tools: Bash
model: haiku
---

**Follow**: `_AGENT_SHARED_RULES.md`

Check system health and flag issues before bottlenecks.

## Quick Check (run frequently)
1. `top -l 1 | head -3` - CPU/Memory
2. `lsof -i :6400,63342 | wc -l` - MCP connections
3. Check for duplicate processes

## Flag Format
- ✅ Healthy: [resource]
- ⚠️ Warning: [resource] at [value]
- 🔴 Critical: [resource] at [value] → [action]

## Auto-Fix Actions
- High memory → `purge`
- Duplicate MCP → `mcp-kill-dupes`
- Unity frozen → Suggest restart
```

---

## 2. Proactive Bottleneck Detection

### Early Warning Triggers

| Indicator | Detection | Prevention |
|-----------|-----------|------------|
| Token usage climbing | >50K mid-session | Use subagents, compact |
| MCP latency increasing | Response >5s | Check server health |
| Unity compile time | >30s | Check for loops/errors |
| Rider indexing | CPU spike | Wait before operations |
| File system pressure | Many temp files | Clean temp directories |

### Detection Patterns

```bash
# Token pressure (check context size)
# Manual: /cost command in Claude Code

# MCP health
curl -s http://localhost:6400/health 2>/dev/null || echo "Unity MCP not responding"

# Unity process health
ps aux | grep Unity | grep -v grep | awk '{print $3, $4}'  # CPU%, MEM%

# Disk pressure
df -h | grep -E "/$|/Users" | awk '{print $5}'  # Usage %
```

### Bottleneck Prevention Rules

1. **Before large operations**: Check available resources
2. **During long tasks**: Monitor progress, allow interrupts
3. **After completion**: Clean up temp files, release resources
4. **Periodically**: Health check, defrag if needed

---

## 3. Self-Healing Patterns

### Automatic Recovery

| Issue | Detection | Auto-Recovery |
|-------|-----------|---------------|
| MCP timeout | No response 30s | `mcp-kill-dupes && restart` |
| Unity Editor frozen | No heartbeat | Log state, suggest restart |
| Memory pressure | >90% | `purge`, close background apps |
| Token overflow | >180K | Trigger /compact, use subagents |
| Broken symlink | File read fails | Recreate symlink |
| Stale cache | Outdated results | Clear and refresh |

### Recovery Scripts

```bash
# MCP recovery
mcp-kill-dupes  # Custom script in GLOBAL_RULES

# Memory recovery
purge  # macOS memory cleanup

# Unity recovery
# Via Unity MCP: manage_editor(action="refresh")

# Temp cleanup
find /tmp -name "*.tmp" -mtime +1 -delete 2>/dev/null
```

### State Preservation

Before any recovery action:
1. Log current state to session log
2. Save any pending work
3. Checkpoint progress in TodoWrite
4. Execute recovery
5. Verify restoration
6. Resume from checkpoint

---

## 4. Integration with KB Systems

### Connected Systems

| System | Integration Point | Data Flow |
|--------|-------------------|-----------|
| `_CONTINUOUS_LEARNING_SYSTEM.md` | Pattern extraction | Failures → Prevention |
| `_AUTO_FIX_PATTERNS.md` | Auto-remediation | Detection → Fix |
| `FAILURE_LOG.md` | Issue tracking | Incident → Root cause |
| `ANTI_PATTERNS.md` | Prevention rules | Issue → Don't repeat |
| `_TOKEN_EFFICIENCY_COMPLETE.md` | Token health | Usage → Optimization |
| `_TOOL_INTEGRATION_MAP.md` | Tool health | MCP status |

### Cross-System Flow

```
Performance Issue Detected
    ↓
Log to FAILURE_LOG.md
    ↓
Check _AUTO_FIX_PATTERNS.md for fix
    ↓
If fix exists → Apply automatically
    ↓
If new issue → Add to _AUTO_FIX_PATTERNS.md
    ↓
Update ANTI_PATTERNS.md (prevention)
    ↓
Log success to SUCCESS_LOG.md
    ↓
Update _CONTINUOUS_LEARNING_SYSTEM.md metrics
```

---

## 5. Spec-Kit Methodology for Improvements

Apply spec structure to performance improvements:

### Performance Improvement Spec Template

```markdown
# Spec: [Improvement Name]

## Context
- Current performance: [metric]
- Target performance: [metric]
- Impact: [affected tools/workflows]

## Analysis
- Root cause: [why slow]
- Bottleneck location: [specific component]
- Dependencies: [what else affected]

## Solution
### Option A: [approach]
- Pros: [benefits]
- Cons: [tradeoffs]
- Effort: [low/medium/high]

### Option B: [approach]
- Pros: [benefits]
- Cons: [tradeoffs]
- Effort: [low/medium/high]

## Implementation
- [ ] Step 1: [action]
- [ ] Step 2: [action]
- [ ] Step 3: [action]

## Validation
- Test: [how to verify]
- Metrics: [what to measure]
- Rollback: [how to undo]

## Documentation
- Update: [which files]
- Pattern: [reusable pattern created]
```

---

## 6. Performance Optimization Triggers

### Automatic Optimizations

| Trigger | Optimization | Applied To |
|---------|--------------|------------|
| Session start | Run mcp-kill-dupes | All sessions |
| Token >80K | Suggest compact | Claude Code |
| MCP slow | Restart server | MCP servers |
| Unity slow | Check console | Unity Editor |
| Rider indexing | Wait before search | JetBrains |

### Manual Optimization Commands

```bash
# Full system optimization
purge                                    # Clear memory
mcp-kill-dupes                          # Clean MCP servers
find ~/Library/Caches -type f -mtime +7 -delete  # Old caches

# Unity-specific
# Close unused scenes, reduce quality in Edit mode

# Rider-specific
# Invalidate caches: File > Invalidate Caches
```

---

## 7. Continuous Performance Metrics

### Session Metrics

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| Average MCP response | <2s | >5s | >15s |
| Token per task | <5K | >10K | >20K |
| First-attempt success | >80% | <60% | <40% |
| Recovery actions needed | 0 | >2 | >5 |

### Weekly Rollup

```markdown
## Week of [Date] - Performance Report

### Resource Health
- Peak CPU: [%] (acceptable: <80%)
- Peak Memory: [%] (acceptable: <85%)
- MCP Restarts: [count] (target: 0)

### Efficiency
- Avg tokens/task: [count]
- First-attempt success: [%]
- Auto-fixes applied: [count]

### Issues Resolved
- [Issue 1]: [fix applied]
- [Issue 2]: [fix applied]

### Improvements Made
- [Optimization 1]
- [Optimization 2]
```

---

## 8. Never Slow Down Guarantee

### Core Principles

1. **Lightweight Monitoring**: All checks use minimal resources
2. **Lazy Evaluation**: Don't compute until needed
3. **Graceful Degradation**: If resources tight, reduce non-essential ops
4. **Immediate Recovery**: Detect and fix in <30s
5. **No Blocking**: Long operations run in background

### Implementation Rules

- Health checks: <100ms each
- MCP calls: Timeout at 30s, retry once
- File operations: Batch when possible
- Token budget: Reserve 20K for recovery
- Background tasks: Low priority, interruptible

---

## Activation Commands

```
# Quick health check
"Run health-monitor for system status"

# Performance audit
"Check performance metrics and flag issues"

# Recovery
"Apply self-healing to fix [issue]"

# Optimization
"Optimize [tool] performance"
```

---

**Sources**:
- [Self-Evolving Agents Survey](https://arxiv.org/abs/2508.07407)
- [Agentic AI Maturity Model](https://dextralabs.com/blog/agentic-ai-maturity-model-2025/)
- [OpenAI Self-Evolving Agents Cookbook](https://cookbook.openai.com/examples/partners/self_evolving_agents/autonomous_agent_retraining)
- [Agentic AI Trends 2026](https://machinelearningmastery.com/7-agentic-ai-trends-to-watch-in-2026/)

---

**Last Updated**: 2026-01-21
