# AntiGravity Global Rules - Unity XR Development

**Version**: 1.4 (AntiGravity-Optimized)
**Last Updated**: 2026-01-21
**Project**: portals_v4 (Unity 6000.2.14f1, AR Foundation 6.2.1, React Native 0.73.2)

---

## 🔁 Synchronization

This file must stay **in sync** with `/Users/jamestunick/GLOBAL_RULES.md`. If there is any conflict, update this file to match the global rules first.

Keep docs/rules/memory aligned across Codex, Claude Code, and Gemini.
Codex must follow the **Rider + Claude Code + Unity (PRIMARY WORKFLOW)** as the third workflow.
Codex also follows the Cross-Tool Integration and Unity MCP Optimization guidance in this file.

---

## Cross-Tool Integration

### Shared Resources (Symlinked)
```
~/GLOBAL_RULES.md              ← This file (Single Source of Truth)
├── ~/.claude/CLAUDE.md        ← Claude Code specific
├── ~/.windsurf/CLAUDE.md      → ~/CLAUDE.md
├── ~/.cursor/CLAUDE.md        → ~/CLAUDE.md
└── project/CLAUDE.md          ← Project overrides

Knowledgebase (all tools access):
~/.claude/knowledgebase/       → Unity-XR-AI/KnowledgeBase/
~/.windsurf/knowledgebase/     → Unity-XR-AI/KnowledgeBase/
~/.cursor/knowledgebase/       → Unity-XR-AI/KnowledgeBase/
```

### MCP Consistency (No Conflicts)
All IDE tools use same Unity MCP config (v9.0.1):
- Claude Code: `~/.claude/settings.json`
- Windsurf: `~/.windsurf/mcp.json`
- Cursor: `~/.cursor/mcp.json`

### Multi-AI Orchestration (Research-Backed)

### When AI Helps vs Hurts (RCT Evidence)
| Developer Level | AI Impact | Strategy |
|-----------------|-----------|----------|
| Junior (0-2 yrs) | +35-55% faster | Use AI extensively |
| Mid (2-5 yrs) | +20-30% faster | AI for boilerplate, manual for logic |
| Senior (5-10 yrs) | +8-16% faster | AI for exploration, manual for familiar code |
| Expert (10+ yrs, own repos) | **-19% slower** | AI for new domains only |

**Source**: METR RCT (arXiv:2507.09089), Microsoft/Accenture RCT (SSRN:4945566)

### Why Experts Slow Down
1. <44% AI suggestion acceptance rate in mature codebases
2. Tacit knowledge AI can't access
3. Review/edit overhead exceeds generation benefits
4. Context-switching between thinking modes

### Tool Selection Matrix
| Tool | Best For | Context | Cost |
|------|----------|---------|------|
| Claude Code | Implementation, complex code | 200K | $$$ |
| Gemini CLI | Research, large docs | 1M | FREE |
| Codex CLI | Quick fixes, automation | 128K | $$ |

### Rollover When Token Limits Reached
When Claude Code hits limits, switch to Gemini or Codex:
```bash
# Quick switch (knowledge base available in all tools)
gemini    # FREE, 1M context
codex     # 128K context

# Both read same files: GLOBAL_RULES.md, CLAUDE.md, KnowledgeBase/
```

**Rollover Context Block** (paste in new tool):
```
Read these for context:
1. ~/GLOBAL_RULES.md - Universal rules
2. ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md - Project overview
3. ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md - Error fixes
```

**Full guide**: `KnowledgeBase/_CROSS_TOOL_ROLLOVER_GUIDE.md`

### Integration Map
See: `KnowledgeBase/_TOOL_INTEGRATION_MAP.md`

---

### Prefer JetBrains MCP Tools (MANDATORY when Rider open)
**ALWAYS use JetBrains MCP over raw tools** - indexed searches are 5-10x faster.

| Instead Of | Use This | Speed |
|------------|----------|-------|
| Grep | `search_in_files_by_text` | 10x |
| Grep (regex) | `search_in_files_by_regex` | 10x |
| Glob | `find_files_by_name_keyword` | 5x |
| Read | `get_file_text_by_path` | 2x |
| LSP | `get_symbol_info` | Native |

**Optimal Parameters**:
```
search_in_files_by_text:
  searchText: "query"
  maxUsageCount: 50          # Cap results (default unlimited)
  caseSensitive: false       # Faster when false
  fileMask: "*.cs"           # Narrow scope
  timeout: 10000             # 10s max

find_files_by_name_keyword:
  nameKeyword: "Manager"
  fileCountLimit: 25         # Cap results
  timeout: 5000

get_file_text_by_path:
  pathInProject: "Assets/Scripts/File.cs"
  maxLinesCount: 500         # Limit large files
  truncateMode: "MIDDLE"     # Keep start+end

get_symbol_info:
  filePath: "path/to/file.cs"
  line: 42                   # 1-based
  column: 15                 # 1-based
```

**Enforcement**: If Rider is open, NEVER use raw Grep/Glob/Read for project files.

#

## Unity MCP Optimization (Token-Efficient)

**Token Savings Matrix**:
| Default | Optimized | Savings |
|---------|-----------|---------|
| Individual calls | `batch_execute` | **10-100x** |
| `include_properties=true` | `=false` | **5-10x** |
| `generate_preview=true` | `=false` | **10x+** |
| Full console | `types=["error"]` | **3-5x** |
| Polling tests | `wait_timeout=60` | **10x fewer calls** |

