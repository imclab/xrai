# Tasks: Open Brush Integration

**Spec**: `011-openbrush-integration`
**Created**: 2026-01-20

## Sprint Overview

| Sprint | Focus | Status | Effort |
|--------|-------|--------|--------|
| Sprint 0 | Project Setup & Brush Infrastructure | Not Started | ~8h |
| Sprint 1 | Core Brush Materials (Tier 1) | Not Started | ~12h |
| Sprint 2 | Audio Reactive System | Not Started | ~8h |
| Sprint 3 | Mirror/Symmetry System | Not Started | ~10h |
| Sprint 4 | Save/Load System | Not Started | ~6h |
| Sprint 5 | API Painting System | Not Started | ~8h |
| Sprint 6 | Enhanced Brushes (Tier 2) & Polish | Not Started | ~10h |

**Total Estimated Effort**: ~62 hours across 7 sprints

---

## Sprint 0: Project Setup & Brush Infrastructure

### Task 0.1: Create Brush Namespace Structure
**Priority**: P1 | **Estimate**: 1h | **Status**: Not Started

Create folder structure and namespace for brush system.

**Subtasks**:
- [ ] Create `Assets/Scripts/Brush/` directory
- [ ] Create namespace `Metavido.Brush`
- [ ] Create assembly definition `Metavido.Brush.asmdef`
- [ ] Add reference to `Metavido.Core` assembly

**File Structure**:
```
Assets/Scripts/Brush/
├── Metavido.Brush.asmdef
├── Core/
│   ├── BrushManager.cs
│   ├── BrushData.cs
│   ├── BrushStroke.cs
│   └── ControlPoint.cs
├── Input/
│   └── BrushInput.cs
├── Mirror/
│   ├── BrushMirror.cs
│   └── SymmetryTypes.cs
├── Audio/
│   └── BrushAudioReactive.cs
├── Serialization/
│   └── BrushSerializer.cs
└── API/
    └── BrushApi.cs
```

**Acceptance**:
- [ ] Namespace compiles without errors
- [ ] Assembly definition resolves dependencies

---

### Task 0.2: Create BrushData ScriptableObject
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Define brush data structure based on Open Brush's BrushDescriptor.

**Subtasks**:
- [ ] Create `BrushData.cs` ScriptableObject
- [ ] Define brush properties (name, GUID, material, geometry type)
- [ ] Define audio reactive parameters struct
- [ ] Create `BrushGeometryType` enum (Flat, Tube, Particles)
- [ ] Add default brush size, color properties
- [ ] Add texture references (albedo, normal, noise)

**Code Structure**:
```csharp
[CreateAssetMenu(fileName = "Brush", menuName = "Metavido/Brush Data")]
public class BrushData : ScriptableObject
{
    public string BrushName;
    public Guid BrushGuid;  // For .tilt compatibility
    public Material Material;
    public BrushGeometryType GeometryType;

    [Header("Audio Reactive")]
    public bool IsAudioReactive;
    public AudioReactiveParams AudioParams;

    [Header("Defaults")]
    public float DefaultSize = 0.02f;
    public Color DefaultColor = Color.white;

    [Header("Geometry")]
    public float MinSize = 0.001f;
    public float MaxSize = 0.5f;
    public int TubeSegments = 8;
}

public enum BrushGeometryType { Flat, Tube, Particles, Quad }

[Serializable]
public struct AudioReactiveParams
{
    public bool ModulateSize;
    public float SizeAmplitude;
    public bool ModulateColor;
    public bool ModulateEmission;
}
```

**Acceptance**:
- [ ] BrushData assets can be created via menu
- [ ] All properties serialize correctly
- [ ] Inspector shows organized fields

---

### Task 0.3: Create BrushStroke Class
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Implement stroke data and mesh generation based on PointerScript.cs.

**Subtasks**:
- [ ] Create `BrushStroke.cs` class
- [ ] Create `ControlPoint` struct
- [ ] Implement `AddPoint(ControlPoint cp)` for stroke building
- [ ] Implement `GenerateFlatMesh()` for ribbon geometry
- [ ] Implement `GenerateTubeMesh()` for tube geometry
- [ ] Add mesh optimization (reduce point count, simplify)

**Reference**: Open Brush `PointerScript.cs:1000-1500` (mesh generation)

