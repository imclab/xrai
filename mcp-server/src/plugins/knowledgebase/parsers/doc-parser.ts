/**
 * Document Parser - Parses general markdown documentation files
 *
 * Creates searchable documents from markdown files with:
 * - Title from first H1 or filename
 * - Description from first paragraph
 * - Full content for searching
 */

import type { SearchDocument } from '../../../search/search-engine.js';
import * as path from 'path';

/**
 * Parse a general markdown document
 */
export function parseMarkdownDocument(content: string, filePath: string): SearchDocument {
  const filename = path.basename(filePath, '.md');
  const lines = content.split('\n');

  // Extract title from first H1 or use filename
  let title = filename.replace(/^_+/, '').replace(/_/g, ' ');
  let description = '';
  let firstParagraphFound = false;

  for (const line of lines) {
    const trimmedLine = line.trim();

    // First H1 becomes title
    if (trimmedLine.startsWith('# ') && title === filename.replace(/^_+/, '').replace(/_/g, ' ')) {
      title = trimmedLine
        .replace(/^#\s*/, '')
        .replace(/[🎨🎮🧠🧍🎵🌍📱🔧⚡]+/g, '')
        .trim();
      continue;
    }

    // First non-empty, non-header, non-metadata line becomes description
    if (
      !firstParagraphFound &&
      trimmedLine.length > 0 &&
      !trimmedLine.startsWith('#') &&
      !trimmedLine.startsWith('**') &&
      !trimmedLine.startsWith('|') &&
      !trimmedLine.startsWith('-') &&
      !trimmedLine.startsWith('```')
    ) {
      description = trimmedLine.slice(0, 200);
      if (trimmedLine.length > 200) description += '...';
      firstParagraphFound = true;
    }

    // Stop after finding both
    if (title !== filename && firstParagraphFound) break;
  }

  // Extract metadata from common patterns
  const metadata = extractMetadata(content);

  // Extract tags from content
  const tags = extractTags(content, filename);

  // Extract platforms
  const platforms = extractPlatforms(content);

  // Generate ID from filename
  const id = `doc:${filename.toLowerCase().replace(/[^a-z0-9]/g, '-')}`;

  return {
    id,
    category: 'docs',
    title,
    content,
    description: description || `Documentation: ${title}`,
    platforms: platforms.length > 0 ? platforms : undefined,
    tags,
    sourceFile: filePath,
    metadata
  };
}

/**
 * Extract metadata from markdown frontmatter or headers
 */
function extractMetadata(content: string): Record<string, unknown> {
  const metadata: Record<string, unknown> = {};

  // Look for common metadata patterns
  const patterns = [
    { regex: /\*\*Purpose\*\*:\s*(.+)/i, key: 'purpose' },
    { regex: /\*\*Last Updated\*\*:\s*(.+)/i, key: 'lastUpdated' },
    { regex: /\*\*Version\*\*:\s*(.+)/i, key: 'version' },
    { regex: /\*\*Status\*\*:\s*(.+)/i, key: 'status' },
    { regex: /\*\*Author\*\*:\s*(.+)/i, key: 'author' }
  ];

  for (const { regex, key } of patterns) {
    const match = content.match(regex);
    if (match) {
      metadata[key] = match[1].trim();
    }
  }

  // Count sections
  const sectionCount = (content.match(/^##\s+/gm) || []).length;
  metadata.sectionCount = sectionCount;

  // Count code blocks
  const codeBlockCount = (content.match(/```/g) || []).length / 2;
  metadata.codeBlockCount = Math.floor(codeBlockCount);

  return metadata;
}

/**
 * Extract tags from content and filename
 */
function extractTags(content: string, filename: string): string[] {
  const tags = new Set<string>();

  // Add filename-based tags
  const filenameParts = filename.toLowerCase().replace(/^_+/, '').split('_');
  for (const part of filenameParts) {
    if (part.length > 2) tags.add(part);
  }

  // Add technology tags based on content
  const techKeywords = [
    'arfoundation', 'arkit', 'arcore', 'unity', 'vfx', 'vfx graph',
    'dots', 'ecs', 'urp', 'hdrp', 'sentis', 'barracuda', 'mediapipe',
    'webgl', 'webxr', 'three.js', 'babylon', 'react native', 'normcore',
    'multiplayer', 'networking', 'lidar', 'depth', 'segmentation',
    'face tracking', 'body tracking', 'hand tracking', 'gaussian splatting'
  ];

  const contentLower = content.toLowerCase();
  for (const keyword of techKeywords) {
    if (contentLower.includes(keyword)) {
      tags.add(keyword.replace(/\s+/g, '-'));
    }
  }

  return Array.from(tags);
}

/**
 * Extract platforms from content
 */
function extractPlatforms(content: string): string[] {
  const platforms: string[] = [];
  const contentLower = content.toLowerCase();

  const platformKeywords = [
    { keyword: 'ios', name: 'iOS' },
    { keyword: 'android', name: 'Android' },
    { keyword: 'webgl', name: 'WebGL' },
    { keyword: 'quest', name: 'Quest' },
    { keyword: 'visionos', name: 'visionOS' },
    { keyword: 'hololens', name: 'HoloLens' },
    { keyword: 'windows', name: 'Windows' },
    { keyword: 'macos', name: 'macOS' }
  ];

  for (const { keyword, name } of platformKeywords) {
    if (contentLower.includes(keyword) && !platforms.includes(name)) {
      platforms.push(name);
    }
  }

  return platforms;
}

/**
 * Check if a file should be parsed as a specialized format
 */
export function getParserType(filename: string): 'repo' | 'snippet' | 'doc' {
  const lowerFilename = filename.toLowerCase();

  if (lowerFilename.includes('github_repo_knowledgebase')) {
    return 'repo';
  }

  if (lowerFilename.includes('vfx_knowledge_base') || lowerFilename.includes('code_patterns')) {
    return 'snippet';
  }

  return 'doc';
}
