# AI-XR-MCP: 3D WebGL Visualization for MCP

A Model Context Protocol (MCP) server providing interactive 3D WebGL visualizations for websites, GitHub repositories, and local file systems. Built in TypeScript with optimized configurations for various MCP clients.

This tool enables AI models like Claude to generate powerful 3D visualizations of code repositories, websites, and file systems. It's designed to work with the Model Context Protocol (MCP) standard, allowing any MCP-compatible AI assistant to access WebGL-powered visualizations.

## Features

- 🚀 Full TypeScript support with strict type checking
- 🌐 Interactive 3D visualization using Three.js and force-directed graphs
- 🖥️ Visualize websites, GitHub repositories, and local directories
- ⚙️ Multiple layout algorithms: force-directed, radial, and hierarchical
- 📊 Flexible output formats: HTML, JSON, or data URLs
- 🧪 Complete test coverage with Vitest
- 🔌 Ready-to-use configurations for Claude Desktop, Windsurf Browser, and Cursor

## Getting Started

### Development

1. Install dependencies:

   ```bash
   npm install
   ```

2. Start the development server with hot reload:

   ```bash
   npm run dev
   ```

3. Build the project:

   ```bash
   npm run build
   ```

4. Run tests:

   ```bash
   npm test
   ```

5. Start the production server:

   ```bash
   npm start
   ```

### Using with Claude CLI

1. Add this configuration to your `~/.claude/config.json`:

   ```json
   {
     "mcp": {
       "providers": {
         "webgl3d": {
           "description": "3D Visualization Tool",
           "url": "http://localhost:3000"
         }
       }
     }
   }
   ```

2. Launch Claude with the MCP provider:

   ```bash
   claude --mcp-provider webgl3d
   ```

3. Use the tool with commands like:

   ```
   /create3d https://example.com website
   ```

## Project Structure

```
.
├── src/                # Source code
│   ├── index.ts        # Entry point
│   ├── tools/          # Tool implementations
│   │   ├── example.ts  # Example tool
│   │   └── webgl3d.ts  # 3D WebGL visualization tool
│   └── utils/          # Utility functions and types
├── configs/            # Client-specific configurations
│   ├── claude-desktop.json  # Claude Desktop config
│   ├── cursor.json     # Cursor Editor config
│   └── windsurf.json   # Windsurf Browser config
└── shortcuts/          # Command shortcuts
    └── webgl3d.json    # /create3d shortcut definition
```

## WebGL 3D Visualization Tool

Generate interactive 3D visualizations for websites, GitHub repositories, or local directories using WebGL and force-directed graphs.

### Tool Usage

```json
{
  "name": "webgl-3d-visualization",
  "input": {
    "source": "https://example.com", 
    "sourceType": "website",
    "depth": 2,
    "layout": "force",
    "outputFormat": "html"
  }
}
```

### Parameters

- **source**: URL of website, GitHub repository, or path to local directory
- **sourceType**: Type of source to visualize (`website`, `github`, or `local`)
- **depth**: How deep to scan (1-5, default: 2)
- **layout**: Layout algorithm to use (`force`, `radial`, or `hierarchical`, default: `force`)
- **outputFormat**: Format of visualization output (`html`, `json`, or `url`, default: `html`)

### Visualization Examples

#### Website Structure

Creates a 3D graph showing the HTML structure of a website:

```json
{
  "name": "webgl-3d-visualization",
  "input": {
    "source": "https://example.com",
    "sourceType": "website",
    "depth": 2
  }
}
```

#### GitHub Repository

Creates a 3D graph showing the file and directory structure of a GitHub repository:

```json
{
  "name": "webgl-3d-visualization",
  "input": {
    "source": "https://github.com/username/repo",
    "sourceType": "github"
  }
}
```

#### Local Directory

Creates a hierarchical 3D graph showing the file and directory structure of a local folder:

```json
{
  "name": "webgl-3d-visualization",
  "input": {
    "source": "/path/to/directory",
    "sourceType": "local",
    "depth": 3,
    "layout": "hierarchical"
  }
}
```

## Shortcut Commands

Use the `/create3d` shortcut for quick access to the visualization tool:

```
/create3d [source] [sourceType] [layout] [depth]
```

### Examples

```
# Visualize a website
/create3d https://example.com website

# Visualize a GitHub repository
/create3d https://github.com/username/repo github

# Visualize a local directory with hierarchical layout
/create3d /path/to/directory local hierarchical 3
```

## Client Configurations

### Claude Desktop

The `configs/claude-desktop.json` file configures the tool for Claude Desktop with optimized settings:

```json
{
  "name": "AI-XR-MCP-3D-Visualization",
  "description": "Generate 3D WebGL visualizations of websites, repositories, and file structures",
  "tools": [
    {
      "name": "webgl-3d-visualization",
      "description": "Generate a 3D force-directed graph visualization for websites, GitHub repositories, or local file systems",
      "icon": "🌐",
      "preferredOutput": "html",
      "documentation": "Generates interactive 3D visualizations from websites, repositories, and directories"
    }
  ],
  "settings": {
    "defaultOutputFormat": "html",
    "maxDepth": 3
  }
}
```

