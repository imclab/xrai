# Token Efficiency - Complete Reference

**Version**: 2.2 (2026-01-21) - Added Subagents, Thinking Triggers, Custom Agents
**Goal**: Stay below 95% weekly limits. Maximize 200K context efficiency.

---

## ⚠️ TOKEN LIMIT RULE
**Check every response. Stay below 95% weekly limit.** If approaching: stop non-essential work, summarize, end session.

---

## Quick Rules (Always Apply)

1. Agents use **independent token budgets** - use them for multi-step tasks
2. **.claudeignore** saves 180K+ tokens per Unity session
3. **Parallel tool calls** - batch independent operations
4. **Concise responses** - no preambles, bullets over prose
5. **Edit over Write** - smaller diffs use fewer tokens
6. **JetBrains MCP over raw tools** - 5-10x faster when Rider open
7. **Plan before code** - prevents wasted tokens on false starts
8. **Resources over tools** - use MCP resources for read-only data
9. **Checklists for bulk tasks** - systematic, not one massive prompt
10. **/clear between tasks** - don't let irrelevant context accumulate
11. **Check `/cost` or `/stats`** - monitor usage frequently
12. **Use hooks to filter output** - PreToolUse hooks reduce test/log verbosity

---

## Claude Code CLI Commands (Official)

| Command | Purpose | Token Impact |
|---------|---------|--------------|
| `/cost` | Check token usage (API users) | Free |
| `/stats` | Check usage (Max/Pro subscribers) | Free |
| `/clear` | Start fresh for unrelated work | Saves 10-50K |
| `/compact <focus>` | Shrink context with focus | Saves 20-80% |
| `/context` | See MCP server overhead | Free |
| `/mcp` | Disable unused servers mid-session | Saves 5-25K per server |
| `/model` | Switch to cheaper model | Sonnet = 0.3x Opus cost |
| `/rewind` | Restore to previous checkpoint | Recovery |
| `/rename <name>` | Name session before clearing | Organization |
| `/resume <name>` | Return to saved session | Recovery |

### Keyboard Shortcuts
- `Shift+Tab` - Enter plan mode (prevents false starts)
- `Escape` - Stop current direction
- `Double-Escape` - Restore conversation + code

### Environment Variables (settings.json)
```json
{
  "env": {
    "ENABLE_TOOL_SEARCH": "auto:5",
    "MAX_THINKING_TOKENS": "10000",
    "CLAUDE_CODE_MAX_OUTPUT_TOKENS": "16384",
    "CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC": "1"
  }
}
```

| Variable | Effect |
|----------|--------|
| `ENABLE_TOOL_SEARCH=auto:5` | Dynamic MCP tool loading at 5% context |
| `MAX_THINKING_TOKENS=10000` | Reduce from 31,999 default (billed as output) |
| `CLAUDE_CODE_MAX_OUTPUT_TOKENS=16384` | Limits output per response |
| `CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC=1` | Reduces background traffic |

### Hook Preprocessing (50-80% savings)

**settings.json PreToolUse hook for test filtering:**
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{
        "type": "command",
        "command": "~/.claude/hooks/filter-test-output.sh",
        "timeout": 30
      }]
    }]
  }
}
```

**filter-test-output.sh:**
```bash
#!/bin/bash
input=$(cat)
cmd=$(echo "$input" | jq -r '.tool_input.command')
if [[ "$cmd" =~ ^(npm\ test|pytest|go\ test) ]]; then
  filtered="$cmd 2>&1 | grep -A 10 -E '(FAIL|ERROR)' | head -100"
  echo "{\"hookSpecificOutput\":{\"hookEventName\":\"PreToolUse\",\"permissionDecision\":\"allow\",\"updatedInput\":{\"command\":\"$filtered\"}}}"
else
  echo "{}"
fi
```

### Skills vs CLAUDE.md
- **CLAUDE.md** loads at session start (~10-15K tokens if 1200+ lines)
- **Skills** load on-demand only when invoked
- **Action**: Move specialized instructions to Skills, keep CLAUDE.md < 500 lines

---

## Subagent Optimization (Independent Context Windows)

### Built-in Subagents

| Agent | Model | Use Case | Token Impact |
|-------|-------|----------|--------------|
| **Explore** | Haiku | Fast codebase search | Cheapest, read-only |
| **Plan** | Inherit | Research for planning | Read-only |
| **General-purpose** | Inherit | Complex multi-step | Full access |
| **Bash** | Inherit | Terminal operations | Bash only |

### Token Trade-offs

| Approach | Token Cost | Speed |
|----------|------------|-------|
| Single-threaded | 1x | Slow |
| 3-4 specialized agents | 2-3x | Fast |
| 10+ parallel agents | 4-5x | Fastest but expensive |

**Optimal**: 3-4 highly specialized agents, sequential with file handoffs.

### When to Use Subagents

✅ **Use subagents for**:
- Verbose output you don't need in main context (tests, logs)
- Enforcing specific tool restrictions
- Self-contained work returning summary
- Parallel research of independent modules
- Code review (separate from writing context)

❌ **Avoid subagents for**:
- Simple single-file edits
- Quick lookups (use direct tools)
- Tasks needing main conversation context

### Parallel Execution Patterns

**Up to 10 concurrent tasks** (additional queue):
```
Research auth, database, and API modules in parallel using separate subagents
```

**Sequential with file handoffs** (recommended for reviews):
```
1. style-checker → writes findings.md
2. security-reviewer → reads findings.md, appends
3. test-validator → reads all, final report
```

### Custom Subagent Configuration

**Location**: `~/.claude/agents/` (user) or `.claude/agents/` (project)

**Minimal example** (`~/.claude/agents/unity-checker.md`):
```yaml
---
name: unity-checker
description: Check Unity console and fix errors
tools: Read, Edit, Bash
model: haiku
---
You fix Unity compilation errors. Check console, read error file, apply fix.
```

**Model routing for cost**:
- `model: haiku` - Simple checks (0.3x cost)
- `model: sonnet` - Standard work (1x cost)
- `model: opus` - Complex architecture (3-5x cost)

### Background vs Foreground

| Mode | MCP Access | Permissions | Use Case |
|------|------------|-------------|----------|
| Foreground | Yes | Prompts through | Interactive work |
| Background | No | Auto-deny | Long-running tasks |

**Trigger background**: "run this in the background" or Ctrl+B

### Resume Subagents (Context Preservation)
```
Continue that code review and now analyze authorization
```
Resumed subagents retain full history - don't restart unnecessarily.

### Thinking Budget Triggers

Use graduated language for reasoning depth:
| Phrase | Effect |
|--------|--------|
| "think" | Baseline reasoning |
| "think hard" | Increased computation |
| "think harder" | More analysis |
| "ultrathink" | Maximum reasoning (expensive) |

### Pre-built Token-Efficient Agents (`~/.claude/agents/`)

| Agent | Model | Purpose |
|-------|-------|---------|
| `unity-error-fixer` | Haiku | Fix compilation errors (one file, minimal changes) |
| `unity-console-checker` | Haiku | Check console status (no file reads) |
| `vfx-tuner` | Haiku | Batch VFX property changes |
| `code-searcher` | Haiku | JetBrains indexed search (not Grep) |
| `test-runner` | Haiku | Async test execution, failures only |
| `research-agent` | Sonnet | KB and codebase research |
| `tech-lead` | Sonnet | Architecture decisions |

**Usage**: "Use unity-error-fixer to fix the console errors"

---

## Rider + Claude Code + Unity Integration (PRIMARY WORKFLOW)

### Tool Selection Matrix

| Task | Best Tool | Why |
|------|-----------|-----|
| Find files by name | JetBrains `find_files_by_name_keyword` | Indexed, 5x faster |
| Search code content | JetBrains `search_in_files_by_text` | Indexed, 10x faster |
| Read C# file | JetBrains `get_file_text_by_path` | Truncation support |
| Edit C# file | Claude `Edit` tool | Smaller diff, no IDE reload |
| Symbol lookup | JetBrains `get_symbol_info` | Native LSP |
| Rename refactor | JetBrains `rename_refactoring` | Project-wide safe |
| Find GameObjects | Unity `find_gameobjects` | IDs only, lightweight |
| Check errors | Unity `read_console` | Direct console access |
| Scene structure | Unity `manage_scene(get_hierarchy)` | Paged, filtered |
| Component ops | Unity `manage_components` | Direct manipulation |
| Run tests | Unity `run_tests` + `get_test_job` | Async, wait_timeout |
| File problems | JetBrains `get_file_problems` | Inspections |

### Workflow: Fix Compilation Error

```
FAST (3 tool calls):
1. Unity: read_console(types=["error"], count=3)
2. JetBrains: get_file_text_by_path(path, maxLinesCount=100)
3. Claude: Edit(file_path, old_string, new_string)

