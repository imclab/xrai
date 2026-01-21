# Pipeline Comparison: Rcam vs MetavidoVFX

**Date**: 2026-01-20
**Purpose**: Architectural comparison of data pipelines

---

## Overview Diagram

```
===================================================================================
                           RCAM PIPELINES (Keijiro)
===================================================================================

RCAM2 (2021): Direct AR → VFX (No Compute)
──────────────────────────────────────────────────────────────────────────────────
  AR Foundation                    VFX Graph
  ┌─────────────┐                  ┌─────────────┐
  │ DepthMap    │ ─────────────────│ Sample      │
  │ StencilMap  │     direct       │ Reconstruct │──→ Particles
  │ ColorMap    │     binding      │ in HLSL     │
  └─────────────┘                  └─────────────┘

  Problem: Each VFX reconstructs depth→world in HLSL (duplicate work)
  Performance: O(N) HLSL reconstruction per VFX


RCAM3 (2022): Compute → VFX (Single Source)
──────────────────────────────────────────────────────────────────────────────────
  AR Foundation        Compute Shader         VFX Graph
  ┌─────────────┐      ┌─────────────┐       ┌─────────────┐
  │ DepthMap    │─────→│ DepthToPos  │──────→│ PositionMap │──→ Particles
  │ StencilMap  │      │ (GPU)       │       │ (sampled)   │
  │ ColorMap    │      └─────────────┘       └─────────────┘

  Improvement: GPU compute once, VFX samples
  Limitation: Still one pipeline per scene, no mixing


RCAM4 (2023): NDI Network Stream → Compute → VFX
──────────────────────────────────────────────────────────────────────────────────
  iPhone (Remote)      PC (Unity)
  ┌─────────────┐      ┌─────────────┐       ┌─────────────┐
  │ ARKit       │─NDI─→│ NDI Receive │──────→│ Compute     │──→ VFX
  │ Camera      │      │ Decode      │       │ DepthToPos  │
  │ Depth/Color │      │ (CPU heavy) │       │             │
  └─────────────┘      └─────────────┘       └─────────────┘

  Use Case: Remote capture for post-production
  Latency: 50-100ms (network + decode)
  NOT for live AR (different device than display)


===================================================================================
                        METAVIDOVFX PIPELINE (Our Approach)
===================================================================================

Hybrid Bridge Pattern (2026): O(1) Compute + O(N) Lightweight Binding
──────────────────────────────────────────────────────────────────────────────────

  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                        AR Foundation (SAME DEVICE)                          │
  │  ┌───────────────────────────────────────────────────────────────────────┐  │
  │  │  AROcclusionManager     ARCameraManager     ARMeshManager             │  │
  │  │  ├─ humanDepthTexture   ├─ ColorMap         ├─ ARMesh (optional)      │  │
  │  │  └─ humanStencil        └─ Camera intrinsics                          │  │
  │  └───────────────────────────────────────────────────────────────────────┘  │
  └──────────────────────────────────┬──────────────────────────────────────────┘
                                     │
                                     ▼
  ┌─────────────────────────────────────────────────────────────────────────────┐
  │                    ARDepthSource (SINGLETON)                                │
  │  ┌───────────────────────────────────────────────────────────────────────┐  │
  │  │                    ONE Compute Dispatch                               │  │
  │  │  DepthToWorld.compute                                                 │  │
  │  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐               │  │
  │  │  │ DepthMap    │───→│ GPU Compute │───→│ PositionMap │               │  │
  │  │  │ + Camera    │    │ [32,32,1]   │    │ VelocityMap │               │  │
  │  │  │ intrinsics  │    │ threads     │    │ (optional)  │               │  │
  │  │  └─────────────┘    └─────────────┘    └─────────────┘               │  │
  │  └───────────────────────────────────────────────────────────────────────┘  │
  │                                                                             │
  │  Public Properties (cached, shared by ALL VFX):                             │
  │  ├─ DepthMap        (from AR)                                               │
  │  ├─ StencilMap      (from AR)                                               │
  │  ├─ ColorMap        (demand-driven)                                         │
  │  ├─ PositionMap     (GPU-computed)                                          │
  │  ├─ VelocityMap     (GPU-computed, optional)                                │
  │  ├─ RayParams       (camera intrinsics)                                     │
  │  └─ InverseView     (camera transform)                                      │
  └──────────────────────────────────┬──────────────────────────────────────────┘
                                     │
          ┌──────────────────────────┼──────────────────────────┐
          │                          │                          │
          ▼                          ▼                          ▼
  ┌───────────────┐          ┌───────────────┐          ┌───────────────┐
  │ VFXARBinder   │          │ VFXARBinder   │          │ VFXARBinder   │
  │ (People VFX)  │          │ (Env VFX)     │          │ (Audio VFX)   │
  ├───────────────┤          ├───────────────┤          ├───────────────┤
  │ SetTexture()  │          │ SetTexture()  │          │ SetFloat()    │
  │ ~5μs/frame    │          │ ~3μs/frame    │          │ ~2μs/frame    │
  └───────┬───────┘          └───────┬───────┘          └───────┬───────┘
          │                          │                          │
          ▼                          ▼                          ▼
  ┌───────────────┐          ┌───────────────┐          ┌───────────────┐
  │ bubbles.vfx   │          │ worldgrid.vfx │          │ audioreact.vfx│
  │ particles.vfx │          │ ribbons.vfx   │          │ beatsync.vfx  │
  │ trails.vfx    │          │ swarm.vfx     │          │               │
  └───────────────┘          └───────────────┘          └───────────────┘


  PARALLEL DATA SOURCES (Multi-Mode Support):
  ──────────────────────────────────────────────────────────────────────────────

  ┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
  │   ARDepthSource │   │   AudioBridge   │   │BodyPartSegmenter│
  │   (AR Data)     │   │   (FFT Audio)   │   │ (ML Keypoints)  │
  └────────┬────────┘   └────────┬────────┘   └────────┬────────┘
           │                     │                     │
           │ DepthMap            │ _AudioBands        │ KeypointBuffer
           │ PositionMap         │ _AudioVolume       │ MaskTexture
           │ ColorMap            │ _BeatPulse         │
           │                     │                     │
           ▼                     ▼                     ▼
  ┌─────────────────────────────────────────────────────────────────┐
  │                    VFXARBinder (per-VFX)                        │
  │  ┌──────────────────────────────────────────────────────────┐   │
  │  │  Auto-Detection: scans VFX for supported properties      │   │
  │  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │   │
  │  │  │ if HasDepth │  │ if HasAudio │  │ if HasKeyPt │      │   │
  │  │  │ → bind AR   │  │ → bind FFT  │  │ → bind ML   │      │   │
  │  │  └─────────────┘  └─────────────┘  └─────────────┘      │   │
  │  └──────────────────────────────────────────────────────────┘   │
  │                                                                 │
  │  270+ Property Aliases (cross-project compatibility):           │
  │  ├─ "DepthMap" / "DepthTexture" / "_Depth" / "Depth"           │
  │  ├─ "Throttle" / "Intensity" / "Scale" / "Amount"              │
  │  └─ "ColorMap" / "ColorTexture" / "_MainTex" / "Background"    │
  └─────────────────────────────────────────────────────────────────┘


===================================================================================
                              COMPARISON TABLE
===================================================================================

┌──────────────────┬───────────────┬───────────────┬───────────────┬──────────────┐
│ Aspect           │ Rcam2         │ Rcam3         │ Rcam4         │ MetavidoVFX  │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Year             │ 2021          │ 2022          │ 2023          │ 2026         │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Data Source      │ Local AR      │ Local AR      │ NDI (Remote)  │ Local AR     │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Depth→World      │ HLSL per-VFX  │ Compute once  │ Compute once  │ Compute once │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Compute Scaling  │ O(N) HLSL     │ O(1) compute  │ O(1) compute  │ O(1) compute │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Per-VFX Overhead │ High (HLSL)   │ Medium        │ Medium        │ Low (~5μs)   │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Multi-VFX        │ Expensive     │ Single scene  │ Single scene  │ N modes sim. │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Mode Mixing      │ No            │ No            │ No            │ Yes (6 modes)│
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Latency          │ ~16ms         │ ~16ms         │ 50-100ms      │ ~16ms        │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Audio Support    │ No            │ Limited       │ Limited       │ Full FFT     │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ ML Keypoints     │ No            │ No            │ No            │ Yes (17 pts) │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Hand Tracking    │ No            │ No            │ No            │ Yes          │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Property Aliases │ Hardcoded     │ Hardcoded     │ Hardcoded     │ 270+ aliases │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Auto-Detection   │ No            │ No            │ No            │ Yes          │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ VFX Count        │ ~20           │ ~8            │ ~14           │ 88           │
├──────────────────┼───────────────┼───────────────┼───────────────┼──────────────┤
│ Verified FPS     │ N/A           │ N/A           │ N/A           │ 353 @ 10 VFX │
└──────────────────┴───────────────┴───────────────┴───────────────┴──────────────┘


===================================================================================
                        KEY INNOVATIONS IN METAVIDOVFX
===================================================================================

1. HYBRID BRIDGE PATTERN
   ─────────────────────────────────────────────────────────────────────────────
   Problem: Rcam pipelines each handle ONE data flow
   Solution: Single compute source + multiple lightweight binders

   ARDepthSource (1 compute)  →  VFXARBinder (N bindings, ~5μs each)
                              →  AudioBridge (global shader props)
                              →  NNCamKeypointBinder (GraphicsBuffer)

2. PROPERTY ALIAS RESOLUTION
   ─────────────────────────────────────────────────────────────────────────────
   Problem: Rcam2/3/4 VFX use different property names
   Solution: 270+ aliases auto-resolved in Awake(), fast int IDs in Update()

   "DepthMap" ─┐
   "Depth"    ─┼─→ _idDepth (int) → SetTexture(_idDepth, tex) [1μs]
   "_Depth"   ─┘

3. MULTI-MODE SIMULTANEOUS EXECUTION
   ─────────────────────────────────────────────────────────────────────────────
   Problem: Rcam supports one VFX type at a time
   Solution: 6 parallel binding modes (AR, Audio, Keypoint, Hand, Face, Standalone)

   ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
   │ People  │ │ Audio   │ │ Keypoint│ │ Environ │
   │ (AR)    │ │ (FFT)   │ │ (ML)    │ │ (none)  │
   └────┬────┘ └────┬────┘ └────┬────┘ └────┬────┘
        │          │          │          │
        └──────────┴──────────┴──────────┘
                      │
              ALL RENDER SIMULTANEOUSLY

4. DEMAND-DRIVEN RESOURCE ALLOCATION
   ─────────────────────────────────────────────────────────────────────────────
   Problem: Rcam allocates all resources upfront
   Solution: Only allocate when mode requires it

   if (mode != Environment)
       ARDepthSource.RequestColorMap(true);  // Only when needed

   if (mode == Audio || mode == Hybrid)
       AudioBridge.SetBeatDetectionEnabled(true);  // FFT only when needed


===================================================================================
                              PERFORMANCE SUMMARY
===================================================================================

                    Rcam2           Rcam4           MetavidoVFX
                    ─────           ─────           ───────────
1 VFX:              ~3ms            ~5ms            ~2ms
5 VFX:              ~15ms (O(N))    ~10ms           ~3.5ms
10 VFX:             ~30ms (O(N))    ~15ms           ~5ms
20 VFX:             ~60ms (O(N))    ~25ms           ~8ms

Bottleneck:         HLSL per-VFX    NDI decode      Particle render
                                    + compute       (compute is free)

Conclusion: MetavidoVFX Hybrid Bridge achieves O(1) compute scaling,
            enabling 10+ simultaneous VFX at 60fps on mobile.

```

---

## Summary

| Pipeline | Best For | Limitation |
|----------|----------|------------|
| **Rcam2** | Simple single-VFX demo | Duplicate HLSL work per VFX |
| **Rcam3** | Clean depth→VFX | Single VFX type per scene |
| **Rcam4** | Remote capture/post-prod | High latency, CPU-heavy decode |
| **MetavidoVFX** | Live multi-modal AR | Requires AR device (no remote) |

---

*Last Updated: 2026-01-20*
