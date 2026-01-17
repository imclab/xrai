# Mac Optimization for Unity Development

**Last Updated**: 2026-01-16
**Target**: macOS (Apple Silicon)

---

## Quick Maintenance Commands

```bash
# Clear all dev caches (~60-90GB typically)
rm -rf ~/Library/Caches/JetBrains/Rider*/caches
rm -rf ~/Library/Caches/JetBrains/Rider*/index
rm -rf ~/Library/Developer/Xcode/DerivedData/*
rm -rf ~/Library/Unity/cache/*

# Check disk space recovered
df -h /
```

---

## Cache Locations & Safe Cleanup

| Location | Typical Size | Safe to Delete | Notes |
|----------|--------------|----------------|-------|
| `~/Library/Developer/Xcode/DerivedData/` | 20-50GB | Yes | Rebuilds on next Xcode build |
| `~/Library/Caches/JetBrains/Rider*/caches/` | 10-30GB | Yes | Rebuilds on Rider restart |
| `~/Library/Caches/JetBrains/Rider*/index/` | 5-15GB | Yes | Rebuilds on project open |
| `~/Library/Unity/cache/` | 20-40GB | Yes | Rebuilds on Unity project open |
| `~/Library/Unity/Asset Store-5.x/` | 10-30GB | Caution | Re-downloads if deleted |
| `~/Library/Caches/com.unity3d.*` | 1-5GB | Yes | Unity Hub caches |
| `~/.gradle/caches/` | 5-15GB | Yes | Android build caches |
| `~/.nuget/packages/` | 2-10GB | Caution | NuGet package cache |

---

## Background Services to Disable

### Disable During Development Sessions

```bash
# Adobe Creative Cloud (restarts when apps open)
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.adobe.ccxprocess.plist
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.adobe.GC.Invoker-1.0.plist

# Epic Games Launcher
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.epicgames.launcher.plist

# Google Update (check for Chrome/Drive updates)
launchctl bootout gui/$(id -u) ~/Library/LaunchAgents/com.google.keystone.agent.plist
```

### Re-enable Services

```bash
launchctl bootstrap gui/$(id -u) ~/Library/LaunchAgents/com.adobe.ccxprocess.plist
# (repeat for other services)
```

---

## Spotlight Optimization

### Exclude from Indexing (System Settings → Siri & Spotlight → Spotlight Privacy)

Add these directories:
- `~/Library/Unity`
- `~/Library/Developer`
- `~/Library/Caches/JetBrains`
- All Unity project `Library/` folders
- `node_modules` directories
- `.git` directories (optional)

### Check Indexing Status

```bash
# Check if a path is being indexed
mdutil -s ~/Library/Unity

# Rebuild Spotlight index if corrupted
sudo mdutil -E /
```

---

## Unity Editor Settings

### Preferences (Edit → Preferences)

| Setting | Recommended Value | Why |
|---------|-------------------|-----|
| Auto Refresh | Off during heavy coding | Prevents constant reimports |
| Script Changes While Playing | Recompile After Finished | Avoids mid-play recompiles |
| Compress Assets on Import | Off for dev, On for builds | Faster iteration |
| Parallel Import | On | Uses multiple cores |

### Project Settings

| Setting | Location | Value |
|---------|----------|-------|
| Burst Compilation | Jobs menu → Burst → Enable | Faster builds |
| Incremental GC | Player Settings → Other | On |
| IL2CPP Code Generation | Player Settings | Faster (Smaller) Builds for dev |

---

## Rider/JetBrains Optimization

### Settings (Preferences)

| Setting | Path | Value |
|---------|------|-------|
| Animate Windows | Appearance | Off |
| Recent Files Limit | Editor → General | 15 |
| Power Save Mode | File menu | Toggle during battery |
| Heap Size | Help → Edit Custom VM Options | `-Xmx4096m` for large projects |

### .idea Exclusions

Add to `.gitignore` and Rider exclusions:
```
**/Library/
**/Temp/
**/obj/
**/Logs/
*.csproj
*.sln
```

---

## Memory Management

### Check Current Usage

