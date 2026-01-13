#!/bin/bash
# Quick preview and test of XRAI system

echo "🎯 XRAI SYSTEM PREVIEW"
echo "====================="

# Show current files
echo "📁 Current XRAI files:"
ls -la /Users/jamestunick/xrai/

echo ""
echo "🚀 INSTALLATION OPTIONS:"
echo "1. Install complete system (voice agent + desktop overlay)"
echo "2. Preview GeekTool-style overlay only"
echo "3. Test voice agent only"
echo "4. View logs"
echo "5. Exit"

read -p "Choose option (1-5): " choice

case $choice in
    1)
        echo "🚀 Installing complete XRAI system..."
        chmod +x /Users/jamestunick/xrai/install-complete-system.sh
        /Users/jamestunick/xrai/install-complete-system.sh
        ;;
    2)
        echo "🖥️  Starting GeekTool-style overlay preview..."
        python3 /Users/jamestunick/xrai/geektool-style-overlay.py &
        echo "✅ Overlay started in background - check your desktop"
        echo "Press any key to stop..."
        read -n 1
        pkill -f geektool-style-overlay.py
        ;;
    3)
        echo "🎤 Testing voice agent (Ctrl+C to stop)..."
        python3 /Users/jamestunick/xrai/system-voice-agent.py
        ;;
    4)
        echo "📊 XRAI Logs:"
        if [ -f "/Users/jamestunick/Library/Logs/xrai-voice.log" ]; then
            echo "Voice agent log (last 10 lines):"
            tail -10 /Users/jamestunick/Library/Logs/xrai-voice.log
        fi
        if [ -f "/Users/jamestunick/xrai/system_agent.jsonl" ]; then
            echo ""
            echo "System agent activity (last 5 events):"
            tail -5 /Users/jamestunick/xrai/system_agent.jsonl
        fi
        ;;
    5)
        echo "👋 Exiting..."
        exit 0
        ;;
    *)
        echo "❌ Invalid option"
        ;;
esac

echo ""
echo "🔄 Run again? ./quick-preview.sh"