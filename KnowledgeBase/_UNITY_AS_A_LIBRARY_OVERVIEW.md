# Unity as a Library - Cross-Platform Overview

**Source**: [Unity 6000.2 Cross-Platform Features](https://docs.unity3d.com/6000.2/Documentation/Manual/cross-platform-features.html)
**UAAL Example Repository**: [Unity-Technologies/uaal-example](https://github.com/Unity-Technologies/uaal-example)
**Last Updated**: 2026-01-08

---

## What is Unity as a Library?

Starting with Unity 2019.3, Unity can be embedded as a library in native platform applications. This allows developers to include Unity-powered features in their native apps:

- 3D/2D Real-Time Rendering
- AR/VR Experiences
- 3D Model Interaction
- 2D Mini-games
- Interactive Visualizations

The Unity Runtime Library exposes controls to manage when and how to load, activate, and unload Unity content.

## Requirements

| Platform | Unity Version |
|----------|---------------|
| Android | Unity 6000.0.0b16 or higher |
| iOS | Unity 2021.3.28f1 or higher |

**Warning**: Using Unity as a Library requires experience with native platform development (Java/Kotlin for Android, Objective-C/Swift for iOS).

## Architecture Overview

### Build Output Structure

Unity generates platform-specific projects with two parts:

| Part | iOS | Android | Description |
|------|-----|---------|-------------|
| **Library** | UnityFramework.framework | unityLibrary (AAR) | Source, plugins, runtime |
| **Launcher** | Unity-iPhone target | launcher module | App representation, starts Unity |

### Integration Pattern

```
Native App
    └── Unity Library (embedded)
        ├── Unity Runtime
        ├── Player Data (scenes, assets)
        └── Native Plugins
```

## Platform-Specific Details

### iOS
- **Framework**: `UnityFramework.framework`
- **API Class**: `UnityFramework` (Objective-C)
- **Key Methods**: `runEmbeddedWithArgc`, `sendMessageToGOWithName`, `unloadApplication`
- **Process Model**: Same process as host app
- **Full Guide**: See `_UNITY_AS_A_LIBRARY_IOS.md`

### Android
- **Module**: `unityLibrary` (Gradle module)
- **API Class**: `UnityPlayer` (Java)
- **Key Methods**: `UnitySendMessage`, `unload`, `pause`, `resume`
- **Process Model**: Separate process (`:Unity`)
- **Full Guide**: See `_UNITY_AS_A_LIBRARY_ANDROID.md`

## Supported Platforms

From official Unity 6000.2 documentation:

| Platform | Documentation |
|----------|---------------|
| Android | [UnityasaLibrary-Android.html](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-Android.html) |
| iOS | [UnityasaLibrary-iOS.html](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-iOS.html) |
| Windows | [UnityasaLibrary-Windows.html](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-Windows.html) |
| UWP | [UnityasaLibrary-UWP.html](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-UWP.html) |

## Memory Overhead

When Unity is in unloaded state (after `Application.Unload`):
- **Range**: 80-180 MB retained
- **Varies by**: Device graphics resolution

This memory is retained to enable instant reload without full reinitialization.

## Known Limitations (Both Platforms)

1. **Full-screen only** - Unity cannot render on part of the screen
2. **Single instance** - Cannot load multiple Unity runtimes
3. **Third-party plugins** - May need adaptation
4. **Lifecycle control** - Unity doesn't control runtime lifecycle

### Platform-Specific Limitations

| Limitation | iOS | Android |
|------------|-----|---------|
| Status bar control | Via Info.plist only | N/A |
| Xamarin compatibility | N/A | Not supported |
| Play Feature Delivery | N/A | Not supported |

## Messaging Bridge Pattern

### React Native → Unity

```typescript
// React Native (TypeScript)
unityRef.current?.sendMessage('BridgeTarget', 'OnMessage', jsonPayload);
```

**iOS Under the Hood**:
```objc
[[ufw] sendMessageToGOWithName:gameObject functionName:method message:msg];
```

**Android Under the Hood**:
```java
UnityPlayer.UnitySendMessage(gameObject, method, msg);
```

### Unity → React Native

```csharp
// Unity C# (BridgeTarget.cs)
public void SendToMobileApp(string payload)
{
#if UNITY_ANDROID
    using (var jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
    {
        jc.CallStatic("sendMessageToMobileApp", payload);
    }
#elif UNITY_IOS && !UNITY_EDITOR
    NativeAPI.sendMessageToMobileApp(payload);
#endif
}
```

## Lifecycle Management

### Load States

```
┌─────────────┐     load()      ┌─────────────┐
│   Unloaded  │ ───────────────>│   Running   │
└─────────────┘                 └─────────────┘
       ▲                              │
       │         unload()             │
       └──────────────────────────────┘

       │         quit()               │
       └─────────────> Process Ends ──┘
```

### Key Lifecycle Events

| Event | iOS | Android | Description |
|-------|-----|---------|-------------|
| Unload | `unityDidUnload:` | `onUnityPlayerUnloaded()` | Unity paused, scenes unloaded |
| Quit | `unityDidQuit:` | `onUnityPlayerQuitted()` | Unity terminated |

### Important: unload() vs quit()

- **unload()**: Releases most memory, Unity can be restarted
- **quit()**: Full termination, Unity CANNOT be restarted in same process

## Cross-Platform Development Tips

### 1. Conditional Compilation

```csharp
#if UNITY_IOS && !UNITY_EDITOR
    // iOS-specific code
#elif UNITY_ANDROID
    // Android-specific code
#else
    // Editor/fallback code
#endif
```

### 2. Unified Message Handler

```csharp
public class BridgeTarget : MonoBehaviour
{
    public void OnMessage(string json)
    {
        // Same handler for both platforms
        var data = JsonUtility.FromJson<MessageData>(json);
        HandleMessage(data);
    }
}
```

### 3. Platform-Specific Initialization

```csharp
void Start()
{
#if UNITY_IOS
    // iOS-specific initialization
#elif UNITY_ANDROID
    // Android-specific initialization
#endif

    // Common initialization
    SendReadyMessage();
}
```

## React Native Package Comparison

| Feature | @azesmway/react-native-unity | Manual Integration |
|---------|------------------------------|-------------------|
| Setup complexity | Low (npm + pod install) | High (manual Xcode/Gradle) |
| Customization | Limited | Full control |
| Maintenance | Package updates | Manual maintenance |
| Build script | Provided | Custom |
| Best for | Standard integrations | Complex/custom needs |

## Build Flow Summary

### iOS (React Native)

```bash
Unity Export → /tmp/unity-ios-export/
            ↓
xcodebuild UnityFramework.framework
            ↓
Copy to unity/builds/ios/
            ↓
pod install (clears cache first)
            ↓
xcodebuild iOS app
```

### Android (React Native)

```bash
Unity Export → Gradle project
            ↓
./gradlew assembleRelease
            ↓
React Native links unityLibrary
            ↓
./gradlew bundleRelease
            ↓
adb install
```

## Debugging

### iOS
```bash
idevicesyslog | grep -E "Bridge|Unity"
```

### Android
```bash
adb logcat -v color -s Unity
```

### Both Platforms
- Add `[Bridge]` prefix to Unity logs
- Add `[UnityArView]` prefix to RN logs
- Use debug toggles for production builds

## Related Documentation

- [_UNITY_AS_A_LIBRARY_IOS.md](./_UNITY_AS_A_LIBRARY_IOS.md) - iOS-specific details
- [_UNITY_AS_A_LIBRARY_ANDROID.md](./_UNITY_AS_A_LIBRARY_ANDROID.md) - Android-specific details
- [Unity Official: Unity as a Library](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary.html)
- [Unity Official: Cross-Platform Features](https://docs.unity3d.com/6000.2/Documentation/Manual/cross-platform-features.html)
- [UAAL Example Repository](https://github.com/Unity-Technologies/uaal-example)

## Other Cross-Platform Features

From Unity's cross-platform features documentation:

| Feature | Description |
|---------|-------------|
| Deep Linking | Enable deep links within your application |
| Xcode Frame Debugger | GPU performance analysis on iOS/macOS |
| Graphics API Support | Platform-specific graphics backends |
| Mobile Accessibility | Accessible mobile applications |
| Unity Remote | Test without building (Android, iOS, tvOS) |

---

**Verified**: portals_v4 implementation correctly follows all official UAAL requirements for iOS. Android support follows the same patterns with Gradle/AAR instead of CocoaPods/Framework.
