# Learning Log Entry: Model Context Protocol (MCP)

**Date**: 2025-01-13  
**Session**: MCP Documentation Review

---

## What Was Learned

### Model Context Protocol (MCP)
- Open protocol by Anthropic for AI-to-external-service communication
- Solves N×M integration problem with standardized interface
- Current version: 2025-11-25

### Architecture
- **Hosts**: AI apps (Claude Desktop, Claude Code, VS Code)
- **Clients**: One per server connection within host
- **Servers**: Provide tools, resources, prompts

### Core Primitives
1. **Tools**: Executable functions (tools/list, tools/call)
2. **Resources**: Data sources (resources/list, resources/read)
3. **Prompts**: LLM templates (prompts/list, prompts/get)

### Official Servers
- filesystem, memory, fetch, git, time, sequentialthinking

### Key Insight
MCP is analogous to USB - standardized interface enabling any compatible host to connect with any compatible server without custom integration code.

---

## Files Created
- `_MCP_MODEL_CONTEXT_PROTOCOL.md` - Full knowledge base entry

## Sources Reviewed
- https://modelcontextprotocol.io/specification/
- https://modelcontextprotocol.io/docs/learn/architecture
- https://github.com/modelcontextprotocol
- https://github.com/modelcontextprotocol/servers

---

## Potential Applications
- Create Unity MCP server exposing scene data/commands
- Use memory server for persistent AI context in dev workflow
- Leverage filesystem server for controlled project access

---

*Move to main LEARNING_LOG.md in knowledgebase when syncing*
