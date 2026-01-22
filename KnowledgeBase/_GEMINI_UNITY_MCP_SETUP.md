# Gemini Unity MCP Setup Guide

**Version**: 1.0
**Last Updated**: 2026-01-21
**Project**: Unity-XR-AI (Portals V6)

## Overview
This document specifies the setup and optimization patterns for the Gemini CLI using the Model Context Protocol (MCP) to interact with the Unity Editor.

## Configuration (.ai/mcp/mcp.json)
Gemini uses `uvx` to dynamically run the Unity MCP server and persistent memory.

```json
{
  "mcpServers": {
    "UnityMCP": {
      "command": "uvx",
      "args": [
        "--from",
        "git+https://github.com/CoplayDev/unity-mcp@v9.0.1#subdirectory=Server",
        "mcp-for-unity"
      ]
    },
    "claude-mem": {
      "command": "uvx",
      "args": [
        "chroma-mcp",
        "--client-type",
        "persistent",
        "--data-dir",
        "/Users/jamestunick/.claude-mem/chroma"
      ]
    }
  }
}
```

## Core Unity MCP Commands for Gemini

| Command | Purpose |
|---------|---------|
| `read_console` | **MANDATORY** after every code change to check for errors. |
| `manage_scene` | Navigate scenes and check hierarchy. |
| `find_gameobjects` | Locate objects by name/tag (returns lightweight IDs). |
| `manage_gameobject` | Detailed inspection/modification of objects using IDs. |
| `manage_vfx` | Tune VFX Graph and Particle Systems. |
| `batch_execute` | Combine multiple calls to save tokens and time. |

## Optimization Patterns

### 1. Token-Efficient Scene Inspection
Don't fetch the whole hierarchy. Use `find_gameobjects` first.
```bash
# Correct Pattern
find_gameobjects(search_term="MainCamera")
# Then fetch specific data using the returned ID
manage_gameobject(action="get_components", target="<id>", search_method="by_id")
```

### 2. Rapid Debug Loop
```
1. Edit Code (replace/write_file)
2. refresh_unity(mode="if_dirty")
3. read_console(types=["error"], count=5)
```

### 3. VFX Tuning
Use `batch_execute` for simultaneous property updates.
```javascript
batch_execute([
  { tool: "manage_vfx", params: { action: "vfx_set_float", target: "Fire", parameter: "Size", value: 2.0 } },
  { tool: "manage_vfx", params: { action: "vfx_set_color", target: "Fire", parameter: "Color", value: [1, 0, 0, 1] } }
])
```

> [!WARNING]
> **Batch Tool Scoping**: `batch_execute` only supports tools provided by the **UnityMCP server**. It **cannot** execute project-level tools like `read_file`, `write_file`, or `run_shell_command`. To read C# scripts within a batch, use `manage_script(action="read")`.

## Rider + Unity Integration Tuning

### 1. Tool Selection Hierarchy
When Rider is active, Gemini should prioritize JetBrains MCP tools over shell tools for 10x faster indexed searching.

| Action | Priority 1 (JetBrains) | Priority 2 (Shell) |
|--------|-----------------------|-------------------|
| Search Code | `search_in_files_by_text` | `grep` / `rg` |
| Find File | `find_files_by_name_keyword` | `glob` |
| Read Code | `get_file_text_by_path` | `read_file` |

### 2. The "Unity Clean" Verification Loop
After every edit in Gemini:
1. `refresh_unity(mode="if_dirty")`
2. `read_console(types=["error"], count=5)`
3. If errors found: `get_symbol_info` via JetBrains to find definition.

### 3. Token-Efficient Commands
- **Grep**: Use `--max-count 10` to prevent token flooding.
- **Unity**: Use `include_properties=false` for `manage_gameobject` until a specific object is targeted.
- **Batch**: Group up to 25 commands in `batch_execute`.

### 3. VFX Tuning
Use `batch_execute` for simultaneous property updates.
```javascript
batch_execute([
  { tool: "manage_vfx", params: { action: "vfx_set_float", target: "Fire", parameter: "Size", value: 2.0 } },
  { tool: "manage_vfx", params: { action: "vfx_set_color", target: "Fire", parameter: "Color", value: [1, 0, 0, 1] } }
])
```

## Troubleshooting
- **Connection Refused**: Ensure Unity Editor is open and `MCP for Unity` server is started (`Window > MCP for Unity > Start Server`).
- **Duplicate Servers**: Run `mcp-kill-dupes` if response times exceed 10s.
- **Port Conflict**: Unity MCP default port is 6400. Check with `lsof -i :6400`.

## Memory & Rules Integration
- All Gemini sessions read `GLOBAL_RULES.md` and `GEMINI.md`.
- Learnings are logged to `KnowledgeBase/LEARNING_LOG.md`.
