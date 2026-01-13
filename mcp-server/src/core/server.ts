/**
 * Core MCP Server - Base server with core tools
 *
 * This module creates the MCP server instance and registers base tools
 * that work independently of any plugins. The server remains functional
 * even if all plugins are disabled.
 */

import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { pluginRegistry } from './plugin-registry.js';

const SERVER_NAME = 'unity-xr-kb-mcp';
const SERVER_VERSION = '1.0.0';

/**
 * Server metadata for MCP protocol
 */
export const serverInfo = {
  name: SERVER_NAME,
  version: SERVER_VERSION
};

/**
 * Create and configure the core MCP server with base tools
 */
export function createCoreServer(): McpServer {
  const server = new McpServer(serverInfo, {
    capabilities: {
      tools: {},
      resources: {}
    }
  });

  // Register base tools that always work
  registerCoreTools(server);

  return server;
}

/**
 * Register core tools that work without any plugins
 */
function registerCoreTools(server: McpServer): void {
  // Health check tool
  server.tool(
    'health',
    'Check server health status',
    {},
    async () => {
      const plugins = pluginRegistry.list();
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(
              {
                status: 'healthy',
                server: SERVER_NAME,
                version: SERVER_VERSION,
                uptime: process.uptime(),
                pluginsLoaded: plugins.length,
                timestamp: new Date().toISOString()
              },
              null,
              2
            )
          }
        ]
      };
    }
  );

  // Version info tool
  server.tool(
    'version',
    'Get server version information',
    {},
    async () => {
      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(
              {
                server: SERVER_NAME,
                version: SERVER_VERSION,
                nodeVersion: process.version,
                platform: process.platform,
                arch: process.arch
              },
              null,
              2
            )
          }
        ]
      };
    }
  );

  // List loaded plugins
  server.tool(
    'list_plugins',
    'List all loaded plugins and their status',
    {},
    async () => {
      const plugins = pluginRegistry.list();

      if (plugins.length === 0) {
        return {
          content: [
            {
              type: 'text',
              text: 'No plugins loaded. Core server is running with base tools only.'
            }
          ]
        };
      }

      return {
        content: [
          {
            type: 'text',
            text: JSON.stringify(
              {
                count: plugins.length,
                plugins: plugins.map(p => ({
                  name: p.name,
                  version: p.version,
                  description: p.description,
                  loadedAt: p.loadedAt.toISOString()
                }))
              },
              null,
              2
            )
          }
        ]
      };
    }
  );
}
