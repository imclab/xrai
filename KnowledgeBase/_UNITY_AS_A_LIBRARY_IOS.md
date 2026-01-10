# Unity as a Library - iOS Integration Guide

**Source**: [Unity 6000.2 Official Docs](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-iOS.html)
**Last Updated**: 2026-01-08
**Verified With**: @azesmway/react-native-unity@1.0.11

---

## Overview

Unity as a Library allows embedding Unity-powered features (3D/2D rendering, AR, mini-games) into native iOS applications. The Unity Runtime Library exposes controls to manage when and how to load, activate, and unload content.

## Architecture

Every Unity iOS Xcode project has two parts:

1. **UnityFramework target** (library)
   - Contains source, plugins, dependent frameworks
   - Produces `UnityFramework.framework`

2. **Unity-iPhone target** (launcher)
   - App representation data
   - Runs the library
   - Single dependency on UnityFramework

## Integration Steps

1. Build Xcode project from Unity (standard iOS build)
2. Export Unity to location **OUTSIDE** the host project (e.g., `/tmp/unity-ios-export/`)
3. Copy ONLY `UnityFramework.framework` to your project
4. Add framework to **Embedded Binaries** section of Application target
5. Use `UnityFramework` class to control runtime

## Critical UnityFramework API

| Method | Description | When to Call |
|--------|-------------|--------------|
| `getInstance` | Singleton accessor | Always |
| `setExecuteHeader:` | **REQUIRED** for CrashReporter | Before running Unity |
| `setDataBundleId:` | Sets Data folder location | Before running Unity |
| `runEmbeddedWithArgc:argv:appLaunchOpts:` | **Use this for React Native** (other Views exist) | Startup |
| `runUIApplicationMainWithArgc:argv:` | Default (no other Views) | NOT for RN |
| `sendMessageToGOWithName:functionName:message:` | RN→Unity messaging | Anytime after init |
| `registerFrameworkListener:` | Lifecycle events | After getInstance |
| `unloadApplication` | Partial unload (can restart) | Cleanup |
| `quitApplication:` | Full unload (cannot restart) | Final cleanup |
| `pause:` | Pause/resume Unity | As needed |
| `showUnityWindow` | Show Unity when hidden | View switching |

## React Native Integration (@azesmway/react-native-unity)

### Correct Implementation (Verified)

```objc
// 1. Load framework
UnityFramework* UnityFrameworkLoad() {
    NSString* bundlePath = [[NSBundle mainBundle] bundlePath];
    bundlePath = [bundlePath stringByAppendingString:@"/Frameworks/UnityFramework.framework"];
    NSBundle* bundle = [NSBundle bundleWithPath:bundlePath];
    if (![bundle isLoaded]) [bundle load];

    UnityFramework* ufw = [bundle.principalClass getInstance];

    // CRITICAL: Set execute header for CrashReporter
    #ifdef DEBUG
      [ufw setExecuteHeader: &_mh_dylib_header];
    #else
      [ufw setExecuteHeader: &_mh_execute_header];
    #endif

    // CRITICAL: Set data bundle ID
    [ufw setDataBundleId: [bundle.bundleIdentifier cStringUsingEncoding:NSUTF8StringEncoding]];

    return ufw;
}

// 2. Initialize with embedded mode (for RN)
[[self ufw] runEmbeddedWithArgc:argc argv:argv appLaunchOpts:launchOpts];

// 3. Register for lifecycle events
[[self ufw] registerFrameworkListener:self];

// 4. Connect NativeCallProxy for Unity→RN messaging
[NSClassFromString(@"FrameworkLibAPI") registerAPIforNativeCalls:self];
```

### Messaging Bridge

**RN → Unity** (via UnityFramework API):
```objc
[[self ufw] sendMessageToGOWithName:[gameObject UTF8String]
                       functionName:[methodName UTF8String]
                            message:[message UTF8String]];
```

