#!/bin/bash
# XRAI Quick Install
echo "Installing XRAI..."
chmod +x ~/xrai/xrai.py

# LaunchAgent for auto-start
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
</dict>
</plist>
EOF

launchctl load ~/Library/LaunchAgents/com.xrai.plist
echo "✅ XRAI installed and running!"