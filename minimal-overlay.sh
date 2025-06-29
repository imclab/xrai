#!/bin/bash
# Minimal GeekTool-style overlay using native macOS tools
# No dependencies, just pure shell commands

# Create status window functions
create_status_window() {
    local title="$1"
    local x="$2" 
    local y="$3"
    local content="$4"
    
    # Use AppleScript to create floating text window
    osascript <<EOF
tell application "System Events"
    display notification "$content" with title "$title"
end tell
EOF
}

# XRAI Activity Monitor
show_xrai_activity() {
    echo "🎯 XRAI ACTIVITY"
    echo "=================="
    
    # Show running XRAI processes
    ps aux | grep -i xrai | grep -v grep
    echo ""
    
    # Show recent voice activity  
    if [ -f "/Users/jamestunick/xrai/system_agent.jsonl" ]; then
        echo "Recent Activity:"
        tail -5 /Users/jamestunick/xrai/system_agent.jsonl | while read line; do
            echo "$line" | jq -r '.event' 2>/dev/null || echo "$line"
        done
    fi
    echo ""
    
    # System stats
    echo "System:"
    top -l 1 | head -4 | tail -2
    echo ""
    
    # Network
    echo "Network: $(ping -c 1 8.8.8.8 >/dev/null 2>&1 && echo "Online" || echo "Offline")"
    
    echo "=================="
    echo "$(date '+%H:%M:%S')"
}

# Continuous monitoring
monitor_loop() {
    while true; do
        clear
        show_xrai_activity
        sleep 3
    done
}

# Minimal HUD using terminal
echo "🖥️  XRAI Minimal HUD"
echo "Press Ctrl+C to stop"
echo ""

# Run monitoring
monitor_loop