**Key Algorithm** (Flat ribbon):
```csharp
// For each control point:
// 1. Calculate tangent from prev->current->next points
// 2. Calculate bitangent perpendicular to tangent and up
// 3. Create two vertices at ±(size/2) along bitangent
// 4. UV mapping: U = 0|1 for left|right, V = distance along stroke
// 5. Generate triangles connecting quad strips
```

**Acceptance**:
- [ ] Flat ribbons render correctly
- [ ] Tube meshes render correctly
- [ ] Mesh has proper UVs and normals

---

### Task 0.4: Create BrushManager Singleton
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Implement central brush management following Open Brush patterns.

**Subtasks**:
- [ ] Create `BrushManager.cs` singleton
- [ ] Implement brush registry (name → BrushData)
- [ ] Implement `SetBrush(string name)`
- [ ] Implement `BeginStroke()`, `UpdateStroke()`, `EndStroke()`
- [ ] Manage active strokes list
- [ ] Connect to BrushInput for stroke updates

**API**:
```csharp
public class BrushManager : MonoBehaviour
{
    public static BrushManager Instance { get; private set; }

    public BrushData CurrentBrush { get; private set; }
    public Color CurrentColor { get; set; }
    public float CurrentSize { get; set; }

    public List<BrushStroke> AllStrokes { get; }
    public BrushStroke ActiveStroke { get; private set; }

    public void SetBrush(string name);
    public void SetBrush(BrushData brush);
    public void BeginStroke(Vector3 pos, Quaternion rot);
    public void UpdateStroke(Vector3 pos, Quaternion rot, float pressure);
    public void EndStroke();
    public void ClearAllStrokes();
    public void Undo();
}
```

**Acceptance**:
- [ ] Singleton pattern works correctly
- [ ] Brush changes apply to new strokes
- [ ] Stroke lifecycle (begin/update/end) functions

---

## Sprint 1: Core Brush Materials (Tier 1)

### Task 1.1: Import Brush Materials from Open Brush
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Copy and configure Tier 1 brush materials.

**Subtasks**:
- [ ] Create `Assets/Resources/Brushes/` directory structure
- [ ] Copy 10 Tier 1 brush folders from Open Brush
- [ ] Verify shaders are URP compatible
- [ ] Fix any shader compilation errors
- [ ] Create BrushData asset for each brush

**Tier 1 Brushes**:
```
Brushes/
├── Ink/
│   ├── Ink.mat
│   └── BrushData_Ink.asset
├── Light/
├── Flat/
├── Marker/
├── Highlighter/
├── Wire/
├── Waveform/
├── WaveformFFT/
├── Smoke/
└── Fire/
```

**Acceptance**:
- [ ] All 10 materials import without errors
- [ ] Shaders compile for URP
- [ ] BrushData assets created and linked

---

### Task 1.2: Port Essential Brush Shaders
**Priority**: P1 | **Estimate**: 4h | **Status**: Not Started

Convert Open Brush shaders to URP compatible versions.

**Subtasks**:
- [ ] Analyze Open Brush shader structure
- [ ] Create URP shader template for brush rendering
- [ ] Port `Ink` shader (simple unlit)
- [ ] Port `Light` shader (additive, bloom compatible)
- [ ] Port `Flat` shader (lit, double-sided)
- [ ] Port `Marker` shader (opaque)
- [ ] Port `Highlighter` shader (transparent additive)

**Shader Properties to Preserve**:
- `_MainTex` - Albedo texture
- `_Color` - Tint color
- `_EmissionColor` - Emissive (for Light brush)
- `_Cutoff` - Alpha cutoff
- `_TimeOverrideValue` - Animation time (audio reactive)

**Acceptance**:
- [ ] All 5 shaders compile for URP
- [ ] Visual appearance matches Open Brush reference
- [ ] Double-sided rendering works

---

### Task 1.3: Implement Flat Geometry Generator
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Implement flat ribbon mesh generation for basic brushes.

**Subtasks**:
- [ ] Study Open Brush `QuadStripBrushStretchUV.cs`
- [ ] Implement ribbon vertex generation
- [ ] Implement UV mapping (stretch along stroke)
- [ ] Implement pressure-based width variation
- [ ] Add smoothing for control points

