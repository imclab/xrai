#!/bin/bash

# Configuration
UNITY_PATH="/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app/Contents/MacOS/Unity"
# Pin Xcode toolchain to 16.4 (avoids picking newer betas/other installs)
export DEVELOPER_DIR="/Applications/Xcode-164.app/Contents/Developer"
PROJECT_PATH="$(pwd)"
BUILD_PATH="${PROJECT_PATH}/Builds/iOS"
LOG_FILE="${PROJECT_PATH}/build.log"

echo "=========================================="
echo "Starting Automated Build & Deploy Cycle"
echo "=========================================="

# 1. Unity Build (Generates Xcode Project)
echo "[1/3] Running Unity Build..."
"$UNITY_PATH" -batchmode -quit -projectPath "$PROJECT_PATH" -executeMethod AutomatedBuild.BuildiOS -logFile "$LOG_FILE"

if [ $? -ne 0 ]; then
    echo "Error: Unity Build Failed! Check log at $LOG_FILE"
    exit 1
fi
echo "Unity Build Complete."

# 2. Xcode Build (Generates IPA/App)
echo "[2/3] Building Xcode Project..."
cd "$BUILD_PATH"

# Find the xcodeproj
XCODEPROJ=$(find . -name "*.xcodeproj" -maxdepth 1 | head -n 1)
SCHEME="Unity-iPhone" # Default Unity scheme

# Build with xcodebuild (allow provisioning to handle signing)
xcodebuild -project "$XCODEPROJ" -scheme "$SCHEME" -configuration Release -allowProvisioningUpdates -archivePath ./build/MetavidoVFX.xcarchive archive
# Export IPA (optional, but good for distribution) or just find the .app in the archive
APP_PATH="./build/MetavidoVFX.xcarchive/Products/Applications/MetavidoVFX.app"

if [ -z "$APP_PATH" ]; then
    echo "Error: Could not find .app bundle!"
    exit 1
fi

ios-deploy --debug --bundle "$APP_PATH"

echo "=========================================="
echo "Cycle Complete!"
echo "=========================================="
