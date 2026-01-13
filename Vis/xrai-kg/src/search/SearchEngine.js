/**
 * XRAI Knowledge Graph - Search Layer
 * Fuzzy search with Fuse.js (or fallback to built-in)
 *
 * Principle: Use proven libraries, don't reinvent the wheel
 */

// Check if Fuse.js is available
let Fuse = null;
try {
    // Dynamic import for environments that support it
    if (typeof window !== 'undefined' && window.Fuse) {
        Fuse = window.Fuse;
    }
} catch (e) {
    // Fuse not available, will use fallback
}

/**
 * SearchEngine wraps Fuse.js for fuzzy search
 * Falls back to built-in search if Fuse.js not available
 */
export class SearchEngine {
    constructor(knowledgeGraph, options = {}) {
        this.kg = knowledgeGraph;
        this.options = {
            // Fuse.js options
            threshold: 0.3,          // 0 = exact, 1 = match anything
            distance: 100,           // Max distance for fuzzy match
            minMatchCharLength: 2,
            includeScore: true,
            includeMatches: true,
            ignoreLocation: true,    // Match anywhere in string
            useExtendedSearch: true, // Enable ^ prefix, etc.

            // Search keys (fields to search)
            keys: [
                { name: 'name', weight: 0.7 },
                { name: 'entityType', weight: 0.2 },
                { name: 'observations', weight: 0.1 }
            ],

            // Cache settings
            cacheEnabled: true,
            cacheMaxSize: 100,
            cacheTTL: 60000,         // 1 minute

            ...options
        };

        this.cache = new Map();
        this.fuse = null;
        this._buildIndex();

        // Rebuild index when data changes
        this.kg.on((event) => {
            if (['entityAdded', 'entityRemoved', 'entityUpdated', 'bulkAdd', 'cleared'].includes(event.type)) {
                this._buildIndex();
            }
        });
    }

    /**
     * Build/rebuild the search index
     */
    _buildIndex() {
        const entities = this.kg.getAllEntities().map(e => ({
            id: e.id,
            name: e.name,
            entityType: e.entityType,
            observations: e.observations?.join(' ') || ''
        }));

        if (Fuse) {
            // Use Fuse.js
            this.fuse = new Fuse(entities, {
                threshold: this.options.threshold,
                distance: this.options.distance,
                minMatchCharLength: this.options.minMatchCharLength,
                includeScore: this.options.includeScore,
                includeMatches: this.options.includeMatches,
                ignoreLocation: this.options.ignoreLocation,
                useExtendedSearch: this.options.useExtendedSearch,
                keys: this.options.keys
            });
        } else {
            // Store for fallback search
            this._entities = entities;
        }

        // Clear cache on reindex
        this.cache.clear();
    }

    /**
     * Fuzzy search entities
     * @param {string} query - Search query
     * @param {Object} options - Search options
     * @returns {Array} Search results with scores
     */
    search(query, options = {}) {
        const {
            limit = 20,
            threshold = this.options.threshold,
            types = null,  // Filter by entity types
            minScore = 0   // Minimum score (0-1)
        } = options;

        if (!query || query.trim().length < 2) {
            return [];
        }

        const cacheKey = `${query}|${limit}|${threshold}|${types?.join(',')}`;

        // Check cache
        if (this.options.cacheEnabled) {
            const cached = this._getFromCache(cacheKey);
            if (cached) return cached;
        }

        let results;

        if (this.fuse) {
            // Use Fuse.js search
            results = this.fuse.search(query, { limit: limit * 2 });
            results = results.map(r => ({
                entity: this.kg.getEntity(r.item.id),
                score: 1 - r.score,  // Fuse returns 0 = best, convert to 1 = best
                matches: r.matches
            }));
        } else {
            // Fallback search
            results = this._fallbackSearch(query, limit * 2);
        }

        // Apply filters
        if (types && types.length > 0) {
            const typeSet = new Set(types.map(t => t.toLowerCase()));
            results = results.filter(r =>
                typeSet.has(r.entity?.entityType?.toLowerCase())
            );
        }

        // Filter by minimum score
        if (minScore > 0) {
            results = results.filter(r => r.score >= minScore);
        }

        // Limit results
        results = results.slice(0, limit);

        // Cache results
        if (this.options.cacheEnabled) {
            this._addToCache(cacheKey, results);
        }

        return results;
    }

