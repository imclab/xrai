# PROJECT_CLAUDE_TEMPLATE.md

**Copy this template to your Unity project's CLAUDE.md to access Unity-XR-AI knowledge base**

---

# [Your Project Name]

**Brief project description**

**Platform**: iOS/Android/Quest/WebGL/Vision Pro (choose applicable)
**Unity Version**: 6000.1.2f1 (or your version)
**AR Foundation**: 6.1.0 (if applicable)

---

## 🎯 Project Overview

[Brief description of what your project does]

**Key Features**:
- Feature 1
- Feature 2
- Feature 3

---

## 📚 Unity-XR-AI Knowledgebase

**BEFORE implementing ANY Unity XR/AR feature**: Search these knowledge bases first.

**Knowledge Base Location**: `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/`

### Quick References

**520+ GitHub Repos**: `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md`
- Search by category: ARFoundation, VFX Graph, DOTS, Networking, Cross-Platform, ML/AI
- 520+ verified repos with Unity versions and key features
- Quick search by use case (audio reactive, human tracking, multiplayer, etc.)

**AR/VFX Implementation Patterns**: `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`
- Human depth → VFX particles (49K points @ 60fps)
- 91-joint skeleton tracking (NOT 17!)
- Face tracking → VFX (4096 particles)
- Audio reactive patterns (FFT → VFX properties)
- DOTS million-particle systems (Quest 90fps)

**Platform Compatibility Matrix**: `/Users/jamestunick/Documents/GitHub/Unity-XR-AI/PLATFORM_COMPATIBILITY_MATRIX.md`
- iOS vs Android vs Quest vs WebGL vs Vision Pro
- Pattern support status per platform
- Critical verifications (triple-checked 2025-11-02)
- Platform selection decision trees

### AI Assistant Workflow

When implementing features:

1. **Search knowledge base first**
   ```
   User: "Add body tracking particles"
   AI: [Reads _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md]
       [Searches "Human Depth/Segmentation" section]
       [Finds verified repos: YoHana19/HumanParticleEffect, keijiro/Rcam2]
       [Gets code snippet from _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md]
   ```

2. **Check platform compatibility**
   ```
   [Reads PLATFORM_COMPATIBILITY_MATRIX.md]
   [Verifies iOS: ✅ 91 joints, Quest: ⚠️ 70 joints]
   [Applies platform-specific optimizations]
   ```

3. **Copy code snippet and reference full implementation**
   ```
   [Copies snippet from _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md]
   [References external repo for full implementation]
   [Attributes source repo in code comments]
   [Tests on target platform]
   ```

4. **Never claim impossible without checking**
   - ❌ "That feature is impossible"
   - ✅ "Searched knowledge base, found 3 existing implementations: [repos]"

---

## 🔧 Project-Specific Setup

[Add your project-specific instructions here]

**Scene Structure**:
- Main scene: [SceneName.unity]
- Manager objects: [list key managers]

**Key Scripts**:
- [ScriptName.cs] - [description]
- [ScriptName.cs] - [description]

**Dependencies**:
```json
{
  "dependencies": {
    "com.unity.xr.arfoundation": "6.1.0",
    "com.unity.visualeffectgraph": "17.1.0",
    // ... add your packages
  }
}
```

---

## 📊 Platform Targets

[Specify your target platforms and performance goals]

| Platform | Target FPS | Max Particles | Memory Budget |
|----------|-----------|---------------|---------------|
| iOS      | 60        | 750K          | 6GB           |
| Quest 3  | 90        | 1M            | 8GB           |
| WebGL    | 60        | 100K          | 2GB           |

---

## 🚀 Build & Test

**Build Commands**:
```bash
# iOS build
# [Add your build commands]

# Quest build
# [Add your build commands]

# WebGL build
# [Add your build commands]
```

**Testing Workflow**:
1. [Step 1]
2. [Step 2]
3. [Step 3]

---

## 📝 Project Documentation

[Link to your project-specific docs]

- `docs/Architecture.md` - System architecture
- `docs/Features.md` - Feature specifications
- `docs/Troubleshooting.md` - Common issues and fixes

---

## 🔗 Related Projects

[If your project references other projects in workspace]

**Reference Projects**:
- Project 1: `/path/to/project1/` - [description]
- Project 2: `/path/to/project2/` - [description]

---

**Last Updated**: 2025-11-02
**Maintained by**: [Your Name]