**Algorithm**:
```csharp
void GenerateFlatMesh()
{
    var vertices = new List<Vector3>();
    var normals = new List<Vector3>();
    var uvs = new List<Vector2>();

    float totalDist = 0;
    for (int i = 0; i < Points.Count; i++)
    {
        Vector3 tangent = CalculateTangent(i);
        Vector3 normal = Vector3.up; // Or camera facing
        Vector3 bitangent = Vector3.Cross(tangent, normal).normalized;

        float width = Size * Points[i].Pressure;
        Vector3 offset = bitangent * width * 0.5f;

        vertices.Add(Points[i].Position - offset);
        vertices.Add(Points[i].Position + offset);

        normals.Add(normal);
        normals.Add(normal);

        float v = totalDist; // Or normalized 0-1
        uvs.Add(new Vector2(0, v));
        uvs.Add(new Vector2(1, v));

        if (i > 0) totalDist += Vector3.Distance(Points[i].Position, Points[i-1].Position);
    }

    // Generate triangles for quad strip
    GenerateQuadStripTriangles(vertices.Count / 2);
}
```

**Acceptance**:
- [ ] Flat ribbons render correctly
- [ ] Width varies with pressure
- [ ] UVs stretch correctly along stroke

---

### Task 1.4: Implement Tube Geometry Generator
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Implement tube mesh generation for 3D brushes.

**Subtasks**:
- [ ] Study Open Brush `TubeBrush.cs`
- [ ] Implement Frenet-Serret frame calculation
- [ ] Generate circular cross-section vertices
- [ ] Implement smooth normal interpolation
- [ ] Add cap geometry for stroke ends

**Algorithm**:
```csharp
void GenerateTubeMesh(int segments = 8)
{
    for (int i = 0; i < Points.Count; i++)
    {
        Vector3 tangent = CalculateTangent(i);
        Matrix4x4 frame = CalculateFrenetFrame(i, tangent);

        float radius = Size * Points[i].Pressure * 0.5f;

        // Generate ring of vertices
        for (int s = 0; s < segments; s++)
        {
            float angle = (s / (float)segments) * Mathf.PI * 2;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            Vector3 worldPos = frame.MultiplyPoint3x4(localPos);
            Vector3 normal = frame.MultiplyVector(localPos.normalized);

            vertices.Add(worldPos);
            normals.Add(normal);
            uvs.Add(new Vector2(s / (float)segments, i / (float)Points.Count));
        }
    }

    // Generate triangles connecting rings
    GenerateTubeTriangles(segments);
}
```

**Acceptance**:
- [ ] Tube meshes render correctly
- [ ] Normals are smooth across surface
- [ ] End caps close the tube

---

## Sprint 2: Audio Reactive System

### Task 2.1: Port Reaktion Audio Injector
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Integrate keijiro's Reaktion audio analysis.

**Subtasks**:
- [ ] Create `BrushAudioReactive.cs` component
- [ ] Port `AudioInjector.cs` RMS calculation
- [ ] Add FFT frequency band analysis
- [ ] Implement dbLevel smoothing
- [ ] Create public properties for audio values

**Reference**: `_ref/open-brush-feature-pure-openxr/Assets/ThirdParty/Reaktion/`

**Implementation**:
```csharp
public class BrushAudioReactive : MonoBehaviour
{
    public bool Enabled = true;

    [Header("Audio Analysis")]
    public float RmsLevel { get; private set; }  // 0-1 normalized
    public float DbLevel { get; private set; }   // -60 to 0 dB
    public float[] FrequencyBands { get; private set; } = new float[8];

    private const float RefLevel = 0.70710678118f; // 1/sqrt(2)
    private const float MinDb = -60f;

    private float _squareSum;
    private int _sampleCount;

    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            _squareSum += data[i] * data[i];
        }
        _sampleCount += data.Length / channels;
    }

    void Update()
    {
        if (_sampleCount < 1) return;

        float rms = Mathf.Sqrt(_squareSum / _sampleCount);
        RmsLevel = Mathf.Clamp01(rms);
        DbLevel = 20f * Mathf.Log10(rms / RefLevel + 1e-13f);

        _squareSum = 0;
        _sampleCount = 0;
    }
}
```

**Acceptance**:
- [ ] RMS level responds to audio input
- [ ] dB level in expected range
- [ ] Frequency bands calculated

---

### Task 2.2: Implement Audio Modulation System
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Create modulation pipeline from audio to brush parameters.

**Subtasks**:
- [ ] Create `AudioModulation` struct
- [ ] Implement modulation calculation based on brush params
- [ ] Connect to BrushManager for real-time updates
- [ ] Update shader properties with audio values

