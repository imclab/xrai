# IDE Extensions, Local Models & AI Tools

## VS Code Extensions

### AI & MCP

| Extension | ID | Purpose |
|-----------|-----|---------|
| Claude Code | `anthropic.claude-code` | Claude AI integration |
| GitHub Copilot | `github.copilot` | AI code completion |
| Copilot Chat | `github.copilot-chat` | AI chat interface |
| MCP Audit | `agentity.mcp-audit-extension` | MCP debugging |
| MCP Inspector | `dhananjaysenday.mcp--inspector` | MCP inspection |
| MCP Integration | `buildwithlayer.mcp-integration-expert-eligr` | MCP tools |
| MCP Client | `m1self.mcp-client` | MCP client |
| MCP Server | `semanticworkbenchteam.mcp-server-vscode` | MCP server |
| Copilot MCP | `automatalabs.copilot-mcp` | Copilot + MCP |
| Claude Dev | `saoudrizwan.claude-dev` | Claude development |
| ChatGPT | `openai.chatgpt` | OpenAI chat |
| ChatGPT Helper | `kiranshah.chatgpt-helper` | GPT helper |
| OpenAI Developer | `manassahu.openai-developer` | OpenAI tools |
| Windows AI Studio | `ms-windows-ai-studio.windows-ai-studio` | Windows AI |
| AI Foundry | `teamsdevapp.vscode-ai-foundry` | AI foundry |

### Unity & C#

| Extension | ID | Purpose |
|-----------|-----|---------|
| Unity Tools | `visualstudiotoolsforunity.vstuc` | Unity integration |
| Unity ECS Snippets | `ashtondev.unity-ecs-snippets` | DOTS snippets |
| Unity DOTS Snippets | `diegosarmentero.unity-dots-snippets` | DOTS helpers |
| C# | `ms-dotnettools.csharp` | C# language |
| C# IntelliCode | `ms-dotnettools.vscodeintellicode-csharp` | AI completion |
| C# Snippets | `jorgeserrano.vscode-csharp-snippets` | Code snippets |

### Three.js & WebGL

| Extension | ID | Purpose |
|-----------|-----|---------|
| Three.js Snippets | `aerokaido.three-js-snippets` | Three.js code |
| Three.js Snippets | `haohailiang.vscode-snippet-threejs` | More snippets |
| Three.js Autocomplete | `maxprogrammercomputer.threejs-autocomplete` | Autocomplete |
| React Three Fiber | `ashabb.ashabb-react-three-fiber` | R3F snippets |
| WebGL GLSL Editor | `raczzalan.webgl-glsl-editor` | Shader editing |
| Three Shader Reader | `bhushanwagh.three-shader-reader` | Shader tools |
| Shader Factory | `chenng.shader-factory` | Shader factory |
| Shader Support | `slevesque.shader` | Shader language |
| Shaderc | `majicdave.vscode-shaderc` | Shader compiler |

### XR & 3D

| Extension | ID | Purpose |
|-----------|-----|---------|
| Code XR | `amontesl.code-xr` | XR development |
| HTML in XR | `dlumbrer.html-in-xr` | XR HTML |
| 3D HTML Viewer | `aizhe.3d-html-viewer` | 3D viewing |
| 3D Preview | `mohitkumartoshniwal.3d-preview` | Model preview |
| VRM Viewer | `metrosoft-application.vrm-viewer` | VRM models |
| Protein Viewer | `arianjamasb.protein-viewer` | 3D proteins |

### Blender & Design

| Extension | ID | Purpose |
|-----------|-----|---------|
| Blender Development | `jacqueslucke.blender-development` | Blender addon dev |
| Blender Python | `blenderfreetimeprojects.blender-python-code-templates` | Python templates |
| MCP Figma | `sethford.mcp-figma-extension` | Figma MCP |

### ECharts & Visualization

| Extension | ID | Purpose |
|-----------|-----|---------|
| ECharts Doc Syntax | `pissang.echarts-doc-syntax-highlight` | ECharts syntax |
| ECharts Completion | `ren-wei.echarts-enhanced-completion` | Autocomplete |
| ECharts Extension | `susiwen8.vscode-echarts-extension` | ECharts tools |

---

## LM Studio

### Location
```
~/.lmstudio/
├── models/           # Downloaded models
├── config-presets/   # Configuration presets
├── conversations/    # Chat history
├── credentials/      # API credentials
├── extensions/       # LM Studio extensions
└── bin/              # CLI binaries
```

### Installed Models

| Provider | Model | Size | Purpose |
|----------|-------|------|---------|
| lmstudio-community | Devstral-Small-2507-MLX-4bit | ~2GB | Small dev assistant |
| lmstudio-community | Qwen3-Coder-30B-A3B-Instruct-MLX-4bit | ~15GB | Code generation |
| lmstudio-community | Qwen3-VL-8B-Instruct-MLX-4bit | ~4GB | Vision-language |
| mlx-community | gpt-oss-20b-MXFP4-Q8 | ~10GB | General purpose |
| mlx-community | Llama-3.2-1B-Instruct-4bit | ~1GB | Fast inference |
| reedmayhew | claude-3.7-sonnet-reasoning-gemma3-12B | ~6GB | Reasoning model |

### CLI Usage
```bash
# LM Studio CLI
~/.lmstudio/bin/lms

# Start server
lms server start

# List models
lms ls

# Load model
lms load <model-name>
```

### API Endpoint
```
http://localhost:1234/v1/chat/completions
```

---

## Pinokio

### Location
```
~/pinokio/
└── api/              # Installed applications
```

### Installed Apps

| App | Purpose |
|-----|---------|
| **comfyui** | Image generation workflow |
| **Hunyuan3D-2-lowvram** | 3D generation (low VRAM) |
| **macOS-use** | macOS automation |
| **N8N-Pinokio** | Workflow automation |
| **TheStoryDiffusion** | Story-driven image gen |
| **whisper-webui** | Speech-to-text UI |

### Usage
```bash
# Launch Pinokio
open ~/Applications/Pinokio.app

# Apps accessible at
http://localhost:42000
```

---

## Model Recommendations by Task

| Task | Recommended Model |
|------|-------------------|
| Code completion | Qwen3-Coder-30B |
| Quick chat | Llama-3.2-1B |
| Vision tasks | Qwen3-VL-8B |
| Reasoning | claude-3.7-sonnet-reasoning |
| Image generation | ComfyUI (Pinokio) |
| 3D generation | Hunyuan3D-2 (Pinokio) |
| Speech-to-text | whisper-webui (Pinokio) |

---

## Hardware Optimization

### M3 Max (128GB RAM)

| Model Size | RAM Usage | Recommendation |
|------------|-----------|----------------|
| 1-3B | ~2-4GB | Multiple concurrent |
| 7-8B | ~6-10GB | Good balance |
| 30B | ~15-20GB | Single instance |
| 70B | ~40-50GB | Close other apps |

### LM Studio Settings for M3 Max
```json
{
  "n_gpu_layers": -1,  // Use all GPU layers
  "n_ctx": 8192,       // Context window
  "n_batch": 512,      // Batch size
  "threads": 12        // CPU threads
}
```

---

## Quick Reference

```bash
# Start LM Studio server
~/.lmstudio/bin/lms server start

# Check LM Studio status
curl http://localhost:1234/v1/models

# Open Pinokio
open ~/Applications/Pinokio.app

# List VS Code extensions
code --list-extensions | grep -iE "unity|ai|mcp"
```

---

*Updated: 2026-01-13*
