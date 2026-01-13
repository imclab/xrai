/**
 * Snippet Parser - Parses _ARFOUNDATION_VFX_KNOWLEDGE_BASE.md
 *
 * Extracts code snippets and patterns with format:
 * #### Key Technique: Title
 * **Source**: filename
 * **Repos**: related repos
 * **Pattern**: ```csharp code ```
 * **Insights**: bullet points
 * **Applications**: bullet points
 */

import type { SearchDocument } from '../../../search/search-engine.js';

/**
 * Parsed code snippet entry
 */
export interface SnippetEntry {
  id: string;
  title: string;
  source: string;
  relatedRepos: string[];
  pattern: string;
  language: string;
  insights: string[];
  applications: string[];
  category: string;
}

/**
 * Parse VFX knowledgebase markdown file for code snippets
 */
export function parseSnippetKnowledgebase(content: string, sourceFile: string): SearchDocument[] {
  const documents: SearchDocument[] = [];
  const lines = content.split('\n');

  let currentCategory = 'General';
  let currentSnippet: Partial<SnippetEntry> | null = null;
  let inCodeBlock = false;
  let codeBlockContent: string[] = [];
  let codeBlockLanguage = '';
  let collectingSection = '';

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    const trimmedLine = line.trim();

    // Detect category headers (## or ### prefix)
    if (trimmedLine.startsWith('## ') || trimmedLine.startsWith('### ')) {
      // Save previous snippet if exists
      if (currentSnippet?.title) {
        documents.push(snippetToDocument(currentSnippet as SnippetEntry, sourceFile));
      }
      currentSnippet = null;

      currentCategory = trimmedLine
        .replace(/^#+\s*/, '')
        .replace(/[🎯🎨🎮🧠🧍🎵🌍📱🔧⚡\d.]+/g, '')
        .trim();
      continue;
    }

    // Detect technique headers (#### Key Technique: Title)
    if (trimmedLine.startsWith('#### Key Technique:') || trimmedLine.startsWith('#### Pattern:')) {
      // Save previous snippet if exists
      if (currentSnippet?.title) {
        documents.push(snippetToDocument(currentSnippet as SnippetEntry, sourceFile));
      }

      const title = trimmedLine.replace(/^####\s*(Key Technique|Pattern):\s*/, '').trim();
      currentSnippet = {
        id: `snippet:${title.toLowerCase().replace(/[^a-z0-9]/g, '-')}`,
        title,
        source: '',
        relatedRepos: [],
        pattern: '',
        language: 'csharp',
        insights: [],
        applications: [],
        category: currentCategory
      };
      collectingSection = '';
      continue;
    }

    // Skip if no current snippet
    if (!currentSnippet) continue;

    // Handle code blocks
    if (trimmedLine.startsWith('```')) {
      if (!inCodeBlock) {
        inCodeBlock = true;
        codeBlockContent = [];
        codeBlockLanguage = trimmedLine.replace('```', '').trim() || 'csharp';
      } else {
        inCodeBlock = false;
        currentSnippet.pattern = codeBlockContent.join('\n');
        currentSnippet.language = codeBlockLanguage;
        collectingSection = '';
      }
      continue;
    }

    if (inCodeBlock) {
      codeBlockContent.push(line);
      continue;
    }

    // Parse metadata fields
    if (trimmedLine.startsWith('**Source**:')) {
      currentSnippet.source = trimmedLine.replace('**Source**:', '').trim();
      continue;
    }

    if (trimmedLine.startsWith('**Repos**:') || trimmedLine.startsWith('**Related Repos**:')) {
      const reposText = trimmedLine.replace(/\*\*(Related )?Repos\*\*:/, '').trim();
      currentSnippet.relatedRepos = parseRepoList(reposText);
      continue;
    }

    if (trimmedLine.startsWith('**Pattern**:')) {
      collectingSection = 'pattern';
      continue;
    }

    if (trimmedLine.startsWith('**Insights**:')) {
      collectingSection = 'insights';
      continue;
    }

    if (trimmedLine.startsWith('**Applications**:')) {
      collectingSection = 'applications';
      continue;
    }

    // Collect bullet points for current section
    if (trimmedLine.startsWith('-') || trimmedLine.startsWith('*')) {
      const bulletContent = trimmedLine.replace(/^[-*]\s*/, '').trim();
      if (collectingSection === 'insights') {
        currentSnippet.insights?.push(bulletContent);
      } else if (collectingSection === 'applications') {
        currentSnippet.applications?.push(bulletContent);
      }
    }

    // Reset section on horizontal rule or empty significant content
    if (trimmedLine === '---') {
      collectingSection = '';
    }
  }

  // Don't forget the last snippet
  if (currentSnippet?.title) {
    documents.push(snippetToDocument(currentSnippet as SnippetEntry, sourceFile));
  }

  return documents;
}

/**
 * Parse comma-separated repo list
 */
function parseRepoList(text: string): string[] {
  return text
    .split(/[,;]/)
    .map(repo => repo.trim())
    .filter(repo => repo.length > 0);
}

/**
 * Convert SnippetEntry to SearchDocument
 */
function snippetToDocument(snippet: SnippetEntry, sourceFile: string): SearchDocument {
  // Build searchable content
  const contentParts = [
    snippet.pattern,
    snippet.source,
    snippet.insights.join(' '),
    snippet.applications.join(' '),
    snippet.relatedRepos.join(' ')
  ].filter(Boolean);

  // Build tags
  const tags = [
    snippet.language,
    snippet.category.toLowerCase().replace(/[^a-z0-9\s]/g, '').trim(),
    'code',
    'snippet'
  ].filter(Boolean);

  // Extract platforms from insights/applications
  const platforms: string[] = [];
  const allText = contentParts.join(' ').toLowerCase();
  if (allText.includes('ios') || allText.includes('arkit')) platforms.push('iOS');
  if (allText.includes('android') || allText.includes('arcore')) platforms.push('Android');
  if (allText.includes('quest')) platforms.push('Quest');
  if (allText.includes('visionos')) platforms.push('visionOS');

  return {
    id: snippet.id,
    category: 'snippets',
    title: snippet.title,
    content: contentParts.join('\n\n'),
    description: `${snippet.category} - ${snippet.source || 'Code pattern'}`,
    platforms: platforms.length > 0 ? platforms : undefined,
    tags,
    sourceFile,
    metadata: {
      source: snippet.source,
      relatedRepos: snippet.relatedRepos,
      language: snippet.language,
      snippetCategory: snippet.category,
      insights: snippet.insights,
      applications: snippet.applications,
      codePreview: snippet.pattern.slice(0, 200) + (snippet.pattern.length > 200 ? '...' : '')
    }
  };
}
