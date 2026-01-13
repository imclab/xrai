/**
 * XRAI Knowledge Graph - Core Module
 * Platform-agnostic knowledge graph with fuzzy search and chat interface
 *
 * Works in: Browser, Node.js, VS Code, Chrome Extension, Electron
 */

export class KnowledgeGraph {
    constructor(options = {}) {
        this.entities = new Map();
        this.relations = [];
        this.index = new Map(); // For fast lookups
        this.typeIndex = new Map(); // Index by type
        this.searchCache = new Map(); // LRU cache for search results
        this.maxCacheSize = options.maxCacheSize || 100;
        this.listeners = new Set();

        // Performance tracking
        this.stats = {
            entityCount: 0,
            relationCount: 0,
            lastModified: null,
            searchCount: 0
        };
    }

    // ========== DATA OPERATIONS ==========

    addEntity(entity) {
        const id = entity.id || this.generateId(entity.name);
        const normalized = {
            id,
            name: entity.name,
            entityType: entity.entityType || entity.type || 'Unknown',
            observations: entity.observations || [],
            metadata: entity.metadata || {},
            createdAt: Date.now()
        };

        this.entities.set(id, normalized);
        this._updateIndex(normalized);
        this.stats.entityCount = this.entities.size;
        this.stats.lastModified = Date.now();
        this._clearSearchCache();
        this._emit('entityAdded', normalized);

        return normalized;
    }

    addRelation(relation) {
        const normalized = {
            id: relation.id || `rel_${this.relations.length}`,
            from: relation.from,
            to: relation.to,
            relationType: relation.relationType || relation.type || 'related_to',
            weight: relation.weight || 1,
            metadata: relation.metadata || {}
        };

        this.relations.push(normalized);
        this.stats.relationCount = this.relations.length;
        this.stats.lastModified = Date.now();
        this._emit('relationAdded', normalized);

        return normalized;
    }

    bulkImport(data) {
        const startTime = performance.now();
        const results = { entities: 0, relations: 0 };

        if (data.entities) {
            data.entities.forEach(e => {
                this.addEntity(e);
                results.entities++;
            });
        }

        if (data.relations) {
            data.relations.forEach(r => {
                this.addRelation(r);
                results.relations++;
            });
        }

        results.duration = performance.now() - startTime;
        this._emit('bulkImport', results);
        return results;
    }

    getEntity(id) {
        return this.entities.get(id);
    }

    getEntitiesByType(type) {
        return this.typeIndex.get(type.toLowerCase()) || [];
    }

    getRelationsFor(entityId) {
        return this.relations.filter(r => r.from === entityId || r.to === entityId);
    }

    getAllEntities() {
        return Array.from(this.entities.values());
    }

    getAllRelations() {
        return [...this.relations];
    }

    // ========== INDEXING ==========

    _updateIndex(entity) {
        // Word index for search
        const words = this._tokenize(entity.name);
        words.forEach(word => {
            if (!this.index.has(word)) {
                this.index.set(word, new Set());
            }
            this.index.get(word).add(entity.id);
        });

        // Type index
        const type = (entity.entityType || 'unknown').toLowerCase();
        if (!this.typeIndex.has(type)) {
            this.typeIndex.set(type, []);
        }
        this.typeIndex.get(type).push(entity);
    }

    _tokenize(text) {
        return (text || '')
            .toLowerCase()
            .split(/[\s\-_./\\]+/)
            .filter(w => w.length > 1);
    }

    // ========== FUZZY SEARCH ==========

    search(query, options = {}) {
        const {
            limit = 20,
            threshold = 0.3, // 0 = exact, 1 = match anything
            types = null,    // Filter by types
            fuzzy = true
        } = options;

        this.stats.searchCount++;

        // Check cache
        const cacheKey = `${query}|${limit}|${threshold}|${types?.join(',')}|${fuzzy}`;
        if (this.searchCache.has(cacheKey)) {
            return this.searchCache.get(cacheKey);
        }

        const queryLower = query.toLowerCase();
        const queryTokens = this._tokenize(query);
        const results = [];

        this.entities.forEach(entity => {
            // Type filter
            if (types && !types.includes(entity.entityType.toLowerCase())) {
                return;
            }

            const score = fuzzy
                ? this._fuzzyScore(queryLower, queryTokens, entity)
                : this._exactScore(queryLower, entity);

            if (score >= threshold) {
                results.push({ entity, score });
            }
        });

        // Sort by score descending
        results.sort((a, b) => b.score - a.score);
        const limited = results.slice(0, limit);

        // Cache result
        this._addToCache(cacheKey, limited);

        return limited;
    }

    _fuzzyScore(queryLower, queryTokens, entity) {
        const nameLower = entity.name.toLowerCase();
        let score = 0;

        // Exact match bonus
        if (nameLower === queryLower) return 1.0;

        // Starts with bonus
        if (nameLower.startsWith(queryLower)) score += 0.8;

        // Contains bonus
        else if (nameLower.includes(queryLower)) score += 0.6;

        // Token matching
        const nameTokens = this._tokenize(entity.name);
        let tokenMatches = 0;

        queryTokens.forEach(qt => {
            nameTokens.forEach(nt => {
                if (nt === qt) tokenMatches += 1.0;
                else if (nt.startsWith(qt)) tokenMatches += 0.7;
                else if (nt.includes(qt)) tokenMatches += 0.4;
                else {
                    // Levenshtein distance for typo tolerance
                    const dist = this._levenshtein(qt, nt);
                    if (dist <= 2 && nt.length > 3) {
                        tokenMatches += 0.3 * (1 - dist / Math.max(qt.length, nt.length));
                    }
                }
            });
        });

        if (queryTokens.length > 0) {
            score += (tokenMatches / queryTokens.length) * 0.4;
        }

        // Observation matching (lower weight)
        if (entity.observations) {
            const obsText = entity.observations.join(' ').toLowerCase();
            if (obsText.includes(queryLower)) score += 0.15;
        }

        // Type matching
        if (entity.entityType.toLowerCase().includes(queryLower)) {
            score += 0.1;
        }

        return Math.min(score, 1.0);
    }

