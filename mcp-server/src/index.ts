#!/usr/bin/env node
/**
 * Unity XR Knowledgebase MCP Server
 *
 * Entry point that:
 * 1. Creates the core server (always)
 * 2. Conditionally loads the KB plugin based on ENABLE_KB_PLUGIN env var
 * 3. Connects to stdio transport for MCP communication
 *
 * Usage:
 *   ENABLE_KB_PLUGIN=true node dist/index.js   # With KB plugin
 *   ENABLE_KB_PLUGIN=false node dist/index.js  # Core only
 */

import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { createCoreServer, serverInfo } from './core/server.js';
import { pluginRegistry } from './core/plugin-registry.js';
import type { PluginContext } from './core/plugin-registry.js';

/**
 * Main entry point
 */
async function main(): Promise<void> {
  console.error(`[Server] Starting ${serverInfo.name} v${serverInfo.version}`);

  // Create core server
  const server = createCoreServer();
  console.error(`[Server] Core server created with base tools`);

  // Prepare plugin context
  const context: PluginContext = {
    kbPath: process.env.KB_PATH,
    config: {
      enableKbPlugin: process.env.ENABLE_KB_PLUGIN !== 'false'
    }
  };

  // Conditionally load KB plugin
  const enableKbPlugin = process.env.ENABLE_KB_PLUGIN !== 'false';

  if (enableKbPlugin) {
    try {
      console.error(`[Server] Loading knowledgebase plugin...`);

      // Dynamic import to allow the plugin to be completely removed
      const kbPluginModule = await import('./plugins/knowledgebase/index.js');
      const kbPlugin = kbPluginModule.default;

      await pluginRegistry.register(kbPlugin, server, context);
      console.error(`[Server] Knowledgebase plugin loaded successfully`);
    } catch (error) {
      // Plugin failed to load - server continues without it
      console.error(`[Server] Failed to load KB plugin (server will continue without it):`, error);
    }
  } else {
    console.error(`[Server] KB plugin disabled (ENABLE_KB_PLUGIN=false)`);
  }

  // Log loaded plugins
  const plugins = pluginRegistry.list();
  console.error(`[Server] ${plugins.length} plugin(s) loaded: ${plugins.map(p => p.name).join(', ') || 'none'}`);

  // Connect to stdio transport
  const transport = new StdioServerTransport();

  // Handle shutdown gracefully
  process.on('SIGINT', async () => {
    console.error(`[Server] Shutting down...`);
    await pluginRegistry.unregisterAll();
    await server.close();
    process.exit(0);
  });

  process.on('SIGTERM', async () => {
    console.error(`[Server] Terminating...`);
    await pluginRegistry.unregisterAll();
    await server.close();
    process.exit(0);
  });

  // Start server
  await server.connect(transport);
  console.error(`[Server] Connected to stdio transport, ready for requests`);
}

// Run
main().catch((error) => {
  console.error(`[Server] Fatal error:`, error);
  process.exit(1);
});
