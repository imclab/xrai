# KB Tools Reference

**Quick access to knowledgebase management tools**

---

## Overview

Two tools for effortless KB management:
- **`kb-add`** - Add entries (manual & automatic)
- **`kb-audit`** - Health check & recommendations

---

## kb-add - Easy Additions

### Quick Usage

```bash
# Patterns (reusable solutions)
kb-add --pattern "Use symlinks for cross-tool KB access"

# Insights (mental models)
kb-add --insight "Token optimization: separate philosophy from protocols"

# Anti-patterns (what NOT to do)
kb-add --antipattern "Don't inline large examples in core directives"

# Automation opportunities
kb-add --automation "Auto-extract patterns from git commits daily"

# Quick daily note
kb-add --quick "Quest 2 needs 90 FPS for smooth hand tracking"

# Auto-extract from git
kb-add --auto-git

# Create new KB file
kb-add --file unity-xr-tips.md -i
```

### Aliases (add to .bashrc/.zshrc)

```bash
# Source aliases
source ~/.local/bin/kb-aliases.sh

# Then use shortcuts:
kb-p "Your pattern"              # --pattern
kb-i "Your insight"              # --insight
kb-a "Your anti-pattern"         # --antipattern
kb-quick "Quick note"            # --quick
kb-auto                          # --auto-git
```

### Interactive Mode

```bash
kb-add -i
# Guides you through:
# 1. Select type (Pattern/Insight/Anti-pattern/Automation)
# 2. Enter content (multi-line supported)
# 3. Category, tags, priority
```

### All Options

```bash
kb-add [OPTIONS]

MODES:
  --pattern "text"        Add pattern to LEARNING_LOG.md
  --insight "text"        Add insight
  --antipattern "text"    Add anti-pattern
  --automation "text"     Add automation opportunity
  --file FILE             Create/edit KB file
  --quick "text"          Append to daily notes
  --auto-git              Extract from recent commits
  --auto-session          Extract from AI session

OPTIONS:
  --category CAT          Category (default: auto-detect)
  --tags "tag1,tag2"      Add tags
  --priority high|med|low Set priority (default: med)
  -i, --interactive       Interactive mode
  -h, --help              Show help
```

---

## kb-audit - Health Check

### Quick Usage

```bash
# Quick audit (5 seconds)
kb-audit --quick

# Full audit with recommendations (15 seconds)
kb-audit --full

# Security check only
kb-audit --security

# Metrics only
kb-audit --metrics

# Health score only
kb-audit --health
```

### Output Example

```
━━━ KB Quick Audit ━━━

Metrics:
  Files: 37 total, 37 this week
  Size: 1.05MB
  Learning entries: 9
  Git commits: 3

Health Score: 100/100 (Excellent)
```

### Health Score Ranges

- **90-100**: Excellent ⭐ - KB is thriving
- **70-89**: Good ✓ - Minor improvements possible
- **50-69**: Needs Improvement ⚠️ - Address recommendations
- **<50**: Critical Issues ❌ - Immediate action required

### Full Audit Sections

1. **Metrics** - File counts, size, growth rate
2. **Security** - Sensitive data scan, permissions, git status
3. **Health Score** - Overall KB health rating
4. **Recommendations** - Prioritized improvement suggestions
5. **File Analysis** - Recent additions, largest files

### Aliases

```bash
kb-check     # kb-audit --quick
kb-full      # kb-audit --full
kb-sec       # kb-audit --security
```

### All Options

```bash
kb-audit [OPTIONS]

MODES:
  --quick           Quick audit (5 sec)
  --full            Full audit with recommendations (15 sec)
  --security        Security-focused audit
  --metrics         Metrics & growth analysis only
  --health          Health score only

OPTIONS:
  --json            Output as JSON
  --report FILE     Save report to file
  --fix             Auto-fix issues (where possible)
  --threshold N     Health score threshold (default: 70)
  -h, --help        Show help
```

---

## Additional Aliases

### Navigation

```bash
kb-cd        # cd to KB directory
kb-log       # Edit LEARNING_LOG.md
```

### Git Shortcuts

```bash
kb-commit    # Add all, commit with date, push
kb-sync      # Pull, add all, commit, push
```

---

## Workflows

### Daily Pattern Extraction (30 seconds)

