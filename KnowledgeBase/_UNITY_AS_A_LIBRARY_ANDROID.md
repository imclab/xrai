# Unity as a Library - Android Integration Guide

**Source**: [Unity 6000.2 Official Docs](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-Android.html)
**UAAL Example**: [Unity-Technologies/uaal-example (Android)](https://github.com/Unity-Technologies/uaal-example/blob/master/docs/android.md)
**Last Updated**: 2026-01-08

---

## Overview

Unity as a Library allows embedding Unity-powered features (3D/2D rendering, AR, mini-games) into native Android applications. The Unity Runtime Library exposes controls to manage when and how to load, activate, and unload content.

## Requirements

- Android Studio Iguana (2023.2.1) or later
- Unity 6000.0.0b16 or later (for current UAAL example)
- For Unity 2019-2021 LTS: Use `19LTS-21LTS` branch
- For Unity 2022 LTS: Use `22LTS` branch

## Architecture

Every Unity Android Gradle project has two parts:

1. **unityLibrary module** (library)
   - Contains Unity runtime and Player data
   - Can integrate into any Gradle project

2. **launcher module** (thin launcher)
   - Application name and icons
   - Simple Android app that launches Unity
   - Can be replaced with your own application

## Integration Steps

### 1. Project Structure

```
UnityProject/
├── Assets/
│   └── Plugins/
│       └── Android/
│           └── MainApp.androidlib/  # Android Library Project
└── ...
```

### 2. Configure Unity Build

1. Open Unity Editor → Build Profiles (File / Build Profiles)
2. Switch to Android Platform
3. Player Settings → Other Settings → Configuration:
   - **Application Entry Point**: Select both Activity AND GameActivity
4. Enable "Export Project" option
5. Export to folder

### 3. Application Entry Points

Unity 6 supports two entry point types:

| Entry Point | Class | Use Case |
|-------------|-------|----------|
| Activity | `UnityPlayerActivity` | Traditional Android Activity |
| GameActivity | `UnityPlayerGameActivity` | Google's GameActivity (better performance) |

**If using only one entry point**, delete the unused Java file:
- Activity only: Delete `MainUnityGameActivity.java`
- GameActivity only: Delete `MainUnityActivity.java`

## Critical Android Concepts

### Unity Runs in Separate Process

```xml
<!-- AndroidManifest.xml -->
<activity android:process=":Unity" ... />
```

Unity runs in a separate Android process (`:Unity`), which:
- Isolates Unity memory from main app
- Allows independent lifecycle management
- Requires IPC for communication

### IUnityPlayerLifecycleEvents

Interface for lifecycle event callbacks:

```java
public interface IUnityPlayerLifecycleEvents {
    // Called when Application.Unload or UnityPlayer.unload() executes
    // Unity enters paused state, scenes unloaded but runtime stays in memory
    void onUnityPlayerUnloaded();

    // Called when Unity Player quits
    // Process running Unity ends after this call
    void onUnityPlayerQuitted();
}
```

Pass to UnityPlayer constructor or override in subclasses.

### UnityPlayer Java API

```java
// Load Unity
UnityPlayer mUnityPlayer = new UnityPlayer(this, this);

// Send message to Unity (like iOS sendMessageToGOWithName)
UnityPlayer.UnitySendMessage("GameObjectName", "MethodName", "message");

// Pause/Resume
mUnityPlayer.pause();
mUnityPlayer.resume();

// Unload (keeps runtime in memory, can reload)
mUnityPlayer.unload();

// Quit (terminates Unity process, cannot restart)
mUnityPlayer.quit();
```

## React Native Integration (@azesmway)

### Android-Specific Setup

The `@azesmway/react-native-unity` package handles most integration via:

```java
// ReactNativeUnityViewManager.java
public class ReactNativeUnityViewManager extends SimpleViewManager<UnityView> {

    // Send message to mobile app (Unity → RN)
    public static void sendMessageToMobileApp(String message) {
        // Emits event to React Native
    }
}
```

### Messaging Bridge (Android)

**RN → Unity**:
```java
UnityPlayer.UnitySendMessage("BridgeTarget", "OnMessage", jsonPayload);
```

**Unity → RN**:
```csharp
// BridgeTarget.cs
#if UNITY_ANDROID
using (var jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
{
    jc.CallStatic("sendMessageToMobileApp", payload);
}
#endif
```

### BridgeTarget.cs Android Implementation

```csharp
private void SendToMobileApp(string payload)
{
#if UNITY_ANDROID
    try
    {
        using (var jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
        {
            jc.CallStatic("sendMessageToMobileApp", payload);
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[Bridge] Android sendMessage failed: {e.Message}");
    }
#elif UNITY_IOS && !UNITY_EDITOR
    // iOS implementation...
#endif
}
```

## Known Workarounds

### Android 7.* Task Background Issue

Some Android 7 devices incorrectly handle `frontOfTask` state, causing the whole task to go to background instead of returning to Main activity.

**Fix**: Add to `MainUnityActivity.java` or `UnityPlayerGameActivity.java`:

```java
@Override
public void onUnityPlayerQuitted() {
    SharedClass.showMainActivity("");
    finish();
}
```

## Gradle Configuration

### settings.gradle

```groovy
// Include Unity library module
include ':unityLibrary'
project(':unityLibrary').projectDir = new File('../UnityProject/androidBuild/unityLibrary')
```

### app/build.gradle

```groovy
dependencies {
    implementation project(':unityLibrary')
}
```

## Known Limitations

1. **Full-screen only** - Unity cannot render partial screen (unless Unity Industry customer)
2. **Single instance** - Cannot load multiple Unity runtimes
3. **Third-party plugins** - May need adaptation for UAAL
4. **Xamarin** - Not compatible with Xamarin app platform
5. **Play Feature Delivery** - Cannot integrate as dynamic module

## iOS vs Android Comparison

| Feature | iOS | Android |
|---------|-----|---------|
| Framework/Module | UnityFramework.framework | unityLibrary module |
| Message API | sendMessageToGOWithName | UnitySendMessage |
| Native→Unity | NativeCallProxy | AndroidJavaClass |
| Process Model | Same process | Separate process (`:Unity`) |
| Entry Point | UIApplicationDelegate | Activity/GameActivity |
| Lifecycle | UnityFrameworkListener | IUnityPlayerLifecycleEvents |

## Debugging

### ADB Logs

```bash
# All Unity logs
adb logcat -v color -s Unity

# Filter by Bridge prefix
adb logcat | grep -E "\[Bridge\]"

# Clear and monitor
adb logcat -c && adb logcat -v color -s Unity
```

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Unity not loading | Missing unityLibrary | Check settings.gradle include |
| Messages not received | Wrong class name | Verify AndroidJavaClass path |
| App goes to background | Android 7.* bug | Add onUnityPlayerQuitted workaround |
| Crash on quit | Improper shutdown | Use unload() before quit() |

## Build Flow (React Native)

```bash
# 1. Unity exports Gradle project
./scripts/build_and_run_android.sh

# 2. Gradle builds AAR/APK

# 3. React Native links native module

# 4. Deploy to device
adb install -r app-release.apk
```

## References

- [Unity Docs: Unity as a Library Android](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-Android.html)
- [Unity UAAL Example (Android)](https://github.com/Unity-Technologies/uaal-example/blob/master/docs/android.md)
- [Android Intents Documentation](https://developer.android.com/guide/components/intents-filters)
- [@azesmway/react-native-unity](https://github.com/azesmway/react-native-unity)

---

**Note**: portals_v4 currently focuses on iOS. Android integration follows similar patterns with Gradle instead of CocoaPods.
