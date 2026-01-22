# Continuous Learning System

**Version**: 1.0 (2026-01-21)
**Purpose**: Apply auto-insight extraction across all development domains.
**Core Pattern**: Extract → Log → Compound → Accelerate

---

## The Pattern

Every task follows:
```
Pre:    Search existing knowledge
During: Execute with awareness
Post:   Extract patterns → Log → Improve system
```

This applies to: Agents, Unity, Claude Code, Dev, Debug, Test

---

## 1. Agent Intelligence Growth

### Auto-Extract After Agent Runs

When any agent completes, extract:
- What worked well?
- What was slow/inefficient?
- What pattern emerged?

### Agent Pattern Log

**Location**: `KnowledgeBase/AGENT_PATTERNS.md`

```markdown
## [Date] - [Agent Name] - [Pattern]

**Task**: What the agent did
**Insight**: What we learned
**Improvement**: How to make it better
**Applied**: ✅/⬜ Updated agent definition
```

### Agent Improvement Triggers

| Trigger | Action |
|---------|--------|
| Agent takes >3 calls for simple task | Optimize workflow |
| Same fix applied 3+ times | Add to agent prompt |
| New capability discovered | Add to agent tools |
| Agent fails repeatedly | Add fallback logic |

---

## 2. Unity Capability Discovery

### Auto-Extract After Unity Work

When implementing Unity features:
- New VFX property discovered?
- Performance optimization found?
- AR/XR pattern that works?

### Unity Pattern Log

**Location**: `KnowledgeBase/UNITY_PATTERNS.md`

```markdown
## [Date] - [Category] - [Pattern]

**Context**: What we were building
**Discovery**: What works
**Code Snippet**: (if applicable)
**Performance**: FPS/memory impact
**Platform**: iOS/Android/Quest/WebGL
```

### Unity Learning Triggers

| Trigger | Action |
|---------|--------|
| VFX property works well | Add to `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` |
| Performance issue solved | Add to `PLATFORM_COMPATIBILITY_MATRIX.md` |
| AR crash fixed | Add to bug fixes section |
| New asset pattern | Add to project docs |

---

## 3. Claude Code Optimization

### Auto-Extract After Sessions

End of session, note:
- What tools were most efficient?
- What wasted tokens?
- What workflow was slow?

### Claude Code Pattern Log

**Location**: `KnowledgeBase/CLAUDE_CODE_PATTERNS.md`

```markdown
## [Date] - [Optimization]

**Problem**: What was inefficient
**Solution**: What worked better
**Token Impact**: Estimated savings
**Applied**: ✅/⬜ Updated to rules
```

### Claude Code Learning Triggers

| Trigger | Action |
|---------|--------|
| Found faster tool combo | Add to GLOBAL_RULES workflows |
| Token waste identified | Add to anti-patterns |
| New MCP pattern | Add to tool optimization section |
| Effective prompt pattern | Add to agent prompts |

---

## 4. Dev Speed Patterns

### Auto-Extract During Development

While coding, notice:
- What accelerated progress?
- What caused delays?
- What reusable pattern emerged?

### Dev Speed Log

**Location**: `LEARNING_LOG.md` (existing)

```markdown
## [Date] - Dev Speed - [Pattern]

**Leverage**: Existing/Adapt/AI/Scratch
**Time Saved**: Estimated hours
**Reusable**: Yes/No
**Applied To**: List of projects
```

### Dev Speed Triggers

