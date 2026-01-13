# Unity XR AI Knowledgebase

> **Permanent intelligence amplification infrastructure for Unity XR/AR/VR development**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Token Optimized](https://img.shields.io/badge/tokens-9.3K-green.svg)](https://github.com/imclab/xrai)
[![Security Audited](https://img.shields.io/badge/security-audited-brightgreen.svg)](KnowledgeBase/_PROJECT_CONFIG_REFERENCE.md)

A comprehensive, token-optimized knowledgebase with an AI Agent Intelligence Amplification System designed for 10x faster learning and execution in Unity XR development.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [AI Agent System](#ai-agent-system)
- [Visualization Frontends](#️-visualization-frontends-vis)
- [Knowledgebase Structure](#knowledgebase-structure)
- [Monitoring & Maintenance](#monitoring--maintenance)
- [KB Management Tools](#kb-management-tools)
- [Usage Examples](#usage-examples)
- [Contributing](#contributing)
- [Recent Updates](#-recent-updates)

---

## 🎯 Overview

This repository contains a self-improving knowledgebase and AI agent system optimized for Unity XR (AR/VR/MR) development. It implements state-of-the-art best practices from 2026, including:

- **AI Agent Intelligence Amplification** - 7 meta-frameworks unified into a single execution system
- **Token-optimized architecture** - 9.3K tokens (26% reduction from initial 12.7K)
- **Security audited** - No private information, clean codebase
- **Cross-tool compatible** - Works with Claude Code, Windsurf, Cursor, and other AI tools
- **Comprehensive documentation** - AR Foundation, VFX, WebGL, performance patterns, and more

**Primary use cases**:

- Unity AR/VR/XR development with AR Foundation 6.x
- VFX and particle systems optimization
- WebGL/Three.js integration
- Cross-platform performance optimization (Quest, iOS, Android)
- AI-assisted development workflows

---

## ⚡ Key Features

### 1. **AI Agent Intelligence Amplification System**

Ultra-compact directive (875 tokens) that transforms every interaction into compound learning:

- **Leverage Hierarchy**: `Existing(0h) > Adapt(0.1x) > AI(0.3x) > Scratch(1x)`
- **Emergency Override**: Protocol for >15min stalls (Simplify → Leverage → Reframe → Ship)
- **Auto-logging**: Pattern extraction → `LEARNING_LOG.md`
- **Success Metrics**: Session/month/quarter tracking

**Philosophy**: Based on mental models from Elon Musk (first principles), Naval Ravikant (leverage), and Jeff Bezos (long-term thinking).

### 2. **Intelligence Pattern Libraries (NEW)**

Three activation phrase-based pattern libraries for domain-specific knowledge:

| Phrase | Patterns | Coverage |
|--------|----------|----------|
| **"Using Unity Intelligence patterns"** | 500+ | ARFoundation, VFX Graph, DOTS, Normcore, Open Brush |
| **"Using WebGL Intelligence patterns"** | 200+ | WebGPU, Three.js, R3F, GLSL, WebXR |
| **"Using 3DVis Intelligence patterns"** | 100+ | Sorting, clustering, anomaly detection, force layouts |

### 3. **Comprehensive Unity XR Knowledge**

- **AR Foundation 6.2+** - Plane detection, image tracking, human segmentation, meshing
- **VFX Optimization** - Quest 2/3 performance patterns, GPU-instanced particles
- **WebGL Integration** - Three.js interop, Unity-to-Web bridges
- **Cross-platform** - iOS, Android, Quest, WebGL compatibility matrices
- **GitHub Repos** - Curated collection of high-quality XR projects

### 4. **Monitoring & Automation**

Built-in tools for health checking and optimization:

- `ai-system-monitor.sh` - Token usage, symlink verification, health dashboard (<3s)
- `kb-security-audit.sh` - Privacy scanning, permission checks, integrity validation
- `validate-ai-config.sh` - Configuration validation and auto-healing

**New KB Management Tools**:

- `kb-add` - Easy manual/automatic KB additions (patterns, insights, auto-git extraction)
- `kb-audit` - Health check with metrics, recommendations, and security scan

### 5. **Token-Optimized Architecture**

```
Load Order:
1. GLOBAL_RULES.md        (~7.6K tokens)
2. AI_AGENT_V3.md         (~0.9K tokens)
3. Claude config          (~0.8K tokens)
4. Project overrides      (~2K tokens)
────────────────────────────────────────
Total overhead:           ~9.3K tokens ✅
```

**26% reduction** from initial 12.7K tokens while maintaining full functionality.

---

## 🚀 Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/imclab/xrai.git
cd xrai

# Set up symlinks for AI tools (optional)
ln -sf $(pwd)/KnowledgeBase ~/.claude/knowledgebase
ln -sf $(pwd)/KnowledgeBase ~/.windsurf/knowledgebase
ln -sf $(pwd)/KnowledgeBase ~/.cursor/knowledgebase
```

### Basic Usage

**With Claude Code**:

```bash
# The knowledgebase is automatically loaded via configuration hierarchy
# See ~/.claude/CLAUDE.md for load order
```

**Access the AI Agent directive**:

```bash
cat ~/.claude/AI_AGENT_CORE_DIRECTIVE_V3.md
```

**Browse the knowledgebase**:

```bash
ls KnowledgeBase/
cat KnowledgeBase/_MASTER_KNOWLEDGEBASE_INDEX.md
```

---

## 🌐 Access Methods

### Local Machine Access

**1. Direct Filesystem Access**

After cloning, the knowledgebase is immediately available:

```bash
# Navigate to repository
cd ~/path/to/xrai

# Read any file directly
cat KnowledgeBase/LEARNING_LOG.md
cat KnowledgeBase/_AI_AGENT_PHILOSOPHY.md

# Search across all knowledge
rg "performance optimization" KnowledgeBase/

# List all markdown files
find KnowledgeBase -name "*.md" -type f
```

**2. Symlinked Access (Recommended for AI Tools)**

Create symlinks for seamless integration with AI development tools:

```bash
# Claude Code
ln -sf ~/path/to/xrai/KnowledgeBase ~/.claude/knowledgebase

# Windsurf
ln -sf ~/path/to/xrai/KnowledgeBase ~/.windsurf/knowledgebase

# Cursor
ln -sf ~/path/to/xrai/KnowledgeBase ~/.cursor/knowledgebase

# Verify symlinks
ls -lh ~/.claude/knowledgebase
ls -lh ~/.windsurf/knowledgebase
ls -lh ~/.cursor/knowledgebase
```

**3. IDE/Editor Integration**

**VS Code** (with Claude Code extension):

```bash
# Open in VS Code
code ~/path/to/xrai

# The knowledgebase is automatically available via MCP
# Files appear in: ~/.claude/knowledgebase/
```

**Command Line** (with ripgrep):

```bash
# Search for Unity patterns
rg -i "AR Foundation|ARKit" ~/.claude/knowledgebase/

# Search for performance tips
rg "Quest.*90 fps|optimization" ~/.claude/knowledgebase/

# Find specific topics
rg "VFX Graph|particle system" ~/.claude/knowledgebase/
```

**4. Monitoring Tools (if installed)**

```bash
# Health check
ai-system-monitor.sh --quick

# Security audit
kb-security-audit.sh

# Token usage analysis
ai-system-monitor.sh --full | grep "token usage"
```

---

### Cloud Access

**1. GitHub Web Interface**

Browse directly in your browser:

- **Main repo**: https://github.com/imclab/xrai
- **Knowledgebase**: https://github.com/imclab/xrai/tree/main/KnowledgeBase
- **Specific file**: https://github.com/imclab/xrai/blob/main/KnowledgeBase/LEARNING_LOG.md

**2. GitHub API (RESTful Access)**

Access files programmatically:

```bash
# List knowledgebase contents
curl https://api.github.com/repos/imclab/xrai/contents/KnowledgeBase

# Get specific file (base64 encoded)
curl https://api.github.com/repos/imclab/xrai/contents/KnowledgeBase/LEARNING_LOG.md

# Search within repository
curl -H "Accept: application/vnd.github.v3+json" \
  https://api.github.com/search/code?q=performance+repo:imclab/xrai
```

**3. Raw File Access (Direct Download)**

Access raw markdown files:

```bash
# Direct raw file URL
curl https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/LEARNING_LOG.md

# Download specific file
wget https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_AI_AGENT_PHILOSOPHY.md

# View in browser
open "https://raw.githubusercontent.com/imclab/xrai/main/KnowledgeBase/_MASTER_KNOWLEDGEBASE_INDEX.md"
```

**4. Clone on Any Device**

Access from any machine with git:

```bash
# Clone to new device
git clone https://github.com/imclab/xrai.git ~/xrai-remote
cd ~/xrai-remote

# Pull latest updates
git pull origin main

# Read-only access (no git needed)
curl -L https://github.com/imclab/xrai/archive/refs/heads/main.zip -o xrai.zip
unzip xrai.zip
cd xrai-main/KnowledgeBase
```

**5. GitHub Mobile App**

On iOS/Android:

1. Install GitHub mobile app
2. Navigate to `imclab/xrai`
3. Browse `KnowledgeBase/` folder
4. Read any `.md` file (rendered)

---

### Multi-Device Sync Strategy

**Scenario 1: Work from multiple machines**

```bash
# Machine 1: Set up and push changes
cd ~/xrai
echo "New discovery..." >> KnowledgeBase/LEARNING_LOG.md
git add . && git commit -m "Add discovery"
git push origin main

# Machine 2: Pull changes
cd ~/xrai
git pull origin main
# Your changes are now synced
```

**Scenario 2: Mobile-to-Desktop workflow**

1. **On mobile**: View files via GitHub web or app
2. **Take notes**: Use GitHub Issues or local notes app
3. **On desktop**: Pull repo and add notes to `LEARNING_LOG.md`
4. **Push changes**: Available everywhere

**Scenario 3: Cloud-first (no local clone)**

```bash
# Edit via GitHub web interface
# 1. Navigate to file on GitHub
# 2. Click "Edit" (pencil icon)
# 3. Make changes
# 4. Commit directly to main

# Or use GitHub CLI
gh repo clone imclab/xrai
cd xrai
gh browse  # Opens in browser
```

---

### AI Tool Integration

**Claude Code** (automatic):

```bash
# Configuration auto-loads KB via symlink
# Location: ~/.claude/knowledgebase/
# See: ~/.claude/CLAUDE.md for load order

# Query in Claude Code:
# "Check knowledgebase for AR Foundation patterns"
# "Search KB for Quest optimization techniques"
```

**Windsurf**:

```bash
# Set up symlink (one-time)
ln -sf ~/xrai/KnowledgeBase ~/.windsurf/knowledgebase

# Access in Windsurf:
# Files appear in sidebar under "Knowledgebase"
```

**Cursor**:

```bash
# Set up symlink (one-time)
ln -sf ~/xrai/KnowledgeBase ~/.cursor/knowledgebase

# Access in Cursor:
# Reference files via @knowledgebase/filename.md
```

**MCP Server** (if available):

```python
# Via Model Context Protocol
# Tools have access to:
# - mcp__filesystem__read_file(path="~/.claude/knowledgebase/LEARNING_LOG.md")
# - mcp__filesystem__search_files(pattern="*.md")
# - mcp__filesystem__list_directory(path="~/.claude/knowledgebase/")
```

---

### Access Patterns Comparison

| Method                     | Speed   | Offline    | Multi-device | Versioned | AI-Ready          |
| -------------------------- | ------- | ---------- | ------------ | --------- | ----------------- |
| **Local filesystem** | Instant | ✅         | ❌           | ✅ (git)  | ✅                |
| **Symlinks**         | Instant | ✅         | ❌           | ✅ (git)  | ✅✅              |
| **GitHub web**       | 1-2s    | ❌         | ✅           | ✅        | ❌                |
| **GitHub API**       | 1-2s    | ❌         | ✅           | ✅        | ✅ (programmatic) |
| **Git clone**        | 5-10s   | ✅ (after) | ✅           | ✅        | ✅                |
| **Raw files**        | 1-2s    | ❌         | ✅           | ❌        | ⚠️ (limited)    |

**Recommended**:

- **Development**: Local clone + symlinks (instant, AI-integrated)
- **Reference**: GitHub web (convenient, no setup)
- **Automation**: GitHub API (programmatic access)
- **Multi-device**: Git clone on each machine + sync via push/pull

---

## 🏗️ Architecture

### Directory Structure

```
Unity-XR-AI/
├── KnowledgeBase/                 # Core knowledge repository (~75 MD files)
│   ├── .claude/                   # Claude-specific documentation
│   ├── AgentSystems/              # Agent architecture patterns
│   ├── CodeSnippets/              # Reusable code snippets
│   ├── scripts/                   # KB automation scripts
│   ├── _scraps/                   # Archived files (aliases, dated reports)
│   ├── LEARNING_LOG.md            # Continuous discovery log
│   ├── _MASTER_KNOWLEDGEBASE_INDEX.md
│   ├── _PROJECT_CONFIG_REFERENCE.md  # All configs documented
│   ├── _VFX25_HOLOGRAM_PORTAL_PATTERNS.md
│   └── [70+ knowledge files]
├── Vis/                           # 3D Visualization frontends
│   ├── xrai-kg/                   # Modular KG library (ECharts)
│   ├── HOLOVIS/                   # Three.js holographic visualizer
│   ├── cosmos-standalone-web/     # 3d-force-graph visualizer
│   ├── cosmos-needle-web/         # Needle Engine WebXR
│   ├── cosmos-visualizer/         # D3 + Three.js graphs
│   ├── WarpDashboard/             # Jobs data dashboard
│   ├── chalktalk-master/          # Ken Perlin's Chalktalk (WebGL)
│   └── *.html                     # Standalone dashboards
├── MetavidoVFX-main/              # Unity VFX project (AR Foundation)
│   ├── Assets/H3M/                # H3M Hologram system
│   ├── Packages/                  # Unity packages
│   └── build_and_deploy.sh        # iOS build scripts
├── Scripts/                       # Utility scripts
│   └── scraps/                    # Archived experimental scripts
├── mcp-server/                    # MCP KB Server (TypeScript)
├── specs/                         # Spec-Kit specifications
├── xrai-speckit/                  # Specify.ai templates
├── build_ios.sh                   # iOS Unity build
├── deploy_ios.sh                  # iOS device deploy
├── install.sh                     # Project setup
├── CLAUDE.md                      # Configuration pointer
└── README.md                      # This file
```

### Integration Points

**Local Tools** (via symlinks):

```
~/.claude/knowledgebase/    → xrai/KnowledgeBase/
~/.windsurf/knowledgebase/  → xrai/KnowledgeBase/
~/.cursor/knowledgebase/    → xrai/KnowledgeBase/
```

**Cloud Access** (via GitHub):

```
GitHub API: https://api.github.com/repos/imclab/xrai/contents/
Web: https://github.com/imclab/xrai
```

---

## 🤖 AI Agent System

### Core Principles

**1. Leverage-First Execution**

```
Always prefer higher leverage:
├─ 0h:   Use existing solution (search KB, past projects)
├─ 0.1x: Adapt from KB (modify existing pattern)
├─ 0.3x: AI-assisted generation with review
└─ 1x:   Write from scratch (avoid unless necessary)
```

**2. Compound Learning**
Every task should:

- Extract 2-3 reusable patterns
- Document novel insights → `LEARNING_LOG.md`
- Identify automation opportunities
- Connect to past projects (cross-domain learning)

**3. Emergency Override**
If stuck >5 minutes:

1. **Simplify**: Did we overcomplicate? What's the dumbest working solution?
2. **Leverage**: Who solved this already?
3. **Reframe**: Is this the right problem?
4. **Ship**: Can we ship 20% now and iterate?

### Philosophy

See [`KnowledgeBase/_AI_AGENT_PHILOSOPHY.md`](KnowledgeBase/_AI_AGENT_PHILOSOPHY.md) for deep-dive on:

- Billionaire-level thinking (systems over symptoms)
- 10x learning velocity (spaced extraction, Feynman technique)
- Time compression strategies (10 years → 1 year)
- Identity reprogramming (limiting beliefs → growth beliefs)

### Quick Reference

Daily activation checklist:

**Pre-Task** (5 seconds):

- [ ] Can I reuse existing solution?
- [ ] What's the 20% that gives 80%?
- [ ] What pattern will I extract?

**Post-Task** (10 seconds):

- [ ] Novel pattern → `LEARNING_LOG.md`
- [ ] Automation opportunity?
- [ ] Meta-reflection: How can I think better?

---

## 🖥️ Visualization Frontends (Vis/)

The `Vis/` folder contains 10 3D visualization and dashboard tools, sorted by creation date:

| Project | Created | Stack | Description |
|---------|---------|-------|-------------|
| **chalktalk-master** | 2018 | Node.js + WebGL | Ken Perlin's sketch-to-3D visualization |
| **HOLOVIS** | 2025-06 | Three.js + Express | Unity codebase 3D visualizer |
| **cosmos-visualizer** | 2025-09 | Vite + D3 + Three.js | Force-directed graph visualization |
| **cosmos-standalone-web** | 2025-09 | Vite + 3d-force-graph | Standalone 3D force graphs |
| **cosmos-needle-web** | 2025-09 | Needle Engine + Vite | WebXR-ready visualization |
| **WarpDashboard** | 2025-12 | Static HTML | Jobs data dashboard |
| **xrai-kg** | 2026-01 | ES6 + ECharts | Modular knowledge graph library |
| **dashboard.html** | 2026-01 | Standalone HTML | General dashboard |
| **knowledge-graph-*.html** | 2026-01 | ECharts | Interactive knowledge graph dashboards |

**Quick Start**:
```bash
cd Vis/xrai-kg && npm install && npm run dev     # ECharts KG library
cd Vis/HOLOVIS && npm install && npm run serve   # Three.js visualizer
cd Vis/cosmos-standalone-web && npm run dev      # 3D force graph
```

See [`Vis/README.md`](Vis/README.md) for complete setup documentation.

---

## 📚 Knowledgebase Structure

### Core Files

| File                                      | Purpose                           | Size         |
| ----------------------------------------- | --------------------------------- | ------------ |
| `LEARNING_LOG.md`                       | Continuous discoveries & patterns | Growing      |
| `_AI_AGENT_PHILOSOPHY.md`               | Mental models & why               | ~3.5K tokens |
| `_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md`   | AR Foundation + VFX patterns      | ~15K tokens  |
| `_WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md` | WebGL/Three.js integration        | ~12K tokens  |
| `_PERFORMANCE_PATTERNS_REFERENCE.md`    | Unity & WebGL optimization        | ~8K tokens   |
| `_MASTER_KNOWLEDGEBASE_INDEX.md`        | Navigation & organization         | ~2K tokens   |

### Topics Covered

**Unity XR**:

- AR Foundation 6.x (plane detection, image tracking, meshing)
- ARKit & ARCore platform-specific features
- Human segmentation, body tracking, face tracking
- XR Interaction Toolkit patterns

**VFX & Performance**:

- GPU-instanced particles (Quest 2/3 optimized)
- Visual Effect Graph best practices
- Shader optimization patterns
- Cross-platform performance targets

**WebGL Integration**:

- Unity → Three.js bridges
- WebXR compatibility
- Performance considerations
- Asset pipeline

**GitHub Resources**:

- Curated high-quality XR repositories
- Example projects & demos
- Community tools & libraries

### Searching the Knowledgebase

```bash
# Find performance-related content
rg -i "performance|optimization|fps" KnowledgeBase/

# Find Unity-specific patterns
rg -i "unity|arfoundation|xr" KnowledgeBase/

# Search by project
rg "Portals_6|Paint-AR" KnowledgeBase/

# View recent discoveries
tail -n 100 KnowledgeBase/LEARNING_LOG.md
```

---

## 🔧 Monitoring & Maintenance

### Health Dashboard

**Quick check** (<3 seconds):

```bash
ai-system-monitor.sh --quick
```

**Full audit** (~10 seconds):

```bash
ai-system-monitor.sh --full
```

**Auto-heal issues**:

```bash
ai-system-monitor.sh --fix
```

### Security Audit

```bash
kb-security-audit.sh
```

Checks for:

- Sensitive data patterns (API keys, passwords)
- File permissions (world-writable files)
- Symlink integrity
- Git status & remote configuration

---

## 🛠️ KB Management Tools

Easy manual and automatic additions to the knowledgebase with built-in auditing.

### kb-add - Easy Additions

Add patterns, insights, and discoveries to your knowledgebase in seconds:

```bash
# Add patterns (reusable solutions)
kb-add --pattern "Use symlinks for cross-tool KB access"

# Add insights (mental models)
kb-add --insight "Token optimization: separate philosophy from protocols"

# Quick daily notes
kb-add --quick "Quest 2 needs 90 FPS for smooth hand tracking"

# Auto-extract from git commits
kb-add --auto-git

# Interactive mode (guided)
kb-add -i

# Create new KB file with template
kb-add --file unity-xr-tips.md -i
```

**Quick aliases** (load with `source ~/.local/bin/kb-aliases.sh`):

```bash
kb-p "pattern"       # Add pattern
kb-i "insight"       # Add insight
kb-a "antipattern"   # Add anti-pattern
kb-quick "note"      # Quick note
kb-auto              # Auto-extract from git
```

### kb-audit - Health Check

Comprehensive knowledgebase audit with metrics, recommendations, and security:

```bash
# Quick audit (5 seconds)
kb-audit --quick

# Full audit with recommendations (15 seconds)
kb-audit --full

# Security scan only
kb-audit --security

# Metrics only
kb-audit --metrics

# Health score only
kb-audit --health
```

**Example output**:

```
━━━ KB Quick Audit ━━━

Metrics:
  Files: 37 total, 37 this week
  Size: 1.05MB
  Learning entries: 9
  Git commits: 3

Health Score: 100/100 (Excellent)
```

**Health score ranges**:

- **90-100**: Excellent ⭐
- **70-89**: Good ✓
- **50-69**: Needs improvement ⚠️
- **<50**: Critical issues ❌

**Full audit includes**:

1. Metrics (file counts, size, growth rate)
2. Security (sensitive data scan, permissions)
3. Health score (overall rating)
4. Recommendations (prioritized improvements)
5. File analysis (recent additions, largest files)

### Quick Workflows

**Daily pattern extraction** (30 seconds):

```bash
kb-check              # Quick health check
kb-auto               # Extract from git
kb-i "Your insight"   # Add manual insights
```

**Weekly full audit** (5 minutes):

```bash
kb-full --report ~/Desktop/kb-audit-$(date +%Y%m%d).md
# Review recommendations
kb-commit             # Commit changes
```

**Post-session reflection** (2 minutes):

```bash
kb-p "Pattern discovered"
kb-i "Insight about system"
kb-check
```

See [KB_TOOLS_REFERENCE.md](KnowledgeBase/KB_TOOLS_REFERENCE.md) for complete documentation.

---

## 💡 Usage Examples

### Example 1: Starting a New Unity XR Project

```bash
# 1. Access AR Foundation knowledge
cat KnowledgeBase/_ARFOUNDATION_VFX_KNOWLEDGE_BASE.md

# 2. Check platform compatibility
rg "Quest 2|Quest 3" KnowledgeBase/_PERFORMANCE_PATTERNS_REFERENCE.md

# 3. Find example projects
cat KnowledgeBase/_MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
```

### Example 2: Optimizing VFX Performance

```bash
# 1. Search for performance patterns
rg -i "gpu instancing|particle" KnowledgeBase/

# 2. Check Quest-specific optimizations
rg "Quest.*fps|90 fps" KnowledgeBase/

# 3. Log discoveries
echo "## $(date +%Y-%m-%d) - VFX Optimization Discovery
**Context**: Optimizing particles for Quest 2
**Discovery**: GPU instancing reduced draw calls by 80%
**Pattern**: Use Graphics.DrawMeshInstanced for >100 particles
**ROI**: 15ms → 3ms frame time
" >> KnowledgeBase/LEARNING_LOG.md
```

### Example 3: AI-Assisted Development

**Activate AI Agent directive**:

```
> "Apply AI Agent Core Directive: Check knowledgebase for AR Foundation
  patterns, focus on highest-leverage approach, explain trade-offs."
```

**When stuck**:

```
> "Emergency override: simplest solution for hand tracking on Quest 3?
  Who solved this already?"
```

**End of session**:

```
> "Extract 2-3 patterns for LEARNING_LOG.md. What automation
  opportunities exist?"
```

---

## 🤝 Contributing

### Adding to Knowledgebase

**When to add**:

- ✅ Novel patterns discovered
- ✅ Performance optimizations found
- ✅ Platform-specific workarounds
- ✅ GitHub repos that solve real problems
- ✅ Reusable code patterns

**When NOT to add**:

- ❌ Routine tasks with no new learning
- ❌ Temporary/session-specific info
- ❌ Already documented elsewhere
- ❌ Personal notes

### Format for LEARNING_LOG.md

```markdown
## YYYY-MM-DD - [Tool Name] - [Brief Title]

**Discovery**: [What was learned/discovered]

**Context**: [What prompted this, what problem was solved]

**Pattern**: [Reusable approach]

**Future Application**: [Where else can this apply?]

**ROI**: [Time saved / leverage gained]

---
```

---

## 📈 Success Metrics

### Per Session

- **Leverage ratio**: Hours saved / invested (target: >3:1)
- **Insights extracted**: (target: ≥2)
- **Automations identified**: (target: ≥1)

### Per Month

- **Speed**: 2x faster than last month?
- **Patterns reused**: ≥5?
- **Complexity**: Decreasing?

### Per Quarter

- **Capability multiplier**: ≥3 new capabilities unlocked
- **Time freedom**: ≥20 hours gained through automation
- **Mastery progression**: ≥1 skill level increase

---

## 📄 License

MIT License - See [LICENSE](LICENSE) for details

---

## 🔗 Links

- **Repository**: https://github.com/imclab/xrai
- **Issues**: https://github.com/imclab/xrai/issues
- **Discussions**: https://github.com/imclab/xrai/discussions

---

## 📝 Recent Updates

### 2026-01-13 - Intelligence Pattern Libraries & Memory System

- ✅ **Intelligence Pattern Libraries** (NEW):
  - `_UNITY_PATTERNS_BY_INTEREST.md` - Brushes, hand tracking, audio reactive, LiDAR (459 lines)
  - `_WEBGL_INTELLIGENCE_PATTERNS.md` - WebGPU, Three.js, R3F, GLSL (548 lines)
  - `_3DVIS_INTELLIGENCE_PATTERNS.md` - Sorting, clustering, anomaly detection (698 lines)
  - Activation phrases added to GLOBAL_RULES.md (REQUIRED for all projects)
- ✅ **MCP Memory/Knowledge Graph**:
  - Location verified: `~/Applications/claude_memory.json` (42KB, 99+ entities)
  - New entities added: Unity_Intelligence_Patterns, WebGL_Intelligence_Patterns, 3DVis_Intelligence_Patterns
  - Documentation updated: `_GLOBAL_RULES_AND_MEMORY.md`
- ✅ **GitHub repos extracted**:
  - Open Brush (GeometryPool, GeniusParticlesBrush patterns)
  - Operation Swarm (WebGPU 400K+ particles)
  - HandPoseBarracuda (gesture recognition)

### 2026-01-13 (Earlier) - Project Organization & Vis Folder

- ✅ **Vis/ folder created**: Centralized 10 visualization frontends
  - xrai-kg, HOLOVIS, cosmos-*, WarpDashboard, chalktalk-master
  - Comprehensive `Vis/README.md` with setup docs sorted by date
- ✅ **KnowledgeBase cleanup**:
  - Moved 35 files to `_scraps/` (aliases, dated reports, old docs)
  - Now 78+ active MD files with clear organization
  - Created `_PROJECT_CONFIG_REFERENCE.md` documenting all configs
- ✅ **Scripts organized**:
  - Moved 19 experimental scripts to `Scripts/scraps/`
  - Kept core build scripts (`build_ios.sh`, `deploy_ios.sh`, `install.sh`)
- ✅ **VFX25 patterns extracted**: `_VFX25_HOLOGRAM_PORTAL_PATTERNS.md`
  - BodyPixSentis, MetavidoVFX, Rcam4, Portal stencil patterns

### 2026-01-08 - KB Management Tools Added

- ✅ **kb-add**: Easy manual/automatic KB additions
- ✅ **kb-audit**: Comprehensive health check with metrics
- ✅ Shell aliases for rapid access (kb-p, kb-i, kb-check, etc.)

### 2026-01-08 - Initial Release

- ✅ AI Agent Intelligence Amplification System (V3 ultra-compact)
- ✅ Token optimization: 9.3K overhead (26% reduction)
- ✅ Security audit passed
- ✅ Monitoring tools (health dashboard, security scanner)
- ✅ Full spec-kit compliance

---

## 🙏 Acknowledgments

**AI Agent philosophy** based on:

- Naval Ravikant (leverage thinking)
- Elon Musk (first principles reasoning)
- Jeff Bezos (long-term compounding)

**Knowledge contributions** from:

- Unity XR AI community
- AR Foundation developers
- Open source XR projects

**Built with**:

- Claude Sonnet 4.5 (AI pair programming)
- GitHub spec-kit (specification format)
- State-of-the-art 2026 best practices

---

**Made with ❤️ for the Unity XR community**

*Permanent intelligence amplification infrastructure for faster learning and execution.*