To use:
1. Place the configuration file in the Claude Desktop MCP tools directory
2. Restart Claude Desktop to see the new tool

### Windsurf Browser

The `configs/windsurf_mcp_config.json` file integrates the tool with the Windsurf browser extension:

```json
{
  "servers": [
    {
      "id": "ai-xr-mcp-3d",
      "name": "3D Visualization Tool",
      "description": "Generate interactive 3D visualizations of web content and code repositories",
      "command": "npx example-mcp-tool",
      "type": "command",
      "enabled": true,
      "defaultTools": ["webgl-3d-visualization"],
      "config": {
        "serverName": "AI-XR-MCP-3D",
        "clientName": "WindSurf Browser AI",
        "tools": [
          {
            "name": "webgl-3d-visualization",
            "shortDescription": "Create 3D visualizations",
            "icon": "3d_rotation",
            "quickAccess": true
          }
        ],
        "permissions": {
          "fileSystem": { "read": true },
          "network": { "domains": ["*"] }
        }
      },
      "shortcuts": [
        {
          "name": "create3d",
          "description": "Create a 3D visualization",
          "command": "/create3d"
        }
      ]
    }
  ],
  "defaultServer": "ai-xr-mcp-3d",
  "settings": { "autoStart": true }
}
```

To use:
1. Import the configuration in the Windsurf extension settings
2. Enable the tool in the Windsurf AI menu

### Cursor Editor

The `configs/cursor.json` file configures the tool for the Cursor code editor:

```json
{
  "serverName": "XR-3D-Visualization",
  "serverDescription": "Generate interactive 3D visualizations for code analysis and exploration",
  "serverCommand": "npx example-mcp-tool",
  "tools": [
    {
      "name": "webgl-3d-visualization",
      "description": "Generate a 3D force-directed graph visualization for websites, GitHub repositories, or local file systems",
      "showInContext": true,
      "category": "Visualization",
      "icon": "construction",
      "allowedModels": ["*"]
    }
  ],
  "contextActions": [
    {
      "name": "Visualize Repository",
      "description": "Create a 3D visualization of the current repository structure",
      "tool": "webgl-3d-visualization",
      "defaultParams": {
        "sourceType": "local",
        "depth": 3,
        "layout": "hierarchical"
      },
      "showOn": ["repo", "directory"]
    }
  ],
  "configuration": {
    "autoStart": true,
    "debug": false
  }
}
```

To use:
1. Open Cursor settings
2. Go to the Features tab and navigate to MCP Servers section
3. Click "Add Server" and choose "Import from file"
4. Select the cursor.json file

## Testing

### Using TestClient

The TestClient allows for easy testing of tools:

```typescript
import { TestClient } from "./utils/TestClient";

describe("WebGL3D", () => {
  const client = new TestClient();

  it("should process website correctly", async () => {
    const result = await client.callTool(
      "webgl-3d-visualization",
      { 
        source: "https://example.com", 
        sourceType: "website"
      }
    );
    expect(result.toolResult.content).toBeDefined();
  });
});
```

### Using MCP Inspector

For visual debugging of your tools:

1. Start the inspector:

   ```bash
   npx @modelcontextprotocol/inspector node dist/index.js
   ```

2. Open the inspector UI at http://localhost:5173

## Local Development with Cursor

To test your MCP server locally with Cursor:

1. Build and link the package:

   ```bash
   npm run build
   npm run link
   ```

2. Add the server to Cursor:
   - Use the provided `configs/cursor.json` file
   - Or manually add a new server pointing to `npx example-mcp-tool`

3. Verify the server starts correctly and test your visualizations

## Use Cases

The WebGL 3D visualization tool can be used for:

- **Code Structure Visualization**: Explore complex codebases in 3D space to understand relationships between files and directories
- **Website Analysis**: Visualize website DOM structures to understand page composition and hierarchy
- **Repository Exploration**: Navigate GitHub repositories in an interactive 3D environment
- **Dependency Mapping**: Visualize relationships between components in large systems
- **Educational Purposes**: Create interactive 3D graphs for teaching software architecture concepts
- **Data Presentation**: Generate visually engaging representations of hierarchical data structures

## How It Works

The tool leverages several key technologies to create interactive 3D visualizations:

1. **Three.js**: Provides WebGL-based 3D rendering capabilities
2. **Force-directed graphs**: Position nodes dynamically based on physics simulations
3. **JSDOM**: Parse website DOM structures for visualization
4. **Octokit**: Interface with GitHub API to access repository structures
5. **Node.js file system API**: Process local directory structures
6. **MCP Protocol**: Connect to AI models through a standardized interface

When processing a source (website, GitHub repo, or local directory), the tool:
1. Parses the structure into a graph data model with nodes and links
2. Applies a layout algorithm (force, radial, or hierarchical)
3. Generates an interactive HTML visualization with Three.js
4. Returns the visualization to the client

## License

MIT