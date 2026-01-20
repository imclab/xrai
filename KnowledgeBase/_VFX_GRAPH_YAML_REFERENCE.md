# VFX Graph YAML Reference

Unity VFX Graph assets are stored as YAML files. This document provides a comprehensive reference for understanding and programmatically manipulating VFX Graph assets.

**Last Updated**: 2026-01-20
**Unity Version**: 6000.2.14f1 (VFX Graph 17.2.0)

---

## File Types

| Extension | Purpose | Category Path |
|-----------|---------|---------------|
| `.vfx` | Complete VFX Graph asset | - |
| `.vfxoperator` | Subgraph operator (pure computation) | `Subgraph Operator` |
| `.vfxblock` | Subgraph block (particle manipulation) | `Subgraph Block` |

---

## YAML Structure Overview

All VFX files share the same basic structure:

```yaml
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!2058629511 &8926484042661614527
VisualEffectResource:
  m_Name: MyVFX
  m_Graph: {fileID: 114350483966674976}
  m_Infos:
    m_RendererSettings: ...
    m_CullingFlags: 3
    m_UpdateMode: 0
    m_InitialEventName: OnPlay
    m_InstancingMode: 0
    m_InstancingCapacity: 64
```

### Key File IDs

| Class ID | Type | Purpose |
|----------|------|---------|
| `2058629511` | VisualEffectResource | Main asset container |
| `114` | MonoBehaviour | All graph elements (contexts, blocks, slots, operators) |

---

## Core Components

### 1. VisualEffectResource (Root)

The main asset container. References the graph and runtime settings.

```yaml
VisualEffectResource:
  m_Name: MyVFX
  m_Graph: {fileID: 114350483966674976}  # Reference to VFXGraph MonoBehaviour
  m_Infos:
    m_RendererSettings:
      motionVectorGenerationMode: 0  # 0=Camera, 1=Object, 2=ForceNoMotion
      shadowCastingMode: 0           # 0=Off, 1=On, 2=TwoSided, 3=ShadowsOnly
      rayTracingMode: 0              # 0=Off, 1=Static, 2=Dynamic
      receiveShadows: 0              # 0/1
      reflectionProbeUsage: 0        # 0=Off, 1=BlendProbes
      lightProbeUsage: 0             # 0=Off, 1=BlendProbes
    m_CullingFlags: 3                # 0=AlwaysSimulate, 1=CullSimulate, 2=CullBoundsUpdate, 3=All
    m_UpdateMode: 0                  # 0=DeltaTime, 1=FixedDeltaTime, 2=IgnoreTimeScale
    m_PreWarmDeltaTime: 0.05
    m_PreWarmStepCount: 0
    m_InitialEventName: OnPlay
    m_InstancingMode: 0              # 0=Disabled, 1=Automatic, -1=Default
    m_InstancingCapacity: 64
```

### 2. VFXGraph (Main Graph Container)

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 7d4c867f6b72b714dbb5fd1780afe208, type: 3}
  m_Name: MyVFX
  m_Children:           # List of all top-level elements (contexts, parameters)
  - {fileID: ...}
  m_UIInfos: {fileID: ...}
  m_CustomAttributes: []
  m_ParameterInfo: []   # Exposed property definitions (see below)
  m_GraphVersion: 18    # Current graph format version
  m_ResourceVersion: 1
  m_SubgraphDependencies: []
  m_CategoryPath:       # For subgraphs: "Subgraph Operator" or "Subgraph Block"
```

---

## Context Types

VFX Graphs use a pipeline of contexts. Each context type has a specific GUID in `m_Script`.

### Context Script GUIDs

| Context | GUID | Purpose |
|---------|------|---------|
| VFXBasicSpawner | `73a13919d81fb7444849bae8b5c812a2` | Spawn system (rate/burst) |
| VFXBasicInitialize | `9dfea48843f53fc438eabc12a3a30abc` | Initialize particles |
| VFXBasicUpdate | `2dc095764ededfa4bb32fa602511ea4b` | Update particles per frame |
| VFXQuadOutput | `a0b9e6b9139e58d4c957ec54595da7d3` | Render as billboards/quads |
| VFXMeshOutput | `...` | Render as meshes |
| VFXDecalOutput | `...` | Render as decals |
| GPUEvent | `...` | GPU-triggered spawn |

### Spawn Context

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 73a13919d81fb7444849bae8b5c812a2, type: 3}
  m_Name: VFXBasicSpawner
  m_Label: Spawn System
  m_Data: {fileID: ...}        # Reference to VFXDataSpawner
  m_InputFlowSlot:
  - link: []                    # Start trigger
  - link: []                    # Stop trigger
  m_OutputFlowSlot:
  - link:
    - context: {fileID: ...}    # Link to Initialize
      slotIndex: 0
  loopDuration: 0
  loopCount: 0
  delayBeforeLoop: 0
  delayAfterLoop: 0
```

