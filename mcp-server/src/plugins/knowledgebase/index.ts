/**
 * Knowledgebase Plugin - MCP plugin for Unity XR knowledgebase search
 *
 * This plugin is completely modular and can be enabled/disabled via
 * the ENABLE_KB_PLUGIN environment variable. The core server works
 * independently without this plugin.
 */

import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { McpPlugin, PluginContext } from '../../core/plugin-registry.js';
import { SearchEngine, searchEngine } from '../../search/search-engine.js';
import { loadKnowledgebase, type LoaderStats } from './loader.js';
import * as path from 'path';

/**
 * Plugin metadata
 */
const PLUGIN_NAME = 'knowledgebase';
const PLUGIN_VERSION = '1.0.0';
const PLUGIN_DESCRIPTION = 'Search 530+ Unity XR GitHub repos, code snippets, and documentation';

/**
 * Internal state
 */
let loaderStats: LoaderStats | null = null;
let kbPath: string = '';

/**
 * Knowledgebase Plugin Implementation
 */
const knowledgebasePlugin: McpPlugin = {
  name: PLUGIN_NAME,
  version: PLUGIN_VERSION,
  description: PLUGIN_DESCRIPTION,

  async initialize(server: McpServer, context: PluginContext): Promise<void> {
    // Determine KB path
    kbPath = context.kbPath || process.env.KB_PATH || '../KnowledgeBase';

    // Resolve relative paths
    if (!path.isAbsolute(kbPath)) {
      kbPath = path.resolve(process.cwd(), kbPath);
    }

    console.error(`[KBPlugin] Initializing with KB path: ${kbPath}`);

    // Load knowledgebase into search engine
    loaderStats = await loadKnowledgebase(kbPath, searchEngine);

    if (loaderStats.errors.length > 0) {
      console.error(`[KBPlugin] Loaded with ${loaderStats.errors.length} errors`);
    }

    // Register KB tools
    registerKbTools(server);

    console.error(`[KBPlugin] Initialized successfully`);
  },

  async cleanup(): Promise<void> {
    searchEngine.clear();
    loaderStats = null;
    console.error(`[KBPlugin] Cleaned up`);
  }
};

/**
 * Register all knowledgebase-specific tools
 */