SLOW (avoid):
1. Grep for error pattern
2. Read entire file
3. Write entire file
4. Check console
5. Read file again to verify
```

### Workflow: Implement Feature

```
FAST:
1. JetBrains: search_in_files_by_text("similar pattern", fileMask="*.cs", maxUsageCount=5)
2. JetBrains: get_file_text_by_path(best_match)
3. Claude: Edit (add new code following pattern)
4. Unity: read_console(types=["error"], count=3)

SLOW (avoid):
1. Glob for all .cs files
2. Read multiple files speculatively
3. Write new file
4. Read console
5. Read file to verify
6. Read other files for context
```

### Workflow: Debug Runtime Issue

```
FAST:
1. Unity: read_console(types=["error","warning"], count=10)
2. Unity: find_gameobjects(search_term="ObjectName", page_size=5)
3. Unity: manage_components(target=<id>, include_properties=true, page_size=5)
4. JetBrains: get_file_text_by_path(script_path, maxLinesCount=200)

SLOW (avoid):
1. Get full scene hierarchy
2. Read all components on all objects
3. Read multiple script files
4. Search entire codebase
```

### Parallel Tool Calls (Same Message)

```
# Independent operations - call together:
- JetBrains search + Unity console check
- Multiple JetBrains searches with different patterns
- Unity find_gameobjects + JetBrains file read

# Dependent operations - call sequentially:
- Edit → then console check
- Find gameobject → then get components
- Search → then read found file
```

### Cross-Tool Caching

```
# Cache in conversation memory:
- Instance IDs from find_gameobjects
- File paths from JetBrains searches
- Asset paths from Unity searches
- Error patterns from console

# Don't re-fetch:
- File content you just edited
- Console after read-only operations
- Hierarchy if you have the ID
- Symbol info for same location
```

### Optimal Parameters Summary

```
# JetBrains (always use when Rider open)
search_in_files_by_text:
  maxUsageCount: 20-50
  fileMask: "*.cs"
  timeout: 10000

find_files_by_name_keyword:
  fileCountLimit: 10-25
  timeout: 5000

get_file_text_by_path:
  maxLinesCount: 200-500
  truncateMode: "MIDDLE"

# Unity MCP
read_console:
  types: ["error"]  # or ["error","warning"]
  count: 3-10
  include_stacktrace: false

find_gameobjects:
  page_size: 10-20
  search_method: "by_name" or "by_id"

manage_scene(get_hierarchy):
  page_size: 50
  max_depth: 3
  max_nodes: 100

manage_components:
  include_properties: false  # first pass
  page_size: 10

# Claude Native
Edit: Always prefer over Write
Grep: Only if Rider not open
Read: Only if Rider not open
```

### Error Recovery Patterns

```
# JetBrains timeout → retry with smaller scope
search_in_files_by_text(maxUsageCount=10, timeout=5000)

# Unity MCP not responding → check server
manage_editor(action="telemetry_ping")

# Compilation error loop → batch fixes
1. Collect all errors first
2. Fix in single Edit per file
3. Check console once after all fixes

# Large file → use truncation
get_file_text_by_path(maxLinesCount=300, truncateMode="MIDDLE")
```

### Speed Benchmarks (Approximate)

| Operation | JetBrains | Raw Tool | Speedup |
|-----------|-----------|----------|---------|
| Code search | 0.5-2s | 5-20s | 10x |
| File find | 0.2-1s | 1-5s | 5x |
| File read | 0.3-1s | 0.5-2s | 2x |
| Symbol info | 0.2-0.5s | N/A | Native |

| Operation | Unity MCP | Manual | Speedup |
|-----------|-----------|--------|---------|
| Console check | 0.2-0.5s | 5-10s | 20x |
| Find object | 0.3-1s | 2-5s | 5x |
| Get components | 0.5-2s | N/A | Direct |

### Anti-Patterns (Never Do)

```
❌ Grep when Rider is open
❌ Read when JetBrains can get_file_text_by_path
❌ Write when Edit works
❌ Full hierarchy when find_gameobjects suffices
❌ Console check after every micro-edit
❌ Re-read files you just edited
❌ Search without fileMask/scope
❌ Fetch with include_properties=true first pass
❌ generate_preview=true for asset searches
```

### Workflow: Refactor/Rename

```
FAST (2-3 calls):
1. JetBrains: rename_refactoring(pathInProject, symbolName, newName)
   # Project-wide, safe, handles all references
2. Unity: read_console(types=["error"], count=5)  # Verify no breaks

SLOW (avoid):
1. Search for all occurrences
2. Read each file
3. Edit each file manually
4. Check console after each edit
```

### Workflow: Add Component to GameObject

```
FAST (2-3 calls):
1. Unity: find_gameobjects(search_term="Name", page_size=1)
2. Unity: manage_components(action="add", target=<id>, component_type="Rigidbody")
3. [Optional] Unity: manage_components(action="set_property", target=<id>, ...)