### Initialize Context

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 9dfea48843f53fc438eabc12a3a30abc, type: 3}
  m_Name: VFXBasicInitialize
  m_Label: Initialize Particles
  m_Data: {fileID: ...}        # Reference to VFXDataParticle
  m_InputSlots:
  - {fileID: ...}              # bounds (AABox)
  - {fileID: ...}              # boundsPadding (Vector3)
  m_InputFlowSlot:
  - link:
    - context: {fileID: ...}   # Link from Spawn
      slotIndex: 0
  m_OutputFlowSlot:
  - link:
    - context: {fileID: ...}   # Link to Update
      slotIndex: 0
```

### Update Context

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 2dc095764ededfa4bb32fa602511ea4b, type: 3}
  m_Name: VFXBasicUpdate
  m_Label: Update Particles
  integration: 0               # 0=None, 1=Euler, 2=EulerIntegration
  angularIntegration: 0        # 0=None, 1=Euler
  ageParticles: 1              # 0/1 - increment age
  reapParticles: 1             # 0/1 - kill particles when age > lifetime
  skipZeroDeltaUpdate: 0       # 0/1
```

### Output Context (Quad/Billboard)

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: a0b9e6b9139e58d4c957ec54595da7d3, type: 3}
  m_Name: VFXQuadOutput
  m_Label: Render Quad
  blendMode: 1                 # 0=Opaque, 1=Additive, 2=Alpha, 3=AlphaPremultiplied
  cullMode: 0                  # 0=Default, 1=Front, 2=Back
  zWriteMode: 0                # 0=Default, 1=On, 2=Off
  zTestMode: 0                 # 0=Default, 1=Less, 2=LEqual, etc.
  useAlphaClipping: 0
  generateMotionVector: 0
  sortingPriority: 0
  colorMapping: 0              # 0=Default, 1=GradientMapped
  uvMode: 0                    # 0=Default, 1=Simple, 2=ScaleAndBias
  flipbookLayout: 0            # 0=Texture2D, 1=Texture2DArray
  flipbookBlendFrames: 0       # 0/1
  useSoftParticle: 0           # 0/1
  sort: 0                      # 0=Off, 1=Auto
  sortMode: 0                  # 0=ByDistance, 1=YoungestFirst, 2=OldestFirst
  indirectDraw: 0              # 0/1 - GPU culling
  computeCulling: 0            # 0/1
  frustumCulling: 0            # 0/1
  castShadows: 0               # 0/1
  primitiveType: 1             # 0=Triangle, 1=Quad, 2=Octagon
  shaderGraph: {fileID: 0}     # Optional Shader Graph reference
```

---

## Data Containers

### VFXDataParticle

Shared particle data container referenced by Initialize, Update, and Output contexts.

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: d78581a96eae8bf4398c282eb0b098bd, type: 3}
  m_Name: VFXDataParticle
  title: System Name
  m_Owners:                    # Contexts sharing this data
  - {fileID: ...}              # Initialize
  - {fileID: ...}              # Update
  - {fileID: ...}              # Output
  dataType: 0                  # 0=Particle, 1=ParticleStrip
  capacity: 64                 # Max particle count
  stripCapacity: 16            # Strip mode only
  particlePerStripCount: 16    # Strip mode only
  needsComputeBounds: 0
  boundsMode: 0                # 0=Manual, 1=Recorded, 2=Automatic
  m_Space: 0                   # 0=Local, 1=World
```

### VFXDataSpawner

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: f68759077adc0b143b6e1c101e82065e, type: 3}
  m_Name: VFXDataSpawner
  m_Owners:
  - {fileID: ...}              # Spawn context
