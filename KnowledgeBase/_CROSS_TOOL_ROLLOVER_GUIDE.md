# Cross-Tool Rollover Guide

**Tags**: `claude-code` `gemini` `codex` `migration` `token-efficiency`
**Updated**: 2026-01-21
**Purpose**: Seamlessly switch between AI tools when token limits are reached

---

## Quick Rollover (30 Seconds)

When Claude Code hits token limits, switch to Gemini or Codex with full context:

```bash
# Option 1: Gemini (FREE, 1M context)
gemini

# Option 2: Codex CLI (128K context)
codex

# Both tools will find:
# - GLOBAL_RULES.md (universal rules)
# - KnowledgeBase/ (symlinked)
# - Project CLAUDE.md (project context)
```

---

## Architecture: Files Are Memory

**Critical Insight**: All AI tools share the same filesystem. Your knowledge is in files, not tool-specific storage.

```
~/                                    # Home directory
├── GLOBAL_RULES.md                   # Universal rules (ALL tools read this)
├── CLAUDE.md                         # Pointer to GLOBAL_RULES.md
│
├── ~/.claude/                        # Claude Code specific
│   ├── CLAUDE.md                     # Claude-specific pointers
│   ├── settings.json                 # MCP config, permissions
│   └── knowledgebase/                # → Unity-XR-AI/KnowledgeBase/ (symlink)
│
├── ~/.gemini/                        # Gemini specific (create if needed)
│   └── config                        # API key, preferences
│
├── ~/.codex/                         # Codex specific (create if needed)
│   └── config                        # API key, preferences
│
└── ~/Documents/GitHub/Unity-XR-AI/   # Shared knowledge (primary source)
    ├── KnowledgeBase/                # 116 markdown files
    ├── CLAUDE.md                     # Project context
    └── specs/                        # Spec-Kit specifications
```

---

## Tool-Specific Setup

### Gemini CLI Setup

```bash
# Install
npm install -g @anthropic-ai/gemini-cli

# Configure API key
export GOOGLE_AI_API_KEY="your-key"

# Create config pointing to shared knowledge
mkdir -p ~/.gemini
cat > ~/.gemini/system_prompt.md << 'EOF'
# Gemini System Prompt

Read these files for context:
- ~/GLOBAL_RULES.md - Universal rules
- ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md - Project context
- ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/ - Knowledge base

Key commands:
- Session start: Check _QUICK_FIX.md for common fixes
- Before coding: Search KnowledgeBase/ first
- Discovery: Append to LEARNING_LOG.md
EOF
```

### Codex CLI Setup

```bash
# Install
npm install -g @openai/codex-cli

# Configure API key
export OPENAI_API_KEY="your-key"

# Codex uses .claude/ files automatically
# Just ensure symlinks exist
```

### Symlink Setup (One-Time)

```bash
# Ensure all tools can access the knowledge base
ln -sf ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase ~/.claude/knowledgebase
ln -sf ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase ~/.gemini/knowledgebase 2>/dev/null
ln -sf ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase ~/.codex/knowledgebase 2>/dev/null

# Verify
ls -la ~/.claude/knowledgebase
```

---

## Rollover Scenarios

### Scenario 1: Claude Code Token Limit Reached

```
Claude Code: "Token limit reached"
→ Open terminal
→ Run: gemini (or codex)
→ Paste: "Continue from Claude Code session. Context: [brief summary]"
→ Reference: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/
```

### Scenario 2: Mid-Task Rollover

Before switching, export context:

```bash
# In Claude Code before limit:
"Summarize current state for handoff to another AI tool"

# Claude outputs summary
# Copy summary

# In Gemini:
"Continuing work. Previous context:
[paste summary]

Read ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md for project context."
```

### Scenario 3: Parallel Work

Use multiple tools for different tasks:

```
Claude Code: Complex implementation (200K context)
Gemini: Research, large docs (1M context, FREE)
Codex: Quick fixes, refactors (128K context, fast)
```

---

## What Transfers vs What Doesn't

### Automatically Available (Files)

| Resource | Location | All Tools Access |
|----------|----------|------------------|
| GLOBAL_RULES.md | ~/GLOBAL_RULES.md | Yes |
| Project context | project/CLAUDE.md | Yes |
| Knowledge base | KnowledgeBase/ | Yes |
| Specs | specs/ | Yes |
| Learning log | LEARNING_LOG.md | Yes |
| Pattern files | _*.md | Yes |

### Requires Manual Transfer

| Resource | Transfer Method |
|----------|-----------------|
| Conversation history | Summary in new session |
| MCP connections | Tool-specific setup |
| In-progress edits | Git stash or describe |
| Claude-mem memories | Query & paste relevant |

