/**
 * Repository Parser - Parses _MASTER_GITHUB_REPO_KNOWLEDGEBASE.md
 *
 * Extracts GitHub repositories from markdown tables with format:
 * | [owner/repo](url) | Description | Platform Support | Status |
 */

import type { SearchDocument } from '../../../search/search-engine.js';

/**
 * Parsed repository entry
 */
export interface RepoEntry {
  id: string;
  name: string;
  owner: string;
  url: string;
  description: string;
  platforms: string[];
  category: string;
  status?: string;
  iosSupport?: boolean;
}

/**
 * Parse repository knowledgebase markdown file
 */
export function parseRepoKnowledgebase(content: string, sourceFile: string): SearchDocument[] {
  const documents: SearchDocument[] = [];
  const lines = content.split('\n');

  let currentCategory = 'Uncategorized';
  let inTable = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i].trim();

    // Detect category headers (## or ### prefix)
    if (line.startsWith('## ') || line.startsWith('### ')) {
      currentCategory = line.replace(/^#+\s*/, '').replace(/[🎨🎮🧠🧍🎵🌍📱🔧⚡]+/g, '').trim();
      inTable = false;
      continue;
    }

    // Detect table start (line with |---|)
    if (line.includes('|---')) {
      inTable = true;
      continue;
    }

    // Parse table rows
    if (inTable && line.startsWith('|') && !line.includes('Project') && !line.includes('---')) {
      const repo = parseTableRow(line, currentCategory);
      if (repo) {
        documents.push(repoToDocument(repo, sourceFile));
      }
    }

    // End table on empty line or non-table content
    if (inTable && !line.startsWith('|') && line.length > 0 && !line.startsWith('**')) {
      inTable = false;
    }
  }

  return documents;
}

/**
 * Parse a single table row into a RepoEntry
 */
function parseTableRow(line: string, category: string): RepoEntry | null {
  // Split by | and clean up
  const cells = line
    .split('|')
    .map(cell => cell.trim())
    .filter(cell => cell.length > 0);

  if (cells.length < 2) return null;

  // Parse project cell: [owner/repo](url) or [name](url)
  const projectCell = cells[0];
  const linkMatch = projectCell.match(/\[([^\]]+)\]\(([^)]+)\)/);

  if (!linkMatch) return null;

  const name = linkMatch[1];
  const url = linkMatch[2];

  // Extract owner from name or URL
  let owner = '';
  if (name.includes('/')) {
    owner = name.split('/')[0];
  } else if (url.includes('github.com/')) {
    const urlParts = url.split('github.com/')[1]?.split('/');
    owner = urlParts?.[0] || '';
  }

  // Description is usually the second cell
  const description = cells[1] || '';

  // Platform/techniques from third cell
  const platformCell = cells[2] || '';
  const platforms = extractPlatforms(platformCell);

  // Status/iOS support from fourth cell (if exists)
  const statusCell = cells[3] || '';
  const iosSupport = statusCell.includes('✅') || platformCell.toLowerCase().includes('ios');

  // Generate unique ID
  const id = `repo:${name.toLowerCase().replace(/[^a-z0-9]/g, '-')}`;

  return {
    id,
    name,
    owner,
    url,
    description,
    platforms,
    category,
    status: statusCell,
    iosSupport
  };
}

/**
 * Extract platform names from a cell
 */
function extractPlatforms(cell: string): string[] {
  const platforms: string[] = [];
  const cellLower = cell.toLowerCase();

  const platformKeywords = [
    { keyword: 'ios', name: 'iOS' },
    { keyword: 'android', name: 'Android' },
    { keyword: 'webgl', name: 'WebGL' },
    { keyword: 'quest', name: 'Quest' },
    { keyword: 'visionos', name: 'visionOS' },
    { keyword: 'hololens', name: 'HoloLens' },
    { keyword: 'windows', name: 'Windows' },
    { keyword: 'macos', name: 'macOS' },
    { keyword: 'linux', name: 'Linux' },
    { keyword: 'multi-platform', name: 'Multi-platform' },
    { keyword: 'all', name: 'Multi-platform' },
    { keyword: 'unity', name: 'Unity' },
    { keyword: 'vr', name: 'VR' },
    { keyword: 'ar', name: 'AR' },
    { keyword: 'arkit', name: 'ARKit' },
    { keyword: 'arcore', name: 'ARCore' }
  ];

  for (const { keyword, name } of platformKeywords) {
    if (cellLower.includes(keyword) && !platforms.includes(name)) {
      platforms.push(name);
    }
  }

  return platforms;
}

/**
 * Convert RepoEntry to SearchDocument
 */
function repoToDocument(repo: RepoEntry, sourceFile: string): SearchDocument {
  // Build searchable content
  const contentParts = [
    repo.description,
    repo.category,
    repo.platforms.join(' '),
    repo.status || ''
  ].filter(Boolean);

  // Build tags from category and platforms
  const tags = [
    repo.category.toLowerCase().replace(/[^a-z0-9\s]/g, '').trim(),
    ...repo.platforms.map(p => p.toLowerCase())
  ].filter(Boolean);

  return {
    id: repo.id,
    category: 'repos',
    title: repo.name,
    content: contentParts.join(' | '),
    url: repo.url,
    description: repo.description,
    platforms: repo.platforms,
    tags,
    sourceFile,
    metadata: {
      owner: repo.owner,
      iosSupport: repo.iosSupport,
      repoCategory: repo.category,
      status: repo.status
    }
  };
}