```

---

## Slots and Properties

### Slot Types (by GUID)

| GUID | Type | Purpose |
|------|------|---------|
| `f780aa281814f9842a7c076d436932e7` | VFXSlotFloat | Single float |
| `ac39bd03fca81b849929b9c966f1836a` | VFXSlotFloat3 | Vector3 |
| `70a331b1d86cc8d4aa106ccbe0da5852` | VFXSlotTexture2D | Texture2D |
| `d5ff4abdfc4ddb54992e15a0636f5d0e` | VFXSlotGraphicsBuffer | GraphicsBuffer |
| `1b605c022ee79394a8a776c0869b3f9a` | VFXSlot (generic) | Container slot |

### Slot Structure

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: f780aa281814f9842a7c076d436932e7, type: 3}
  m_Name: VFXSlotFloat
  m_MasterSlot: {fileID: ...}    # Root slot (self if this is root)
  m_MasterData:
    m_Owner: {fileID: ...}        # Owning context/block
    m_Value:
      m_Type:
        m_SerializableType: System.Single, mscorlib, ...
      m_SerializableObject: 1.0   # Default value
    m_Space: -1                   # -1=None, 0=Local, 1=World
  m_Property:
    name: myProperty
    m_serializedType:
      m_SerializableType: System.Single, mscorlib, ...
  m_Direction: 0                  # 0=Input, 1=Output
  m_LinkedSlots: []               # Connections to other slots
```

---

## Exposed Properties (m_ParameterInfo)

Exposed properties appear in the VFX Graph inspector and can be bound at runtime.

```yaml
m_ParameterInfo:
- name: KeypointBuffer           # Inspector display name
  path:                          # Empty for GraphicsBuffer type
  tooltip: Pose keypoint buffer
  space: -1                      # -1=None, 0=Local, 1=World
  spaceable: 0                   # Can user toggle space?
  sheetType:                     # Property sheet type (see below)
  realType: GraphicsBuffer       # C# type name
  defaultValue:
    m_Type:
      m_SerializableType: UnityEngine.GraphicsBuffer, ...
    m_SerializableObject:        # JSON default value
  min: -Infinity
  max: Infinity
  enumValues: []
  descendantCount: 0
```

### Common sheetType Values

| sheetType | C# Type | Example |
|-----------|---------|---------|
| `m_Float` | `Single` | 0.5 |
| `m_Vector2f` | `Vector2` | `{"x":0.0,"y":0.0}` |
| `m_Vector3f` | `Vector3` | `{"x":0.0,"y":0.0,"z":0.0}` |
| `m_Vector4f` | `Vector4` | `{"x":0.0,"y":0.0,"z":0.0,"w":0.0}` |
| `m_Matrix4x4f` | `Matrix4x4` | Identity matrix JSON |
| `m_NamedObject` | `Texture2D` | Asset reference |
| (empty) | `GraphicsBuffer` | Buffer reference |

---

## Parameter Node Structure

Parameters (exposed properties) are defined as nodes in the graph:

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 330e0fca1717dde4aaa144f48232aa64, type: 3}
  m_Name: Parameter
  m_OutputSlots:
  - {fileID: ...}                 # Output slot
  m_ExposedName: PositionMap      # Exposed property name
  m_Exposed: 1                    # 1=visible in inspector
  m_Order: 0                      # Inspector order
  m_Category:                     # Grouping category
  m_Min:                          # Range constraint
  m_Max:                          # Range constraint
  m_IsOutput: 0                   # 0=input param, 1=output param
  m_Tooltip: World positions      # Inspector tooltip
  m_Nodes:                        # Node instances in graph
  - m_Id: 1
    linkedSlots:
    - outputSlot: {fileID: ...}
      inputSlot: {fileID: ...}
    position: {x: 100, y: 200}
    expanded: 1
```

---

## UI Information (VFXUI)

Graph visual layout and grouping:

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: d01270efd3285ea4a9d6c555cb0a8027, type: 3}
  m_Name: VFXUI
  groupInfos:
  - title: Keypoint Retrieval
    position:
      x: -363
      y: 638
      width: 916
      height: 498
    contents:
    - model: {fileID: ...}        # Node in group
      id: 0
      isStickyNote: 0
  stickyNoteInfos: []
  categories: []
  uiBounds:                       # Overall graph bounds
    x: 0
    y: 0
    width: 1999
    height: 1918
```

---

## Spawn Blocks

