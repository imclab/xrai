#!/bin/bash
# H3M iOS Build and Deploy Script
# Supports environment variables for CI/CD compatibility

set -e  # Exit on error

# Configuration with environment variable fallbacks
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_PATH="${METAVIDO_PROJECT_PATH:-$SCRIPT_DIR}"
UNITY_VERSION="${UNITY_VERSION:-6000.2.14f1}"
UNITY_PATH="${UNITY_PATH:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"
BUILD_PATH="$PROJECT_PATH/Builds/iOS"
LOG_FILE="${BUILD_LOG:-/tmp/unity_ios_build_$(date +%Y%m%d_%H%M%S).log}"
DEVICE_NAME="${DEVICE_NAME:-any}"
TEAM_ID="${APPLE_TEAM_ID:-Z8622973EB}"

echo "=== H3M iOS Build and Deploy ==="
echo "Project: $PROJECT_PATH"
echo "Log: $LOG_FILE"

# Step 1: Unity Build
echo ""
echo "[1/3] Building Unity project for iOS..."
"$UNITY_PATH" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget iOS \
  -logFile "$LOG_FILE" \
  -executeMethod BuildScript.BuildiOS

if [ $? -ne 0 ]; then
    echo "Unity build may have issues. Checking log..."
    grep -E "(error|Error|Build succeeded|Build failed)" "$LOG_FILE" | tail -10
fi

# Check if Xcode project exists
if [ ! -d "$BUILD_PATH/Unity-iPhone.xcodeproj" ]; then
    echo "ERROR: Xcode project not found. Unity build failed."
    exit 1
fi

echo "Unity build completed. Xcode project at: $BUILD_PATH"

# Step 2: Set Team ID
echo ""
echo "[2/3] Configuring signing..."
sed -i '' 's/DEVELOPMENT_TEAM = "";/DEVELOPMENT_TEAM = '"$TEAM_ID"';/g' "$BUILD_PATH/Unity-iPhone.xcodeproj/project.pbxproj"

# Step 3: Xcode Build and Install
echo ""
echo "[3/3] Building and installing to device: $DEVICE_NAME..."
cd "$BUILD_PATH"

# Build destination - use generic if device not specified
if [ "$DEVICE_NAME" = "any" ]; then
    DESTINATION="generic/platform=iOS"
else
    DESTINATION="platform=iOS,name=$DEVICE_NAME"
fi

xcodebuild \
  -project Unity-iPhone.xcodeproj \
  -scheme Unity-iPhone \
  -destination "$DESTINATION" \
  -allowProvisioningUpdates \
  CODE_SIGN_STYLE=Automatic \
  DEVELOPMENT_TEAM=$TEAM_ID \
  build

if [ $? -eq 0 ]; then
    echo ""
    echo "=== BUILD SUCCEEDED ==="
    echo "Installing to device..."

    # Get app path and install
    APP_PATH=$(find ~/Library/Developer/Xcode/DerivedData -name "Unity-iPhone.app" -type d 2>/dev/null | head -1)
    if [ -n "$APP_PATH" ]; then
        ios-deploy --bundle "$APP_PATH" --debug 2>/dev/null || \
        xcrun devicectl device install app --device "$DEVICE_NAME" "$APP_PATH" 2>/dev/null || \
        echo "App built. Open Xcode to run on device: $BUILD_PATH/Unity-iPhone.xcodeproj"
    fi
else
    echo ""
    echo "=== BUILD FAILED ==="
    echo "Check Xcode for errors: $BUILD_PATH/Unity-iPhone.xcodeproj"
fi
