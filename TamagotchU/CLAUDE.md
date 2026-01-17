# TamagotchU - Interactive Virtual Pet Installation

**Source**: [EyezLee/TamagotchU_Unity](https://github.com/EyezLee/TamagotchU_Unity)
**Installations**: Mutek Festival 2025, Signals Festival XR Lounge (VIFF)
**Companion App**: [tama_pager](https://github.com/EyezLee/tama_pager)
**License**: MIT

---

## Overview

Interactive virtual pet art installation combining ML-Agents, Spine animation, dynamic bone physics, and VFX. Designed for festival/gallery exhibition with companion pager app.

---

## Project Structure

```
TamagotchU/
├── Assets/
│   ├── AI Toolkit/              # Unity AI Assistant integration
│   ├── Audio/                   # Sound effects
│   ├── DynamicBone/             # Physics bone simulation
│   ├── Materials/               # Materials
│   ├── Meshes/                  # 3D models
│   ├── ML_Models/               # Trained ML models (.onnx)
│   ├── ML-Agents/               # ML-Agents config
│   ├── Prefabs/                 # Prefab assets
│   ├── RayFire/                 # Destruction physics
│   ├── Samples/                 # Sample scenes
│   ├── Scenes/
│   │   ├── ML_Turtle.unity      # Main ML scene
│   │   └── Spine/               # Spine animation samples
│   ├── Script/                  # C# scripts
│   ├── Settings/                # Project settings
│   ├── Shaders/                 # Custom shaders
│   ├── Textures/                # Texture assets
│   ├── VatBakerOutput/          # Baked vertex animations
│   ├── VFX/                     # VFX Graph effects
│   └── Videos/                  # Video assets
│
├── Packages/
│   ├── com.unity.ml-agents/     # Local ML-Agents package
│   └── manifest.json
│
└── ProjectSettings/
```

---

## Key Technologies

| Technology | Purpose |
|------------|---------|
| **ML-Agents** | Reinforcement learning for creature behavior |
| **Spine Runtime** | 2D skeletal animation (4.3-beta) |
| **Dynamic Bone** | Secondary motion physics |
| **VATBaker** | Vertex Animation Textures for GPU animation |
| **RayFire** | Destruction/fracture physics |
| **Unity AI Assistant** | AI-powered development tools |
| **NoiseShader** | Keijiro's noise generation |

---

## Dependencies

| Package | Version | Source |
|---------|---------|--------|
| Spine C# | 4.3-beta | GitHub |
| Spine Unity | 4.3-beta | GitHub |
| Spine URP Shaders | 4.3-beta | GitHub |
| ML-Agents | local | Packages/ |
| ML-Agents Extensions | local | Packages/ |
| Unity AI Assistant | 1.0.0-pre.8 | Unity |
| Unity AI Generators | 1.0.0-pre.15 | Unity |
| URP | 17.2.0 | Unity |
| VFX Graph | 17.2.0 | Unity |
| Animation Rigging | 1.3.0 | Unity |
| VATBaker | 1.0.5 | fuqunaga registry |
| NoiseShader | 3.0.0 | Keijiro registry |
| Input System | 1.14.0 | Unity |

---

## Scenes

### ML_Turtle.unity
Main scene with ML-trained turtle creature.

### Spine Samples (20+ scenes)
- `Goblins.unity` - Character animation
- `Mix and Match.unity` - Equipment system
- `SkeletonUtility Ragdoll.unity` - Ragdoll physics
- `Physics Constraints.unity` - Spine physics
- `VertexEffect.unity` - Shader effects
- `BlendModes.unity` - Material blending

---

## ML-Agents Integration

```
Assets/ML_Models/       # Trained .onnx models
Assets/ML-Agents/       # Training configuration
Packages/com.unity.ml-agents/  # Local ML-Agents package
```

The project uses local ML-Agents packages for custom modifications.

---

## VATBaker Workflow

Vertex Animation Texture baking for GPU-accelerated animation:
1. Animate mesh in Unity
2. Use VATBaker to export position/normal textures
3. Apply VAT shader for playback
4. Output in `Assets/VatBakerOutput/`

---

## Dynamic Bone Setup

Secondary motion system for organic movement:
- Hair, tails, clothing physics
- No rigid body required
- Performance optimized for mobile

---

## Quick Start

1. Open `Assets/Scenes/ML_Turtle.unity`
2. Enter Play Mode
3. Observe ML-trained behavior
4. Build for Windows (`.exe` release)

---

## Festival Setup

- Windows build with executable
- Companion pager app for interaction
- Network communication between installations

---

## Patterns for MetavidoVFX

### VAT Shader Pattern
Useful for pre-baking complex animations to texture for VFX Graph.

### ML-Agent → VFX Binding
Connect ML behavior outputs to VFX Graph properties.

### Spine + VFX Integration
Layer VFX effects on 2D skeletal animation.

---

## Related KB Files

- `KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` - ML-Agents repos
- `KnowledgeBase/_UNITY_INTELLIGENCE_PATTERNS.md` - ML patterns