### Constant Spawn Rate

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: ...}
  m_Name: ConstantSpawnRate
  m_InputSlots:
  - {fileID: ...}                 # Rate slot
```

### Single Burst

```yaml
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: 5e382412bb691334bb79457a6c127924, type: 3}
  m_Name: VFXSpawnerBurst
  m_InputSlots:
  - {fileID: ...}                 # Count
  - {fileID: ...}                 # Delay
  repeat: 0                       # 0=once, 1=repeat
  spawnMode: 0                    # 0=Total, 1=Rate
  delayMode: 0                    # 0=Constant, 1=Random
```

---

## Subgraph Operators (.vfxoperator)

Reusable computation nodes that take inputs and produce outputs.

```yaml
m_CategoryPath: Subgraph Operator
m_ParameterInfo:
- name: PositionMap
  path: PositionMap
  tooltip: World position texture
  realType: Texture2D
- name: UV
  path: UV
  realType: Vector2
  defaultValue:
    m_SerializableObject: '{"x":0.5,"y":0.5}'
```

---

## Subgraph Blocks (.vfxblock)

Reusable particle manipulation blocks.

```yaml
m_CategoryPath: Subgraph Block
m_ParameterInfo:
- name: Metavido Data            # Input group
- name: ColorMap
  path: ColorMap
  realType: Texture2D
- name: UV
  path: UV
  realType: Vector2
```

---

## Common Patterns

### Adding a New Exposed Property (C#)

```csharp
// Use VisualEffect.SetTexture/SetVector/etc. at runtime
// The property must exist in m_ParameterInfo

var vfx = GetComponent<VisualEffect>();
vfx.SetTexture("PositionMap", positionTexture);
vfx.SetVector4("RayParams", new Vector4(0, 0, tanFovX, tanFovY));
vfx.SetGraphicsBuffer("KeypointBuffer", keypointBuffer);
```

### Property Name Resolution

VFX Graph uses `ExposedProperty<T>` internally:
```csharp
static readonly ExposedProperty<Texture> PositionMap = "PositionMap";
vfx.SetTexture(PositionMap, texture);  // Faster than string lookup
```

### Finding Properties Programmatically

```csharp
bool HasExposedProperty(VisualEffect vfx, string name)
{
    return vfx.HasTexture(name) || vfx.HasVector4(name) ||
           vfx.HasFloat(name) || vfx.HasGraphicsBuffer(name);
}
```

---

## MetavidoVFX Property Standards

Based on project conventions:

| Property | Type | Source | Description |
|----------|------|--------|-------------|
| `DepthMap` | Texture2D | ARDepthSource | Raw AR depth |
| `StencilMap` | Texture2D | ARDepthSource | Human segmentation |
| `PositionMap` | Texture2D | ARDepthSource | GPU-computed world positions |
| `VelocityMap` | Texture2D | ARDepthSource | GPU-computed velocity |
| `ColorMap` | Texture2D | AR Camera | Camera RGB |
| `RayParams` | Vector4 | ARDepthSource | (0, 0, tan(fov/2)*aspect, tan(fov/2)) |
| `InverseView` | Matrix4x4 | ARDepthSource | Camera inverse view |
| `DepthRange` | Vector2 | Config | Near/far (default 0.1-10m) |
| `KeypointBuffer` | GraphicsBuffer | BodyPartSegmenter | 17 pose landmarks |
| `Threshold` | Float | Config | Confidence threshold (0-1) |
| `MaskTexture` | Texture2D | Config | Masking texture |

---

## Runtime API (Official Documentation)

> Source: [Visual Effect Component API](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@7.1/manual/ComponentAPI.html)

### Property Access Methods

Properties can be accessed via:
- **String name** - Easy but less optimized
- **Integer ID** - Generated via `Shader.PropertyToID(string name)`
- **ExposedProperty Helper Class** - Caches int value for best performance

### Supported Property Types

| Type | Has | Get | Set |
|------|-----|-----|-----|
| Int | `HasInt()` | `GetInt()` | `SetInt()` |
| UInt | `HasUInt()` | `GetUInt()` | `SetUInt()` |
| Bool | `HasBool()` | `GetBool()` | `SetBool()` |
| Float | `HasFloat()` | `GetFloat()` | `SetFloat()` |
| Vector2 | `HasVector2()` | `GetVector2()` | `SetVector2()` |
| Vector3 | `HasVector3()` | `GetVector3()` | `SetVector3()` |
| Vector4 | `HasVector4()` | `GetVector4()` | `SetVector4()` |
| Gradient | `HasGradient()` | `GetGradient()` | `SetGradient()` |
| AnimationCurve | `HasAnimationCurve()` | `GetAnimationCurve()` | `SetAnimationCurve()` |
| Mesh | `HasMesh()` | `GetMesh()` | `SetMesh()` |
| Texture | `HasTexture()` | `GetTexture()` | `SetTexture()` |
| Matrix4x4 | `HasMatrix4x4()` | `GetMatrix4x4()` | `SetMatrix4x4()` |
| GraphicsBuffer | `HasGraphicsBuffer()` | - | `SetGraphicsBuffer()` |

### Event Handling

```csharp
// Send events to VFX
vfx.SendEvent("OnPlay");
vfx.SendEvent(eventNameOrId, eventAttribute);

