/**
 * Search Engine - Full-text search with category and platform filtering
 *
 * A lightweight, in-memory search engine optimized for the knowledgebase.
 * Supports fuzzy matching, relevance scoring, and multi-field search.
 */

/**
 * Searchable document with category and metadata
 */
export interface SearchDocument {
  /** Unique identifier */
  id: string;

  /** Document category (repos, snippets, docs, platform-matrix) */
  category: string;

  /** Document title */
  title: string;

  /** Full text content for searching */
  content: string;

  /** Optional URL */
  url?: string;

  /** Optional description */
  description?: string;

  /** Supported platforms */
  platforms?: string[];

  /** Tags for filtering */
  tags?: string[];

  /** Source file path */
  sourceFile?: string;

  /** Additional metadata */
  metadata?: Record<string, unknown>;
}

/**
 * Search result with relevance score
 */
export interface SearchResult {
  document: SearchDocument;
  score: number;
  matchedFields: string[];
  highlights?: string[];
}

/**
 * Search options
 */
export interface SearchOptions {
  /** Filter by category */
  category?: string;

  /** Filter by platform */
  platform?: string;

  /** Maximum results to return */
  limit?: number;

  /** Minimum score threshold (0-1) */
  minScore?: number;
}

/**
 * Search Engine class
 */
export class SearchEngine {
  private documents: Map<string, SearchDocument> = new Map();
  private categoryIndex: Map<string, Set<string>> = new Map();
  private platformIndex: Map<string, Set<string>> = new Map();

  /**
   * Add a document to the search index
   */
  addDocument(doc: SearchDocument): void {
    this.documents.set(doc.id, doc);

    // Update category index
    if (!this.categoryIndex.has(doc.category)) {
      this.categoryIndex.set(doc.category, new Set());
    }
    this.categoryIndex.get(doc.category)!.add(doc.id);

    // Update platform index
    if (doc.platforms) {
      for (const platform of doc.platforms) {
        const normalizedPlatform = platform.toLowerCase();
        if (!this.platformIndex.has(normalizedPlatform)) {
          this.platformIndex.set(normalizedPlatform, new Set());
        }
        this.platformIndex.get(normalizedPlatform)!.add(doc.id);
      }
    }
  }

  /**
   * Add multiple documents at once
   */
  addDocuments(docs: SearchDocument[]): void {
    for (const doc of docs) {
      this.addDocument(doc);
    }
  }

  /**
   * Remove a document from the index
   */
  removeDocument(id: string): boolean {
    const doc = this.documents.get(id);
    if (!doc) return false;

    this.documents.delete(id);
    this.categoryIndex.get(doc.category)?.delete(id);

    if (doc.platforms) {
      for (const platform of doc.platforms) {
        this.platformIndex.get(platform.toLowerCase())?.delete(id);
      }
    }

    return true;
  }

  /**
   * Clear all documents from the index
   */
  clear(): void {
    this.documents.clear();
    this.categoryIndex.clear();
    this.platformIndex.clear();
  }

  /**
   * Search documents with query and optional filters
   */
  search(query: string, options: SearchOptions = {}): SearchResult[] {
    const { category, platform, limit = 20, minScore = 0.1 } = options;

    // Get candidate document IDs based on filters
    let candidateIds: Set<string>;

    if (category && platform) {
      // Intersection of category and platform filters
      const categoryIds = this.categoryIndex.get(category) || new Set();
      const platformIds = this.platformIndex.get(platform.toLowerCase()) || new Set();
      candidateIds = new Set([...categoryIds].filter(id => platformIds.has(id)));
    } else if (category) {
      candidateIds = this.categoryIndex.get(category) || new Set();
    } else if (platform) {
      candidateIds = this.platformIndex.get(platform.toLowerCase()) || new Set();
    } else {
      candidateIds = new Set(this.documents.keys());
    }

    // Score and rank documents
    const results: SearchResult[] = [];
    const queryTerms = this.tokenize(query);

    for (const id of candidateIds) {
      const doc = this.documents.get(id)!;
      const result = this.scoreDocument(doc, queryTerms);

      if (result.score >= minScore) {
        results.push(result);
      }
    }

    // Sort by score descending
    results.sort((a, b) => b.score - a.score);

    // Apply limit
    return results.slice(0, limit);
  }

