# Repository Guidelines

## Project Structure & Module Organization
- Scenes: `Assets/Scenes/HOLOGRAM_Mirror_MVP.unity` (main build) and `Player.unity` (preview).
- Code: `Assets/Scripts` runtime (AR→VFX bridges, hologram systems); build menus in `Assets/Scripts/Editor` and `Assets/Editor`.
- Visuals: `Assets/VFX`, `Assets/Shaders` (compute/HLSL), `Assets/Resources`/`Assets/UI`; group by feature.
- Config: `Packages/manifest.json`, `ProjectSettings/`, `UserSettings/`. Tests scaffolded under `Assets/Tests`.
- Pipelines (see `Assets/Documentation/PIPELINE_ARCHITECTURE.md`): VFXBinderManager is primary; H3M uses `HologramSource` + `HologramRenderer`; `OptimalARVFXBridge` is legacy/under review.

## Build, Test, and Development Commands
- Open `Assets/Scenes/HOLOGRAM_Mirror_MVP.unity`, run `Metavido/Build iOS` or `Build/Build iOS`.
- CI-friendly build: `./build_and_deploy.sh` (BuildScript.BuildiOS → xcodebuild). Env vars: `UNITY_VERSION`, `APPLE_TEAM_ID`, `DEVICE_NAME`.
- Alternate: `./build_ios.sh` (pins Xcode 16.4, calls AutomatedBuild.BuildiOS, archives, installs via `ios-deploy`).
- Fast rebuild/install: `./auto_build.sh` (kills Unity, rebuilds, deploys via `devicectl`).
- Logging: `./debug.sh` (stream) or `./debug.sh --dump` (crash logs).
- Tests: `Unity -batchmode -projectPath "$(pwd)" -runTests -testPlatform EditMode -testResults results.xml` (PlayMode similar).

## Coding Style & Naming Conventions
- C# with 4-space indentation; PascalCase for classes/methods/properties; camelCase for locals and serialized fields.
- Keep MonoBehaviour lifecycle methods grouped (Awake/OnEnable/Update/OnDisable) and favor serialized references over runtime `Find` calls.
- Document public APIs with `///` summaries; log with clear prefixes (`Debug.LogWarning("OptimalARVFXBridge: ...")`).
- Assets: name scenes and prefabs descriptively (`Feature_Context`), keep shaders/VFX under corresponding folders, avoid mixing platform-specific assets.

## Testing Guidelines
- Tests planned under `Assets/Tests/EditMode` and `Assets/Tests/PlayMode` (see `Assets/Tests/README.md`). Name files `*Tests.cs` and align namespaces to runtime code.
- Prefer fast Edit Mode tests for logic; keep Play Mode for AR Foundation/VFX integration. Prioritize critical AR/VFX paths.
- Commit new tests with run instructions (Test Runner or batch command above) and note any required packages (`com.unity.test-framework`).

## Commit & Pull Request Guidelines
- Follow existing history: concise, imperative summaries with optional scope (`VFX pipeline fixes: compute dispatch and binding conflict`).
- PRs should state scenes touched, Unity/AR Foundation versions, build target, and test evidence (Test Runner output, device logs, screenshots/GIFs for visual changes).
- Link issues/tasks; update relevant docs (`README.md`, `OptimizedARVFXBridge.md`, test README) when behavior or pipelines change.

## Docs, Specs, and Global Rules
- Read `README.md`, `CLAUDE.md`, `RULES.md`, `DEBUGGING.md`, `ARFoundationRemoteSetup.md`, and `OptimizedARVFXBridge.md` before significant changes; update them when workflows shift.
- Documentation hub: `Assets/Documentation/` (pipeline, system architecture, quick references); hologram pipelines in `PIPELINE_ARCHITECTURE.md`.
- Knowledge base: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/` (see `KB_MASTER_INDEX.md`); consult before major changes.
- Honor `RULES.md`: prefer Unity MCP for console checks, avoid deleting assets without backups, and maintain production-ready quality.
- Check `PLATFORM_COMPATIBILITY_MATRIX.md` for targets and `Assets/Tests/README.md` for planned coverage when adding features.