// Create event attributes
var attr = vfx.CreateVFXEventAttribute();
attr.SetVector3("position", Vector3.zero);
vfx.SendEvent("Spawn", attr);
```

### Reset Property Overrides

```csharp
// Reset a property to its default value
vfx.ResetOverride("PropertyName");
```

---

## Effect Playback Control

> Source: [VisualEffect API](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html)

### Playback Methods

| Method | Description |
|--------|-------------|
| `Play()` | Sends the default "OnPlay" event to the effect |
| `Play(VFXEventAttribute)` | Sends "OnPlay" with custom attributes |
| `Stop()` | Sends the default "OnStop" event |
| `Stop(VFXEventAttribute)` | Sends "OnStop" with custom attributes |
| `Reinit()` | Resets simulation and re-sends "OnPlay" |
| `AdvanceOneFrame()` | Advances simulation by one frame (useful for prewarming) |

### Playback Properties

```csharp
// Check if effect is playing
bool isPlaying = vfx.HasAnySystemAwake();

// Pause/resume
vfx.pause = true;   // Pauses simulation
vfx.pause = false;  // Resumes

// Playback rate
vfx.playRate = 0.5f;  // Half speed
vfx.playRate = 2.0f;  // Double speed

// Random seed control
vfx.resetSeedOnPlay = true;  // New seed each Play()
vfx.startSeed = 42;          // Fixed seed for reproducibility
```

### Prewarming Effects

```csharp
// Advance simulation without rendering (useful for prewarming)
IEnumerator PrewarmEffect(VisualEffect vfx, float seconds, float deltaTime = 0.02f)
{
    int steps = Mathf.CeilToInt(seconds / deltaTime);
    for (int i = 0; i < steps; i++)
    {
        vfx.Simulate(deltaTime);
    }
    yield return null;
}
```

---

## Particle Attributes

> Source: [Attributes Documentation](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/Attributes.html)

### Standard Attributes

| Attribute | Type | Description | Default |
|-----------|------|-------------|---------|
| `position` | Vector3 | Particle world/local position | (0,0,0) |
| `velocity` | Vector3 | Movement per second | (0,0,0) |
| `direction` | Vector3 | Normalized direction | (0,0,1) |
| `lifetime` | float | Total lifetime in seconds | 1.0 |
| `age` | float | Current age in seconds | 0.0 |
| `color` | Vector4/Color | RGBA color | (1,1,1,1) |
| `alpha` | float | Alpha transparency | 1.0 |
| `size` | float | Uniform scale | 0.1 |
| `scale` | Vector3 | Non-uniform scale | (1,1,1) |
| `angle` | Vector3 | Euler rotation (degrees) | (0,0,0) |
| `angularVelocity` | Vector3 | Rotation per second | (0,0,0) |
| `mass` | float | Particle mass | 1.0 |
| `oldPosition` | Vector3 | Previous frame position | position |
| `targetPosition` | Vector3 | Attraction target | (0,0,0) |
| `seed` | uint | Per-particle random seed | random |
| `particleId` | uint | Unique ID per particle | auto |

### Attribute Storage Rules

Attributes can be **stored** (persisted per particle) or **computed** (calculated each frame):

```
Storage Decision:
├── Written in Initialize → STORED (if read later)
├── Written in Update → STORED (if read later in Update/Output)
├── Read in Output only → Can be CURRENT (not stored)
└── Not used → OPTIMIZED OUT
```

**Current vs Stored:**
- **Current**: Computed fresh each frame, no memory cost
- **Stored**: Persisted in particle buffer, costs memory per particle

### Custom Attributes

Define in Initialize or Update context:

```yaml
# In YAML (m_CustomAttributes)
m_CustomAttributes:
- customAttribute: myCustomFloat
  type: 0                    # 0=Float, 1=Float2, 2=Float3, 3=Float4