    /**
     * Fallback search without Fuse.js
     */
    _fallbackSearch(query, limit) {
        const queryLower = query.toLowerCase();
        const results = [];

        this._entities.forEach(item => {
            const score = this._calculateScore(queryLower, item);
            if (score > 0) {
                results.push({
                    entity: this.kg.getEntity(item.id),
                    score
                });
            }
        });

        results.sort((a, b) => b.score - a.score);
        return results.slice(0, limit);
    }

    /**
     * Simple scoring for fallback search
     */
    _calculateScore(query, item) {
        const nameLower = item.name.toLowerCase();
        const typeLower = item.entityType.toLowerCase();
        const obsLower = item.observations.toLowerCase();

        let score = 0;

        // Exact match
        if (nameLower === query) return 1.0;

        // Starts with
        if (nameLower.startsWith(query)) score = Math.max(score, 0.9);

        // Contains
        if (nameLower.includes(query)) score = Math.max(score, 0.7);

        // Type match
        if (typeLower.includes(query)) score = Math.max(score, 0.4);

        // Observation match
        if (obsLower.includes(query)) score = Math.max(score, 0.3);

        // Word matching
        const queryWords = query.split(/\s+/);
        const nameWords = nameLower.split(/[\s\-_./]+/);

        let wordMatches = 0;
        queryWords.forEach(qw => {
            if (nameWords.some(nw => nw.startsWith(qw) || nw.includes(qw))) {
                wordMatches++;
            }
        });

        if (queryWords.length > 0) {
            score = Math.max(score, (wordMatches / queryWords.length) * 0.6);
        }

        return score;
    }

    /**
     * Get autocomplete suggestions
     */
    suggest(partial, limit = 5) {
        if (!partial || partial.length < 1) {
            return [];
        }

        const results = this.search(partial, { limit, threshold: 0.4 });

        return results.map(r => ({
            text: r.entity.name,
            type: r.entity.entityType,
            score: r.score
        }));
    }

    /**
     * Filter entities by predicate
     */
    filter(predicate) {
        return this.kg.getAllEntities().filter(predicate);
    }

    /**
     * Filter by entity types
     */
    filterByTypes(types) {
        const typeSet = new Set(types.map(t => t.toLowerCase()));
        return this.filter(e => typeSet.has(e.entityType.toLowerCase()));
    }

    /**
     * Get all unique entity types
     */
    getTypes() {
        const types = new Map();
        this.kg.getAllEntities().forEach(e => {
            const t = e.entityType;
            types.set(t, (types.get(t) || 0) + 1);
        });

        return Array.from(types.entries())
            .map(([name, count]) => ({ name, count }))
            .sort((a, b) => b.count - a.count);
    }

    // ========== CACHE MANAGEMENT ==========

    _getFromCache(key) {
        const entry = this.cache.get(key);
        if (!entry) return null;

        // Check TTL
        if (Date.now() - entry.timestamp > this.options.cacheTTL) {
            this.cache.delete(key);
            return null;
        }

        return entry.data;
    }

    _addToCache(key, data) {
        // Evict old entries if cache is full
        if (this.cache.size >= this.options.cacheMaxSize) {
            const oldestKey = this.cache.keys().next().value;
            this.cache.delete(oldestKey);
        }

        this.cache.set(key, {
            data,
            timestamp: Date.now()
        });
    }

    clearCache() {
        this.cache.clear();
    }

    // ========== CONFIGURATION ==========

    /**
     * Set Fuse.js instance (for dependency injection)
     */
    static setFuseLibrary(FuseLib) {
        Fuse = FuseLib;
    }

    /**
     * Update search options
     */
    setOptions(options) {
        this.options = { ...this.options, ...options };
        this._buildIndex();
    }
}

export default SearchEngine;
