# Unity MCP Quick Reference

**Tags**: `unity` `mcp` `token-optimization` `debugging`
**Updated**: 2026-01-21
**Purpose**: Token-efficient MCP patterns for rapid Unity development

---

## Token Optimization Matrix

| Default | Optimized | Savings |
|---------|-----------|---------|
| Individual calls | `batch_execute` | **10-100x** |
| `include_properties=true` | `include_properties=false` | **5-10x** |
| `generate_preview=true` | `generate_preview=false` | **10x+** (no base64) |
| Full console | `types=["error"]` | **3-5x** |
| `include_stacktrace=true` | `include_stacktrace=false` | **2-3x** |
| Polling tests | `wait_timeout=60` | **10x** fewer calls |
| Full hierarchy | `page_size=50, max_depth=3` | **5x** |

---

## Most Used Tools (By Frequency)

### 1. Console (Debugging)
```javascript
// OPTIMAL: Errors only, no stacktrace
read_console({
  types: ["error"],
  count: 5,
  include_stacktrace: false
})

// When debugging: Include stacktrace
read_console({
  types: ["error", "warning"],
  count: 10,
  include_stacktrace: true
})
```

### 2. Find GameObjects
```javascript
// Returns IDs only (lightweight)
find_gameobjects({
  search_term: "Player",
  search_method: "by_name",  // by_tag, by_component, by_path, by_id
  page_size: 10
})

// Then fetch specific data
// Use resource: mcpforunity://scene/gameobject/{instance_id}
```

### 3. Batch Execute (CRITICAL)
```javascript
// 10-100x faster than individual calls
batch_execute({
  commands: [
    {tool: "manage_gameobject", params: {action: "create", name: "Cube1"}},
    {tool: "manage_gameobject", params: {action: "create", name: "Cube2"}},
    {tool: "manage_components", params: {action: "add", target: "Cube1", component_type: "Rigidbody"}},
    {tool: "manage_components", params: {action: "add", target: "Cube2", component_type: "Rigidbody"}}
  ],
  parallel: true  // For read-only operations
})
```

### 4. Scene Hierarchy
```javascript
// OPTIMAL: Paginated, limited depth
manage_scene({
  action: "get_hierarchy",
  page_size: 50,
  max_depth: 3,
  max_nodes: 100
})
// Follow cursor for more
```

### 5. Asset Search
```javascript
// OPTIMAL: No previews, paginated
manage_asset({
  action: "search",
  path: "Assets/VFX",
  search_pattern: "*.vfx",
  page_size: 25,
  generate_preview: false  // CRITICAL: Prevents huge base64
})
```

### 6. Script Validation
```javascript
// Check before compile
validate_script({
  uri: "Assets/Scripts/MyScript.cs",
  level: "standard",
  include_diagnostics: true
})
```

### 7. Tests (Async Pattern)
```javascript
// Start test (returns immediately)
run_tests({mode: "EditMode"})  // → job_id

// Wait for completion (reduces polling)
get_test_job({
  job_id: "xxx",
  wait_timeout: 60,  // Waits up to 60s
  include_failed_tests: true,
  include_details: false
})
```

### 8. Refresh Unity
```javascript
// OPTIMAL: Only if dirty, specific scope
refresh_unity({
  mode: "if_dirty",    // Not "force"
  scope: "scripts",    // Not "all"
  wait_for_ready: true
})
```

---

## Gemini Specific Optimization Patterns

*   **Scene Inspection**: `find_gameobjects` (ID-only) → `manage_gameobject(target=ID, search_method="by_id")`.
*   **Rapid Debug Loop**: Edit → `refresh_unity(mode="if_dirty")` → `read_console(types=["error"], count=5)`.
*   **VFX Batching**: Group `manage_vfx` calls in `batch_execute` for simultaneous property updates.
*   **Rider Priority**: Use `search_in_files_by_text` (JetBrains MCP) over `grep` for code search when Rider is active.
*   **Token Efficiency**: Use `--max-count 10` for shell searches to prevent context flooding.

---

## Resources vs Tools

**Use Resources (Read-Only)**:
| Resource | Purpose |
|----------|---------|
| `editor_state` | Check readiness, compilation status |
| `project_info` | Unity version, platform, paths |
| `project_tags` | Available tags |
| `project_layers` | Available layers |
| `editor_selection` | Currently selected objects |
| `get_tests` | Available tests |
| `menu_items` | All menu items |
| `gameobject/{id}` | Full GameObject data |
| `gameobject/{id}/components` | Component data |