SLOW (avoid):
1. Get full hierarchy
2. Search through results
3. Get all components
4. Add component
5. Read back to verify
```

### Workflow: Create New Script

```
FAST (3-4 calls):
1. JetBrains: search_in_files_by_text("similar class", fileMask="*.cs", maxUsageCount=3)
2. JetBrains: get_file_text_by_path(template_file, maxLinesCount=200)
3. Claude: Write(new_file_path, adapted_content)  # Write OK for new files
4. Unity: read_console(types=["error"], count=3)

# Or use Unity MCP for script creation:
1. Unity: create_script(path="Assets/Scripts/NewScript.cs", contents="...")
2. Unity: refresh_unity(scope="scripts", wait_for_ready=true)
```

### Workflow: VFX Property Tuning

```
FAST (batch):
Unity: batch_execute([
  {"tool": "manage_vfx", "params": {"action": "particle_set_main", "target": "VFX", "startSize": 0.5}},
  {"tool": "manage_vfx", "params": {"action": "particle_set_emission", "target": "VFX", "rateOverTime": 100}},
  {"tool": "manage_vfx", "params": {"action": "particle_set_color_over_lifetime", "target": "VFX", ...}}
], parallel=true)
# 1 call instead of 3

SLOW (avoid):
3 separate manage_vfx calls
```

### Workflow: Run and Check Tests

```
FAST (2 calls with wait):
1. Unity: run_tests(mode="EditMode") → job_id
2. Unity: get_test_job(job_id, wait_timeout=60, include_failed_tests=true)
   # Single call, waits for completion

SLOW (avoid):
1. run_tests
2. get_test_job (polling)
3. get_test_job (polling)
4. get_test_job (polling)...
```

### Workflow: Multi-File Edit (Same Pattern)

```
FAST (batch edits):
# Parallel Edit calls in single message
Claude: [
  Edit(file1, old1, new1),
  Edit(file2, old2, new2),
  Edit(file3, old3, new3)
]
# Then single console check
Unity: read_console(types=["error"], count=10)

SLOW (avoid):
Edit file1 → console → Edit file2 → console → Edit file3 → console
```

### Workflow: Find and Fix All Usages

```
FAST (3-4 calls):
1. JetBrains: search_in_files_by_text("pattern", fileMask="*.cs", maxUsageCount=50)
2. Review results, identify files
3. Claude: [Edit(file1, ...), Edit(file2, ...), ...]  # Parallel
4. Unity: read_console(types=["error"], count=10)

SLOW (avoid):
Sequential search → read → edit → verify per file
```

### Session Optimization

```
# When to /compact
- Context > 100K tokens
- Switching sub-tasks within same feature
- After completing a major milestone

# When to /clear
- Switching to unrelated task
- Context > 150K tokens
- After significant back-and-forth debugging

# When to start new session
- Context approaching 180K
- Session > 2 hours
- Changing projects entirely
```

### Context Window Budget (200K)

```
Typical Allocations:
- CLAUDE.md + GLOBAL_RULES.md: ~15K
- .claudeignore saves: 180K → 10K for Unity scan
- MCP tool schemas: ~5K per active server
- Conversation history: Variable

Target Usage:
- Stay under 100K for responsive performance
- Reserve 50K for tool results and responses
- Use agents for exploration (independent budget)
```

### Parallel Call Patterns (Advanced)

```
# Maximum parallelism example:
[
  JetBrains: search_in_files_by_text("pattern1", ...),
  JetBrains: search_in_files_by_text("pattern2", ...),
  Unity: read_console(types=["error"], count=5),
  Unity: find_gameobjects(search_term="Manager", page_size=10)
]
# 4 calls, 1 round trip

# After getting results, parallel edits:
[
  Edit(file1, old1, new1),
  Edit(file2, old2, new2),
  Edit(file3, old3, new3)
]
# 3 edits, 1 round trip

# Then single verification:
Unity: read_console(types=["error"], count=10)
```

### Project Path Optimization (JetBrains)

```
# Always pass projectPath when known
# Reduces ambiguous calls, faster resolution

mcp__jetbrains__search_in_files_by_text(
  searchText="pattern",
  projectPath="/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main",
  fileMask="*.cs",
  maxUsageCount=50
)
```

### Unity Instance Management

```
# If multiple Unity instances:
1. Unity: set_active_instance("MetavidoVFX-main@abc123")
2. Then proceed with operations

# Check instances:
Resource: mcpforunity://instances
```

### Common Error Fixes (Don't Research, Just Apply)

```
# CS0246 - Type not found
→ Add using statement

# CS0103 - Name not found
→ Check spelling, add using, or qualify name

# CS0019 - Operator cannot be applied
→ Cast types or use correct operator

# CS0029 - Cannot convert type
→ Add explicit cast or use conversion method

# NullReferenceException in AR
→ Use TryGetTexture pattern (see spec-005)

# Missing component
→ Add [RequireComponent(typeof(X))] or null check
```

### File Size Thresholds

```
# Small file (<200 lines): Read entire
get_file_text_by_path(path)

# Medium file (200-500 lines): Truncate
get_file_text_by_path(path, maxLinesCount=300, truncateMode="MIDDLE")

# Large file (>500 lines): Target read
get_file_text_by_path(path, maxLinesCount=100, truncateMode="START")
# Then search for specific section if needed
search_in_files_by_text("method name", directoryToSearch="path/to/file")
```

### MetavidoVFX-Specific Shortcuts

```
# VFX scripts location
Assets/Scripts/VFX/

# Core systems
ARDepthSource.cs - Hybrid Bridge entry point
VFXARBinder.cs - Per-VFX binding
VFXLibraryManager.cs - VFX lifecycle

# Hand tracking
Assets/Scripts/HandTracking/

# Common search patterns
search_in_files_by_text("VFXPropertyBinder", directoryToSearch="Assets/Scripts")
search_in_files_by_text("ExposedProperty", fileMask="*.cs")
```

### Workflow: Prefab Editing

```
FAST (3-4 calls):
1. Unity: manage_prefabs(action="open_stage", prefab_path="Assets/Prefabs/X.prefab")
2. Unity: batch_execute([
     {"tool":"manage_components","params":{"action":"add",...}},
     {"tool":"manage_gameobject","params":{"action":"modify",...}}
   ])
3. Unity: manage_prefabs(action="save_open_stage")
4. Unity: manage_prefabs(action="close_stage")

SLOW (avoid):
- Open, edit one thing, save, edit another, save again
```

### Workflow: Scene Setup

```
FAST (batch create):
Unity: batch_execute([
  {"tool":"manage_gameobject","params":{"action":"create","name":"Manager","components_to_add":["GameManager"]}},
  {"tool":"manage_gameobject","params":{"action":"create","name":"UI","components_to_add":["Canvas"]}},
  {"tool":"manage_gameobject","params":{"action":"create","primitive_type":"Cube","name":"Ground"}}
], parallel=true)
# Then single save
Unity: manage_scene(action="save", path="Assets/Scenes/NewScene.unity")
```

### Workflow: Asset Search and Modify

```
FAST (3 calls):
1. Unity: manage_asset(action="search", path="Assets", filter_type="Material", page_size=20)
2. Identify target asset from results
3. Unity: manage_material(action="set_material_color", material_path="...", color=[1,0,0,1])

