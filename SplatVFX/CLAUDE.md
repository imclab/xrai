# SplatVFX - Gaussian Splatting for VFX Graph

**Source**: [keijiro/SplatVFX](https://github.com/keijiro/SplatVFX)
**Author**: Keijiro Takahashi
**License**: MIT

---

## Overview

Experimental implementation of 3D Gaussian Splatting with Unity VFX Graph. Renders `.splat` files (converted from `.ply` point clouds) as VFX particles with Gaussian projection.

---

## Project Structure

```
SplatVFX/
├── jp.keijiro.splat-vfx/    # UPM Package
│   ├── Editor/              # SplatImporter, SplatDataInspector
│   ├── Runtime/             # SplatData, SplatDataBinder
│   ├── Shaders/             # Gaussian.shadergraph
│   └── VFX/                 # Splat.vfx + custom blocks/operators
│
├── URP/                     # URP Sample Project
│   ├── Assets/
│   │   ├── Test.unity       # Main test scene
│   │   ├── Misc/            # ProjectionTest, Marker VFX
│   │   └── URP/             # URP pipeline assets
│   ├── Packages/
│   └── ProjectSettings/
│
├── README.md
└── LICENSE
```

---

## Key Components

| File | Purpose |
|------|---------|
| `SplatData.cs` | ScriptableObject holding point cloud data |
| `SplatDataBinder.cs` | VFX Graph property binder for SplatData |
| `SplatImporter.cs` | Imports `.splat` files as SplatData assets |
| `Splat.vfx` | Main VFX Graph (8M point capacity) |
| `Gaussian.shadergraph` | Shader for Gaussian ellipsoid rendering |

### VFX Graph Blocks

- `InitializeSplat.vfxblock` - Initialize particles from splat data
- `ProjectSplat.vfxblock` - Project 3D Gaussians to screen
- `SampleSplatAxes.vfxoperator` - Sample ellipsoid axes
- `SelectMajorAxes.vfxoperator` - Sort axes by size

---

## Quick Start

1. Download [bicycle.splat](https://huggingface.co/cakewalk/splat-data/resolve/main/bicycle.splat)
2. Place in `URP/Assets/`
3. Open `URP/Assets/Test.unity`
4. Enter Play Mode

---

## Creating .splat Files

1. Train Gaussian splatting model → get `.ply` file
2. Open [WebGL Gaussian Splat Viewer](https://github.com/antimatter15/splat)
3. Drag & drop `.ply` → downloads `.splat`

---

## Increasing Capacity

Default: 8 million points. To increase:

1. Duplicate `Splat.vfx` to your project
2. Edit Initialize Particle context
3. Increase capacity value

Check point count on SplatData asset Inspector.

---

## Limitations

- **Color Space**: `.splat` files trained in sRGB may have artifacts in Linear mode
- **Projection**: VFX Graph algorithm causes artifacts (sudden pops with camera motion)
- **Experimental**: Author recommends [UnityGaussianSplatting](https://github.com/aras-p/UnityGaussianSplatting) for production

---

## Dependencies

- Unity 6 / URP 17
- VFX Graph 17
- jp.keijiro.klak.motion (Keijiro registry)

---

## Integration with MetavidoVFX

To use in MetavidoVFX-main:

```json
// Add to MetavidoVFX-main/Packages/manifest.json
{
  "scopedRegistries": [
    {
      "name": "Keijiro",
      "url": "https://registry.npmjs.com",
      "scopes": ["jp.keijiro"]
    }
  ],
  "dependencies": {
    "jp.keijiro.splat-vfx": "file:../../SplatVFX/jp.keijiro.splat-vfx"
  }
}
```

---

## Related KB Files

- `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - SplatVFX entry
- `KnowledgeBase/_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` - VFX patterns