**Unity → RN** (via NativeCallProxy):
```csharp
// Unity C# (BridgeTarget.cs)
[DllImport("__Internal")]
public static extern void sendMessageToMobileApp(string message);

// Calls to Objective-C NativeCallProxy.mm
extern "C" void sendMessageToMobileApp(const char* message) {
    return [api sendMessageToMobileApp:[NSString stringWithUTF8String:message]];
}
```

## NativeCallProxy Files (Required)

### NativeCallProxy.h
```objc
#import <Foundation/Foundation.h>

@protocol NativeCallsProtocol
@required
- (void) sendMessageToMobileApp:(NSString*)message;
@end

__attribute__ ((visibility("default")))
@interface FrameworkLibAPI : NSObject
+(void) registerAPIforNativeCalls:(id<NativeCallsProtocol>) aApi;
@end
```

### NativeCallProxy.mm
```objc
#import <Foundation/Foundation.h>
#import "NativeCallProxy.h"

@implementation FrameworkLibAPI

id<NativeCallsProtocol> api = NULL;
+(void) registerAPIforNativeCalls:(id<NativeCallsProtocol>) aApi {
    api = aApi;
}

@end

extern "C" {
    void sendMessageToMobileApp(const char* message) {
        return [api sendMessageToMobileApp:[NSString stringWithUTF8String:message]];
    }
}
```

**Location**: `unity/Assets/Plugins/iOS/`

## Known Limitations

1. **Full-screen only** - Unity cannot render on part of the screen
2. **Single instance** - Cannot load multiple Unity runtimes
3. **Third-party plugins** - May need adaptation
4. **Status bar** - Cannot control via PlayerSettings; use:
   - `UIStatusBarHidden` in Info.plist
   - `prefersStatusBarHidden` view controller property
   - `childViewControllerForStatusBarHidden`

## Build Flow (Recommended)

```bash
# 1. Unity exports to temp location (OUTSIDE RN project)
/tmp/unity-ios-export/

# 2. Build UnityFramework.framework via xcodebuild

# 3. Copy ONLY the framework to RN project
cp -R /tmp/unity-ios-export/build/.../UnityFramework.framework unity/builds/ios/

# 4. Clear pods cache (prevents stale framework issues)
rm -rf ios/Pods ios/Podfile.lock

# 5. Fresh pod install (copies framework to node_modules)
cd ios && pod install
```

## Debugging Tips

### Unity Side (BridgeTarget.cs)
```csharp
const bool DEBUG_ENABLED = true;
const string LOG_PREFIX = "[Bridge]";

private static void LogDebug(string message) {
    if (!DEBUG_ENABLED) return;
    Debug.Log($"{LOG_PREFIX} {message}");
}
```

### React Native Side (UnityArView.tsx)
```typescript
const DEBUG_ENABLED = __DEV__;
const LOG_PREFIX = '[UnityArView]';

const logDebug = (message: string) => {
    if (!DEBUG_ENABLED) return;
    console.log(`${LOG_PREFIX} ${message}`);
};
```

### Live Device Logs
```bash
# iOS
idevicesyslog | grep -E "Bridge|UnityArView"

# Check Unity console in Xcode
# Filter by "[Bridge]" prefix
```

## Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Framework not found | Stale pods cache | Clear pods, pod install |
| Messages not received | NativeCallProxy not registered | Verify files in Plugins/iOS |
| Crash on startup | Missing setExecuteHeader | Package handles this |
| Unity won't restart | Used quitApplication | Use unloadApplication instead |

## Official UAAL Example (Unity-Technologies)

