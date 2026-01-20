# Tasks: VFX Library & Hybrid Bridge Pipeline

**Spec**: [006-vfx-library-pipeline](./spec.md)
**Status**: All phases complete

## Phase 1: Core Pipeline Components

- [x] **T-001**: Create ARDepthSource singleton
  - Single compute dispatch for DepthToWorld conversion
  - Expose DepthMap, StencilMap, PositionMap, VelocityMap, RayParams
  - TryGetTexture pattern for safe AR texture access
  - Editor mock textures for Play mode testing

- [x] **T-002**: Create VFXARBinder per-VFX component
  - Auto-detect which properties VFX Graph expects
  - SetTexture() calls only (no compute)
  - Support property name variants (InverseView/InverseViewMatrix, etc.)
  - Context menu for manual binding refresh

- [x] **T-003**: Create AudioBridge component
  - FFT frequency band analysis
  - Set global shader properties (_AudioBands, _AudioVolume)
  - 6 bands: SubBass, Bass, Mid, Treble, Volume, Pitch

## Phase 2: VFX Library Management

- [x] **T-004**: Rewrite VFXLibraryManager (~920 LOC)
  - Load VFX from Resources/VFX at runtime
  - Organize by category (8 categories)
  - One-click SetupCompletePipeline()
  - RemoveAllLegacyComponents()
  - AutoDetectAllBindings()

- [x] **T-005**: Organize 73 VFX into Resources/VFX
  - People (5): bubbles, glitch, humancube_stencil, particles, trails
  - Environment (5): swarm, warp, worldgrid, ribbons, markers
  - NNCam2 (9): joints, eyes, electrify, mosaic, tentacles, etc.
  - Akvfx (7): point, web, spikes, voxel, particles, etc.
  - Rcam2 (20): HDRP→URP converted body effects
  - Rcam3 (8): depth people/environment effects
  - Rcam4 (14): NDI-style body effects
  - SdfVfx (5): SDF environment effects

- [x] **T-006**: Create VFXCategory.cs
  - Enum for category types
  - Binding requirements flags
  - SetCategory method for runtime assignment

## Phase 3: Editor Automation

- [x] **T-007**: Create VFXPipelineMasterSetup (~500 LOC)
  - Menu: `H3M > VFX Pipeline Master > Setup Complete Pipeline`
  - Menu: `H3M > VFX Pipeline Master > Pipeline Components > *`
  - Menu: `H3M > VFX Pipeline Master > Legacy Management > *`
  - Menu: `H3M > VFX Pipeline Master > Testing > *`

- [x] **T-008**: Create InstantiateVFXFromResources (~90 LOC)
  - Batch VFX instantiation from Resources
  - Automatic VFXARBinder attachment

- [x] **T-009**: Create VFXLibraryManagerEditor
  - Custom Inspector with pipeline controls
  - One-click buttons for common operations

## Phase 4: Testing & Debug Tools

- [x] **T-010**: Create VFXPipelineDashboard (~350 LOC)
  - Real-time IMGUI overlay
  - FPS graph (60-frame history, min/avg/max)
  - Pipeline flow visualization
  - Binding status indicators (green/red)
  - Memory usage display
  - Active VFX list with particle counts

- [x] **T-011**: Create VFXTestHarness (~250 LOC)
  - Keyboard shortcuts:
    - `Tab`: Toggle Dashboard
    - `1-9`: Select VFX by index
    - `Space`: Cycle to next VFX
    - `C`: Cycle categories
    - `A`: Toggle all VFX
    - `P`: Auto-cycle profiling mode
    - `R`: Refresh VFX list

## Phase 5: Legacy Migration

- [x] **T-012**: Mark legacy components
  - VFXBinderManager → disabled/removed
  - VFXARDataBinder → disabled/removed
  - EnhancedAudioProcessor → replaced by AudioBridge
  - PeopleOcclusionVFXManager → removed

- [x] **T-013**: Move legacy to _Legacy folder
  - Preserve for reference
  - Exclude from builds

## Phase 6: Documentation

- [x] **T-014**: Update MetavidoVFX documentation
  - README.md with pipeline architecture
  - SYSTEM_ARCHITECTURE.md with data flow
  - QUICK_REFERENCE.md with VFX properties
  - VFX_PIPELINE_FINAL_RECOMMENDATION.md

- [x] **T-015**: Update CLAUDE.md files
  - Main project CLAUDE.md
  - MetavidoVFX CLAUDE.md
  - Menu commands reference

## Phase 7: Performance Validation

- [x] **T-016**: Performance testing
  - 1 VFX: ~2ms GPU time
  - 10 VFX: ~5ms GPU time (353 FPS)
  - 20 VFX: ~8ms GPU time (60fps feasible)

- [x] **T-017**: Device testing
  - iPhone 15 Pro: Verified Jan 16, 2026
  - AR Foundation 6.2.1 compatibility confirmed

---

## Verification Checklist

- [x] ARDepthSource creates PositionMap correctly
- [x] VFXARBinder auto-detects VFX properties
- [x] 73 VFX load from Resources
- [x] Categories display correctly in UI
- [x] Dashboard shows real-time FPS
- [x] Test Harness responds to keyboard
- [x] Legacy components auto-removed
- [x] 60+ FPS with 10 VFX on device
- [x] TryGetTexture prevents AR startup crash

---

*All tasks completed: 2026-01-16*
*Verified: 2026-01-20*