```

Access in blocks/operators:
```hlsl
// Read/write custom attribute
float myValue = Get Attribute: myCustomFloat
Set Attribute: myCustomFloat = newValue
```

---

## VFXEventAttribute (Detailed)

> Source: [Component API](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@7.1/manual/ComponentAPI.html)

### Creating Event Attributes

```csharp
// Create reusable event attribute
VFXEventAttribute eventAttr = vfx.CreateVFXEventAttribute();

// Pool these for performance - don't create every frame
private VFXEventAttribute _cachedAttr;
void Start() => _cachedAttr = vfx.CreateVFXEventAttribute();
```

### Setting Attribute Values

```csharp
// Supported types for VFXEventAttribute
eventAttr.SetFloat("rate", 100f);
eventAttr.SetInt("count", 50);
eventAttr.SetUint("seed", 12345u);
eventAttr.SetBool("enabled", true);
eventAttr.SetVector2("uv", new Vector2(0.5f, 0.5f));
eventAttr.SetVector3("position", transform.position);
eventAttr.SetVector4("color", new Vector4(1, 0, 0, 1));
eventAttr.SetMatrix4x4("transform", transform.localToWorldMatrix);
```

### Getting Attribute Values

```csharp
float rate = eventAttr.GetFloat("rate");
Vector3 pos = eventAttr.GetVector3("position");
bool enabled = eventAttr.GetBool("enabled");
```

### Checking Attribute Existence

```csharp
if (eventAttr.HasFloat("rate"))
{
    float rate = eventAttr.GetFloat("rate");
}
```

### Copying Between Attributes

```csharp
// Copy all values from one attribute to another
eventAttr.CopyValuesFrom(sourceAttr);
```

### Sending Events with Attributes

```csharp
// Standard spawn with position override
var attr = vfx.CreateVFXEventAttribute();
attr.SetVector3("position", hitPoint);
attr.SetVector3("velocity", hitNormal * 5f);
attr.SetVector4("color", Color.red);
vfx.SendEvent("OnSpawn", attr);

// GPU Event spawn (from Output context)
// Attributes are inherited from dying particle
vfx.SendEvent("OnDeath", attr);
```

### Event Attribute Flow

```
SendEvent("MyEvent", attr)
    ↓
Spawn Context receives event
    ↓
Initialize Context: Get Attribute: eventAttr.position
    ↓
Particle spawns with overridden position
```

---

## Output Events (GPU → CPU)

> Source: [Output Event Handler](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/OutputEvent.html)

Output Events allow particles to send data back to C# code.

### Setting Up Output Events

```csharp
// Subscribe to output events
void OnEnable()
{
    var vfx = GetComponent<VisualEffect>();
    vfx.outputEventReceived += OnOutputEvent;
}

void OnDisable()
{
    var vfx = GetComponent<VisualEffect>();
    vfx.outputEventReceived -= OnOutputEvent;
}

void OnOutputEvent(VFXOutputEventArgs args)
{
    // args.nameId - Event name ID
    // args.eventAttribute - Contains particle data

    Vector3 position = args.eventAttribute.GetVector3("position");
    Vector3 velocity = args.eventAttribute.GetVector3("velocity");
    Vector4 color = args.eventAttribute.GetVector4("color");

    // Spawn audio, trigger effects, etc.
    AudioSource.PlayClipAtPoint(clip, position);
}
```

### Batched Output Events

```csharp
// For high-frequency events, use batched handler
void OnOutputEvent(VFXOutputEventArgs args)
{
    // Check event count for this frame
    int eventCount = args.eventAttribute.GetInt("eventCount");

    // Process in batch
    for (int i = 0; i < eventCount; i++)
    {
        // Event data is in arrays
    }
}
```

---

## Simulation Control

### Manual Simulation

```csharp
// Disable auto-update
vfx.culled = true;  // Prevent automatic updates