**Implementation**:
```csharp
public struct AudioModulation
{
    public float SizeMultiplier;
    public float ColorHueShift;
    public float EmissionIntensity;
    public float TimeOverride;
}

public AudioModulation GetModulation(BrushData brush)
{
    if (!brush.IsAudioReactive || !Enabled)
        return AudioModulation.Default;

    var mod = new AudioModulation();
    var p = brush.AudioParams;

    if (p.ModulateSize)
        mod.SizeMultiplier = 1f + RmsLevel * p.SizeAmplitude;

    if (p.ModulateColor)
        mod.ColorHueShift = RmsLevel * 0.1f;

    if (p.ModulateEmission)
        mod.EmissionIntensity = RmsLevel * 2f;

    mod.TimeOverride = Time.time + RmsLevel * 0.5f;

    return mod;
}
```

**Acceptance**:
- [ ] Audio modulates brush size
- [ ] Audio modulates color/emission
- [ ] Smooth transitions between values

---

### Task 2.3: Port Waveform Brush Shader
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Port audio reactive shader with time-based animation.

**Subtasks**:
- [ ] Copy `Waveform.shader` from Open Brush
- [ ] Convert to URP shader graph or HLSL
- [ ] Add `_TimeOverrideValue` property
- [ ] Add `_AudioLevel` property
- [ ] Test with audio input

**Shader Properties**:
```hlsl
Properties
{
    _MainTex ("Texture", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
    _EmissionColor ("Emission", Color) = (1,1,1,1)
    _TimeOverrideValue ("Time Override", Float) = 0
    _AudioLevel ("Audio Level", Range(0,1)) = 0
    _WaveSpeed ("Wave Speed", Float) = 1
    _WaveAmplitude ("Wave Amplitude", Float) = 0.1
}
```

**Acceptance**:
- [ ] Waveform shader compiles for URP
- [ ] Time-based animation works
- [ ] Audio level drives intensity

---

### Task 2.4: Create Audio Reactive Brush Presets
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Configure BrushData for all 5 audio reactive brushes.

**Subtasks**:
- [ ] Create Waveform BrushData (size modulation)
- [ ] Create WaveformFFT BrushData (frequency color)
- [ ] Create WaveformParticles BrushData (emission rate)
- [ ] Create WaveformPulse BrushData (emission intensity)
- [ ] Create WaveformTube BrushData (tube radius)

**Configuration Table**:
| Brush | Size Mod | Color Mod | Emission Mod | Geometry |
|-------|----------|-----------|--------------|----------|
| Waveform | Yes (0.5) | No | Yes | Flat |
| WaveformFFT | No | Yes | Yes | Flat |
| WaveformParticles | No | No | Yes | Particles |
| WaveformPulse | No | No | Yes | Tube |
| WaveformTube | Yes (1.0) | No | No | Tube |

**Acceptance**:
- [ ] All 5 brushes respond to audio
- [ ] Each has distinct audio behavior
- [ ] Visual appearance matches references

---

## Sprint 3: Mirror/Symmetry System

### Task 3.1: Create SymmetryTypes Enums
**Priority**: P1 | **Estimate**: 1h | **Status**: Not Started

Define all symmetry types from Open Brush.

**Subtasks**:
- [ ] Create `PointSymmetryFamily` enum (14 families)
- [ ] Create `WallpaperGroup` enum (17 groups)
- [ ] Create `SymmetryMode` enum
- [ ] Add helper methods for transform count calculation

**Code**:
```csharp
public enum SymmetryMode { None, Point, Wallpaper }

public enum PointSymmetryFamily
{
    Cn,   // Cyclic
    Cnv,  // Cyclic + vertical mirrors
    Cnh,  // Cyclic + horizontal mirror
    Sn,   // Rotoreflection
    Dn,   // Dihedral
    Dnh,  // Dihedral + horizontal mirror
    Dnd,  // Dihedral + diagonal mirrors
    T,    // Tetrahedral
    Th,   // Pyritohedral
    Td,   // Full tetrahedral
    O,    // Chiral octahedral
    Oh,   // Full octahedral
    I,    // Chiral icosahedral
    Ih    // Full icosahedral
}

public enum WallpaperGroup
{
    p1, pg, cm, pm,
    p6, p6m, p3, p3m1, p31m,
    p4, p4m, p4g,
    p2, pgg, pmg, pmm, cmm
}
```

**Acceptance**:
- [ ] All enums defined correctly
- [ ] Helper methods calculate correct counts

---

### Task 3.2: Implement Point Symmetry Transforms
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Calculate point symmetry transforms from Open Brush PointSymmetry.cs.

