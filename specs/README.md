# Spec-Kit: Unity-XR-AI Feature Specifications

**Last Updated**: 2026-01-20
**Triple Verified**: All specs cross-referenced with KB, docs, and online research

---

## Master Development Plan

**[MASTER_DEVELOPMENT_PLAN.md](./MASTER_DEVELOPMENT_PLAN.md)** - Consolidated implementation order, sprint plan, debug infrastructure, testing strategy.

### Development Principles
- **Incremental**: Each task produces testable output
- **Test-First**: Mock providers, recorded sessions, MCP-integrated
- **Debug-Verbose**: Selective logging with compile-time stripping
- **MCP-Integrated**: Unity Editor testing without device

### Implementation Order (Sprints)

| Sprint | Spec | Focus | Tasks |
|--------|------|-------|-------|
| 0 | 008 | Debug Infrastructure | D-001 to D-007 |
| 1 | 007 | VFX Multi-Mode & Audio | T-001 to T-019 |
| 2 | 008 | Tracking Core Interfaces | T-005 to T-007, D-007 to D-009 |
| 3 | 008 | ARKit Providers | T-008b to T-013c |
| 4 | 008 | Voice Architecture | T-030 to T-037 |
| 5 | **009** | **Icosa/Sketchfab Foundation** | 0.1-0.3 (SketchfabClient, ModelCache) |
| 6 | **009** | **Unified Search & Caching** | 1.1-1.3 (UnifiedModelSearch, AR placement) |
| 7 | 008 | Testing & LLM | T-038 to T-051 |
| 8 | 008 | Platform Providers | T-012 to T-019 |
| 9 | 008 | Multiuser Sync | T-023 to T-026 |
| 10 | 009 | 3D Model UI & Polish | 2.1-3.4 (UI panels, testing) |
| 11 | 003 | Hologram Conferencing | (depends on 008 multiuser) |

---

## Active Specs

| ID | Name | Status | Priority | Tasks |
|----|------|--------|----------|-------|
| 002 | [H3M Hologram Foundation](./002-h3m-foundation/spec.md) | Implemented | P1 | - |
| 003 | [Hologram Conferencing](./003-hologram-conferencing/spec.md) | Draft | P2 | Pending |
| 004 | [MetavidoVFX Systems](./004-metavidovfx-systems/spec.md) | Implemented ✅ | P1 | - |
| 005 | [AR Texture Safety](./005-ar-texture-safety/spec.md) | Implemented ✅ | P1 | - |
| 006 | [VFX Library & Pipeline](./006-vfx-library-pipeline/spec.md) | Implemented ✅ | P1 | Complete |
| 007 | [VFX Multi-Mode & Audio/Physics](./007-vfx-multi-mode/spec.md) | Ready | P1 | 19 tasks |
| **008** | [**Multimodal ML Foundations**](./008-crossplatform-multimodal-ml-foundations/spec.md) | **Architecture Approved** | **P0** | **67 tasks** |
| 009 | [Icosa & Sketchfab 3D Model Integration](./009-icosa-sketchfab-integration/spec.md) | Draft | P1 | 14 tasks |

## Removed Specs

| ID | Name | Reason |
|----|------|--------|
| 001 | WarpJobs Engine | Deprecated - not relevant to current roadmap |

## Verification Sources

### Knowledge Base Cross-References
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR Foundation patterns
- `_COMPREHENSIVE_HOLOGRAM_PIPELINE_ARCHITECTURE.md` - 6-layer architecture
- `_HOLOGRAM_RECORDING_PLAYBACK.md` - Metavido format specs
- `_WEBRTC_MULTIUSER_MULTIPLATFORM_GUIDE.md` - Multiplayer patterns
- `_HAND_VFX_PATTERNS.md` - 52 VFX effects
- `_LLMR_XR_AI_ARCHITECTURE_PATTERNS.md` - LLM integration (LLMR, meta prompting)
- `_XR_AI_INDUSTRY_ROADMAP_2025-2027.md` - Industry roadmap (Unity, Meta, Google, OpenAI)

