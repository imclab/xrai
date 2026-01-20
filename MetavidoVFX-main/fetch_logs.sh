#!/bin/bash
# Fetch logs from MetavidoVFX app running on iOS device
# Usage: ./fetch_logs.sh [device-ip] [endpoint]
# Endpoints: logs, recent, errors, status

# Default to finding device IP from recent devicectl output
DEVICE_IP="${1:-}"
ENDPOINT="${2:-errors}"
PORT=8085

# If no IP provided, try to find it
if [ -z "$DEVICE_IP" ]; then
    echo "Usage: ./fetch_logs.sh <device-ip> [endpoint]"
    echo ""
    echo "Endpoints:"
    echo "  logs   - Full log file"
    echo "  recent - Recent 100 lines"
    echo "  errors - Errors and warnings only (default)"
    echo "  status - App status"
    echo ""
    echo "Find device IP: Settings > Wi-Fi > (i) > IP Address"
    exit 1
fi

URL="http://${DEVICE_IP}:${PORT}/${ENDPOINT}"

echo "=== Fetching logs from ${URL} ==="
echo ""

# Fetch with timeout
curl -s --connect-timeout 5 "$URL"
RESULT=$?

if [ $RESULT -ne 0 ]; then
    echo ""
    echo "ERROR: Could not connect to device"
    echo "Make sure:"
    echo "  1. App is running on device"
    echo "  2. Device is on same Wi-Fi network"
    echo "  3. IP address is correct: $DEVICE_IP"
fi
