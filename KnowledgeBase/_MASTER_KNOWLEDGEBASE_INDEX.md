# Master Knowledgebase Index

**Version**: 1.0
**Last Updated**: 2025-01-07
**Purpose**: Complete map of all knowledge resources for AI-assisted development

---

## Quick Navigation

| Category | Files | Token Cost | Use When |
|----------|-------|------------|----------|
| [Setup & Config](#setup--configuration) | 2 files | ~5K | Setting up tools, troubleshooting |
| [Unity Development](#unity-development) | 5 files | ~15K | Unity XR/AR/VR tasks |
| [Web Development](#web-development) | 2 files | ~8K | WebGL/Three.js/React projects |
| [Performance](#performance--optimization) | 1 file | ~4K | Optimization needed |
| [GitHub Resources](#github-resources) | 1 file | ~10K | Finding repos/examples |
| [Project Planning](#project-planning) | 3 files | ~12K | Roadmaps, priorities |

**Total Knowledgebase**: 14 files, ~54K tokens (load selectively!)

---

## Setup & Configuration

### _MASTER_AI_TOOLS_REGISTRY.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_AI_TOOLS_REGISTRY.md
Size: ~4K tokens
Purpose: Unified registry of all AI tools and their configurations

Contains:
  - Claude Code setup (~/..claude/)
  - Windsurf config (~/.windsurf/)
  - Cursor setup (~/.cursor/)
  - MCP server configurations
  - Symlink strategy for shared access
  - Cross-tool memory sharing

Use When:
  - Setting up new AI tool
  - Configuring MCP servers
  - Creating symlinks for shared knowledge
  - Troubleshooting tool access
```

### _MASTER_KNOWLEDGEBASE_INDEX.md (This File)
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_KNOWLEDGEBASE_INDEX.md
Size: ~3K tokens
Purpose: Map of all knowledge resources

Contains:
  - File locations and descriptions
  - Token cost estimates
  - Quick navigation by topic
  - Loading recommendations

Use When:
  - Need to find specific knowledge
  - Planning what to load for a task
  - Onboarding new tool/user
```

---

## Unity Development

### _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md ⭐
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
Size: ~10K tokens
Purpose: 530+ curated GitHub repos for Unity XR/AR/VR

Contains:
  - Unity-Technologies official repos
  - Keijiro's VFX collection
  - Gaussian Splatting implementations
  - AR Foundation examples
  - Multiplayer/Normcore samples
  - Hand tracking solutions
  - Audio reactive VFX
  - DOTS/ECS examples

Use When:
  - Implementing new XR feature
  - Need code examples
  - Looking for Unity packages
  - Researching best practices

**CRITICAL**: Always search this BEFORE implementing Unity XR features!
```

### _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md
Size: ~3K tokens
Purpose: AR Foundation + VFX Graph code snippets and patterns

Contains:
  - ARKit integration examples
  - LiDAR depth processing
  - Human segmentation → particles
  - Face tracking VFX
  - Environment occlusion
  - Reusable code snippets

Use When:
  - Implementing AR features
  - Connecting AR data to VFX
  - Need working code snippets
  - Debugging AR issues
```

### _ADVANCED_AR_FEATURES_IMPLEMENTATION_PLAN.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_ADVANCED_AR_FEATURES_IMPLEMENTATION_PLAN.md
Size: ~8K tokens
Purpose: Comprehensive AR feature implementation guide

Contains:
  - Face tracking implementation
  - Body tracking setup
  - Hand tracking integration
  - Plane detection
  - Image tracking
  - Environment probes
  - Platform-specific code

Use When:
  - Planning complex AR feature
  - Need architecture guidance
  - Cross-platform AR development
  - Understanding AR Foundation APIs
```

### PLATFORM_COMPATIBILITY_MATRIX.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/PLATFORM_COMPATIBILITY_MATRIX.md
Purpose: Platform support for Unity features

Contains:
  - iOS vs Android feature matrix
  - Quest 2/3/Pro capabilities
  - ARKit vs ARCore comparison
  - WebGL limitations
  - VisionOS support

Use When:
  - Planning cross-platform features
  - Troubleshooting platform-specific issues
  - Checking feature availability
  - Optimizing for specific devices
```

### _CLAUDE_AI_DOCUMENTATION.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_CLAUDE_AI_DOCUMENTATION.md
Size: ~2K tokens
Purpose: Claude-specific Unity development patterns

Contains:
  - Claude Code workflows
  - Unity MCP usage
  - Agent strategies
  - Best practices

Use When:
  - Learning Claude Code for Unity
  - Setting up automated workflows
  - Using Unity MCP tools
```

---

## Web Development

### _WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md ⭐
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md
Size: ~6K tokens
Purpose: Complete WebGL/Three.js development reference

Contains:
  - Three.js core resources
  - React Three Fiber (R3F)
  - Critical CodeSandbox examples
  - Gaussian Splatting (web)
  - Shader resources (Shadertoy, GLSL)
  - Spatial viewers (PlayCanvas, Rerun)
  - P5.js prototyping
  - Icosa Gallery/Open Brush
  - 30-second deploy scripts

Use When:
  - Starting web 3D project
  - Need Three.js examples
  - Looking for shader inspiration
  - WebGL performance issues
  - Deploying to Vercel/Netlify

**Philosophy**: CDN > NPM, minimal dependencies, instant deploy
```

### _WEB_INTEROPERABILITY_STANDARDS.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_WEB_INTEROPERABILITY_STANDARDS.md
Size: ~8K tokens
Purpose: Web standards and Unity interop

Contains:
  - glTF/GLB formats
  - WebXR specifications
  - Unity → WebGL pipeline
  - Asset compression
  - Cross-platform formats

Use When:
  - Exporting Unity to web
  - Need standard formats
  - WebXR development
  - Asset pipeline setup
```

---

## Performance & Optimization

### _PERFORMANCE_PATTERNS_REFERENCE.md ⭐
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PERFORMANCE_PATTERNS_REFERENCE.md
Size: ~4K tokens
Purpose: Unity & WebGL performance patterns with code

Contains:
  Unity:
    - Object pooling (FastPool<T>)
    - Addressables async loading
    - VFX Graph optimization
    - Burst-compiled jobs
    - Quest 2 targets (90 FPS)

  WebGL:
    - Instanced rendering (1M @ 144fps)
    - Texture atlases
    - GPGPU particles
    - Web Workers
    - Performance targets

  Tools:
    - Unity Profiler usage
    - Chrome DevTools
    - Optimization decision tree

Use When:
  - Performance below target
  - Optimizing for Quest/mobile
  - Need code examples
  - Profiling applications
  - Setting performance budgets

**Critical**: Always profile before optimizing!
```

---

## GitHub Resources

### See: _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
530+ repos organized by:
- Unity XR frameworks
- Multiplayer tools
- ML & neural rendering
- VFX & particle systems
- Audio reactive
- Gaussian splatting
- DOTS/ECS
- WebGL viewers
- And much more!

---

## Project Planning

### _JT_PRIORITIES.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_JT_PRIORITIES.md
Size: ~5K tokens
Purpose: James Tunick's project priorities and roadmaps

Contains:
  - Current project status
  - Feature priorities
  - Technical debt
  - Sprint planning

Use When:
  - Planning work
  - Checking priorities
  - Understanding context
```

### _H3M_HOLOGRAM_ROADMAP.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_H3M_HOLOGRAM_ROADMAP.md
Size: ~2K tokens
Purpose: Hologram project roadmap

Contains:
  - Hologram features
  - Implementation plan
  - Milestones

Use When:
  - Working on hologram features
  - Planning hologram work
```

### _COMPREHENSIVE_AI_DEVELOPMENT_GUIDE.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_COMPREHENSIVE_AI_DEVELOPMENT_GUIDE.md
Size: ~12K tokens
Purpose: Original comprehensive AI development guide

Contains:
  - Complete link collection
  - Installation scripts
  - Best practices
  - All Unity docs
  - All web resources

Use When:
  - Need complete reference
  - Setting up new project
  - Comprehensive research needed

**Note**: Load selectively from this file - it's large!
```

---

## Troubleshooting & Logic

### TROUBLESHOOTING_AND_LOGIC.md
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/TROUBLESHOOTING_AND_LOGIC.md
Size: ~1K tokens
Purpose: Common issues and solutions

Contains:
  - Debugging strategies
  - Common errors
  - Resolution steps

Use When:
  - Encountering errors
  - Need debugging approach
  - Troubleshooting tools
```

---

## Agent Systems

### AgentSystems/ Directory
```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AgentSystems/
Purpose: Claude Code agent configurations

Contains:
  - Custom agent definitions
  - Agent templates
  - Specialized workflows

Use When:
  - Creating custom agents
  - Complex multi-step tasks
  - Automated workflows
```

---

## Loading Recommendations by Task

### Unity XR Feature Implementation
```yaml
Load:
  - _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md (10K)
  - _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md (3K)
  - _PERFORMANCE_PATTERNS_REFERENCE.md (4K)
Total: ~17K tokens

Approach:
  1. Search GitHub KB for examples
  2. Check AR Foundation snippets
  3. Implement with performance in mind
  4. Profile and optimize
```

### WebGL/Three.js Project
```yaml
Load:
  - _WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md (6K)
  - _PERFORMANCE_PATTERNS_REFERENCE.md (4K)
Total: ~10K tokens

Approach:
  1. Check CodeSandbox examples
  2. Use CDN-first approach
  3. Implement with web workers
  4. Deploy in 30 seconds
```

### Performance Optimization
```yaml
Load:
  - _PERFORMANCE_PATTERNS_REFERENCE.md (4K)
  - PLATFORM_COMPATIBILITY_MATRIX.md (2K)
Total: ~6K tokens

Approach:
  1. Profile first
  2. Find bottleneck
  3. Apply relevant pattern
  4. Measure improvement
```

### New AI Tool Setup
```yaml
Load:
  - _MASTER_AI_TOOLS_REGISTRY.md (4K)
  - _MASTER_KNOWLEDGEBASE_INDEX.md (3K)
Total: ~7K tokens

Approach:
  1. Create symlinks
  2. Configure MCP servers
  3. Copy global rules
  4. Test access
```

### Research/Discovery
```yaml
Load:
  - _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md (10K)
  - _COMPREHENSIVE_AI_DEVELOPMENT_GUIDE.md (12K)
Total: ~22K tokens

Approach:
  1. Search GitHub repos
  2. Check comprehensive guide
  3. Web search for latest
  4. Document findings
```

---

## Token Budget Guidelines

### Session Types
```yaml
Light Session (5-10K tokens):
  - Quick edits
  - Simple features
  - Bug fixes
  - Load: Global rules only

Medium Session (15-20K tokens):
  - New feature implementation
  - Performance optimization
  - Load: Global + 1-2 specific guides

Heavy Session (30-40K tokens):
  - Complex architecture
  - Research + implementation
  - Multi-system integration
  - Load: Multiple comprehensive guides

Research Session (40-60K tokens):
  - Deep investigation
  - Multiple technologies
  - Cross-platform planning
  - Load: Most of knowledgebase
```

### Optimization Tips
```yaml
✅ DO:
  - Load only what you need
  - Use index to find files
  - Search before loading full file
  - Reference links instead of full text

❌ DON'T:
  - Load entire knowledgebase at once
  - Load files you won't use
  - Ignore token budgets
  - Repeat content from docs
```

---

## Global Configuration Paths

### For All AI Tools
```bash
# Global rules (symlinked)
~/CLAUDE.md

# Knowledgebase (symlinked)
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/

# Tool-specific
~/.claude/               # Claude Code
~/.windsurf/             # Windsurf
~/.cursor/               # Cursor
~/.config/github-copilot/ # Copilot
```

### Quick Access Setup
```bash
# Create symlinks for all tools
KB=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
ln -sf $KB ~/.claude/knowledgebase
ln -sf $KB ~/.windsurf/knowledgebase
ln -sf $KB ~/.cursor/knowledgebase
```

---

## Self-Improving Architecture

### Knowledge Flow
```
Task Discovery → Document → Categorize → Share → All Tools Benefit
     ↑                                                       ↓
     └────────────── Better Performance ←────────────────────┘
```

### Update Workflow
```yaml
1. Discovery:
   - New pattern found
   - Performance improvement
   - Better approach

2. Document:
   - Add to relevant KB file
   - Include code example
   - Note context/constraints

3. Categorize:
   - Update this index
   - Add to GitHub KB if repo
   - Tag for search

4. Share:
   - Commit to Git
   - Symlinks auto-sync
   - All tools see it

5. Benefit:
   - Faster next time
   - Better quality
   - Shared learning
```

---

## Success Criteria

### Knowledge Accessibility
- ✅ All AI tools can access entire knowledgebase
- ✅ Zero duplication across tools
- ✅ Instant sync via symlinks
- ✅ Organized by topic and use case
- ✅ Token-optimized loading

### Developer Experience
- ✅ Find what you need in < 30 seconds
- ✅ Load only what's relevant
- ✅ Examples for every pattern
- ✅ Clear navigation paths
- ✅ Self-documenting structure

### Intelligence Growth
- ✅ Continuous knowledge accumulation
- ✅ Cross-project pattern recognition
- ✅ Automated categorization (future)
- ✅ Version controlled history
- ✅ World-class expert quality

---

**Navigation Tip**: Press Ctrl+F and search for your task keyword to jump to relevant section.

**Remember**: This index is your map. The knowledgebase files are your treasure. Load wisely, work efficiently.
