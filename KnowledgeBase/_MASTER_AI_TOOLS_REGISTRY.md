# Master AI Tools Configuration Registry

**Version**: 1.0
**Last Updated**: 2025-01-07
**Purpose**: Unified registry of all AI development tools, their configurations, and shared knowledgebase access

---

## Philosophy: Shared Intelligence, Zero Duplication

**Core Principles**:
1. Single source of truth for all knowledge
2. Symlinked configurations for instant sync
3. Modular, tool-agnostic documentation
4. Token-optimized, fast loading
5. Self-improving through continuous learning

---

## AI Tools Installed on Mac

### Primary Development Tools

#### Claude Code (CLI)
```yaml
Location: ~/.claude/
Config: ~/.claude/CLAUDE.md
Global Config: ~/.claude/CLAUDE.md
Docs: ~/.claude/docs/
Commands: ~/.claude/commands/
Agents: ~/.claude/agents/
History: ~/.claude/history.jsonl
Settings: ~/.claude/settings.json

Key Features:
  - MCP server integration
  - Autonomous agents
  - Shell integration
  - File history tracking
  - Custom commands/skills
```

#### Windsurf (VSCode-based IDE)
```yaml
Location: ~/.windsurf/
Config: ~/.windsurfignore, ~/.windsurf_profile
MCP Config: ~/.windsurf/mcp.json
Extensions: ~/.windsurf/extensions/

Key Features:
  - VSCode compatibility
  - MCP server support
  - Extension ecosystem
  - Performance monitoring
```

#### Cursor (AI-powered IDE)
```yaml
Location: ~/.cursor/
Config: ~/.cursorignore
MCP Config: ~/.cursor/mcp.json
Extensions: ~/.cursor/extensions/
Tutor: ~/.cursor-tutor

Key Features:
  - AI pair programming
  - MCP server support
  - VSCode extensions
  - Context-aware suggestions
```

#### GitHub Copilot
```yaml
Location: ~/.config/github-copilot/
Integration: VSCode/Cursor/Windsurf extensions

Key Features:
  - Code completion
  - Multi-language support
  - GitHub integration
```

### Potential Future Tools
```yaml
Gemini Code Assist: TBD
Amazon CodeWhisperer: TBD
Tabnine: TBD
Cody: TBD
Antigravity: TBD (if exists)
```

---

## Unified Knowledgebase Architecture

### Central Knowledgebase Location
```bash
# Primary knowledgebase
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/

# Global AI rules (shared across all tools)
~/CLAUDE.md (symlinked to multiple locations)
~/.claude/CLAUDE.md
~/.windsurf/CLAUDE.md (to be created)
~/.cursor/CLAUDE.md (to be created)
```

### Knowledgebase Structure
```
~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
├── _MASTER_AI_TOOLS_REGISTRY.md          # This file
├── _MASTER_KNOWLEDGEBASE_INDEX.md        # Complete index (to be created)
├── _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md  # 530+ GitHub repos
├── _WEBGL_THREEJS_COMPREHENSIVE_GUIDE.md # Web development
├── _PERFORMANCE_PATTERNS_REFERENCE.md    # Optimization patterns
├── _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md   # AR/VFX snippets
├── _COMPREHENSIVE_AI_DEVELOPMENT_GUIDE.md # Full reference
├── _ADVANCED_AR_FEATURES_IMPLEMENTATION_PLAN.md
├── _WEB_INTEROPERABILITY_STANDARDS.md
├── _JT_PRIORITIES.md
├── _H3M_HOLOGRAM_ROADMAP.md
├── _CLAUDE_AI_DOCUMENTATION.md
├── TROUBLESHOOTING_AND_LOGIC.md
└── AgentSystems/                         # Agent configurations
```

### Global Configuration Files
```bash
# Global rules (symlinked everywhere)
~/CLAUDE.md → Multiple tool locations
~/.claude/CLAUDE.md → Claude Code primary

# Tool-specific configs
~/.claude/settings.json
~/.windsurf/mcp.json
~/.cursor/mcp.json
~/.config/github-copilot/
```

---

## Symlink Strategy for Shared Access