SLOW (avoid):
- Search all assets
- Read each asset info
- Modify one by one
```

### Workflow: Debug with Logging

```
FAST (2-3 calls):
1. JetBrains: get_file_text_by_path(script, maxLinesCount=50)
2. Claude: Edit (add Debug.Log at key points)
3. Unity: read_console(types=["log"], count=20, filter_text="[DEBUG]")

# Use conditional compilation
#if UNITY_EDITOR
Debug.Log($"[DEBUG] {value}");
#endif
```

### Workflow: Performance Investigation

```
FAST (3 calls):
1. Unity: read_console(types=["warning"], count=20)  # Check for perf warnings
2. Unity: manage_scene(action="get_hierarchy", max_nodes=50)  # Overview
3. JetBrains: search_in_files_by_text("Update|LateUpdate|FixedUpdate", fileMask="*.cs", maxUsageCount=30)

# Look for:
- GetComponent in Update
- Instantiate/Destroy in hot paths
- LINQ in Update
- String concatenation in loops
```

### Workflow: AR Texture Pipeline

```
FAST (verify once):
1. Unity: find_gameobjects(search_term="ARCameraManager", page_size=1)
2. Unity: manage_components(target=<id>, include_properties=true, page_size=3)
3. JetBrains: get_file_text_by_path("Assets/Scripts/Bridges/ARDepthSource.cs", maxLinesCount=100)

# Check for TryGetTexture pattern
# Check compute shader dispatch
```

### Workflow: Git + Unity

```
FAST (parallel):
[
  Bash: git status --porcelain
  Unity: read_console(types=["error"], count=5)
]
# Verify clean before commit

# Commit workflow
Bash: git add -A && git commit -m "message"
# Don't: separate add, status check, commit
```

### Workflow: Build Validation

```
FAST (pre-build check):
1. Unity: read_console(types=["error","warning"], count=20)
2. Unity: manage_scene(action="get_build_settings")
3. JetBrains: get_file_problems(filePath="Assets/Scripts/EntryPoint.cs")

# Only build if all pass
```

### Workflow: Shader Debugging

```
FAST (3 calls):
1. JetBrains: find_files_by_name_keyword(nameKeyword="shader", fileCountLimit=10)
2. JetBrains: get_file_text_by_path(shader_path, maxLinesCount=200)
3. Unity: manage_material(action="get_material_info", material_path="...")

# Check shader keywords, properties
```

### Workflow: Find Dead Code

```
FAST (2 calls):
1. JetBrains: search_in_files_by_text("class.*:.*MonoBehaviour", fileMask="*.cs", maxUsageCount=100)
2. Unity: find_gameobjects(search_method="by_component", search_term="ScriptName", page_size=1)
   # If 0 results, script might be unused

# Or use Rider's inspection
JetBrains: get_file_problems(filePath, errorsOnly=false)
```

### Workflow: API Migration

```
FAST (find all → batch replace):
1. JetBrains: search_in_files_by_text("oldAPICall", fileMask="*.cs", maxUsageCount=100)
2. Group by file
3. Claude: [Edit(f1, old, new), Edit(f2, old, new), ...]  # Parallel per file
4. Unity: read_console(types=["error"], count=20)

# For project-wide symbol rename, use:
JetBrains: rename_refactoring(...)
```

### Response Tokens: Minimize Output

```
# When asking Claude to summarize/report:
- Request bullet points only
- Request file:line format
- Request "changes made" not "what I did and why"
- Skip explanations unless asked

# Good: "Fixed. 3 files edited."
# Bad: "I have successfully completed the task by editing three files..."
```

### Tool Result Optimization

```
# Reduce result size:
- maxUsageCount / fileCountLimit: Start low (10-25)
- maxLinesCount: 100-300 usually enough
- include_properties: false first
- generate_preview: false always
- include_stacktrace: false unless debugging
- truncateMode: "MIDDLE" preserves structure

# Increase only if needed:
- Not finding what you need → increase limit
- Need full context → increase lines
- Need property values → include_properties=true
```

### Incremental Context Loading

```
# Start narrow, expand if needed:
1. search_in_files_by_text(maxUsageCount=10)
   → If not found, increase to 30

2. get_file_text_by_path(maxLinesCount=100)
   → If need more, request specific range or truncateMode

3. manage_components(include_properties=false)
   → If need values, request with include_properties=true, page_size=5
```

### Conversation Memory Patterns

```
# Remember across conversation:
- Instance IDs: "Player has ID 12345"
- File paths: "Main script at Assets/Scripts/GameManager.cs"
- Error patterns: "Getting CS0246 on line 42"
- Search results: "Found 5 files with pattern"

# Don't re-query:
- Same file you just read
- Same objects you just found
- Same console you just checked (unless edited)
```

### Early Exit Patterns

```
# Stop when done:
- Error fixed → don't re-verify multiple times
- Feature works → don't add unnecessary polish
- Question answered → don't elaborate unless asked

# Verify once:
- Single read_console after batch edits
- Single test run after changes
- Single search to confirm pattern
```

### Tool Call Batching Rules

```
# Same message (parallel):
✅ Multiple independent searches
✅ Multiple independent reads
✅ Search + console check
✅ Multiple Edits to different files

# Sequential (wait for result):
⏳ Search → then read found file
⏳ Find object → then get components
⏳ Edit → then verify console
⏳ Create script → then refresh Unity
```

### Error Handling Shortcuts

```
# MCP timeout
→ Retry with smaller scope/limit

# Unity MCP not connected
→ manage_editor(action="telemetry_ping") to check
→ If fails, user needs to restart Unity/server

# JetBrains not responding
→ Check if Rider is running
→ Verify project is open

# Compilation errors
→ Batch collect all errors first
→ Fix in priority order (dependencies first)
→ Single verify at end
```

### Cost/Benefit Decisions

```
# Worth the tokens:
✅ JetBrains indexed search (saves time)
✅ Unity MCP console check (direct access)
✅ batch_execute for multiple ops
✅ Parallel Edit calls
✅ Agent for exploration (independent budget)

# Not worth tokens:
❌ Re-reading to verify edit worked
❌ Multiple console checks during edits
❌ Full hierarchy for single object
❌ All properties when you need one
❌ Research for common error fixes
```

### MetavidoVFX Quick Paths

```
# Scripts by category
VFX:        Assets/Scripts/VFX/
Bridges:    Assets/Scripts/Bridges/
Recording:  Assets/Scripts/Recording/
HandTracking: Assets/Scripts/HandTracking/
UI:         Assets/Scripts/UI/