Source: [Unity UAAL Example - iOS](https://github.com/Unity-Technologies/uaal-example/blob/master/docs/ios.md)

### Xcode Workspace Setup Pattern

The official approach uses an Xcode workspace to combine:
1. **NativeiOSApp.xcodeproj** - The host application
2. **Unity-iPhone.xcodeproj** - Generated by Unity

```
UaaLExample/
├── both.xcworkspace          # Workspace combining both
├── NativeiOSApp/            # Native iOS app
│   └── NativeiOSApp.xcodeproj
└── UnityProject/
    └── iosBuild/
        └── Unity-iPhone.xcodeproj
```

### Critical Xcode Configuration Steps

#### 1. Add UnityFramework.framework
- Select NativeiOSApp target
- **General** tab → **Frameworks, Libraries, and Embedded Content** → Add
- Choose `Unity-iPhone/UnityFramework.framework`
- **CRITICAL**: In **Build Phases** → **Link Binary With Libraries** → **REMOVE** UnityFramework.framework
  - Framework must be **Embedded**, NOT **Linked**

#### 2. Expose NativeCallProxy.h (CRITICAL)
- Find `Unity-iPhone/Libraries/Plugins/iOS/NativeCallProxy.h`
- Enable **UnityFramework** in Target Membership
- Set header visibility to **PUBLIC** (dropdown on right side)

Without this step, Unity→Native communication breaks!

#### 3. Data Folder Target Membership
- Change Data folder Target Membership from `Unity-iPhone` to `UnityFramework`
- This encapsulates everything in one framework file
- If keeping in Unity-iPhone, call:
```objc
[ufw setDataBundleId: "com.unity3d.framework"];
```

### UnityFrameworkLoad Pattern (Official)

```objc
#include <UnityFramework/UnityFramework.h>

UnityFramework* UnityFrameworkLoad()
{
    NSString* bundlePath = nil;
    bundlePath = [[NSBundle mainBundle] bundlePath];
    bundlePath = [bundlePath stringByAppendingString: @"/Frameworks/UnityFramework.framework"];

    NSBundle* bundle = [NSBundle bundleWithPath: bundlePath];
    if ([bundle isLoaded] == false) [bundle load];

    UnityFramework* ufw = [bundle.principalClass getInstance];
    if (![ufw appController])
    {
        // Initialize Unity for first time
        [ufw setExecuteHeader: &_mh_execute_header];

        // Keep in sync with Data folder Target Membership setting
        [ufw setDataBundleId: "com.unity3d.framework"];
    }
    return ufw;
}
```

### How @azesmway/react-native-unity Differs

The React Native package automates much of this:

| Official UAAL | @azesmway Package |
|---------------|-------------------|
| Manual Xcode workspace | CocoaPods handles framework linking |
| Manual framework embedding | podspec `vendored_frameworks` |
| Manual NativeCallProxy exposure | Files copied via Unity build |
| Manual Data folder config | Bundle ID auto-detected |

The package handles:
- Framework loading via podspec `prepare_command`
- Proper `setExecuteHeader` with debug/release distinction
- Auto bundle ID detection
- React Native event bridging

### React Native vs Manual Integration Flow

**Manual (UAAL Example)**:
```
Unity Build → Unity-iPhone.xcodeproj → Xcode Workspace → Manual Config → Build
```

**React Native (@azesmway)**:
```
Unity Build → /tmp/export → xcodebuild framework → copy to unity/builds/ios/ → pod install → Auto-config
```

## Checklist for React Native Unity Integration

- [ ] Unity exports to temp location (outside RN project)
- [ ] Only UnityFramework.framework copied to `unity/builds/ios/`
- [ ] NativeCallProxy.h/.mm in `unity/Assets/Plugins/iOS/`
- [ ] Pods cache cleared before install
- [ ] `pod install` runs fresh
- [ ] BridgeTarget GameObject in Unity scene
- [ ] UnityArView component with proper dimensions (flex:1)

## References

- [Unity Docs: Unity as a Library iOS](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-iOS.html)
- [Unity UAAL Examples](https://github.com/Unity-Technologies/uaal-example/blob/master/docs/ios.md)
- [@azesmway/react-native-unity](https://github.com/azesmway/react-native-unity)

---

**Verified**: Implementation in portals_v4 correctly follows all official requirements.
