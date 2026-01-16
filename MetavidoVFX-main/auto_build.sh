#!/bin/bash
set -e

echo "=== H3M Auto Build ==="
date

# Kill Unity if running
pkill -9 Unity 2>/dev/null || true
sleep 3

# Run Unity build
echo "Starting Unity build..."
/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit \
  -projectPath "/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main" \
  -buildTarget iOS \
  -executeMethod BuildScript.BuildiOS

echo "Unity build complete!"

# Configure Xcode project
cd /Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main/Builds/iOS
sed -i '' 's/DEVELOPMENT_TEAM = "";/DEVELOPMENT_TEAM = Z8622973EB;/g' Unity-iPhone.xcodeproj/project.pbxproj
sed -i '' 's/OTHER_LDFLAGS = (/OTHER_LDFLAGS = ("-Wl,-ld_classic", /g' Unity-iPhone.xcodeproj/project.pbxproj

echo "Xcode project configured!"

# Build with Xcode
echo "Starting Xcode build..."
xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone \
  -destination "platform=iOS,name=IMClab 15" \
  -allowProvisioningUpdates \
  CODE_SIGN_STYLE=Automatic \
  DEVELOPMENT_TEAM=Z8622973EB \
  build

echo "Xcode build complete!"

# Install on device
APP_PATH=$(find ~/Library/Developer/Xcode/DerivedData -name "MetavidoVFX.app" -type d 2>/dev/null | head -1)
if [ -n "$APP_PATH" ]; then
  echo "Installing $APP_PATH..."
  xcrun devicectl device install app --device "IMClab 15" "$APP_PATH"
  xcrun devicectl device process launch --device "IMClab 15" com.DefaultCompany.MetavidoVFX
  echo "App launched!"
else
  echo "App not found - open Xcode and run manually"
fi

echo "=== DONE ==="