**Subtasks**:
- [ ] Create `BrushMirror.cs` class
- [ ] Implement cyclic (Cn) transforms
- [ ] Implement dihedral (Dn) transforms
- [ ] Implement polyhedral transforms (T, O, I)
- [ ] Add mirror plane reflections

**Reference**: Open Brush `PointSymmetry.cs`

**Key Algorithm**:
```csharp
List<Matrix4x4> CalculatePointTransforms(PointSymmetryFamily family, int order)
{
    var transforms = new List<Matrix4x4>();

    switch (family)
    {
        case PointSymmetryFamily.Cn:
            // n-fold rotation around Y axis
            for (int i = 0; i < order; i++)
            {
                float angle = (360f / order) * i;
                transforms.Add(Matrix4x4.Rotate(Quaternion.Euler(0, angle, 0)));
            }
            break;

        case PointSymmetryFamily.Cnv:
            // Cn + vertical mirror planes
            var cn = CalculatePointTransforms(PointSymmetryFamily.Cn, order);
            transforms.AddRange(cn);
            foreach (var t in cn)
            {
                transforms.Add(t * Matrix4x4.Scale(new Vector3(-1, 1, 1)));
            }
            break;

        // ... more families
    }

    return transforms;
}
```

**Acceptance**:
- [ ] Cn produces n copies rotated around Y
- [ ] Cnv produces 2n copies (rotations + mirrors)
- [ ] Polyhedral groups produce correct counts

---

### Task 3.3: Implement Wallpaper Group Transforms
**Priority**: P1 | **Estimate**: 3h | **Status**: Not Started

Calculate 2D tiling transforms for wallpaper groups.

**Subtasks**:
- [ ] Study Open Brush `SymmetryGroup.cs`
- [ ] Implement lattice vector calculation
- [ ] Implement p1 (translation only)
- [ ] Implement p6m (hexagonal full symmetry)
- [ ] Implement remaining 15 groups

**Reference**: Open Brush `SymmetryGroup.cs`

**Lattice Types**:
- Oblique: p1, p2
- Rectangular: pm, pg, pmm, pmg, pgg, cmm, cm
- Square: p4, p4m, p4g
- Hexagonal: p3, p3m1, p31m, p6, p6m

**Acceptance**:
- [ ] p1 produces translation grid
- [ ] p4m produces 4-fold rotational + mirrors
- [ ] p6m produces hexagonal tiling

---

### Task 3.4: Integrate Mirror with BrushManager
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Apply symmetry transforms to stroke generation.

**Subtasks**:
- [ ] Add BrushMirror reference to BrushManager
- [ ] Create mirrored strokes during BeginStroke
- [ ] Update all mirror strokes during UpdateStroke
- [ ] Finalize all mirror strokes during EndStroke
- [ ] Add preview visualization for mirror positions

**Implementation**:
```csharp
void BeginStroke(Vector3 pos, Quaternion rot)
{
    var transforms = _mirror.GetTransforms();
    _activeStrokes = new List<BrushStroke>();

    foreach (var t in transforms)
    {
        Vector3 mirrorPos = t.MultiplyPoint3x4(pos);
        Quaternion mirrorRot = t.rotation * rot;

        var stroke = new BrushStroke(CurrentBrush, CurrentColor, CurrentSize);
        stroke.AddPoint(new ControlPoint(mirrorPos, mirrorRot, 1f));
        _activeStrokes.Add(stroke);
    }
}
```

**Acceptance**:
- [ ] Mirror strokes created for each transform
- [ ] All strokes update in sync
- [ ] Preview shows brush positions

---

### Task 3.5: Create Mirror UI Controls
**Priority**: P2 | **Estimate**: 1h | **Status**: Not Started

Add UI for symmetry mode selection.

**Subtasks**:
- [ ] Create symmetry toggle button
- [ ] Create point family dropdown
- [ ] Create order slider (1-12)
- [ ] Create wallpaper group dropdown
- [ ] Display current mirror count

**Acceptance**:
- [ ] UI toggles symmetry modes
- [ ] Family/group changes apply immediately
- [ ] Order changes recalculate transforms

---

## Sprint 4: Save/Load System

### Task 4.1: Define JSON Scene Format
**Priority**: P1 | **Estimate**: 1h | **Status**: Not Started

Design simplified JSON format for scenes.

**Subtasks**:
- [ ] Define `BrushSceneData` class
- [ ] Define `StrokeData` class
- [ ] Define `LayerData` class
- [ ] Add version field for future compatibility
- [ ] Document format specification

