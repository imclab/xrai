# MCP Build Server & Client Guide

> **Sources**:  
> - https://modelcontextprotocol.io/docs/develop/build-server  
> - https://modelcontextprotocol.io/docs/develop/build-client  
> **Last Updated**: 2025-01-13

---

## Build an MCP Server

### Critical: Logging for STDIO Servers

**NEVER write to stdout** in STDIO-based servers - it corrupts JSON-RPC messages!

```python
# ❌ Bad (STDIO)
print("Processing request")

# ✅ Good (STDIO)
import logging
logging.info("Processing request")
```

```javascript
// ❌ Bad (STDIO)
console.log("Server started");

// ✅ Good (STDIO)
console.error("Server started"); // stderr is safe
```

---

## Python Server (FastMCP)

### Setup

```bash
# Install uv package manager
curl -LsSf https://astral.sh/uv/install.sh | sh

# Create project
uv init weather && cd weather
uv venv && source .venv/bin/activate
uv add "mcp[cli]" httpx
touch weather.py
```

### Complete Server Example

```python
from typing import Any
import httpx
from mcp.server.fastmcp import FastMCP

# Initialize FastMCP server
mcp = FastMCP("weather")

# Constants
NWS_API_BASE = "https://api.weather.gov"
USER_AGENT = "weather-app/1.0"

async def make_nws_request(url: str) -> dict[str, Any] | None:
    """Make a request to the NWS API with proper error handling."""
    headers = {"User-Agent": USER_AGENT, "Accept": "application/geo+json"}
    async with httpx.AsyncClient() as client:
        try:
            response = await client.get(url, headers=headers, timeout=30.0)
            response.raise_for_status()
            return response.json()
        except Exception:
            return None

@mcp.tool()
async def get_alerts(state: str) -> str:
    """Get weather alerts for a US state.

    Args:
        state: Two-letter US state code (e.g. CA, NY)
    """
    url = f"{NWS_API_BASE}/alerts/active/area/{state}"
    data = await make_nws_request(url)

    if not data or "features" not in data:
        return "Unable to fetch alerts or no alerts found."

    if not data["features"]:
        return "No active alerts for this state."

    return "\n---\n".join([format_alert(f) for f in data["features"]])

@mcp.tool()
async def get_forecast(latitude: float, longitude: float) -> str:
    """Get weather forecast for a location.

    Args:
        latitude: Latitude of the location
        longitude: Longitude of the location
    """
    points_url = f"{NWS_API_BASE}/points/{latitude},{longitude}"
    points_data = await make_nws_request(points_url)

    if not points_data:
        return "Unable to fetch forecast data for this location."

    forecast_url = points_data["properties"]["forecast"]
    forecast_data = await make_nws_request(forecast_url)

    if not forecast_data:
        return "Unable to fetch detailed forecast."

    periods = forecast_data["properties"]["periods"]
    return "\n---\n".join([format_period(p) for p in periods[:5]])

def main():
    mcp.run(transport="stdio")

if __name__ == "__main__":
    main()
```

### Run Python Server

```bash
uv run weather.py
```

---

## TypeScript Server

### Setup

```bash
mkdir weather && cd weather
npm init -y
npm install @modelcontextprotocol/sdk zod@3
npm install -D @types/node typescript
mkdir src && touch src/index.ts
```

### package.json

```json
{
  "type": "module",
  "bin": { "weather": "./build/index.js" },
  "scripts": { "build": "tsc && chmod 755 build/index.js" },
  "files": ["build"]
}
```

### tsconfig.json

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "Node16",
    "moduleResolution": "Node16",
    "outDir": "./build",
    "rootDir": "./src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true
  },
  "include": ["src/**/*"],
  "exclude": ["node_modules"]
}
```

### Complete Server Example

```typescript
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const server = new McpServer({
  name: "weather",
  version: "1.0.0",
});

// Register tools
server.registerTool(
  "get_alerts",
  {
    description: "Get weather alerts for a state",
    inputSchema: {
      state: z.string().length(2).describe("Two-letter state code (e.g. CA, NY)"),
    },
  },
  async ({ state }) => {
    // Implementation here
    return { content: [{ type: "text", text: "Alert data..." }] };
  }
);

