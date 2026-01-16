# AntiGravity Global Rules - Unity XR Development

**Version**: 1.0 (AntiGravity-Optimized)
**Last Updated**: 2026-01-08
**Project**: portals_v4 (Unity 6000.2.14f1, AR Foundation 6.2.1, React Native 0.73.2)

---

## 📚 Knowledgebase (530+ Repos - ALWAYS CHECK FIRST)

**Location**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/`

**Quick Access**:
```
"Read ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.claude/KB_MASTER_INDEX.md"
```

**Essential Files**:
- `KB_MASTER_INDEX.md` - Auto-generated index (134 lines)
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

## ⚡ Core Workflow

### 1. Unity MCP First
**ALWAYS** use Unity MCP tools before manual approaches (30 sec vs 10 min)
- `manage_editor`, `manage_scene`, `read_console`
- **NEVER** create helper scripts for one-time tasks

### 2. Debugging: Binary Search
Create 3-5 step plan, isolate with binary search, **NEVER STOP until working**

### 3. Unity Best Practices (MANDATORY)
- **Check console after EVERY change**: Ask to read Unity console via MCP
- **Fix errors immediately** - Never leave broken code
- **Quest testing**: `adb logcat -v color -s Unity`

### 4. File Safety
- **ALWAYS ask before creating/deleting/moving files**
- **Backup OUTSIDE Assets** before deletion:
  ```bash
  BACKUP=~/Documents/GitHub/code-backups/$(date +%Y%m%d-%H%M%S)
  mkdir -p "$BACKUP" && cp -r "Assets/Folder" "$BACKUP/"
  ```

### 5. Code Quality
- Add comments explaining "why" not "what"
- XML docs (///) for methods/classes
- **NO emojis** unless requested

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
