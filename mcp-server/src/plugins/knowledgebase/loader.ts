/**
 * Knowledgebase Loader - Reads and indexes all KB markdown files
 *
 * Scans the KnowledgeBase directory, determines parser type for each file,
 * and loads all documents into the search engine.
 */

import * as fs from 'fs/promises';
import * as path from 'path';
import { SearchEngine, type SearchDocument } from '../../search/search-engine.js';
import { parseRepoKnowledgebase } from './parsers/repo-parser.js';
import { parseSnippetKnowledgebase } from './parsers/snippet-parser.js';
import { parseMarkdownDocument, getParserType } from './parsers/doc-parser.js';

/**
 * Loader statistics
 */
export interface LoaderStats {
  totalFiles: number;
  totalDocuments: number;
  byCategory: Record<string, number>;
  byPlatform: Record<string, number>;
  loadTimeMs: number;
  errors: string[];
}

/**
 * Files to skip during loading
 */
const SKIP_FILES = [
  '.ds_store',
  'readme.md',
  '.gitignore',
  '.gitkeep'
];

/**
 * Directories to skip
 */
const SKIP_DIRS = [
  '.git',
  '.claude',
  'node_modules',
  'scripts'
];

/**
 * Load all knowledgebase files into the search engine
 */
export async function loadKnowledgebase(
  kbPath: string,
  searchEngine: SearchEngine
): Promise<LoaderStats> {
  const startTime = Date.now();
  const stats: LoaderStats = {
    totalFiles: 0,
    totalDocuments: 0,
    byCategory: {},
    byPlatform: {},
    loadTimeMs: 0,
    errors: []
  };

  try {
    // Verify KB path exists
    await fs.access(kbPath);

    // Recursively find all markdown files
    const files = await findMarkdownFiles(kbPath);
    stats.totalFiles = files.length;

    console.error(`[KBLoader] Found ${files.length} markdown files in ${kbPath}`);

    // Process each file
    for (const filePath of files) {
      try {
        const documents = await processFile(filePath);

        for (const doc of documents) {
          searchEngine.addDocument(doc);
          stats.totalDocuments++;

          // Track category counts
          stats.byCategory[doc.category] = (stats.byCategory[doc.category] || 0) + 1;

          // Track platform counts
          if (doc.platforms) {
            for (const platform of doc.platforms) {
              stats.byPlatform[platform] = (stats.byPlatform[platform] || 0) + 1;
            }
          }
        }
      } catch (error) {
        const errorMsg = `Failed to process ${filePath}: ${error}`;
        console.error(`[KBLoader] ${errorMsg}`);
        stats.errors.push(errorMsg);
      }
    }

    stats.loadTimeMs = Date.now() - startTime;
    console.error(`[KBLoader] Loaded ${stats.totalDocuments} documents in ${stats.loadTimeMs}ms`);

  } catch (error) {
    const errorMsg = `Failed to access KB path ${kbPath}: ${error}`;
    console.error(`[KBLoader] ${errorMsg}`);
    stats.errors.push(errorMsg);
    stats.loadTimeMs = Date.now() - startTime;
  }

  return stats;
}

/**
 * Recursively find all markdown files in a directory
 */
async function findMarkdownFiles(dirPath: string): Promise<string[]> {
  const files: string[] = [];

  try {
    const entries = await fs.readdir(dirPath, { withFileTypes: true });

    for (const entry of entries) {
      const fullPath = path.join(dirPath, entry.name);
      const lowerName = entry.name.toLowerCase();

      if (entry.isDirectory()) {
        // Skip certain directories
        if (!SKIP_DIRS.includes(lowerName)) {
          const subFiles = await findMarkdownFiles(fullPath);
          files.push(...subFiles);
        }
      } else if (entry.isFile()) {
        // Include markdown files, skip others
        if (lowerName.endsWith('.md') && !SKIP_FILES.includes(lowerName)) {
          files.push(fullPath);
        }
      }
    }
  } catch (error) {
    console.error(`[KBLoader] Error reading directory ${dirPath}: ${error}`);
  }

  return files;
}

/**
 * Process a single markdown file and return documents
 */
async function processFile(filePath: string): Promise<SearchDocument[]> {
  const content = await fs.readFile(filePath, 'utf-8');
  const filename = path.basename(filePath);
  const parserType = getParserType(filename);

  switch (parserType) {
    case 'repo':
      return parseRepoKnowledgebase(content, filePath);

    case 'snippet':
      return parseSnippetKnowledgebase(content, filePath);

    case 'doc':
    default:
      return [parseMarkdownDocument(content, filePath)];
  }
}

/**
 * Reload the knowledgebase (clear and reload)
 */
export async function reloadKnowledgebase(
  kbPath: string,
  searchEngine: SearchEngine
): Promise<LoaderStats> {
  searchEngine.clear();
  return loadKnowledgebase(kbPath, searchEngine);
}