**Use Tools (Mutations)**:
| Tool | Purpose |
|------|---------|
| `manage_gameobject` | Create/modify/delete GameObjects |
| `manage_components` | Add/remove/modify components |
| `manage_scene` | Load/save/create scenes |
| `manage_editor` | Play/pause/stop, tags, layers |
| `manage_vfx` | VFX Graph, ParticleSystem, Line/Trail |
| `manage_material` | Create/modify materials |
| `manage_asset` | Asset CRUD operations |
| `script_apply_edits` | Structured C# edits |

---

## VFX Management

### Action Prefixes
| Prefix | Component Required |
|--------|-------------------|
| `particle_*` | ParticleSystem |
| `vfx_*` | VisualEffect (VFX Graph) |
| `line_*` | LineRenderer |
| `trail_*` | TrailRenderer |

### Common VFX Operations
```javascript
// Set VFX Graph parameter
manage_vfx({
  action: "vfx_set_float",
  target: "HologramVFX",
  parameter: "IntensityMultiplier",
  value: 2.5
})

// Send VFX event with attributes
manage_vfx({
  action: "vfx_send_event",
  target: "ParticleEmitter",
  event_name: "OnSpawn",
  position: [0, 1, 0],
  color: [1, 0, 0, 1],
  size: 0.5
})

// Control playback
manage_vfx({action: "vfx_play", target: "MyVFX"})
manage_vfx({action: "vfx_stop", target: "MyVFX"})
```

---

## Rapid Debug Loop (MCP-Powered)

```
1. read_console(types=["error"], count=5)
   ↓
2. find_in_file(uri, pattern)  OR  get_file_text_by_path()
   ↓
3. Edit(file, old, new)  OR  script_apply_edits()
   ↓
4. refresh_unity(mode="if_dirty", scope="scripts")
   ↓
5. read_console(types=["error"], count=5)  → Verify fix
   ↓
6. run_tests() + get_test_job(wait_timeout=60)  → Regression check
```

**Time Savings**: 30-60% faster than manual debugging

---

## Anti-Patterns (Never Do)

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| `generate_preview=true` | Huge base64 blobs | Always false |
| Individual create calls | 100x slower | Use batch_execute |
| `include_stacktrace=true` default | Bloated responses | Only when debugging |
| Polling test status | Many round trips | Use wait_timeout |
| Full hierarchy fetch | Token explosion | Use find_gameobjects |
| `include_properties=true` first | Unnecessary data | Metadata first |
| `refresh_unity(force, all)` | Slow, wasteful | if_dirty, scripts |

---

## Script Editing Patterns

### Prefer Structured Edits
```javascript
// BETTER: Structured method replacement
script_apply_edits({
  name: "VFXARBinder",
  path: "Assets/Scripts/Bridges",
  edits: [{
    op: "replace_method",
    className: "VFXARBinder",
    methodName: "UpdateTexture",
    replacement: "public void UpdateTexture() { /* new impl */ }"
  }]
})

// ALSO GOOD: Anchor-based insert
script_apply_edits({
  name: "VFXARBinder",
  path: "Assets/Scripts/Bridges",
  edits: [{
    op: "anchor_insert",
    anchor: "// INSERT_POINT",
    text: "\n    public void NewMethod() { }\n"
  }]
})
```

### Use apply_text_edits for Precise Edits
```javascript
// When you need exact character positions
apply_text_edits({
  uri: "Assets/Scripts/MyScript.cs",
  edits: [{
    startLine: 45,
    startCol: 1,
    endLine: 50,
    endCol: 1,
    newText: "// Replaced block\n"
  }]
})
```

---

## Editor State Check (Before Operations)

```javascript
// Quick readiness check
// Resource: mcpforunity://editor/state

// Key fields:
{
  "advice": {
    "ready_for_tools": true,  // Safe to proceed
    "blocking_reasons": [],
    "recommended_next_action": "none"
  },
  "compilation": {
    "is_compiling": false,
    "is_domain_reload_pending": false
  }
}
```

**Always check `advice.ready_for_tools` before batch operations.**

---

## Integration with KB