### Tool-Specific (Not Transferable)

| Resource | Claude | Gemini | Codex |
|----------|--------|--------|-------|
| MCP servers | Yes | No | No |
| claude-mem | Yes | No | No |
| Extended thinking | Yes | Limited | No |
| Visual analysis | Yes | Yes | No |

---

## Context Preservation Patterns

### Pattern 1: Summary Checkpoint

Before hitting limits, ask Claude:

```
"Create a rollover checkpoint:
1. Current task state
2. Files modified
3. Key decisions made
4. Next steps
5. Relevant KB files to reference"
```

Save output to `ROLLOVER_CHECKPOINT.md` in project.

### Pattern 2: Git-Based Handoff

```bash
# Before switching
git stash -m "WIP: [task description]"
git log -1 --oneline > /tmp/last_commit.txt

# In new tool
"Check git stash list and /tmp/last_commit.txt for context"
```

### Pattern 3: Todo List Handoff

```bash
# Export current todos
cat project/TODO.md  # If using TodoWrite

# Or check git status
git status > /tmp/work_state.txt
```

---

## Token Limit Reference

| Tool | Context | Cost | Best For |
|------|---------|------|----------|
| Claude Code | 200K | $$$ | Complex code, planning |
| Gemini CLI | 1M | FREE | Research, large docs |
| Codex CLI | 128K | $$ | Refactors, quick fixes |
| Cursor | 100K | $$ | IDE integration |
| Windsurf | 100K | $$ | Smooth UX |

### Recommended Workflow

```
1. Start with Gemini (FREE) for research/exploration
2. Use Claude Code for implementation
3. When limits approach, switch to Codex for finishing
4. Use Gemini again for documentation
```

---

## Unified Knowledge Access

All tools can access the same knowledge:

```bash
# From any tool's terminal:
grep -r "pattern" ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/

# Quick reference files
cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md
cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_PATTERN_TAGS.md
cat ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md
```

### For Gemini/Codex Initial Context

Copy-paste this context block:

```markdown
# Project Context

Read these files:
1. ~/GLOBAL_RULES.md - Universal development rules
2. ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md - Project overview
3. ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_QUICK_FIX.md - Error solutions
4. ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/_SIMPLIFIED_INTELLIGENCE_CORE.md - Core patterns

Key project: Unity-XR-AI (AR/VR development)
Unity project: MetavidoVFX-main
Knowledge base: 116 markdown files with patterns
```

---

## MCP Considerations

**Claude-Only Features**: Unity MCP, JetBrains MCP, claude-mem are Claude-specific.

**Workarounds for Gemini/Codex**:

| Claude MCP | Gemini/Codex Alternative |
|------------|--------------------------|
| Unity read_console | Check Unity Editor manually |
| JetBrains search | Use grep/find in terminal |
| claude-mem | Reference LEARNING_LOG.md |
| GitHub MCP | Use gh CLI directly |

```bash
# Example: Unity console check without MCP
# (Manual but works in any tool)
cat ~/Library/Logs/Unity/Editor.log | grep -i error | tail -20
```

---

## Verification Checklist

After rollover, verify access:

```bash
# 1. Check global rules
cat ~/GLOBAL_RULES.md | head -20

# 2. Check project context
cat ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md | head -30

# 3. Check knowledge base
ls ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/*.md | wc -l
# Should show: 116

# 4. Check specs
ls ~/Documents/GitHub/Unity-XR-AI/specs/
```

---

## Emergency Rollover Script

Save this script for quick rollover:

```bash
#!/bin/bash
# ~/bin/ai-rollover.sh

echo "=== AI Tool Rollover ==="
echo ""
echo "Current state:"
git -C ~/Documents/GitHub/Unity-XR-AI status --short
echo ""
echo "Recent commits:"
git -C ~/Documents/GitHub/Unity-XR-AI log -3 --oneline
echo ""
echo "Key files:"
echo "- ~/GLOBAL_RULES.md"
echo "- ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md"
echo "- ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/"
echo ""
echo "Switch to:"
echo "  gemini    # FREE, 1M context"
echo "  codex     # 128K context"
echo ""
echo "Paste this in new tool:"
echo "\"Read ~/GLOBAL_RULES.md and ~/Documents/GitHub/Unity-XR-AI/CLAUDE.md for context\""
```

---

## See Also

- `GLOBAL_RULES.md` - Universal rules
- `_SIMPLIFIED_INTELLIGENCE_CORE.md` - Core patterns
- `_AI_CODING_BEST_PRACTICES.md` - Research-backed workflows
- `_TOKEN_EFFICIENCY_COMPLETE.md` - Token optimization
