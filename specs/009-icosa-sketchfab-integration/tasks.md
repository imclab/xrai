# Tasks: Icosa & Sketchfab 3D Model Integration

**Spec**: `009-icosa-sketchfab-integration`
**Created**: 2026-01-20

## Sprint Overview

| Sprint | Focus | Status |
|--------|-------|--------|
| Sprint 0 | Foundation & Sketchfab Client | Not Started |
| Sprint 1 | Unified Search & Caching | Not Started |
| Sprint 2 | UI & Voice Integration | Not Started |
| Sprint 3 | Polish & Testing | Not Started |

---

## Sprint 0: Foundation & Sketchfab Client

### Task 0.1: Sketchfab API Client
**Priority**: P1 | **Estimate**: 4h | **Status**: Not Started

Create `SketchfabClient.cs` wrapper for Sketchfab Download API.

**Subtasks**:
- [ ] Create `Assets/H3M/Icosa/SketchfabClient.cs`
- [ ] Implement OAuth token storage in `PtSettings`
- [ ] Implement `SearchModels(query, page, pageSize)` async method
- [ ] Implement `GetModelDownloadUrl(uid)` for glTF retrieval
- [ ] Add `SKETCHFAB_AVAILABLE` scripting define check
- [ ] Handle rate limiting (429 responses) with exponential backoff

**API Reference**:
```csharp
public class SketchfabClient
{
    public static async Task<SketchfabSearchResult> SearchModels(
        string query,
        int page = 1,
        int pageSize = 24,
        bool downloadableOnly = true);

    public static async Task<string> GetDownloadUrl(string modelUid);

    public static bool IsAuthenticated { get; }
    public static void SetApiToken(string token);
}
```

**Acceptance**:
- [ ] Search returns results from Sketchfab
- [ ] Only CC-licensed models returned when `downloadableOnly = true`
- [ ] Rate limit handled gracefully

---

### Task 0.2: Model Cache System
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Create `ModelCache.cs` for LRU disk caching of downloaded models.

**Subtasks**:
- [ ] Create `Assets/H3M/Icosa/ModelCache.cs`
- [ ] Implement `index.json` manifest for cache metadata
- [ ] Implement `TryGetCached(id)` → returns path or null
- [ ] Implement `CacheModel(id, source, bytes)` → writes to disk
- [ ] Implement `EvictOldest(targetSize)` for LRU cleanup
- [ ] Add cache statistics (total size, model count, hit rate)

**Cache Structure**:
```
Application.persistentDataPath/IcosaCache/
├── index.json
├── models/
│   ├── icosa_{id}.glb
│   └── sketchfab_{uid}.glb
└── thumbnails/
    └── {id}.png
```

**Acceptance**:
- [ ] Models cached to disk after download
- [ ] Cache hit returns local path without network
- [ ] LRU eviction keeps cache under 500MB default

---

### Task 0.3: Editor Settings Extension
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Extend `IcosaSettingsCreator` and `PtSettings` for Sketchfab configuration.

**Subtasks**:
- [ ] Add Sketchfab API token field to `PtSettings` inspector
- [ ] Add cache settings (maxSize, maxAge) to `PtSettings`
- [ ] Create `H3M > Icosa > Configure Sketchfab API Key` menu item
- [ ] Create `H3M > Icosa > Clear Model Cache` menu item
- [ ] Add OAuth browser flow for Sketchfab authentication

**Acceptance**:
- [ ] API token persists in `PtSettings.asset`
- [ ] Cache can be cleared from menu
- [ ] Settings visible in Icosa API Client Settings window

---

## Sprint 1: Unified Search & Caching

### Task 1.1: Unified Model Search
**Priority**: P1 | **Estimate**: 4h | **Status**: Not Started

Create `UnifiedModelSearch.cs` to aggregate results from both APIs.

**Subtasks**:
- [ ] Create `Assets/H3M/Icosa/UnifiedModelSearch.cs`
- [ ] Implement parallel search to Icosa + Sketchfab
- [ ] Implement result merging with relevance scoring
- [ ] Add source preference setting (Icosa-first, parallel, Sketchfab-first)
- [ ] Handle single-source fallback when one API fails
- [ ] Implement result deduplication by model name similarity

