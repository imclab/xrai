# Feature Specification: Icosa & Sketchfab 3D Model Integration

**Feature Branch**: `009-icosa-sketchfab-integration`
**Created**: 2026-01-20
**Status**: In Progress (Core implemented 2026-01-21)
**Implementation**:
- ✅ UnifiedModelSearch.cs - Dual-source search aggregation
- ✅ SketchfabClient.cs - Sketchfab API wrapper
- ✅ ModelCache.cs - LRU disk cache (500MB default)
- ✅ ModelSearchUI.cs - UI Toolkit search panel
- ✅ ModelPlacer.cs - AR placement on planes
- ✅ IcosaAssetMetadata.cs - CC attribution tracking
- ⬜ Voice integration (WhisperIcosaController wiring)
- ⬜ GLTFast runtime loading
**Input**: Voice-to-object and search-to-placement for 3D models from Icosa Gallery and Sketchfab

## Triple Verification

| Source | Status | Notes |
|--------|--------|-------|
| KB `_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md` | Pending | Add Icosa/Sketchfab repos |
| `ICOSA_INTEGRATION.md` | Verified | Existing WhisperIcosaController |
| Icosa Gallery API | Verified | `api.icosa.gallery/v1/docs` |
| Sketchfab Unity Plugin | Verified | Editor-focused, glTF transport |
| Device Testing | Pending | iOS AR placement validation |

## Overview

This spec extends the existing Icosa integration to provide a unified 3D model search and placement system:

1. **Unified Search** - Query both Icosa Gallery and Sketchfab from one interface
2. **Voice-to-Object** - "Put a cat on the table" → search → import → AR placement
3. **Runtime glTF Loading** - Download and instantiate models at runtime
4. **Attribution Tracking** - Automatic CC license compliance
5. **Offline Caching** - Cache downloaded models for repeated use
6. **AR Placement** - Place models on detected planes or at fixed distance

### Platform Comparison

| Feature | Icosa Gallery | Sketchfab |
|---------|---------------|-----------|
| License | Open Source (Apache-2.0) | Commercial (free tier) |
| Models | ~50k+ (restored from Poly) | 500k+ (150k+ CC-licensed) |
| API | REST + OpenAPI | REST + OAuth |
| Format | glTF/GLB | glTF/GLB |
| Unity SDK | icosa-toolkit-unity | sketchfab/unity-plugin |
| Runtime | Yes (via UnityGLTF) | Limited (editor-focused) |
| Self-Host | Yes | No |
| Auth | Optional | Required for downloads |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Voice-to-Object Placement (Priority: P1)

As a user, I want to speak a command like "Put a robot here" and have a 3D model appear in AR.

**Why this priority**: Core differentiating feature for AR experiences.

**Independent Test**:
1. Enable microphone and AR session
2. Speak "Put a cute cat here"
3. Verify keyword extraction ("cat")
4. Verify model search across Icosa/Sketchfab
5. Verify model imports and places on detected plane

**Acceptance Scenarios**:
1. **Given** valid speech input, **When** confidence > 0.5, **Then** keywords extracted and search initiated
2. **Given** search results, **When** model found, **Then** first result auto-imports
3. **Given** AR plane detected, **When** model ready, **Then** places on nearest plane
4. **Given** no plane detected, **When** model ready, **Then** places at `placementDistance` (1.5m default)

---

### User Story 2 - Manual Search and Browse (Priority: P1)

As a user, I want to search for 3D models by keyword and browse results before placing.

**Why this priority**: Users need control over model selection.

**Independent Test**:
1. Open search UI panel
2. Enter "dragon" in search field
3. Verify results from both Icosa and Sketchfab
4. Tap thumbnail to preview
5. Tap "Place" to add to AR scene

**Acceptance Scenarios**:
1. **Given** search query, **When** submitted, **Then** results show source (Icosa/Sketchfab badge)
2. **Given** results loaded, **When** scrolling, **Then** pagination fetches more results
3. **Given** model selected, **When** preview requested, **Then** 3D preview displays before placement

---

### User Story 3 - Attribution and Licensing (Priority: P1)

As a developer, I want automatic attribution tracking for all imported models.

**Why this priority**: Legal compliance for Creative Commons content.

**Independent Test**:
1. Import model with CC-BY license
2. Verify `IcosaAssetMetadata` component attached
3. Call `GenerateAttributions()` API
4. Verify attribution text includes author, license, URL

**Acceptance Scenarios**:
1. **Given** imported model, **When** queried, **Then** metadata includes: assetId, displayName, authorName, license
2. **Given** multiple models, **When** attribution generated, **Then** all models listed with correct licenses
3. **Given** CC-BY-NC model, **When** commercial use detected, **Then** warning displayed

---

### User Story 4 - Offline Caching (Priority: P2)

As a user, I want previously downloaded models available offline.

**Why this priority**: Network independence for AR experiences.

**Independent Test**:
1. Search and import "robot" model (online)
2. Disable network
3. Search "robot" again
4. Verify cached model loads without network