# Key files
ARDepthSource:     Assets/Scripts/Bridges/ARDepthSource.cs
VFXLibraryManager: Assets/Scripts/VFX/VFXLibraryManager.cs
VFXARBinder:       Assets/Scripts/VFX/VFXARBinder.cs
HandVFXController: Assets/Scripts/HandTracking/HandVFXController.cs

# VFX assets
Resources/VFX/     - 73 VFX by category
Assets/VFX/        - 292 additional VFX

# Scenes
Assets/HOLOGRAM.unity
Assets/Scenes/SpecDemos/
```

---

## Session Management

| Action | Token Impact |
|--------|--------------|
| Start fresh session | Saves 10-50K vs compression |
| /compact mid-task | Use when context >100K |
| /clear for new task | Full reset, zero carryover |
| Auto-compress (>150K) | Start new session instead |

**Session Budget**:
- Target: <100K tokens (50% utilization)
- Warning: 100-150K tokens
- Critical: >150K (start new session)

---

## Tool Usage Optimization

### Do This
- `head_limit` on Grep/Glob to cap results
- Prefer JetBrains MCP (indexed, 10x faster)
- Use `haiku` model for simple Task agents
- Batch independent tool calls in parallel
- Task/Explore agent for open-ended searches
- One Grep with regex OR (`a|b|c`) vs multiple
- `glob` filter on Grep to narrow scope
- `offset` param for pagination

### Don't Do This
- Re-read files to "verify" edits
- WebSearch/WebFetch unless needed
- Full PR/issue fetch when title suffices
- Manual multi-step tasks agents can handle
- Read entire PackageCache (50-100K tokens)

### Tool Token Costs
| Tool | Avg Cost |
|------|----------|
| Tool call (avg) | ~100 tokens |
| File read (small) | 500-2K tokens |
| Unity MCP console | 50-100 tokens |
| Agent execution | **0 tokens** (independent) |

---

## Response Efficiency

### Do This
- No code blocks unless requested
- Bullets over paragraphs
- Single-line answers when possible
- Reference line numbers, don't quote
- Say "done" not "I have successfully..."
- Truncate long lists with "..."

### Don't Do This
- Preambles ("let me", "I'll", "sure")
- Emoji (unless requested)
- Rhetorical questions
- "Feel free to ask" closers
- Repeat user's question back
- Explain obvious code
- Offer to do more after task

### Token Savings
- Concise responses: 20-40% savings per exchange
- Thinking mode: 2-10K tokens per response
- Standard response: 500-2K tokens

---

## MCP Server Optimization

### Essential (Keep Active)
- UnityMCP - Unity Editor integration
- jetbrains - Rider indexed search
- claude-mem - Persistent memory
- github - Repo operations

### Disable When Unused
- memory, filesystem, fetch - Redundant
- supabase, chrome-devtools, notion
- Each unused MCP: 5-25K tokens/session

**Command**: `claude mcp remove memory filesystem fetch 2>/dev/null`

### MCP Protocol Optimizations (from official docs)

**Resources vs Tools**:
- Use **resources** for static/read-only data (faster, cacheable)
- Use **tools** only when execution/mutation required

**Pagination** (50-90% savings):
```json
{"method": "resources/list", "params": {"cursor": "..."}}
```
- Use cursor-based pagination, don't fetch all at once

**URI Templates** (70-95% savings):
- Request specific: `file:///src/config.json`
- Not comprehensive lists

**Priority Filtering** (10-30% savings):
- Skip resources with `priority < 0.5`
- Filter by `audience` (assistant vs user)

**Change Subscriptions** (40-60% savings):
- Subscribe to updates instead of polling
- Only re-fetch on `list_changed` notification

| Strategy | Token Savings |
|----------|---------------|
| Pagination | 50-90% |
| URI Templates | 70-95% |
| Subscriptions | 40-60% |
| Priority Filter | 10-30% |

---

## JetBrains MCP (MANDATORY when Rider open)

**ALWAYS prefer over raw tools** - indexed = 5-10x faster.

| Instead Of | Use This | Speed |
|------------|----------|-------|
| Grep | `search_in_files_by_text` | 10x |
| Grep (regex) | `search_in_files_by_regex` | 10x |
| Glob | `find_files_by_name_keyword` | 5x |
| Read | `get_file_text_by_path` | 2x |
| LSP | `get_symbol_info` | Native |

### Optimal Parameters

**search_in_files_by_text**:
- `maxUsageCount: 50` - Cap results
- `caseSensitive: false` - Faster
- `fileMask: "*.cs"` - Narrow scope
- `timeout: 10000` - 10s max

**find_files_by_name_keyword**:
- `fileCountLimit: 25` - Cap results
- `timeout: 5000`

**get_file_text_by_path**:
- `maxLinesCount: 500` - Limit large files
- `truncateMode: "MIDDLE"` - Keep start+end

**get_symbol_info**:
- Use for hover/definition (line/column 1-based)

**Enforcement**: If Rider open, NEVER use raw Grep/Glob/Read for project files.

---

## .claudeignore (CRITICAL for Unity)

```
Library/
Temp/
Logs/
Builds/
UserSettings/
obj/
*.pdb
*.cache
```

**Impact**: 190K → 10K tokens (95% reduction)

---

## Unity MCP Token Costs (Estimate)

| Operation | Low | High | Notes |
|-----------|-----|------|-------|
| read_console (5 errors) | 100 | 500 | +stacktrace = 2-5x |
| find_gameobjects | 50 | 200 | IDs only |
| get_hierarchy (full) | 1K | 10K | Use paging! |
| get_hierarchy (paged) | 100 | 500 | Recommended |
| get_components (meta) | 50 | 200 | No properties |
| get_components (full) | 200 | 2K | With properties |
| asset search (25) | 100 | 500 | +previews = 10x |
| batch_execute (5 ops) | 150 | 400 | vs 250-1000 separate |
| script read (500 lines) | 500 | 1K | Use JetBrains if Rider open |
| resource URI read | 50 | 300 | Lightweight |

**Rule of Thumb**: Batch = 3-5x savings, Paging = 5-10x savings, No previews = 10x savings

---

## Unity MCP Optimization

### Console Checking
```
read_console(types=["error"], count=5, include_stacktrace=false)
```
- Filter to errors only (skip log/warning)
- Small count, increase only if needed
- Skip stacktrace unless debugging

### Scene Hierarchy (can be huge)
```
manage_scene(action="get_hierarchy", page_size=50, max_depth=3, max_nodes=100)
```
- Paginate with cursor, don't fetch all
- Limit depth and nodes

### Asset Search
```
manage_asset(action="search", page_size=25, generate_preview=false)
```
- **generate_preview=false** CRITICAL - previews add huge base64

### GameObject Components
```
manage_gameobject(action="get_components", include_properties=false, page_size=10)
```
- Metadata only first, properties only when needed