**API Reference**:
```csharp
public class UnifiedModelSearch
{
    public enum SearchMode { IcosaFirst, SketchfabFirst, Parallel }

    public static async Task<List<UnifiedSearchResult>> Search(
        string query,
        SearchMode mode = SearchMode.Parallel,
        int maxResults = 24);
}

public class UnifiedSearchResult
{
    public string Id;
    public string DisplayName;
    public string AuthorName;
    public string License;
    public string ThumbnailUrl;
    public string Source; // "icosa" or "sketchfab"
    public float RelevanceScore;
}
```

**Acceptance**:
- [ ] Single query returns results from both sources
- [ ] Results sorted by relevance, not source
- [ ] Graceful degradation when one API unavailable

---

### Task 1.2: Extend IcosaAssetLoader for Sketchfab
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Generalize `IcosaAssetLoader` to handle both Icosa and Sketchfab URLs.

**Subtasks**:
- [ ] Add `DownloadAndImport(UnifiedSearchResult)` method
- [ ] Add download progress callback
- [ ] Integrate with ModelCache (check cache before download)
- [ ] Write to cache after successful import
- [ ] Handle Sketchfab redirect URLs (signed S3 URLs)

**Acceptance**:
- [ ] Same import flow for both sources
- [ ] Progress reported for downloads >5MB
- [ ] Cache hit skips download entirely

---

### Task 1.3: Extend WhisperIcosaController
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Update existing controller to use unified search.

**Subtasks**:
- [ ] Replace direct Icosa API call with `UnifiedModelSearch.Search()`
- [ ] Add `SearchMode` setting to inspector
- [ ] Add cache lookup before search
- [ ] Add "search in progress" status to events

**Acceptance**:
- [ ] Voice commands search both sources
- [ ] Cached models load instantly
- [ ] Events fire for all search stages

---

## Sprint 2: UI & Voice Integration

### Task 2.1: Model Search UI Panel
**Priority**: P2 | **Estimate**: 5h | **Status**: Not Started

Create UI Toolkit panel for browsing and selecting models.

**Subtasks**:
- [ ] Create `Assets/H3M/Icosa/UI/ModelSearchUI.cs`
- [ ] Create `Assets/H3M/Icosa/UI/ModelSearchUI.uxml`
- [ ] Create `Assets/H3M/Icosa/UI/ModelSearchUI.uss`
- [ ] Implement search field with debounce (300ms)
- [ ] Implement grid view of results with thumbnails
- [ ] Add source badge (Icosa/Sketchfab icon)
- [ ] Implement tap-to-preview → tap-to-place flow
- [ ] Add loading spinner during search/download

**UI Layout**:
```
┌─────────────────────────────────────────┐
│ [🔍 Search models...              ] [X] │
├─────────────────────────────────────────┤
│ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐        │
│ │ 📷  │ │ 📷  │ │ 📷  │ │ 📷  │        │
│ │cat  │ │dog  │ │bird │ │fish │        │
│ │ 🅘  │ │ 🅢  │ │ 🅘  │ │ 🅢  │        │
│ └─────┘ └─────┘ └─────┘ └─────┘        │
│ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐        │
│ │ ... │ │ ... │ │ ... │ │ ... │        │
│ └─────┘ └─────┘ └─────┘ └─────┘        │
├─────────────────────────────────────────┤
│ [Load More]          Page 1 of 5        │
└─────────────────────────────────────────┘
```

**Acceptance**:
- [ ] Search field triggers search on typing
- [ ] Results display with thumbnails and source badges
- [ ] Tap result opens preview panel
- [ ] Place button adds model to AR scene

---

### Task 2.2: Model Preview Panel
**Priority**: P2 | **Estimate**: 3h | **Status**: Not Started

Create 3D preview before placing in AR.

**Subtasks**:
- [ ] Create preview camera + render texture setup
- [ ] Import model into preview scene (off-screen)
- [ ] Show model rotating with orbit controls
- [ ] Display metadata (name, author, license)
- [ ] Add "Place in AR" button
- [ ] Add "Cancel" button

**Acceptance**:
- [ ] Model renders in preview before download completes
- [ ] User can orbit/zoom preview
- [ ] Attribution visible in preview

---

### Task 2.3: Attribution Panel
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Create UI for viewing and exporting attributions.

**Subtasks**:
- [ ] Create `AttributionPanel.cs` and `.uxml`
- [ ] List all placed models with license info
- [ ] Add "Copy Attribution Text" button
- [ ] Add "Export to File" button
- [ ] Highlight models requiring attribution (CC-BY, CC-BY-NC, etc.)