**Acceptance Scenarios**:
1. **Given** model downloaded, **When** cached, **Then** stored in `Application.persistentDataPath/IcosaCache/`
2. **Given** cache hit, **When** same search, **Then** loads from cache (no network)
3. **Given** cache full, **When** new download, **Then** LRU eviction frees space

---

### User Story 5 - Multi-Source Search Aggregation (Priority: P2)

As a user, I want search results from both Icosa and Sketchfab merged intelligently.

**Why this priority**: Best results regardless of source.

**Independent Test**:
1. Search "spaceship"
2. Verify results from both sources
3. Verify sorting by relevance (not source)
4. Verify deduplication of similar models

**Acceptance Scenarios**:
1. **Given** search query, **When** both APIs respond, **Then** results merged by relevance score
2. **Given** Icosa offline, **When** Sketchfab available, **Then** graceful fallback to single source
3. **Given** API rate limit, **When** exceeded, **Then** queued retry with backoff

---

### Edge Cases

- Network timeout during download → Retry with exponential backoff
- Invalid glTF file → Log error, skip model, try next result
- AR session not ready → Queue placement until planes detected
- Very large model (>50MB) → Show progress indicator, warn user
- Model with animations → Import as Legacy animation type
- Sketchfab auth expired → Prompt re-authentication

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST search Icosa Gallery via REST API
- **FR-002**: System MUST search Sketchfab via Download API (CC-licensed models)
- **FR-003**: System MUST import glTF/GLB models at runtime
- **FR-004**: System MUST track attribution metadata for all imported models
- **FR-005**: System MUST cache downloaded models locally
- **FR-006**: System MUST place models on AR planes when available
- **FR-007**: System MUST support voice-to-object via WhisperIcosaController
- **FR-008**: System MUST handle network failures gracefully

### Non-Functional Requirements

- **NFR-001**: Search results MUST return within 3 seconds
- **NFR-002**: Model import MUST show progress for downloads >5MB
- **NFR-003**: Cache MUST support LRU eviction with configurable max size (default: 500MB)
- **NFR-004**: System MUST work offline with cached models

### Key Components

| Component | File | Purpose |
|-----------|------|---------|
| WhisperIcosaController | `H3M/Icosa/WhisperIcosaController.cs` | Voice transcription → search → place |
| IcosaAssetLoader | `H3M/Icosa/IcosaAssetLoader.cs` | Model import and metadata |
| UnifiedModelSearch | `H3M/Icosa/UnifiedModelSearch.cs` | NEW: Multi-source search aggregator |
| SketchfabClient | `H3M/Icosa/SketchfabClient.cs` | NEW: Sketchfab API wrapper |
| ModelCache | `H3M/Icosa/ModelCache.cs` | NEW: LRU cache for downloaded models |
| ModelSearchUI | `H3M/Icosa/UI/ModelSearchUI.cs` | NEW: Search and browse UI |
| IcosaSettingsCreator | `Scripts/Editor/IcosaSettingsCreator.cs` | PtSettings auto-creation |

### Dependencies

| Package | Source | Purpose |
|---------|--------|---------|
| `com.icosa.icosa-api-client-unity` | Local (Packages/) | Icosa Gallery API |
| `org.khronos.unitygltf` | GitHub | glTF/GLB runtime loading |
| `ai.undream.llm` | GitHub | Optional: Local LLM for smarter keyword extraction |

### Scripting Defines

```
ICOSA_API_AVAILABLE      - Icosa API client installed
GLTFAST_AVAILABLE        - glTF loader available
SKETCHFAB_AVAILABLE      - Sketchfab client configured
```

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Voice command to model placement in <10 seconds (network dependent)
- **SC-002**: Search returns results from both sources in <3 seconds
- **SC-003**: Cached model loads in <1 second
- **SC-004**: Attribution generation includes all required CC fields
- **SC-005**: 95% of glTF models import successfully
- **SC-006**: Cache eviction maintains <500MB storage by default

## Architecture

### Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      Input Sources                               │
│  ┌─────────────────┐  ┌──────────────────┐  ┌────────────────┐  │
│  │ Voice (Whisper) │  │ Text Search UI   │  │ Deep Link URL  │  │
│  └────────┬────────┘  └────────┬─────────┘  └───────┬────────┘  │
└───────────┼────────────────────┼─────────────────────┼───────────┘
            │                    │                     │
            v                    v                     v
┌───────────────────────────────────────────────────────────────────┐
│              WhisperIcosaController / UnifiedModelSearch          │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ 1. Extract keywords (filter stop words)                     │  │
│  │ 2. Check cache for exact match                              │  │
│  │ 3. Query Icosa API + Sketchfab API in parallel              │  │
│  │ 4. Merge and rank results                                   │  │
│  └─────────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬────────────────────────────────────┘
                               │
            ┌──────────────────┼──────────────────┐
            v                  v                  v
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  Icosa Gallery  │  │   Sketchfab     │  │   ModelCache    │
│  api.icosa.     │  │   api.sketchfab │  │   (LRU local)   │
│  gallery/v1/    │  │   .com/v3/      │  │                 │
└────────┬────────┘  └────────┬────────┘  └────────┬────────┘
         │                    │                     │
         v                    v                     v
