# Unity-XR-AI Project Configuration Reference

**Last Updated**: 2026-01-13
**Purpose**: Central reference for all ignore files, configs, and scripts in this project

---

## Git Ignore Files

### Root `.gitignore`
**Path**: [`Unity-XR-AI/.gitignore`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/.gitignore)

```gitignore
# Unity project (selective inclusion)
MetavidoVFX-main/
!MetavidoVFX-main/Assets/H3M/...  # H3M files included

# Personal files - NEVER commit
*resume*, *.credentials*, *.secrets*

# Media files (large)
*.MOV, *.mov, *.MP4, *.mp4, *.wav

# Build artifacts
__pycache__/, node_modules/, dist/, *.min.js
```

### MetavidoVFX `.gitignore`
**Path**: [`MetavidoVFX-main/.gitignore`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main/.gitignore)

```gitignore
# Unity standard ignores
/Library, /Logs, /Recordings, /Temp, /UserSettings
.DS_Store, Thumbs.db, *.swp, *.mp4
ARFoundationRemoteInstaller
```

### KnowledgeBase `.gitignore`
**Path**: [`KnowledgeBase/.gitignore`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/.gitignore)

```gitignore
# Security & Privacy
*.key, *.pem, *.p12, **/secret*, **/credential*, .env*

# Personal Information
potential_jobs.md, **/personal/**, **/*_PERSONAL.*

# Sensitive Configs
.zshrc, .bashrc, .ssh/

# IDE & Build
.DS_Store, .vscode/, .idea/, node_modules/, dist/
```

---

## Claude Code Configuration

### Project Settings
**Path**: [`Unity-XR-AI/.claude/settings.local.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/.claude/settings.local.json)

```json
{
  "permissions": {
    "allow": [
      "Bash(claude mcp list:*)",
      "Bash(python3:*)",
      "Bash(lsof:*)",
      "Bash(cat:*)",
      "Bash(claude mcp add:*)",
      "mcp__ide__getDiagnostics",
      "WebFetch(domain:raw.githubusercontent.com)"
    ]
  }
}
```

---

## MCP Server

### Package Configuration
**Path**: [`mcp-server/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/mcp-server/package.json)

| Field | Value |
|-------|-------|
| Name | `unity-xr-kb-mcp-server` |
| Version | `1.0.0` |
| Type | ES Module |
| Node | `>=18.0.0` |
| MCP SDK | `^1.0.0` |

**Scripts**:
```bash
npm run build   # TypeScript compile
npm run dev     # Development with tsx
npm start       # Production server
npm test        # Jest tests
```

### Environment Example
**Path**: [`mcp-server/.env.example`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/mcp-server/.env.example)

---

## Shell Scripts

### Build & Deploy Scripts

| Script | Path | Purpose |
|--------|------|---------|
| **build_ios.sh** | [`build_ios.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/build_ios.sh) | iOS Unity build |
| **deploy_ios.sh** | [`deploy_ios.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/deploy_ios.sh) | Deploy to iOS device |
| **install.sh** | [`install.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/install.sh) | Project setup |

### MetavidoVFX Scripts

| Script | Path | Purpose |
|--------|------|---------|
| **build_and_deploy.sh** | [`MetavidoVFX-main/build_and_deploy.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main/build_and_deploy.sh) | Full build + deploy |
| **auto_build.sh** | [`MetavidoVFX-main/auto_build.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main/auto_build.sh) | Automated build |
| **debug.sh** | [`MetavidoVFX-main/debug.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/MetavidoVFX-main/debug.sh) | Debug utilities |

### KnowledgeBase Maintenance Scripts

| Script | Path | Purpose |
|--------|------|---------|
| **KB_AUDIT.sh** | [`KnowledgeBase/KB_AUDIT.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_AUDIT.sh) | Audit KB health |
| **KB_BACKUP.sh** | [`KnowledgeBase/KB_BACKUP.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_BACKUP.sh) | Backup KB |
| **KB_MAINTENANCE.sh** | [`KnowledgeBase/KB_MAINTENANCE.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_MAINTENANCE.sh) | Maintenance tasks |
| **KB_OPTIMIZE.sh** | [`KnowledgeBase/KB_OPTIMIZE.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_OPTIMIZE.sh) | Optimize KB |
| **KB_RESEARCH.sh** | [`KnowledgeBase/KB_RESEARCH.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/KB_RESEARCH.sh) | Research automation |
| **SETUP_VERIFICATION.sh** | [`KnowledgeBase/SETUP_VERIFICATION.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/SETUP_VERIFICATION.sh) | Verify setup |

### KnowledgeBase/scripts/

