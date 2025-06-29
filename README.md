# XRAI - Extensible Real-time AI Assistant

A modular, always-on AI voice assistant system with multiple implementations for different use cases.

## Project Overview

XRAI is a collection of AI assistant implementations designed to provide always-listening, context-aware voice interaction with minimal resource usage. The project includes multiple approaches ranging from simple voice loops to advanced orchestrated agent systems.

## Features

- **Always-Listening Voice Interface**: Multiple implementations for continuous voice interaction
- **Modular Architecture**: Choose from simple scripts to complex multi-agent systems
- **Local-First Design**: Run entirely on your machine without external dependencies
- **API Integration Options**: Support for OpenAI, Anthropic, and local models
- **Memory & Context**: Persistent conversation history and context awareness
- **Desktop Integration**: System overlays and HUD displays
- **Developer Tools**: Code analysis, GitHub integration, and predictive assistance

## Components

### Core Voice Assistants
- **xrai.py**: Main entry point and core functionality
- **quick-voice-assistant.py**: Simple 3-second recording loops with Ollama
- **system-voice-agent.py**: System-integrated voice control
- **simple-always-listening-ai.sh**: Continuous listening with VAD (Voice Activity Detection)

### Desktop Overlays
- **desktop-overlay.py**: Desktop HUD for visual feedback
- **system-overlay.py**: System status and monitoring overlay
- **geektool-style-overlay.py**: Customizable GeekTool-style displays
- **hud.py**: Heads-up display for real-time information

### Advanced Agents
- **master-agent-orchestrator.py**: Coordinates multiple specialized agents
- **github-knowledge-agent.py**: GitHub repository analysis and insights
- **elite-repos-agent.py**: Elite repository discovery and analysis
- **predictive-agent.py**: Predictive assistance based on context
- **viral-innovation-agent.py**: Innovation and trend analysis
- **deep-code-analyzer.py**: Advanced code analysis and understanding

### Installation & Setup
- **install.sh**: Basic installation script
- **install-complete-system.sh**: Full system setup with all dependencies
- **install-system-agent.sh**: System agent specific installation
- **auto-demo.sh**: Automated demonstration of capabilities
- **quick-preview.sh**: Quick preview of functionality
- **minimal-overlay.sh**: Minimal overlay setup

### Web Interface
- **dashboard.html**: Web-based dashboard for monitoring and control

## Quick Start

### Basic Voice Assistant
```bash
# Install dependencies
./install.sh

# Run simple voice assistant
python3 quick-voice-assistant.py
```

### Full System
```bash
# Complete installation
./install-complete-system.sh

# Run main XRAI system
python3 xrai.py
```

### Demo Mode
```bash
# See it in action
./auto-demo.sh
```

## Requirements

- Python 3.8+
- macOS (primary support) or Linux
- Optional: Ollama for local LLM
- Optional: API keys for OpenAI/Anthropic

## Architecture

The project supports multiple configurations:

1. **Local-Only**: Uses Ollama and local Whisper for complete offline operation
2. **API-Based**: Leverages OpenAI/Anthropic for higher quality responses
3. **Hybrid**: Falls back between API and local models based on availability
4. **Multi-Agent**: Orchestrates specialized agents for complex tasks

## Memory & Persistence

- JSON-based conversation logs in `~/ai-logs/`
- Daily logs with weekly summaries
- Context injection for continuity
- Optional Letta (MemGPT) integration for advanced memory

## Current Status

- Voice Mode MCP Server integration complete
- Basic voice loops operational
- Desktop overlays functional
- Advanced agents in beta
- Memory system being refined

## Future Roadmap

- [ ] Unified configuration system
- [ ] Plugin architecture for extensions
- [ ] Mobile companion app
- [ ] ESP32 hardware integration
- [ ] Advanced context understanding
- [ ] Multi-modal input support

## Contributing

This project is under active development. Contributions welcome!

## License

MIT License - See LICENSE file for details

---

*XRAI: Your extensible, always-on AI companion*