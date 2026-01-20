# Unity Sentis ML Inference Guide

**Package**: `com.unity.ai.inference` (formerly com.unity.sentis)
**Version**: 2.4.0+
**Documentation**: https://docs.unity3d.com/Packages/com.unity.ai.inference@2.4/manual/index.html

---

## Overview

Unity Sentis is the neural inference engine for Unity, enabling on-device AI model execution across all platforms.

### Key Capabilities
- GPU-accelerated inference via compute shaders
- Cross-platform support (iOS, Android, WebGL, Standalone, Quest)
- ONNX model import with automatic optimization
- Streaming/async inference for responsive applications
- Model quantization (INT8, FP16) for mobile optimization

---

## Package Samples

### Sample Projects (Unity 6+)

| Sample | Description | Key Technologies |
|--------|-------------|------------------|
| **Chat Sample** | Interactive multimodal LLM chat | LLaVA OneVision, text+image input |
| **Tensor Conversion** | Convert data to/from tensors | Blit, ComputeBuffer, ReadbackAndClone |
| **Model Quantization** | Reduce model size for mobile | INT8, FP16 quantization |
| **Async Readback** | Non-blocking inference results | AsyncReadbackRequest |
| **Custom Layers** | Extend Sentis with custom ops | IComputeFunctionParams |

### Installing Samples

```
Window > Package Manager > AI > Sentis > Samples > Import
```

---

## Chat Sample (Multimodal LLM)

### Architecture

```
[User Input] → [Tokenizer] → [Text Embedder]
                                    ↓
[Image Input] → [Vision Encoder] → [LLaVA Decoder] → [Response]
```

### Components

| Component | Purpose |
|-----------|---------|
| `LlavaRunner.cs` | Main inference orchestrator |
| `LlavaTokenizer.cs` | Text tokenization (BPE) |
| `LlavaEmbedder.cs` | Text → embedding vectors |
| `LlavaVisionEncoder.cs` | Image → visual features |
| `LlavaDecoder.cs` | Generate response tokens |
| `ModelScheduler.cs` | Async model execution |
| `HfDownloader.cs` | HuggingFace model download |

### Model Details

**LLaVA OneVision (0.5B parameters)**
- Source: `huggingface.co/llava-hf/llava-onevision-qwen2-0.5b-si-hf`
- Format: ONNX (quantized for mobile)
- Input: Text + optional image
- Output: Streaming text response

### Usage

```csharp
// Editor menu
Sentis > Sample > Chat > Download Models
Sentis > Sample > Chat > Start Chat

// Runtime scene
ChatSample/Assets/ChatLLM/Runtime/Scenes/Chat.unity
```

### Features
- Multimodal understanding (text + images)
- Streaming token-by-token responses
- Conversation history retention
- Redux-pattern state management
- Unity AppUI for modern interfaces

---

## Tensor Conversion Patterns

### Texture to Tensor

```csharp
using Unity.AI.Inference;

// CPU readback (slow, blocking)
var tensor = TextureConverter.ToTensor(texture);

// GPU tensor (fast, stays on GPU)
var gpuTensor = TextureConverter.ToTensor(texture, new TextureTransform()
    .SetDimensions(224, 224)
    .SetTensorLayout(TensorLayout.NHWC)
);
```

### Tensor to Texture

```csharp
// Blit to RenderTexture
TextureConverter.Blit(outputTensor, renderTexture);

// Create new Texture2D
var tex = TextureConverter.ToTexture(outputTensor);
```

### ComputeBuffer Interop

```csharp
// Direct buffer access (no copy)
var buffer = tensor.dataOnBackend.buffer;

// Copy to existing buffer
tensor.dataOnBackend.Download<float>(buffer);
```

---

## Model Optimization

### Quantization

```csharp
// Load and quantize
var model = ModelLoader.Load(modelPath);
var quantizedModel = ModelOptimizer.Quantize(model, QuantizationType.INT8);
```

### Memory Optimization

```csharp
// Use GPUCompute backend (fastest)
var worker = new Worker(model, BackendType.GPUCompute);

// Dispose after use
worker.Dispose();
```

### Batch Processing

```csharp
// Process multiple inputs
var batchTensor = new Tensor(new TensorShape(batchSize, 224, 224, 3));
worker.SetInput("input", batchTensor);
worker.Execute();
```

---

## Platform-Specific Notes

### iOS
- Metal compute shaders
- Use FP16 quantization for A-series chips
- Memory budget: ~1.5GB max model size

### Android
- Vulkan compute preferred
- NNAPI delegate optional
- Test on target GPU (Adreno, Mali, PowerVR)

### Quest
- Vulkan compute shaders
- FP16 required for 6 DoF tracking apps
- 4GB shared memory limit

### WebGL
- WebGPU required (Chrome 113+)
- Model size limited by memory
- Use smallest quantized models

---

## VFX Graph Integration

### Binding Inference Output to VFX

```csharp
public class SentisVFXBinder : MonoBehaviour
{
    public VisualEffect vfx;
    private Tensor outputTensor;

    void Update()
    {
        // Run inference
        worker.SetInput("input", inputTensor);
        worker.Execute();
        outputTensor = worker.PeekOutput("output");

        // Convert to texture for VFX
        TextureConverter.Blit(outputTensor, segmentationTexture);
        vfx.SetTexture("SegmentationMap", segmentationTexture);
    }
}
```

### Segmentation Mask to Particles

```csharp
// YOLO11 segmentation → VFX particle spawn
var maskTensor = worker.PeekOutput("segmentation_mask");
TextureConverter.Blit(maskTensor, maskTexture);
vfx.SetTexture("SpawnMask", maskTexture);
vfx.SetFloat("SegmentCount", detectedClasses);
```

---

## References

- [Sentis Package Manual](https://docs.unity3d.com/Packages/com.unity.ai.inference@2.4/manual/index.html)
- [Sentis GitHub Samples](https://github.com/Unity-Technologies/sentis-samples)
- [ONNX Model Zoo](https://github.com/onnx/models)
- [HuggingFace Hub](https://huggingface.co/models?library=onnx)

---

## Related KB Files

- `_ONNXRUNTIME_UNITY_EXAMPLES.md` - Alternative ONNX Runtime integration
- `_YOLO_SEGMENTATION_UNITY.md` - YOLO11 segmentation patterns
- `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md` - VFX integration patterns

---

*Created: 2026-01-20*
*Source: Unity Sentis 2.4.0 documentation and samples*