  /**
   * Get a document by ID
   */
  getDocument(id: string): SearchDocument | undefined {
    return this.documents.get(id);
  }

  /**
   * Get all documents in a category
   */
  getByCategory(category: string): SearchDocument[] {
    const ids = this.categoryIndex.get(category) || new Set();
    return Array.from(ids).map(id => this.documents.get(id)!);
  }

  /**
   * Get all available categories
   */
  getCategories(): { name: string; count: number }[] {
    return Array.from(this.categoryIndex.entries()).map(([name, ids]) => ({
      name,
      count: ids.size
    }));
  }

  /**
   * Get all available platforms
   */
  getPlatforms(): { name: string; count: number }[] {
    return Array.from(this.platformIndex.entries()).map(([name, ids]) => ({
      name,
      count: ids.size
    }));
  }

  /**
   * Get total document count
   */
  get size(): number {
    return this.documents.size;
  }

  /**
   * Score a document against query terms
   */
  private scoreDocument(doc: SearchDocument, queryTerms: string[]): SearchResult {
    let totalScore = 0;
    const matchedFields: string[] = [];
    const highlights: string[] = [];

    // Field weights for scoring
    const weights = {
      title: 3.0,
      description: 2.0,
      content: 1.0,
      tags: 2.5
    };

    for (const term of queryTerms) {
      const termLower = term.toLowerCase();

      // Title match (highest weight)
      if (doc.title.toLowerCase().includes(termLower)) {
        totalScore += weights.title;
        if (!matchedFields.includes('title')) matchedFields.push('title');
        highlights.push(this.extractHighlight(doc.title, term));
      }

      // Description match
      if (doc.description?.toLowerCase().includes(termLower)) {
        totalScore += weights.description;
        if (!matchedFields.includes('description')) matchedFields.push('description');
        highlights.push(this.extractHighlight(doc.description, term));
      }

      // Content match
      if (doc.content.toLowerCase().includes(termLower)) {
        totalScore += weights.content;
        if (!matchedFields.includes('content')) matchedFields.push('content');
        highlights.push(this.extractHighlight(doc.content, term));
      }

      // Tags match
      if (doc.tags?.some(tag => tag.toLowerCase().includes(termLower))) {
        totalScore += weights.tags;
        if (!matchedFields.includes('tags')) matchedFields.push('tags');
      }
    }

    // Normalize score (0-1 range based on max possible score)
    const maxScore = queryTerms.length * (weights.title + weights.description + weights.content + weights.tags);
    const normalizedScore = maxScore > 0 ? totalScore / maxScore : 0;

    return {
      document: doc,
      score: normalizedScore,
      matchedFields,
      highlights: highlights.slice(0, 3) // Limit highlights
    };
  }

  /**
   * Tokenize a query string into search terms
   */
  private tokenize(query: string): string[] {
    return query
      .toLowerCase()
      .split(/\s+/)
      .filter(term => term.length > 1); // Filter out single characters
  }

  /**
   * Extract a highlighted snippet around a match
   */
  private extractHighlight(text: string, term: string, contextLength: number = 50): string {
    const lowerText = text.toLowerCase();
    const lowerTerm = term.toLowerCase();
    const index = lowerText.indexOf(lowerTerm);

    if (index === -1) return '';

    const start = Math.max(0, index - contextLength);
    const end = Math.min(text.length, index + term.length + contextLength);

    let snippet = text.slice(start, end);

    // Add ellipsis if truncated
    if (start > 0) snippet = '...' + snippet;
    if (end < text.length) snippet = snippet + '...';

    return snippet;
  }
}

// Singleton instance for the KB plugin
export const searchEngine = new SearchEngine();
