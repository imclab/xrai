# Claude Code + Unity MCP Development Workflow

**Created**: 2026-01-16
**Purpose**: Document proven workflow patterns for rapid Unity development with Claude Code, Unity MCP, and JetBrains Rider MCP

---

## Overview

This workflow achieves **5-10x faster iteration** compared to traditional Unity development by:
1. Eliminating context-switching between tools
2. Immediate error detection via MCP
3. Knowledgebase-augmented pattern recognition
4. Structured project documentation

---

## Core Workflow: MCP-First Development

```
┌─────────────────────────────────────────────────────────────┐
│  1. Read file(s) with context                               │
│  2. Make targeted edit (Edit tool)                          │
│  3. mcp__UnityMCP__refresh_unity(compile: "request")        │
│  4. mcp__UnityMCP__read_console(types: ["error"])           │
│  5. If errors → fix and repeat from step 2                  │
│  6. mcp__UnityMCP__validate_script() for confirmation       │
└─────────────────────────────────────────────────────────────┘
```

### Timing Comparison

| Action | Traditional | MCP-First |
|--------|-------------|-----------|
| Detect compilation error | 30-120s (wait for Unity) | <5s |
| Verify fix worked | 30-120s (Unity recompile) | <5s |
| Find file in project | 10-30s (manual search) | <2s |
| Understand type definition | 30-60s (navigate code) | <5s |

---

## Essential MCP Tools

### Unity MCP (Primary)

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `read_console` | Get Unity console messages | After EVERY edit |
| `validate_script` | Check script compiles | Confirm fix worked |
| `refresh_unity` | Force recompilation | After file changes |
| `find_gameobjects` | Locate scene objects | Scene queries |
| `manage_components` | Add/modify components | Runtime setup |
| `manage_gameobject` | Create/modify GameObjects | Scene building |
| `manage_scene` | Scene operations | Load/save scenes |

### JetBrains Rider MCP (Secondary)

| Tool | Purpose | When to Use |
|------|---------|-------------|
| `search_in_files_by_text` | Fast text search | Find usages across project |
| `get_symbol_info` | Type definitions | Understand APIs |
| `get_file_problems` | Roslyn diagnostics | Catch errors Unity misses |
| `rename_refactoring` | Safe rename | Refactoring |
| `find_files_by_name_keyword` | Find files | Locate by partial name |

---

## Project Setup Requirements

### 1. CLAUDE.md (Critical)

Every Unity project needs a `CLAUDE.md` with:

```markdown
# Project Name

## Quick Reference
- Unity Version: X.X.X
- Target Platform: iOS/Android/etc
- Render Pipeline: URP/HDRP/Built-in

## Key Files
| File | Purpose |
|------|---------|
| path/to/MainScript.cs | Description |
| path/to/Manager.cs | Description |

## Build Commands
./build.sh

## Architecture
[Diagram or description]

## Common Issues
[Known problems and fixes]
```

### 2. Unity MCP Connection

Ensure Unity MCP server is running:
- Package: `com.coplaydev.unity-mcp`
- Default port: 6400
- Check: `mcp__UnityMCP__manage_editor(action: "telemetry_ping")`

### 3. Knowledgebase Access

Symlink knowledgebase for cross-session memory:
```bash
ln -sf ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase ~/.claude/knowledgebase
```

---

## Code Pattern Library

### Input System Compatibility

```csharp
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

[SerializeField] private Key toggleKey = Key.Tab;

void Update()
{
    if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
    {
        DoAction();
    }
}
#else
[SerializeField] private KeyCode toggleKey = KeyCode.Tab;

void Update()
{
    if (Input.GetKeyDown(toggleKey))
    {
        DoAction();
    }
}
#endif
```

### Editor Persistence for Runtime Objects

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