**Schema**:
```csharp
[Serializable]
public class BrushSceneData
{
    public string version = "1.0";
    public string created;
    public string[] brushIndex;
    public StrokeData[] strokes;
    public LayerData[] layers;
    public CameraData camera;
}

[Serializable]
public class StrokeData
{
    public int brushIdx;
    public float[] color; // [r,g,b,a]
    public float size;
    public ControlPointData[] points;
}

[Serializable]
public class ControlPointData
{
    public float[] pos; // [x,y,z]
    public float[] rot; // [x,y,z,w] quaternion
    public float pressure;
}
```

**Acceptance**:
- [ ] Classes serialize to valid JSON
- [ ] Format is human-readable
- [ ] Version allows future migration

---

### Task 4.2: Implement BrushSerializer Save
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Implement scene serialization to JSON.

**Subtasks**:
- [ ] Create `BrushSerializer.cs` class
- [ ] Implement `SaveScene(string path)`
- [ ] Build brush index from all used brushes
- [ ] Convert strokes to StrokeData
- [ ] Write JSON to file

**Implementation**:
```csharp
public class BrushSerializer
{
    public void SaveScene(string path)
    {
        var scene = new BrushSceneData
        {
            created = DateTime.UtcNow.ToString("O"),
            brushIndex = BuildBrushIndex(),
            strokes = SerializeStrokes(),
            layers = SerializeLayers(),
            camera = SerializeCamera()
        };

        string json = JsonUtility.ToJson(scene, prettyPrint: true);
        File.WriteAllText(path, json);
    }
}
```

**Acceptance**:
- [ ] Save creates valid JSON file
- [ ] All stroke data preserved
- [ ] File size reasonable (<1MB for 500 strokes)

---

### Task 4.3: Implement BrushSerializer Load
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Implement scene deserialization from JSON.

**Subtasks**:
- [ ] Implement `LoadScene(string path)`
- [ ] Resolve brush index to BrushData assets
- [ ] Reconstruct strokes from StrokeData
- [ ] Rebuild mesh geometry
- [ ] Apply layer visibility

**Implementation**:
```csharp
public void LoadScene(string path)
{
    string json = File.ReadAllText(path);
    var scene = JsonUtility.FromJson<BrushSceneData>(json);

    // Validate version
    if (!IsVersionSupported(scene.version))
        throw new InvalidOperationException($"Unsupported version: {scene.version}");

    // Clear existing
    BrushManager.Instance.ClearAllStrokes();

    // Resolve brushes
    var brushes = ResolveBrushIndex(scene.brushIndex);

    // Recreate strokes
    foreach (var strokeData in scene.strokes)
    {
        var stroke = new BrushStroke(
            brushes[strokeData.brushIdx],
            new Color(strokeData.color[0], strokeData.color[1], strokeData.color[2], strokeData.color[3]),
            strokeData.size
        );

        foreach (var cp in strokeData.points)
        {
            stroke.AddPoint(new ControlPoint(
                new Vector3(cp.pos[0], cp.pos[1], cp.pos[2]),
                new Quaternion(cp.rot[0], cp.rot[1], cp.rot[2], cp.rot[3]),
                cp.pressure
            ));
        }

        stroke.GenerateMesh();
        BrushManager.Instance.AddStroke(stroke);
    }
}
```

**Acceptance**:
- [ ] Load restores all strokes
- [ ] Brush types preserved
- [ ] Colors and sizes correct

---

### Task 4.4: Add Save/Load UI
**Priority**: P2 | **Estimate**: 1h | **Status**: Not Started

Create UI for save/load operations.

**Subtasks**:
- [ ] Add Save button to toolbar
- [ ] Add Load button to toolbar
- [ ] Create file browser for load
- [ ] Add save confirmation dialog
- [ ] Display error messages for failures

**Acceptance**:
- [ ] Save button saves to app storage
- [ ] Load button shows file picker
- [ ] Errors displayed to user

---

## Sprint 5: API Painting System

### Task 5.1: Create HTTP Server Infrastructure
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Set up lightweight HTTP server for API.

**Subtasks**:
- [ ] Create `BrushApi.cs` MonoBehaviour
- [ ] Implement HttpListener-based server
- [ ] Parse query string commands
- [ ] Route to appropriate handlers
- [ ] Return JSON responses

**Reference**: Open Brush `ApiManager.cs`

