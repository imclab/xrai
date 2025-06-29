#!/bin/bash
# Complete XRAI system installation

echo "🚀 Installing Complete XRAI System..."

# Install system voice agent
echo "1. Installing System Voice Agent..."
chmod +x /Users/jamestunick/xrai/system-voice-agent.py
chmod +x /Users/jamestunick/xrai/desktop-overlay.py

# Create Launch Agent for voice system
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
    </dict>
</dict>
</plist>
EOF

# Create logs directory
mkdir -p "$HOME/Library/Logs"

echo "2. Loading Launch Agents..."
launchctl unload "$HOME/Library/LaunchAgents/com.xrai.voice-agent.plist" 2>/dev/null
launchctl unload "$HOME/Library/LaunchAgents/com.xrai.desktop-overlay.plist" 2>/dev/null

launchctl load "$HOME/Library/LaunchAgents/com.xrai.voice-agent.plist"
launchctl load "$HOME/Library/LaunchAgents/com.xrai.desktop-overlay.plist"

echo "3. Checking system dependencies..."

# Check if Whisper server is available
if curl -s http://localhost:2022/health > /dev/null 2>&1; then
    echo "✅ Whisper STT server running"
else
    echo "⚠️  Whisper STT server not running - start with ./jt-tools/voice-scripts/voice-scripts/start_whisper.sh"
fi

# Check if Ollama is available
if command -v ollama > /dev/null 2>&1; then
    echo "✅ Ollama available"
    if ollama list | grep -q phi3:mini; then
        echo "✅ phi3:mini model ready"
    else
        echo "📥 Installing phi3:mini model..."
        ollama pull phi3:mini
    fi
else
    echo "⚠️  Ollama not found - install from https://ollama.ai"
fi

# Check system tools
for tool in sox curl say; do
    if command -v $tool > /dev/null 2>&1; then
        echo "✅ $tool available"
    else
        echo "❌ $tool missing - install with brew install $tool"
    fi
done

echo ""
echo "🎉 XRAI SYSTEM INSTALLATION COMPLETE!"
echo ""
echo "📋 What's running:"
echo "  🎤 Voice Agent: Always listening for commands"
echo "  🖥️  Desktop Overlay: Ambient system info display"
echo "  🤖 Child Agents: Ready to spawn for parallel tasks"
echo ""
echo "🎙️ Voice Commands:"
echo "  'open [app]' - Open applications"
echo "  'find [term]' - Search files"
echo "  'create file [name]' - Create files"
echo "  'research [topic]' - Spawn research agent"
echo "  'run [command]' - Execute system commands"
echo ""
echo "📊 Monitoring:"
echo "  tail -f ~/Library/Logs/xrai-voice.log"
echo "  tail -f ~/Library/Logs/xrai-overlay.log"
echo ""
echo "🛑 Control:"
echo "  launchctl stop com.xrai.voice-agent"
echo "  launchctl stop com.xrai.desktop-overlay"

# Check if agents started successfully
sleep 3
if launchctl list | grep -q "com.xrai.voice-agent"; then
    echo "🟢 Voice agent running"
else
    echo "🔴 Voice agent failed to start"
fi

if launchctl list | grep -q "com.xrai.desktop-overlay"; then
    echo "🟢 Desktop overlay running"
else
    echo "🔴 Desktop overlay failed to start"
fi