### Implementation Plan
```bash
#!/bin/bash
# Create symlinks for shared knowledgebase access

KB_PATH=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
GLOBAL_RULES=~/CLAUDE.md

# Ensure knowledgebase is accessible
# Method 1: Symlink entire knowledgebase (recommended)
ln -sf $KB_PATH ~/.claude/knowledgebase
ln -sf $KB_PATH ~/.windsurf/knowledgebase
ln -sf $KB_PATH ~/.cursor/knowledgebase

# Method 2: Symlink global rules to each tool
ln -sf $GLOBAL_RULES ~/.claude/CLAUDE.md
ln -sf $GLOBAL_RULES ~/.windsurf/CLAUDE.md
ln -sf $GLOBAL_RULES ~/.cursor/CLAUDE.md

# Method 3: Add knowledgebase path to each tool's config
# (Tool-specific implementation varies)
```

### Accessing Knowledgebase from Any Tool
```yaml
From Claude Code:
  - Direct: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
  - Symlink: ~/.claude/knowledgebase/
  - Global: ~/CLAUDE.md

From Windsurf:
  - Symlink: ~/.windsurf/knowledgebase/
  - Global: ~/.windsurf/CLAUDE.md
  - MCP: Can read any file via filesystem server

From Cursor:
  - Symlink: ~/.cursor/knowledgebase/
  - Global: ~/.cursor/CLAUDE.md
  - MCP: Can read any file via filesystem server
```

---

## MCP Server Configuration (Shared)

### Unity MCP (Port 6400)
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "python",
      "args": ["-m", "unity_mcp"],
      "env": {
        "UNITY_MCP_PORT": "6400"
      }
    }
  }
}
```

### Filesystem MCP (Knowledgebase Access)
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem",
        "/Users/jamestunick",
        "/Users/jamestunick/Documents/GitHub/Unity-XR-AI"
      ]
    }
  }
}
```

### Memory MCP (Persistent Context)
```json
{
  "mcpServers": {
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

### GitHub MCP (Repository Access)
```json
{
  "mcpServers": {
    "github": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github"],
      "env": {
        "GITHUB_PERSONAL_ACCESS_TOKEN": "<token>"
      }
    }
  }
}
```

---

## Self-Improving Memory Architecture

### Knowledge Accumulation Strategy
```yaml
Phase 1: Collection
  - Every AI session logs discoveries to ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
  - Automatic categorization by topic
  - Deduplicate before adding
  - Version control via Git

Phase 2: Organization
  - Weekly consolidation of new learnings
  - Update master indexes
  - Cross-reference related topics
  - Archive outdated information

Phase 3: Optimization
  - Monthly review of token usage
  - Compress verbose sections
  - Extract reusable patterns
  - Create quick reference cards

Phase 4: Distribution
  - Sync knowledgebase across all tools via symlinks
  - Update tool-specific configs
  - Push to GitHub for backup
  - Share common patterns across projects
```

### Continuous Learning Loop
```
User Task → AI Tool → Discovery → Knowledgebase Update → All Tools Benefit
    ↑                                                              ↓
    └──────────────────── Improved Performance ←──────────────────┘
```

---

## Token Optimization Strategy

### Loading Strategy (On-Demand)
```yaml
Default Session (<10K tokens):
  - Load: Global CLAUDE.md (~3K tokens)
  - Load: Project-specific rules (~2K tokens)
  - Total: ~5K tokens baseline

Unity Task (+5-10K tokens):
  - Load: Unity quick reference
  - Load: Relevant GitHub repos section
  - Total: ~15K tokens

Web Development (+5-10K tokens):
  - Load: WebGL/Three.js guide
  - Load: Performance patterns
  - Total: ~15K tokens

Complex Architecture (+10-20K tokens):
  - Load: Comprehensive guides
  - Load: Multiple knowledgebase sections
  - Total: ~30K tokens max
```

### Best Practices
```yaml
✅ DO:
  - Load only relevant sections per task
  - Use symlinks to avoid duplication
  - Compress historical data
  - Use images for visual info
  - Reference by link, not full text

❌ DON'T:
  - Load entire knowledgebase at once
  - Duplicate content across files
  - Include verbose examples
  - Store binary data in markdown
  - Repeat official documentation
```

---

## Cross-Tool Memory Sharing

### MCP Memory Server (Shared Graph)
```yaml
Purpose: Persistent knowledge graph across all tools

Entities:
  - Projects: Portals_6, Paint-AR, Open Brush
  - Technologies: Unity, WebGL, AR Foundation, VFX Graph
  - Patterns: Object pooling, Addressables, Instancing
  - People: Keijiro, Dilmerv, Unity-Technologies

Relations:
  - "Portals_6" uses "AR Foundation 6.2.1"
  - "Keijiro" created "SplatVFX"
  - "Object pooling" optimizes "Quest 2 performance"