| Trigger | Action |
|---------|--------|
| Reused code from KB | Note success pattern |
| Built from scratch (should've reused) | Add to KB for next time |
| Found existing solution | Document where |
| Created reusable component | Add to patterns |

---

## 5. Debug Intelligence

### Auto-Extract After Debugging

After fixing bugs, capture:
- Root cause pattern
- Fix approach
- Prevention strategy

### Debug Pattern Log

**Location**: `KnowledgeBase/DEBUG_PATTERNS.md`

```markdown
## [Date] - [Error Type] - [Root Cause]

**Symptom**: What appeared wrong
**Root Cause**: Actual problem
**Fix**: Solution applied
**Prevention**: How to avoid
**Platform**: If platform-specific
```

### Debug Learning Triggers

| Trigger | Action |
|---------|--------|
| CS#### error fixed | Add to common fixes |
| AR crash resolved | Add to AR safety patterns |
| Performance issue solved | Add to optimization guide |
| Null reference fixed | Add to null safety patterns |

### Common Debug Patterns (Quick Reference)

| Error | Root Cause | Fix |
|-------|------------|-----|
| CS0246 | Missing using | Add `using Namespace;` |
| NullRef in AR | Texture not ready | TryGetTexture pattern |
| VFX not updating | Wrong property type | Use ExposedProperty |
| iOS crash on AR | Thread dispatch | Use main thread |

---

## 6. Test Intelligence

### Auto-Extract After Testing

After test runs:
- What test strategy worked?
- What caught bugs early?
- What was overkill?

### Test Pattern Log

**Location**: `KnowledgeBase/TEST_PATTERNS.md`

```markdown
## [Date] - [Test Type] - [Pattern]

**Approach**: How we tested
**Coverage**: What it caught
**Efficiency**: Time spent vs value
**Reusable**: Test template created?
```

### Test Learning Triggers

| Trigger | Action |
|---------|--------|
| Test caught regression | Document test pattern |
| Test was flaky | Document fix |
| Missing test coverage | Add test template |
| Test too slow | Document optimization |

---

## Implementation: Auto-Insight Agent

Create an agent that runs after significant work:

**File**: `~/.claude/agents/insight-extractor.md`

```yaml
---
name: insight-extractor
description: Extract and log patterns after completing tasks. Run automatically after feature implementations, bug fixes, or optimization work.
tools: Read, Write, Grep
model: haiku
---

You extract and log insights from completed work.

## Workflow

1. Analyze what was just done
2. Identify reusable patterns
3. Append to appropriate log file
4. Suggest improvements

## Output

Append to relevant log:
- LEARNING_LOG.md (general)
- AGENT_PATTERNS.md (agent insights)
- UNITY_PATTERNS.md (Unity/VFX)
- DEBUG_PATTERNS.md (bug fixes)
- TEST_PATTERNS.md (testing)
- CLAUDE_CODE_PATTERNS.md (tool optimizations)

## Format

Use standard format with date, context, insight, application.
```

---

## Session Hooks for Auto-Extraction

### Stop Hook Enhancement

**File**: `~/.claude-mem/hooks/stop.js`

Add insight extraction prompt:
```javascript
// After session, prompt for insights
console.log("Session ending. Insights to log?");
console.log("- LEARNING_LOG.md: General patterns");
console.log("- Use /save to extract and store");
```

---

## Compound Learning Metrics

### Per Session
- Insights extracted: ≥2
- Patterns reused: ≥1
- New patterns added: ≥1

### Per Week
- LEARNING_LOG entries: ≥5
- Patterns applied: ≥3
- Agent improvements: ≥1

### Per Month
- KB files updated: ≥10
- Agents optimized: ≥2
- Workflow speedups: ≥3

---

## Quick Commands

```
# Extract insights manually
"Use insight-extractor to log what we learned"

# Check learning velocity
"How many LEARNING_LOG entries this week?"

# Apply accumulated knowledge
"Search KB for similar problems before implementing"

# Update agents with patterns
"Update [agent] with the pattern we just discovered"
```

---

## Integration Points

| System | Learning Input | Learning Output |
|--------|----------------|-----------------|
| Agents | Task results | Agent prompt improvements |
| Unity | Code changes | VFX/AR patterns |
| Claude Code | Tool usage | Workflow optimizations |
| Debug | Bug fixes | Prevention patterns |
| Test | Test results | Test templates |

---

---

## 7. Auto-Improvement System

The system doesn't just log - it **actively improves** configurations.

### Code Pattern Auto-Addition

**Trigger**: Reusable code pattern discovered
**Action**: Append to `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`

```markdown
## [Category] - [Pattern Name]

**Context**: When to use
**Code**:
```csharp
// Pattern implementation
```
**Performance**: [metrics]
**Platform**: [compatibility]
```

### Tool Use Auto-Improvements

**Trigger**: Found faster tool combination
**Action**: Update `~/GLOBAL_RULES.md` workflows section

| Discovery | Update Location |
|-----------|-----------------|
| Faster JetBrains params | Tool Selection table |
| Better Unity MCP pattern | Unity MCP Optimization |
| New batch operation | Fast Workflows section |
| Token-saving technique | Token Efficiency section |

### Agent Auto-Improvements

**Trigger**: Agent pattern works well / fails repeatedly
**Action**: Update agent definition in `~/.claude/agents/`

| Discovery | Update |
|-----------|--------|
| Better prompt pattern | Add to agent instructions |
| Common task handled | Add to agent workflow |
| Tool combo works | Add to agent tools list |
| Failure pattern | Add to agent anti-patterns |

### Global Rules Auto-Updates

**Trigger**: Rule improvement discovered
**Action**: Update `~/GLOBAL_RULES.md`

| Section | Auto-Update Triggers |
|---------|---------------------|
| Tool Selection | New faster tool found |
| Fast Workflows | Workflow < 3 calls works |
| Anti-Patterns | Pattern causes problems |
| Session Triggers | Better threshold found |
| Common Fixes | Fix applied 3+ times |

### Token Optimization Auto-Updates

**Trigger**: Token savings discovered
**Action**: Update `_TOKEN_EFFICIENCY_COMPLETE.md`

| Discovery | Update |
|-----------|--------|
| Tool saves tokens | Add to tool comparison |
| Model routing works | Add to model selection |
| Batch pattern saves | Add to batch patterns |
| Context management | Add to session section |

---

## 8. Rider + Claude Code + Unity Auto-Improvements

### Dev Speed Improvements

**Trigger**: Workflow is faster than documented
**Action**: Update GLOBAL_RULES workflow section

```
Current: 4 calls
Discovered: 3 calls
→ Update Fast Workflows
```

### Debug Auto-Fixes

**Trigger**: Same error fixed 3+ times
**Action**: Add to auto-fix patterns

**Location**: `KnowledgeBase/_AUTO_FIX_PATTERNS.md`

```markdown
## [Error Code] - [Description]

**Detection**: How to identify
**Fix**: Automated fix steps
**Verification**: How to confirm fixed
```

### Test Speed Improvements

**Trigger**: Test approach is faster
**Action**: Update test-runner agent

| Improvement | Update |
|-------------|--------|
| Better async pattern | Agent workflow |
| Filter reduces noise | Agent output format |
| Parallel test strategy | Agent instructions |

---

## 9. Quality Auto-Improvements

### Modularity Patterns

**Trigger**: Code is more modular than pattern
**Action**: Update code patterns with modularity notes

```markdown
**Modularity**: Single responsibility, interface-based, DI-ready
```

### Scalability Patterns

**Trigger**: Pattern scales better
**Action**: Add to patterns with complexity notes

```markdown
**Scalability**: O(1) lookup, pooling, pagination
```

### Extensibility Patterns

**Trigger**: Pattern allows easy extension
**Action**: Add to patterns with extension notes

```markdown
**Extensibility**: Strategy pattern, plugin architecture, event-driven
```

---

## 10. Best Practices Auto-Integration

### Unity Best Practices

**Source**: Official Unity docs, proven repos
**Action**: Add to `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`

| Category | Examples |
|----------|----------|
| VFX Graph | Property naming, buffer sizing |
| AR Foundation | Lifecycle, threading, null safety |
| Performance | Pooling, batching, LOD |

### Claude Code Best Practices

**Source**: Official docs, discovered patterns
**Action**: Add to `_TOKEN_EFFICIENCY_COMPLETE.md`

| Category | Examples |
|----------|----------|
| Tool usage | Parallel calls, batch ops |
| Context management | Compact triggers, clear points |
| Agent usage | Model routing, resume patterns |

---

## 11. Auto-Fix Library

### Location: `KnowledgeBase/_AUTO_FIX_PATTERNS.md`

```markdown
# Auto-Fix Patterns

## CS0246 - Type or namespace not found

**Detection**: `error CS0246.*type or namespace.*could not be found`
**Fix**:
1. Check if using statement needed
2. Add: `using [Namespace];`
3. If package missing: Add to manifest.json
**Auto-Apply**: Yes (for common namespaces)

## NullReferenceException in AR

**Detection**: `NullReferenceException` + AR-related stack
**Fix**:
1. Find texture/buffer access
2. Replace with TryGet pattern
3. Add null guard
**Auto-Apply**: Yes (template available)

## VFX Property Not Updating

**Detection**: VFX property set but visual unchanged
**Fix**:
1. Check property name matches VFX Graph
2. Use `ExposedProperty` not string
3. Verify property type matches
**Auto-Apply**: Partial (name matching)
```

---

## 12. Self-Improvement Triggers

### Automatic Updates (No Confirmation Needed)

| Trigger | Action |
|---------|--------|
| Same fix 3+ times | Add to _AUTO_FIX_PATTERNS.md |
| Pattern reused 2+ projects | Add to KB |
| Tool combo faster | Update GLOBAL_RULES workflow |
| Agent improvement obvious | Update agent prompt |

### Confirmation Required

| Trigger | Action |
|---------|--------|
| New anti-pattern | Propose GLOBAL_RULES update |
| Architecture change | Propose pattern update |
| Breaking change | Flag for review |
| Significant rule change | Ask before applying |

---

## 13. Improvement Agent

### File: `~/.claude/agents/system-improver.md`

```yaml
---
name: system-improver
description: Automatically improves system configurations based on discoveries. Run after significant work.
tools: Read, Write, Edit, Grep
model: sonnet
---

**Follow**: `_AGENT_SHARED_RULES.md`

You improve system configurations based on discovered patterns.

## Improvement Targets

1. `~/GLOBAL_RULES.md` - Workflows, tool selection, anti-patterns
2. `~/.claude/agents/*.md` - Agent prompts and workflows
3. `KnowledgeBase/_*.md` - Code patterns, auto-fixes
4. `_TOKEN_EFFICIENCY_COMPLETE.md` - Token optimizations

## Workflow

1. Analyze recent work/discoveries
2. Identify improvement type
3. Locate target file
4. Apply minimal, focused update
5. Log change to LEARNING_LOG.md

## Rules

- Small, focused updates only
- Don't break existing functionality
- Preserve file structure
- Add, don't replace (unless fixing)
- Test-related: require verification
```

---

## 14. Metrics for Self-Improvement

### Weekly Tracking

| Metric | Target |
|--------|--------|
| Auto-fixes added | ≥2 |
| Patterns added to KB | ≥3 |
| Agent improvements | ≥1 |
| GLOBAL_RULES updates | ≥1 |
| Token optimizations | ≥1 |

### Quality Metrics

| Metric | Target |
|--------|--------|
| Reuse rate | >50% patterns reused |
| Fix success | >90% auto-fixes work |
| Speed improvement | 10% faster per month |
| Token reduction | 5% less per month |

---

---

## 15. Learning from Failures & Successes

### Failure Learning (Critical for Growth)

**Why Failures Matter More**: Failures reveal gaps in knowledge, patterns, and tools. Each failure is a high-value learning opportunity.

#### Failure Capture Template

**Location**: `KnowledgeBase/FAILURE_LOG.md`

```markdown
## [Date] - [Category] - [Failure Type]

**What Happened**: Brief description
**Expected**: What should have occurred
**Actual**: What actually occurred
**Root Cause**: Why it failed (be specific)
**Time Lost**: Estimated debugging time
**Prevention**: How to avoid in future
**Auto-Fix Created**: ✅/⬜ Added to _AUTO_FIX_PATTERNS.md
**Pattern Updated**: ✅/⬜ Which file updated
```

#### Failure Categories

| Category | Examples | Learning Target |
|----------|----------|-----------------|
| **Tool Failure** | Wrong tool, inefficient tool chain | GLOBAL_RULES workflows |
| **Agent Failure** | Agent gave wrong answer, took too long | Agent prompt update |
| **Unity Failure** | Compile error, runtime crash, AR issue | _AUTO_FIX_PATTERNS.md |
| **Architecture** | Wrong pattern, poor structure | KB code patterns |
| **Integration** | MCP error, tool conflict | _TOOL_INTEGRATION_MAP.md |
| **Token Waste** | Unnecessary reads, large outputs | Token efficiency rules |

#### Failure Triggers (Auto-Log)

| Trigger | Action |
|---------|--------|
| >3 attempts to fix same issue | Log to FAILURE_LOG.md |
| Agent retry required | Log agent failure |
| Compile error >2 fixes | Log to Unity failures |
| Context switch mid-task | Log interruption pattern |
| Had to restart approach | Log dead-end |

### Success Learning (Amplify What Works)

**Why Successes Matter**: Successes reveal optimal patterns. Replicate and systematize what works.

#### Success Capture Template

**Location**: `KnowledgeBase/SUCCESS_LOG.md`

```markdown
## [Date] - [Category] - [Success Type]

**What Worked**: Brief description
**Why It Worked**: Key factors (be specific)
**Time Saved**: vs naive approach
**Replicable**: Yes/No + conditions
**Pattern Created**: ✅/⬜ Added to appropriate KB file
**Shared To**: List of targets (agents, rules, etc.)
```

#### Success Categories

| Category | Examples | Replication Target |
|----------|----------|-------------------|
| **Fast Fix** | <5 min for complex issue | _AUTO_FIX_PATTERNS.md |
| **Tool Combo** | Efficient tool chain | GLOBAL_RULES workflows |
| **Agent Win** | Agent completed perfectly | Agent docs/examples |
| **Code Pattern** | Clean, reusable solution | _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md |
| **Debug Speed** | Found root cause quickly | DEBUG_PATTERNS.md |
| **Integration** | Smooth cross-tool flow | _TOOL_INTEGRATION_MAP.md |

#### Success Triggers (Auto-Log)

| Trigger | Action |
|---------|--------|
| Task completed in 1 attempt | Log success pattern |
| Agent returned perfect answer | Log agent success |
| Reused KB pattern successfully | Log reuse success |
| 0 compile errors on first try | Log clean code pattern |
| Found solution in KB | Log KB value confirmation |

### Failure → Success Pipeline

```
Failure Detected
    ↓
Log to FAILURE_LOG.md
    ↓
Analyze Root Cause
    ↓
Create Prevention Pattern
    ↓
Add to _AUTO_FIX_PATTERNS.md (if applicable)
    ↓
Update relevant KB/agent/rules
    ↓
Next occurrence → Success (pattern applied)
    ↓
Log to SUCCESS_LOG.md
    ↓
Compound learning achieved
```

### Metrics: Failure-to-Success Ratio

| Metric | Target | Meaning |
|--------|--------|---------|
| Repeat failure rate | <10% | Same failure shouldn't recur |
| Pattern reuse rate | >70% | KB patterns being applied |
| First-attempt success | >80% | Clean implementations |
| Auto-fix success | >90% | Auto-fixes work correctly |

### Anti-Failure Patterns (Learn What NOT to Do)

**Location**: Add to `KnowledgeBase/ANTI_PATTERNS.md`

```markdown
## [Date] - [Anti-Pattern Name]

**Description**: What to avoid
**Why It Fails**: Root cause
**Better Approach**: What to do instead
**Frequency**: How often this trap appears
```

Common anti-patterns to track:
- Reading files before searching (wastes tokens)
- Using Grep for file search (use Glob)
- Ignoring existing KB solutions (reinventing)
- Not checking MCP server status before Unity ops
- Hardcoding paths that should be relative
- Not using ExposedProperty for VFX (runtime breaks)

---

## 16. Compound Learning Velocity

### The Goal: Accelerating Returns

Each session should be faster than the last due to:
1. More patterns in KB → faster solutions
2. Better auto-fixes → fewer debugging cycles
3. Optimized agents → better first-attempt results
4. Documented failures → avoided repeated mistakes

### Weekly Velocity Check

```markdown
## Week of [Date]

### Failures Logged: [count]
- Top failure category: [category]
- Patterns created from failures: [count]

### Successes Logged: [count]
- Top success category: [category]
- Patterns replicated: [count]

### Velocity Indicators
- Repeat failures: [count] (target: <2)
- KB reuse events: [count] (target: >10)
- First-attempt successes: [%] (target: >80%)
- Auto-fix applications: [count]

### Improvements Made
- Agents updated: [list]
- KB patterns added: [list]
- Rules updated: [list]
```

---

**Key Insight**: Every task is a learning opportunity. The compound effect of systematic extraction creates exponential capability growth.

**Activation**: "Apply continuous learning: extract pattern, log to KB, improve system"

**Self-Improvement Activation**: "Use system-improver to apply discovered patterns to configs"

**Failure Learning Activation**: "Log failure to FAILURE_LOG.md, create prevention pattern"

**Success Amplification**: "Log success to SUCCESS_LOG.md, replicate pattern across system"

---

## 17. Stuck Detection & Course Correction

### Detecting When User is Stuck

**Automatic Detection Triggers**:

| Signal | Detection | Response |
|--------|-----------|----------|
| Same error 3+ times | Error message repeats | Suggest different approach |
| Task taking too long | >30 min on same step | Offer to reassess |
| Circular conversation | Similar questions repeat | Summarize and redirect |
| No progress on todos | In-progress >15 min | Check blockers |
| User frustration signals | "still not working", "again" | Step back, audit |

### Detecting Suboptimal Approach

**Warning Signs**:

| Signal | What It Means | Correction |
|--------|---------------|------------|
| Spec creep | Adding features mid-task | Return to original spec |
| Over-engineering | Complex solution for simple task | Simplify |
| Reinventing wheel | Not using KB patterns | Search KB first |
| Wrong tool | Slow tool for fast task | Switch tools |
| Missing context | Asking for info in docs | Point to docs |

### Detecting Drift from Goal

**Drift Indicators**:

| Indicator | Detection | Recovery |
|-----------|-----------|----------|
| Scope expansion | Tasks added >50% | Freeze scope, create new spec |
| Topic change | Working on unrelated code | Confirm intentional |
| Yak shaving | Fixing fix of fix | Stop, address root cause |
| Premature optimization | Optimizing before working | Get it working first |
| Analysis paralysis | Research without action | Make decision, iterate |

### Proactive Intervention Patterns

**When to Intervene**:

```markdown
## Intervention Triggers

1. **3+ Failed Attempts**
   - Stop current approach
   - "I notice we've tried this 3 times. Let me step back and try a different approach."
   - Review what's been tried, identify what's different

2. **Long Task with No Progress**
   - Check todo list status
   - "We've been on this task for a while. Let me check if we're blocked."
   - Identify specific blocker, break into smaller steps

3. **Circular Pattern Detected**
   - Summarize the loop
   - "We seem to be going in circles. Let me summarize what we've tried..."
   - Propose concrete exit from loop

4. **Spec Approach Not Optimal**
   - Flag deviation from spec
   - "This is veering from our spec. Should we update the spec or return to plan?"
   - User decides: update spec OR refocus

5. **User Frustration Signals**
   - Acknowledge difficulty
   - "This is proving tricky. Let me try a completely different approach."
   - Fresh perspective, different tools/pattern
```

### Course Correction Actions

| Situation | Action |
|-----------|--------|
| Wrong approach | "Let me try [alternative] instead" |
| Missing info | "I need to check [source] first" |
| Scope creep | "That's beyond current spec. Create new task?" |
| Blocked | "I'm blocked on [X]. Can you [specific ask]?" |
| Stuck | "Let me step back and reconsider the problem" |

### Meta-Awareness Prompts

**Self-Check Questions** (Internal, not shown to user):

1. Am I making progress or spinning?
2. Is this the simplest approach?
3. Have I checked KB for existing solutions?
4. Is the spec still relevant?
5. Should I ask for clarification?

### Logging Stuck Events

**Location**: `KnowledgeBase/STUCK_LOG.md`

```markdown
## [Date] - [Task] - Stuck Event

**Signal**: What triggered detection
**Duration**: How long stuck
**Root Cause**: Why we got stuck
**Resolution**: How we got unstuck
**Prevention**: How to avoid next time
**Pattern Created**: ✅/⬜ Added to prevention
```

### Integration with Other Systems

| System | Integration |
|--------|-------------|
| FAILURE_LOG | Stuck events are a type of failure |
| ANTI_PATTERNS | Stuck patterns become anti-patterns |
| system-improver | Learns from stuck resolutions |
| insight-extractor | Extracts prevention patterns |

---

## 18. Persistent Issue Tracking

### What Makes an Issue "Persistent"

- Same error/issue 3+ times across sessions
- Known issue with no complete fix
- Workaround exists but root cause unresolved
- Platform/environment-specific limitation

### Persistent Issue Registry

**Location**: `KnowledgeBase/PERSISTENT_ISSUES.md`

```markdown
## [Issue ID] - [Short Description]

**First Seen**: [Date]
**Occurrences**: [Count]
**Platforms**: iOS/Android/Editor/etc.
**Status**: Open/Workaround/Investigating/Fixed
**Symptoms**: How it manifests
**Root Cause**: If known
**Workaround**: Current mitigation
**Fix Attempts**: What's been tried
**Blockers**: Why not fixed yet
**Related**: Links to other issues
```

### Escalation Thresholds

| Occurrences | Action |
|-------------|--------|
| 3 | Create entry in PERSISTENT_ISSUES.md |
| 5 | Escalate to session priority |
| 10 | Mark as critical, dedicated investigation |
| 20+ | Consider architectural change |

### Connecting to Learning Systems

```
Persistent Issue Detected
    ↓
Add to PERSISTENT_ISSUES.md
    ↓
If workaround exists → Add to _AUTO_FIX_PATTERNS.md
    ↓
Track occurrences
    ↓
When fixed → Log to SUCCESS_LOG.md
    ↓
Extract pattern → Update prevention rules
```

---

## 19. Predictive Intelligent Suggestions

### Goal: Fastest Path to Completion

Proactively suggest next steps that:
- Align with current goal
- Use proven patterns from KB
- Avoid known pitfalls
- Minimize time to completion

### Suggestion Triggers

| Context | Suggestion Type |
|---------|-----------------|
| Task started | Recommended approach based on similar past tasks |
| Task completed | Logical next steps in workflow |
| Error encountered | Likely fix from _AUTO_FIX_PATTERNS.md |
| Spec created | Implementation sequence from similar specs |
| Feature requested | KB patterns that apply |

### Suggestion Format

```markdown
**Suggested Next Steps**:

1. **[Action]** - [Why this is optimal]
   - KB Reference: [file if applicable]
   - Time estimate: [relative: quick/medium/extended]

2. **[Action]** - [Why this follows]
   - Dependency: [if depends on #1]

3. **[Action]** - [Final step toward goal]
```

### Prediction Sources

| Source | What It Provides |
|--------|------------------|
| SUCCESS_LOG.md | Patterns that worked |
| Spec history | Similar feature implementations |
| _AUTO_FIX_PATTERNS.md | Known fixes for likely errors |
| ANTI_PATTERNS.md | What to avoid |
| _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md | Unity code patterns |
| MASTER_DEVELOPMENT_PLAN.md | Sprint priorities |

### Context-Aware Recommendations

**When Starting a Feature**:
```
1. Check if spec exists in specs/
2. Search KB for similar implementations
3. Review SUCCESS_LOG for related patterns
4. Create spec if needed (spec-kit methodology)
5. Break into tasks.md
6. Begin implementation with most foundational task
```

**When Debugging**:
```
1. Check Unity console (read_console)
2. Search _AUTO_FIX_PATTERNS.md for known fix
3. If new error → log to FAILURE_LOG
4. Try fix → verify → iterate
5. If stuck 3+ times → try alternative approach
6. Once fixed → add to _AUTO_FIX_PATTERNS.md
```

**When Optimizing**:
```
1. Profile to identify bottleneck
2. Search _PERFORMANCE_PATTERNS_REFERENCE.md
3. Check platform-specific in PLATFORM_COMPATIBILITY_MATRIX.md
4. Apply minimal change
5. Verify improvement
6. Log to SUCCESS_LOG if significant
```

### Goal-Aware Prioritization

**Priority Matrix**:

| Goal Type | Prioritize | Avoid |
|-----------|------------|-------|
| Ship feature | Working code | Perfect code |
| Fix bug | Root cause | Band-aids |
| Performance | Measured gains | Premature optimization |
| Learning | Understanding | Copy-paste |
| Exploration | Breadth | Depth (initially) |

### Proactive Suggestion Examples

**After completing a Unity script**:
> "Script created. Suggested next steps:
> 1. Check console for compile errors (read_console)
> 2. Add to relevant scene if not referenced
> 3. Wire up in Inspector or via code
> 4. Test in Play mode before device build"

**After encountering AR null reference**:
> "AR texture null error. Suggested approach:
> 1. This is a known pattern - see _AUTO_FIX_PATTERNS.md 'AR Session Not Ready'
> 2. Apply TryGetTexture pattern from KB
> 3. Add to WaitForARSession coroutine if at startup
> 4. Verify on device (Editor may not reproduce)"

**When user asks about new feature**:
> "For [feature], suggested approach:
> 1. Check specs/ for similar: [list any matches]
> 2. KB patterns that apply: [list from search]
> 3. Recommended architecture: [based on existing codebase]
> 4. Estimated phases: [based on similar features]"

### Learning from Suggestions

**Track Suggestion Effectiveness**:

```markdown
## Suggestion Tracking

| Date | Suggestion | Followed? | Outcome | Adjustment |
|------|------------|-----------|---------|------------|
| 2026-01-21 | Check console first | Yes | Found error quickly | Keep |
| 2026-01-21 | Use async pattern | No | User preferred sync | Note preference |
```

**Improve Suggestions Over Time**:
- If suggestion followed → success → reinforce
- If suggestion followed → failure → revise
- If suggestion ignored → success → learn user preference
- If suggestion ignored → failure → note for future reference

### Integration with Todo System

When creating todos, include intelligent ordering:

```markdown
## Tasks: [Feature]

**Critical Path** (do these first):
- [ ] Task A - Foundation, blocks everything
- [ ] Task B - Depends on A, enables C and D

**Parallel** (can do simultaneously):
- [ ] Task C - Independent of D
- [ ] Task D - Independent of C

**Final** (do last):
- [ ] Task E - Integration, needs C and D done
- [ ] Task F - Validation
```

---

## 20. Intelligent Workflow Orchestration

### Workflow Templates by Goal

**Feature Implementation Workflow**:
```
1. [Search] Check KB for existing patterns
2. [Spec] Create or review spec
3. [Plan] Break into tasks with dependencies
4. [Implement] Foundation → Core → Integration → Polish
5. [Validate] Tests → Console → Device
6. [Document] Update KB if new pattern
7. [Log] Success/failure to appropriate log
```

**Bug Fix Workflow**:
```
1. [Reproduce] Confirm issue
2. [Search] Check _AUTO_FIX_PATTERNS.md
3. [Diagnose] Read console, check logs
4. [Fix] Minimal change to resolve
5. [Verify] Issue resolved, no regressions
6. [Prevent] Add to auto-fix patterns
7. [Log] To appropriate log
```

**Performance Optimization Workflow**:
```
1. [Measure] Baseline metrics
2. [Profile] Identify bottleneck
3. [Search] KB for optimization patterns
4. [Optimize] Targeted change
5. [Measure] Verify improvement
6. [Document] Log gains and technique
```

**Research/Exploration Workflow**:
```
1. [Scope] Define what we're looking for
2. [Search] KB first, then web
3. [Evaluate] Pros/cons of options
4. [Prototype] Quick test if needed
5. [Decide] Choose approach
6. [Document] Add findings to KB
```

### Predictive Blockers

**Anticipate Issues Before They Occur**:

| Task Type | Likely Blockers | Prevention |
|-----------|-----------------|------------|
| AR feature | Device-only testing needed | Test on device early |
| VFX integration | Property name mismatch | Use ExposedProperty |
| Multi-platform | Platform differences | Check PLATFORM_COMPATIBILITY_MATRIX |
| Networking | Connection issues | Test with mock first |
| Performance | Unknown bottleneck | Profile before optimizing |

### End-of-Session Suggestions

**Before ending session**:
```
"Session summary:
- Completed: [list]
- In progress: [list]
- Blocked: [list]

Suggested for next session:
1. [Most important incomplete task]
2. [Blocked item resolution]
3. [Logical next feature]

Patterns to log:
- [Any discoveries worth documenting]"
```