| Script | Path | Purpose |
|--------|------|---------|
| **generate-kb-index.sh** | [`scripts/generate-kb-index.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/generate-kb-index.sh) | Generate index |
| **auto_cross_link_configs.sh** | [`scripts/auto_cross_link_configs.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/auto_cross_link_configs.sh) | Auto cross-link |
| **KB_RESEARCH_AND_UPDATE.sh** | [`scripts/KB_RESEARCH_AND_UPDATE.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/scripts/KB_RESEARCH_AND_UPDATE.sh) | Research + update |

### AI Knowledge Base System Scripts

| Script | Path | Purpose |
|--------|------|---------|
| **install.sh** | [`AI_Knowledge_Base_System/install.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI_Knowledge_Base_System/install.sh) | Install system |
| **setup_knowledge_base.sh** | [`AI_Knowledge_Base_System/setup_knowledge_base.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI_Knowledge_Base_System/setup_knowledge_base.sh) | Setup KB |
| **quick_start.sh** | [`AI_Knowledge_Base_System/quick_start.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI_Knowledge_Base_System/quick_start.sh) | Quick start |
| **setup_gdrive.sh** | [`AI_Knowledge_Base_System/setup_gdrive.sh`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI_Knowledge_Base_System/setup_gdrive.sh) | Google Drive sync |

---

## YAML Configuration

### Global Rules
**Path**: [`AI_Knowledge_Base_System/global_rules.yaml`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI_Knowledge_Base_System/global_rules.yaml)

```yaml
claude_rules:
  knowledge_base:
    enabled: true
    auto_detect_links: true
    min_links_to_save: 3
    triggers:
      - pattern: "list.*links"
      - pattern: "github.*projects"
    save_formats: [markdown, json, html]
    storage:
      local_path: "~/Desktop/AI_Knowledge_Base/Claude"
      gdrive_path: "~/Google Drive/My Drive/AI_Knowledge_Base/Claude"

global_settings:
  sync_frequency: "weekly"
  backup_retention: "365 days"
  duplicate_detection: true
```

---

## JSON Data Files

| File | Path | Purpose |
|------|------|---------|
| **metrics.json** | [`KnowledgeBase/metrics.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/metrics.json) | KB metrics |
| **analysis-results.json** | [`Vis/HOLOVIS/analysis-results.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/HOLOVIS/analysis-results.json) | Analysis data |
| **sparse-projects.json** | [`Vis/HOLOVIS/sparse-projects.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/HOLOVIS/sparse-projects.json) | Project list |
| **jobs_data.json** | [`Vis/WarpDashboard/jobs_data.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/WarpDashboard/jobs_data.json) | Jobs data |

---

## Package.json Files (Node Projects)

| Project | Path | Description |
|---------|------|-------------|
| **mcp-server** | [`mcp-server/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/mcp-server/package.json) | MCP KB Server |
| **xrai-kg** | [`Vis/xrai-kg/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/xrai-kg/package.json) | Knowledge Graph |
| **cosmos-standalone-web** | [`Vis/cosmos-standalone-web/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/cosmos-standalone-web/package.json) | Cosmos Viz |
| **cosmos-visualizer** | [`Vis/cosmos-visualizer/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/cosmos-visualizer/package.json) | Cosmos Viz |
| **HOLOVIS** | [`Vis/HOLOVIS/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/HOLOVIS/package.json) | Holo Visualizer |
| **AI-XR-MCP-main** | [`KnowledgeBase/AI-XR-MCP-main/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI-XR-MCP-main/package.json) | AI XR MCP |
| **cosmos-needle-web** | [`Vis/cosmos-needle-web/package.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/Vis/cosmos-needle-web/package.json) | Needle Cosmos |

---

## TypeScript Configs

| Project | Path |
|---------|------|
| **mcp-server** | [`mcp-server/tsconfig.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/mcp-server/tsconfig.json) |
| **AI-XR-MCP-main** | [`AI-XR-MCP-main/tsconfig.json`](/Users/jamestunick/Documents/GitHub/Unity-XR-AI/KnowledgeBase/AI-XR-MCP-main/tsconfig.json) |

---

## Quick Reference

### File Counts by Type
| Type | Count | Location |
|------|-------|----------|
| `.gitignore` | 9 | Various |
| Shell scripts | 20+ | Root, MetavidoVFX, KnowledgeBase |
| `package.json` | 7 | Node projects |
| `.json` configs | 10+ | Various |
| `.yaml` configs | 1 | AI_Knowledge_Base_System |

### Key Directories
```
Unity-XR-AI/
├── .gitignore                    # Root ignore
├── .claude/settings.local.json   # Claude project settings
├── mcp-server/                   # MCP KB server
│   ├── package.json
│   ├── tsconfig.json
│   └── .env.example
├── MetavidoVFX-main/
│   ├── .gitignore                # Unity ignores
│   └── build_and_deploy.sh
├── KnowledgeBase/
│   ├── .gitignore                # Security ignores
│   ├── KB_*.sh                   # Maintenance scripts
│   ├── scripts/                  # Utility scripts
│   └── AI_Knowledge_Base_System/
│       └── global_rules.yaml
├── Vis/                          # 3D Visualization frontends
│   ├── xrai-kg/                  # ECharts knowledge graph (2026)
│   ├── HOLOVIS/                  # Three.js holographic viz (2025)
│   ├── cosmos-*/                 # Cosmos visualizers (2025)
│   ├── WarpDashboard/            # Jobs dashboard (2025)
│   └── chalktalk-master/         # Ken Perlin's Chalktalk (2018)
└── specs/                        # Spec-Kit specs
```

---

**Maintainer**: James Tunick
**Confidence**: 100% (all paths verified)
