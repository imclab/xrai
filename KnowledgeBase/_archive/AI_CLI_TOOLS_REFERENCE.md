# AI CLI Coding Tools Reference Guide

**Last Updated**: 2026-01-08
**Purpose**: Comprehensive reference for AI-powered coding assistants and CLIs

---

## Quick Comparison

| Tool | Developer | Key Model | Memory System | Agent Features |
|------|-----------|-----------|---------------|----------------|
| **Claude Code** | Anthropic | Opus 4.5, Sonnet 4.5 | CLAUDE.md files | Subagents, Plan Mode |
| **Windsurf** | Codeium | SWE-1.5, Cascade | Memories + Rules | Write/Chat/Turbo modes |
| **Codex CLI** | OpenAI | GPT-5.2-Codex | AGENTS.md, Skills | MCP, Web Search |
| **Gemini CLI** | Google | Gemini 2.5 Pro | GEMINI.md | Agent Mode, ReAct loop |

---

## 1. Claude Code (Anthropic)

**Documentation**: https://code.claude.com/docs/en/

### Model Configuration

| Alias | Model | Use Case |
|-------|-------|----------|
| `default` | Varies by account | General use |
| `sonnet` | Sonnet 4.5 | Daily coding tasks |
| `opus` | Opus 4.5 | Complex reasoning |
| `haiku` | Haiku | Simple/fast tasks |
| `opusplan` | Opus (plan) + Sonnet (execute) | Hybrid approach |
| `sonnet[1m]` | Sonnet with 1M context | Long sessions |

**Set model**: `/model <alias>` or `claude --model <alias>`

### Extended Thinking

- **Toggle**: `Option+T` (macOS) / `Alt+T` (Windows/Linux)
- **Keyword**: Use `ultrathink` in prompt for deep reasoning
- **Budget**: Up to 31,999 tokens for internal reasoning
- **Verbose mode**: `Ctrl+O` to see thinking process

### Memory System

| Type | Location | Scope |
|------|----------|-------|
| Enterprise | `/Library/Application Support/ClaudeCode/CLAUDE.md` | All users |
| Project | `./CLAUDE.md` or `./.claude/CLAUDE.md` | Team shared |
| Project Rules | `./.claude/rules/*.md` | Modular instructions |
| User | `~/.claude/CLAUDE.md` | Personal, all projects |
| Local | `./CLAUDE.local.md` | Personal, this project |

**Imports**: Use `@path/to/file` syntax in CLAUDE.md files

### Settings Scopes

1. **Managed** (highest) - IT-deployed, can't override
2. **Command line** - Temporary session overrides
3. **Local** - `.claude/settings.local.json`
4. **Project** - `.claude/settings.json`
5. **User** (lowest) - `~/.claude/settings.json`

### Key Settings

```json
{
  "permissions": {
    "allow": ["Bash(npm run lint)", "Read(~/.zshrc)"],
    "deny": ["Bash(curl:*)", "Read(./.env)"]
  },
  "model": "opus",
  "env": { "KEY": "value" },
  "hooks": { "PreToolUse": { "Bash": "echo 'Running...'" } }
}
```

### Permission Modes

- **Normal**: Ask for confirmations
- **Auto-accept**: `Shift+Tab` to toggle accept edits
- **Plan Mode**: `--permission-mode plan` - Read-only analysis
- **Bypass**: `--dangerously-skip-permissions` (dangerous)

### Plan Mode

- Analyze codebase without making changes
- Perfect for: Multi-step implementations, code exploration, architecture review
- Start: `claude --permission-mode plan` or `Shift+Tab` twice

### Custom Status Line

```json
{
  "statusLine": {
    "type": "command",
    "command": "~/.claude/statusline.sh"
  }
}
```

Receives JSON via stdin with model, workspace, cost, context_window info.

### Session Management

- **Continue**: `claude --continue` - Resume most recent
- **Resume**: `claude --resume` or `/resume` - Pick session
- **Name sessions**: For easier retrieval

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+C` | Cancel current operation |
| `Shift+Tab` | Cycle permission modes |
| `Option+T` | Toggle extended thinking |
| `Ctrl+O` | Toggle verbose mode |
| `\` + Enter | Newline in input |

### Environment Variables

| Variable | Purpose |
|----------|---------|
| `ANTHROPIC_MODEL` | Default model |
| `MAX_THINKING_TOKENS` | Custom thinking budget |
| `ANTHROPIC_DEFAULT_OPUS_MODEL` | Override opus alias |
| `ANTHROPIC_DEFAULT_SONNET_MODEL` | Override sonnet alias |
| `CLAUDE_CODE_SUBAGENT_MODEL` | Subagent model |
| `DISABLE_PROMPT_CACHING` | Disable caching |

---

## 2. Windsurf / Cascade (Codeium)

**Documentation**: https://docs.windsurf.com/windsurf/cascade/cascade

### Cascade Modes

| Mode | Description |
|------|-------------|
| **Write** | Direct code modifications, file creation, debugging |
| **Chat** | Contextual help without code changes |
| **Turbo** | Fully autonomous task execution |

### Core Features

- **Flow Awareness**: Tracks edits, commands, clipboard, terminal in real-time
- **Planning Agent**: Background planning for complex tasks with Todo lists
- **Codemaps**: Hierarchical codebase visualization
- **MCP Support**: GitHub, Slack, Stripe, Figma integrations

### Model Options

| Model | Speed | Best For |
|-------|-------|----------|
| **SWE-1.5** | 950 tokens/sec (14x Sonnet) | Fast coding |
| **Opus 4.5** | Standard | Complex reasoning |
| **GPT-5.2** | Standard | General tasks |

### Memory System

1. **User Memories (Rules)**: Explicitly defined preferences
2. **Auto-generated Memories**: Created by Cascade from interactions

### Recent Features (2026)

- Parallel multi-agent sessions (Wave 13)
- Git worktrees support
- Side-by-side Cascade panes
- Dedicated terminal profile
- Mermaid diagram rendering
- Image understanding (SWE-1.5)
- Cascade Hooks

### Tool Capabilities

- Search, Analyze, Web Search, MCP, Terminal
- Auto-detects and installs packages
- Up to 20 tool calls per prompt

---

## 3. OpenAI Codex CLI

**Documentation**: https://developers.openai.com/codex/cli
**GitHub**: https://github.com/openai/codex

### Installation

```bash
npm i -g @openai/codex
# or
brew install --cask codex
```

### Key Features

- Built in Rust for speed
- MCP server support
- Web search integration
- Agent skills system
- Code review before commits

### GPT-5.2-Codex Model

- Optimized for agentic coding
- Context compaction for long-horizon work
- Better large code changes (refactors, migrations)
- Improved Windows support
- Default for ChatGPT users (Jan 2026)

### Agent Skills

Reusable instruction bundles for specific tasks:

```bash
# Invoke explicitly
$skill-installer
$create-plan

