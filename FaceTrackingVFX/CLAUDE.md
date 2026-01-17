# FaceTracking-VFX - ARKit Face Tracking + VFX Graph

**Source**: [mao-test-h/FaceTracking-VFX](https://github.com/mao-test-h/FaceTracking-VFX)
**Unity Version**: 2019.1.0f2 (Legacy - needs upgrade)
**License**: MIT

---

## Overview

ARKit face tracking data driving VFX Graph particle effects. Demonstrates mesh-to-VFX workflow using keijiro's Smrvfx pattern.

---

## Key Concept

```
ARKit Face Mesh → Smrvfx Bake → VFX Graph → Particles
```

Face mesh vertices are baked to textures (position, normal) and sampled in VFX Graph for particle emission from face geometry.

---

## Dependencies (2019 versions)

- LWRP 5.7.2 (now URP)
- Visual Effect Graph preview-5.13.0
- Unity ARKit Plugin

---

## Key References

| Repo | Purpose |
|------|---------|
| [keijiro/Smrvfx](https://github.com/keijiro/Smrvfx) | Skinned mesh → VFX baking |
| Unity-ARKit-Plugin | ARKit face tracking |

---

## Upgrade Notes

This project uses Unity 2019.1 and legacy ARKit plugin. To use with modern Unity:

1. Upgrade to Unity 2022.3+ / Unity 6
2. Replace LWRP → URP
3. Replace Unity-ARKit-Plugin → AR Foundation
4. Update VFX Graph to current version
5. Use ARFaceManager instead of legacy ARKit

---

## Integration Pattern

### Face Mesh → VFX (Smrvfx pattern)
```csharp
// Bake face mesh to position texture
Graphics.Blit(faceMesh.vertices, positionRT);
// Sample in VFX Graph
vfxGraph.SetTexture("PositionMap", positionRT);
```

---

## Related Projects

- `MetavidoVFX-main/` - Modern VFX pipeline
- `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`
