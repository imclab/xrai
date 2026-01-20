# Icosa API Client Integration

## Overview

The Icosa API Client enables voice-to-object functionality: users can speak commands like "Put a cat on the table" and the system will search the Icosa Gallery for 3D models and place them in AR.

**Integration Date**: January 17, 2026

**Full Specification**: See `specs/009-icosa-sketchfab-integration/` for complete architecture including:
- Unified search across Icosa + Sketchfab
- Offline caching with LRU eviction
- Attribution tracking for CC-licensed content
- UI panels for browsing and preview

## Packages Installed

| Package | Version/Source | Purpose |
|---------|---------------|---------|
| `com.icosa.icosa-api-client-unity` | Local copy in `Packages/` | 3D model search & import from Icosa Gallery |
| `org.khronos.unitygltf` | GitHub | glTF/GLB model loading (Icosa dependency) |
| `ai.undream.llm` | GitHub | Local LLM inference for AI characters |

## Installation Notes

### Why Local Package Copy?

The Icosa API Client requires `allowUnsafeCode: true` in its assembly definition files. Unity's PackageCache is immutable for Git-based packages, so we copied the package to `Packages/com.icosa.icosa-api-client-unity/` where we have full control.

**Files Modified in Local Package:**
- `Runtime/icosa-api-client-unity-runtime.asmdef` - Set `allowUnsafeCode: true`
- `Editor/icosa-api-client-unity-editor.asmdef` - Set `allowUnsafeCode: true` and added `"Editor"` platform constraint

### Scripting Defines

The following scripting defines were added to `ProjectSettings/ProjectSettings.asset`:

```
ICOSA_API_AVAILABLE
GLTFAST_AVAILABLE
```

These enable conditional compilation for Icosa-dependent code paths.

## Core Components

### WhisperIcosaController

**Location**: `Assets/H3M/Icosa/WhisperIcosaController.cs`

Voice-to-object controller that:
1. Receives voice transcriptions (from Whisper or other speech recognition)
2. Extracts keywords (filters stop words like "a", "the", "put", "place")
3. Searches Icosa Gallery for matching 3D models
4. Imports and places the model in AR

**Usage:**
```csharp
// Get reference to controller
var controller = GetComponent<WhisperIcosaController>();

// Process a voice transcription
controller.ProcessTranscription("Put a cute cat here", 0.95f);

// Or search directly
controller.SearchAndPlace("robot");

// Events
controller.OnTranscriptionReceived += (text) => Debug.Log($"Heard: {text}");
controller.OnKeywordsExtracted += (keywords) => Debug.Log($"Keywords: {string.Join(", ", keywords)}");
controller.OnObjectPlaced += (go) => Debug.Log($"Placed: {go.name}");
controller.OnError += (error) => Debug.LogError(error);

// Undo/clear
controller.UndoLastPlacement();
controller.ClearAllPlacedObjects();
```

**Inspector Settings:**
- `AR Raycast Manager` - For plane detection placement
- `AR Plane Manager` - For plane visualization
- `AR Camera` - Main AR camera (auto-finds if not set)
- `Placement Distance` - Fallback distance when no plane detected (default: 1.5m)
- `Default Scale` - Scale applied to imported models (default: 0.1)
- `Require Plane` - Only place on detected planes (default: true)
- `Min Confidence` - Minimum transcription confidence to process (default: 0.5)
- `Stop Words` - Words to filter from transcriptions

### IcosaAssetMetadata

**Location**: `Assets/H3M/Icosa/IcosaAssetLoader.cs`

Component automatically added to imported Icosa models for attribution tracking.

**Properties:**
- `AssetId` - Icosa asset identifier
- `DisplayName` - Human-readable name
- `AuthorName` - Creator's name
- `License` - License type (CC-BY, CC0, etc.)

### IcosaSettingsCreator

**Location**: `Assets/Scripts/Editor/IcosaSettingsCreator.cs`

Editor utility that auto-creates the required `PtSettings.asset` on Editor load.

**Menu Commands:**
- `H3M > Icosa > Create PtSettings Asset` - Manually create settings
- `H3M > Icosa > Select PtSettings` - Select existing settings

## PtSettings Asset

**Location**: `Assets/Resources/PtSettings.asset`

Required ScriptableObject for Icosa API Client configuration. Created automatically by `IcosaSettingsCreator` on Editor load.

**Configuration Tabs:**
1. **General** - Scene units, shader materials
2. **Editor** - Asset paths, import options
3. **Runtime** - Auth config, cache config

**Access via Menu:** `Icosa > Icosa API Client Settings...`

## API Reference

### IcosaApi (from Icosa package)