```bash
# Memory pressure
memory_pressure | head -5

# Top processes by memory
ps aux | sort -nrk 4 | head -10

# Top processes by CPU
ps aux | sort -nrk 3 | head -10
```

### Recommended Limits

| Application | Max Memory | Notes |
|-------------|------------|-------|
| Unity Editor | 16-32GB | Varies by project |
| Rider | 4-8GB | Set via VM options |
| Chrome | Close unused tabs | Each tab ~100-500MB |

---

## Scheduled Maintenance

### Weekly

```bash
# Clear Xcode derived data
rm -rf ~/Library/Developer/Xcode/DerivedData/*

# Clear Rider caches
rm -rf ~/Library/Caches/JetBrains/Rider*/caches
```

### Monthly

```bash
# Full cache clear
rm -rf ~/Library/Unity/cache/*
rm -rf ~/Library/Caches/JetBrains/Rider*/index

# Clear system caches (safe)
rm -rf ~/Library/Caches/com.apple.dt.Xcode/*
```

### Before Major Builds

```bash
# Nuclear option - clear everything
rm -rf ~/Library/Developer/Xcode/DerivedData/*
rm -rf ~/Library/Caches/JetBrains/*
rm -rf ~/Library/Unity/cache/*

# Restart services
killall Rider
killall Unity
```

---

## Automation Script

Save as `~/bin/unity-dev-cleanup.sh`:

```bash
#!/bin/bash
echo "Unity Development Cache Cleanup"
echo "================================"

# Sizes before
echo "Before cleanup:"
du -sh ~/Library/Developer/Xcode/DerivedData 2>/dev/null
du -sh ~/Library/Caches/JetBrains 2>/dev/null
du -sh ~/Library/Unity/cache 2>/dev/null

# Cleanup
rm -rf ~/Library/Developer/Xcode/DerivedData/*
rm -rf ~/Library/Caches/JetBrains/Rider*/caches
rm -rf ~/Library/Caches/JetBrains/Rider*/index
rm -rf ~/Library/Unity/cache/*

echo ""
echo "Cleanup complete!"
df -h / | tail -1 | awk '{print "Disk space: " $4 " free"}'
```

Make executable: `chmod +x ~/bin/unity-dev-cleanup.sh`

---

## Hardware-Specific Notes

### Apple Silicon (M1/M2/M3)

- Native ARM Unity builds preferred over Rosetta
- Metal API is default and optimized
- Unified memory means GPU shares RAM - close GPU-heavy apps
- Efficiency cores handle background tasks well

### Recommended Specs for Unity XR Development

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| RAM | 16GB | 32GB+ |
| Storage | 256GB SSD | 512GB+ NVMe |
| Chip | M1 | M2 Pro/M3 Pro+ |

---

## Troubleshooting

### Unity Slow to Open Projects

1. Delete `Library/` folder in project (will rebuild)
2. Clear global Unity cache: `rm -rf ~/Library/Unity/cache/*`
3. Check Spotlight isn't indexing Library folders

### Rider Indexing Forever

1. Invalidate caches: File → Invalidate Caches
2. Delete index: `rm -rf ~/Library/Caches/JetBrains/Rider*/index`
3. Exclude `Library/` and `Temp/` from indexing

### High CPU from mds_stores (Spotlight)

1. Add dev folders to Spotlight Privacy exclusions
2. Rebuild index: `sudo mdutil -E /`
3. Disable Spotlight temporarily: `sudo mdutil -a -i off`

---

## References

