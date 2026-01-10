# Unity Asset Management Reference

**Source**: [Unity 6000.2 Asset Management](https://docs.unity3d.com/6000.2/Documentation/Manual/assets-managing-introduction.html)
**Last Updated**: 2026-01-08

---

## Overview

Unity provides several asset management systems for different use cases:

| System | Best For | Remote Loading | Incremental Updates |
|--------|----------|----------------|---------------------|
| **Direct References** | Small apps, fixed content | No | No |
| **Resources** | Prototyping, simple apps | No | No |
| **AssetBundles** | Complex apps, DLC | Yes | Yes |
| **Addressables** | Large apps, CDN delivery | Yes | Yes |
| **ECS Content** | Entities-based projects | Yes | Yes |

## Build Content Determination

Unity includes content based on:
1. Scenes in **Scene List** (Build Settings)
2. **Directly referenced** assets from those scenes
3. Assets in **Resources** folders (always included)
4. Assets assigned to **AssetBundles**
5. Assets marked as **Addressable**

---

## 1. Direct References (Default)

### How It Works
- Drag assets to scenes/components in Inspector
- Unity bundles referenced assets with scenes
- All assets loaded before scene loads

### Advantages
- Simple, no extra code
- Only referenced assets included in build
- Works with ScriptableObjects for efficient management

### Limitations
- Entire asset file loads into memory before scene
- No dynamic loading/unloading (scene-level only)
- Assets must be local (no CDN)
- Full rebuild required for any changes

### Best For
- Small applications
- Fixed content
- Simple projects

---

## 2. Resources System

### How It Works
```csharp
// Create folder: Assets/Resources/
// Place assets inside

// Load at runtime
GameObject prefab = Resources.Load<GameObject>("Prefabs/MyPrefab");
Texture2D tex = Resources.Load<Texture2D>("Textures/MyTexture");

// Unload unused
Resources.UnloadUnusedAssets();
```

### Advantages
- Simple API
- No build configuration needed
- Good for prototyping

### Limitations
- **All** Resources folder assets included in build (even unused)
- Can cause large build sizes
- No remote loading
- Full rebuild for changes
- Slow startup with many assets
- String-based loading = runtime errors if missing

### Best For
- Prototyping
- Small projects
- Assets needed throughout app lifetime

---

## 3. AssetBundle System

### How It Works
```csharp
// Build AssetBundles (Editor script)
BuildPipeline.BuildAssetBundles(
    outputPath,
    BuildAssetBundleOptions.None,
    BuildTarget.iOS
);

// Load AssetBundle
AssetBundle bundle = AssetBundle.LoadFromFile(path);
// Or from web
UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);

// Load asset from bundle
GameObject prefab = bundle.LoadAsset<GameObject>("MyPrefab");

// Unload bundle
bundle.Unload(false); // Keep loaded assets
bundle.Unload(true);  // Unload everything
```

### Advantages
- On-demand downloading
- DLC and post-release updates
- Remote hosting (CDN)
- Incremental updates

### Limitations
- Script-only API (no Editor UI)
- Manual dependency management
- Manual memory management (potential leaks)
- Manual location tracking (local vs remote)

### Use Cases
- DLC content
- Large asset libraries
- Streaming content

---

## 4. Addressables Package

### How It Works
```csharp
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// Mark asset as Addressable in Editor
// Assign address like "Characters/Hero"

// Load by address
AsyncOperationHandle<GameObject> handle =
    Addressables.LoadAssetAsync<GameObject>("Characters/Hero");
handle.Completed += OnLoadComplete;

// Or with await
GameObject hero = await Addressables.LoadAssetAsync<GameObject>("Characters/Hero").Task;

// Instantiate
Addressables.InstantiateAsync("Characters/Hero");

// Release
Addressables.Release(handle);
```

### Advantages
- Built on AssetBundles but with Editor UI
- Automatic dependency management
- Automatic memory management
- Address-based loading (location independent)
- CDN support
- Incremental updates

### When to Use
- Large applications
- CDN-hosted content
- Frequent content updates
- Complex asset dependencies

### When NOT to Use
- Fixed content shipped in initial download
- Simple applications (use Direct References instead)

---

## 5. ECS Content Management

For projects using **Entities** package:

```csharp
// Uses weak references and content archives
// See: com.unity.entities package documentation
```

---

## Comparison Matrix

| Feature | Direct | Resources | AssetBundles | Addressables |
|---------|--------|-----------|--------------|--------------|
| Setup complexity | None | Low | High | Medium |
| Editor UI | Yes | No | No | Yes |
| Dynamic loading | No | Yes | Yes | Yes |
| Remote hosting | No | No | Yes | Yes |
| Auto dependencies | N/A | N/A | No | Yes |
| Memory management | Auto | Manual | Manual | Auto |
| Incremental updates | No | No | Yes | Yes |
| Build size control | Good | Poor | Good | Good |

---

## React Native Unity Considerations

For Unity as a Library projects:

### Resources System (Recommended for RN)
```csharp
// BridgeTarget.cs - Load VFX from Resources
var vfxAsset = Resources.Load<VisualEffectAsset>("VFX/SimpleBrush");
if (vfxAsset != null)
{
    var vfx = brushGO.AddComponent<VisualEffect>();
    vfx.visualEffectAsset = vfxAsset;
}
```

**Why Resources for RN Unity:**
- Simple integration
- No complex build pipeline
- Assets bundled with UnityFramework
- Works within single framework file

### Addressables in RN Unity
- More complex setup
- Requires catalog configuration
- Better for large asset libraries
- Consider if assets > 100MB

---

## Best Practices

### Memory Management
```csharp
// Always unload unused assets after scene transitions
Resources.UnloadUnusedAssets();
System.GC.Collect();
```

### Asset Organization
```
Assets/
├── Resources/           # Runtime-loaded assets
│   ├── VFX/
│   ├── Prefabs/
│   └── Materials/
├── Scenes/              # Scene files
├── Scripts/             # C# scripts
├── Plugins/             # Native plugins
│   └── iOS/
└── StreamingAssets/     # Raw files (copied as-is)
```

### Loading Patterns
```csharp
// Async loading (preferred)
ResourceRequest request = Resources.LoadAsync<GameObject>("Prefabs/Heavy");
yield return request;
GameObject obj = request.asset as GameObject;

// Sync loading (blocks main thread)
GameObject obj = Resources.Load<GameObject>("Prefabs/Light");
```

---

## Build Reports & Size Optimization

### BuildReport API

The `BuildReport` class (`UnityEditor.Build.Reporting`) provides detailed build information:

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;

public class BuildAnalyzer
{
    [MenuItem("Build/Build and Analyze")]
    static void BuildAndAnalyze()
    {
        BuildReport report = BuildPipeline.BuildPlayer(
            new[] { "Assets/Scenes/Main.unity" },
            "Builds/MyGame",
            BuildTarget.iOS,
            BuildOptions.None
        );

        // Access build summary
        Debug.Log($"Result: {report.summary.result}");
        Debug.Log($"Total Size: {report.summary.totalSize} bytes");
        Debug.Log($"Build Time: {report.summary.totalTime}");
        Debug.Log($"Errors: {report.SummarizeErrors()}");

        // List all output files
        foreach (var file in report.GetFiles())
        {
            Debug.Log($"{file.path}: {file.size} bytes ({file.role})");
        }

        // Review build steps
        foreach (var step in report.steps)
        {
            Debug.Log($"{step.name}: {step.duration}");
        }
    }
}
```

### BuildReport Properties

| Property | Description |
|----------|-------------|
| `summary` | BuildSummary with overall stats (result, size, time) |
| `steps` | Array of BuildStep with timing info |
| `packedAssets` | Array of PackedAssets in build |
| `strippingInfo` | Native code stripping information |
| `scenesUsingAssets` | Asset usage per scene (requires `DetailedBuildReport`) |

### BuildReport Methods

| Method | Description |
|--------|-------------|
| `GetFiles()` | Returns all output files with sizes and roles |
| `SummarizeErrors()` | Returns string summary of build errors |
| `GetLatestReport()` | Static - gets most recent build report |

### AssetBundle Build Reports

```csharp
// Build AssetBundles
BuildPipeline.BuildAssetBundles(outputPath, bundleOptions, target);

// Get report immediately after
BuildReport report = BuildReport.GetLatestReport();
if (report != null)
{
    // Analyze bundle build
}
```

### Build Report Inspector Package

Install `com.unity.build-report-inspector` for visual analysis:

```
Window → Analysis → Build Report Inspector
```

Features:
- Build size breakdown by asset type
- Unused asset detection
- Duplicate asset warnings
- Platform-specific analysis

### Size Optimization Strategies

1. **Texture Compression**: Use platform-appropriate formats (ASTC for iOS/Android)
2. **Mesh Compression**: Enable in import settings
3. **Audio Compression**: Use appropriate codec per platform
4. **Code Stripping**: Enable in Player Settings (IL2CPP)
5. **Unused Assets**: Remove from Resources folders

---

## References

- [Direct Reference Asset Management](https://docs.unity3d.com/6000.2/Documentation/Manual/assets-direct-reference.html)
- [Resources System](https://docs.unity3d.com/6000.2/Documentation/Manual/LoadingResourcesatRuntime.html)
- [AssetBundles Introduction](https://docs.unity3d.com/6000.2/Documentation/Manual/AssetBundlesIntro.html)
- [Addressables Package](https://docs.unity3d.com/Packages/com.unity.addressables@latest)
- [Resources API](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Resources.html)
- [BuildReport API](https://docs.unity3d.com/ScriptReference/Build.Reporting.BuildReport.html)
- [Build Report Inspector](https://docs.unity3d.com/Packages/com.unity.build-report-inspector@0.3/manual/index.html)
- [Reducing File Size](https://docs.unity3d.com/6000.2/Documentation/Manual/ReducingFilesize.html)