```csharp
using IcosaApiClient;

// Search for assets
var request = new IcosaListAssetsRequest
{
    keywords = "cat",
    orderBy = IcosaOrderBy.BEST,
    pageSize = 10
};

IcosaApi.ListAssets(request, result =>
{
    if (result.Ok && result.Value.assets.Count > 0)
    {
        var asset = result.Value.assets[0];
        Debug.Log($"Found: {asset.displayName} by {asset.authorName}");
    }
});

// Import an asset
var options = new IcosaImportOptions
{
    rescalingMode = IcosaImportOptions.RescalingMode.FIT,
    desiredSize = 0.5f, // 50cm
    recenter = true
};

IcosaApi.Import(asset, options, (importedAsset, result) =>
{
    if (result.Ok)
    {
        GameObject model = result.Value.gameObject;
        model.transform.position = Vector3.zero;
    }
});
```

### Key Types

| Type | Description |
|------|-------------|
| `IcosaAsset` | Asset metadata (name, author, license, formats) |
| `IcosaListAssetsRequest` | Search parameters (keywords, category, order) |
| `IcosaListAssetsResult` | Search results (assets list, pagination) |
| `IcosaImportOptions` | Import settings (scaling, centering) |
| `IcosaImportResult` | Import result (gameObject, throttler) |
| `IcosaStatusOr<T>` | Result wrapper with `.Ok`, `.Value`, `.Status` |
| `IcosaOrderBy` | Sort order (BEST, NEWEST, OLDEST) |
| `IcosaCategory` | Asset categories (ANIMALS, ARCHITECTURE, etc.) |

## Troubleshooting

### "Found no PtSettings assets"

The PtSettings asset is missing from Resources. Fix:
1. Run `H3M > Icosa > Create PtSettings Asset`
2. Or restart Unity (auto-creates on load)

### CS0227: Unsafe code error

The package's asmdef files need `allowUnsafeCode: true`. This should already be set in the local package copy. If not:
1. Edit `Packages/com.icosa.icosa-api-client-unity/Runtime/icosa-api-client-unity-runtime.asmdef`
2. Set `"allowUnsafeCode": true`

### Editor scripts not compiling (CS0234: 'Build' not found)

The Editor asmdef needs `"Editor"` in `includePlatforms`. This should already be set. If not:
1. Edit `Packages/com.icosa.icosa-api-client-unity/Editor/icosa-api-client-unity-editor.asmdef`
2. Set `"includePlatforms": ["Editor"]`

### Models not appearing

1. Check AR plane detection is working
2. Verify network connection (Icosa API requires internet)
3. Check console for import errors
4. Try different search keywords

## Testing

**Editor Testing (Context Menu):**
1. Add `WhisperIcosaController` to a GameObject
2. Right-click component header
3. Select test options:
   - `Test: Search 'cat'`
   - `Test: Search 'tree'`
   - `Test: Process 'Put a cute dog here'`
   - `Clear All Objects`

**Device Testing:**
1. Build to iOS device with AR capabilities
2. Connect to Whisper speech recognition
3. Speak commands like "Put a robot here"

## Attribution

Icosa Gallery models are licensed by their creators. The `IcosaAssetMetadata` component stores attribution info. Generate attribution text:

```csharp
var attributions = IcosaApi.GenerateAttributions(
    includeStatic: true,
    runtimeAssets: placedAssets
);
Debug.Log(attributions);
```

## Related Files

- `Assets/H3M/Icosa/WhisperIcosaController.cs` - Main controller
- `Assets/H3M/Icosa/IcosaAssetLoader.cs` - Asset loading utilities
- `Assets/Scripts/Editor/IcosaSettingsCreator.cs` - PtSettings auto-creation
- `Assets/Scripts/Editor/EnableUnsafeCode.cs` - Unsafe code enabler
- `Assets/Resources/PtSettings.asset` - API configuration
- `Packages/com.icosa.icosa-api-client-unity/` - Local package copy

## Roadmap (spec/009)

See `specs/009-icosa-sketchfab-integration/` for planned enhancements:

### Sprint 0: Foundation
- [ ] SketchfabClient.cs - Sketchfab Download API wrapper
- [ ] ModelCache.cs - LRU disk caching for downloaded models
- [ ] Editor settings for Sketchfab API key

### Sprint 1: Unified Search
- [ ] UnifiedModelSearch.cs - Aggregate Icosa + Sketchfab results
- [ ] Cache integration before API calls
- [ ] Source preference setting

### Sprint 2: UI & Voice
- [ ] ModelSearchUI panel with thumbnail grid
- [ ] 3D model preview before placement
- [ ] Attribution panel for CC compliance

### Sprint 3: Polish
- [ ] Error handling and retry logic
- [ ] Performance optimization
- [ ] Device testing on iPhone 15 Pro

## References

- [Icosa Gallery](https://icosa.gallery) - Open source 3D model hosting
- [Icosa GitHub](https://github.com/icosa-foundation/icosa-gallery)
- [Sketchfab Download API](https://sketchfab.com/blogs/community/announcing-the-sketchfab-download-api-a-search-bar-for-the-3d-world/)
- [UnityGLTF](https://github.com/KhronosGroup/UnityGLTF) - glTF runtime loader