```bash
# 1. Quick health check
kb-check

# 2. Extract from git
kb-auto

# 3. Add any manual insights
kb-i "Discovered X while working on Y"
```

### Weekly Full Audit (5 minutes)

```bash
# 1. Full audit with recommendations
kb-full --report ~/Desktop/kb-audit-$(date +%Y%m%d).md

# 2. Review recommendations

# 3. Address high-priority items

# 4. Commit changes
kb-commit
```

### Post-Session Reflection (2 minutes)

```bash
# Add key learnings
kb-p "Pattern discovered during session"
kb-i "Insight about system behavior"

# Quick check
kb-check
```

### Security Audit (Monthly)

```bash
# Run security scan
kb-sec

# Review any warnings

# Rotate sensitive data if needed
```

---

## Integration with AI Tools

### Claude Code

```markdown
# In conversation:
"Extract patterns from this session and add to KB"

# Claude will use:
kb-add --pattern "Pattern extracted from session"
```

### Automation

```bash
# Add to crontab for daily auto-extraction
0 18 * * * /Users/jamestunick/.local/bin/kb-add --auto-git >> ~/.claude/logs/kb-auto.log 2>&1

# Daily health check
0 9 * * * /Users/jamestunick/.local/bin/kb-audit --quick >> ~/.claude/logs/kb-health.log 2>&1

# Weekly full audit
0 9 * * 0 /Users/jamestunick/.local/bin/kb-audit --full --report ~/kb-audit-$(date +\%Y\%m\%d).md
```

---

## Troubleshooting

### KB not found

```bash
# Set KB_DIR environment variable
export KB_DIR="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase"

# Or add to ~/.bashrc / ~/.zshrc
echo 'export KB_DIR="$HOME/Documents/GitHub/Unity-XR-AI/KnowledgeBase"' >> ~/.zshrc
```

### Commands not found

```bash
# Ensure ~/.local/bin is in PATH
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
source ~/.zshrc
```

### Permission denied

```bash
# Make scripts executable
chmod +x ~/.local/bin/kb-add
chmod +x ~/.local/bin/kb-audit
```

---

## Examples

### Example 1: Pattern Discovery

```bash
# Discovered a useful pattern while coding
kb-p "Symlink knowledgebases for cross-tool compatibility" "Architecture" "symlinks,tools" "high"

# Result: Added to LEARNING_LOG.md with timestamp, category, tags, priority
```

### Example 2: Daily Notes

```bash
# Quick notes throughout the day
kb-quick "Quest 2 tracking works best at 90 FPS"
kb-quick "Unity 6 AR Foundation 6.2 has breaking changes"
kb-quick "React Native bridge requires 'ready' handshake"

# Result: Timestamped entries in daily-notes-2026-01-08.md
```

### Example 3: Full Audit Report

```bash
kb-full --report ~/Desktop/audit.md

# Result: Comprehensive report saved to file
# - 37 files, 1.05MB total
# - Health score: 100/100 (Excellent)
# - 0 security issues
# - 2 recommendations (both low priority)
```

### Example 4: Auto-Extract from Git

```bash
cd ~/my-project
git log --since="1 day ago"  # See recent commits

kb-auto  # Extract patterns from commits

# Result:
# ✓ Found: Token optimization reduced overhead by 26%
# ✓ Added pattern to LEARNING_LOG.md
```

---

## File Locations

- **Tools**: `~/.local/bin/kb-add`, `~/.local/bin/kb-audit`
- **Aliases**: `~/.local/bin/kb-aliases.sh`
- **KB Directory**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/`
- **Learning Log**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md`
- **Daily Notes**: `~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/daily-notes-YYYY-MM-DD.md`

---

## Quick Start

```bash
# 1. Install (if not already)
curl -fsSL https://raw.githubusercontent.com/imclab/xrai/main/install.sh | bash

# 2. Load aliases
source ~/.local/bin/kb-aliases.sh

# 3. Test
kb-check

# 4. Add first entry
kb-p "Started using KB tools!"

# 5. See it worked
kb-log
```

---

**Pro tip**: Run `kb-check` at the start of each day and `kb-full` weekly to maintain KB health.

**Related**:
- [Installation Guide](../README.md#quick-start)
- [Full Documentation](../README.md)
- [AI Agent Philosophy](_AI_AGENT_PHILOSOPHY.md)