**Resources vs Tools** (CRITICAL):
- **Resources** = Read-only, use for state checks: `editor_state`, `project_info`, `gameobject/{id}`
- **Tools** = Mutations: `manage_*`, `script_apply_edits`, `batch_execute`

**Console Checking**:
```
read_console:
  count: 5                   # Small batch
  types: ["error"]           # Filter to errors only (skip log/warning)
  include_stacktrace: false  # Skip unless debugging
```

**Scene Hierarchy** (can be huge):
```
manage_scene(action="get_hierarchy"):
  page_size: 50              # Start small
  max_depth: 3               # Limit nesting
  max_nodes: 100             # Cap total
  cursor: <next_cursor>      # Paginate, don't fetch all
```

**Asset Search**:
```
manage_asset(action="search"):
  page_size: 25              # Keep small
  generate_preview: false    # CRITICAL - previews add huge base64
```

**GameObject Components**:
```
manage_gameobject(action="get_components"):
  include_properties: false  # Metadata only first
  page_size: 10              # Small pages
```

**Test Running** (async pattern):
```
1. run_tests(mode="EditMode") → returns job_id
2. get_test_job(job_id, wait_timeout=30, include_details=false)
3. Only include_failed_tests=true if failures found
```

**Script Validation** (before full read):
```
validate_script(uri, level="basic")  # Quick syntax check
get_sha(uri)                          # Metadata without content
```

**Batch Operations** (10-100x savings for repetitive tasks):
```
batch_execute(commands=[
  {"tool": "manage_gameobject", "params": {...}},
  {"tool": "manage_gameobject", "params": {...}},
  ...
], parallel=true)
# One call instead of 5-25 separate calls
```

**Find GameObjects** (use instance IDs):
```
find_gameobjects(search_term="Player", search_method="by_name", page_size=10)
# Returns IDs only - lightweight
# Then fetch specific: manage_gameobject(target=<id>, search_method="by_id")
```

**Script Editing** (prefer structured over raw):
```
script_apply_edits > apply_text_edits > manage_script
# Structured edits = smaller payloads, safer
```

**VFX Management** (action prefixes):
```
particle_* → ParticleSystem
vfx_*      → VisualEffect (VFX Graph)
line_*     → LineRenderer
trail_*    → TrailRenderer

# Common: vfx_set_float, vfx_send_event, vfx_play/stop
manage_vfx({action: "vfx_set_float", target: "HologramVFX", parameter: "Intensity", value: 2.5})
```

**Script Validation** (Roslyn if enabled):
```
validate_script(uri, level="standard")  # Catches errors pre-compile
get_sha(uri)                            # Metadata without content
# Enable Roslyn: Add USE_ROSLYN to Scripting Define Symbols
```

**Editor State Check** (before batch ops):
```
# Read resource: mcpforunity://editor/state
# Verify: advice.ready_for_tools === true
# Skip if just completed successful operation
```

