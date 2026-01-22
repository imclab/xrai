# Gaussian Splatting & Visualization Tools

## Gaussian Splatting Implementations

### keijiro/SplatVFX (Unity VFX Graph)
**URL**: https://github.com/keijiro/SplatVFX
**Platform**: Unity (HDRP)
**Status**: Experimental

3D Gaussian Splatting rendered via Unity VFX Graph.

**Key Features**:
- Up to 8 million points (configurable)
- Uses `.splat` file format
- VFX Graph-based rendering

**Limitations**:
- sRGB color space artifacts in Linear Lighting Mode
- Gaussian projection algorithm causes pops with camera motion
- Author recommends UnityGaussianSplatting for production

**Usage**:
```bash
# Get sample .splat file
wget https://huggingface.co/cakewalk/splat-data/resolve/main/bicycle.splat
# Place in URP/Assets directory
```

**Convert PLY to SPLAT**:
Use [antimatter15/splat](https://github.com/antimatter15/splat) WebGL viewer - drag & drop PLY file.

---

### aras-p/UnityGaussianSplatting (Windows)
**URL**: https://github.com/aras-p/UnityGaussianSplatting
**Platform**: Windows only (DX12)
**Status**: Production-ready

Full-featured Gaussian Splatting for Unity by Aras Pranckevičius (Unity graphics veteran).

**Key Features**:
- High-quality rendering
- Better performance than VFX Graph approach
- Proper sorting and blending
- Editor tools for .ply import

**Note**: Windows/DX12 only - not compatible with macOS or mobile.

---

### playcanvas/supersplat (Web Editor)
**URL**: https://github.com/playcanvas/supersplat
**Live Editor**: https://superspl.at/editor
**Gallery**: https://superspl.at/

Free, open-source web tool for inspecting, editing, optimizing and publishing 3D Gaussian Splats.

**Features**:
- Browser-based (no install)
- Edit and optimize splats
- VR-ready exports
- Multi-language support
- Built on PlayCanvas engine

**Local Development**:
```bash
git clone https://github.com/playcanvas/supersplat.git
cd supersplat
npm install
npm run develop
# Open http://localhost:3000
```

**Popular Splats on superspl.at**:
- Garage v2 (VR Ready)
- Auckland Domain winter garden
- Botanical Garden series
- Various architectural scans

---

## Academic Influence Visualization

### csmetrics/influencemap (ANU)
**URL**: https://github.com/csmetrics/influencemap
**Purpose**: Visualize academic citation influence as "influence flowers"

**Concept**:
- Ego node (center) = entity being analyzed
- Leaf nodes = most influential entities
- Red edges = influence OUT (ego cited by others)
- Blue edges = influence IN (ego cites others)
- Edge thickness = influence strength

**Data Source**: OpenAlex (2025-05-30 release)
- Previously used Microsoft Academic Graph (deprecated)

**Entity Types**:
1. Author outer nodes
2. Venue (conferences/journals)
3. Author Affiliation
4. Paper topic

**Influence Calculation**:
- Citations weighted by number of authors in cited paper
- Self-citations can be filtered
- Co-authors shown with greyed names

**Citation**:
```
Minjeong Shin, et al. "Influence Flowers of Academic Entities"
IEEE VAST 2019
```

---

## Unity Intelligence Patterns

### unity-intelligence-ultimate.md
**Location**: `~/Downloads/unity-intelligence-ultimate.md`
**Purpose**: 500+ Unity repository patterns consolidated

**Key Patterns Included**:

#### ARFoundation + VFX
```csharp
// Human Depth to VFX
public class HumanDepthVFX : MonoBehaviour {
    [SerializeField] AROcclusionManager occlusionManager;
    [SerializeField] VisualEffect vfxGraph;

    void Update() {
        if (occlusionManager.humanDepthTexture != null) {
            vfxGraph.SetTexture("HumanDepth", occlusionManager.humanDepthTexture);
        }
    }
}
```

#### Platform-Specific Particle Limits
```csharp
public static int GetMaxParticles() {
    #if UNITY_IOS
        return 500000-750000;
    #elif UNITY_ANDROID // Quest
        return 500000-1000000;
    #elif UNITY_STANDALONE_OSX // M3 Max
        return 2000000;
    #elif UNITY_VISIONOS
        return 750000;
    #endif
}
```

#### DOTS Million Particles
```csharp
[BurstCompile]
public partial struct ParticleSystem : ISystem {
    public void OnUpdate(ref SystemState state) {
        new ParticleUpdateJob {
            deltaTime = SystemAPI.Time.DeltaTime
        }.ScheduleParallel();
    }
}
```

**Required Packages**:
```json
{
  "com.unity.xr.arfoundation": "5.1.5",
  "com.normalcore.normcore": "2.16.2",
  "com.unity.entities": "1.3.5",
  "com.unity.burst": "1.8.18",
  "com.unity.visualeffectgraph": "17.0.3"
}
```

---

## Comparison Table

| Tool | Platform | Use Case | Points | Status |
|------|----------|----------|--------|--------|
| SplatVFX | Unity (HDRP) | Experimental/Learning | 8M | Experimental |
| UnityGaussianSplatting | Windows/DX12 | Production | High | Production |
| SuperSplat | Web | Edit/Optimize/Publish | N/A | Production |
| InfluenceMap | Web | Academic visualization | N/A | Research |

---

## Related KB Files

- `_3D_VISUALIZATION_KNOWLEDGEBASE.md` - Force layouts, city layouts
- `_WEBGPU_THREEJS_2025.md` - WebGPU Gaussian splatting (Visionary)
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - ARFoundation patterns
- `_UNITY_MCP_DEV_HOOKS.md` - Unity MCP tools

---

*Updated: 2026-01-13*