| MCP Result | Log To |
|------------|--------|
| New error pattern | `_QUICK_FIX.md` |
| Useful VFX pattern | `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` |
| Performance insight | `LEARNING_LOG.md` |
| Debug workflow | `_AI_CODING_BEST_PRACTICES.md` |

---

---

## Custom MCP Tools (Extend Your Workflow)

### When to Create Custom Tools
- Repetitive Unity operations (scene setup, prefab creation)
- Project-specific automation (MetavidoVFX patterns)
- Domain logic the AI should invoke (hologram config)

### Implementation Patterns (3 Frameworks)

| Framework | Attribute | Location |
|-----------|-----------|----------|
| CoplayDev | `[McpForUnityTool]` | `Editor/` folder |
| IvanMurzak | `[McpPluginTool]` | Any folder |
| CoderGamester | `McpToolBase` class | `Editor/` folder |

### Quick Example (CoplayDev Pattern)
```csharp
// Assets/Editor/MyCustomTool.cs
[McpForUnityTool("setup_hologram_scene")]
public static class SetupHologramScene
{
    public class Parameters
    {
        [ToolParameter("VFX prefab path")]
        public string vfxPath { get; set; }
    }

    public static object HandleCommand(JObject @params)
    {
        var p = @params.ToObject<Parameters>();

        // Use Undo for editor operations
        Undo.RecordObject(target, "Setup Hologram");

        // Your automation logic
        return new SuccessResponse("Scene ready", new { created = true });
    }
}
```

### Best Practices (From GitHub Repos)
```
1. Undo.RecordObject() before modifying scene/assets
2. Undo.RegisterCreatedObjectUndo() for new objects
3. Single Responsibility - one tool, one task
4. MainThread.Instance.Run() for Unity API in async
5. Return SuccessResponse/ErrorResponse (not exceptions)
6. [ToolParameter] descriptions help AI understand params
```

### Long-Running Operations (Polled)
```csharp
[McpForUnityTool("build_project", RequiresPolling = true, PollAction = "status")]
// Return PendingResponse, AI polls until complete
// Use McpJobStateStore for state persistence across domain reloads
```

---

## Runtime AI (IvanMurzak Unique Feature)

**Only IvanMurzak/Unity-MCP supports this:**
```
LLM → MCP Server → Unity Runtime (not just Editor)
```

### Use Cases
- In-game AI debugging
- Dynamic NPC behavior
- Player-AI interaction in shipped games
- Live code compilation without restart

### Pattern
```csharp
[McpPluginToolType]
public class GameAI
{
    [McpPluginTool("npc_decide_action")]
    public static string DecideAction(string gameState)
    {
        // Called by LLM at runtime
        return MainThread.Instance.Run(() => {
            // Access Unity API on main thread
            return "move_forward";
        });
    }
}
```

---

## Roslyn Validation (Strict C# Checking)

**Enable in Project Settings:**
1. Install NuGetForUnity
2. Add Microsoft.CodeAnalysis v4.14.0
3. Add `USE_ROSLYN` to Scripting Define Symbols
4. Restart Unity

**Benefits:**
- Catches undefined namespaces before compile
- Type checking before domain reload
- Faster error detection

**MCP Usage:**
```javascript
validate_script({
  uri: "Assets/Scripts/MyScript.cs",
  level: "standard",  // Uses Roslyn if enabled
  include_diagnostics: true
})
```

---

## MCP Debugging

### Inspector Tool
```bash
npx @modelcontextprotocol/inspector node Server/build/index.js
```
Real-time MCP traffic debugging.

### Check Editor State First
```javascript
// Before batch operations, verify readiness
// Resource: mcpforunity://editor/state
// Check: advice.ready_for_tools === true
```

---

## See Also

- `GLOBAL_RULES.md` - MCP optimization section
- `_AI_CODING_BEST_PRACTICES.md` - Research-backed workflows
- `_QUICK_FIX.md` - Error → Fix lookup
- `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` - VFX patterns

## Sources

- [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) - 24 tools, batch_execute, Roslyn
- [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) - 31 tools, multi-client
- [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) - 50+ tools, runtime AI
- [notargs/UnityNaturalMCP](https://github.com/notargs/UnityNaturalMCP) - Natural UX
- [mika-f/uMCP](https://github.com/mika-f/umcp) - Security-focused
