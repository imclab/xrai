#!/bin/bash
# Auto-demo of XRAI GeekTool-style overlay

echo "🖥️  Starting XRAI GeekTool-style overlay demo..."
echo "✨ This mimics your existing GeekTool widgets"

# Start the overlay in background
python3 /Users/jamestunick/xrai/geektool-style-overlay.py &
OVERLAY_PID=$!

echo "✅ Overlay started (PID: $OVERLAY_PID)"
echo "📊 Check your desktop - you should see:"
echo "   • Activity monitor (top-left)"
echo "   • XRAI logs (bottom-left)" 
echo "   • Network stats (top-right)"
echo "   • System stats (center-right)"
echo "   • Voice activity (bottom-right)"
echo ""
echo "⏱️  Demo will run for 30 seconds..."

sleep 30

echo "🛑 Stopping demo..."
kill $OVERLAY_PID

echo "✅ Demo complete!"
echo "🚀 To install permanently: ./install-complete-system.sh"