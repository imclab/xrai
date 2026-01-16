#!/bin/bash
APP_PATH="./MetavidoVFX-main/Builds/iOS/build/MetavidoVFX.xcarchive/Products/Applications/MetavidoVFX.app"
# Ensure deployment uses Xcode 16.4 developer disk images/tools
export DEVELOPER_DIR="/Applications/Xcode-164.app/Contents/Developer"

if [ ! -d "$APP_PATH" ]; then
    echo "Error: App bundle not found at $APP_PATH"
    exit 1
fi

echo "Deploying to device..."
ios-deploy --debug --bundle "$APP_PATH"