### Test Running (Async)
```
1. run_tests(mode="EditMode") → job_id
2. get_test_job(job_id, wait_timeout=30, include_details=false)
3. include_failed_tests=true only if failures
```

### Script Operations
```
validate_script(uri, level="basic")  # Quick check before full read
get_sha(uri)                          # Metadata without content
```

### Debugging Workflow (Token-Efficient)
```
1. read_console(types=["error"], count=5)
2. Read ONLY the specific file with error
3. Fix with Edit (not Write)
4. read_console to verify (once)
5. Stop when clean
```

**DON'T**:
- Read multiple files speculatively
- Fetch full stacktraces unless stuck
- Check console after every small edit
- Read entire scene hierarchy at once

### Batch Operations (10-100x savings)
```
batch_execute(commands=[
  {"tool": "manage_gameobject", "params": {...}},
  {"tool": "manage_components", "params": {...}},
], parallel=true)
```
- One call instead of 5-25 separate
- Use for: creating multiple objects, adding components, setting properties

### Find GameObjects (IDs only)
```
find_gameobjects(search_term="Enemy", search_method="by_name", page_size=10)
```
- Returns IDs only (lightweight)
- Cache IDs, don't re-search
- Fetch specific: `target=<id>, search_method="by_id"`

### Script Editing Priority
```
script_apply_edits > apply_text_edits > manage_script
```
- Structured edits = smaller payloads
- Use Edit tool for simple changes (even smaller)

### Refresh Unity (Minimize)
```
refresh_unity(mode="if_dirty", scope="scripts", wait_for_ready=true)
```
- Only after batch of edits, not each one
- Scope to what changed (scripts vs all)

### Caching (Session Memory)
- Remember instance IDs from searches
- Remember asset paths
- Don't re-read files you just edited
- Trust edit success unless error

### Avoid Redundant
- Console check: After batch, not each edit
- Refresh: After batch, not each edit
- Hierarchy: Use find_gameobjects instead
- Components: Skip if only need transform
- Editor ping: Skip if last operation succeeded

### VFX Operations (Batch when possible)
```
# BAD: 5 separate calls
manage_vfx(action="particle_set_main", target="Fire", ...)
manage_vfx(action="particle_set_emission", target="Fire", ...)

# GOOD: 1 batch call
batch_execute(commands=[
  {"tool": "manage_vfx", "params": {"action": "particle_set_main", ...}},
  {"tool": "manage_vfx", "params": {"action": "particle_set_emission", ...}},
])
```

### Material Operations
```
manage_material(action="get_material_info", ...)  # Read first
# Then batch property sets
batch_execute([
  {"tool": "manage_material", "params": {"action": "set_material_color", ...}},
  {"tool": "manage_material", "params": {"action": "set_material_shader_property", ...}},
])
```

### Prefab Operations
```
# Open once, make all edits, save once
manage_prefabs(action="open_stage", prefab_path="...")
# ... batch edits ...
manage_prefabs(action="save_open_stage")
manage_prefabs(action="close_stage")
```

### Resource URIs (Skip tool calls)
```
# Instead of tool call, use resource read:
mcpforunity://scene/gameobject/{id}           # GameObject data
mcpforunity://scene/gameobject/{id}/components # Component list
mcpforunity://editor_state                     # Editor status
mcpforunity://project_info                     # Project metadata
```

### Progressive Detail Loading
```
1. find_gameobjects → IDs only
2. Resource URI → basic data
3. get_components(include_properties=false) → component list
4. get_components(include_properties=true, page_size=3) → only if needed
```

### Common Workflows (Optimized)

**Add Component to Object**:
```
1. find_gameobjects (if don't have ID)
2. manage_components(action="add", target=<id>, component_type="...")
# Skip: hierarchy fetch, full component read
```

**Debug Script Error**:
```
1. read_console(types=["error"], count=3)
2. Read ONLY file in error
3. Edit fix
4. read_console(types=["error"], count=3)
# Skip: full refresh, stacktrace, multiple files
```

**Create Prefab with Components**:
```
batch_execute([
  {"tool": "manage_gameobject", "params": {"action": "create", "name": "X", ...}},
  {"tool": "manage_components", "params": {"action": "add", ...}},
  {"tool": "manage_components", "params": {"action": "add", ...}},
  {"tool": "manage_prefabs", "params": {"action": "create_from_gameobject", ...}},
])
# 1 call instead of 4
```

---

## Agent Usage (Massive Savings)

Agents use **independent token budgets** - not counted toward main session.

| Agent | Model | Use For |
|-------|-------|---------|
| Explore | Haiku | Fast searches, 10-30 sec |
| research-agent | Sonnet | Knowledge base research |
| code-tester | Sonnet | Automated testing |
| Plan | Sonnet | Architecture planning |

**Rule**: If task is 3+ steps, use an agent.

---

## Model Selection

| Model | Use For | Token Cost |
|-------|---------|------------|
| Sonnet | 95% of tasks | 1x baseline |
| Opus | Complex architecture | 3-5x baseline |
| Haiku | Simple agents | 0.3x baseline |

**Never use Opus for**: File edits, searches, simple fixes

---

## Code Efficiency

### Do This
- Edit over Write (smaller diffs)
- Reuse existing patterns/utilities
- Minimal whitespace changes

### Don't Do This
- Unnecessary comments/docstrings
- "Improvements" beyond request
- Type annotations (unless required)
- Refactor adjacent code
- Defensive coding for impossible cases

---

## Memory (claude-mem)

### Do This
- Query sparingly, targeted searches
- Combine related memories into single save
- Include project name in queries

### Don't Do This
- Query for tasks with full context provided
- Save what's already in project docs
- Save routine/trivial tasks

---

## Visual Communication

**One image = 500-2000 words saved**

Use for:
- Error screenshots
- UI layouts
- Visual bugs
- Design references

---

## Planning Efficiency

### Do This
- Clarifying questions upfront (avoid rework)
- Direct action when path is clear
- One plan, not multiple options
- Assume reasonable defaults

### Don't Do This
- Speculative exploration
- Ask obvious questions
- Skip insight blocks (unless high value)
- Background/context user already knows

---

## Git/GitHub

- Short commit messages (one line)
- Skip PR body unless required
- Don't read git history unless needed
- Assume main branch

---

## Documentation

