# React Native Unity Package Comparison

**Last Updated**: 2026-01-08
**Current Project**: portals_v4 uses `@artmajeur/react-native-unity@0.0.6`

> **Reference Implementation**: [YourArtOfficial/react-native-unity](https://github.com/YourArtOfficial/react-native-unity)
> This is our canonical reference - identical to @artmajeur (same repository, different org mirror).

> **Note**: This document covers our active packages (@azesmway/@artmajeur).
> For the alternative fusetools template, see the **Alternative Packages** section at the end.

---

## Package Overview

| Aspect | @azesmway/react-native-unity | @artmajeur/react-native-unity |
|--------|------------------------------|-------------------------------|
| **NPM Package** | `@azesmway/react-native-unity` | `@artmajeur/react-native-unity` |
| **Version** | 1.0.11 | 0.0.6 |
| **Repository** | [azesmway/react-native-unity](https://github.com/azesmway/react-native-unity) | [2ico/react-native-unity](https://github.com/2ico/react-native-unity) |
| **Relationship** | Original | Fork with improvements |
| **RN Version** | 0.72.7 | 0.76.9 |
| **Status** | Stable, widely used | Active development |

---

## Key Differences

### 1. iOS Framework Path (podspec)

**@azesmway** - Hardcoded path only:
```ruby
s.prepare_command =
<<-CMD
  cp -R ../../../unity/builds/ios/ ios/
CMD
```

**@artmajeur** - Supports environment variable:
```ruby
s.prepare_command = <<-CMD
  if [ -z "${UNITY_FRAMEWORK_DIR}" ]; then
    cp -R ../../../unity/builds/ios/ ios/
  else
    cp -R "${UNITY_FRAMEWORK_DIR}/" ios/
  fi
CMD
```

**Impact**: @artmajeur allows flexible Unity build locations via `UNITY_FRAMEWORK_DIR` env var.

---

### 2. iOS View Lifecycle (RNUnityView.mm)

#### layoutSubviews

**@azesmway** - Always adds subview:
```objc
- (void)layoutSubviews {
   [super layoutSubviews];
   if([self unityIsInitialized]) {
      self.ufw.appController.rootView.frame = self.bounds;
      [self addSubview:self.ufw.appController.rootView];  // Always adds
   }
}
```

**@artmajeur** - Checks before adding (prevents duplicates):
```objc
- (void)layoutSubviews {
   [super layoutSubviews];
   if([self unityIsInitialized]) {
      self.ufw.appController.rootView.frame = self.bounds;
      // Check if rootView is already a subview before adding
      if (![self.subviews containsObject:self.ufw.appController.rootView]) {
        [self addSubview:self.ufw.appController.rootView];
        [self.ufw.appController.rootView setNeedsLayout];
        [self.ufw.appController.rootView layoutIfNeeded];
        [self.ufw.appController.rootView setNeedsDisplay];
      }
   }
}
```

**Impact**: @artmajeur prevents duplicate subview additions and forces immediate layout.

---

#### prepareForRecycle (New Architecture)

**@azesmway** - Full unload on recycle:
```objc
- (void)prepareForRecycle {
    [super prepareForRecycle];
    if ([self unityIsInitialized]) {
      [[self ufw] unloadApplication];  // Full unload
      // ... remove subviews ...
      [self setUfw:nil];
    }
}
```

**@artmajeur** - Pause instead of unload (performance improvement):
```objc
- (void)prepareForRecycle {
    [super prepareForRecycle];
    if ([self unityIsInitialized]) {
      // Don't unload Unity completely, just remove from view hierarchy
      [self.ufw.appController.rootView removeFromSuperview];
      // Pause Unity instead of unloading
      [[self ufw] pause:true];
      // Don't set ufw to nil - keep the framework instance alive
    }
}
```

**Impact**: @artmajeur dramatically faster view recycling - Unity stays loaded, just paused.

---

#### updateProps (New Architecture)

**@azesmway** - Only initializes if not ready:
```objc
- (void)updateProps:(Props::Shared const &)props oldProps:(Props::Shared const &)oldProps {
    if (![self unityIsInitialized]) {
      [self initUnityModule];
    }
    [super updateProps:props oldProps:oldProps];
}
```

**@artmajeur** - Handles resume after pause:
```objc
- (void)updateProps:(Props::Shared const &)props oldProps:(Props::Shared const &)oldProps {
    if ([self unityIsInitialized]) {
        // Resume Unity if it was paused
        [[self ufw] pause:false];
    } else {
        [self initUnityModule];
    }
    [super updateProps:props oldProps:oldProps];
}
```

**Impact**: @artmajeur properly resumes paused Unity on prop updates.

---

#### dealloc (Memory Management)

**@azesmway** - No explicit dealloc.

**@artmajeur** - Proper cleanup:
```objc
- (void)dealloc {
    if (sharedInstance == self) {
        sharedInstance = nil;
    }
    if ([self unityIsInitialized]) {
        [[self ufw] unloadApplication];
        NSArray *viewsToRemove = self.subviews;
        for (UIView *v in viewsToRemove) {
            [v removeFromSuperview];
        }
        [self setUfw:nil];
    }
}
```

**Impact**: @artmajeur has explicit cleanup preventing memory leaks.

---

### 3. Android Implementation

**Both packages are IDENTICAL** (same file SHA). No differences in Android code.

---

### 4. Expo Plugin

**@azesmway** - References external build:
```javascript
module.exports = require('./plugin/build');
```

**@artmajeur** - Self-contained plugin with:
- Android `settings.gradle` modifications
- Android `build.gradle` flatDir configuration
- Android `gradle.properties` unityStreamingAssets
- Android `strings.xml` game_view_content_description
- iOS Podfile arm64 exclusion for simulator

**Impact**: @artmajeur has more complete Expo support out-of-the-box.

---

## Recent @artmajeur Commits (Aug 2025)

1. **iOS unloading workaround** - "keep unity without unloading and reinitializing upon recycle and prop update"
2. **UNITY_FRAMEWORK_DIR variable** - Flexible Unity build path support
3. **Updated packages** - RN 0.76.9 support

---

## Feature Comparison

| Feature | @azesmway | @artmajeur |
|---------|-----------|------------|
| iOS Device | ✅ | ✅ |
| iOS Simulator | ❌ | ❌ |
| Android Device | ✅ | ✅ |
| Android Emulator | ✅ | ✅ |
| New Architecture (Fabric) | ✅ | ✅ (improved) |
| Unity 2023+ | ✅ | ✅ |
| Expo Plugin | Basic | Complete |
| Flexible framework path | ❌ | ✅ |
| View recycling perf | Basic | Optimized |

---

## API (Both Packages)

### Props
- `style: ViewStyle` - Required, use `flex: 1`
- `onUnityMessage?: (event)` - Unity → RN messages
- `androidKeepPlayerMounted?: boolean` - Keep player on blur (Android)
- `fullScreen?: boolean` - Default true (Android)

### Methods
- `postMessage(gameObject, methodName, message)` - RN → Unity
- `unloadUnity()` - Unload Unity runtime
- `pauseUnity(pause: boolean)` - Pause/resume
- `windowFocusChanged(hasFocus)` - Focus recovery (Android)

---

## Recommendation for portals_v4

### Stay with @azesmway if:
- Current implementation works well
- Not using New Architecture heavily
- Prefer stable, widely-used package

### Consider @artmajeur if:
- Need New Architecture performance
- Want flexible Unity build paths
- Need better Expo integration
- Experience view recycling issues

### Migration Path
```bash
# Replace package
npm uninstall @azesmway/react-native-unity
npm install @artmajeur/react-native-unity

# Update imports (same API)
# No code changes needed - API is identical
```

---

## Known Issues (Both)

1. **iOS Simulator** - Not supported (Unity limitation)
2. **Zero dimensions crash** - `MTLTextureDescriptor has width of zero`
   - Solution: Ensure parent view has dimensions before mounting
3. **Single instance** - Cannot run multiple Unity views

---

## Alternative Package: react-native-unity2 (fusetools)

> **IMPORTANT**: This is a SEPARATE ALTERNATIVE, not compatible with @azesmway/@artmajeur.
> Different architecture, different Unity C# scripts, different API.

### Overview

| Aspect | react-native-unity2 |
|--------|---------------------|
| **NPM Package** | `react-native-unity2` |
| **Version** | 1.4.3 |
| **Repository** | [fusetools/react-native-unity2](https://github.com/fusetools/react-native-unity2) |
| **Author** | Fusetools AS |
| **RN Version** | >=0.64.1 (tested 0.75.3) |
| **Unity Versions** | 2020.x, 2021.x, 2022.x |

### Key Differentiators

1. **Promise-based API** - Can get return values from Unity methods:
```typescript
// fusetools - async/await with return values
const result = await UnityModule.callMethod("Cube", "getAccountRN");
console.log(result); // Gets actual return value!

// @azesmway/@artmajeur - fire and forget
unityRef.current.postMessage("Cube", "doSomething", "message");
// No return value possible
```

2. **Included Unity Project** - Complete pre-configured Unity template
3. **Swift Support** - Native Swift AppDelegate integration
4. **Newtonsoft.Json** - Uses Newtonsoft for Unity serialization
5. **RNPromise System** - Built-in promise/reject pattern in C#

### Unity C# Bridge (fusetools)

```csharp
// RNBridge.cs - Send message to RN
RNBridge.SendMessage(new { type = "event", data = myData });

// RNPromise.cs - Promise-based method handling
public void getAccountRN(object param) {
    var promise = RNPromise<string>.Begin(param);
    try {
        promise.Resolve(GetAccount(promise.input));
    } catch (Exception e) {
        promise.Reject(e.Message);
    }
}
```

### Setup Differences

| Aspect | @azesmway/@artmajeur | fusetools |
|--------|---------------------|-----------|
| Unity build location | `unity/builds/ios/` | `unity/builds/ios/` |
| Unity files to copy | NativeCallProxy only | Complete RNUnity folder |
| Xcode workspace | Not required | Required (add Unity-iPhone.xcodeproj) |
| AppDelegate changes | None | Required (lifecycle methods) |
| iOS main.m changes | None | Required (setArgc/setArgv) |

### When to Use fusetools

**Choose fusetools if you need:**
- Return values from Unity methods
- Complete Unity project template
- Swift AppDelegate support
- Starting a new project from scratch

**Stick with @artmajeur if you:**
- Already have working integration
- Don't need return values
- Want simpler setup (no AppDelegate changes)
- Need New Architecture optimizations

---

## 3-Way Comparison Matrix

| Feature | @azesmway | @artmajeur | fusetools |
|---------|-----------|------------|-----------|
| **Version** | 1.0.11 | 0.0.6 | 1.4.3 |
| **RN Support** | 0.72.7 | 0.76.9 | >=0.64.1 |
| **New Architecture** | ✅ | ✅ (optimized) | ❌ |
| **iOS Device** | ✅ | ✅ | ✅ |
| **iOS Simulator** | ❌ | ❌ | ❌ |
| **Android Device** | ✅ | ✅ | ✅ |
| **Android Emulator** | ✅ | ✅ | ✅ |
| **Promise API** | ❌ | ❌ | ✅ |
| **Return values** | ❌ | ❌ | ✅ |
| **Unity template** | ❌ | ❌ | ✅ |
| **Swift support** | ❌ | ❌ | ✅ |
| **Expo plugin** | Basic | Complete | ❌ |
| **View recycling** | Basic | Optimized | Basic |
| **Flexible paths** | ❌ | ✅ | ❌ |
| **AppDelegate mods** | None | None | Required |
| **Maintenance** | Active | Active | Active |

---

## References

### Primary (portals_v4)
- [**YourArtOfficial/react-native-unity**](https://github.com/YourArtOfficial/react-native-unity) - **Reference Implementation** (our canonical source)
- [@artmajeur GitHub](https://github.com/2ico/react-native-unity) - Same as YourArtOfficial (mirror)
- [@azesmway GitHub](https://github.com/azesmway/react-native-unity) - Original package

### Alternative
- [fusetools GitHub](https://github.com/fusetools/react-native-unity2)
- [fusetools Getting Started](https://github.com/fusetools/react-native-unity2/blob/main/docs/getting-started.md)

### Unity Documentation
- [Unity as a Library iOS](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-iOS.html)
- [Unity as a Library Android](https://docs.unity3d.com/6000.2/Documentation/Manual/UnityasaLibrary-Android.html)