function registerKbTools(server: McpServer): void {
  // kb_search - Main search tool
  server.tool(
    'kb_search',
    'Search the Unity XR knowledgebase for repos, code snippets, and documentation',
    {
      query: z.string().describe('Search query (e.g., "ARFoundation body tracking", "VFX particle")'),
      category: z.enum(['repos', 'snippets', 'docs', 'platform-matrix']).optional()
        .describe('Filter by category'),
      platform: z.string().optional()
        .describe('Filter by platform (iOS, Android, Quest, visionOS, WebGL, etc.)'),
      limit: z.number().min(1).max(50).optional().default(10)
        .describe('Maximum number of results (default: 10)')
    },
    async ({ query, category, platform, limit }) => {
      const results = searchEngine.search(query, {
        category,
        platform,
        limit: limit || 10
      });

      if (results.length === 0) {
        return {
          content: [{
            type: 'text',
            text: `No results found for "${query}"${category ? ` in category "${category}"` : ''}${platform ? ` for platform "${platform}"` : ''}`
          }]
        };
      }

      // Format results
      const formatted = results.map((r, i) => {
        const doc = r.document;
        const lines = [
          `### ${i + 1}. ${doc.title}`,
          `**Category**: ${doc.category} | **Score**: ${(r.score * 100).toFixed(1)}%`
        ];

        if (doc.url) lines.push(`**URL**: ${doc.url}`);
        if (doc.description) lines.push(`**Description**: ${doc.description}`);
        if (doc.platforms?.length) lines.push(`**Platforms**: ${doc.platforms.join(', ')}`);

        // Show highlights if available
        if (r.highlights?.length) {
          lines.push(`**Match**: "${r.highlights[0]}"`);
        }

        // Show code preview for snippets
        if (doc.category === 'snippets' && doc.metadata?.codePreview) {
          lines.push('```');
          lines.push(doc.metadata.codePreview as string);
          lines.push('```');
        }

        return lines.join('\n');
      });

      return {
        content: [{
          type: 'text',
          text: [
            `## Search Results for "${query}"`,
            `Found ${results.length} results${category ? ` in "${category}"` : ''}${platform ? ` for "${platform}"` : ''}`,
            '',
            ...formatted
          ].join('\n')
        }]
      };
    }
  );

  // kb_list_categories - List available categories
  server.tool(
    'kb_list_categories',
    'List all available categories in the knowledgebase with document counts',
    {},
    async () => {
      const categories = searchEngine.getCategories();
      const platforms = searchEngine.getPlatforms();

      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            categories: categories.sort((a, b) => b.count - a.count),
            platforms: platforms.sort((a, b) => b.count - a.count),
            totalDocuments: searchEngine.size
          }, null, 2)
        }]
      };
    }
  );

  // kb_get_repo - Get specific repo by ID or name
  server.tool(
    'kb_get_repo',
    'Get detailed information about a specific GitHub repository',
    {
      query: z.string().describe('Repository name or partial match (e.g., "keijiro/SplatVFX" or "SplatVFX")')
    },
    async ({ query }) => {
      // Search in repos category
      const results = searchEngine.search(query, {
        category: 'repos',
        limit: 5
      });

      if (results.length === 0) {
        return {
          content: [{
            type: 'text',
            text: `No repository found matching "${query}"`
          }]
        };
      }

      // Return detailed info for top match
      const top = results[0].document;

      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            id: top.id,
            name: top.title,
            url: top.url,
            description: top.description,
            category: top.metadata?.repoCategory,
            platforms: top.platforms,
            owner: top.metadata?.owner,
            iosSupport: top.metadata?.iosSupport,
            tags: top.tags,
            sourceFile: top.sourceFile,
            matchScore: (results[0].score * 100).toFixed(1) + '%',
            otherMatches: results.slice(1).map(r => ({
              name: r.document.title,
              score: (r.score * 100).toFixed(1) + '%'
            }))
          }, null, 2)
        }]
      };
    }
  );

  // kb_get_snippet - Get specific code snippet
  server.tool(
    'kb_get_snippet',
    'Get a specific code snippet/pattern with full code and insights',
    {
      query: z.string().describe('Snippet title or technique (e.g., "ARKit Human Segmentation" or "FFT")')
    },
    async ({ query }) => {
      // Search in snippets category
      const results = searchEngine.search(query, {
        category: 'snippets',
        limit: 5
      });

      if (results.length === 0) {
        return {
          content: [{
            type: 'text',
            text: `No code snippet found matching "${query}"`
          }]
        };
      }

      // Return detailed info for top match
      const top = results[0].document;
      const metadata = top.metadata || {};

      // Format the full snippet
      const lines = [
        `# ${top.title}`,
        '',
        `**Category**: ${metadata.snippetCategory || top.category}`,
        `**Source**: ${metadata.source || 'Unknown'}`,
        ''
      ];

      if (metadata.relatedRepos && (metadata.relatedRepos as string[]).length > 0) {
        lines.push(`**Related Repos**: ${(metadata.relatedRepos as string[]).join(', ')}`);
        lines.push('');
      }

      // Full code
      lines.push('## Code Pattern');
      lines.push('```' + (metadata.language || 'csharp'));
      lines.push(top.content.split('\n\n')[0] || top.content); // First block is usually the code
      lines.push('```');
      lines.push('');

      // Insights
      if (metadata.insights && (metadata.insights as string[]).length > 0) {
        lines.push('## Insights');
        for (const insight of metadata.insights as string[]) {
          lines.push(`- ${insight}`);
        }
        lines.push('');
      }

      // Applications
      if (metadata.applications && (metadata.applications as string[]).length > 0) {
        lines.push('## Applications');
        for (const app of metadata.applications as string[]) {
          lines.push(`- ${app}`);
        }
      }

      return {
        content: [{
          type: 'text',
          text: lines.join('\n')
        }]
      };
    }
  );

  // kb_stats - Get knowledgebase statistics
  server.tool(
    'kb_stats',
    'Get statistics about the loaded knowledgebase',
    {},
    async () => {
      return {
        content: [{
          type: 'text',
          text: JSON.stringify({
            kbPath,
            loaderStats,
            searchEngineSize: searchEngine.size,
            categories: searchEngine.getCategories(),
            platforms: searchEngine.getPlatforms()
          }, null, 2)
        }]
      };
    }
  );
}

export default knowledgebasePlugin;
