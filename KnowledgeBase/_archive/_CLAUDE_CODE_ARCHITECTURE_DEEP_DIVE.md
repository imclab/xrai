# Claude Code Architecture: Comprehensive Deep Dive

> **Research Date**: 2026-01-21
> **Sources**: Official Anthropic docs, web research, local knowledgebase
> **Purpose**: Complete understanding of Claude Code's internal architecture

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Tool System](#2-tool-system)
3. [Agent/Subagent System](#3-agentsubagent-system)
4. [Context Management](#4-context-management)
5. [MCP Integration](#5-mcp-integration)
6. [Hooks System](#6-hooks-system)
7. [Memory/Persistence](#7-memorypersistence)
8. [Performance Optimizations](#8-performance-optimizations)
9. [Error Handling](#9-error-handling)
10. [Best Practices](#10-best-practices)

---

## 1. Architecture Overview

### Core Design Philosophy

**Claude Code is intentionally low-level and unopinionated**, providing close to raw model access without forcing specific workflows. This creates a flexible, customizable, scriptable, and safe power tool.

### Technology Stack

```yaml
Languages: TypeScript (90% of code written by Claude itself)
UI Framework: React + Ink (terminal UI)
Layout: Yoga (flexbox for terminal)
Runtime: Bun
Architecture: Single-threaded agent loop
Model Access: Claude API (Opus 4.5, Sonnet 4.5, Haiku)
```

### Agent Loop (The Heart of Claude Code)

At its core, Claude Code uses a **simple while(tool_use) loop**:

```
1. Model produces a message
2. If message includes tool call → execute tool
3. Feed results back to model
4. If no tool call → stop and wait for user input
5. Repeat
```

This is fundamentally different from competitors who chase multi-agent swarms and complex orchestration. **Anthropic built a single-threaded loop that does one thing obsessively well: think, act, observe, repeat.**

### Conversation Flow

The agent operates in a specific feedback loop:

```
Gather Context → Take Action → Verify Work → Repeat
```

Behind the scenes, **system reminders** inject the current TODO list state after tool uses, preventing the model from losing track of objectives in long conversations.

### Key Architectural Principles

1. **Stateful Application, Stateless Protocol**: The application maintains state, but communication can be optimized for stateless patterns
2. **Tool-First Design**: All capabilities exposed as tools, discoverable and composable
3. **Context Preservation**: Aggressive caching and intelligent compaction
4. **Permission Layering**: Fine-grained control over tool execution
5. **Extensibility**: Hooks, subagents, skills, and plugins for customization

---

## 2. Tool System

### Tool Discovery and Registration

Tools are registered at multiple levels:

```yaml
Built-in Tools:
  - Bash: Execute terminal commands
  - Read, Write, Edit: File operations
  - Glob, Grep: File search
  - WebSearch, WebFetch: Internet access
  - LSP: Language server protocol integration
  - Skill: Execute pre-defined workflows
  - TodoWrite: Task management

MCP Tools:
  - Pattern: mcp__<server>__<tool>
  - Example: mcp__github__create_pull_request
  - Discovery: MCPSearch tool loads MCP tools on-demand
  - Lazy Loading: Tools not loaded until discovered

Plugin Tools:
  - Custom tools from .claude/plugins/
  - Distributed via npm or git
  - Full access to Claude Code SDK
```

### Tool Selection Mechanism

**Pure LLM Reasoning** - Claude Code doesn't use embeddings, classifiers, or pattern matching to decide which tool to invoke.

The system:
1. Formats all available tools into text description
2. Embeds in system prompt
3. Lets Claude's language model make the decision

For scenarios with thousands of tools: **Tool Search Tool** discovers tools on-demand, so Claude only sees tools actually needed for current task.

### Tool Execution Flow

```
1. User Input
   ↓
2. Claude Generates Tool Call
   ↓
3. PreToolUse Hook Fires (can block/modify)
   ↓
4. Permission Check
   ↓
5. Tool Execution
   ↓
6. PostToolUse Hook Fires (can provide feedback)
   ↓
7. Result Fed Back to Model
   ↓
8. Loop Continues or Stops
```

### Advanced Tool Features (2026)

#### Programmatic Tool Calling

Claude can orchestrate tools through **code rather than individual API round-trips**. Claude writes code that calls multiple tools, processes outputs, and controls what information enters its context window.

#### Tool Use Examples

Provides a **universal standard for demonstrating how to effectively use a given tool**, improving tool call quality.

#### Tool Annotations (Hints)

MCP 2025-03-26+ supports tool annotations for client behavior hints:

| Annotation | Purpose |
|------------|---------|
| `readOnlyHint` | Tool only reads, doesn't modify |
| `idempotentHint` | Safe to retry with same arguments |
| `destructiveHint` | May overwrite/delete data |

Example applications:
- `read_text_file`: readOnlyHint=true
- `write_file`: idempotentHint=true, destructiveHint=true
- `edit_file`: destructiveHint=true (re-applying can double-apply)

### Parallel Tool Calls

Claude Code intelligently calls independent tools in parallel when possible:

```yaml
Pattern:
  - Collect tool calls as Promise tasks
  - Use Promise.all() to execute concurrently
  - Dramatically improves performance for batch operations

Example: "Read these 5 files" → All 5 Read calls execute in parallel

Performance Impact:
  - Sequential: 5 files × 100ms = 500ms
  - Parallel: max(100ms, 100ms, 100ms, 100ms, 100ms) = 100ms
  - 5x speedup
```

---

## 3. Agent/Subagent System

### Subagent Architecture

Subagents are **specialized AI assistants** that handle specific types of tasks within Claude Code. Each runs in its own context window with:

- Custom system prompt
- Specific tool access
- Independent permissions
- Separate context from main conversation

### Key Benefits

```yaml
Preserve Context: Keep exploration/implementation out of main conversation
Enforce Constraints: Limit which tools a subagent can use
Reuse Configurations: User-level subagents work across projects
Specialize Behavior: Focused prompts for specific domains
Control Costs: Route to faster/cheaper models like Haiku
```

### Built-in Subagents

| Subagent | Model | Tools | Purpose |
|----------|-------|-------|---------|
| **Explore** | Haiku | Read-only | Fast codebase search and analysis |
| **Plan** | Inherit | Read-only | Research for plan mode |
| **general-purpose** | Inherit | All tools | Complex multi-step tasks |
| **Bash** | Inherit | - | Terminal commands in separate context |
| **Claude Code Guide** | Haiku | - | Questions about Claude Code features |

### Explore Thoroughness Levels

- `quick`: Targeted lookups
- `medium`: Balanced exploration
- `very thorough`: Comprehensive analysis

### Custom Subagent Definition

```yaml
---
name: code-reviewer
description: Reviews code for quality and best practices
tools: Read, Glob, Grep
model: sonnet
permissionMode: default
---

You are a code reviewer. When invoked, analyze the code and provide
specific, actionable feedback on quality, security, and best practices.
```

### Frontmatter Fields

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Unique identifier (lowercase, hyphens) |
| `description` | Yes | When Claude should delegate to this subagent |
| `tools` | No | Allowed tools (inherits all if omitted) |
| `disallowedTools` | No | Tools to deny |
| `model` | No | `sonnet`, `opus`, `haiku`, or `inherit` |
| `permissionMode` | No | `default`, `acceptEdits`, `dontAsk`, `bypassPermissions`, `plan` |
| `skills` | No | Skills to load at startup |
| `hooks` | No | Lifecycle hooks for this subagent |

### Permission Modes

| Mode | Behavior |
|------|----------|
| `default` | Standard permission prompts |
| `acceptEdits` | Auto-accept file edits |
| `dontAsk` | Auto-deny prompts (allowed tools still work) |
| `bypassPermissions` | Skip all checks |
| `plan` | Read-only exploration mode |

### Foreground vs Background

| Mode | Behavior |
|------|----------|
| **Foreground** | Blocks main conversation, interactive prompts work |
| **Background** | Runs concurrently, inherits permissions, auto-denies missing perms |

**Disable background tasks**: `CLAUDE_CODE_DISABLE_BACKGROUND_TASKS=1`

### Subagent Depth Limitation

Sub-agents operate with **depth limitations** - they cannot spawn their own sub-agents, preventing recursive explosion.

### When to Use Subagents vs Main Conversation

**Use Main Conversation**:
- Frequent back-and-forth needed
- Multiple phases share context
- Quick, targeted changes
- Latency matters

**Use Subagents**:
- Task produces verbose output
- Need specific tool restrictions
- Work is self-contained
- Want isolated context

---

## 4. Context Management

### Context Window

Claude Code operates within a **~200K token context window**.

### Auto-Compact Trigger Points

**Critical Discovery (January 2026)**: Claude Code triggers automatic context compaction at approximately **78% context usage** even when `autoCompact: false` is explicitly set in settings.json (reported as bug #18264).

Official behavior:
- Auto-compact triggers when context reaches ~95% capacity (25% remaining)
- However, reports indicate triggering at 75-78% utilization
- This leaves 25% of context window (50K tokens in 200K window) free for reasoning

### Manual Compaction Strategy

**Recommended trigger point**: 70% context usage

Guidelines:
- **0-50%**: Work freely
- **50-70%**: Monitor, prepare
- **70-85%**: `/compact` immediately
- **85-95%**: Emergency `/compact`
- **95%+**: `/clear` required

### Compaction Process

When compaction occurs:

```yaml
1. Conversation History Analysis:
   - Identify key decisions
   - Extract important context
   - Preserve critical information

2. Summarization:
   - Claude generates summary of conversation
   - Key points preserved
   - Details compressed

3. Context Replacement:
   - Old messages replaced with summary
   - Recent messages kept verbatim
   - Context window freed

4. Continuity Maintained:
   - Main narrative thread preserved
   - Important decisions remembered
   - User can continue naturally
```

### Context Protection Strategies

**Recent improvements (December 2025)**: Four major features shipped to tackle problems including context compacting that interrupts focus.

### Prompt Caching

Claude Code automatically optimizes costs through **prompt caching**:

```yaml
Mechanism:
  - Static context (CLAUDE.md, system prompts) cached
  - Cache valid for 5 minutes
  - Subsequent requests reuse cache
  - Reduces API costs significantly

Benefits:
  - Faster responses
  - Lower costs
  - Consistent context
```

### PreCompact Hook

```json
{
  "hooks": {
    "PreCompact": [
      {
        "matcher": "manual",
        "hooks": [{
          "type": "command",
          "command": "./scripts/save-important-context.sh"
        }]
      }
    ]
  }
}
```

Matcher options: `manual` (user triggered) or `auto` (automatic)

### Context Recovery Pattern

If context loss occurs after compaction:

```yaml
Recovery Strategy:
  1. Check session transcript: ~/.claude/projects/{project}/{sessionId}/transcript.jsonl
  2. Extract key information from transcript
  3. Use /memory to add back critical context
  4. Resume work with recovered context
```

### /stats Command

New in recent updates: `/stats` command shows current context usage, allowing proactive compaction decisions.

---

## 5. MCP Integration

### MCP Architecture

The Model Context Protocol (MCP) is an **open standard enabling AI applications to connect with external data sources, tools, and services**.

### MCP Participants

```
┌─────────────────────────────────────────────────────────────┐
│                      MCP HOST                                │
│  (Claude Code, Claude Desktop, VS Code, etc.)               │
│                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ MCP Client  │  │ MCP Client  │  │ MCP Client  │         │
│  │     #1      │  │     #2      │  │     #3      │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
└─────────┼───────────────┼───────────────┼──────────────────┘
          │               │               │
          ▼               ▼               ▼
    ┌───────────┐   ┌───────────┐   ┌───────────┐
    │MCP Server │   │MCP Server │   │MCP Server │
    │(Filesystem)│   │ (Memory)  │   │ (GitHub)  │
    └───────────┘   └───────────┘   └───────────┘
```

- **MCP Host**: AI application coordinating MCP clients (Claude Code)
- **MCP Client**: Component maintaining connection to one MCP server
- **MCP Server**: Program providing context, tools, or resources

### Transport Mechanisms

#### Current (2025-2026)

**STDIO Transport** (Local):
- Standard input/output for local processes
- No network overhead
- Optimal performance
- Process-to-process communication

**Streamable HTTP Transport** (Remote):
- HTTP POST for client-to-server messages
- Server-Sent Events (SSE) for streaming
- Enables remote server communication
- Standard HTTP authentication (bearer tokens, API keys)

#### Future (June 2026 Release)

**Moving Towards Stateless Protocol**:

```yaml
Current State:
  - Stateful protocol
  - Persistent bidirectional channel
  - Handshake exchanges capabilities
  - Session state fixed throughout connection
  - Scaling requires sticky sessions or distributed storage

Future Vision:
  - Stateless protocol design
  - Stateful applications (not protocol)
  - Cookie-like mechanism for session state
  - Decoupled from transport layer
  - Easier horizontal scaling

Target:
  - SEPs finalized Q1 2026
  - Specification release June 2026
```

### MCP Core Primitives

#### 1. Tools

Executable functions AI can invoke:

```json
{
  "name": "get_weather",
  "description": "Get current weather for a location",
  "inputSchema": {
    "type": "object",
    "properties": {
      "location": { "type": "string" }
    },
    "required": ["location"]
  }
}
```

**Discovery**: `tools/list` → **Execution**: `tools/call`

#### 2. Resources

Data sources providing contextual information:

```json
{
  "uri": "file:///project/README.md",
  "name": "Project README",
  "mimeType": "text/markdown"
}
```

**Discovery**: `resources/list` → **Retrieval**: `resources/read`

#### 3. Prompts

Reusable templates for LLM interactions:

```json
{
  "name": "code_review",
  "description": "Template for reviewing code",
  "arguments": [
    { "name": "language", "required": true }
  ]
}
```

**Discovery**: `prompts/list` → **Retrieval**: `prompts/get`

### MCP in Claude Code

**Tool Discovery Pattern**:

```yaml
Phase 1: Tool Search
  - User requests functionality
  - Claude uses MCPSearch tool
  - Query: "select:mcp__github__create_pull_request"
  - Tool loaded into available tools

Phase 2: Tool Execution
  - Claude calls loaded MCP tool
  - MCP client routes to appropriate server
  - Server executes and returns result
  - Result fed back to Claude
```

**Critical Rule**: MCP tools **MUST** be loaded via MCPSearch before calling them directly.

### Official MCP Servers

| Server | Purpose | Key Tools |
|--------|---------|-----------|
| **filesystem** | File operations | read_file, write_file, edit_file, search_files, directory_tree |
| **memory** | Knowledge graph persistence | create_entities, create_relations, search_nodes, read_graph |
| **fetch** | Web content retrieval | fetch URL as markdown |
| **git** | Git operations | status, diff, commit, log |
| **time** | Time/timezone operations | get_current_time, convert_timezone |
| **sequentialthinking** | Step-by-step reasoning | structured thinking workflow |
| **everything** | Demo server | showcase of MCP capabilities |

### Configuration

**Claude Code** (`~/.claude/mcp.json` or project `.claude/mcp.json`):

```json
{
  "mcpServers": {
    "memory": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    },
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/allowed/path"]
    }
  }
}
```

### MCP Tool Naming Convention

Pattern: `mcp__<server>__<tool>`

Examples:
- `mcp__github__create_pull_request`
- `mcp__memory__create_entities`
- `mcp__filesystem__read_file`

---

## 6. Hooks System

### Overview

Hooks are **user-defined shell commands that execute at various points in Claude Code's lifecycle**. They provide **deterministic control** over behavior - ensuring actions always happen rather than relying on LLM choice.

### Hook Events

| Event | When it Fires | Matcher Support |
|-------|---------------|-----------------|
| `PreToolUse` | Before tool calls (can block) | Yes |
| `PermissionRequest` | When permission dialog shown | Yes |
| `PostToolUse` | After tool completes | Yes |
| `UserPromptSubmit` | When user submits prompt | No |
| `Notification` | When Claude sends notifications | Yes |
| `Stop` | When Claude finishes responding | No |
| `SubagentStop` | When subagent completes | No |
| `PreCompact` | Before compact operation | Yes (`manual`/`auto`) |
| `SessionStart` | Session starts/resumes | Yes (`startup`/`resume`/`clear`/`compact`) |
| `SessionEnd` | Session ends | No |

### Configuration Locations (by priority)

1. `.claude/settings.local.json` - Local project (not committed)
2. `.claude/settings.json` - Project settings
3. `~/.claude/settings.json` - User settings

### Hook Structure

```json
{
  "hooks": {
    "EventName": [
      {
        "matcher": "ToolPattern",
        "hooks": [
          {
            "type": "command",
            "command": "your-command-here",
            "timeout": 30
          }
        ]
      }
    ]
  }
}
```

### Matcher Patterns

- **Simple**: `Write` matches only Write tool
- **Regex**: `Edit|Write` matches both
- **Wildcard**: `*` or `""` matches all tools

### Exit Codes

| Code | Behavior |
|------|----------|
| 0 | Success - stdout shown in verbose mode |
| 2 | **Blocking error** - stderr fed back to Claude |
| Other | Non-blocking error - stderr shown to user |

### Exit Code 2 Behavior per Event

| Event | Exit 2 Behavior |
|-------|-----------------|
| `PreToolUse` | Blocks tool call, shows stderr to Claude |
| `PermissionRequest` | Denies permission |
| `PostToolUse` | Shows stderr to Claude (tool already ran) |
| `Stop` | Blocks stoppage, shows stderr to Claude |
| `UserPromptSubmit` | Blocks prompt, erases it |

### Hook Input (JSON via stdin)

**Common Fields**:

```json
{
  "session_id": "abc123",
  "transcript_path": "/path/to/transcript.jsonl",
  "cwd": "/current/working/directory",
  "permission_mode": "default",
  "hook_event_name": "PreToolUse"
}
```

**PreToolUse - Bash**:

```json
{
  "tool_name": "Bash",
  "tool_input": {
    "command": "npm test",
    "description": "Run tests",
    "timeout": 120000
  },
  "tool_use_id": "toolu_01ABC..."
}
```

### JSON Output (Advanced)

Hooks can return JSON for complex control:

```json
{
  "continue": true,
  "stopReason": "string",
  "suppressOutput": true,
  "systemMessage": "string",
  "hookSpecificOutput": {
    "hookEventName": "PreToolUse",
    "permissionDecision": "allow|deny|ask",
    "permissionDecisionReason": "Reason here",
    "updatedInput": { "field": "new value" }
  }
}
```

### Practical Examples

#### Log All Bash Commands

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [{
          "type": "command",
          "command": "jq -r '.tool_input.command' >> ~/.claude/bash-log.txt"
        }]
      }
    ]
  }
}
```

#### Auto-Format TypeScript

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [{
          "type": "command",
          "command": "jq -r '.tool_input.file_path' | { read f; [[ $f == *.ts ]] && npx prettier --write \"$f\"; }"
        }]
      }
    ]
  }
}
```

#### Block Sensitive Files

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [{
          "type": "command",
          "command": "python3 -c \"import json,sys; d=json.load(sys.stdin); p=d.get('tool_input',{}).get('file_path',''); sys.exit(2 if any(x in p for x in ['.env','.git/']) else 0)\""
        }]
      }
    ]
  }
}
```

### Prompt-Based Hooks

Use LLM (Haiku) for intelligent decisions:

```json
{
  "hooks": {
    "Stop": [{
      "hooks": [{
        "type": "prompt",
        "prompt": "Evaluate if Claude should stop: $ARGUMENTS. Check if all tasks are complete.",
        "timeout": 30
      }]
    }]
  }
}
```

LLM responds with: `{ "ok": true | false, "reason": "explanation" }`

### Hook Execution Details

- **Timeout**: 60 seconds default, configurable per command
- **Parallelization**: All matching hooks run in parallel
- **Deduplication**: Identical commands deduplicated
- **Environment**: Runs in cwd with Claude Code's environment

### Environment Variables

| Variable | Description |
|----------|-------------|
| `CLAUDE_PROJECT_DIR` | Absolute path to project root |
| `CLAUDE_ENV_FILE` | File for persisting env vars (SessionStart) |
| `CLAUDE_CODE_REMOTE` | `"true"` if remote/web environment |
| `CLAUDE_PLUGIN_ROOT` | Plugin directory (for plugin hooks) |

---

## 7. Memory/Persistence

### Native Claude Code Memory

#### CLAUDE.md Files

Claude Code has a built-in memory system that **recursively reads CLAUDE.md and CLAUDE.local.md files**:

```yaml
Behavior:
  - Automatically loaded into context when Claude Code launches
  - Content directly injected into model's prompt context
  - Every session starts with this context

Locations:
  1. ~/CLAUDE.md - Global user instructions
  2. ~/.claude/CLAUDE.md - User-level Claude Code specific
  3. <project>/CLAUDE.md - Project instructions (committed)
  4. <project>/.claude/CLAUDE.local.md - Project local (not committed)

Priority: More specific files override general ones
```

#### Instant Memory Addition

```bash
# Prefix any instruction with # to add to memory instantly
# Opens in system editor
/memory
```

### claude-mem Plugin

**Github**: https://github.com/thedotmack/claude-mem
**Docs**: https://docs.claude-mem.ai/

Claude-mem is a **Claude Code plugin that automatically captures everything Claude does during coding sessions**, compresses it with AI (using Claude's agent-sdk), and injects relevant context back into future sessions.

#### Key Features

```yaml
Persistent Memory:
  - Context survives across sessions
  - ChromaDB vector storage
  - MCP integration
  - Semantic search

Automatic Process:
  - Compress conversations
  - Load relevant context at startup
  - Enable semantic search
  - Zero manual intervention

Installation:
  /plugin marketplace add thedotmack/claude-mem
  /plugin install claude-mem
```

#### Architecture

```
Session 1:
  User: "How does auth work?"
  Claude: [explains, reads files]
  claude-mem: [captures context, creates embeddings]

Session 2 (next day):
  User: "Improve the auth flow"
  claude-mem: [semantic search finds auth context from Session 1]
  claude-mem: [injects: "Remembering: auth uses JWT, stored in localStorage..."]
  Claude: [continues with full context]
```

### MCP Memory Server

**Official memory server**: `@modelcontextprotocol/server-memory`

Provides **persistent storage across conversations using a knowledge graph model**:

#### Entities

```json
{
  "name": "John_Smith",
  "entityType": "person",
  "observations": ["Speaks Spanish", "Works at Anthropic"]
}
```

#### Relations (Active Voice)

```json
{
  "from": "John_Smith",
  "to": "Anthropic",
  "relationType": "works_at"
}
```

#### Recommended System Prompt for Memory

```
1. User Identification: Assume interacting with default_user
2. Memory Retrieval: Begin with "Remembering..." and query knowledge graph
3. Memory Categories:
   - Basic Identity (age, location, job)
   - Behaviors (interests, habits)
   - Preferences (communication style)
   - Goals (aspirations, targets)
   - Relationships (up to 3 degrees)
4. Memory Update: Create entities, connect via relations, store as observations
```

### Unified Knowledgebase Architecture

From `_SELF_IMPROVING_MEMORY_ARCHITECTURE.md`:

```yaml
Location: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/

Distribution: Symlinks to all AI tools
  ~/.claude/knowledgebase → Main KB
  ~/.windsurf/knowledgebase → Main KB
  ~/.cursor/knowledgebase → Main KB

Benefits:
  - Instant sync (no copying)
  - Zero disk duplication
  - Single edit updates all tools
  - Operating system level (fast)
  - Works offline

Knowledge Types:
  - GitHub Repos (530+)
  - Code Patterns (50+)
  - Performance Data
  - Web Resources
  - Learning Log (continuous discoveries)
```

### Learning Log Pattern

```yaml
Path: ~/Documents/GitHub/Unity-XR-AI/KnowledgeBase/LEARNING_LOG.md

Format:
  ## 2026-01-21 14:30 - Claude Code - Unity XR
  **Discovery**: Burst-compiled jobs are 50x faster than MonoBehaviour loops
  **Context**: Optimizing particle system for Quest 2
  **Impact**: Achieved 90 FPS with 50k particles (was 30 FPS)
  **Code**: See _PERFORMANCE_PATTERNS_REFERENCE.md#burst-jobs
  **Related**: Unity DOTS, ECS, Burst compiler

Usage:
  - All tools can read
  - All tools can append
  - Timestamped entries
  - Searchable by grep/rg
  - Version controlled
```

### Session Transcripts

Every Claude Code session saves a complete transcript:

```yaml
Location: ~/.claude/projects/{project}/{sessionId}/transcript.jsonl

Format: JSON Lines (one JSON object per line)

Contents:
  - User messages
  - Claude responses
  - Tool calls and results
  - System messages
  - Timestamps

Usage:
  - Session recovery
  - Debugging
  - Audit trail
  - Context reconstruction
```

---

## 8. Performance Optimizations

### Prompt Caching

**Automatic optimization** in Claude Code:

```yaml
Mechanism:
  - Static context cached (CLAUDE.md, system prompts)
  - Cache valid for 5 minutes
  - Subsequent requests reuse cache
  - Reduces API costs 90%+

Benefits:
  - Faster responses
  - Lower costs
  - Consistent context
  - No configuration needed
```

### Parallel Tool Execution

When multiple independent tool calls are possible, Claude Code executes them **in parallel**:

```yaml
Example 1: Reading Multiple Files
  Sequential: 5 × 100ms = 500ms
  Parallel: max(100ms) = 100ms
  Speedup: 5x

Example 2: Web Searches
  Sequential: 3 × 2s = 6s
  Parallel: max(2s) = 2s
  Speedup: 3x

Implementation:
  - Collect tool calls as Promise tasks
  - Use Promise.all() for concurrent execution
  - Aggregate results
  - Feed back to model
```

### Asynchronous API Calls

For programmatic usage (Claude Agent SDK):

```typescript
// Sequential (slow)
const result1 = await client.call(tool1);
const result2 = await client.call(tool2);
const result3 = await client.call(tool3);

// Parallel (fast)
const [result1, result2, result3] = await Promise.all([
  client.call(tool1),
  client.call(tool2),
  client.call(tool3)
]);
```

**Performance Impact**: Can transform operations from minutes to seconds.

### Parallel Claude Code Sessions

Claude Code **wasn't originally built for concurrency** but can be parallelized using:

#### Git Worktrees Approach

```yaml
Mechanism:
  - Each worktree has independent file state
  - Perfect for parallel Claude Code sessions
  - Prevents interference

Setup:
  git worktree add ../project-feature1 feature1
  git worktree add ../project-feature2 feature2
  cd ../project-feature1 && claude
  cd ../project-feature2 && claude
```

#### Cloud Environment Solution (Gitpod)

```yaml
Mechanism:
  - Each Claude Code agent in own environment
  - Own CPU, memory, file system, git state
  - Purpose-built infrastructure

Benefits:
  - True isolation
  - Resource guarantees
  - No local conflicts
```

#### Subagents Pattern

```yaml
Mechanism:
  - Break task into constituent parts
  - Specialist agent for each part
  - Run concurrently

Example:
  Main: "Research auth, DB, and API in parallel"
  - Subagent 1: auth investigation
  - Subagent 2: database investigation
  - Subagent 3: API investigation
  - All run concurrently

Benefits:
  - Massive time savings
  - Context isolation
  - Specialized expertise
```

### Context Window Optimization

**Token Budget Guidelines** (from KB):

| Session Type | Budget | Load Strategy |
|--------------|--------|---------------|
| Quick fix | 5-10K | Just target file |
| New feature | 15-20K | GitHub KB + AR patterns + 1 guide |
| Research | 30-40K | Multiple guides + web search |
| Architecture | 40-60K | Full KB sections |

**Intelligent Loading**:

```yaml
Session Start (5K tokens):
  - Global CLAUDE.md
  - Master Index
  - Wait for user request

Unity Task (+10K tokens):
  - Relevant GitHub repos section
  - AR Foundation snippets if AR
  - Performance patterns if optimization

WebGL Task (+8K tokens):
  - Three.js guide
  - CodeSandbox examples
  - Web performance patterns

Research Task (+15K tokens):
  - Full GitHub KB
  - Comprehensive guide sections
  - Web search as needed

Total Max: ~40K tokens (well within 200K limit)
```

### Build Optimization (Project-Specific)

**ccache (C++ Compilation Cache)**:

```bash
brew install ccache

# Enable in ios/Podfile.properties.json
{"apple.ccacheEnabled": "true"}

# Fresh pod install required
cd ios && pod install
```

**Build Lock Pattern** (Prevent concurrent builds):

```bash
LOCK_FILE="/tmp/build.lock"
if [ -f "$LOCK_FILE" ]; then
    OTHER_PID=$(cat "$LOCK_FILE")
    kill -0 "$OTHER_PID" 2>/dev/null && exit 1
fi
echo $$ > "$LOCK_FILE"
trap "rm -f '$LOCK_FILE'" EXIT
```

### Model Selection for Performance

From Anthropic best practices (2026):

```yaml
Claude Code Creator's Approach:
  - Uses Opus 4.5 exclusively
  - "I use Opus 4.5 with thinking for everything"
  - Prioritizes quality over speed

Best Practice:
  - Quick tasks: Sonnet 4.5 or Haiku
  - Complex reasoning: Opus 4.5
  - Deep thinking: Opus 4.5 + extended thinking
  - Exploration: Haiku (via Explore subagent)

Thinking Budget Triggers:
  - "think" → Low budget
  - "think hard" → Medium budget
  - "think harder" → High budget
  - "ultrathink" → Maximum budget
```

---

## 9. Error Handling

### Built-in Error Recovery

#### /doctor Command

Claude Code includes a `/doctor` command to **diagnose common issues automatically**:

```yaml
Checks:
  - MCP server connections
  - Permission configurations
  - Tool availability
  - Session state
  - File access

Output:
  - Issues found
  - Recommended fixes
  - Automated repairs where possible
```

### Error Handling Patterns

#### Retry Logic with Exponential Backoff

Production-tested implementation:

```yaml
Strategy:
  - Exponential backoff with jitter
  - Circuit breaker patterns
  - Intelligent failure detection
  - Avoid overwhelming strained servers
  - Maximize recovery chances

Performance:
  - 10,000+ requests daily
  - 73% successful recovery rate during 529 incidents

Implementation:
  - Separate user-facing requests from background retry queues
  - Exponential backoff with jitter
  - Reasonable timeout values (30s max)
  - Circuit breakers with half-open states
```

#### Circuit Breaker Pattern

Activate when:
- Task execution fails
- Timeouts occur
- External services fail
- Database transactions fail
- Cascade failure risks detected

Pattern:
```
1. Closed State: Normal operation
2. Open State: Fast-fail without trying (after N failures)
3. Half-Open State: Test with single request
4. Recovery: Return to Closed if successful
```

### Known Issues (January 2026)

#### CLI Freezing

**Issue**: When running Claude Code CLI in long-running automated scripts, occasionally encounters "No messages returned" error causing infinite freeze.

**Workaround**:
- Hit Esc twice to revert and retry
- Ctrl + C to exit, then restart

#### Infinite Retry Loops

**Issue**: Common validation errors (large images, interrupted tool execution) cause CLI to become unresponsive to new input.

**Impact**: Forces session termination, all conversation context lost.

**Mitigation**:
- Implement timeout at wrapper script level
- Monitor for stuck states
- Automatic restart with context recovery

### Error Recovery Strategies

#### Immediate Recovery

```bash
# Hit Esc twice to revert last message
ESC ESC

# Force exit if frozen
Ctrl + C

# Restart session
claude
```

#### Context Recovery After Error

```yaml
1. Check Transcript:
   - Location: ~/.claude/projects/{project}/{sessionId}/transcript.jsonl
   - Extract: Last successful state

2. Recover Context:
   - Use /memory to add critical info back
   - Reference important files
   - Summarize completed work

3. Resume:
   - Continue from last good state
   - Avoid repeating failed operation
```

#### Graceful Degradation

```yaml
Pattern:
  1. Primary approach fails
  2. Fall back to simpler method
  3. Inform user of degraded mode
  4. Continue with reduced functionality

Example:
  - WebRTC unavailable → Fall back to HTTP polling
  - GPU compute fails → Fall back to CPU
  - MCP server down → Use built-in tools only
```

### Tool-Specific Error Handling

#### Bash Tool Failures

```yaml
Exit Codes:
  0: Success
  1-255: Various failures (except 2)
  2: Special - fed back to Claude as blocking error

Claude Behavior:
  - Exit 0: Continue normally
  - Exit 1-255 (except 2): Show error to user, may retry
  - Exit 2: Block operation, feed error to Claude for reasoning
```

#### File Operation Failures

```yaml
Common Failures:
  - Permission denied
  - File not found
  - Disk full
  - Path too long

Recovery:
  - Claude automatically tries alternative approaches
  - May ask user for guidance
  - Can use different tools (Edit vs Write)
  - Falls back to manual intervention
```

### PreToolUse Validation

Use hooks to **prevent errors before they happen**:

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [{
          "type": "command",
          "command": "./scripts/validate-bash-command.sh"
        }]
      }
    ]
  }
}
```

Validation script can:
- Check for dangerous commands (`rm -rf /`)
- Verify prerequisites exist
- Check resource availability
- Exit 2 to block if unsafe

---

## 10. Best Practices

### Official Anthropic Recommendations

From **"Claude Code: Best practices for agentic coding"** (anthropic.com/engineering):

#### 1. Explore, Plan, Execute Workflow

```yaml
Phase 1: Explore
  - Ask Claude to read relevant files, images, or URLs
  - Provide general pointers or specific filenames
  - EXPLICITLY tell it not to write code yet
  - Use "think" to trigger extended thinking mode

Phase 2: Plan
  - Claude analyzes findings
  - Proposes approach
  - User reviews and approves
  - Adjustments made before implementation

Phase 3: Execute
  - Implementation begins
  - Claude writes code
  - Tests and verifies
  - Iterates as needed

Benefits:
  - Prevents premature implementation
  - Better solution quality
  - Less rework
  - Context preserved for execution
```

#### 2. Strong Use of Subagents

**Recommendation**: Tell Claude to use subagents to verify details or investigate questions, **especially early on** in a conversation or task.

Benefits:
- Preserves context availability
- Isolates exploration from implementation
- Enables parallel investigation
- Specialized expertise per subagent

Example prompts:
```
"Use subagents to research authentication, database design, and API architecture in parallel"

"Before implementing, use an Explore subagent to verify how the existing auth system works"
```

#### 3. Thinking Levels for Complex Tasks

| Phrase | Thinking Budget | Use Case |
|--------|----------------|----------|
| "think" | Low | Standard tasks |
| "think hard" | Medium | Complex problems |
| "think harder" | High | Multi-step reasoning |
| "ultrathink" | Maximum | Deep architectural decisions |

#### 4. CLAUDE.md File Management

**Pattern from Claude Code's creator**:

> "Anytime we see Claude do something incorrectly we add it to the CLAUDE.md, so Claude knows not to do it next time."

This creates a **continuously improving instruction set** specific to your project.

Structure:
```markdown
# Project Guidelines

## Never Do This
- Don't use deprecated API X, use Y instead
- Don't run tests without building first
- Don't commit without running linter

## Always Do This
- Use TypeScript strict mode
- Write tests for new features
- Update docs when changing APIs

## Project-Specific Patterns
- Auth uses JWT stored in httpOnly cookies
- Database migrations use Prisma
- Deploy via ./scripts/deploy.sh
```

#### 5. Custom Slash Commands

For repeated workflows, store prompt templates in `.claude/commands/`:

```markdown
# .claude/commands/debug-loop.md
Debug the following error: $ARGUMENTS

1. Analyze the error message
2. Search codebase for relevant files
3. Implement fix
4. Run tests to verify
5. If tests fail, iterate
6. Commit when green
```

Usage: `/project:debug-loop "NullReferenceException in BridgeTarget"`

Benefits:
- Codify best practices
- Share across team (commit to git)
- Consistent workflows
- Faster execution

#### 6. Model Selection Strategy

**From Claude Code's Creator**:
- Uses **Opus 4.5 exclusively**
- Always with extended thinking enabled
- Prioritizes quality over speed/cost

**Practical Approach**:
- **Development**: Sonnet 4.5 (balanced speed/quality)
- **Exploration**: Haiku (fast read-only research)
- **Complex Problems**: Opus 4.5 + thinking
- **Production Code**: Opus 4.5 (highest quality)

### Permission Configuration Best Practices

From `_CLAUDE_CODE_WORKFLOW_OPTIMIZATION.md`:

```json
{
  "permissions": {
    "allow": [
      "Bash",                              // ALL bash commands
      "Read", "Write", "Edit",             // ALL file operations
      "Glob", "Grep",                      // ALL searches
      "WebSearch",                         // Web search
      "WebFetch(domain:github.com)",       // Specific domains only
      "mcp__UnityMCP__*",                  // All Unity MCP tools
      "mcp__github__*"                     // All GitHub MCP tools
    ],
    "deny": [
      "**/.env*",                          // Block env files
      "**/*secret*",                       // Block secrets
      "**/.git/config"                     // Block git config
    ]
  }
}
```

**Common Mistakes**:
- ❌ `Bash(*)` → ✅ `Bash` (no parentheses for "allow all")
- ❌ `WebFetch(*)` → ✅ `WebFetch(domain:example.com)` (domain prefix required)

### Multi-Agent Coordination

From `_MULTI_AGENT_COORDINATION.md`:

#### Never Parallelize

- Git operations (add, commit, rebase, merge)
- Build processes (xcodebuild, gradle, Unity)
- Device deployments (iOS/Android)
- Unity MCP write operations
- Pod install / npm install
- Schema migrations

#### Safe to Parallelize

- Reading different files
- Web searches and research
- Independent API calls
- Searching different directories
- Read-only MCP queries

#### Requires Coordination

- Editing files in same project (check for overlap)
- Running tests (may conflict with builds)
- Database operations (transaction isolation)
- Cache operations (invalidation timing)

### TDD Workflow (Anthropic Favorite)

```yaml
Pattern:
  1. Write tests first
     - Tell Claude NOT to implement yet
     - Just write the tests

  2. Run tests, confirm they fail
     - Verify tests are valid

  3. Commit tests
     - Lock in expected behavior

  4. Implement code to pass tests
     - Claude writes implementation

  5. Iterate until green
     - Fix failures
     - Refine implementation

  6. Commit implementation
     - Clean commit history

Benefits:
  - Clear success criteria
  - Prevents over-engineering
  - Documentation via tests
  - Regression protection
```

### Block-at-Submit Pattern

Create hooks that **block commits until tests pass**:

```bash
#!/bin/bash
# PreToolUse hook for git commit

# Check for test results file
if [ ! -f "/tmp/tests-passed" ]; then
  echo "Tests have not passed. Run tests first." >&2
  exit 2  # Block the commit
fi

exit 0  # Allow commit
```

Forces **test → fix → test** loop before code can be committed.

### Headless Mode for CI/CD

```bash
# Non-interactive automation
claude -p "Run tests and fix failures" --output-format stream-json

# In GitHub Actions
- run: |
    claude -p "Review PR and add comments" \
      --allowedTools "Bash,Read,mcp__github__*"
```

### Context Management Best Practices

```yaml
Monitoring:
  - Use /stats to check context usage
  - Monitor throughout session
  - Proactive compaction at 70%

Preservation:
  - Use /memory for critical context
  - Document decisions in CLAUDE.md
  - Keep important info in project files (not just conversation)

Recovery:
  - Save transcripts for critical sessions
  - Use claude-mem for automatic context persistence
  - Document workflow in project README
```

### Knowledge Accumulation Strategy

From `_SELF_IMPROVING_MEMORY_ARCHITECTURE.md`:

```yaml
What to Capture:
  ✅ New GitHub repos discovered
  ✅ Performance measurements
  ✅ Code patterns that work well
  ✅ Solutions to problems
  ✅ Better approaches found
  ✅ Tool configurations
  ✅ Integration techniques

What to Filter:
  ❌ Temporary/session-specific info
  ❌ Duplicate information
  ❌ Incorrect/outdated approaches
  ❌ Personal/sensitive data
  ❌ Verbose explanations
  ❌ Official doc duplication

Process:
  1. AI identifies potential knowledge
  2. Validates against existing KB
  3. Deduplicates if already known
  4. Categorizes by topic
  5. Appends to appropriate file
  6. Updates indexes
  7. Commits to Git
```

### Session Startup Ritual

```yaml
Recommended Startup:
  1. Check recent sessions
     - Review transcript if continuing work

  2. Load relevant context
     - Read CLAUDE.md updates
     - Check /memory
     - Review recent Learning Log entries

  3. State intent clearly
     - "Today I want to..."
     - Provide context upfront
     - Reference previous work if relevant

  4. Establish workflow
     - "Let's use subagents for research"
     - "Think hard about this problem"
     - "Explore first, then implement"
```

---

## Summary: Key Insights for Intelligence Systems

### Architecture Insights

1. **Simplicity Wins**: Claude Code's single-threaded agent loop outperforms complex multi-agent orchestration through focused execution

2. **Tool-First Design**: Everything is a tool - makes system composable, extensible, and debuggable

3. **Stateful Application, Stateless Protocol**: Future-proofs for horizontal scaling while maintaining rich session state

### Performance Insights

4. **Parallel Execution**: Independent tool calls execute concurrently - 5-10x speedups common

5. **Prompt Caching**: Automatic 90%+ cost reduction and faster responses for static context

6. **Intelligent Loading**: Load only needed knowledge (5-40K tokens) vs. everything upfront

### Context Management Insights

7. **Proactive Compaction**: Monitor at 70%, compact before 85%, never let reach 95%

8. **Context is Expensive**: Preserve what matters, summarize what doesn't, discard what's temporary

9. **Multi-Layer Memory**: CLAUDE.md (static), claude-mem (semantic), MCP memory (graph), transcripts (complete history)

### Extensibility Insights

10. **Hooks for Determinism**: When you need guarantees (logging, validation, formatting), use hooks not LLM

11. **Subagents for Isolation**: Keep exploration out of main context, specialize by task, enable parallelism

12. **MCP for Integration**: Standard protocol beats custom integrations - write once, use everywhere

### Quality Insights

13. **Explore → Plan → Execute**: Best results come from NOT coding immediately

14. **Thinking Budget Matters**: "ultrathink" for critical decisions, regular mode for routine tasks

15. **Continuous Learning**: Every session should improve the system (CLAUDE.md updates, Learning Log entries)

### Team Insights

16. **Codify Workflows**: Custom slash commands + CLAUDE.md = team consistency

17. **Tool Safety**: PreToolUse hooks prevent errors, PostToolUse hooks provide feedback

18. **Shared Knowledge**: Symlinks + git = zero-latency knowledge distribution across tools

---

## Sources

### Official Documentation

- [Claude Code Overview](https://code.claude.com/docs/en/overview)
- [Claude Code Best Practices](https://www.anthropic.com/engineering/claude-code-best-practices)
- [Common Workflows](https://code.claude.com/docs/en/common-workflows)
- [Hooks Guide](https://code.claude.com/docs/en/hooks-guide)
- [Sub-agents Documentation](https://code.claude.com/docs/en/sub-agents)
- [Manage Costs Effectively](https://code.claude.com/docs/en/costs)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/specification/2025-11-25)
- [MCP Architecture Overview](https://modelcontextprotocol.io/docs/learn/architecture)
- [Claude Agent SDK](https://docs.anthropic.com/en/docs/claude-code/sdk)

### Technical Articles

- [How Claude Code is Built - Pragmatic Engineer](https://newsletter.pragmaticengineer.com/p/how-claude-code-is-built)
- [Claude Code Behind-the-Scenes Master Agent Loop](https://blog.promptlayer.com/claude-code-behind-the-scenes-of-the-master-agent-loop/)
- [Building Agents with the Claude Agent SDK](https://www.anthropic.com/engineering/building-agents-with-the-claude-agent-sdk)
- [Advanced Tool Use](https://www.anthropic.com/engineering/advanced-tool-use)
- [How Claude Code Got Better by Protecting More Context](https://hyperdev.matsuoka.com/p/how-claude-code-got-better-by-protecting)
- [Exploring the Future of MCP Transports](http://blog.modelcontextprotocol.io/posts/2025-12-19-mcp-transport-future/)

### Community Resources

- [Claude Code Must-Haves - January 2026](https://dev.to/valgard/claude-code-must-haves-january-2026-kem)
- [How to Run Claude Code in Parallel](https://ona.com/stories/parallelize-claude-code)
- [Claude Code Subagents for Parallel Development](https://zachwills.net/how-to-use-claude-code-subagents-to-parallelize-development/)
- [Claude-Mem Plugin](https://github.com/thedotmack/claude-mem)
- [Error Recovery Patterns](https://claude-plugins.dev/skills/@applied-artificial-intelligence/claude-code-toolkit/error-recovery-patterns)

### Release Notes

- [Claude Code 2.1.0 Release](https://venturebeat.com/orchestration/claude-code-2-1-0-arrives-with-smoother-workflows-and-smarter-agents)
- [Claude Code Finally Fixed Its Biggest Problems](https://medium.com/@joe.njenga/claude-code-finally-fixed-its-biggest-problems-stats-instant-compact-and-more-0c85801c8d10)

### Local Knowledgebase

- `_CLAUDE_CODE_HOOKS.md`
- `_CLAUDE_CODE_SUBAGENTS.md`
- `_CLAUDE_CODE_WORKFLOW_OPTIMIZATION.md`
- `_MCP_MODEL_CONTEXT_PROTOCOL.md`
- `_MULTI_AGENT_COORDINATION.md`
- `_SELF_IMPROVING_MEMORY_ARCHITECTURE.md`
- `_KB_CLAUDE_CODE_QUICK_ACCESS.md`

---

**Report Generated**: 2026-01-21
**For**: Unity-XR-AI Knowledge Base
**Token Estimate**: ~15K tokens
