#!/bin/bash
# Rider Flicker Fix - Reduce file watcher sensitivity
#
# Usage:
#   ./rider-flicker-fix.sh          # Apply settings
#   ./rider-flicker-fix.sh --revert # Revert to defaults
#
# Manual steps (in Rider):
#   1. Settings > Appearance > System Settings > Synchronization
#   2. Uncheck "Synchronize external changes immediately"
#   3. Set "Save files after" to 30 seconds
#   4. Settings > Editor > General > Auto Import
#   5. Uncheck all auto-import options

RIDER_CONFIG="$HOME/Library/Application Support/JetBrains/Rider2025.3/options"

echo "=== Rider Flicker Fix ==="

if [ "$1" == "--revert" ]; then
    echo "Reverting to defaults..."
    # Remove our custom entries
    sed -i '' '/vfs.local.refresh.debounce/d' "$RIDER_CONFIG/ide.general.xml"
    sed -i '' '/external.system.auto.import.disabled/d' "$RIDER_CONFIG/ide.general.xml"
    echo "Done. Restart Rider."
    exit 0
fi

echo "Current settings applied:"
echo "  - VFS refresh disabled"
echo "  - Refresh debounce: 2000ms"
echo "  - Auto-import disabled"
echo "  - Inactive timeout: 30s"
echo ""
echo "Manual steps still recommended:"
echo "  1. Rider > Settings > Appearance > System Settings > Synchronization"
echo "  2. Uncheck 'Synchronize external changes immediately'"
echo "  3. Or press Cmd+Shift+A and search 'Synchronize'"
echo ""
echo "To revert: $0 --revert"
echo "Settings take effect after Rider restart."