    _exactScore(queryLower, entity) {
        const nameLower = entity.name.toLowerCase();
        if (nameLower === queryLower) return 1.0;
        if (nameLower.includes(queryLower)) return 0.5;
        return 0;
    }

    _levenshtein(a, b) {
        if (a.length === 0) return b.length;
        if (b.length === 0) return a.length;

        const matrix = [];
        for (let i = 0; i <= b.length; i++) matrix[i] = [i];
        for (let j = 0; j <= a.length; j++) matrix[0][j] = j;

        for (let i = 1; i <= b.length; i++) {
            for (let j = 1; j <= a.length; j++) {
                const cost = a[j - 1] === b[i - 1] ? 0 : 1;
                matrix[i][j] = Math.min(
                    matrix[i - 1][j] + 1,
                    matrix[i][j - 1] + 1,
                    matrix[i - 1][j - 1] + cost
                );
            }
        }

        return matrix[b.length][a.length];
    }

    // ========== SUGGESTIONS ==========

    getSuggestions(partial, limit = 5) {
        if (!partial || partial.length < 2) return [];

        const results = this.search(partial, { limit, threshold: 0.2, fuzzy: true });
        return results.map(r => ({
            text: r.entity.name,
            type: r.entity.entityType,
            score: r.score
        }));
    }

    getRelatedEntities(entityId, depth = 1) {
        const visited = new Set([entityId]);
        let current = [entityId];
        const related = [];

        for (let d = 0; d < depth; d++) {
            const next = [];
            current.forEach(id => {
                this.relations.forEach(r => {
                    let targetId = null;
                    if (r.from === id && !visited.has(r.to)) targetId = r.to;
                    else if (r.to === id && !visited.has(r.from)) targetId = r.from;

                    if (targetId) {
                        visited.add(targetId);
                        next.push(targetId);
                        related.push({
                            entity: this.entities.get(targetId),
                            relation: r,
                            depth: d + 1
                        });
                    }
                });
            });
            current = next;
        }

        return related;
    }

    // ========== FILTERING ==========

    filter(predicate) {
        const results = [];
        this.entities.forEach(entity => {
            if (predicate(entity)) results.push(entity);
        });
        return results;
    }

    filterByTypes(types) {
        const typeSet = new Set(types.map(t => t.toLowerCase()));
        return this.filter(e => typeSet.has(e.entityType.toLowerCase()));
    }

    // ========== EXPORT ==========

    toJSON() {
        return {
            entities: this.getAllEntities(),
            relations: this.getAllRelations(),
            stats: { ...this.stats }
        };
    }

    toMermaid(options = {}) {
        const { direction = 'TB', maxNodes = 50 } = options;
        let code = `%%{init: {"theme": "dark"}}%%\nflowchart ${direction}\n`;

        const entities = this.getAllEntities().slice(0, maxNodes);
        entities.forEach(e => {
            const id = this._sanitizeId(e.name);
            const shape = this._getMermaidShape(e.entityType);
            code += `    ${id}${shape[0]}"${e.name.substring(0, 30)}"${shape[1]}\n`;
        });

        const entityNames = new Set(entities.map(e => e.name));
        this.relations.forEach(r => {
            if (entityNames.has(r.from) && entityNames.has(r.to)) {
                code += `    ${this._sanitizeId(r.from)} -->|"${r.relationType}"| ${this._sanitizeId(r.to)}\n`;
            }
        });

        return code;
    }

    _sanitizeId(name) {
        return 'n_' + name.replace(/[^a-zA-Z0-9]/g, '_').substring(0, 30);
    }

    _getMermaidShape(type) {
        const shapes = {
            project: ['([', '])'],
            repository: ['[[', ']]'],
            pattern: ['{{', '}}'],
            technology: ['[/', '/]'],
            concept: ['((', '))']
        };
        return shapes[type?.toLowerCase()] || ['[', ']'];
    }

    // ========== UTILITIES ==========

    generateId(name) {
        let hash = 0;
        for (let i = 0; i < name.length; i++) {
            hash = ((hash << 5) - hash) + name.charCodeAt(i);
            hash = hash & hash;
        }
        return `kg_${Math.abs(hash).toString(16)}`;
    }

    _clearSearchCache() {
        this.searchCache.clear();
    }

    _addToCache(key, value) {
        if (this.searchCache.size >= this.maxCacheSize) {
            const firstKey = this.searchCache.keys().next().value;
            this.searchCache.delete(firstKey);
        }
        this.searchCache.set(key, value);
    }

    // ========== EVENT SYSTEM ==========

    on(callback) {
        this.listeners.add(callback);
        return () => this.listeners.delete(callback);
    }

    _emit(event, data) {
        this.listeners.forEach(cb => cb({ type: event, data, timestamp: Date.now() }));
    }

    // ========== STATIC FACTORY ==========

    static fromMCPData(mcpData) {
        const kg = new KnowledgeGraph();
        kg.bulkImport(mcpData);
        return kg;
    }

    static fromJSON(json) {
        const data = typeof json === 'string' ? JSON.parse(json) : json;
        return KnowledgeGraph.fromMCPData(data);
    }
}

export default KnowledgeGraph;
