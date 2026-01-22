# MCP TypeScript SDK Reference

> **Source**: https://github.com/modelcontextprotocol/typescript-sdk  
> **Last Updated**: 2025-01-13  
> **Version**: v2 (main branch, pre-alpha) | v1.x (production recommended)

---

## Overview

The Model Context Protocol (MCP) TypeScript SDK provides libraries for building MCP servers and clients in TypeScript/JavaScript.

## Packages

| Package | Purpose |
|---------|---------|
| `@modelcontextprotocol/server` | Build MCP servers |
| `@modelcontextprotocol/client` | Build MCP clients |

**Required peer dependency**: `zod` (v3.25+)

---

## Installation

### Server

```bash
npm install @modelcontextprotocol/server zod
```

### Client

```bash
npm install @modelcontextprotocol/client zod
```

---

## Quick Start

### Run Example Server

```bash
# From repo root
pnpm install
pnpm --filter @modelcontextprotocol/examples-server exec tsx src/simpleStreamableHttp.ts
```

### Run Example Client

```bash
pnpm --filter @modelcontextprotocol/examples-client exec tsx src/simpleStreamableHttp.ts
```

---

## Server Example

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const server = new McpServer({
  name: "my-server",
  version: "1.0.0",
});

// Register a tool
server.registerTool(
  "my_tool",
  {
    description: "Description of what the tool does",
    inputSchema: {
      param1: z.string().describe("Parameter description"),
      param2: z.number().optional().describe("Optional number"),
    },
  },
  async ({ param1, param2 }) => {
    // Tool implementation
    return {
      content: [{ type: "text", text: `Result: ${param1}` }],
    };
  }
);

// Start server with STDIO transport
async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
}

main();
```

---

## Client Example

```typescript
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const client = new Client({
  name: "my-client",
  version: "1.0.0",
});

// Connect to server
const transport = new StdioClientTransport({
  command: "node",
  args: ["path/to/server.js"],
});
await client.connect(transport);

// List available tools
const tools = await client.listTools();
console.log("Available tools:", tools.tools.map(t => t.name));

// Call a tool
const result = await client.callTool({
  name: "my_tool",
  arguments: { param1: "value" },
});
console.log("Result:", result);

// Cleanup
await client.close();
```

---

## Transports

### STDIO (Local)
- Best for local process communication
- No network overhead
- Used by Claude Desktop

### Streamable HTTP (Remote)
- HTTP POST for client-to-server
- Server-Sent Events for streaming
- Supports OAuth authentication

---

## Documentation

- **Server Guide**: `docs/server.md` - Tools, resources, prompts, transports, CORS, deployment
- **Client Guide**: `docs/client.md` - High-level client, transports, OAuth
- **Capabilities**: `docs/capabilities.md` - Sampling, elicitation, task execution
- **FAQ**: `docs/faq.md` - Troubleshooting, Node.js crypto support

---

## Version Notes

- **v2 (main)**: In development, pre-alpha. Expected stable Q1 2026.
- **v1.x**: Production recommended. Bug fixes and security updates for 6+ months after v2.

For v1 documentation: https://github.com/modelcontextprotocol/typescript-sdk/tree/v1.x

---

## External Links

- [MCP Documentation](https://modelcontextprotocol.io)
- [MCP Specification](https://spec.modelcontextprotocol.io)
- [Example Servers](https://github.com/modelcontextprotocol/servers)

---

*Created for Unity-XR-AI Knowledge Base*
