# VFX Duplicate Analysis

Analysis of potential duplicate and redundant VFX assets.

**Date**: 2026-01-16
**Total Production VFX**: 88
**Samples/Learning Templates**: 37

---

## Summary

| Category | Count | Action |
|----------|-------|--------|
| Exact Duplicates | 1 pair | Delete one |
| Copy with Space in Name | 1 | Delete |
| Near-Identical Copies | 3 | Keep (different purposes) |
| Modified Versions | 2 | Keep both |
| Similar Concepts Different Origins | ~15 | Keep all |
| Sample Templates | 37 | Optional delete |
| Experimental Library | 3 | Optional delete |

**Recommended Deletions**: 2 files (~300KB)
**Optional Deletions**: 40 files (~2MB) - samples/experimental

---

## 1. EXACT DUPLICATE (Delete One)

**Files with identical MD5 hash:**

| File | MD5 | Size |
|------|-----|------|
| `VFX/Rcam3/grid_any_rcam3.vfx` | 1b1d77a9e0af81fb6d607ea879c1400c | 89KB |
| `VFX/Rcam4/Environment/grid_environment_rcam4.vfx` | 1b1d77a9e0af81fb6d607ea879c1400c | 89KB |

**Scene References**: Both referenced in `HOLOGRAM_Mirror_MVP.unity`

**Recommendation**:
- Keep `VFX/Rcam3/grid_any_rcam3.vfx` (original location)
- Delete `VFX/Rcam4/Environment/grid_environment_rcam4.vfx`
- Update scene references

---

## 2. COPY WITH SPACE IN NAME (Delete)

| File | Size | Issue |
|------|------|-------|
| `Resources/VFX/pointcloud_depth_people_metavido 1.vfx` | 289KB | Accidental copy |
| `Resources/VFX/pointcloud_depth_people_metavido.vfx` | 290KB | Original |

**Difference**: 45 bytes (negligible)

**Recommendation**: Delete "pointcloud 1" - it's an accidental duplicate

---

## 3. NEAR-IDENTICAL COPIES (Keep Both)

These Resources copies are intentional for runtime loading:

| Resources Version | Original | Diff |
|-------------------|----------|------|
| `Resources/VFX/bodyparticles_depth_people_metavido.vfx` | `VFX/Metavido/bodyparticles_depth_people_metavido.vfx` | 43B |
| `Resources/VFX/humancube_stencil_people_h3m.vfx` | `VFX/HumanEffects/humancube_stencil_people_h3m.vfx` | 38B |

**Why Keep Both**:
- Resources/ = Runtime loadable via `Resources.Load()`
- VFX/ = Source/development versions
- Small differences are serialization artifacts (GUIDs, paths)

---

## 4. MODIFIED VERSIONS (Keep Both)

These have substantial modifications:

| File | Size | Difference |
|------|------|------------|
| `Resources/VFX/voxels_depth_people_metavido.vfx` | 443KB | +150KB vs original |
| `Resources/VFX/particles_depth_people_metavido.vfx` | 345KB | +34KB vs original |

**Why Different**:
- Resources versions have been customized for production use
- May have different particle counts, exposed properties, or optimizations

---

## 5. SIMILAR CONCEPTS, DIFFERENT ORIGINS (Keep All)

These share similar names but are **completely different implementations** from different Keijiro projects:

### Flame Effects (3)
| File | Origin | Style |
|------|--------|-------|
| `Resources/VFX/flame_depth_people_metavido.vfx` | Metavido | Body-mapped flames |
| `VFX/Rcam3/flame_depth_people_rcam3.vfx` | Rcam3 | NDI-style flames |
| `VFX/Rcam4/Body/flame_depth_people_rcam4.vfx` | Rcam4 | Modern flames |

### Sparkles Effects (3)
| File | Origin | Style |
|------|--------|-------|
| `Resources/VFX/sparkles_depth_people_metavido.vfx` | Metavido | Basic sparkles |
| `VFX/Rcam3/sparkles_depth_people_rcam3.vfx` | Rcam3 | NDI sparkles |
| `VFX/Rcam4/Body/sparkles_depth_people_rcam4.vfx` | Rcam4 | Modern sparkles |

