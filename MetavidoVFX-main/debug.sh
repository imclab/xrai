#!/bin/bash
# H3M iOS Debug Script
# Usages:
#   ./debug.sh          -> Stream logs (tails)
#   ./debug.sh --dump   -> Dump last 5 mins to file
#   ./debug.sh --filters -> Show only relevant Unity/App logs

DEVICE_NAME="93485B6C-D0DD-5535-BD87-A80D0FC9FB54"
APP_BUNDLE="com.DefaultCompany.MetavidoVFX"
LOG_DIR="Logs/Device"

mkdir -p "$LOG_DIR"

DEVICE_UDID="93485B6C-D0DD-5535-BD87-A80D0FC9FB54"

function stream_logs() {
    echo "Streaming logs for $APP_BUNDLE on $DEVICE_UDID via idevicesyslog..."
    echo "Press Ctrl+C to stop."
    # Use idevicesyslog which is often more reliable than devicectl for logs
    idevicesyslog -u "$DEVICE_UDID" | grep -Ei "MetavidoVFX|Unity|ARKitBinder|PlayerLogDumper"
}

function dump_last_session() {
    TIMESTAMP=$(date +%Y%m%d_%H%M%S)
    FILE="$LOG_DIR/crash_log_$TIMESTAMP.log"
    echo "Dumping logs to $FILE..."
    # Attempt to get crash reports too
    rsync -av --ignore-existing /Users/jamestunick/Library/Logs/CrashReporter/MobileDevice/ "$LOG_DIR/Crashes/"
}

if [ "$1" == "--dump" ]; then
    dump_last_session
else
    stream_logs
fi