void CreatePersistentObject(string name)
{
    var obj = new GameObject(name);

    #if UNITY_EDITOR
    if (!Application.isPlaying)
    {
        Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
        EditorUtility.SetDirty(gameObject);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    #endif
}
```

### Read-Only Property with Setter Method

```csharp
// Private backing field
[SerializeField] private MyType _value;

// Read-only property (external code)
public MyType Value => _value;

// Controlled setter with side effects
public void SetValue(MyType newValue)
{
    _value = newValue;
    // Configure related fields
    OnValueChanged();
}
```

### Verbose Logging Control

```csharp
[Header("Debug")]
[Tooltip("Enable verbose logging (disable to reduce console spam)")]
public bool verboseLogging = false;

// One-time log tracking
private bool _loggedInit;

void Start()
{
    if (verboseLogging && !_loggedInit)
    {
        Debug.Log($"[{GetType().Name}] Initialized");
        _loggedInit = true;
    }
}

void Update()
{
    // Periodic logging (not every frame)
    if (verboseLogging && Time.frameCount % 60 == 0)
    {
        Debug.Log($"[{GetType().Name}] State: {currentState}");
    }
}
```

### UI Toolkit Programmatic Creation

```csharp
private VisualElement CreatePanelProgrammatically()
{
    var container = new VisualElement();
    container.style.position = Position.Absolute;
    container.style.top = 10;
    container.style.left = 10;
    container.style.backgroundColor = new Color(0, 0, 0, 0.8f);
    container.style.paddingTop = 10;
    container.style.paddingBottom = 10;
    container.style.paddingLeft = 15;
    container.style.paddingRight = 15;
    container.style.borderTopLeftRadius = 8;
    // ... more styling

    var title = new Label("Panel Title");
    title.style.fontSize = 16;
    title.style.color = Color.white;
    title.style.unityFontStyleAndWeight = FontStyle.Bold;
    container.Add(title);

    return container;
}
```

---

## Debugging Workflow

### Step 1: Check Console First
```
mcp__UnityMCP__read_console(count: 10, types: ["error", "warning"])
```

### Step 2: Validate Specific Script
```
mcp__UnityMCP__validate_script(uri: "Assets/Scripts/MyScript.cs", level: "standard")
```

### Step 3: Force Refresh if Needed
```
mcp__UnityMCP__refresh_unity(scope: "scripts", compile: "request", wait_for_ready: true)
```

### Step 4: Check Again
```
mcp__UnityMCP__read_console(count: 5, types: ["error"])
```

---

## Best Practices

### Do

1. **Check console after EVERY edit** - Catch errors immediately
2. **Read files before editing** - Understand context
3. **Make small, targeted edits** - One change per verify cycle
4. **Document patterns in KB** - Future sessions benefit
5. **Use CLAUDE.md** - Reduces context-gathering reads
6. **Validate before proceeding** - Don't assume success

### Don't

1. **Don't make multiple changes without verifying** - Harder to debug
2. **Don't skip console checks** - Errors compound
3. **Don't ignore warnings** - They often become errors
4. **Don't guess file paths** - Use Glob/search tools
5. **Don't trust "it should work"** - Verify with MCP

---

## Troubleshooting

### Unity MCP Not Responding

1. Check Unity is running
2. Check MCP package installed: `com.coplaydev.unity-mcp`
3. Try: `mcp__UnityMCP__manage_editor(action: "telemetry_ping")`
4. Restart Unity if needed

### Compilation Errors After Edit

1. Read the full error message carefully
2. Check the specific line number
3. Read surrounding code for context
4. Fix and verify with `validate_script`

### Script Changes Not Detected

1. Force refresh: `refresh_unity(scope: "scripts", compile: "request")`
2. Check for syntax errors blocking compilation
3. Verify file saved correctly

---

## Related Documentation

- `LEARNING_LOG.md` - Discovery journal with specific examples
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR/VFX patterns
- `MetavidoVFX-main/CLAUDE.md` - Project-specific patterns
- `QUICK_REFERENCE.md` - VFX properties cheat sheet

---

**Last Updated**: 2026-01-16
**Category**: workflow|claude-code|unity-mcp|rider|development
