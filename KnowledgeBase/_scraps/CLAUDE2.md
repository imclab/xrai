# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XRAI is a comprehensive cross-platform AI+XR development ecosystem focused on spatial media, 3D content generation, and immersive experiences. The repository contains multiple interconnected projects and tools for Unity, WebGL/ThreeJS, and AI-driven content creation.

**Core Focus Areas:**
- AI-powered 3D content generation (text/speech/image/video → 3D models)
- Real-time multiplayer collaborative 3D environments
- Cross-platform XR experiences (Unity ↔ Web ↔ Mobile ↔ AR/VR)
- Next-generation spatial media formats (XRAI, VNMF)
- Neural rendering technologies (Gaussian Splatting, NeRFs)

## Repository Structure

```
XRAI/
├── AI-XR-MCP-main/          # MCP server for 3D WebGL visualizations
├── xrrai-prompt/            # Main AI-powered 3D generation platform
├── xrai-format/             # XRAI spatial media format specification
├── MANIFEST/                # Creative AI+XR manifesto and demos
├── vis/                     # 3D visualization tools and experiments
├── vnmf stuff/              # Virtual Neural Media Format research
├── mgx/                     # React templates and specifications
└── Unity samples/           # Various Unity projects and experiments
```

## Key Development Commands

### AI-XR-MCP (3D Visualization MCP Server)
```bash
cd AI-XR-MCP-main/
npm install                  # Install dependencies
npm run build               # Build TypeScript to JavaScript
npm run dev                 # Development with hot reload
npm start                   # Start production server
npm test                    # Run vitest tests
npm run lint                # ESLint checking
```

### XRR AI Platform (Main Platform)
```bash
cd xrrai-prompt/
npm install                 # Install Node.js dependencies
./install-ai-services-fixed.sh  # Setup AI services (Coqui TTS, Shap-E, etc.)
./download-ai-models.sh     # Pre-download AI models (optional but recommended)
npm start                   # Start development server
npm run build              # Production build
./test-ai-quick.sh         # Test AI services
```

### XRAI Format Tools
```bash
cd xrai-format/
npm install
node tools/xrai-encoder.js  # Encode content to XRAI format
node tools/xrai-decoder.js  # Decode XRAI content
node tools/benchmark.js     # Performance benchmarks
```

## Architecture Overview

### AI-Powered Content Generation Pipeline
1. **Input Processing**: Text prompts, speech, images, videos
2. **AI Services**: 
   - Coqui TTS (speech synthesis)
   - Shap-E (text-to-3D)
   - Hunyuan3D-2 (advanced 3D generation)
   - ObjaversePlus (3D asset search)