- [Unity Editor Performance](https://docs.unity3d.com/Manual/EditorPerformance.html)
- [JetBrains Rider Performance](https://www.jetbrains.com/help/rider/Performance_Tips.html)
- [Apple Developer - Performance](https://developer.apple.com/documentation/xcode/improving-build-efficiency)

---

## Claude Code Configuration

**Location**: `~/.claude/settings.json`

### Optimized Settings for Unity

```json
{
  "env": {
    "BASH_DEFAULT_TIMEOUT_MS": "60000",
    "CLAUDE_CODE_MAX_OUTPUT_TOKENS": "16384",
    "CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC": "1"
  },
  "permissions": {
    "deny": [
      "**/Library/**",
      "**/Temp/**",
      "**/obj/**",
      "**/Logs/**",
      "**/*.meta"
    ]
  },
  "ignorePatterns": [
    "**/Library/**",
    "**/Temp/**",
    "**/obj/**",
    "**/Logs/**",
    "**/Build/**",
    "**/*.meta",
    "**/node_modules/**",
    "**/.git/objects/**"
  ]
}
```

### Cache Cleanup

```bash
# Clear Claude Code debug logs (can grow to 1GB+)
rm -rf ~/.claude/debug/*

# Check sizes
du -sh ~/.claude/debug ~/.claude/history.jsonl ~/.claude/file-history
```

### Useful MCP Permissions for Unity

```json
"permissions": {
  "allow": [
    "mcp__UnityMCP__*",
    "WebFetch(domain:docs.unity3d.com)",
    "WebFetch(domain:forum.unity.com)",
    "WebFetch(domain:discussions.unity.com)"
  ]
}
```

---

## Antigravity Configuration

**Location**: `~/Library/Application Support/Antigravity/User/settings.json`

### Performance Settings

```json
{
  // Disable animations
  "editor.smoothScrolling": false,
  "editor.cursorBlinking": "solid",
  "editor.cursorSmoothCaretAnimation": "off",
  "editor.stickyScroll.enabled": false,
  "editor.minimap.enabled": false,

  // Disable background git
  "git.autofetch": false,
  "git.autorefresh": false,

  // File exclusions (critical for Unity)
  "files.watcherExclude": {
    "**/Library/**": true,
    "**/Temp/**": true,
    "**/Logs/**": true,
    "**/Build/**": true,
    "**/obj/**": true,
    "**/node_modules/**": true,
    "**/.git/objects/**": true,
    "**/Packages/com.unity.*/**": true
  },

  "search.exclude": {
    "**/Library/**": true,
    "**/Temp/**": true,
    "**/*.meta": true
  }
}
```

### Unity C# Settings

```json
{
  "omnisharp.useModernNet": true,
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "csharp.semanticHighlighting.enabled": true,

  "files.associations": {
    "*.shader": "hlsl",
    "*.cginc": "hlsl",
    "*.compute": "hlsl",
    "*.asmdef": "json"
  },

  "explorer.fileNesting.patterns": {
    "*.cs": "${capture}.cs.meta",
    "*.prefab": "${capture}.prefab.meta",
    "*.unity": "${capture}.unity.meta"
  }
}
```

### Cache Cleanup

```bash
# Clear Antigravity caches
rm -rf ~/Library/Application\ Support/Antigravity/CachedData/*
rm -rf ~/Library/Application\ Support/Antigravity/Cache/*
rm -rf ~/Library/Application\ Support/Antigravity/Code\ Cache/*
```

---

## Combined Cleanup Script

Save as `~/bin/unity-dev-cleanup.sh`:

```bash
#!/bin/bash
echo "Unity Development Full Cleanup"
echo "=============================="

# Xcode
rm -rf ~/Library/Developer/Xcode/DerivedData/* 2>/dev/null
echo "✓ Xcode DerivedData cleared"

# JetBrains/Rider
rm -rf ~/Library/Caches/JetBrains/Rider*/caches 2>/dev/null
rm -rf ~/Library/Caches/JetBrains/Rider*/index 2>/dev/null
echo "✓ Rider caches cleared"

# Unity
rm -rf ~/Library/Unity/cache/* 2>/dev/null
echo "✓ Unity cache cleared"

# Claude Code
rm -rf ~/.claude/debug/* 2>/dev/null
echo "✓ Claude Code debug logs cleared"

# Antigravity
rm -rf ~/Library/Application\ Support/Antigravity/CachedData/* 2>/dev/null
rm -rf ~/Library/Application\ Support/Antigravity/Cache/* 2>/dev/null
rm -rf ~/Library/Application\ Support/Antigravity/Code\ Cache/* 2>/dev/null
echo "✓ Antigravity caches cleared"

echo ""
df -h / | tail -1 | awk '{print "Disk: " $4 " free"}'
```
