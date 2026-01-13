# H3M Portals & Unity-XR-AI Constitution

<!-- Sync Impact Report
Version Change: 1.0.0 -> 1.1.0
- Refocused on H3M Portals (Holographic/Immersive).
- Added "Holographic First" and "Cross-Platform" principles.
- Removed "Power User" specifics that were WarpJobs-centric.
-->

## Core Principles

### I. Holographic & Immersive First
This project is about building dynamic, 3D functional worlds, not 2D interfaces.
- **Volumetric**: Think in RGBD, point clouds, and mesh data, not flat video.
- **Immersive**: User experience must be "inside" the content (AR/VR/XR).
- **Art-Driven**: Tools are for *creation* (painting, sculpting, collaborative world-building).

### II. Cross-Platform Reality
Content must flow seamlessly between devices.
- **Universal**: iOS (local High-Fi) <-> WebGL (access everywhere) <-> VR (deep immersion).
- **Blueprints**: Use **MetavidoVFX**, **Needle Engine**, and **Rcam** as architectural references.
- **Standards**: glTF, USDZ, and WebXR are the common languages.

### III. Robustness & Self-Healing
The system must be resilient to the chaotic reality of a local dev environment.
- **Environment Awareness**: Detect configuration issues (like auth tokens) and warn or fix them automatically.
- **Logging**: Non-interactive processes must log to files, and agents must read those logs.
- **Zero Config**: "Edit one, publish everywhere" flow is the goal.

### IV. Spec-Driven Development (SDD)
We define "What" and "Why" before "How".
- **Living Roadmap**: `_H3M_HOLOGRAM_ROADMAP.md` is the strategic guide.
- **Phased Workflow**: Specify -> Plan -> Task -> Implement.
- **Guardrails**: Use checklists to ensure technical feasibility (e.g., performance on mobile) before coding.

### V. Evolutionary Intelligence (LLM R-Weir)
AI is not just for text; it creates worlds.
- **Future Vision**: Construct immersive experiences from natural language.
- **Immediate Goal**: Integrate foundational LLM tools (Whisper STT, basic scrapers) as modular microservices.
- **Microservices**: Keep AI logic decoupled from the core real-time Unity engine.

## Technology Standards

### Stack Choices
- **Core Engine**: Unity 6000.1.2f1 (AR Foundation, VFX Graph).
- **Web Runtime**: Needle Engine (Three.js integration).
- **Networking**: Normcore (Realtime) or WebRTC for peer-to-peer streams.
- **Data**: RGBD Video, glTF.

### Code Quality
- **High Performance**: 60 FPS on mobile is non-negotiable. Use Compute Shaders and Job System.
- **Modular**: Keep features (e.g., "Hand Painting", "Hologram Stream") isolated.
- **Type-Safe**: C# for Unity, TypeScript for Web.

## Governance

### Amendments
This Constitution supersedes previous ad-hoc rules. Amendments must be proposed via the `/speckit.constitution` workflow and ratified by the user.

### Compliance
All automated agents (Windsurf, Gemini, Claude) must adhere to these principles. Code reviews must verify performance and cross-platform compatibility.

**Version**: 1.1.0 | **Ratified**: 2025-12-06 | **Last Amended**: 2025-12-06
