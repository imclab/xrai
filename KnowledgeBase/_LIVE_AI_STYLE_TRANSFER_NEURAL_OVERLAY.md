# Live AI Style Transfer / Neural Overlay Research

**Tags**: AI, AR, Sentis, ONNX, Style Transfer, Neural Filters
**Source**: Team research May 2025

---

## Overview

Real-time neural style transfer and AR neural filters using Unity Sentis, ARFoundation, and ONNX models. Pipeline: ARF→Sentis/ONNX→Optimize.

---

## Key Repositories

### Doji Technologies (julienkay) - "Keijiro of Sentis"

| Repo | Purpose |
|------|---------|
| `com.doji.diffusers` | Stable Diffusion with Sentis |
| `com.doji.transformers` | Transformer models with Sentis |
| `com.doji.sentis-utils` | Utility scripts for Sentis |
| `com.doji.mobilesam` | Fast segmentation (MobileSAM) |
| `com.doji.midas` | Monocular depth estimation |
| `com.doji.neutron` | Neural network viewer |
| `com.doji.genesis` | Genesis physics simulation |
| `com.doji.snerg` | SNeRG web viewer port |
| `MobileNeRF-Unity-Viewer` | MobileNeRF port |

### Unity Official

| Repo | Purpose |
|------|---------|
| `Unity-Technologies/sentis-samples` | Official Sentis examples |
| `Unity-Technologies/sentis-edge-detect` | Edge detection sample |
| `Unity-Technologies/arfoundation-samples` | AR camera access |

### Other Notable

| Repo | Purpose |
|------|---------|
| `huailiang/nnStyle` | Neural style transfer for Unity |
| `inferenceengine/shadernn` | Mobile neural compute shaders |
| `asus4/tf-lite-unity-sample` | TensorFlow Lite samples |
| `RageAgainstThePixel/com.rest.meshy` | Meshy REST API client |
| `Blockade-Games/BlockadeLabs-SDK-Unity` | Skybox AI SDK |
| `CoplayDev/unity-mcp` | Unity MCP server |

---

## Live Style Transfer Pipeline

### Architecture
```
[AR Camera] → ARCameraManager.frameReceived
     ↓
[XRCpuImage] → Convert to RGBA Texture2D
     ↓
[Compute Shader] → Resize + Normalize (256x256)
     ↓
[Sentis Worker] → Style Transfer ONNX Model
     ↓
[Output Tensor] → TextureConverter.RenderToTexture
     ↓
[URP Render Feature] → Final composite
```

### Core Components

**1. ARCameraFrameProvider** - Captures AR camera frames
```csharp
m_CameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)
image.Convert(conversionParams, buffer)
cameraTexture.LoadRawTextureData(buffer)
```

**2. StyleTransferProcessor** - Runs Sentis inference
```csharp
model = ModelLoader.Load(modelAsset);
worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);
inputTensor = TextureConverter.ToTensor(sourceTexture);
worker.Execute(inputTensor);
outputTensor = worker.PeekOutput();
TextureConverter.RenderToTexture(outputTensor, outputTexture);
```

**3. ImagePreprocessor** - GPU resize/normalize via compute
```csharp
preprocessShader.Dispatch(resizeKernel, w/8, h/8, 1);
preprocessShader.Dispatch(normalizeKernel, w/8, h/8, 1);
```

**4. StyleTransferRenderFeature** - URP integration
```csharp
cmd.Blit(styleProcessor.outputTexture, tempTexture.Identifier());
cmd.Blit(tempTexture.Identifier(), cameraColorTarget, material);
```

---

## ONNX Style Transfer Models

### Pre-trained (Ready to Use)
- **ONNX Fast Neural Style**: Mosaic, Candy, Rain Princess, Udnie
  - Input: `[1, 3, H, W]` RGB normalized [0,1]
  - Output: `[1, 3, H, W]` RGB [0,1]
  - Source: `github.com/onnx/models/tree/main/vision/style_transfer`

### Mobile-Optimized
- **MobileStyleGAN**: 2-4MB (vs 10-15MB standard)
- **Lite Style Transfer**: 4-5x smaller, minimal quality loss

---

## Performance Optimization

### Device Capability Detection
```csharp
public static BackendType GetOptimalBackendType()
{
    if (SystemInfo.supportsComputeShaders && IsHighEndDevice())
        return BackendType.GPUCompute;
    return BackendType.CPU;
}

public static int GetRecommendedProcessingResolution()
{
    return IsHighEndDevice() ? 384 : 256;
}

public static int GetRecommendedFrameSkip()
{
    return IsHighEndDevice() ? 1 : 3;
}
```

### Optimization Techniques

| Category | Technique |
|----------|-----------|
| Model | Quantization (INT8/FP16), pruning, MobileNet-based |
| Processing | Frame skipping, reduced input resolution (256x256) |
| Rendering | URP quality levels, disable AA on low-end |
| Memory | Proper tensor/texture disposal, object pooling |
| Battery | Power-saving mode, pause on background |

### Android-Specific
- Use `UnityWebRequest` for StreamingAssets models
- Or use `Model.Loader(asset)` with AssetModel field

---

## URP Render Features

### Documentation Links
- [URP Renderer Features](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/urp-renderer-feature.html)
- [Custom Renderer Features](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/rendererfeatures/create-custom-renderer-feature.html)
- [Render Objects Effect](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/rendererfeatures/how-to-custom-effect-render-objects.html)
- [Render Graph Import Texture](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/render-graph-import-a-texture.html)

### Example: keijiro/KinoFeedbackURP
Feedback effect as URP render feature.

---

## OpenUPM AI Packages

Browse: https://openupm.com/packages/topics/ai/?sort=downloads

---

## Related Tools

### AI Generation Services
- **Skybox AI** (Blockade Labs): One-click 360 image generation
- **Meshy API**: Text/image to 3D mesh generation

### MCP Integration
- `justinpbarnett/unity-mcp`: MCP server for Claude/Cursor
- `CoderGamester/mcp-unity`: Alternative MCP implementation

---

## Unity Sentis Roadmap
https://unity.com/roadmap#unity-ai-sentis

---

## Related KB Files

- `_UNITY_SENTIS_ML_INFERENCE.md` - Sentis fundamentals
- `_ONNXRUNTIME_UNITY_EXAMPLES.md` - ONNX Runtime patterns
- `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR + VFX integration

---

*Created: 2026-01-20*
*Source: Team research document, May 2025*
