/**
 * Plugin Registry - Core module for managing MCP plugins
 *
 * This module provides a clean interface for registering/unregistering
 * plugins at runtime. Plugins can add tools, resources, and prompts
 * to the MCP server.
 */

import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';

/**
 * Interface that all plugins must implement
 */
export interface McpPlugin {
  /** Unique plugin identifier */
  name: string;

  /** Plugin version (semver) */
  version: string;

  /** Human-readable description */
  description?: string;

  /**
   * Initialize the plugin - register tools, resources, prompts
   * Called when the plugin is loaded
   */
  initialize(server: McpServer, context: PluginContext): Promise<void>;

  /**
   * Cleanup function called before plugin is unloaded
   * Use this to close connections, clear caches, etc.
   */
  cleanup?(): Promise<void>;
}

/**
 * Context passed to plugins during initialization
 */
export interface PluginContext {
  /** Path to knowledgebase (if relevant) */
  kbPath?: string;

  /** Server configuration */
  config: Record<string, unknown>;
}

/**
 * Information about a loaded plugin
 */
export interface LoadedPlugin {
  name: string;
  version: string;
  description?: string;
  loadedAt: Date;
}

/**
 * Plugin Registry - manages plugin lifecycle
 */
export class PluginRegistry {
  private plugins: Map<string, McpPlugin> = new Map();
  private loadTimes: Map<string, Date> = new Map();

  /**
   * Register a plugin with the MCP server
   */
  async register(plugin: McpPlugin, server: McpServer, context: PluginContext): Promise<void> {
    if (this.plugins.has(plugin.name)) {
      throw new Error(`Plugin '${plugin.name}' is already registered`);
    }

    try {
      await plugin.initialize(server, context);
      this.plugins.set(plugin.name, plugin);
      this.loadTimes.set(plugin.name, new Date());
      console.error(`[PluginRegistry] Loaded plugin: ${plugin.name} v${plugin.version}`);
    } catch (error) {
      console.error(`[PluginRegistry] Failed to load plugin '${plugin.name}':`, error);
      throw error;
    }
  }

  /**
   * Unregister a plugin (calls cleanup if defined)
   */
  async unregister(pluginName: string): Promise<void> {
    const plugin = this.plugins.get(pluginName);
    if (!plugin) {
      throw new Error(`Plugin '${pluginName}' is not registered`);
    }

    if (plugin.cleanup) {
      try {
        await plugin.cleanup();
      } catch (error) {
        console.error(`[PluginRegistry] Error during cleanup of '${pluginName}':`, error);
      }
    }

    this.plugins.delete(pluginName);
    this.loadTimes.delete(pluginName);
    console.error(`[PluginRegistry] Unloaded plugin: ${pluginName}`);
  }

  /**
   * Check if a plugin is loaded
   */
  has(pluginName: string): boolean {
    return this.plugins.has(pluginName);
  }

  /**
   * Get a loaded plugin by name
   */
  get(pluginName: string): McpPlugin | undefined {
    return this.plugins.get(pluginName);
  }

  /**
   * List all loaded plugins with metadata
   */
  list(): LoadedPlugin[] {
    return Array.from(this.plugins.entries()).map(([name, plugin]) => ({
      name,
      version: plugin.version,
      description: plugin.description,
      loadedAt: this.loadTimes.get(name)!
    }));
  }

  /**
   * Get count of loaded plugins
   */
  get count(): number {
    return this.plugins.size;
  }

  /**
   * Unregister all plugins (for shutdown)
   */
  async unregisterAll(): Promise<void> {
    const names = Array.from(this.plugins.keys());
    for (const name of names) {
      await this.unregister(name);
    }
  }
}

// Singleton instance for use across the server
export const pluginRegistry = new PluginRegistry();