// Manually simulate
vfx.Simulate(Time.deltaTime, 1);  // (deltaTime, stepCount)
```

### Culling Control

```csharp
// Culling modes (set in VisualEffectResource YAML)
// m_CullingFlags: 0 = Always simulate
// m_CullingFlags: 1 = Cull simulate when invisible
// m_CullingFlags: 2 = Cull bounds update
// m_CullingFlags: 3 = Cull all

// Runtime override
vfx.culled = true;   // Force culled state
vfx.culled = false;  // Resume normal culling
```

### Bounds Control

```csharp
// Get current bounds
Bounds bounds = vfx.GetOutputMeshBounds();

// Manual bounds (when boundsMode = Manual)
vfx.SetVector3("bounds_center", center);
vfx.SetVector3("bounds_size", size);
```

---

## Instancing (Unity 6+)

VFX Graph supports GPU instancing for multiple instances of the same effect.

### YAML Configuration

```yaml
m_InstancingMode: 1       # 0=Disabled, 1=Automatic, -1=Default
m_InstancingCapacity: 64  # Max instances
```

### Runtime Instance Control

```csharp
// Each VisualEffect component is an instance
// Shared VFX asset = automatic batching
GameObject[] spawned = new GameObject[100];
for (int i = 0; i < 100; i++)
{
    spawned[i] = Instantiate(vfxPrefab, positions[i], Quaternion.identity);
    // All share same VFX asset = GPU instanced rendering
}
```

---

## Subgraph Types (Official Documentation)

> Source: [Subgraphs](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.4/manual/Subgraph.html)

### Three Subgraph Types

| Type | File | Purpose |
|------|------|---------|
| **System Subgraph** | `.vfx` | One or many Systems contained in one Graph (appears as Context) |
| **Block Subgraph** | `.vfxblock` | Operators and blocks that function as reusable block units |
| **Operator Subgraph** | `.vfxoperator` | Only contains Operators, used as Operators in other graphs |

### Creating Subgraphs

**Block Subgraph:**
1. `Assets > Create > Visual Effects > Visual Effect Subgraph Block`
2. Or: Select blocks/operators → Right-click → "Convert to Subgraph Block"

**Operator Subgraph:**
1. `Assets > Create > Visual Effects > Visual Effect Subgraph Operator`
2. Or: Select operators → Right-click → "Convert to Subgraph Operator"

### Input/Output Properties (Blackboard)

For Operator Subgraphs:
- **Inputs**: Add properties and enable "Exposed" flag
- **Outputs**: Add properties and move to Output Category

### Category Organization

Set category in Blackboard subtitle:
- Use `/` for hierarchies: `MySubgraphs/Math`
- Use `#` with numbers for sorting separators

---

## Custom HLSL Nodes (Official Documentation)

> Source: [Custom HLSL Nodes](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/CustomHLSL-Common.html)

### Node Types

| Type | Flow | Use Case |
|------|------|----------|
| **Operator** | Horizontal | Pure computation |
| **Block** | Vertical (in contexts) | Particle manipulation |

### Function Requirements

- Return a supported type
- All parameters must be supported types
- Multiple functions require unique names
- Helper functions need `/// Hidden` comment (HLSL files only)

### Supported HLSL Types

**Basic:** `bool`, `uint`, `int`, `float`, `float2`, `float3`, `float4`, `float4x4`, `VFXGradient`, `VFXCurve`

**Textures:** `VFXSampler2D`, `VFXSampler3D`, `VFXSampler2DArray`, `VFXSamplerCube`, `Texture1D/2D/3D`, `RWTexture*`

**Buffers:** `StructuredBuffer`, `ByteAddressBuffer`, `Buffer`, `AppendStructuredBuffer`, `ConsumeStructuredBuffer`, `RWStructuredBuffer`

### Sampling Utilities

```hlsl
// Texture sampling
float4 color = SampleTexture(VFXSampler2D sampler, float2 uv, float mipLevel);

// Gradient sampling
float4 color = SampleGradient(VFXGradient gradient, float t);

// Curve sampling
float value = SampleCurve(VFXCurve curve, float t);

// Buffer access
float4 data = myStructuredBuffer[index];
uint rawData = myByteAddressBuffer.Load(byteOffset);
```

### Parameter Documentation

```hlsl
/// parameterName: This is the tooltip text
float4 MyFunction(float3 position)
{
    return float4(position, 1);
}
```

