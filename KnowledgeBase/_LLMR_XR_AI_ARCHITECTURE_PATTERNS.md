# LLMR & XR+AI Architecture Patterns

**Last Updated**: 2026-01-20
**Source**: MIT Media Lab LLMR, Google XR Blocks, CHI 2024/2025 research

---

## LLMR (Large Language Model for Mixed Reality)

MIT Media Lab project for real-time XR scene creation via natural language.

### Multi-GPT Orchestration

| Component | Role |
|-----------|------|
| **SceneAnalyzerGPT** | Scene understanding, object inventory |
| **SkillLibraryGPT** | Determines needed skills/tools for task |
| **BuilderGPT** | Code generation (C# Unity) |
| **InspectorGPT** | Validates code against rules, self-debugging |

**Key Result**: 4x error rate reduction vs standalone GPT-4.

### Architecture Pattern

```
User Prompt → SceneAnalyzer → SkillLibrary → Builder → Inspector → Unity Compiler → XR Scene
                    ↑                                        │
                    └────────────── Feedback Loop ───────────┘
```

---

## Meta Prompting

LLMs generating and optimizing their own prompts.

### Key Patterns

1. **Prompt Chaining**: Break complex tasks into subtask chain
2. **Self-Refinement**: Generate → Critique → Improve loop
3. **Skill Selection**: Match intent to available capabilities

### Self-Refinement Loop

```
Draft Response → Self-Critique → Feedback → Improved Response → (repeat until "correct + complete")
```

---

## Testing Without Hardware

### Mock Provider Pattern

- Priority -100 (lowest), only used when nothing else available
- Simulates all capabilities (`TrackingCap.All`)
- Generates realistic idle motion, occasional gestures

### Record & Replay

- Record sessions on device
- Replay in editor for testing
- Unit tests run against recordings

### Simulation-First Development (XR Blocks)

- Build/test in desktop browser
- Same code deploys to XR
- Web reproducibility for all demos

---

## Platform-Agnostic Design

### Zero External Dependencies Core

```csharp
namespace Tracking.Core
{
    // Pure C# - no Unity, no external libs
    public struct Vector3f { float X, Y, Z; }
    public struct JointData { int Id; Vector3f Position; float Confidence; }
}
```

### Capability-Based Routing

```csharp
[Flags]
public enum TrackingCap
{
    BodySegmentation24Part = 1 << 0,
    HandTracking21 = 1 << 1,
    // ... etc
}
```

### Layer Architecture

```
APPLICATION  - VFX, Avatars, Games (interfaces only)
SERVICE      - TrackingService, VoiceService (orchestration)
ABSTRACTION  - ITrackingProvider, IVoiceProvider (contracts)
ADAPTER      - ARKit, Meta, Sentis, WebXR implementations
PLATFORM     - Native SDKs, Hardware, OS APIs
```

Dependencies flow DOWN only.

---

## LLM Integration Patterns

### IContextProvider (like SceneAnalyzerGPT)

```csharp
public interface IContextProvider
{
    string GetContextSummary();
    Dictionary<string, object> GetContextDetails();
}
```

### ISkillRouter (like SkillLibraryGPT)

```csharp
public interface ISkillRouter
{
    ISkill SelectBestSkill(string intent, TrackingCap available);
}
```

### ISelfValidator (like InspectorGPT)

```csharp
public interface ISelfValidator
{
    ValidationResult Validate(object output);
    object FixIfPossible(object output, ValidationResult errors);
}
```

---

## XR+LLM Interaction Paradigms (CHI 2025)

1. **Understanding** - Users and contexts
2. **Responding** - To user requests
3. **Changing** - Contexts
4. **Prompting** - Users to act

### Five Pillars of Awareness

1. Situational
2. Self
3. Spatial
4. Social
5. **Ethical** (newly proposed)

---

## Debugging & Extension

### Hot-Swap Providers (Runtime)

- F1: Switch to Mock
- F2: Switch to Recording Playback
- F3: Return to Auto-Select

### Plugin Loading

Load providers from separate DLLs at runtime.

### Provider Composition

Wrap existing provider to add new capabilities without modification.

---

## Key Takeaways

| Principle | Why |
|-----------|-----|
| Multi-GPT orchestration | Specialized agents > monolithic LLM |
| Self-refinement loops | Better accuracy, catches errors |
| Mock-first testing | Zero hardware dependency |
| Capability routing | Runtime adaptation |
| Layer isolation | Platform changes don't cascade |

---

## References

- [LLMR: MIT Media Lab](https://www.media.mit.edu/projects/large-language-model-for-mixed-reality/overview/)
- [LLMR Paper (CHI 2024)](https://dl.acm.org/doi/10.1145/3613904.3642579)
- [LLMR GitHub](https://github.com/microsoft/llmr)
- [LLM Integration in XR (CHI 2025)](https://dl.acm.org/doi/10.1145/3706598.3714224)
- [XR Blocks (Google)](https://research.google/blog/xr-blocks-accelerating-ai-xr-innovation/)
- [Meta Prompting Guide](https://www.promptingguide.ai/techniques/meta-prompting)
- [Prompt Chaining Guide](https://www.promptingguide.ai/techniques/prompt_chaining)
- [iv4XR: AI Test Agents](https://www.openaccessgovernment.org/article/artificial-intelligence-test-agents-for-automated-testing-of-extended-reality-xr-ai/149203/)
- [SpatialLM](https://manycore-research.github.io/SpatialLM/)

---

*See also: `specs/008-cross-platform-ml-segmentation/MODULAR_TRACKING_ARCHITECTURE.md` for full implementation details.*
