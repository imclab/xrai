# MyakuMyaku VFX Pipeline

**Source**: [keijiro/MyakuMyakuAR](https://github.com/keijiro/MyakuMyakuAR)
**Migrated**: 2026-01-20

## Pipeline Comparison

| Aspect | Original | MetavidoVFX |
|--------|----------|-------------|
| **Detection** | YOLO11 Segmentation | AR Foundation StencilMap |
| **Target** | MyakuMyaku mascot | Human body |
| **Runtime** | ONNX Runtime | Unity native |
| **Model** | yolo11n-seg.onnx (~6MB) | ARKit/ARCore built-in |
| **Latency** | ~30-50ms | ~16ms |

## Data Flow

```
┌─────────────────────────────────────────────────┐
│              AR Foundation                       │
│   AROcclusionManager → StencilMap (human mask)  │
│   ARCameraManager → ColorMap (RGB)              │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│            ARDepthSource (singleton)            │
│   Exposes: StencilMap, ColorMap, PositionMap    │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│            MyakuMyakuBinder (per-VFX)           │
│   _SegmentationTex ← StencilMap                 │
│   _ARRgbDTex ← ColorMap                         │
│   _SpawnUvMinMax ← spawn bounds                 │
│   _SpawnRate ← coverage estimate                │
└─────────────────────┬───────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────┐
│         myakumyaku_ar_myaku.vfx                 │
│   Spawns metaball particles on segmented area   │
│   Uses MatCap shaders for character rendering   │
└─────────────────────────────────────────────────┘
```

## VFX Properties

| Property | Type | Description |
|----------|------|-------------|
| `_SegmentationTex` | Texture2D | Segmentation mask (white = spawn area) |
| `_ARRgbDTex` | Texture2D | Camera color for sampling |
| `_SpawnUvMinMax` | Vector4 | UV bounds (xMin, yMin, xMax, yMax) |
| `_SpawnRate` | float | Particle spawn rate (0-1) |

## Shader Dependencies

Located in `Shaders/`:
- `SG_Metaball2DPass.shadergraph` - Main metaball rendering
- `SG_MetaballPrepass.shadergraph` - Depth prepass
- `SG_EyeSphere.shadergraph` - Eye rendering
- `SGS_MatCap_subGraph.shadersubgraph` - MatCap lighting
- `M_Metaball2DPass.mat` - Material instance

## Textures

Located in `Textures/`:
- MatCap color ramps (hex-named .png files)
- `MatCapNormal.png` - Normal map for lighting

## Usage

```csharp
// Auto-setup (recommended)
// MyakuMyakuBinder auto-finds ARDepthSource

// Manual control
var binder = GetComponent<MyakuMyakuBinder>();
binder.SetSpawnBounds(new Rect(0.2f, 0.2f, 0.6f, 0.6f));
binder.SetSpawnRate(0.5f);
```

## Adding YOLO11 Detection (Optional)

To detect MyakuMyaku mascot instead of humans:

1. Add ONNX Runtime: `com.microsoft.onnxruntime.unity`
2. Copy from `_ref/MyakuMyakuAR-main-plantblobs/Assets/Scripts/Runtime/ObjectDetection/`
3. Add YOLO11 model to StreamingAssets
4. Replace `MyakuMyakuBinder` with `Yolo11SegARController`

## Performance

- Human segmentation: ~16ms (AR Foundation native)
- YOLO11 segmentation: ~30-50ms (ML inference)
- VFX rendering: ~2-3ms

Recommended: Use AR Foundation approach for mobile performance.

---

## YOLO11 Detection System (Added)

### Package Dependencies

```json
// manifest.json
"com.github.asus4.onnxruntime": "0.3.2",
"com.github.asus4.onnxruntime.unity": "0.3.2",
"com.github.asus4.texture-source": "0.3.4"
```

### Scripts (Assets/Scripts/ObjectDetection/)

| File | Purpose |
|------|---------|
| `Yolo11Seg.cs` | Core YOLO11 segmentation inference |
| `Yolo11SegARController.cs` | AR camera → YOLO → VFX pipeline |
| `Yolo11SegVisualize.cs` | Debug visualization |
| `VisualizeSegmentations.compute` | GPU segmentation rendering |
| `RemoteFile.cs` | Downloads ONNX model at runtime |
| `labels-coco.txt` | COCO class labels |

### Model

- **URL**: `https://github.com/asus4/onnxruntime-unity-examples/releases/download/v0.2.7/yolo11n-seg-dynamic.onnx`
- **Size**: ~6MB (downloaded on first run)
- **Classes**: 80 COCO classes (person, car, dog, etc.)

### Usage

```csharp
// Add to AR Camera GameObject
var controller = gameObject.AddComponent<Yolo11SegARController>();
controller.OnDetect += (ctrl) => {
    // Access segmentation mask
    Texture mask = ctrl.SegmentationTexture;
    // Access detections
    var detections = ctrl.Detections;
};
```

### Key Learning: ONNX vs Sentis

| Aspect | ONNX Runtime | Unity Sentis |
|--------|--------------|--------------|
| **Package** | com.github.asus4.onnxruntime | com.unity.ai.inference |
| **Model Format** | .onnx | .sentis / .onnx |
| **GPU Backend** | CoreML/NNAPI/DirectML | Unity Compute |
| **Flexibility** | Any ONNX model | Unity-optimized |
| **Size** | ~15MB runtime | Built into Unity |

For new projects, prefer **Unity Sentis** when possible.

---

## VFX Binder Options

| Binder | Source | Use Case |
|--------|--------|----------|
| `MyakuMyakuBinder` | ARDepthSource (StencilMap) | Human body particles |
| `Yolo11VFXBinder` | Yolo11SegARController | Object detection particles |

### MyakuMyakuBinder (Default)
Uses AR Foundation human segmentation. Fast (~16ms), no ML download.

### Yolo11VFXBinder (Optional)
Uses YOLO11 object detection. Downloads model on first run (~6MB).

```csharp
// Switch binder on existing VFX
var vfx = GetComponent<VisualEffect>();
Destroy(GetComponent<MyakuMyakuBinder>());
gameObject.AddComponent<Yolo11VFXBinder>();
```

---

## ONNX Importer Conflict

Unity Sentis and ONNX Runtime both target `.onnx` files:
- **Sentis** (.onnx) - BodyPix, Unity ML models
- **ONNX Runtime** - YOLO11 (downloads at runtime, no import needed)

The warning is safe to ignore since YOLO11 downloads models at runtime.
