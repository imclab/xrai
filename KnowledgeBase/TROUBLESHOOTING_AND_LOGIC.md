# System Troubleshooting & Global Logic

**Purpose**: This document contains troubleshooting guides for common environment issues (like Spec Kit setup) and a reference of optional global logic/rules for AI assistants.

---

## 🔧 Spec Kit Setup & Troubleshooting

### Issue: GitHub API 401 "Bad credentials"

When running project setup scripts (e.g., for `xrai-speckit`) that interact with the GitHub API, you may encounter an error like this:

```
GitHub API returned 401 for https://api.github.com/repos/...
Body: { "message": "Bad credentials", ... }
```

**Cause**:
This usually happens because a stale or invalid `GITHUB_TOKEN` environment variable is excessively overriding the active `gh` CLI authentication.

**Fix**:
Unset the environment variable in your current terminal session to force the script to use your active `gh auth` credentials (keyring).

```bash
# Run this in your terminal
unset GITHUB_TOKEN

# Then retry the setup command
```

### General Setup Logic

1. **Auth Check**: Always verify `gh auth status` before running repo-creation scripts.
2. **Token Priority**: Scripts often prioritize `GITHUB_TOKEN` over local keyring. If you are logged in via `gh` but failing, clear the env var.

---

## 🧠 Optional Global Rules & Logic

> **Note**: These rules are provided as an optional reference for "Power User" alignment. They reflect the user's preferred operational philosophy for AI agents (Gemini, Claude, etc.).

### 1. Philosophy: "Power User" First

* **Proactive but Safe**: Be proactive, but ask before non-trivial changes.
* **Simplicity**: Prioritize simplicity, efficiency, and standard tools over convoluted scripts.
* **Research**: Never declare something impossible without exhaustive research (e.g., check `defaults read` before assuming a macOS setting is locked).
* **Automate Busy Work**: Don't create work. If a log is needed, implement logging and *read it yourself*.

### 2. Operational Mandates

* **System Health**: Check for rogue processes before high-load tasks.
* **Token Efficiency**: Reduce token usage; warn if a workflow is inefficient.
* **Toolchain Awareness**: Know the user's tools (macOS, Unity, Xcode, Windsurf).
* **Clean Code**: Write production-ready, modular code.

## 🚀 Spec Kit Usage Guide

**Important**: Spec Kit commands (e.g., `/speckit.constitution`) are **Chat Commands**, not Terminal Commands.

**How to run**:

1. Ensure you have the Spec Kit installed (typically in `xrai-speckit/`).
2. **Do not** type `/` in your zsh terminal.
3. Type the command directly into your AI Assistant's chat input:
   * `/speckit.constitution`: Establish project principles.
   * `/speckit.specify`: Create baseline specs.
   * `/speckit.plan`: Create implementation plan.
   * `/speckit.tasks`: Generate tasks.
   * `/speckit.implement`: Execute code.

### 3. Debugging Protocols (macOS/Automator)

* **Logging**: For non-interactive scripts (Automator/Services), always redirect output to a file:
  ```bash
  exec > ~/Desktop/logfile.txt 2>&1
  ```
* **Permissions**: "Operation not permitted" usually requires Full Disk Access for the *parent* executable (bash, Automator).
* **Caching**: Automator aggressively caches. Restart or rename scripts to break cache.
* **Question the "Why"**: Before fixing a complex Automator workflow, ask: "Is there a simpler way?" (e.g., Alfred/Keyboard Maestro).

### 4. Continuous Improvement

* **Learn**: Augment productivity by learning from projects.
* **Focus**: Keep the big picture of local and online work in view.

---

## 🛠️ Unity iOS Build Troubleshooting

### Issue: Unity Batch Build Fails (Exit Code 1)

**Cause**: Unity Editor is already open with the same project.

**Solution**:
1. Build from Unity Editor instead: `Build → Build iOS`
2. Or close Unity Editor first, then run batch build

### Issue: Xcode "Provisioning Profile Required"

**Cause**: DEVELOPMENT_TEAM not set in project.pbxproj

**Fix**:
```bash
# Set your team ID in the Xcode project
sed -i '' 's/DEVELOPMENT_TEAM = "";/DEVELOPMENT_TEAM = YOUR_TEAM_ID;/g' \
  Builds/iOS/Unity-iPhone.xcodeproj/project.pbxproj
```

### Issue: Missing lib_burst_generated.cpp

**Cause**: Unity batch build didn't complete Burst compilation

**Solutions**:
1. Build from Unity Editor (not batch mode)
2. Or ensure Burst package is properly installed
3. Check `/tmp/unity_ios_build.log` for Burst errors

### Issue: VFX Particles Not Visible on iOS

**Common Causes**:
1. **Shader stripping**: Set `managedStrippingLevel: 0` in ProjectSettings
2. **Wrong RayParams format**: Must be `Vector4(offsetX, offsetY, scaleX, scaleY)` NOT `Vector4(scale, scale, 0, 0)`
3. **Wrong depth source**: Use `humanDepthTexture` for body, `environmentDepthTexture` for scene
4. **VFX properties mismatch**: Verify VFX has DepthMap, InverseView, RayParams, Spawn properties

### Issue: "Build failed" but Xcode Project Exists

**Explanation**: Unity batch mode sometimes reports "Build failed" even when build succeeds. Check if `Builds/iOS/Unity-iPhone.xcodeproj` exists.

**Verification**:
```bash
ls -la Builds/iOS/Unity-iPhone.xcodeproj  # If exists, build succeeded
```

### Quick Build Command

```bash
# From MetavidoVFX-main directory
./build_and_deploy.sh

# Or manual steps:
# 1. Unity Editor: Build → Build iOS
# 2. Set team ID: sed -i '' 's/DEVELOPMENT_TEAM = "";/DEVELOPMENT_TEAM = Z8622973EB;/g' ...
# 3. Xcode: xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone ...
```