**Implementation**:
```csharp
public class BrushApi : MonoBehaviour
{
    private HttpListener _listener;
    private const int Port = 40020;
    private Queue<string> _commandQueue = new Queue<string>();

    void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();
        StartCoroutine(HandleRequests());
    }

    IEnumerator HandleRequests()
    {
        while (_listener.IsListening)
        {
            var context = _listener.GetContextAsync();
            // ... handle request
            yield return null;
        }
    }
}
```

**Acceptance**:
- [ ] Server starts on port 40020
- [ ] Responds to GET requests
- [ ] Parses query parameters

---

### Task 5.2: Implement Core Brush API Endpoints
**Priority**: P2 | **Estimate**: 3h | **Status**: Not Started

Create essential brush control endpoints.

**Subtasks**:
- [ ] `brush.type` - Set active brush
- [ ] `brush.size.set` - Set brush size
- [ ] `color.set.rgb` - Set brush color
- [ ] `brush.move.to` - Set brush position
- [ ] `brush.turn.y` - Rotate brush
- [ ] `brush.draw` - Draw line segment

**Endpoint Implementation**:
```csharp
[ApiEndpoint("brush.type", "Set active brush by name")]
void SetBrush(string name)
{
    BrushManager.Instance.SetBrush(name);
}

[ApiEndpoint("brush.move.to", "Move brush to position x,y,z")]
void MoveTo(float x, float y, float z)
{
    _brushPosition = new Vector3(x, y, z);
}

[ApiEndpoint("brush.draw", "Draw line of length")]
void Draw(float length)
{
    Vector3 endPos = _brushPosition + _brushRotation * Vector3.forward * length;

    BrushManager.Instance.BeginStroke(_brushPosition, _brushRotation);
    BrushManager.Instance.UpdateStroke(endPos, _brushRotation, 1f);
    BrushManager.Instance.EndStroke();

    _brushPosition = endPos;
}
```

**Acceptance**:
- [ ] All 6 endpoints functional
- [ ] Commands execute on main thread
- [ ] Position state persists between calls

---

### Task 5.3: Implement Stroke Drawing API
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Create endpoint for drawing complete strokes.

**Subtasks**:
- [ ] `draw.stroke` - Draw stroke from point array
- [ ] Parse point array format
- [ ] Support position, rotation, pressure per point
- [ ] Return stroke ID

**Format**: `draw.stroke=[[x,y,z,rx,ry,rz,p],...]`

**Implementation**:
```csharp
[ApiEndpoint("draw.stroke", "Draw complete stroke from points")]
string DrawStroke(string pointsJson)
{
    var points = JsonUtility.FromJson<ControlPointData[]>(pointsJson);

    if (points.Length < 2) return "error: need at least 2 points";

    BrushManager.Instance.BeginStroke(
        new Vector3(points[0].pos[0], points[0].pos[1], points[0].pos[2]),
        Quaternion.identity
    );

    for (int i = 1; i < points.Length; i++)
    {
        BrushManager.Instance.UpdateStroke(
            new Vector3(points[i].pos[0], points[i].pos[1], points[i].pos[2]),
            Quaternion.identity,
            points[i].pressure
        );
    }

    BrushManager.Instance.EndStroke();

    return $"ok: stroke_{BrushManager.Instance.AllStrokes.Count - 1}";
}
```

**Acceptance**:
- [ ] Stroke created from point array
- [ ] Supports variable point count
- [ ] Returns stroke ID

---

### Task 5.4: Add API Help Endpoint
**Priority**: P3 | **Estimate**: 1h | **Status**: Not Started

Create documentation endpoint.

**Subtasks**:
- [ ] `/help` - List all endpoints
- [ ] `/help/commands` - Detailed command info
- [ ] Generate from [ApiEndpoint] attributes
- [ ] Return HTML or JSON format

**Acceptance**:
- [ ] `/help` returns endpoint list
- [ ] Format includes descriptions
- [ ] Examples provided

---

## Sprint 6: Enhanced Brushes & Polish

### Task 6.1: Port Tier 2 Brush Materials
**Priority**: P2 | **Estimate**: 4h | **Status**: Not Started

Import remaining priority brushes.

**Subtasks**:
- [ ] Port Rainbow shader
- [ ] Port Electricity shader
- [ ] Port Bubbles/Stars particle systems
- [ ] Port Streamers ribbon
- [ ] Port Disco reflective shader
- [ ] Port Plasma animated shader