**Acceptance**:
- [ ] All placed models listed with licenses
- [ ] Copy produces valid attribution text
- [ ] CC0 models marked as "no attribution required"

---

### Task 2.4: Voice Feedback Integration
**Priority**: P3 | **Estimate**: 2h | **Status**: Not Started

Add audio/visual feedback for voice commands.

**Subtasks**:
- [ ] Add "Searching..." audio cue
- [ ] Add "Found: [model name]" audio feedback
- [ ] Add "Placing..." visual indicator
- [ ] Add "Done" confirmation sound
- [ ] Add "Not found" error feedback

**Acceptance**:
- [ ] User knows when voice command recognized
- [ ] User knows when model found
- [ ] User knows when placement complete

---

## Sprint 3: Polish & Testing

### Task 3.1: Error Handling & Recovery
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Robust error handling for all failure modes.

**Subtasks**:
- [ ] Handle network timeout with retry UI
- [ ] Handle invalid glTF with skip + error log
- [ ] Handle oversized models (>100MB) with warning
- [ ] Handle Sketchfab auth expiry with re-auth prompt
- [ ] Handle AR session interruption during placement

**Acceptance**:
- [ ] No unhandled exceptions in any failure mode
- [ ] User informed of recoverable errors
- [ ] Automatic retry for transient failures

---

### Task 3.2: Performance Optimization
**Priority**: P2 | **Estimate**: 3h | **Status**: Not Started

Optimize for smooth AR experience.

**Subtasks**:
- [ ] Thumbnail loading: lazy load as user scrolls
- [ ] Model import: stream large files instead of full download
- [ ] Cache: preload frequently used models on app start
- [ ] Memory: unload preview models after placement

**Acceptance**:
- [ ] Search UI maintains 60fps during loading
- [ ] Large model download doesn't freeze app
- [ ] Memory usage stays under 200MB additional

---

### Task 3.3: Device Testing
**Priority**: P1 | **Estimate**: 4h | **Status**: Not Started

Comprehensive testing on iOS devices.

**Subtasks**:
- [ ] Test on iPhone 12 (baseline)
- [ ] Test on iPhone 15 Pro (target device)
- [ ] Test offline mode with cached models
- [ ] Test voice-to-object end-to-end
- [ ] Test with 10+ placed models in scene
- [ ] Profile memory and battery usage

**Test Scenarios**:
1. Voice: "Put a cat here" → model appears on floor
2. Search: "dragon" → browse → select → place
3. Offline: Previously downloaded model loads
4. Attribution: Copy text for 5 models → verify format

**Acceptance**:
- [ ] All test scenarios pass on target devices
- [ ] Performance meets targets (see spec)
- [ ] No crashes during extended use

---

### Task 3.4: Documentation Update
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Update all related documentation.

**Subtasks**:
- [ ] Update `ICOSA_INTEGRATION.md` with Sketchfab info
- [ ] Add Sketchfab setup to `README.md`
- [ ] Document new menu commands in `CLAUDE.md`
- [ ] Add troubleshooting section for common issues
- [ ] Create API reference for new public classes

**Acceptance**:
- [ ] All new components documented
- [ ] Setup instructions complete
- [ ] Troubleshooting covers common issues

---

## Checklist Summary

### Sprint 0 (Foundation)
- [ ] Task 0.1: SketchfabClient.cs
- [ ] Task 0.2: ModelCache.cs
- [ ] Task 0.3: Editor settings extension

### Sprint 1 (Unified Search)
- [ ] Task 1.1: UnifiedModelSearch.cs
- [ ] Task 1.2: Extend IcosaAssetLoader
- [ ] Task 1.3: Extend WhisperIcosaController

### Sprint 2 (UI & Voice)
- [ ] Task 2.1: Model Search UI Panel
- [ ] Task 2.2: Model Preview Panel
- [ ] Task 2.3: Attribution Panel
- [ ] Task 2.4: Voice feedback integration

### Sprint 3 (Polish)
- [ ] Task 3.1: Error handling
- [ ] Task 3.2: Performance optimization
- [ ] Task 3.3: Device testing
- [ ] Task 3.4: Documentation update

---

*Total Estimated Effort: ~42 hours across 4 sprints*
