# Tasks: VFX Multi-Mode & Audio/Physics System

**Spec**: [007-vfx-multi-mode](./spec.md)
**Status**: In Progress (Phases 1-3 Complete, Phase 4-6 Pending)

## Phase 1: VFX Category Integration ✅

- [x] **T-001**: Integrate VFXCategory into VFXARBinder
  - Read VFXCategory.Bindings flags
  - Only bind textures that match category requirements
  - Support runtime category changes
  - Maintain backward compatibility (Hybrid mode = bind all)
  - **Implemented**: VFXARBinder.SetMode(), SupportsMode(), GetSupportedModes()

- [x] **T-002**: Create VFXModeController component
  - UI dropdown for mode selection
  - Call VFXARBinder.SetMode() on change
  - Persist mode preference per VFX
  - Mode transition without VFX restart
  - **Implemented**: Assets/Scripts/VFX/VFXModeController.cs (~325 LOC)

- [x] **T-003**: Implement demand-driven ColorMap in ARDepthSource
  - Track which VFX need ColorMap
  - Create RenderTexture only when requested
  - Release when no longer needed
  - Save ~8MB when unused
  - **Implemented**: ARDepthSource.RequestColorMap(bool)

## Phase 2: Audio System Enhancement ✅

- [x] **T-004**: Implement BeatDetector class (~150 LOC)
  - FFT on bass band (20-200Hz)
  - Spectral flux calculation
  - Adaptive threshold (running average * 1.5)
  - Output: _BeatPulse (0→1→0 on onset)
  - Output: _BeatIntensity (running value)
  - **Implemented**: Assets/Scripts/Audio/BeatDetector.cs (~175 LOC)

- [x] **T-005**: Integrate beat detection into AudioBridge
  - Add BeatDetector instance
  - Set global shader props: _BeatPulse, _BeatIntensity
  - Optional enable/disable (demand-driven)
  - Performance: <0.1ms additional
  - **Implemented**: AudioBridge.cs lines 36, 73, 131-132

- [ ] **T-006**: Configure 4-band audio in AudioBridge
  - Fixed 4-band: Bass, Mids, Treble, SubBass
  - Support both mic input and AudioClip analysis
  - AudioSource component for music file playback
  - Auto-detect active audio source

- [ ] **T-007**: Update VFXAudioDataBinder for beat detection
  - Expose BeatPulse, BeatIntensity per VFX
  - Add parameter mapping configuration
  - Support multiple VFX properties driven by same band

## Phase 3: Physics System Enhancement ✅

- [x] **T-008**: Add AR mesh bounce collision to VFXPhysicsBinder
  - Get GraphicsBuffers from MeshVFX
  - Set MeshPositions, MeshNormals to VFX Graph
  - Bounce-only response (particles reflect off surfaces)
  - Configurable bounce factor (0-1)
  - Performance: <0.3ms per VFX
  - **Implemented**: VFXPhysicsBinder.bounceFactor, mesh collision support

- [x] **T-009**: Add gravity direction control
  - Default: (0, -9.8, 0) world down
  - Custom direction support
  - AR-relative gravity option (based on device orientation)
  - **Implemented**: gravityDirection, useWorldGravity, useARRelativeGravity, SetARRelativeGravity()

- [x] **T-010**: Add hand velocity binding
  - Track hand joint velocities from HandVFXController
  - Average velocity across relevant joints
  - Set HandVelocity Vector3 to VFX
  - **Implemented**: enableHandVelocity, handVelocityProperty, UpdateHandVelocity()

## Phase 4: VFX Compatibility Audit

- [ ] **T-011**: Audit all 73 VFX for mode compatibility
  - Check which properties each VFX exposes
  - Determine supported modes per VFX
  - Create compatibility matrix

- [ ] **T-012**: Add fallback handling for unsupported modes
  - Log warning when mode unsupported
  - Suggest alternative mode
  - Don't break VFX execution

- [ ] **T-013**: Update VFXProfiler for mode tracking
  - Show current mode per VFX
  - Track mode-specific performance
  - Report unsupported mode attempts

## Phase 5: UI Integration

- [ ] **T-014**: Update VFXToggleUI with mode selector
  - Per-VFX mode dropdown
  - Category quick-filter
  - Audio/Physics enable toggles

- [ ] **T-015**: Add audio visualization to dashboard
  - Frequency band bars
  - Beat indicator
  - Input level meter

- [ ] **T-016**: Add physics visualization to dashboard
  - Camera velocity vector
  - Gravity direction indicator
  - Collision count

## Phase 6: Testing & Validation

- [ ] **T-017**: Create audio test scene
  - VFX responding to different frequency bands
  - Beat detection validation
  - Performance profiling

- [ ] **T-018**: Create physics test scene
  - Camera velocity driven VFX
  - AR mesh collision testing
  - Gravity variations

- [ ] **T-019**: Device testing
  - iPhone 12+: Audio latency <50ms
  - iPhone 15 Pro: All features 60+ FPS
  - Quest 3: Verify compatibility

---

## Verification Checklist

- [ ] VFXARBinder respects VFXCategory.Bindings
- [ ] Mode switch completes in <1 frame
- [ ] Beat detection latency <50ms
- [ ] Audio FFT CPU <1ms on mobile
- [ ] 10 audio VFX maintain 60+ FPS
- [ ] ColorMap demand-driven (8MB savings)
- [ ] AR mesh collision working
- [ ] All 73 VFX audited for mode compatibility

---

## Dependencies

| Task | Depends On | Notes |
|------|------------|-------|
| T-004 | - | New class, no dependencies |
| T-005 | T-004 | Needs BeatDetector |
| T-008 | MeshVFX exists | Already implemented |
| T-010 | HandVFXController exists | Already implemented |
| T-014 | T-001, T-002 | Needs mode system first |

---

*Created: 2026-01-20*
*Design decisions finalized: 2026-01-20*