3. **Output Formats**: glTF/GLB, XRAI, Unity-compatible assets
4. **Deployment**: Web (React Three Fiber), Unity (C#), cross-platform

### Core Technologies Stack
- **Frontend**: React, Three.js, React Three Fiber, Drei
- **Backend**: Node.js, Express, Socket.IO, WebRTC
- **AI/ML**: PyTorch, ONNX Runtime, Transformers
- **Unity**: ARFoundation 6.1, VFXGraph 17.2, URP, Addressables
- **Formats**: glTF, XRAI, USD, Gaussian Splats, NeRF
- **XR**: WebXR, ARFoundation, OpenXR

## Development Guidelines

### Unity Development
- **Target Unity Version**: 2022.3 LTS or newer
- **Required Packages**: ARFoundation 6.1, VFXGraph 17.2, URP, Addressables
- **Platform Support**: iOS, Android, WebGL, Windows, macOS
- **Key APIs**: Use Addressables for asset management, avoid legacy AssetBundles
- **Testing**: Always test on mobile devices for AR features

### WebGL Development
- **Framework**: React Three Fiber with Drei components
- **Performance**: Use instancing, LOD systems, and efficient memory management
- **WebXR**: Implement WebXR Device API for VR/AR experiences
- **Formats**: Support glTF, Gaussian Splats, and XRAI format

### AI Service Integration
- **Local-First**: All AI services run locally without external API dependencies
- **Fallback Systems**: Intelligent degradation when AI models unavailable
- **Model Management**: Use model caching and lazy loading
- **Performance**: GPU acceleration (CUDA, MPS, WebGPU) when available

## Code Style & Conventions

### TypeScript/JavaScript
- ES Modules with .js extensions in import paths
- Strict TypeScript mode with explicit return types
- Zod for runtime validation
- ESLint + Prettier for formatting
- camelCase for variables/functions, PascalCase for types

### Unity C#
- Follow Unity C# coding standards
- Use async/await for async operations
- Implement proper object pooling for performance
- Use Addressables for asset loading
- Editor scripts: Functionality under "Edit" menu, no editor windows

### Python (AI Services)
- Black + Flake8 for formatting
- Type hints for all functions
- Virtual environments for dependency isolation
- Async/await for I/O operations

## Testing Strategy

### Unit Testing
```bash
# MCP Server
npm test                    # Vitest for TypeScript
npm test -- -t "specific test"  # Run specific test

# AI Platform
npm run test:unit          # Unit tests
npm run test:integration   # Integration tests
./test-ai-quick.sh        # Quick AI service test
```

### Performance Testing
```bash
cd xrai-format/
node tools/benchmark.js    # Format performance benchmarks
node tools/optimized-benchmark.js  # Optimized benchmarks
```

## Deployment & Production

### Local Development
```bash
npm run dev                # Development server
npm run build             # Production build
npm run preview           # Preview production build
```

### Docker Deployment
```bash
docker-compose up -d       # Start all services
docker-compose down        # Stop services
docker-compose logs        # View logs
```

### Platform-Specific Deployment
- **macOS**: `./deploy/local/setup-macos.sh`
- **Windows**: `.\deploy\local\setup-windows.ps1`
- **Linux**: `./deploy/local/setup-linux.sh`
- **Cloud**: AWS, GCP, Azure scripts in `deploy/` folders

## Performance Optimization

### System Requirements
- **Minimum**: Node.js 18+, 4GB RAM, 5GB storage
- **Recommended**: Node.js 20+, 16GB RAM, 50GB SSD, GPU acceleration
- **GPU Support**: NVIDIA RTX 3060+, AMD RX 6600+, Apple M1+

### Optimization Tips
```bash
# Node.js memory optimization
export NODE_OPTIONS="--max-old-space-size=8192"

# GPU memory management
export PYTORCH_CUDA_ALLOC_CONF=max_split_size_mb:512

# Production optimizations
export NODE_ENV=production
```

## Format Specifications

### XRAI Format
- **Purpose**: Universal spatial media format for AI+XR content
- **Features**: Hybrid geometry + neural fields, VFX systems, AI-driven content
- **Schema**: Located in `xrai-format/schemas/xrai-core.json`
- **Tools**: Encoder/decoder in `xrai-format/tools/`

### VNMF (Virtual Neural Media Format)
- **Purpose**: Research format for neural 3D content
- **Location**: `vnmf stuff/` directory
- **Components**: Encoder, decoder, Unity integration, WebXR viewer

## Troubleshooting

### Common Issues
1. **AI Services Failed**: Run `./install-ai-services-fixed.sh` and `./test-ai-quick.sh`
2. **Model Loading Timeout**: Pre-download models with `./download-ai-models.sh`
3. **Port Conflicts**: Change PORT in .env file or kill conflicting processes
4. **GPU Not Detected**: Install appropriate GPU drivers and PyTorch

### Performance Monitoring
```bash
htop                       # CPU/Memory usage
nvidia-smi                 # NVIDIA GPU usage
npm run analyze           # Bundle size analysis
```

## Key Documentation References

- **Unity**: ARFoundation 6.1, VFXGraph 17.2, URP documentation
- **Three.js**: React Three Fiber docs, Drei components
- **AI Services**: Coqui TTS, Shap-E, Hunyuan3D GitHub repositories
- **Standards**: WebXR, glTF, USD format specifications
- **MCP**: Model Context Protocol documentation

## Important Notes

- **Security**: No external API dependencies, all processing local
- **Compatibility**: Designed for Unity 2022.3+ and modern web browsers
- **Standards**: Follows open standards (glTF, WebXR, USD)
- **Future-Ready**: Architected for AR glasses, neural interfaces, holographic displays
- **Community**: Open source with MIT license, community contributions welcome