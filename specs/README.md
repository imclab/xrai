# Spec-Kit: Unity-XR-AI Feature Specifications

**Last Updated**: 2026-01-20
**Triple Verified**: All specs cross-referenced with KB, docs, and online research

## Active Specs

| ID | Name | Status | Priority |
|----|------|--------|----------|
| 002 | [H3M Hologram Foundation](./002-h3m-foundation/spec.md) | Implemented | P1 |
| 003 | [Hologram Conferencing](./003-hologram-conferencing/spec.md) | Draft | P2 |
| 004 | [MetavidoVFX Systems](./004-metavidovfx-systems/spec.md) | Implemented ✅ | P1 |
| 005 | [AR Texture Safety](./005-ar-texture-safety/spec.md) | Implemented ✅ | P1 |

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

### Online Sources
- [AR Foundation 6.2 Docs](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.2/manual/features/occlusion/occlusion-manager.html)
- [Unity WebRTC Package](https://github.com/Unity-Technologies/com.unity.webrtc)
- [keijiro/Metavido](https://github.com/keijiro/Metavido)
- [keijiro/MetavidoVFX](https://github.com/keijiro/MetavidoVFX)

## Spec-Kit Structure

```
specs/
├── README.md                    # This index
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
```

## Usage

1. **New Feature**: Create `specs/NNN-feature-name/` with `spec.md`
2. **Implementation**: Add `tasks.md` with task breakdown
3. **Validation**: Add `checklists/` for QA
4. **Verification**: Add "Triple Verification" table to spec header

## Template Location

See `xrai-speckit/.specify/templates/` for spec templates.