### Online Sources
- [AR Foundation 6.2 Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/manual/features/occlusion/occlusion-manager.html)
- [Unity WebRTC Package](https://github.com/Unity-Technologies/com.unity.webrtc)
- [keijiro/Metavido](https://github.com/keijiro/Metavido)
- [keijiro/MetavidoVFX](https://github.com/keijiro/MetavidoVFX)
- [Icosa Gallery API](https://api.icosa.gallery/v1/docs) - 3D model search
- [Icosa Unity Toolkit](https://github.com/icosa-gallery/icosa-toolkit-unity)
- [Sketchfab Download API](https://sketchfab.com/blogs/community/announcing-the-sketchfab-download-api-a-search-bar-for-the-3d-world/)
- [KhronosGroup/UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) - Runtime glTF loading

## Spec-Kit Structure

```
specs/
├── README.md                    # This index
├── MASTER_DEVELOPMENT_PLAN.md   # Consolidated sprint plan
│
├── 002-h3m-foundation/
│   ├── spec.md                  # Feature specification
│   └── tasks.md                 # Task breakdown
├── 003-hologram-conferencing/
│   ├── spec.md
│   └── tasks.md
├── 004-metavidovfx-systems/
│   ├── spec.md
│   ├── tasks.md
│   └── checklists/
├── 005-ar-texture-safety/
│   ├── spec.md
│   ├── tasks.md
│   └── checklists/
│       └── implementation.md
├── 006-vfx-library-pipeline/     # ✅ COMPLETE
│   ├── spec.md
│   └── tasks.md
├── 007-vfx-multi-mode/           # Ready (19 tasks)
│   ├── spec.md
│   └── tasks.md
├── 008-crossplatform-multimodal-ml-foundations/  # P0 (67 tasks)
│   ├── spec.md                  # Main specification
│   ├── tasks.md                 # 67 tasks across 14 phases
│   ├── FINAL_RECOMMENDATIONS.md # Architecture decisions
│   ├── MODULAR_TRACKING_ARCHITECTURE.md  # Detailed interfaces & code
│   └── TRACKING_SYSTEMS_DEEP_DIVE.md     # Platform research
├── 009-icosa-sketchfab-integration/  # P1 (14 tasks)
│   ├── spec.md                  # Voice-to-object, 3D model search
│   └── tasks.md                 # 4 sprints, ~42 hours
```

## Usage

1. **New Feature**: Create `specs/NNN-feature-name/` with `spec.md`
2. **Implementation**: Add `tasks.md` with task breakdown
3. **Validation**: Add `checklists/` for QA
4. **Verification**: Add "Triple Verification" table to spec header

## Template Location

Templates are available in `.specify/templates/` (symlinked from `xrai-speckit/.specify/templates/`):

| Template | Purpose |
|----------|---------|
| `spec-template.md` | Feature specification with user stories |
| `tasks-template.md` | Task breakdown with phases |
| `checklist-template.md` | QA/implementation checklists |
| `plan-template.md` | Implementation planning |

### Create New Spec

```bash
# Create new spec directory
mkdir -p specs/NNN-feature-name/checklists

# Copy templates
cp .specify/templates/spec-template.md specs/NNN-feature-name/spec.md
cp .specify/templates/tasks-template.md specs/NNN-feature-name/tasks.md
```

### Constitution

Project principles are defined in `.specify/memory/constitution.md`:
- Holographic & Immersive First
- Cross-Platform Reality
- Robustness & Self-Healing
- Spec-Driven Development (SDD)

### Scripts

Utility scripts available in `.specify/scripts/bash/`:
- `create-new-feature.sh` - Scaffold new feature spec
- `setup-plan.sh` - Setup implementation plan
- `check-prerequisites.sh` - Validate environment
