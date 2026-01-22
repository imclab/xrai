# Repository Guidelines

## Project Structure & Module Organization
- Scenes: `Assets/HOLOGRAM_Mirror_MVP.unity` (main build), `Assets/HOLOGRAM.unity` (active dev), `Assets/Player.unity` (preview), spec demos under `Assets/Scenes/SpecDemos/`.
- Code: runtime in `Assets/Scripts` (AR→VFX bridges, VFX systems, hand/audio); Editor menus/tools in `Assets/Scripts/Editor` and `Assets/Editor`.
- Visuals: `Assets/VFX`, `Assets/Resources/VFX` (73 curated VFX), `Assets/Shaders` (compute/HLSL), `Assets/UI`; group by feature.
- Config: `Packages/manifest.json`, `ProjectSettings/`, `UserSettings/`. Tests scaffolded under `Assets/Tests`.
- Pipelines (see `Assets/Documentation/PIPELINE_ARCHITECTURE.md`):
  - **Primary**: Hybrid Bridge — `ARDepthSource` (singleton compute) + `VFXARBinder` (per-VFX SetTexture).
  - **Audio**: `AudioBridge` (global shader props).
  - **Legacy**: `VFXBinderManager`, `VFXARDataBinder`, `EnhancedAudioProcessor`, `OptimalARVFXBridge` (avoid unless explicitly needed).
  - **Hologram components** (`HologramSource`, `HologramRenderer`, `HologramAnchor`, `HologramPlacer`, `HologramController`) are **reserved for Spec 003**; do not modify outside that spec.

## Build, Test, and Development Commands
- Open `Assets/HOLOGRAM_Mirror_MVP.unity`, run `Metavido/Build iOS` or `Build/Build iOS`.
- CI-friendly build: `./build_and_deploy.sh` (BuildScript.BuildiOS → xcodebuild). Env vars: `UNITY_VERSION`, `APPLE_TEAM_ID`, `DEVICE_NAME`.
- Alternate: `./build_ios.sh` (pins Xcode 16.4, calls AutomatedBuild.BuildiOS, archives, installs via `ios-deploy`).
- Fast rebuild/install: `./auto_build.sh` (kills Unity, rebuilds, deploys via `devicectl`).
- Logging: `./debug.sh` (stream) or `./debug.sh --dump` (crash logs).
- Tests: `Unity -batchmode -projectPath "$(pwd)" -runTests -testPlatform EditMode -testResults results.xml` (PlayMode similar).

## Coding Style & Naming Conventions
- C# with 4-space indentation; PascalCase for classes/methods/properties; camelCase for locals and serialized fields.
- Keep MonoBehaviour lifecycle methods grouped (Awake/OnEnable/Update/OnDisable) and favor serialized references over runtime `Find` calls.
- Document public APIs with `///` summaries; log with clear prefixes (`Debug.LogWarning("ARDepthSource: ...")`).
- Assets: name scenes and prefabs descriptively (`Feature_Context`), keep shaders/VFX under corresponding folders, avoid mixing platform-specific assets.
- AR texture safety: use TryGetTexture pattern (AR Foundation textures can throw before subsystem is ready).
- Compute shaders: ensure dispatch matches `numthreads(32,32,1)` in `DepthToWorld.compute`.

## Testing Guidelines
- Tests planned under `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` (see `Assets/Tests/README.md`). Name files `*Tests.cs` and align namespaces to runtime code.
- Prefer fast Edit Mode tests for logic; keep Play Mode for AR Foundation/VFX integration. Prioritize critical AR/VFX paths.
- Commit new tests with run instructions (Test Runner or batch command above) and note any required packages (`com.unity.test-framework`).

## Commit & Pull Request Guidelines
- Follow existing history: concise, imperative summaries with optional scope (`VFX pipeline fixes: compute dispatch and binding conflict`).
- PRs should state scenes touched, Unity/AR Foundation versions, build target, and test evidence (Test Runner output, device logs, screenshots/GIFs for visual changes).
- Link issues/tasks; update relevant docs (`README.md`, `OptimizedARVFXBridge.md`, test README) when behavior or pipelines change.

## Docs, Specs, and Global Rules
- **Read first**: `/Users/jamestunick/GLOBAL_RULES.md`, `CLAUDE.md`, `RULES.md`, `README.md`, `Assets/Documentation/README.md`, `Assets/Documentation/PIPELINE_ARCHITECTURE.md`, `Assets/Documentation/specs/README.md`.
- Documentation hub: `Assets/Documentation/` (pipeline, system architecture, quick references, changelogs).
- Specs live in `Assets/Documentation/specs/`; update spec docs and tasks when changing behavior. Hologram-related changes go only in Spec 003 tasks.
- Knowledge base: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/` (start with `_MASTER_KNOWLEDGEBASE_INDEX.md` or `.claude/KB_MASTER_INDEX.md`). Search KB before implementing (use `kb`, `kbfix`, `kbtag`).
- Honor global rules: Unity MCP first for editor/console checks, never delete assets without explicit instruction + backup, keep production-ready quality.
- Check `PLATFORM_COMPATIBILITY_MATRIX.md` for targets and `Assets/Tests/README.md` for planned coverage when adding features.
