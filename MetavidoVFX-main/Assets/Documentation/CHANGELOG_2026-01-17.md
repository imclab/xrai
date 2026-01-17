# Changelog - January 17, 2026

## Package Installations

### New Packages Added

| Package | Manifest Entry | Purpose |
|---------|---------------|---------|
| LLMUnity | `"ai.undream.llm": "https://github.com/undreamai/LLMUnity.git"` | Local LLM inference for AI characters |
| UnityGLTF | `"org.khronos.unitygltf": "https://github.com/KhronosGroup/UnityGLTF.git"` | glTF/GLB model loading |
| Icosa API Client | `"com.icosa.icosa-api-client-unity": "file:com.icosa.icosa-api-client-unity"` | 3D model search from Icosa Gallery |

### Package Removals

Conflicting packages removed during troubleshooting:
- `com.icosa.open-brush-unity-tools` - GUID conflicts with Icosa API Client
- `com.icosa.strokereceiver` - GUID conflicts with package.json

---

## Bug Fixes

### 1. Icosa API Client - Unsafe Code Compile Error (CS0227)

**Problem:** Package uses unsafe C# code blocks but `allowUnsafeCode` was false in asmdef files.

**Root Cause:** Unity's PackageCache is immutable for Git-based packages. Manual edits are overwritten.

**Solution:**
1. Copied package from `Library/PackageCache/` to `Packages/com.icosa.icosa-api-client-unity/`
2. Modified `Runtime/icosa-api-client-unity-runtime.asmdef`:
   ```json
   "allowUnsafeCode": true
   ```
3. Updated `manifest.json` to use local file reference:
   ```json
   "com.icosa.icosa-api-client-unity": "file:com.icosa.icosa-api-client-unity"
   ```

**Files Modified:**
- `Packages/manifest.json`
- `Packages/com.icosa.icosa-api-client-unity/Runtime/icosa-api-client-unity-runtime.asmdef`

---

### 2. Icosa API Client - Editor Scripts Not Compiling (CS0234)

**Problem:** Editor scripts couldn't find `UnityEditor.Build` namespace.

**Error Messages:**
```
error CS0234: The type or namespace name 'Build' does not exist in the namespace 'UnityEditor'
error CS0246: The type or namespace name 'EditorWindow' could not be found
error CS0246: The type or namespace name 'MenuItem' could not be found
```

**Root Cause:** Editor asmdef was missing platform constraint, causing it to compile for non-Editor platforms.

**Solution:** Modified `Editor/icosa-api-client-unity-editor.asmdef`:
```json
{
    "includePlatforms": ["Editor"],
    "allowUnsafeCode": true
}
```

**Files Modified:**
- `Packages/com.icosa.icosa-api-client-unity/Editor/icosa-api-client-unity-editor.asmdef`

---

### 3. Icosa API Client - Missing PtSettings Asset

**Problem:** Runtime error "Found no PtSettings assets. Re-import Icosa API Client"

**Root Cause:** Icosa requires a `PtSettings.asset` in Resources folder for configuration.

**Solution:** Created `Assets/Scripts/Editor/IcosaSettingsCreator.cs` that:
1. Auto-creates `Assets/Resources/PtSettings.asset` on Editor load
2. Provides menu commands for manual creation

**Files Created:**
- `Assets/Scripts/Editor/IcosaSettingsCreator.cs`
- `Assets/Resources/PtSettings.asset`

---

### 4. WhisperIcosaController - Incorrect API Usage

**Problem:** Code was written for a different Icosa API version.

**Error Messages:**
```
error CS0117: 'IcosaApi' does not contain a definition for 'OrderBy'
error CS1061: 'IcosaStatusOr<T>' does not contain a definition for 'ok'
error CS1061: 'IcosaAsset' does not contain a definition for 'transform'
```

**Root Cause:** The actual Icosa API uses:
- `IcosaListAssetsRequest` object instead of string parameters
- `IcosaStatusOr<T>.Ok` (capital O) instead of `.ok`
- `result.Value.gameObject` for the imported GameObject