server.registerTool(
  "get_forecast",
  {
    description: "Get weather forecast for a location",
    inputSchema: {
      latitude: z.number().min(-90).max(90).describe("Latitude"),
      longitude: z.number().min(-180).max(180).describe("Longitude"),
    },
  },
  async ({ latitude, longitude }) => {
    // Implementation here
    return { content: [{ type: "text", text: "Forecast data..." }] };
  }
);

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Weather MCP Server running on stdio");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
```

### Build & Run

```bash
npm run build
node build/index.js
```

---

## Build an MCP Client

### Python Client

```python
import asyncio
from typing import Optional
from contextlib import AsyncExitStack

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from anthropic import Anthropic
from dotenv import load_dotenv

load_dotenv()

class MCPClient:
    def __init__(self):
        self.session: Optional[ClientSession] = None
        self.exit_stack = AsyncExitStack()
        self.anthropic = Anthropic()

    async def connect_to_server(self, server_script_path: str):
        """Connect to an MCP server."""
        is_python = server_script_path.endswith('.py')
        command = "python" if is_python else "node"
        
        server_params = StdioServerParameters(
            command=command,
            args=[server_script_path],
            env=None
        )

        stdio_transport = await self.exit_stack.enter_async_context(
            stdio_client(server_params)
        )
        self.stdio, self.write = stdio_transport
        self.session = await self.exit_stack.enter_async_context(
            ClientSession(self.stdio, self.write)
        )

        await self.session.initialize()

        response = await self.session.list_tools()
        print("Connected with tools:", [t.name for t in response.tools])

    async def process_query(self, query: str) -> str:
        """Process a query using Claude and available tools."""
        messages = [{"role": "user", "content": query}]

        response = await self.session.list_tools()
        available_tools = [{
            "name": tool.name,
            "description": tool.description,
            "input_schema": tool.inputSchema
        } for tool in response.tools]

        response = self.anthropic.messages.create(
            model="claude-sonnet-4-20250514",
            max_tokens=1000,
            messages=messages,
            tools=available_tools
        )

        final_text = []
        for content in response.content:
            if content.type == 'text':
                final_text.append(content.text)
            elif content.type == 'tool_use':
                result = await self.session.call_tool(
                    content.name, content.input
                )
                # Continue conversation with tool result...
        
        return "\n".join(final_text)

    async def cleanup(self):
        await self.exit_stack.aclose()

async def main():
    client = MCPClient()
    try:
        await client.connect_to_server(sys.argv[1])
        # Interactive loop here
    finally:
        await client.cleanup()

if __name__ == "__main__":
    import sys
    asyncio.run(main())
```

### TypeScript Client

```typescript
import { Anthropic } from "@anthropic-ai/sdk";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

class MCPClient {
  private mcp: Client;
  private anthropic: Anthropic;
  private tools: Tool[] = [];

  constructor() {
    this.anthropic = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });
    this.mcp = new Client({ name: "mcp-client-cli", version: "1.0.0" });
  }

  async connectToServer(serverScriptPath: string) {
    const isPy = serverScriptPath.endsWith(".py");
    const command = isPy ? "python3" : process.execPath;

    const transport = new StdioClientTransport({
      command,
      args: [serverScriptPath],
    });
    await this.mcp.connect(transport);

    const toolsResult = await this.mcp.listTools();
    this.tools = toolsResult.tools.map((tool) => ({
      name: tool.name,
      description: tool.description,
      input_schema: tool.inputSchema,
    }));
  }

  async processQuery(query: string) {
    const response = await this.anthropic.messages.create({
      model: "claude-sonnet-4-20250514",
      max_tokens: 1000,
      messages: [{ role: "user", content: query }],
      tools: this.tools,
    });

    // Handle response and tool calls...
  }
}
```

---

## Claude Desktop Configuration

### macOS
Location: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "weather": {
      "command": "uv",
      "args": ["--directory", "/ABSOLUTE/PATH/TO/weather", "run", "weather.py"]
    }
  }
}
```

### Windows
Location: `%APPDATA%\Claude\claude_desktop_config.json`

```json
{
  "mcpServers": {
    "weather": {
      "command": "node",
      "args": ["C:\\PATH\\TO\\weather\\build\\index.js"]
    }
  }
}
```

---

## Query Flow

1. Client gets available tools from server (`list_tools`)
2. Query sent to Claude with tool descriptions
3. Claude decides which tools to use
4. Client executes tool calls through server (`call_tool`)
5. Results sent back to Claude
6. Claude provides natural language response

---

## Best Practices

### Server
- Use logging library that writes to stderr (not stdout)
- Validate inputs thoroughly
- Handle errors gracefully
- Document tool parameters clearly

### Client
- Use `AsyncExitStack` for resource management
- Store API keys in `.env` files
- Validate server responses
- Handle connection issues gracefully

---

*Created for Unity-XR-AI Knowledge Base*
