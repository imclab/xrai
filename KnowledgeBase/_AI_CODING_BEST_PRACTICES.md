# AI Coding Best Practices (2025-2026)

**Tags**: `ai` `development` `claude-code` `cursor` `windsurf` `unity` `productivity`
**Updated**: 2026-01-21
**Sources**: Research synthesis from 20+ studies and production workflows

---

## Rigorous Research Studies (RCTs with Statistical Significance)

### METR RCT Study (July 2025) - [arXiv:2507.09089](https://arxiv.org/abs/2507.09089)
**Sample**: 16 experienced OSS developers, 246 tasks, mature repos (avg 22K+ stars, 1M+ LOC)
**Method**: Randomized controlled trial, real issues on their own repos
**Finding**: AI usage increased task completion time by **19%** (p-value significant)
**Perception Gap**: Devs expected 24% faster, believed 20% faster even after slowdown
**Contributing Factors**:
- <44% AI suggestion acceptance rate
- Large/mature codebases (avg 10 years old)
- Undocumented tacit knowledge AI couldn't access
- Review/test/edit time for rejected suggestions

### Microsoft/Accenture Multi-Company RCT (2024) - [SSRN:4945566](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=4945566)
**Sample**: 4,867 developers across Microsoft, Accenture, Fortune 100
**Method**: Randomized controlled trial, production environments
**Finding**: **26.08%** increase in completed tasks (SE: 10.3%)
**Distribution**:
- Less experienced devs: **35-39%** speedup
- Experienced devs: **8-16%** speedup
- No drop in code quality or increase in errors

### Google Internal RCT (2024)
**Sample**: ~100 Google engineers
**Method**: RCT with enterprise-grade coding assignment
**Finding**: AI group completed **21% faster** (96 min vs 114 min)

### GitHub Copilot HTTP Server Study (2023) - [arXiv:2302.06590](https://arxiv.org/abs/2302.06590)
**Sample**: 95 professional developers
**Method**: RCT, JavaScript HTTP server implementation
**Finding**: **55.8% faster** task completion with Copilot
**Who Benefits Most**: Less experience, older programmers, high coding hours/day

### Microsoft 3-Week Diary Study (2024-2025)
**Sample**: 200+ engineers
**Finding**: 11 weeks to fully realize productivity gains
**Challenge**: Validation time cancels out gains initially

### Key Takeaway: Experience Level Determines ROI
| Experience Level | Typical Speedup | Notes |
|------------------|-----------------|-------|
| Junior (0-2 years) | 35-55% | Highest gains |
| Mid (2-5 years) | 20-30% | Strong gains |
| Senior (5-10 years) | 8-16% | Moderate gains |
| Expert (10+ years, own repos) | **-19%** | Often slower |

**The Paradox Explained**:
1. Experts have tacit knowledge AI can't access
2. Acceptance rate drops in complex/mature codebases
3. Review overhead exceeds generation benefits
4. Context-switching between thinking modes

**Solution**: Match tool to task type, not all tasks (see matrix below)

---

## Tool Selection Matrix

| Use Case | Best Tool | Why |
|----------|-----------|-----|
| Large codebase refactoring | Claude Code | 200K context, understands full repo |
| Multi-file autonomous work | Cursor Composer | Best agent workflows |
| Unity Editor automation | Unity MCP | Direct editor control |
| New developer onboarding | Windsurf | Smoothest learning curve |
| Terminal/DevOps automation | Claude Code CLI | Terminal-native |
| Large context refactors | Gemini CLI | Best context handling |
| Daily IDE coding | Cursor or Windsurf | Fast iterations |
| Enterprise/compliance | GitHub Copilot | Best enterprise controls |

---

## Claude Code Patterns (What Works)

### Be Explicit
```
BAD:  "Fix the bugs"
GOOD: "Fix the NullReferenceException in VFXARBinder.cs line 45"
```