**Solution:** Rewrote API calls to match actual package:

**Before:**
```csharp
IcosaApi.ListAssets(keywords, null, IcosaApi.OrderBy.BEST, result =>
{
    if (!result.ok) return;
    // ...
});

IcosaApi.Import(asset, options, (imported, result) =>
{
    if (!result.ok) return;
    imported.transform.position = pos;
});
```

**After:**
```csharp
var request = new IcosaListAssetsRequest
{
    keywords = keywords,
    orderBy = IcosaOrderBy.BEST
};

IcosaApi.ListAssets(request, result =>
{
    if (!result.Ok) return;
    // ...
});

IcosaApi.Import(asset, options, (importedAsset, result) =>
{
    if (!result.Ok) return;
    var importedObject = result.Value.gameObject;
    importedObject.transform.position = pos;
});
```

**Files Modified:**
- `Assets/H3M/Icosa/WhisperIcosaController.cs`

---

### 5. App UI Samples - Rotate Constructor Error (CS1729)

**Problem:** Unity 6 changed the `Rotate` constructor API.

**Error Message:**
```
error CS1729: 'Rotate' does not contain a constructor that takes 1 arguments
```

**Root Cause:** In Unity 6, the `Rotate` struct requires fully qualified namespace due to potential conflicts.

**Solution:** Used fully qualified type names:

**Before:**
```csharp
element.style.rotate = new StyleRotate(new Rotate(30f * progress));
```

**After:**
```csharp
element.style.rotate = new UnityEngine.UIElements.StyleRotate(
    new UnityEngine.UIElements.Rotate(
        UnityEngine.UIElements.Angle.Degrees(30f * progress)
    )
);
```

**Files Modified:**
- `Assets/Samples/App UI/1.3.1/UI Kit/Scripts/Examples.cs` (lines 468, 479)

---

## Scripting Define Changes

**Added to `ProjectSettings/ProjectSettings.asset` (iPhone platform):**
```
ICOSA_API_AVAILABLE
GLTFAST_AVAILABLE
```

These enable conditional compilation blocks in:
- `Assets/H3M/Icosa/WhisperIcosaController.cs`
- `Assets/H3M/Icosa/IcosaAssetLoader.cs`

---

## New Files Created

| File | Purpose |
|------|---------|
| `Assets/Scripts/Editor/IcosaSettingsCreator.cs` | Auto-creates PtSettings asset |
| `Assets/Scripts/Editor/EnableUnsafeCode.cs` | Enables unsafe code via PlayerSettings API |
| `Assets/Resources/PtSettings.asset` | Icosa API configuration |
| `Assets/Documentation/ICOSA_INTEGRATION.md` | Integration documentation |
| `Packages/com.icosa.icosa-api-client-unity/` | Local copy of Icosa package |

---

## Files Modified

| File | Changes |
|------|---------|
| `Packages/manifest.json` | Added LLMUnity, UnityGLTF, Icosa packages |
| `ProjectSettings/ProjectSettings.asset` | Added scripting defines |
| `Assets/H3M/Icosa/WhisperIcosaController.cs` | Fixed API usage |
| `Assets/Samples/App UI/1.3.1/UI Kit/Scripts/Examples.cs` | Fixed Rotate constructor |

---

## Verification

After all fixes, the project compiles with:
- **0 errors**
- **Warnings only** (unassigned fields in XR Hands samples, duplicate using directives)

Console should show no errors related to:
- Unsafe code (CS0227)
- Missing namespaces (CS0234, CS0246)
- API mismatches (CS0117, CS1061, CS1729)
- Missing PtSettings

---

## Related Documentation

- [ICOSA_INTEGRATION.md](./ICOSA_INTEGRATION.md) - Full integration guide
- [README.md](./README.md) - System overview
- [SYSTEM_ARCHITECTURE.md](./SYSTEM_ARCHITECTURE.md) - Architecture documentation
