#!/bin/bash
# Complete XRAI system installation with all dependencies

echo "🚀 Installing Complete XRAI System with Dependencies..."
echo "===================================================="

# Check if running on macOS
if [[ "$OSTYPE" != "darwin"* ]]; then
    echo "❌ This installer is designed for macOS. For Linux, please install dependencies manually."
    exit 1
fi

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# First run basic install script for dependencies
echo "📦 Installing base dependencies..."
if [ -f /Users/jamestunick/xrai/install.sh ]; then
    chmod +x /Users/jamestunick/xrai/install.sh
    /Users/jamestunick/xrai/install.sh
else
    # Fallback installation if install.sh not found
    echo "Installing dependencies manually..."
    
    # Install Homebrew if needed
    if ! command_exists brew; then
        /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    fi
    
    # Install system dependencies
    brew install python@3.13 python-tk@3.13 sox portaudio curl
    
    # Install Python packages
    pip3 install --user pyaudio SpeechRecognition requests pyttsx3 numpy tkinter
    
    # Install Ollama
    if ! command_exists ollama; then
        brew install ollama
        ollama serve &
        sleep 5
        ollama pull llama3.2:latest
        ollama pull phi3:mini
    fi
fi

echo ""
echo "🎯 Installing XRAI System Components..."

# Make all scripts executable
chmod +x /Users/jamestunick/xrai/*.py
chmod +x /Users/jamestunick/xrai/*.sh

# Create necessary directories
mkdir -p "$HOME/Library/Logs"
mkdir -p "$HOME/ai-logs"
mkdir -p "$HOME/xrai-data"

# Create Launch Agent for main voice system
cat > "$HOME/Library/LaunchAgents/com.xrai.voice-agent.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.xrai.voice-agent</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>/Users/jamestunick/xrai/system-voice-agent.py</string>
    </array>
    <key>WorkingDirectory</key>
    <string>/Users/jamestunick/xrai</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/Users/jamestunick/Library/Logs/xrai-voice.log</string>
    <key>StandardErrorPath</key>
    <string>/Users/jamestunick/Library/Logs/xrai-voice-error.log</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>PATH</key>
        <string>/usr/local/bin:/usr/bin:/bin:/opt/homebrew/bin</string>
        <key>PYTHONPATH</key>
        <string>/Users/jamestunick/xrai</string>
    </dict>
</dict>
</plist>
EOF

# Create Launch Agent for desktop overlay
cat > "$HOME/Library/LaunchAgents/com.xrai.desktop-overlay.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.xrai.desktop-overlay</string>
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>/Users/jamestunick/xrai/desktop-overlay.py</string>
    </array>
    <key>WorkingDirectory</key>
    <string>/Users/jamestunick/xrai</string>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardOutPath</key>
    <string>/Users/jamestunick/Library/Logs/xrai-overlay.log</string>
    <key>StandardErrorPath</key>
    <string>/Users/jamestunick/Library/Logs/xrai-overlay-error.log</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>PATH</key>
        <string>/usr/local/bin:/usr/bin:/bin:/opt/homebrew/bin</string>
        <key>PYTHONPATH</key>
        <string>/Users/jamestunick/xrai</string>
    </dict>
</dict>
</plist>
EOF

echo "🔄 Loading Launch Agents..."
# Unload any existing agents
launchctl unload "$HOME/Library/LaunchAgents/com.xrai.voice-agent.plist" 2>/dev/null
launchctl unload "$HOME/Library/LaunchAgents/com.xrai.desktop-overlay.plist" 2>/dev/null

# Load new agents
launchctl load "$HOME/Library/LaunchAgents/com.xrai.voice-agent.plist"
launchctl load "$HOME/Library/LaunchAgents/com.xrai.desktop-overlay.plist"

echo ""
echo "🔍 Checking system dependencies..."

# Check tkinter
echo -n "Checking tkinter... "
if python3 -c "import tkinter" 2>/dev/null; then
    echo "✅"
else
    echo "❌ (run: brew install python-tk@3.13)"
fi

# Check Whisper server
echo -n "Checking Whisper STT... "
if curl -s http://localhost:2022/health > /dev/null 2>&1; then
    echo "✅ Running on port 2022"
else
    echo "⚠️  Not running (optional)"
fi

# Check Ollama
echo -n "Checking Ollama... "
if command_exists ollama; then
    echo "✅ Installed"
    if ollama list | grep -q phi3:mini; then
        echo "  ✅ phi3:mini model ready"
    else
        echo "  📥 Installing phi3:mini model..."
        ollama pull phi3:mini
    fi
    if ollama list | grep -q llama3.2; then
        echo "  ✅ llama3.2 model ready"
    else
        echo "  📥 Installing llama3.2 model..."
        ollama pull llama3.2:latest
    fi
else
    echo "❌ Not installed"
fi

# Check audio tools
for tool in sox portaudio; do
    echo -n "Checking $tool... "
    if command_exists $tool || brew list $tool &>/dev/null; then
        echo "✅"
    else
        echo "❌ (run: brew install $tool)"
    fi
done

# Check Python packages
echo ""
echo "🐍 Checking Python packages..."
for pkg in pyaudio speech_recognition requests pyttsx3 numpy; do
    echo -n "  $pkg... "
    if python3 -c "import $pkg" 2>/dev/null; then
        echo "✅"
    else
        echo "❌ (run: pip3 install $pkg)"
    fi
done

echo ""
echo "🎉 XRAI SYSTEM INSTALLATION COMPLETE!"
echo "===================================="
echo ""
echo "📋 What's running:"
echo "  🎤 Voice Agent: Always listening for commands"
echo "  🖥️  Desktop Overlay: System info display"
echo "  🤖 AI Agents: Ready for advanced tasks"
echo ""
echo "🎙️ Voice Commands:"
echo "  • 'Hey XRAI' - Activate voice assistant"
echo "  • 'open [app]' - Open applications"
echo "  • 'find [term]' - Search files"
echo "  • 'create file [name]' - Create files"
echo "  • 'research [topic]' - Spawn research agent"
echo "  • Natural conversation supported!"
echo ""
echo "📊 Monitoring:"
echo "  • Voice logs: tail -f ~/Library/Logs/xrai-voice.log"
echo "  • Overlay logs: tail -f ~/Library/Logs/xrai-overlay.log"
echo "  • AI logs: tail -f ~/ai-logs/*.log"
echo ""
echo "🛑 Control Commands:"
echo "  • Stop voice: launchctl stop com.xrai.voice-agent"
echo "  • Stop overlay: launchctl stop com.xrai.desktop-overlay"
echo "  • Restart: launchctl kickstart -k gui/$(id -u)/com.xrai.voice-agent"
echo ""

# Final status check
sleep 3
echo "📡 Service Status:"
if launchctl list | grep -q "com.xrai.voice-agent"; then
    echo "  🟢 Voice agent running"
else
    echo "  🔴 Voice agent not running - check ~/Library/Logs/xrai-voice-error.log"
fi

if launchctl list | grep -q "com.xrai.desktop-overlay"; then
    echo "  🟢 Desktop overlay running"
else
    echo "  🔴 Desktop overlay not running - check ~/Library/Logs/xrai-overlay-error.log"
fi

echo ""
echo "🚀 Try it now: Say 'Hey XRAI' to test voice commands!"
echo ""