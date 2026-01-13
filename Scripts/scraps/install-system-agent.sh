#!/bin/bash
# Install System Voice Agent as macOS Launch Agent

AGENT_DIR="/Users/jamestunick/xrai"
PLIST_FILE="$HOME/Library/LaunchAgents/com.xrai.system-agent.plist"

echo "🚀 Installing XRAI System Voice Agent..."

# Make script executable
chmod +x "$AGENT_DIR/system-voice-agent.py"

# Create Launch Agent plist
cat > "$PLIST_FILE" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.xrai.system-agent</string>
    
    <key>ProgramArguments</key>
    <array>
        <string>/usr/bin/python3</string>
        <string>$AGENT_DIR/system-voice-agent.py</string>
    </array>
    
    <key>WorkingDirectory</key>
    <string>$AGENT_DIR</string>
    
    <key>RunAtLoad</key>
    <true/>
    
    <key>KeepAlive</key>
    <true/>
    
    <key>StandardOutPath</key>
    <string>$HOME/Library/Logs/xrai-system-agent.log</string>
    
    <key>StandardErrorPath</key>
    <string>$HOME/Library/Logs/xrai-system-agent-error.log</string>
    
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

# Load the agent
launchctl unload "$PLIST_FILE" 2>/dev/null
launchctl load "$PLIST_FILE"

echo "✅ XRAI System Agent installed!"
echo "📝 Logs: ~/Library/Logs/xrai-system-agent.log"
echo "🎤 Agent will start on system boot"
echo ""
echo "Commands:"
echo "  launchctl start com.xrai.system-agent"
echo "  launchctl stop com.xrai.system-agent"
echo "  launchctl unload $PLIST_FILE"

# Check status
if launchctl list | grep -q "com.xrai.system-agent"; then
    echo "🟢 Agent is running"
else
    echo "🔴 Agent failed to start"
fi