### Voxels Effects (3)
| File | Origin | Style |
|------|--------|-------|
| `Resources/VFX/voxels_depth_people_metavido.vfx` | Metavido | Metavido voxels |
| `VFX/Rcam2/BodyFX/voxel_depth_people_rcam2.vfx` | Rcam2 | HDRP-converted |
| `VFX/Rcam4/Body/voxels_depth_people_rcam4.vfx` | Rcam4 | Modern voxels |

### Bubble Effects (4)
| File | Origin | Description |
|------|--------|-------------|
| `Resources/VFX/bubble_depth_people_metavido.vfx` | Metavido | Single bubble style (174KB) |
| `Resources/VFX/bubbles_depth_people_metavido.vfx` | Metavido | Multiple bubbles (135KB) |
| `VFX/Rcam2/BodyFX/bubble_depth_people_rcam2.vfx` | Rcam2 | HDRP-converted |
| `VFX/Rcam4/Body/bubbles_depth_people_rcam4.vfx` | Rcam4 | Modern bubbles |

---

## 6. SAMPLE TEMPLATES (Optional Delete)

**Location**: `Assets/Samples/Visual Effect Graph/17.2.0/`

**Count**: 37 VFX files

**Categories**:
- Learning Templates (28): Context&Flow, SpawnContext, Capacity, etc.
- VisualEffectGraph Additions (5): Bonfire, Flames, Lightning, Smoke, Sparks

**Recommendation**:
- Delete if not using for reference
- Keep if learning VFX Graph techniques
- These are reinstallable via Package Manager

---

## 7. PROCEDURAL VFX LIBRARY (Optional Delete)

**Location**: `Assets/__Procedural VFX Library 1.0 Prototypes/`

**Files**:
- `LissajousPlex 2.vfx`
- `Plexus 1.vfx`
- `Ribbon 1.vfx`

**Recommendation**: Delete if not actively using - these are experimental sketches

---

## Cleanup Commands

```bash
# Delete exact duplicate
rm "Assets/VFX/Rcam4/Environment/grid_environment_rcam4.vfx"
rm "Assets/VFX/Rcam4/Environment/grid_environment_rcam4.vfx.meta"

# Delete accidental copy
rm "Assets/Resources/VFX/pointcloud_depth_people_metavido 1.vfx"
rm "Assets/Resources/VFX/pointcloud_depth_people_metavido 1.vfx.meta"

# Optional: Delete samples (reinstallable via Package Manager)
rm -rf "Assets/Samples/Visual Effect Graph"

# Optional: Delete experimental library
rm -rf "Assets/__Procedural VFX Library 1.0 Prototypes"
```

---

## VFX Unique to Resources (Runtime Only)

These VFX only exist in Resources/, created specifically for runtime loading:

| VFX | Size | Purpose |
|-----|------|---------|
| `bubble_depth_people_metavido.vfx` | 174KB | Single bubble effect |
| `bubbles_depth_people_metavido.vfx` | 135KB | Multi-bubble effect |
| `flame_depth_people_metavido.vfx` | ~100KB | Body flame effect |
| `glitch_depth_people_metavido.vfx` | ~100KB | Glitch effect |
| `pointcloud_depth_people_metavido.vfx` | 290KB | Point cloud effect |
| `rcam3flame_depth_people_metavido.vfx` | ~100KB | Rcam3-style flame |
| `rcam3sparkles_depth_people_metavido.vfx` | ~100KB | Rcam3-style sparkles |
| `sparkles_depth_people_metavido.vfx` | ~100KB | Basic sparkles |
| `trails_depth_people_metavido.vfx` | ~100KB | Trail effect |

These are **not duplicates** - they're the primary runtime-loadable versions.

---

## Final Recommendation

**Must Delete** (redundant):
1. `VFX/Rcam4/Environment/grid_environment_rcam4.vfx` - exact duplicate
2. `Resources/VFX/pointcloud_depth_people_metavido 1.vfx` - accidental copy

**Consider Deleting** (optional):
3. 37 sample VFX templates (reinstallable)
4. 3 experimental VFX prototypes

**Keep Everything Else** - different implementations serving different purposes.
