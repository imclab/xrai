# ONNX Runtime Unity Examples

**Repository**: https://github.com/asus4/onnxruntime-unity-examples
**Package**: `com.github.asus4.onnxruntime.unity` (OpenUPM: Keijiro registry)
**Unity Version**: 6000.0.24f1+
**Render Pipeline**: URP

---

## Overview

Production-ready ONNX Runtime integration for Unity, featuring optimized inference pipelines for computer vision tasks including object detection, segmentation, and image processing.

### Key Features
- Cross-platform ONNX model execution
- GPU acceleration via DirectML (Windows), CoreML (iOS/macOS), NNAPI (Android)
- VirtualTextureSource abstraction for camera/video input
- Pre-built examples for common ML tasks

---

## Available Examples

| Example | Model | Purpose | FPS |
|---------|-------|---------|-----|
| **MobileOne** | MobileOne-S0 | Image classification | 60+ |
| **Yolox** | YOLOX-Nano | Object detection | 30+ |
| **RT-DETRv2** | RT-DETRv2 | Real-time detection | 25+ |
| **NanoSAM** | NanoSAM | Segment Anything (mobile) | 15+ |
| **Yolo11-Seg** | YOLO11n-seg | Instance segmentation | 20+ |
| **MyakuMyaku** | YOLO11n-seg | AR character overlay | 20+ |

---

## Architecture

### VirtualTextureSource Pattern

```
[Camera/Video] → VirtualTextureSource → BaseTextureSource
                                              ↓
                               [ML Controller] ← ONNX Model
                                              ↓
                               [VFX/Renderer] ← Output Textures
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `VirtualTextureSource` | Abstraction for texture input sources |
| `BaseTextureSource` | ScriptableObject defining texture source |
| `ARFoundationTextureSource` | AR camera feed integration |
| `WebCamTextureSource` | Webcam input for Editor testing |
| `VideoTextureSource` | Video file playback |

---

## YOLO11 Segmentation Pipeline

### Components

```csharp
// Core inference
Yolo11SegARController : MonoBehaviour
├── VirtualTextureSource inputSource  // Camera input
├── RenderTexture colorTex            // RGB output
├── RenderTexture segmentationTex     // Mask output
├── YOLO11Segmenter model             // ONNX inference
└── int maxDetections = 10            // Detection limit

// VFX binding
MainController : MonoBehaviour
├── VisualEffect vfx                  // VFX Graph target
├── postVfxMaterial                   // Post-process material
└── void SetTextures(color, seg)      // Bind outputs
```

### Segmentation Output Format

- **colorTex**: RGB camera frame (resized to model input)
- **segmentationTex**: Per-pixel class mask (0-80 COCO classes)
- **Bounding boxes**: Normalized [x, y, w, h] + confidence + class

---

## Installation

### Package Manager

```json
// manifest.json scopedRegistries
{
  "name": "Keijiro",
  "url": "https://registry.npmjs.com",
  "scopes": ["jp.keijiro", "com.github.asus4"]
}

// dependencies
"com.github.asus4.onnxruntime": "0.3.2",
"com.github.asus4.onnxruntime.unity": "0.3.2",
"com.github.asus4.texture-source": "0.3.4"
```

### Model Setup

1. Download ONNX model (e.g., `yolo11n-seg-dynamic.onnx`)
2. Place in `Assets/StreamingAssets/`
3. Reference in controller component

---

## Platform-Specific Configuration

### iOS (CoreML)
```csharp
var options = new SessionOptions();
options.AppendExecutionProvider_CoreML(CoreMLFlags.COREML_FLAG_ENABLE_ON_SUBGRAPH);
```

### Android (NNAPI)
```csharp
var options = new SessionOptions();
options.AppendExecutionProvider_NNAPI();
```

### Windows (DirectML)
```csharp
var options = new SessionOptions();
options.AppendExecutionProvider_DML(0); // GPU device index
```

---

## VFX Integration Pattern

### Binding Segmentation to VFX Graph

```csharp
public class SegmentationVFXBinder : MonoBehaviour
{
    [SerializeField] VisualEffect vfx;
    [SerializeField] Yolo11SegARController segController;

    static readonly int SegmentationTex = Shader.PropertyToID("_SegmentationTex");
    static readonly int ARRgbDTex = Shader.PropertyToID("_ARRgbDTex");

    void LateUpdate()
    {
        if (segController.segmentationTex != null)
        {
            vfx.SetTexture(SegmentationTex, segController.segmentationTex);
            vfx.SetTexture(ARRgbDTex, segController.colorTex);
        }
    }
}
```

### VFX Graph Sample Texture

```hlsl
// In VFX Graph operator
float4 segColor = SampleTexture2D(_SegmentationTex, uv);
float classId = segColor.r * 255.0; // Class 0-80
bool isTarget = classId == 0; // Person class
```

---

## MyakuMyaku AR Example

### Features
- YOLO11 person segmentation
- Real-time AR overlay
- VFX particle spawning on detected regions
- Mobile-optimized inference

### Scene Setup
```
ARSessionOrigin
├── AR Camera
│   ├── VirtualTextureSource
│   └── Yolo11SegARController
├── MainController
│   └── MyakuMyaku_VFX
└── Canvas
    ├── FpsCounter
    └── DetectionUI
```

---

## Performance Optimization

### Memory
- Use dynamic input shapes for variable resolutions
- Dispose tensors immediately after use
- Pool RenderTextures for output

### Speed
- Prefer GPU execution providers (CoreML, NNAPI, DirectML)
- Quantize models to INT8/FP16
- Reduce input resolution (224x224 → 160x160)

### Threading
- Run inference on background thread
- Use async readback for GPU→CPU transfer
- Double-buffer output textures

---

## Related KB Files

- `_UNITY_SENTIS_ML_INFERENCE.md` - Unity's native inference engine
- `_YOLO_SEGMENTATION_UNITY.md` - YOLO-specific patterns
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR + VFX integration

---

## References

- [ONNX Runtime Documentation](https://onnxruntime.ai/docs/)
- [asus4/onnxruntime-unity](https://github.com/asus4/onnxruntime-unity)
- [YOLO11 Models](https://docs.ultralytics.com/models/yolo11/)
- [VirtualTextureSource](https://github.com/asus4/TextureSource)

---

*Created: 2026-01-20*
*Source: github.com/asus4/onnxruntime-unity-examples*