┌───────────────────────────────────────────────────────────────────┐
│                     IcosaAssetLoader                              │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ 1. Download glTF/GLB from URL                               │  │
│  │ 2. Import via UnityGLTF                                     │  │
│  │ 3. Apply scaling/centering                                  │  │
│  │ 4. Attach IcosaAssetMetadata                                │  │
│  │ 5. Cache to disk                                            │  │
│  └─────────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬────────────────────────────────────┘
                               │
                               v
┌───────────────────────────────────────────────────────────────────┐
│                     AR Placement                                   │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │ 1. Raycast to AR plane (if available)                       │  │
│  │ 2. Fallback to camera-forward placement                     │  │
│  │ 3. Apply default scale (0.1x)                               │  │
│  │ 4. Parent to AR anchor for tracking                         │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

### API Integration

#### Icosa Gallery API

```csharp
// Search
GET https://api.icosa.gallery/v1/assets?q={keywords}&page={page}&pageSize={size}

// Response
{
  "assets": [
    {
      "id": "abc123",
      "displayName": "Cute Cat",
      "authorName": "artist42",
      "license": "CC-BY",
      "formats": [
        { "formatType": "GLTF2", "url": "https://..." }
      ],
      "thumbnail": { "url": "https://..." }
    }
  ],
  "totalCount": 150
}
```

#### Sketchfab Download API

```csharp
// Search (requires OAuth token for downloads)
GET https://api.sketchfab.com/v3/search?type=models&q={keywords}&downloadable=true

// Response
{
  "results": [
    {
      "uid": "xyz789",
      "name": "Robot Character",
      "user": { "displayName": "creator99" },
      "license": { "slug": "cc-by-nc" },
      "thumbnails": { "images": [...] },
      "archives": {
        "gltf": { "url": "https://..." }
      }
    }
  ]
}
```

### Caching Strategy

```
Application.persistentDataPath/
└── IcosaCache/
    ├── index.json           # Cache manifest (id → file mapping)
    ├── models/
    │   ├── icosa_abc123.glb
    │   ├── sketchfab_xyz789.glb
    │   └── ...
    └── thumbnails/
        ├── icosa_abc123.png
        └── ...
```

**LRU Eviction**:
- Track last access time in `index.json`
- When cache exceeds `maxCacheSize` (default 500MB), evict oldest 20%
- Thumbnails evicted separately from models

## Implementation Notes

### Existing Components to Extend

1. **WhisperIcosaController** (`Assets/H3M/Icosa/WhisperIcosaController.cs`)
   - Add Sketchfab search fallback
   - Add cache lookup before API calls
   - Add source preference setting (Icosa-first, Sketchfab-first, parallel)

2. **IcosaAssetLoader** (`Assets/H3M/Icosa/IcosaAssetLoader.cs`)
   - Generalize for Sketchfab URLs
   - Add progress callback for large downloads
   - Add cache write after successful import

3. **IcosaSettingsCreator** (`Assets/Scripts/Editor/IcosaSettingsCreator.cs`)
   - Add Sketchfab API key configuration
   - Add cache size settings

### New Components to Create

1. **UnifiedModelSearch** - Aggregates both APIs
2. **SketchfabClient** - Sketchfab API wrapper with OAuth
3. **ModelCache** - LRU cache with disk persistence
4. **ModelSearchUI** - UI Toolkit search/browse panel

### Menu Commands

- `H3M > Icosa > Create PtSettings Asset` (existing)
- `H3M > Icosa > Configure Sketchfab API Key` (NEW)
- `H3M > Icosa > Clear Model Cache` (NEW)
- `H3M > Icosa > Setup Voice-to-Object Scene` (NEW)

## Security Considerations

- Sketchfab API key stored in `PtSettings.asset` (not in version control)
- OAuth tokens refreshed automatically before expiry
- Downloaded models scanned for oversized meshes (DoS prevention)
- User consent required before first download (GDPR)

## References

### Icosa Gallery
- [GitHub: icosa-foundation/icosa-gallery](https://github.com/icosa-foundation/icosa-gallery)
- [Unity Toolkit: icosa-gallery/icosa-toolkit-unity](https://github.com/icosa-gallery/icosa-toolkit-unity)
- [API Docs: api.icosa.gallery/v1/docs](https://api.icosa.gallery/v1/docs)
- [Open Collective: Icosa Foundation](https://opencollective.com/icosa)

### Sketchfab
- [GitHub: sketchfab/unity-plugin](https://github.com/sketchfab/unity-plugin)
- [GitHub: sketchfab/UnityGLTF](https://github.com/sketchfab/UnityGLTF)
- [Download API Announcement](https://sketchfab.com/blogs/community/announcing-the-sketchfab-download-api-a-search-bar-for-the-3d-world/)
- [Fab Integration Update](https://sketchfab.com/blogs/community/sketchfab-update-what-you-need-to-know-now-that-fabs-live/)

### glTF
- [Khronos UnityGLTF](https://github.com/KhronosGroup/UnityGLTF)
- [glTF 2.0 Spec](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html)

---

*Created: 2026-01-20*
*Author: Claude Code + User*