**Refresh Unity** (don't over-refresh):
```
refresh_unity(mode="if_dirty", scope="scripts", wait_for_ready=true)
# Only refresh what changed, wait once
# DON'T: refresh after every edit
```

**Editor State** (check before operations):
```
manage_editor(action="telemetry_status")
# Quick ping - is Unity responsive?
# Skip if you just did an operation successfully
```

**Caching Strategies**:
- Remember instance IDs from find_gameobjects (don't re-search)
- Remember asset paths from searches (don't re-search)
- Skip re-reading files you just edited
- Trust edit success (don't verify unless error)

**Avoid Redundant Operations**:
- Don't check console after every micro-edit
- Don't refresh after read-only operations
- Don't fetch hierarchy to find one object (use find_gameobjects)
- Don't get_components when you only need transform

### Context Management
- `/compact` - Mid-task when context grows
- `/clear` - Fresh start for new tasks

---

## Debugging & Iteration Protocols

### State-of-the-Art Debugging
- **Isolate**: Binary search - Unity vs React Native vs Bridge vs Deployment
- **Verbose Fallback**: If automated script fails, run raw command with `--verbose`
- **Automated Verification**: Use `verify_device_logs.sh`, never ask user to read device screens
- **Auto Test & Continue**: Test programmatically after deploy, iterate until confirmed working

### Multi-Layer Verbose Logging
Four-channel output: (1) Unity Console, (2) NSLog/Native, (3) File Log, (4) On-Screen Overlay
**Reference**: `knowledgebase/_UNITY_RN_DEBUG_PATTERNS.md` for implementation

### MCP-First Verification Loop
Before ANY device build:
1. `read_console(types=["error"])` - No compilation errors
2. `manage_scene(action="get_hierarchy")` - Scene structure correct
3. Custom validation menu items
**30-second check vs 15-minute rebuild** - Always validate first.

### Auto-Fix Console Errors (PROACTIVE + TOKEN-EFFICIENT)
**Check and fix Unity console errors automatically** - don't wait for user to ask.

**Efficient Pattern**:
```
1. read_console(types=["error"], count=5)     # Errors only, small batch
2. If errors: read ONLY the specific file mentioned
3. Fix with Edit (not Write)
4. read_console(types=["error"], count=5)     # Verify fix
5. Stop when clean (don't over-check)
```

**Common fixes** (don't research, just apply):
- Missing `using` → Add namespace import
- Missing reference → Check asmdef dependencies
- Type mismatch → Check API changes
- Null reference → Add null checks

**DON'T**: Read multiple files speculatively, fetch full stacktraces unless stuck

### Unity Editor Play Mode Testing (BEFORE BUILD)
**Always test changes in Unity Editor Play Mode before iOS build.**
1. Open Unity Editor if not running
2. Enter Play Mode to test changes
3. Verify behavior works as expected
4. Check console for errors
5. THEN proceed to build & deploy

### Iteration Speed
- **Skip Unchanged**: Use `--skip-unity-export`, `--skip-pod-install` for 90% time savings
- **Framework Persistence**: `UnityFramework.framework` is ephemeral in DerivedData - verify exists
- **Live Reload First**: Validate in RN (fast refresh) or Unity (Play Mode) before full build

### Persistent Failures (Nuclear Option)
- **Kill**: `killall -9 Unity Hub xcodebuild java`
- **Purge**: `rm -rf ~/Library/Developer/Xcode/DerivedData ios/build ios/Pods android/build`
- **Reboot**: If stuck >30 mins

### Best Practices
- **Single Source of Truth**: Fix pipelines, not manual copy-paste
- **Adhere to Standards**: Use CocoaPods/Unity Build Player, not custom bypass scripts
- **Reproducibility**: If you can't script it, don't do it

### Code Quality Principles (ALWAYS APPLY)
All features must be:
- **Fast** - Performance-first design, minimize allocations, profile hot paths
- **Modular** - Single responsibility, clear interfaces, dependency injection
- **Simple** - Obvious code > clever code, minimal abstractions
- **Scalable** - O(1) or O(log n) algorithms, pagination, pooling
- **Extensible** - Open/closed principle, strategy pattern, plugin architecture
- **Future-proof** - Interfaces over concrete types, version tolerance
- **Debuggable** - Clear logging, debug flags, inspector exposure
- **Maintainable** - Consistent naming, documented edge cases, test coverage

### Research Strategy
- **Verify Sources**: Official docs, trusted repos, expert forums
- **No Assumptions**: "I verified in 2025 Unity Manual" required
- **Triple Check**: Validate against current constraints before coding

---

## Spec-Driven Development & Task Management

### Always Use Todo Lists
- Start every multi-step task with TodoWrite
- One active task at a time, mark done immediately
- Add discovered tasks as they emerge

### Spec-Kit Workflow (github.com/github/spec-kit)
**Reference**: `knowledgebase/_SPEC_KIT_METHODOLOGY.md`

**Core Philosophy**: Specs are shared context between humans and AI agents. They prevent scope creep, enable parallel work, and create audit trails.

**Workflow Stages**:
```
Constitution → Specify → Clarify → Plan → Tasks → Implement
```

**Commands** (Slash Commands or Tool Calls):
- `/speckit.specify <feature>` - Generate spec.md from description
- `/speckit.plan` - Create implementation plan from spec
- `/speckit.tasks` - Generate tasks.md with checkboxes
- `/speckit.implement` - Execute tasks with auto-checkin

**Spec Structure** (spec.md):
```markdown
# Spec: Feature Name (ID: spec-XXX)
## Overview - One paragraph
## Goals - Bullet points
## Non-Goals - Explicit exclusions
## Technical Approach - Architecture decisions
## Interfaces - Public APIs, data contracts
## Dependencies - External requirements
## Risks - Known challenges
## Success Criteria - Measurable outcomes
```

**Tasks Structure** (tasks.md):
```markdown
# Tasks: Feature Name
## Phase 1: Foundation
- [ ] Task 1 - Create X
- [ ] Task 2 - Wire Y
## Phase 2: Integration
- [x] Task 3 - Connect Z (completed)
```

**When to Use Specs**:
- New features (>100 LOC)
- Architecture changes
- Multi-file refactors
- Cross-team handoffs
- Anything reviewable

**When to Skip Specs**: Bug fixes, <50 LOC changes, config tweaks, urgent hotfixes

---

## Metacognitive Intelligence & Persistent Learning

### Persistent Long-Term Memory
Every successful pattern MUST be encoded in:
- `GLOBAL_RULES.md` (cross-project)
- Project `CLAUDE.md` (project-specific)
- `LEARNING_LOG.md` (discoveries)

### Self-Improvement Loop
Execute → Observe → Extract pattern → Encode → Verify persistence

### Resource-Aware Thinking
- MORE tokens: multi-system debugging, repeated failures, high rebuild costs
- FEWER tokens: isolated issues, obvious fixes
- Goal: OUTCOME QUALITY, not token minimization

### Self-Audit Triggers
- After 3 failed attempts: step back, audit approach
- After successful complex fix: extract and document pattern
- When context compacted: verify key learnings survived

---

## Automatic Pattern Extraction

Before session ends, automatically check for:
- Solved non-obvious problem
- Found reusable pattern (2+ projects)
- Discovered anti-pattern
- Saved significant time

If YES → Log to `LEARNING_LOG.md` and create MCP memory entity
**Reference**: `knowledgebase/_PATTERN_EXTRACTION_PROTOCOL.md`

---

## File Safety (CRITICAL)

### Never Delete Without Permission
- **NEVER delete files** unless user explicitly says "delete" or "remove"
- Moving, renaming, deprecating = OK
- Deleting = ONLY with explicit instruction
- When in doubt, ask first

---

## Process & Agent Coordination

### Process Awareness
- Never kill processes without verification
- Check ownership: `ps aux | grep <process>`
- Respect user background tasks

### MCP Server Management
- **On session start**: Run `mcp-kill-dupes && unity-mcp-cleanup` to clean up duplicates and stale Unity instances
- MCP servers spawn per-app (Claude Code, Claude Desktop, VS Code) - duplicates waste ~1.5GB RAM
- Aliases: `mcp-ps` (running), `mcp-count`, `mcp-mem`, `mcp-kill-dupes`, `mcp-kill-all`
- **Unity MCP cleanup**: `unity-mcp-cleanup` removes stale `~/.unity-mcp/` status files for closed projects

### Agent Parallelism
| Safe | Independent reads, non-overlapping writes |
| Caution | Same-domain operations |
| Avoid | Resource contention, shared state mutation |

### Cross-Tool Awareness
- Shared: `DerivedData`, `node_modules`, `Pods`, Unity `Library/`
- Respect lock files regardless of which tool created them
- One tool to Unity MCP at a time
- No parallel git operations

### Device Availability
- Check before operations: `xcrun devicectl list devices` / `adb devices`
- If unavailable: inform user, do useful work instead of blocking
- iOS needs unlock for launching/debugging

---

## Cross-Tool Memory Architecture

**Principle**: Files are memory. Knowledgebase IS your AI memory.

| Pattern Type | Store In |
|--------------|----------|
| Universal rules | `GLOBAL_RULES.md` |
| Discoveries | `LEARNING_LOG.md` |
| Tool-specific | `~/.{tool}/*.md` |
| Structured facts | MCP Memory entities |
| Project-specific | `project/CLAUDE.md` |

**Reference**: `knowledgebase/_AI_MEMORY_SYSTEMS_DEEP_DIVE.md`

---

## Unity MCP Server (Claude Code)

**Quick Fixes**:
1. **Not Responding**: Unity `Window > MCP for Unity > Start Server`
2. **Config Mismatch**: Restart Claude Code CLI session
3. **Transport**: Use `stdio` (default), not HTTP

After Unity Build: MCP stops during headless builds. Restart Unity Editor to reconnect.

---

## Unity Intelligence Patterns (REQUIRED)

**For ALL Unity projects**: Say **"Using Unity Intelligence patterns"** to activate 500+ repository patterns.

**Covered Domains**:
- ARFoundation depth/body tracking → VFX Graph
- Million-particle optimization (DOTS, Burst, Jobs)
- Particle brush systems (Open Brush patterns)
- Hand tracking + gesture recognition (Barracuda)
- Audio reactive VFX (FFT analysis)
- Networking (Normcore, Netcode for GameObjects)
- Platform optimization (Quest, iOS, visionOS)

**Reference**: `knowledgebase/_UNITY_INTELLIGENCE_PATTERNS.md`, `_UNITY_PATTERNS_BY_INTEREST.md`

---

## WebGL Intelligence Patterns (REQUIRED)

**For ALL WebGL/Three.js projects**: Say **"Using WebGL Intelligence patterns"** to activate web 3D patterns.

**Covered Domains**:
- WebGPU compute shaders (million particles)
- Three.js core (InstancedMesh, BufferGeometry, GLSL)
- React Three Fiber (R3F) + Drei helpers
- WebXR (VR/AR in browser)
- Gaussian splatting (LumaSplatsThree)
- Performance (LOD, pooling, disposal)

**Reference**: `knowledgebase/_WEBGL_INTELLIGENCE_PATTERNS.md`

---

## 3DVis Intelligence Patterns (REQUIRED)

**For ALL data visualization projects**: Say **"Using 3DVis Intelligence patterns"** to activate visualization algorithms.

**Covered Domains**:
- Sorting algorithms (Z-sort, radix, bitonic)
- Clustering (K-means, DBSCAN, hierarchical)
- Pattern recognition (shape descriptors, template matching)
- Anomaly detection (isolation forest, statistical outliers)
- Force-directed graph layouts (Barnes-Hut)
- Spatial data structures (Octree, KD-Tree)
- Semantic query parsing

**Reference**: `knowledgebase/_3DVIS_INTELLIGENCE_PATTERNS.md`

---

## Token Efficiency (MANDATORY)

**Goal**: Stay below 95% of weekly/session limits. Prioritize outcome quality with minimal token expenditure.
**Reference**: `KnowledgeBase/_TOKEN_EFFICIENCY_COMPLETE.md` for full details.

### Session Management
- Start fresh session vs compress (saves 10-50K tokens)
- `/compact <focus>` when context >100K (include focus instruction)
- `/clear` between distinct tasks (not mid-task)
- `/cost` or `/stats` to check usage
- Target <100K tokens per session (50% utilization)
- Checklists for migrations/bulk changes (systematic, not one massive prompt)

### Context Commands (Official Best Practices)
| Command | Purpose |
|---------|---------|
| `/cost` | Check token usage (API users) |
| `/stats` | Check usage (Max/Pro subscribers) |
| `/clear` | Start fresh for unrelated work |
| `/compact <focus>` | Shrink context with specific focus |
| `/context` | See MCP server overhead |
| `/mcp` | Disable unused servers mid-session |
| `/model` | Switch to cheaper model (Sonnet) |
| `/rewind` | Restore to previous checkpoint |
| `Shift+Tab` | Enter plan mode |
| `Escape` | Stop current direction |
| `Double-Escape` | Restore conversation + code |

### Environment Variables (settings.json)
```json
{
  "env": {
    "ENABLE_TOOL_SEARCH": "auto:5",    // Dynamic tool loading at 5% context
    "MAX_THINKING_TOKENS": "10000",    // Reduce from default 31999
    "CLAUDE_CODE_MAX_OUTPUT_TOKENS": "16384"
  }
}
```

### Hook Preprocessing (High Impact)
Use PreToolUse hooks to filter verbose output BEFORE Claude sees it:
- Test output filtering (show failures only)
- Log truncation
- Build output summarization
**Result**: 50-80% token savings on CI/test operations

### Planning Before Implementation
- Ask Claude to explore and plan BEFORE coding
- Prevents wasted tokens on false starts
- "Steps #1-#2 are crucial—without them, Claude jumps straight to coding"

### Pre-configured Permissions
- Set allowed tools in `.claude/settings.json`
- Avoid repeated permission dialogs
- Use stored slash commands for reusable operations

### .claudeignore (CRITICAL for Unity)
- Excludes Library/, Temp/, Logs/, Builds/ → 95% token reduction (190K → 10K)
- Verify exists before Unity work

### Agent Usage (Independent Token Budgets)
- Agents don't count toward main session budget
- Use Explore (haiku) for fast searches
- Use research-agent for KB research
- Rule: 3+ step tasks → use agent
- Up to 10 concurrent subagents (additional queue)
- Resume subagents instead of restarting (preserves context)
- Background agents: "run in background" or Ctrl+B (no MCP access)

### Subagent Patterns (Optimal: 3-4 specialized)
| Pattern | Token Cost | Best For |
|---------|-----------|----------|
| Single-threaded | 1x | Simple tasks |
| 3-4 sequential agents | 2-3x | Reviews with file handoffs |
| 10+ parallel agents | 4-5x | Independent exploration |

### Thinking Budget Triggers
| Phrase | Reasoning Depth | Cost |
|--------|-----------------|------|
| "think" | Baseline | 1x |
| "think hard" | Increased | 2x |
| "think harder" | Deep analysis | 3x |
| "ultrathink" | Maximum | 4x+ |

**Default**: `MAX_THINKING_TOKENS=10000` (vs 31,999)

**Smart Mode Auto-Escalation** (thinking + quality):
- Spec/architecture tasks → Auto "think hard" + high quality
- 2+ failed attempts → Auto "think harder" + extra validation
- Complex debugging → Auto increased reasoning
- Simple edits → Baseline thinking, low overhead

**Token-Free Quality Wins** (from Anthropic official + automation):
- Use `/clear` between distinct tasks (saves 10-50K tokens)
- Explore→Plan→Code→Commit workflow (prevents rework)
- Subagent verification instead of re-reading (parallel, no main context)
- CLAUDE.md ≤200 lines (move details to per-folder files)
- Screenshots: `ss` or `ssu` (automated, then Ctrl+V to paste)
- KB first: `kbfix "error"` before any MCP/agent call (0 tokens)

### Model Selection & Visibility
- Haiku: Simple agents, checks (0.3x cost)
- Sonnet: 95% of tasks (1x cost)
- Opus: Complex architecture only (3-5x cost)
- Use `/model` to switch mid-session

**Show Current Model**: Run `/model` or check first response
**Current Session**: Claude Opus 4.5 (claude-opus-4-5-20251101)

**When to Use Each**:
| Task | Model | Why |
|------|-------|-----|
| Quick fixes, simple edits | Haiku | Fast, cheap |
| Standard coding, debugging | Sonnet | Best value |
| Architecture, complex refactors | Opus | Highest quality |
| Research, large context | Gemini | FREE, 1M context |

### Visual Communication
- One image = 500-2000 words saved
- Use for: errors, UI, visual bugs, design refs

### Tool Usage
- Skip reads for files already seen this session
- Use `head_limit` on Grep/Glob to cap results
- Prefer JetBrains MCP tools (indexed) over raw searches
- Use `haiku` model for simple Task agents
- Batch independent tool calls in parallel
- Use Task/Explore agent for open-ended searches (single call vs many)
- Avoid re-reading files to "verify" edits
- Use `offset` param to paginate large results
- Skip Unity console checks unless actively debugging
- Minimal TodoWrite usage - only for 3+ step tasks
- One Grep pattern with regex OR (`a|b|c`) vs multiple searches
- Use `glob` filter on Grep to narrow file scope
- Skip LSP lookups when file context is sufficient
- Avoid WebSearch/WebFetch unless explicitly needed
- Don't fetch full PR/issue details when title suffices

### Responses
- No code blocks unless requested
- Bullets over paragraphs
- Skip preambles ("let me", "I'll", "sure")
- Omit explanations unless asked
- No emoji
- No summaries of what was just done
- Single-line answers when possible
- Skip file path repetition if obvious from context
- No rhetorical questions
- No "feel free to ask" closers
- Truncate long lists with "..." if pattern is clear
- Reference line numbers, don't quote code back
- Say "done" not "I have successfully completed..."

### Planning
- Ask clarifying questions upfront to avoid rework
- No speculative exploration
- Direct action over research when path is clear
- Skip insight blocks unless high educational value
- Assume reasonable defaults, don't ask obvious questions
- One plan, not multiple options
- Skip background/context if user knows the codebase

### Memory (claude-mem)
- Query sparingly, targeted searches only
- Skip saves for routine/trivial tasks
- Combine related memories into single save
- Don't query memory for tasks with full context provided
- Don't save what's already in project docs

### Code
- Edit over Write (smaller diffs)
- Reuse existing patterns/utilities in codebase
- Minimal whitespace changes

**Smart Mode** (DEFAULT - auto-adjusts quality by context):

| Context | Quality Level | Behavior |
|---------|---------------|----------|
| Spec creation | **High** | Full docstrings, types, defensive checks |
| Public APIs | **High** | Complete documentation, type annotations |
| Persistent issues (3+ attempts) | **High** | Extra validation, verbose logging |
| Architecture/refactors | **High** | Design comments, interface docs |
| Complex algorithms | **High** | Step-by-step comments, edge case handling |
| Bug fixes | **Medium** | Fix + minimal guard, brief comment |
| Simple edits | **Low** | Minimal overhead, trust existing code |
| Exploratory/prototyping | **Low** | Fast iteration, skip formalities |
| Debugging/logging | **Low** | Quick checks, temporary code OK |

**Auto-Triggers for High Quality**:
- File contains `spec`, `interface`, `abstract`, `public class`
- Task mentions "architecture", "design", "spec", "API"
- Previous attempt failed (escalate quality)
- Creating new files (vs editing existing)
- User says "production", "ship", "release"

**Manual Overrides**:
- "quality mode" → Force high quality everything
- "speed mode" → Force minimal overhead
- "hybrid mode" → Explicit smart mode (same as default)

### Git/GitHub
- Short commit messages (one line when possible)
- Skip PR body unless required
- Don't read git history unless needed
- Assume main branch unless told otherwise

### What NOT To Do
- Announce tool usage before calling
- Repeat user's question back
- Provide multiple options when one is clearly best
- Add safety caveats for routine operations
- Explain obvious code
- Warn about "potential issues" speculatively
- Suggest tests unless asked
- Offer to do more after completing task
- List files about to read/edit
- Describe reasoning unless asked

### Session Management
- Reuse context from earlier in conversation
- Don't re-summarize previous work
- Trust that user remembers recent exchanges
- End turns promptly when task complete

### Platform Build Optimization (Quick Reference)

**Xcode/iOS**: Build Active Arch=YES, Compilation Cache (24-77% faster)
**Android**: Gradle cache, IL2CPP "Faster builds", Quick Preview
**WebGL**: LTO, Brotli, Strip Engine Code (7MB→2MB)
**Quest**: Runtime Optimizer, Foveation, max 1K draw calls
**Unity Play Mode**: Disable Domain/Scene Reload (60% faster)

### AI CLI Tools (Quick Reference)

**Claude CLI**: `-p`, `--output-format json`, `/clear`, `/compact`
**Gemini CLI**: FREE 60 req/min, 1M context, Google Search grounding
**Codex CLI**: Compaction (multi-context), 75% cache discount, 94% fewer tokens
**VS Code**: Codeium (free/fast), Tabnine (local), llama.vscode (local LLM)

### Prompt Engineering (Quick Reference)

- Concise prompts: 30-50% token reduction
- RAG + Vector DB: Up to 70% context reduction
- XML tags for Claude: `<task>`, `<context>`
- Multi-AI: Claude (complex) + Gemini (research/free) + Codex (refactors)

See `KnowledgeBase/_TOKEN_EFFICIENCY_COMPLETE.md` for full details.

---

## Performance Monitoring & Self-Healing (ALWAYS ACTIVE)

**Principle**: Never slow down Mac, Rider, Unity, Claude Code, or other tools.

### System Health Thresholds

| Resource | OK | Warning | Critical | Action |
|----------|-----|---------|----------|--------|
| CPU | <70% | 70-90% | >90% | Kill background processes |
| Memory | <80% | 80-95% | >95% | `purge`, close apps |
| MCP Response | <5s | 5-15s | >30s | `mcp-kill-dupes` |
| Token Usage | <80K | 80-150K | >150K | `/compact` |
| Unity FPS | >45 | 30-45 | <30 | Reduce complexity |

### Quick Health Check (Run Periodically)
```bash
top -l 1 | head -3              # CPU/Memory
lsof -i :6400,63342 | wc -l     # MCP connections
```

### Self-Healing Triggers

| Issue | Detection | Auto-Fix |
|-------|-----------|----------|
| MCP timeout | No response 30s | `mcp-kill-dupes` |
| Memory pressure | >90% | `purge` |
| Duplicate servers | >2 connections | Kill duplicates |
| Broken symlink | File read fails | Recreate |
| Token overflow | >180K | Force compact |

### Proactive Bottleneck Prevention

1. **Session Start**: Run `mcp-kill-dupes && unity-mcp-cleanup`
2. **Mid-Session**: Check `/cost` before large operations
3. **Before Builds**: Verify Unity console clean
4. **After Errors**: Log to FAILURE_LOG.md, create pattern

**Reference**: `KnowledgeBase/_SELF_HEALING_SYSTEM.md`

---

## Intelligence Systems (CONTINUOUS LEARNING)

**Core Pattern**: Extract → Log → Compound → Accelerate

### Active Systems

| System | File | Purpose |
|--------|------|---------|
| Continuous Learning | `_CONTINUOUS_LEARNING_SYSTEM.md` | Pattern extraction |
| Self-Healing | `_SELF_HEALING_SYSTEM.md` | Auto-recovery |
| Auto-Fixes | `_AUTO_FIX_PATTERNS.md` | Common fix library |
| Intelligence Index | `_INTELLIGENCE_SYSTEMS_INDEX.md` | Central reference |

### Logging Triggers

| Event | Log To | Action |
|-------|--------|--------|
| Failure (3+ attempts) | FAILURE_LOG.md | Create prevention |
| Success (first try) | SUCCESS_LOG.md | Replicate pattern |
| Bad pattern found | ANTI_PATTERNS.md | Document avoidance |
| New fix discovered | _AUTO_FIX_PATTERNS.md | Add to library |

### Agents for Intelligence

| Agent | Trigger | Purpose |
|-------|---------|---------|
| insight-extractor | After significant work | Extract patterns |
| system-improver | After discovery | Apply to configs |
| health-monitor | Periodically | Check system health |

### Spec-Kit Methodology

Apply to ALL improvements:
1. **Define Spec**: Context, success criteria, constraints
2. **Create Tasks**: Checkboxes, phases
3. **Research**: Existing solutions, trade-offs
4. **Validate**: Checklists, verification

**Reference**: `specs/README.md`, `KnowledgeBase/_INTELLIGENCE_SYSTEMS_INDEX.md`

---

## Quick Activation Commands

```
"Run health-monitor"              # System health check
"Use insight-extractor"           # Extract patterns from work
"Use system-improver"             # Apply improvements
"Log failure: [description]"      # Track failure
"Log success: [description]"      # Track success
"Search KB for [topic]"           # Find existing solutions
"Apply continuous learning"       # Full learning cycle
```

## 📚 Knowledgebase (530+ Repos - ALWAYS CHECK FIRST)

**Location**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/`

**Quick Access**:
```
"Read ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_KNOWLEDGEBASE_INDEX.md"
"Read ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md"
```

**Essential Files**:
- `_MASTER_KNOWLEDGEBASE_INDEX.md` - Canonical knowledgebase index
- `.claude/KB_MASTER_INDEX.md` - Claude quick index
- `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - 530+ repos
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR/VFX code snippets
- `LEARNING_LOG.md` - Append discoveries here

**Rules**: Search KB before implementing, auto-log learnings, ask before deleting

---

## 🎯 Core Principles

**Verification Protocol (ALWAYS FIRST)**:
1. Check: Local configs, logs, code
2. Research: Official 2026 docs + GitHub repos
3. Verify: Triple-check best practices
4. Implement: Confidently and fast

**Quality**: Principal dev level, 9.7/10 code quality, production-ready

**Testing**: Unity MCP first (30 sec) → Manual fallback (10 min)

---

## 💰 Token Optimization

**MCP Servers**: Currently optimized to 2 servers (20-25K tokens)
- Unity MCP v9.0.1 (latest)
- fetch (web docs)
- **Savings**: ~40-60K tokens vs 8 servers (66-75% reduction)

**Check MCP**: Click "..." → "MCP Servers" in Agent pane

---

## ⚡ Core Workflows

### 1. Primary Workflow: Rider + Claude Code + Unity
**Best For**: Implementation, Debugging, Complex Refactoring (200K Context)
- **Tool Selection**:
  - **Claude Code**: Orchestration, MCP tools, Logic
  - **Rider**: Code completion, Local Search, Refactoring
  - **Unity**: Play Mode, Component Setup
- **MCP Integration**: Full (Unity + JetBrains)

### 2. High-Context Workflow: Rider + Gemini + Unity
**Best For**: Research, Architecture, Large Docs, Rollover (1M+ Context, FREE)
- **Tool Selection**:
  - **Gemini**: "The Brain" - Reasoning, Code Generation, Documentation
  - **Rider**: "The Hands" - Applying code, Formatting, Static Analysis
  - **Unity**: "The World" - Verification, Runtime State
- **No MCP**: Gemini cannot see Editor state or search files directly.

**Gemini Protocol**:
1.  **Context Loading**: Paste/Pipe ALL relevant files (don't cherry-pick, use the 1M window).
    *   *Prompt*: "Context: [Paste Files]. Task: [Description]."
2.  **Generation**: Ask for **Full File** outputs.
    *   *Why*: Easier to copy-paste/replace in Rider than applying diffs manually.
3.  **Implementation**: Copy code → Paste in Rider → `Cmd+Opt+L` (Reformat).
4.  **Verification**: Check Rider for syntax errors → Play Mode in Unity.

**Gemini Fast Workflows**:
- **Architectural Review**: Paste file tree + core scripts → "Analyze dependency graph".
- **Documentation**: Paste entire folder content → "Generate README.md and XML docs".
- **Complex Refactor**: Paste all related files → "Refactor pattern X to Y across all files".

**Gemini Anti-Patterns**:
- ❌ Asking Gemini to "check the console" (It can't).
- ❌ Asking Gemini to "find files" (It can't search the disk).
- ❌ Asking for small diffs (Hard to apply manually; prefer full file).

---

### 3. Tool Selection (Claude Code Only)

| Task | Tool | Params |
|------|------|--------|
| Find files | JetBrains `find_files_by_name_keyword` | fileCountLimit=25 |
| Search code | JetBrains `search_in_files_by_text` | maxUsageCount=50, fileMask="*.cs" |
| Read C# | JetBrains `get_file_text_by_path` | maxLinesCount=300 |
| Edit C# | Claude `Edit` | (not Write) |
| Symbol info | JetBrains `get_symbol_info` | line, column |
| Rename | JetBrains `rename_refactoring` | project-wide |
| Find objects | Unity `find_gameobjects` | page_size=10 |
| Check errors | Unity `read_console` | types=["error"], count=5 |
| Components | Unity `manage_components` | include_properties=false first |

### 4. Fast Workflows (Claude Code Only)

**Fix Error** (3 calls):
1. `read_console(types=["error"], count=3)`
2. `get_file_text_by_path(path, maxLinesCount=100)`
3. `Edit(file, old, new)`

**Implement Feature** (4 calls):
1. `search_in_files_by_text("pattern", fileMask="*.cs", maxUsageCount=5)`
2. `get_file_text_by_path(match)`
3. `Edit(file, old, new)`
4. `read_console(types=["error"], count=3)`

**Debug Runtime** (4 calls):
1. `read_console(types=["error","warning"], count=10)`
2. `find_gameobjects(search_term="Name", page_size=5)`
3. `manage_components(target=id, include_properties=true, page_size=5)`
4. `get_file_text_by_path(script)`

**Rapid Debug Loop** (MCP-powered, 30-60% faster):
```
Error in Console
  ↓
1. read_console(types=["error"], count=5)    → AI sees error
  ↓
2. find_in_file() OR get_file_text_by_path() → Locate source
  ↓
3. Edit(file, old, new)                      → Apply fix
  ↓
4. refresh_unity(mode="if_dirty")            → Recompile
  ↓
5. read_console(types=["error"], count=5)    → Verify fix
  ↓
6. run_tests() (optional)                    → Regression check
```

### 5. Additional Fast Workflows

**Refactor/Rename** (2 calls):
1. `rename_refactoring(path, oldName, newName)` - project-wide
2. `read_console(types=["error"], count=5)`

**Multi-File Edit** (2 calls):
1. `[Edit(f1,...), Edit(f2,...), Edit(f3,...)]` - parallel
2. `read_console(types=["error"], count=10)` - single verify

**VFX Tuning** (1 call):
```
batch_execute([
  {"tool":"manage_vfx","params":{...}},
  {"tool":"manage_vfx","params":{...}}
], parallel=true)
```

**Run Tests** (2 calls):
1. `run_tests(mode="EditMode")` → job_id
2. `get_test_job(job_id, wait_timeout=60)` - waits

### 6. Session Triggers
- `/compact`: Context >100K, switching sub-tasks
- `/clear`: Unrelated task, context >150K
- New session: Context >180K, >2 hours, different project

### 7. Common C# Fixes (Don't Research)
- CS0246 → Add `using`
- CS0103 → Check spelling or add `using`
- CS0029 → Add explicit cast
- NullRef in AR → TryGetTexture pattern

### 8. Anti-Patterns (Never Do)
- ❌ Grep/Read when Rider open (use JetBrains)
- ❌ Write when Edit works
- ❌ Full hierarchy when find_gameobjects suffices
- ❌ Console check after every micro-edit
- ❌ Re-read files just edited
- ❌ Search without fileMask scope
- ❌ include_properties=true on first pass
- ❌ Sequential edit→verify per file (batch instead)
- ❌ Polling test status (use wait_timeout)

---

## 🔧 Unity MCP (Port 6400)

**Essential Commands**:
```python
# Console
read_console(action="get", types=["error", "warning"])

# Scene
manage_scene(action="load", name="SceneName", path="Assets/...")
manage_editor(action="play")

# GameObject
manage_gameobject(action="find", search_term="Player", search_method="by_name")
```

**Connection Issues**: `lsof -i :6400` → restart Unity

---

## 🚨 Error Handling

**Console Checking** (MANDATORY after changes):
```bash
# Via MCP (PREFERRED)
"Check Unity console for errors and warnings"

# Or log file
tail -n 500 ~/Library/Logs/Unity/Editor.log | rg "error CS"
```

**Fix iteratively. Never leave broken code.**

---

## 🔍 Search Tools

**Hierarchy**: Windsurf Fast Context > ugrep > ripgrep > grep > python

**Current**: ripgrep (rg) - 10-50ms
```bash
rg "pattern" -t cs           # Search C# files
rg "error CS\d+" ~/Library/Logs/Unity/Editor.log
rg "TODO|FIXME" -t cs
```

**Optional Upgrade**: `brew install ugrep` (5-40ms, beats ripgrep)

---

## 🎮 Quest/VR Development

```bash
alias quest-logs='adb logcat -v color -s Unity'
alias quest-install='adb install -r'
alias adb-fix='adb kill-server && adb start-server'
```

**Package Compatibility**: Check Package Manager for installed versions, write code for installed versions (not latest)

---

## 📌 Always Remember

1. **Unity MCP first** - Test with manage_editor
2. **Fast iteration** - Ship in <60s or declare "too complex"
3. **Quest 2 performance** - 90 FPS Quest 2, 60 FPS iPhone 12+
4. **Free solutions first** - Avoid paid assets
5. **Prefer existing → open source → low code → custom**
6. **Simple, modular code** - Clean, maintainable, production-ready
7. **Use diffs > full files** - Edit tool, not Write tool
8. **Best practices first** - Unity/platform standards
9. **Match package versions** - Check `Packages/manifest.json`
10. **Check console EVERY change** - Fix errors immediately

**Mandatory**: Quest `adb logcat -s Unity`, iOS `idevicesyslog | grep Unity`

---

## 🔗 Extended Documentation

**Workflow Guides** (accessible via KB):
- `UNITY_RN_INTEGRATION_WORKFLOW.md` - Unity + React Native complete workflow
- `TOKEN_OPTIMIZATION.md` - Token management deep dive
- `AGENT_ORCHESTRATION.md` - Agent usage patterns
- `GIT_COMMIT_BEST_PRACTICES.md` - Commit message standards

**Project Docs**:
- `UNITY_SCENE_ANALYSIS.md` - Scene architecture deep dive
- `DEVICE_TESTING_CHECKLIST.md` - Device testing guide

**Official Docs**:
- Unity XRI 3.1: https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/
- AR Foundation 6.1: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.1/
- URP: https://docs.unity3d.com/Manual/urp/urp-introduction.html
- Meta Quest: https://developers.meta.com/horizon/documentation/unity/
