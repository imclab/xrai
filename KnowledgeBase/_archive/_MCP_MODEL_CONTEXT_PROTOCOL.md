# Model Context Protocol (MCP) Knowledge Base

> **Last Updated**: 2025-01-13  
> **Protocol Version**: 2025-11-25 (Current)  
> **Source**: https://modelcontextprotocol.io

---

## Overview

The Model Context Protocol (MCP) is an open standard that enables AI applications to connect with external data sources, tools, and services. Created by Anthropic, MCP provides a standardized way for AI hosts (like Claude Desktop, Claude Code, VS Code) to communicate with servers that provide context, tools, and resources.

**Key Value Proposition**: MCP solves the "N×M integration problem" - instead of each AI app implementing custom integrations with each service, MCP provides one standardized protocol that works across all compatible hosts and servers.

---

## Architecture

### Participants

```
┌─────────────────────────────────────────────────────────────┐
│                      MCP HOST                                │
│  (Claude Desktop, Claude Code, VS Code, etc.)               │
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

- **MCP Host**: AI application that coordinates MCP clients (e.g., Claude Code, Claude Desktop)
- **MCP Client**: Component that maintains connection to one MCP server
- **MCP Server**: Program that provides context, tools, or resources to clients

### Protocol Layers

1. **Transport Layer** (outer): Communication mechanisms
   - **STDIO**: Standard input/output for local processes (no network overhead)
   - **Streamable HTTP**: HTTP POST + Server-Sent Events for remote servers

2. **Data Layer** (inner): JSON-RPC 2.0 based protocol
   - Lifecycle management (init, capability negotiation, termination)
   - Primitives (tools, resources, prompts)
   - Notifications and progress tracking

---

## Core Primitives

### 1. Tools
Executable functions that AI can invoke to perform actions.

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

### 2. Resources
Data sources providing contextual information.

```json
{
  "uri": "file:///project/README.md",
  "name": "Project README",
  "mimeType": "text/markdown"
}
```

**Discovery**: `resources/list` → **Retrieval**: `resources/read`

### 3. Prompts
Reusable templates for LLM interactions.

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

---

## Official MCP Servers

Repository: https://github.com/modelcontextprotocol/servers

| Server | Purpose | Key Tools |
|--------|---------|-----------|
| **filesystem** | File operations | read_file, write_file, edit_file, search_files, directory_tree |
| **memory** | Knowledge graph persistence | create_entities, create_relations, search_nodes, read_graph |
| **fetch** | Web content retrieval | fetch URL as markdown |
| **git** | Git operations | status, diff, commit, log |
| **time** | Time/timezone operations | get_current_time, convert_timezone |
| **sequentialthinking** | Step-by-step reasoning | structured thinking workflow |
| **everything** | Demo server with all primitives | showcase of MCP capabilities |

### Installation (NPX)

```json
{
  "mcpServers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "/path/to/allowed/dir"]
    },
    "memory": {
      "command": "npx", 
      "args": ["-y", "@modelcontextprotocol/server-memory"]
    }
  }
}
```

---

## Configuration

### Claude Desktop (`claude_desktop_config.json`)
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-name"],
      "env": { "OPTIONAL_VAR": "value" }
    }
  }
}
```

### VS Code (`.vscode/mcp.json` or user MCP config)
```json
{
  "servers": {
    "filesystem": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem", "${workspaceFolder}"]
    }
  }
}
```

### Docker (Sandboxed)
```json
{
  "mcpServers": {
    "filesystem": {
      "command": "docker",
      "args": [
        "run", "-i", "--rm",
        "--mount", "type=bind,src=/host/path,dst=/projects/dir",
        "mcp/filesystem", "/projects"
      ]
    }
  }
}
```

---

## Tool Annotations (Hints)

MCP 2025-03-26+ supports tool annotations for client behavior hints:

| Annotation | Purpose |
|------------|---------|
| `readOnlyHint` | Tool only reads, doesn't modify |
| `idempotentHint` | Safe to retry with same arguments |
| `destructiveHint` | May overwrite/delete data |

Example from filesystem server:
- `read_text_file`: readOnlyHint=true
- `write_file`: idempotentHint=true, destructiveHint=true
- `edit_file`: destructiveHint=true (re-applying can double-apply)

---

## SDKs

Official SDKs available for:
- **TypeScript/JavaScript**: Primary reference implementation
- **Python**: Full protocol support
- **Additional languages**: Check https://modelcontextprotocol.io/docs/sdk

All SDKs support:
- Creating MCP servers with tools, resources, prompts
- Building MCP clients
- Local (STDIO) and remote (HTTP) transports
- Full type safety

---

## Memory Server: Knowledge Graph

The memory server provides persistent storage across conversations using a knowledge graph model:

### Entities
```json
{
  "name": "John_Smith",
  "entityType": "person",
  "observations": ["Speaks Spanish", "Works at Anthropic"]
}
```

### Relations (Active Voice)
```json
{
  "from": "John_Smith",
  "to": "Anthropic", 
  "relationType": "works_at"
}
```

### Recommended System Prompt for Memory
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

---

## Development Tools

- **MCP Inspector**: https://github.com/modelcontextprotocol/inspector
  - Debug and test MCP servers
  - Visualize tool/resource/prompt discovery
  - Test tool execution

---

## Key Links

| Resource | URL |
|----------|-----|
| Specification | https://modelcontextprotocol.io/specification/2025-11-25 |
| Architecture Docs | https://modelcontextprotocol.io/docs/learn/architecture |
| Official Servers | https://github.com/modelcontextprotocol/servers |
| Protocol Schema | https://github.com/modelcontextprotocol/modelcontextprotocol |
| SDK Documentation | https://modelcontextprotocol.io/docs/sdk |

---

## Version History

| Version | Status | Notes |
|---------|--------|-------|
| 2025-11-25 | **Current** | Latest stable, tool annotations |
| Earlier | Final | Archived specifications |

---

## Integration Checklist

- [ ] Install desired MCP servers via NPX or Docker
- [ ] Configure in `claude_desktop_config.json` or VS Code `mcp.json`
- [ ] Specify allowed directories for filesystem server
- [ ] Test with MCP Inspector if debugging needed
- [ ] Consider memory server for cross-conversation persistence

---

*Created for Unity-XR-AI Knowledge Base*