Observations:
  - "Quest 2 runs best at <100k particles"
  - "Addressables reduce initial load by 80%"
  - "VFX Graph GPU events are 10x faster than CPU"

Access:
  - Claude Code: Via MCP memory server
  - Windsurf: Via MCP memory server
  - Cursor: Via MCP memory server
  - All tools share same knowledge graph!
```

### Filesystem-Based Sharing (Fallback)
```yaml
Shared Logs:
  - ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md
  - Append-only, timestamped entries
  - All tools can read/write

Format:
  - Timestamp: 2025-01-07 22:30
  - Tool: Claude Code
  - Discovery: Burst jobs are 50x faster than MonoBehaviour
  - Context: Portals_6 particle optimization
  - References: _PERFORMANCE_PATTERNS_REFERENCE.md
```

---

## Implementation Checklist

### Phase 1: Setup (Today)
- [x] Map all AI tool locations
- [ ] Create symlinks to knowledgebase
- [ ] Copy global rules to each tool
- [ ] Configure MCP servers consistently
- [ ] Test access from each tool

### Phase 2: Integration (This Week)
- [ ] Setup MCP memory server for all tools
- [ ] Create learning log workflow
- [ ] Document discovery process
- [ ] Test cross-tool context sharing

### Phase 3: Optimization (Ongoing)
- [ ] Monitor token usage across tools
- [ ] Consolidate duplicate learnings
- [ ] Refactor verbose sections
- [ ] Benchmark performance improvements

### Phase 4: Automation (Future)
- [ ] Auto-sync knowledgebase to GitHub
- [ ] Auto-categorize new discoveries
- [ ] Auto-compress old content
- [ ] Auto-generate quick references

---

## Tool-Specific Configuration Snippets

### Claude Code Settings (~/.claude/settings.json)
```json
{
  "knowledgebase": {
    "path": "/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase",
    "globalRules": "/Users/jamestunick/CLAUDE.md",
    "autoLoad": ["_MASTER_KNOWLEDGEBASE_INDEX.md"]
  },
  "mcp": {
    "enableMemory": true,
    "enableFilesystem": true,
    "enableGithub": true
  }
}
```

### Windsurf MCP Config (~/.windsurf/mcp.json)
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem",
        "/Users/jamestunick/Documents/GitHub/Unity-XR-AI"
      ]
    },
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

### Cursor MCP Config (~/.cursor/mcp.json)
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem",
        "/Users/jamestunick/Documents/GitHub/Unity-XR-AI"
      ]
    },
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

---

## Quick Reference Commands

### Setup Symlinks
```bash
# Run this once to establish shared access
KB=~/Documents/GitHub/Unity-XR-AI/KnowledgeBase
ln -sf $KB ~/.claude/knowledgebase
ln -sf $KB ~/.windsurf/knowledgebase
ln -sf $KB ~/.cursor/knowledgebase
ln -sf ~/CLAUDE.md ~/.windsurf/CLAUDE.md
ln -sf ~/CLAUDE.md ~/.cursor/CLAUDE.md
```

### Verify Access
```bash
# Check symlinks exist
ls -la ~/.claude/knowledgebase
ls -la ~/.windsurf/knowledgebase
ls -la ~/.cursor/knowledgebase

# Check MCP servers running
claude mcp list
```

### Update Global Rules
```bash
# Edit global rules (syncs to all tools)
code ~/CLAUDE.md

# Verify symlinks updated
cat ~/.claude/CLAUDE.md
cat ~/.windsurf/CLAUDE.md
cat ~/.cursor/CLAUDE.md
```

---

## Success Metrics

### Efficiency Gains
```yaml
Before Unified KB:
  - 3 separate rule files
  - Duplicate documentation
  - Inconsistent context
  - Manual sync required
  - 30-50K tokens per tool

After Unified KB:
  - 1 global rule file
  - Zero duplication
  - Shared context/memory
  - Auto-sync via symlinks
  - 5-15K tokens per tool (70% reduction)
```

### Intelligence Amplification
```yaml
Learning Velocity:
  - Discovery in one tool → Available to all tools instantly
  - Cross-project patterns emerge automatically
  - Continuous knowledge accumulation
  - Zero manual knowledge transfer

Context Quality:
  - 530+ GitHub repos accessible
  - Comprehensive Unity/WebGL guides
  - Performance patterns library
  - Shared memory graph across tools
```

---

**Remember**: One knowledgebase, infinite tools. Learn once, benefit everywhere.

**Goal**: World-class expert-level AI development across all platforms, zero duplication, maximum efficiency.