### Request Plans First
```
"Before making changes, propose a structured plan for:
1. Adding hand tracking to the brush system
2. Show which files need modification
3. Explain the approach"
```

### Constrain Scope
```
"Only modify HandVFXController.cs - don't touch other files"
```

### Iterate Incrementally
```
"Let's add velocity tracking first, test it, then add pinch detection"
```

### Review Diffs
Always review changes before accepting. Claude Code shows diffs - use them.

---

## Unity + AI Workflow (MCP Deep Dive)

### Unity MCP Repos Comparison

| Repo | Stars | Tools | Key Differentiator |
|------|-------|-------|-------------------|
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | Active | 24 | batch_execute (10-100x faster), Roslyn validation |
| [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) | Active | 31 | Multi-client support, Package Manager integration |
| [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | Active | 50+ | **Runtime AI** (in-game), dynamic Roslyn compilation |

### Most Useful MCP Tools for Rapid Development

**Scene & Hierarchy (Fast Prototyping)**
```
manage_scene          → Load, create, save scenes
manage_gameobject     → CRUD GameObjects via natural language
find_gameobjects      → Query by name/tag/component
manage_components     → Attach/remove components
manage_prefabs        → Prefab creation and manipulation
```

**Debugging (Critical)**
```
read_console          → AI reads errors/warnings/logs
validate_script       → Roslyn strict type-checking
run_tests            → Execute EditMode/PlayMode tests
get_test_job         → Monitor test status
```

**Batch Operations (10-100x Faster)**
```csharp
// Instead of 10 individual calls:
batch_execute({
  operations: [
    {tool: "manage_gameobject", action: "create", name: "Player"},
    {tool: "manage_components", action: "add", component: "Rigidbody"},
    // ... 8 more operations
  ]
})
```

### Debugging Workflow with MCP

**1. Error Detection**
```
AI: "Check Unity console for errors"
→ read_console(type: "Error", count: 10)
→ Returns: NullReferenceException at VFXARBinder.cs:45
```

**2. Code Validation Before Compile**
```
AI: "Validate this script"
→ validate_script(path: "Assets/Scripts/VFXARBinder.cs", strict: true)
→ Returns: Missing namespace, undefined method errors
```

**3. Test-Driven Fixes**
```
AI: "Run tests to verify fix"
→ run_tests(mode: "EditMode", filter: "VFXBinderTests")
→ AI reads results, iterates if failures
```

### IvanMurzak Unity-MCP: Runtime AI (Unique Feature)

Unlike editor-only solutions, this enables **in-game AI**:
- NPCs call methods dynamically at runtime
- AI agents discover game state via reflection
- Dynamic code compilation without editor restart

**Architecture**:
```
MCP Client (Claude) → MCP Server → Unity Runtime Plugin
                                  ↓
                            Game executes AI decisions
```

### Best Integration Pattern
```
1. Claude Code (CLI) - Codebase changes, refactoring
2. Unity MCP - Editor automation, scene setup, debugging
3. Rider AI - Code completion, shader support
4. IvanMurzak MCP - Runtime AI, dynamic testing
5. MCP Bridge - Connects all
```

### Rider 2025 AI Features
- Free AI features (unlimited code completion)
- Local model support
- Claude 3.7 Sonnet, Gemini 2.0 support
- Multi-line shader completion (Unity, Unreal, Godot)

### Rapid Debug Loop (Recommended)

```
1. Error appears in Unity Console
   ↓
2. MCP: read_console() → AI sees error
   ↓
3. MCP: find_in_file() → AI locates source
   ↓
4. Claude Code: Edit file with fix
   ↓
5. MCP: refresh_unity() → Force recompile
   ↓
6. MCP: read_console() → Verify fix
   ↓
7. MCP: run_tests() → Regression check
```

**Time Savings**: ~30-60% faster than manual debugging

---

## Production Code Patterns (From Unity MCP Repos)

### McpToolBase Pattern (Sync/Async Execution)

```csharp
// Base class pattern from CoderGamester/mcp-unity
public abstract class McpToolBase
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public virtual bool IsAsync => false;  // Override for long-running ops

    // Synchronous (default) - for quick operations
    public virtual JObject Execute(JObject parameters)
    {
        throw new NotImplementedException("Override Execute for sync tools");
    }

    // Asynchronous - for long-running operations (scene creation, tests)
    public virtual void ExecuteAsync(JObject parameters, TaskCompletionSource<JObject> tcs)
    {
        throw new NotImplementedException("Override ExecuteAsync for async tools");
    }
}
```

**When to Use**:
- `IsAsync=false`: Quick queries, property gets, simple creates
- `IsAsync=true`: Scene operations, test runs, batch operations

### Response Format (JSON-RPC 2.0)

```csharp
// Success response
JObject CreateSuccessResponse(string requestId, JObject data)
{
    return new JObject
    {
        ["id"] = requestId,
        ["result"] = data
    };
}

// Error response with typed error codes
JObject CreateErrorResponse(string requestId, string errorType, string message)
{
    return new JObject
    {
        ["id"] = requestId,
        ["error"] = new JObject
        {
            ["type"] = errorType,      // "validation_error", "not_found_error", etc.
            ["message"] = message
        }
    };
}

// Standard error types:
// - invalid_json          : Malformed JSON
// - invalid_request       : Missing required field
// - unknown_method        : Tool not registered
// - validation_error      : Parameter validation failed
// - not_found_error       : GameObject/Component not found
// - tool_execution_error  : Runtime failure
// - internal_error        : Server exception
```

### Component Update with Reflection

```csharp
// Pattern for dynamically setting any component property
public void UpdateComponentProperty(Component component, string fieldName, JToken value)
{
    var componentType = component.GetType();

    // Try field first
    var fieldInfo = componentType.GetField(fieldName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    if (fieldInfo != null)
    {
        Undo.RecordObject(component, $"Set {fieldName}");
        fieldInfo.SetValue(component, ConvertJTokenToValue(value, fieldInfo.FieldType));
        return;
    }

    // Fall back to property
    var propInfo = componentType.GetProperty(fieldName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    if (propInfo != null && propInfo.CanWrite)
    {
        Undo.RecordObject(component, $"Set {fieldName}");
        propInfo.SetValue(component, ConvertJTokenToValue(value, propInfo.PropertyType));
    }
}

// Type conversion for Unity types
object ConvertJTokenToValue(JToken token, Type targetType)
{
    if (targetType == typeof(Vector3))
        return new Vector3(token["x"].Value<float>(), token["y"].Value<float>(), token["z"].Value<float>());

    if (targetType == typeof(Color))
        return new Color(
            token["r"]?.Value<float>() ?? 0,
            token["g"]?.Value<float>() ?? 0,
            token["b"]?.Value<float>() ?? 0,
            token["a"]?.Value<float>() ?? 1
        );

    if (targetType == typeof(Quaternion))
        return new Quaternion(
            token["x"].Value<float>(),
            token["y"].Value<float>(),
            token["z"].Value<float>(),
            token["w"]?.Value<float>() ?? 1
        );

    if (targetType.IsEnum)
        return Enum.Parse(targetType, token.ToString());

    return token.ToObject(targetType);
}
```

### Console Log Service (Thread-Safe Pattern)

```csharp
// Thread-safe log capture service
public class ConsoleLogsService
{
    private readonly List<LogEntry> _logEntries = new();
    private const int MaxEntries = 1000;
    private const int CleanupThreshold = 200;

    public ConsoleLogsService()
    {
        // Register for threaded callback
        Application.logMessageReceivedThreaded += OnLogMessageReceived;
    }

    private void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        lock (_logEntries)
        {
            _logEntries.Add(new LogEntry
            {
                Message = message,
                StackTrace = stackTrace,
                Type = type,
                Timestamp = DateTime.Now
            });

            // Auto-cleanup oldest entries
            if (_logEntries.Count > MaxEntries)
                _logEntries.RemoveRange(0, CleanupThreshold);
        }
    }

    // Paginated, filtered retrieval
    public JObject GetLogsAsJson(int offset, int limit, string[] types, bool includeStackTrace)
    {
        lock (_logEntries)
        {
            var filtered = _logEntries
                .Where(e => types == null || types.Contains(MapLogType(e.Type)))
                .Skip(offset)
                .Take(limit)
                .Select(e => new JObject
                {
                    ["message"] = e.Message,
                    ["type"] = MapLogType(e.Type),
                    ["timestamp"] = e.Timestamp.ToString("o"),
                    ["stackTrace"] = includeStackTrace ? e.StackTrace : null
                });

            return new JObject
            {
                ["logs"] = new JArray(filtered),
                ["total"] = _logEntries.Count,
                ["offset"] = offset,
                ["limit"] = limit
            };
        }
    }

    // MCP types map to multiple Unity types
    private static readonly Dictionary<string, LogType[]> LogTypeMapping = new()
    {
        ["error"] = new[] { LogType.Error, LogType.Exception, LogType.Assert },
        ["warning"] = new[] { LogType.Warning },
        ["log"] = new[] { LogType.Log }
    };
}
```

### Undo Integration (Critical for Editor Tools)

```csharp
// Pattern 1: Before modifying existing objects
Undo.RecordObject(gameObject, "Modify GameObject");
gameObject.name = newName;
gameObject.transform.position = newPosition;

// Pattern 2: After creating new objects
var newObj = new GameObject("Created by MCP");
Undo.RegisterCreatedObjectUndo(newObj, "Create GameObject");

// Pattern 3: Component operations
var component = gameObject.AddComponent<Rigidbody>();
Undo.RegisterCreatedObjectUndo(component, "Add Rigidbody");

// Pattern 4: Grouping multiple undos
Undo.SetCurrentGroupName("Batch Scene Setup");
int undoGroup = Undo.GetCurrentGroup();
// ... multiple operations ...
Undo.CollapseUndoOperations(undoGroup);
```

### Main Thread Dispatcher (Async Unity API)

```csharp
// IvanMurzak pattern for calling Unity API from async contexts
public class MainThread : MonoBehaviour
{
    private static MainThread _instance;
    private readonly Queue<Action> _actions = new();

    public static MainThread Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MainThreadDispatcher");
                _instance = go.AddComponent<MainThread>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public T Run<T>(Func<T> action)
    {
        if (IsMainThread())
            return action();

        T result = default;
        var waitHandle = new ManualResetEvent(false);

        lock (_actions)
        {
            _actions.Enqueue(() =>
            {
                result = action();
                waitHandle.Set();
            });
        }

        waitHandle.WaitOne();
        return result;
    }

    private void Update()
    {
        lock (_actions)
        {
            while (_actions.Count > 0)
                _actions.Dequeue()?.Invoke();
        }
    }

    private static bool IsMainThread() =>
        Thread.CurrentThread.ManagedThreadId == 1;
}

// Usage in MCP tool
[McpPluginTool("get_player_position")]
public static Vector3 GetPlayerPosition()
{
    return MainThread.Instance.Run(() =>
        GameObject.Find("Player").transform.position
    );
}
```

### Parameter Validation Pattern

```csharp
// Multi-layer validation with early returns
public JObject Execute(JObject parameters)
{
    // Layer 1: Required parameters
    if (!parameters.ContainsKey("sceneName"))
        return CreateErrorResponse("validation_error", "Required parameter 'sceneName' not provided");

    string sceneName = parameters["sceneName"].ToString();

    // Layer 2: Format validation
    if (string.IsNullOrWhiteSpace(sceneName))
        return CreateErrorResponse("validation_error", "Scene name cannot be empty");

    // Layer 3: Resource existence
    var scene = EditorBuildSettings.scenes.FirstOrDefault(s => s.path.Contains(sceneName));
    if (scene == null)
        return CreateErrorResponse("not_found_error", $"Scene '{sceneName}' not found in build settings");

    // Layer 4: State validation
    if (EditorApplication.isPlaying)
        return CreateErrorResponse("state_error", "Cannot modify scenes while in Play mode");

    try
    {
        // Execute operation
        EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
        return CreateSuccessResponse(new JObject { ["loaded"] = scene.path });
    }
    catch (Exception ex)
    {
        return CreateErrorResponse("tool_execution_error", ex.Message);
    }
}
```

---

## Multi-Model Workflow (Production Pattern)

```
Planning/Architecture → Claude Opus 4.5
  ↓
Implementation → Claude Code or Cursor
  ↓
Large Refactors → Gemini CLI (best context)
  ↓
Quick Fixes → GitHub Copilot (in-IDE)
  ↓
Unity Automation → Unity MCP
  ↓
Review/Polish → Claude Code
```

### Why Multi-Model?
Each model has strengths:
- **Claude**: Best overall quality, planning, documentation
- **Gemini**: Best large context, refactors
- **GPT/Codex**: Deterministic multi-step execution
- **Copilot**: Fast in-editor completion

---

## Anti-Patterns (What Fails)

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| Full rewrite requests | AI drifts, loses context | Incremental changes |
| No context provided | Generic/wrong solutions | Include relevant files |
| Accepting without review | Subtle bugs introduced | Always review diffs |
| Using one tool for everything | Suboptimal for many tasks | Match tool to task |
| Over-relying on AI | Slower than manual for experts | Know when to type directly |

---

## Transparency & Clarity Practices

### 1. Explicit Context
```markdown
## Context
- Project: MetavidoVFX (Unity AR)
- File: VFXARBinder.cs
- Issue: ExposedProperty not updating VFX
- Constraint: Must work on iOS Quest 3
```

### 2. Structured Requests
```markdown
## Request
1. Find where property binding fails
2. Propose fix with code
3. Explain why original failed
4. Test steps
```

### 3. Document Decisions
```markdown
## Decision: Use ExposedProperty over string
- Why: Compile-time validation
- Tradeoff: Slightly more verbose
- Tested: Works on iOS, Quest 3
- Date: 2026-01-21
```

### 4. Log Learnings
Every discovery → LEARNING_LOG.md
```markdown
## 2026-01-21: ExposedProperty Pattern
- Error: VFX property not updating
- Fix: Use ExposedProperty instead of const string
- Files: VFXARBinder.cs, HandVFXController.cs
- Tags: vfx, unity, binding
```

---

## Speed Optimization

### Tool Selection by Domain (Token-Optimized)

| Domain | Tool | Tokens | Speed |
|--------|------|--------|-------|
| **KB search** | grep + _QUICK_FIX.md | **0** | Instant |
| **C# code** | JetBrains MCP | ~200 | 10x faster than grep |
| **Unity Editor** | Unity MCP | ~300 | batch_execute = 10-100x |
| **Semantic** | ChromaDB (claude-mem) | ~500 | Fuzzy/conceptual |
| **Web** | WebSearch | ~1000+ | Last resort |

### Search Priority (Exhaust Zero-Token First)
1. **_QUICK_FIX.md** - Error code → Solution (0 tokens)
2. **_PATTERN_TAGS.md** - Topic → File (0 tokens)
3. **grep KB** - Keyword search (0 tokens)
4. **JetBrains MCP** - Code search (~200 tokens, Rider indexed)
5. **ChromaDB** - Semantic fallback (~500 tokens)
6. **Web Search** - Only if KB has no answer (~1000+ tokens)

### Reduce AI Tokens
- Pre-computed indexes (shell scripts)
- LaunchAgents for maintenance
- Git hooks for auto-logging
- Pattern files with standard format

### Reduce Round Trips
- Include all relevant context upfront
- Batch related requests
- Use plans to avoid rework

---

## Proven GitHub Repos (High Stars, Production Use)

### AI Coding Infrastructure (Fastest Growing 2025)
| Repo | Stars | Category | Why It Works |
|------|-------|----------|--------------|
| [vllm-project/vllm](https://github.com/vllm-project/vllm) | 50K+ | Model Runtime | 24x faster inference |
| [langgenius/dify](https://github.com/langgenius/dify) | 80K+ | Agent Platform | Production-ready workflows |
| [infiniflow/ragflow](https://github.com/infiniflow/ragflow) | 70K+ | RAG | Deep document understanding |
| [getcursor/cursor](https://github.com/getcursor/cursor) | 50K+ | AI Editor | Best agent mode |
| [anthropics/claude-code](https://github.com/anthropics/claude-code) | Growing | CLI | 200K context, full repo understanding |

### Unity AI Development
| Repo | Tools | Use Case |
|------|-------|----------|
| [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) | 24 | Editor automation, batch ops |
| [CoderGamester/mcp-unity](https://github.com/CoderGamester/mcp-unity) | 31 | Multi-client, Package Manager |
| [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) | 50+ | Runtime AI, reflection |
| [METR/Measuring-Early-2025-AI-on-Exp-OSS-Devs](https://github.com/METR/Measuring-Early-2025-AI-on-Exp-OSS-Devs) | - | RCT study methodology |

### Spec-Driven Development
| Repo | Stars | Why Relevant |
|------|-------|--------------|
| [github/spec-kit](https://github.com/github/spec-kit) | 50K+ | Spec → Code generation |

---

## Research Sources (Peer-Reviewed & RCTs)

### Academic Papers
- [METR RCT Study (2025)](https://arxiv.org/abs/2507.09089) - 19% slowdown for experts
- [Microsoft/Accenture RCT (2024)](https://papers.ssrn.com/sol3/papers.cfm?abstract_id=4945566) - 26% productivity gain
- [GitHub Copilot Study (2023)](https://arxiv.org/abs/2302.06590) - 55.8% faster task completion
- [IBM AI Code Assistant (2024)](https://arxiv.org/html/2412.06603v2) - Enterprise use patterns

### Industry Reports
- [GitHub Octoverse 2025](https://github.blog/news-insights/octoverse/) - 36M new devs, 1B commits
- [Google Cloud/Harris Poll](https://cloud.google.com/) - 90% game devs using AI
- [JetBrains Rider 2025.1](https://blog.jetbrains.com/dotnet/2025/04/16/rider-2025-1-release/) - Free AI features

### Tool Comparisons
- [AI Coding Assistants 2025](https://usama.codes/blog/ai-coding-assistants-2025-comparison)
- [Best AI Code Editor 2026](https://research.aimultiple.com/ai-code-editor/)
- [Claude Code vs Cursor](https://www.qodo.ai/blog/claude-code-vs-cursor/)
- [Claude Code vs Codex vs Gemini](https://www.educative.io/blog/claude-code-vs-codex-vs-gemini-code-assist)
- [AI Coding Agents 2026](https://www.faros.ai/blog/best-ai-coding-agents-2026)

### Unity + AI
- [Unity MCP: CoplayDev](https://github.com/CoplayDev/unity-mcp)
- [Unity MCP: CoderGamester](https://github.com/CoderGamester/mcp-unity)
- [Unity MCP: IvanMurzak](https://github.com/IvanMurzak/Unity-MCP)
- [Unity + AI 2025 Discussion](https://discussions.unity.com/t/unity-ai-coding-tools-current-state-june-2025/1664497)
- [Infosys: Vibe Coding Unity MCP](https://blogs.infosys.com/emerging-technology-solutions/artificial-intelligence/the-digital-alchemist-vibe-coding-with-unity-mcp-and-claude-ai-to-craft-3d-immersive-xr-experiences.html)

---

**See Also**: `_SIMPLIFIED_INTELLIGENCE_CORE.md`, `_PATTERN_ARCHITECTURE.md`, `_QUICK_FIX.md`
