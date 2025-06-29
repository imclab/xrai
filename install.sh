#!/bin/bash
# XRAI Complete Installation Script

echo "🚀 Installing XRAI - Extensible Real-time AI Assistant"
echo "=================================================="

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This installer is designed for macOS. For Linux, please install dependencies manually."
    exit 1
fi

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Install Homebrew if not installed
if ! command_exists brew; then
    echo "📦 Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
fi

# Install Python and dependencies
echo "🐍 Installing Python and dependencies..."
brew install python@3.13 python-tk@3.13 sox portaudio

# Install Python packages
echo "📚 Installing Python packages..."
pip3 install --user pyaudio SpeechRecognition requests pyttsx3 numpy

# Install Ollama if not installed
if ! command_exists ollama; then
    echo "🤖 Installing Ollama..."
    brew install ollama
    
    # Start Ollama service
    echo "Starting Ollama service..."
    ollama serve &
    sleep 5
    
    # Pull a default model
    echo "📥 Pulling default AI model (llama3.2)..."
    ollama pull llama3.2:latest
fi

# Check for Whisper (optional but recommended)
if ! command_exists whisper; then
    echo "💡 Optional: Install Whisper for better speech recognition:"
    echo "   pip3 install openai-whisper"
fi

# Create necessary directories
echo "📁 Creating directories..."
mkdir -p ~/ai-logs
mkdir -p ~/xrai-data

# Make scripts executable
echo "🔧 Setting permissions..."
chmod +x ~/xrai/*.py
chmod +x ~/xrai/*.sh

# Install LaunchAgent for auto-start
echo "🚀 Installing auto-start service..."
cat > ~/Library/LaunchAgents/com.xrai.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.xrai</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>/Users/jamestunick/xrai/xrai.py</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/Users/jamestunick/ai-logs/xrai.log</string>
    <key>StandardErrorPath</key>
    <string>/Users/jamestunick/ai-logs/xrai-error.log</string>
</dict>
</plist>
EOF

# Load the service
launchctl unload ~/Library/LaunchAgents/com.xrai.plist 2>/dev/null
launchctl load ~/Library/LaunchAgents/com.xrai.plist

echo ""
echo "✅ XRAI Installation Complete!"
echo "================================"
echo ""
echo "🎯 Quick Start Commands:"
echo "   • Test voice assistant: python3 ~/xrai/quick-voice-assistant.py"
echo "   • Run desktop overlay: python3 ~/xrai/desktop-overlay.py"
echo "   • Start full system: python3 ~/xrai/xrai.py"
echo "   • View demo: ~/xrai/auto-demo.sh"
echo ""
echo "📖 Configuration:"
echo "   • Logs: ~/ai-logs/"
echo "   • Data: ~/xrai-data/"
echo "   • Auto-start: Enabled (disable with: launchctl unload ~/Library/LaunchAgents/com.xrai.plist)"
echo ""
echo "🔊 Voice Commands:"
echo "   • Say 'Hey XRAI' to activate"
echo "   • Natural conversation supported"
echo "   • Continuous listening mode"
echo ""