# MetavidoVFX Test Suite

**Status**: Placeholder - Tests to be implemented
**Created**: 2026-01-15

---

## Test Structure

```
Assets/Tests/
├── EditMode/           # Edit-time tests (no Play mode required)
│   ├── VFXBinderTests.cs
│   ├── AudioProcessorTests.cs
│   └── ConfigurationTests.cs
│
├── PlayMode/           # Runtime tests (require Play mode)
│   ├── ARPipelineTests.cs
│   ├── HandTrackingTests.cs
│   └── VFXRenderingTests.cs
│
└── README.md           # This file
```

---

## Planned Test Coverage

### Edit Mode Tests

| Test Class | Purpose | Priority |
|------------|---------|----------|
| `VFXBinderTests` | Verify ARDepthSource + VFXARBinder binding | P1 |
| `AudioProcessorTests` | Test AudioBridge bands (legacy: EnhancedAudioProcessor) | P1 |
| `ConfigurationTests` | Validate ProjectSettings, manifest.json | P2 |
| `BuildScriptTests` | Test scene references exist | P1 |

### Play Mode Tests

| Test Class | Purpose | Priority |
|------------|---------|----------|
| `ARPipelineTests` | Verify AR Foundation initialization | P1 |
| `HandTrackingTests` | Test hand gesture detection | P2 |
| `VFXRenderingTests` | Verify VFX renders without errors | P1 |
| `HologramPlaybackTests` | Test Metavido decode pipeline | P2 |

---

## How to Run Tests

### In Unity Editor
1. Open `Window > General > Test Runner`
2. Select `EditMode` or `PlayMode` tab
3. Click `Run All` or select specific tests

### From Command Line
```bash
# Edit Mode Tests
/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$(pwd)" \
  -runTests -testPlatform EditMode \
  -testResults results.xml

# Play Mode Tests (requires display or virtual framebuffer)
/Applications/Unity/Hub/Editor/6000.2.14f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$(pwd)" \
  -runTests -testPlatform PlayMode \
  -testResults results.xml
```

---

## Sample Test Implementation

```csharp
// Assets/Tests/EditMode/VFXBinderTests.cs
using NUnit.Framework;
using UnityEngine;
// ARDepthSource is in the global namespace

[TestFixture]
public class VFXBinderTests
{
    [Test]
    public void ARDepthSource_InitializesWithoutErrors()
    {
        var go = new GameObject("TestARDepthSource");
        var source = go.AddComponent<ARDepthSource>();

        Assert.IsNotNull(source);
        Assert.IsTrue(source.enabled);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ARDepthSource_RequiresARManagers()
    {
        var go = new GameObject("TestARDepthSource");
        var source = go.AddComponent<ARDepthSource>();

        // TODO: assert on expected warning if AR managers are missing.
        source.SendMessage("Start");

        Object.DestroyImmediate(go);
    }
}
```

---

## Test Dependencies

Add to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.unity.test-framework": "1.4.5"
  }
}
```

---

## CI Integration

Tests run automatically via GitHub Actions:
- Edit Mode: On every push
- Play Mode: On PR to main (requires self-hosted runner)

See `.github/workflows/ios-build.yml` for configuration.

---

## Coverage Goals

| Phase | Target Coverage |
|-------|-----------------|
| MVP | 20% (critical paths) |
| Beta | 50% (core systems) |
| Release | 70% (all public APIs) |

---

*Created: 2026-01-15*
*Author: Claude Code*