---

## Unity YAML Format (Official Documentation)

> Source: [UnityYAML](https://docs.unity3d.com/Manual/UnityYAML.html), [Format Description](https://docs.unity3d.com/Manual/FormatDescription.html)

### Key Format Details

Unity uses a **custom-optimized YAML library** (UnityYAML) that does not support the full YAML specification.

**Supported:**
- Mappings (flow and block styles)
- Scalars (double/single quoted, plain, multi-line with proper indentation)
- UTF-8 characters (in double-quoted scalars only)
- Sequences (block styles, sequences containing mappings)

**Not Supported:**
- Chomping indicators (`+` and `|`)
- Comments
- Complex mapping keys
- Multiple documents per object
- Raw block sequences
- Tags (beyond Unity's internal tags)

### Object Structure

```yaml
--- !u!1 &6                    # Document marker + class ID + object ID
GameObject:
  m_ObjectHideFlags: 0
  m_Name: Cube
```

- **Class ID** (`!u!1`): Indicates object class (1=GameObject, 4=Transform, 114=MonoBehaviour)
- **Object ID** (`&6`): Unique identifier within the file
- **Property prefix** (`m_`): Serializable properties follow naming convention

### File References

```yaml
# Same file reference
m_Transform: {fileID: 6}

# Cross-file reference (requires GUID from .meta file)
m_Script: {fileID: 11500000, guid: 73a13919d81fb7444849bae8b5c812a2, type: 3}
```

### Floating Point Representation

```yaml
# Multiple valid formats
m_Value: 1              # Integer
m_Value: 1.000          # Decimal
m_Value: 0.1e1          # Scientific
m_Value: 0x3F800000(1)  # IEEE 754 hex (only hex is parsed)
```

---

## Version History

| Graph Version | Unity Version | Changes |
|---------------|---------------|---------|
| 18 | Unity 6+ | Current format |
| 17 | 2022.3+ | Instancing support |
| 16 | 2021.3+ | GPU events |

---

## References (Official Documentation)

**Unity Official:**
- [Visual Effect Graph Manual](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/index.html)
- [VisualEffect Scripting API](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.html)
- [VFXEventAttribute API](https://docs.unity3d.com/ScriptReference/VFX.VFXEventAttribute.html)
- [SetGraphicsBuffer API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/VFX.VisualEffect.SetGraphicsBuffer.html)
- [SetTexture API](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SetTexture.html)
- [Component API](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@7.1/manual/ComponentAPI.html)
- [Attributes Reference](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/Attributes.html)
- [Output Events](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.2/manual/OutputEvent.html)
- [Subgraphs](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.4/manual/Subgraph.html)
- [Custom HLSL Nodes](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.3/manual/CustomHLSL-Common.html)
- [UnityYAML](https://docs.unity3d.com/Manual/UnityYAML.html)
- [Format Description](https://docs.unity3d.com/Manual/FormatDescription.html)
- [Understanding Unity's Serialization Language](https://unity.com/blog/engine-platform/understanding-unitys-serialization-language-yaml)

**Community Resources:**
- [VfxGraphAssets (keijiro)](https://github.com/keijiro/VfxGraphAssets) - Custom subgraphs library
- [VisualEffectGraph-Samples](https://github.com/Unity-Technologies/VisualEffectGraph-Samples) - Official samples

**Project-Specific:**
- `MetavidoVFX-main/CLAUDE.md` - Project conventions
- `KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md` - AR + VFX patterns

---

## Related Files

```
MetavidoVFX-main/
├── Assets/VFX/                      # VFX assets organized by category
│   ├── People/                      # Human-following effects
│   ├── Environment/                 # Spatial effects
│   ├── NNCam2/                      # Keypoint-driven VFX
│   ├── Rcam2-4/                     # Depth-camera effects
│   └── SdfVfx/                      # SDF-based effects
├── Packages/jp.keijiro.metavido.vfxgraph/VFX/  # Metavido subgraphs
│   ├── Metavido Sample UV.vfxblock
│   ├── Metavido Sample Random.vfxblock
│   └── Metavido Filter.vfxblock
└── Assets/Scripts/Bridges/          # Runtime binding
    ├── ARDepthSource.cs             # Compute source (singleton)
    └── VFXARBinder.cs               # Per-VFX binding
```