**Acceptance**:
- [ ] All 10 Tier 2 brushes functional
- [ ] Visual quality matches references
- [ ] No shader errors

---

### Task 6.2: Implement AR Touch Input
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

Create AR-specific brush input handling.

**Subtasks**:
- [ ] Create `BrushInput.cs` component
- [ ] Get touch position from Input
- [ ] Raycast to AR planes
- [ ] Fallback to fixed distance
- [ ] Map touch phase to stroke lifecycle

**Implementation**:
```csharp
public class BrushInput : MonoBehaviour
{
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private float _fallbackDistance = 2f;

    public Vector3 BrushPosition { get; private set; }
    public Quaternion BrushRotation { get; private set; }
    public bool IsDrawing { get; private set; }

    void Update()
    {
        if (Input.touchCount == 0)
        {
            if (IsDrawing)
            {
                BrushManager.Instance.EndStroke();
                IsDrawing = false;
            }
            return;
        }

        Touch touch = Input.GetTouch(0);
        Ray ray = Camera.main.ScreenPointToRay(touch.position);

        // Try AR plane hit
        var hits = new List<ARRaycastHit>();
        if (_raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            BrushPosition = hits[0].pose.position;
            BrushRotation = hits[0].pose.rotation;
        }
        else
        {
            // Fallback: fixed distance from camera
            BrushPosition = ray.GetPoint(_fallbackDistance);
            BrushRotation = Quaternion.LookRotation(ray.direction);
        }

        // Handle stroke lifecycle
        switch (touch.phase)
        {
            case TouchPhase.Began:
                BrushManager.Instance.BeginStroke(BrushPosition, BrushRotation);
                IsDrawing = true;
                break;
            case TouchPhase.Moved:
                BrushManager.Instance.UpdateStroke(BrushPosition, BrushRotation, 1f);
                break;
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                BrushManager.Instance.EndStroke();
                IsDrawing = false;
                break;
        }
    }
}
```

**Acceptance**:
- [ ] Touch begins stroke
- [ ] Move updates stroke
- [ ] Release ends stroke
- [ ] AR plane raycast works

---

### Task 6.3: Performance Optimization
**Priority**: P2 | **Estimate**: 2h | **Status**: Not Started

Optimize brush rendering for mobile.

**Subtasks**:
- [ ] Implement mesh batching for same-material strokes
- [ ] Add LOD for distant strokes
- [ ] Optimize shader variants
- [ ] Profile on iPhone 12
- [ ] Target 30+ FPS with 100 strokes

**Acceptance**:
- [ ] 30+ FPS with 100 strokes on iPhone 12
- [ ] Memory usage <200MB for stroke data
- [ ] No GC spikes during drawing

---

### Task 6.4: Integration Testing
**Priority**: P1 | **Estimate**: 2h | **Status**: Not Started

End-to-end testing of all features.

**Subtasks**:
- [ ] Test all Tier 1 brushes on device
- [ ] Test audio reactive with music
- [ ] Test all point symmetry families
- [ ] Test 5 wallpaper groups
- [ ] Test save/load round-trip
- [ ] Test API from external client

**Test Matrix**:
| Feature | Editor | Device |
|---------|--------|--------|
| Ink brush | [ ] | [ ] |
| Waveform audio | [ ] | [ ] |
| C4v symmetry | [ ] | [ ] |
| p6m wallpaper | [ ] | [ ] |
| Save/Load | [ ] | [ ] |
| API draw.stroke | [ ] | [ ] |

**Acceptance**:
- [ ] All features pass on device
- [ ] No crashes or errors
- [ ] Visual quality acceptable

---

## Test Results Log

### Brush Tests
| Date | Brush | Editor | Device | Notes |
|------|-------|--------|--------|-------|
| - | - | - | - | - |

### Audio Tests
| Date | Brush | Audio Source | Response | Notes |
|------|-------|--------------|----------|-------|
| - | - | - | - | - |

### Symmetry Tests
| Date | Family/Group | Order | Mirror Count | Pass |
|------|--------------|-------|--------------|------|
| - | - | - | - | - |

### API Tests
| Date | Endpoint | Request | Response | Pass |
|------|----------|---------|----------|------|
| - | - | - | - | - |

### Performance Tests
| Date | Device | Stroke Count | FPS | Memory |
|------|--------|--------------|-----|--------|
| - | - | - | - | - |

---

*Total Estimated Effort: ~62 hours across 7 sprints*
