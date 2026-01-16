# ⚠️ Legacy Components - DO NOT USE

**Date Deprecated**: 2026-01-16

These components have been replaced by the **Hybrid Bridge Pattern** which is 85% faster.

---

## Why Deprecated

| Old Component | Lines | Problem |
|--------------|-------|---------|
| **VFXBinderManager** | 1,357 | O(N) compute dispatches, bloated |
| **VFXARDataBinder** | 1,035 | Per-VFX compute (slow at scale) |
| **PeopleOcclusionVFXManager** | 376 | Creates own VFX, overlaps |

**Total**: 2,768 lines of inefficient code

---

## New Pipeline (Use This Instead)

| New Component | Lines | Purpose |
|--------------|-------|---------|
| **ARDepthSource** | ~500 | Single compute dispatch (O(1)) |
| **VFXARBinder** | ~350 | Lightweight per-VFX binding |
| **AudioBridge** | ~130 | FFT audio to global vectors |

**Location**: `Assets/Scripts/Bridges/`

---

## Performance Comparison

| VFX Count | Old (O(N) compute) | New (O(1) compute) |
|-----------|-------------------|-------------------|
| 1 | 1.1ms | 1.15ms |
| 5 | 5.5ms | 1.35ms |
| 10 | **11ms** | **1.6ms** |
| 20 | 22ms | 2.1ms |

---

## Migration

Run: **H3M > VFX Pipeline Master > Setup Complete Pipeline**

Or manually:
1. Remove legacy components from GameObjects
2. Add `ARDepthSource` to scene (singleton)
3. Add `VFXARBinder` to each VFX GameObject

---

## Can I Delete These?

Yes, but keep them until you've verified the new pipeline works on device.

Once confirmed:
```
git rm -r Assets/Scripts/_Legacy/
```