# Or let Codex auto-select based on prompt
```

### Configuration

```toml
# ~/.codex/config.toml

[model]
default = "gpt-5.2-codex"

[analytics]
enabled = true
```

### CLI Commands

```bash
codex                    # Start interactive session
codex -p "prompt"        # Run single prompt
codex exec resume        # Resume previous task
```

### MCP Integration

```bash
# Expose CLI as MCP server
codex mcp-serve

# Use with Agents SDK for complex pipelines
```

### Recent Updates (Jan 2026)

- Thread/rollback for IDE undo
- `web_search_cached` for safer cached results
- Global exec flags after `codex exec resume`
- Version-targeted announcement tips
- Reduced CPU usage during streaming

---

## 4. Google Gemini CLI

**Documentation**: https://developers.google.com/gemini-code-assist/docs/gemini-cli
**GitHub**: https://github.com/google-gemini/gemini-cli

### Installation

```bash
npm install -g @anthropic-ai/gemini-cli
# or download from releases
```

### Free Tier Quotas

- **60 requests/minute**
- **1,000 requests/day**
- Access to **Gemini 2.5 Pro**
- **1M token context window**

### ReAct Loop

Reason and Act loop for complex tasks:
1. Analyze situation
2. Plan approach
3. Execute with tools
4. Evaluate results
5. Iterate

### Built-in Tools

- Google Search grounding
- File operations (read/write)
- Shell commands
- Web fetching
- MCP support for custom tools

### Memory System (GEMINI.md)

Similar to CLAUDE.md - persistent context files for Gemini CLI.

### Agent Mode in IDEs

| IDE | Agent Mode |
|-----|------------|
| VS Code | Powered by Gemini CLI |
| IntelliJ | Native (not CLI-based) |

### Agent Mode Capabilities

- Context-aware code generation
- Multi-step task execution
- Plan editing and approval
- Built-in tool access (file, terminal, search)

### Configuration

```yaml
# Control tool access
coreTools:
  - file_read
  - file_write
  - terminal
excludeTools:
  - shell_dangerous
```

### Recent Features (2026)

- Inline diff editing
- Persistent agent mode in chat history
- Real-time shell output
- Batched tool call approvals
- Gemini 3 coming soon

---

## Cross-Tool Patterns

### Memory/Context Files

| Tool | File | Purpose |
|------|------|---------|
| Claude | CLAUDE.md | Project instructions |
| Codex | AGENTS.md | Agent instructions |
| Gemini | GEMINI.md | Context files |
| Windsurf | Memories | Auto-generated context |

### MCP (Model Context Protocol)

All major tools now support MCP for:
- Custom tool integration
- Third-party API connections
- Extended capabilities

### Plan Mode Comparison

| Tool | Plan Feature |
|------|-------------|
| Claude | `--permission-mode plan` |
| Windsurf | Planning agent + Todo lists |
| Codex | `$create-plan` skill |
| Gemini | Agent mode planning |

### Best Practices (All Tools)

1. **Use memory files** - Persist instructions across sessions
2. **Leverage MCP** - Extend with custom tools
3. **Plan first** - Use planning modes for complex tasks
4. **Check context** - Monitor token usage
5. **Update regularly** - Keep tools current for latest features

---

## Sources

### Claude Code
- [Common Workflows](https://code.claude.com/docs/en/common-workflows)
- [Settings](https://code.claude.com/docs/en/settings)
- [Terminal Config](https://code.claude.com/docs/en/terminal-config)
- [Model Config](https://code.claude.com/docs/en/model-config)
- [Memory](https://code.claude.com/docs/en/memory)
- [Status Line](https://code.claude.com/docs/en/statusline)

### Windsurf
- [Cascade Docs](https://docs.windsurf.com/windsurf/cascade/cascade)
- [Cascade Overview](https://windsurf.com/cascade)
- [Changelog](https://windsurf.com/changelog)

### OpenAI Codex
- [CLI Documentation](https://developers.openai.com/codex/cli)
- [CLI Features](https://developers.openai.com/codex/cli/features/)
- [Changelog](https://developers.openai.com/codex/changelog)
- [Agents SDK](https://developers.openai.com/codex/guides/agents-sdk/)
- [GitHub](https://github.com/openai/codex)

### Gemini
- [Gemini CLI](https://developers.google.com/gemini-code-assist/docs/gemini-cli)
- [Agent Mode](https://developers.google.com/gemini-code-assist/docs/agent-mode)
- [GitHub](https://github.com/google-gemini/gemini-cli)