- **Never create .md files** unless requested
- No automatic summaries
- Update existing docs only
- Link to external docs (don't duplicate)

---

## Token Budget Examples

### Efficient Session (40K tokens)
- CLAUDE.md: 10K
- Project scan (.claudeignore): 5K
- 3 active MCPs: 10K
- 5 file reads: 10K
- 2 agent launches: 0K
- 3 standard responses: 5K

### Wasteful Session (180K tokens)
- CLAUDE.md (bloated): 20K
- Project scan (no ignore): 100K
- 7 unused MCPs: 35K
- 3 thinking responses: 15K
- Manual multi-step: 10K

---

## What NOT To Do (Complete List)

- Announce tool usage before calling
- Repeat user's question back
- Multiple options when one is best
- Safety caveats for routine operations
- Explain obvious code
- Warn about "potential issues" speculatively
- Suggest tests unless asked
- Offer to do more after completing task
- List files about to read/edit
- Describe reasoning unless asked
- Create documentation automatically
- Re-summarize previous work

---

## Resources

- [Token Counting](https://platform.claude.com/docs/en/build-with-claude/token-counting)
- [Token-Efficient Tool Use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/token-efficient-tool-use)
- [Claude Code Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)

---

## Advanced Techniques

### Headless Mode for Automation
```bash
claude -p "task description" --output-format json
```
- Batch processing of similar tasks
- More efficient than interactive sessions
- Use for CI/CD integration

### Multi-Instance Parallelization
- Run independent tasks across multiple Claude instances
- Use git worktrees for parallel development
- Consolidates token usage across tasks

### Pre-configured Permissions
```json
// .claude/settings.json
{
  "allowedTools": ["Read", "Edit", "Bash"],
  "permissions": { "allow": ["read:**", "edit:**"] }
}
```
- Avoid repeated permission dialogs
- Reduces back-and-forth token usage

### Selective File References
- Tab-complete specific files, don't paste codebases
- Mention paths: "see src/utils.ts" vs pasting content
- Let Claude read on-demand

### Context Pruning Triggers
- /compact at 100K tokens
- /clear between unrelated tasks
- Start new session at 150K
- Don't let old context accumulate

### Stored Slash Commands
```
/commit - pre-configured commit workflow
/test - run specific test suite
/deploy - deployment checklist
```
- Reusable operations without re-explaining
- Saves setup tokens each time

---

## Summary: Maximum Savings

| Technique | Savings | When to Use |
|-----------|---------|-------------|
| .claudeignore | 95% | All Unity projects |
| Agents | 50-90% | Multi-step tasks |
| JetBrains MCP | 80% | When Rider open |
| batch_execute | 70-90% | Multiple Unity ops |
| Pagination | 50-90% | Large result sets |
| Plan before code | 30-50% | New features |
| /clear between tasks | 20-40% | Session hygiene |
| Concise responses | 20-40% | All interactions |

---

## Platform-Specific Optimizations

### Claude Code CLI Flags
```bash
# Headless/automation
claude -p "task" --output-format json
claude -p "task" --output-format stream-json

# Session management
claude -c                    # Continue last conversation
claude -r                    # Resume from list

# System prompt optimization
--append-system-prompt "..."  # Add to defaults (safest)
--system-prompt "..."         # Replace defaults (full control)

# Debugging
--verbose                     # Debug output
--mcp-debug                  # MCP issues

# Performance
--disable-slash-commands     # Skip slash command parsing
--agent <name>              # Use specific agent config
```

**Slash Commands**:
- `/clear` - Wipe context (between tasks)
- `/compact` - Summarize context (mid-task)
- `/init` - Generate CLAUDE.md
- `/optimize` - Analyze code performance

### Xcode Build Optimization
```
# Build Settings
Build Active Architecture Only: YES (Debug)
Optimization Level: None (Debug), Speed (Release)
Compilation Mode: Incremental (Debug), Whole Module (Release)

# Xcode 26+ Features
Compilation Cache: 24-77% faster rebuilds
Swift Explicit Modules: On by default

# Debug Slow Compilation
-driver-time-compilation
-Xfrontend -debug-time-function-bodies
-Xfrontend -debug-time-expression-type-checking
```

**Best Practices**:
- Modularize code for incremental builds
- Use Carthage over CocoaPods (builds deps once)
- Optimize Run Script phases (skip in Debug)
- Use Tuist cache for team builds

### iOS/macOS Build
```bash
# Skip unchanged
--skip-unity-export
--skip-pod-install

# DerivedData management
rm -rf ~/Library/Developer/Xcode/DerivedData  # Nuclear option

# Device checks
xcrun devicectl list devices
```

### Android/Gradle Optimization
```
# IL2CPP Settings (faster builds)
IL2CPP Code Generation: "Faster (smaller) builds"
C++ Compiler Configuration: Debug (for iteration)

# Gradle
- Enable Custom Main Gradle Template
- Use gradle cache (OVR Build APK tool)
- Application Patching for debug builds

# Quick Preview
OVR Scene Quick Preview - hot reload via Asset Bundles
```

### WebGL Build Size
```
# Build Settings
Code Optimization: "Disk Size with LTO" (release only)
Compression: Brotli
Name Files as Hashes: YES
Strip Engine Code: YES
IL2CPP Code Generation: "Smaller Builds"

# Texture Optimization
ASTC: 8x8 block size (balance)
Crunch compression for DXT/ATCM

# Audio
Compressed memory for background
Decompress on load for SFX
Lower quality for background music

# Typical Sizes
Empty project: 7-8MB → optimized: 2MB
Remove New Input System if unused: -2.4MB
```

### Meta Quest VR
```
# Performance Targets
Max 500-1000 draw calls per frame
Max 1-2M triangles per frame
Minimize texture count/size

# Tools
Meta Quest Runtime Optimizer (Asset Store, free)
- Bottleneck Analysis
- What If? Analysis
- Proactive issue surfacing

OVR Metrics Tool - real-time performance
Meta Quest Developer Hub (MQDH)

# Settings
Dynamic Resolution: Required for GPU Level 5 (Quest 2/Pro)
Foveated Rendering: Enabled
LOD, Culling, Batching
```

### JetBrains Rider
```
# Indexed Search (5-10x faster than raw)
search_in_files_by_text(maxUsageCount=50, timeout=10000)
find_files_by_name_keyword(fileCountLimit=25)
get_file_text_by_path(maxLinesCount=500, truncateMode="MIDDLE")

# Symbol Navigation
get_symbol_info(line, column)  # Native LSP

# Refactoring
rename_refactoring  # Project-wide, safe
replace_text_in_file  # Simple find/replace
```

### macOS (OSX)
```bash
# Process management
killall -9 Unity Hub xcodebuild java  # Nuclear

# Cleanup
rm -rf ~/Library/Developer/Xcode/DerivedData
rm -rf ios/build ios/Pods
rm -rf android/build

# MCP cleanup
mcp-kill-dupes
unity-mcp-cleanup

# Device
xcrun devicectl list devices
```

### Gemini CLI
```bash
# Free tier (massive allowance)
60 requests/minute, 1000 requests/day FREE

# Models
gemini-3-flash    # 78% SWE-bench, <1/4 cost of Pro
gemini-3-pro      # Most intelligent, complex tasks

# Features
- 1M token context window
- Token caching built-in
- MCP Server integration
- Extensions framework (Figma, Shopify, Stripe, etc.)

# Use Cases
- Parallel with Claude for research
- Lower cost for simple queries
- Grounded search (Google Search built-in)
```

### OpenAI Codex CLI
```bash
# Pricing
codex-mini-latest: $1.50/1M input, $6/1M output
75% prompt caching discount

# Token Efficiency
GPT-5-Codex: 93.7% fewer tokens than GPT-5 (bottom 10% turns)
GPT-5.1-Codex-Max: 30% fewer thinking tokens

# Compaction (Native)
- Works across multiple context windows
- Project-scale refactors
- Multi-hour agent loops
- Accurate token estimation during compaction

# Optimization Tips
- Set up environment BEFORE launching (saves probing tokens)
- Prompt optimization: 20-40% reduction
- Use 'medium' reasoning effort when possible
```

### Unity Editor Scripting
```csharp
// Disable Domain/Scene Reload (massive Play Mode speedup)
// Edit > Project Settings > Editor > Enter Play Mode Settings
// Can speed up iteration by 60%+

// Batch Mode (CI/headless)
Unity -batchmode -nographics -executeMethod MyClass.MyMethod -quit

// Assembly Definitions
// Separate Editor scripts → 60% faster iteration in large projects

// Object Pooling
// Reuse objects instead of Instantiate/Destroy
// Eliminates GC spikes

// SRP Batcher
// Reduce render-state changes, not draw call count
// Use few shader variants for optimal batching

// Sprite Atlas
// Bundle sprites → single GPU batch
// Fundamental for 2D/UI optimization
```

### MCP Tools General
```
# Resources over Tools (read-only ops)
# Pagination for large results
# URI templates for specific requests
# Subscriptions for change detection
# Priority filtering (skip < 0.5)
```

### VS Code AI Extensions
```
# Performance Ranked
Codeium          - Free, blazing fast
Tabnine          - Local/private, no external servers
llama.vscode     - Local LLM, large context support
GitHub Copilot   - Most popular, 41M+ installs
Amazon Q         - AWS ecosystem, secure patterns
Continue         - Custom model connection (OpenAI/Anthropic)
Sourcery         - AI code review, 30+ languages

# Performance Tracking
WakaTime         - Time tracking, 600+ languages
```

### Prompt Engineering (Token Optimization)
```
# Concise Prompting: 30-50% token reduction
- Distill to core components
- Use structured delimiters (#, numbered lists)
- Tags: <task>, <context>, <output>

# RAG + Vector DB: Up to 70% context reduction
- Pinecone, Weaviate, Chroma
- Retrieve only relevant context

# Context Engineering
- Smaller = better (context rot is real)
- High-signal tokens only
- End-of-session context files (CLAUDE.md, GEMINI.md)

# Model-Specific
- GPT: Short structured prompts, delimiters
- Claude: Semantic clarity, XML tags
- Gemini: Hierarchical (outline not paragraph), temp=1.0

# Temperature
- Factual/extraction: temp=0
- Gemini 3: Keep temp=1.0 (prevents loops)
```

### Multi-AI Orchestration
```
# Parallel Processing
- Claude: Complex reasoning, long tasks
- Gemini: Research, grounded search (FREE tier)
- Codex: Large refactors, multi-hour loops

# Cost Optimization Pattern
1. Hill climb quality first (best model)
2. Down climb cost second (cheaper model)
3. Balance via evals + error analysis

# Session Handoff
- Context file at end of session
- Include: instructions, deps, architecture
- Next session starts with "cheat sheet"
```

---

## Cross-Platform Checklist

| Platform | Key Optimization | Savings |
|----------|------------------|---------|
| Unity Editor | .claudeignore | 95% |
| Unity Play Mode | Disable Domain/Scene Reload | 60% |
| iOS/Xcode | Build Active Arch, Cache | 24-77% |
| Android | Gradle cache, Quick Preview | 50-80% |
| WebGL | LTO, Strip, Brotli | 30-70% |
| Quest | Runtime Optimizer, Foveation | 20-50% |
| Claude CLI | -p, --output-format json | 30-50% |
| JetBrains | Indexed search over raw | 80% |
| Gemini CLI | Free tier, 1M context | 100% cost |
| Codex CLI | Compaction, 75% cache discount | 30-94% |
| VS Code | Codeium (free), local LLM | Variable |
| Prompt Eng | Concise + RAG | 30-70% |
| Multi-AI | Parallel + model selection | 40-60% |

**See Also**: `~/GLOBAL_RULES.md` §Token Efficiency (MANDATORY)

---

## Sources

### Claude & Anthropic
- [Claude Code CLI Reference](https://code.claude.com/docs/en/cli-reference)
- [Claude Code Tips (ykdojo)](https://github.com/ykdojo/claude-code-tips)
- [Claude Code Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)
- [Effective Context Engineering](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)

### OpenAI Codex
- [Codex CLI](https://developers.openai.com/codex/cli/)
- [GPT-5.2-Codex](https://openai.com/index/introducing-gpt-5-2-codex/)
- [Codex CLI Features](https://developers.openai.com/codex/cli/features/)

### Google Gemini
- [Gemini CLI](https://github.com/google-gemini/gemini-cli)
- [Gemini 3 Developer Guide](https://ai.google.dev/gemini-api/docs/gemini-3)
- [Gemini Prompting Strategies](https://ai.google.dev/gemini-api/docs/prompting-strategies)

### Unity & Platforms
- [Unity WebGL Optimization](https://docs.unity3d.com/2022.3/Documentation/Manual/web-optimization-mobile.html)
- [Unity Script Optimization](https://docs.unity3d.com/2020.1/Documentation/Manual/MobileOptimizationPracticalScriptingOptimizations.html)
- [Unity Android Gradle](https://docs.unity3d.com/6000.2/Documentation/Manual/android-gradle-overview.html)
- [Meta Quest Runtime Optimizer](https://developers.meta.com/horizon/documentation/unity/unity-quest-runtime-optimizer/)
- [Meta VR Performance Guidelines](https://developers.meta.com/horizon/documentation/native/pc/dg-performance-guidelines/)

### Xcode & Apple
- [Apple: Improving Incremental Builds](https://developer.apple.com/documentation/xcode/improving-the-speed-of-incremental-builds)
- [Xcode Build Performance (SwiftLee)](https://www.avanderlee.com/optimization/analysing-build-performance-xcode/)

### Prompt Engineering
- [OpenAI Prompt Engineering](https://platform.openai.com/docs/guides/prompt-engineering)
- [Token Optimization 2025](https://sparkco.ai/blog/optimizing-token-usage-for-ai-efficiency-in-2025)
- [Google AI Coding Best Practices](https://cloud.google.com/blog/topics/developers-practitioners/five-best-practices-for-using-ai-coding-assistants)

### VS Code & Tools
- [Best VS Code AI Extensions](https://graphite.com/guides/best-vscode-extensions-ai)
- [MCP Resources](https://modelcontextprotocol.io/docs/concepts/resources)
