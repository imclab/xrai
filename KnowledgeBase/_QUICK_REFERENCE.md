# Quick Reference - Common Patterns

## Activation Phrases

| Say This | Activates |
|----------|-----------|
| **"Using Unity Intelligence patterns"** | 500+ Unity repo patterns (ARFoundation, VFX, DOTS, networking) |

## Unity Debugging

```bash
# MCP Console Check
read_console(types=["error"])

# Nuclear clean
killall -9 Unity && rm -rf ~/Library/Developer/Xcode/DerivedData
```

## Unity + React Native

```bash
# Fast iOS build (skip Unity export)
./scripts/build_and_run_ios.sh --skip-unity-export

# Full rebuild
rm -rf ios/Pods ios/build && pod install && xcodebuild
```

## Device Logs

```bash
# iOS device logs
idevicesyslog -u <UDID> | grep -E "(Unity|Error|Exception)"

# Check device connected
xcrun devicectl list devices | grep connected
```

## Git Workflow

```bash
# Quick commit
git add -A && git commit -m "$(date +%Y-%m-%d): description"

# Stash and pull
git stash && git pull && git stash pop
```

## MCP Unity Tools

| Tool | Use |
|------|-----|
| `read_console` | Check errors |
| `manage_scene` | Scene operations |
| `find_gameobjects` | Search hierarchy |
| `validate_script` | Check C# syntax |
| `recompile_scripts` | Force recompile |

## ARFoundation Quick Setup

```csharp
// Required components
AROcclusionManager    // Depth
ARHumanBodyManager    // Body tracking
ARCameraManager       // Camera feed
```

## VFX Graph Data Flow

```
ARFoundation → Texture2D → VFX Graph Property → Particle Position
```

## Performance Targets

| Platform | FPS | Particles |
|----------|-----|-----------|
| Quest 2 | 72-90 | <50K |
| iOS | 60 | <100K |
| Desktop | 60+ | <1M |

## Common Errors

| Error | Fix |
|-------|-----|
| `UnityFramework not found` | Rebuild Unity export |
| `Pod install failed` | Delete Pods/, reinstall |
| `MCP not responding` | Restart Unity MCP server |
| `NullReferenceException` | Check component initialization order |

## Token-Efficient Prompts

```
"Check Unity console for errors" → read_console
"Find Player object" → find_gameobjects(name="Player")
"Scene hierarchy" → manage_scene(action="get_hierarchy")
```

---

*Load full references from knowledgebase files as needed*
